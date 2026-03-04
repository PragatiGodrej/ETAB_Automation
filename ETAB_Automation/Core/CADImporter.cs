
// ============================================================================
// FILE: Core/CADImporter.cs — VERSION 4.6
//
// ELEVATION MODEL (matches StoryManager v6.0):
//
//   foundationHeight = 1.5m  (checked in UI → distance base to basement slab)
//   storyHeights[0]  = 3.5m  (Basement1 wall height above its slab)
//
//   ETABS Story Table:
//     Base       = 0.0m
//     Basement1  ETABS height=1.5  → Plan View Z=1.5m  ✓
//     Podium1    ETABS height=3.5  → Plan View Z=5.0m  ✓
//     Ground     ETABS height=4.5  → Plan View Z=9.5m  ✓
//
//   GEOMETRY PLACEMENT (CADImporter):
//     Basement (foundationHeight > 0):
//       Foundation walls : Z=0.000 → 1.500  (height=foundationHeight)
//       Basement walls   : Z=1.500 → 5.000  (height=storyHeights[0]=3.5)
//       Slab/Beams       : Z=1.505            (foundationHeight + 0.005)
//       Columns          : Z=1.500 → 5.000
//       geomBase = storyManager.GetStoryBaseElevation() = 0.0
//       geomTop  = storyManager.GetStoryTopElevation()  = 5.0
//
//     Normal stories:
//       Walls/Slab/Beams/Columns: same as before (geomBase → geomTop)
//
// CHANGES from v4.5:
//   - Basement Step A (foundation walls): placed Z=geomBase → foundationHeight
//     geomBase is now 0.0 from StoryManager v6.0 (was foundationHeight in v5.0)
//   - Basement Step B (basement walls): placed Z=foundationHeight, height=wallHt-foundationHeight
//     wallHt = geomTop - geomBase = foundationHeight + userWallHeight (e.g. 5.0m)
//     so Step B height = wallHt - foundationHeight = userWallHeight (e.g. 3.5m) ✓
//   - Column base for basement = foundationHeight (unchanged)
//   - Column height for basement = wallHt - foundationHeight = userWallHeight ✓
//   - Debug header updated to v4.6
// ============================================================================

using ETAB_Automation.Models;
using ETABS_CAD_Automation.Importers;
using ETABS_CAD_Automation.Models;
using ETABSv1;
using netDxf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ETAB_Automation.Core
{
    public class CADImporterEnhanced
    {
        private cSapModel sapModel;
        private StoryManager storyManager;

        public CADImporterEnhanced(cSapModel model)
        {
            sapModel = model;
            storyManager = new StoryManager(model);
        }

        public bool ImportMultiFloorTypeCAD(
            List<FloorTypeConfig> floorConfigs,
            List<double> storyHeights,
            List<double> rawHeights,
            List<string> storyNames,
            string seismicZone,
            List<string> wallGrades,
            List<int> floorsPerGrade,
            double foundationHeight = 0.0)
        {
            try
            {
                Debug.WriteLine("\n╔═════════════════════════════════════╗");
                Debug.WriteLine("║  CAD IMPORTER v4.6                 ║");
                Debug.WriteLine($"║  foundationHeight = {foundationHeight:F3}m           ║");
                Debug.WriteLine("╚═════════════════════════════════════╝\n");

                sapModel.SetModelIsLocked(false);
                sapModel.SetPresentUnits(eUnits.N_m_C);

                var gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);
                int totalStories = storyHeights.Count;

                if (!gradeSchedule.ValidateTotalFloors(totalStories))
                {
                    MessageBox.Show(
                        $"Grade schedule count mismatch!\nExpected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
                        "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                int totalTypical = CalculateTotalTypicalFloors(floorConfigs);
                int wallRef = totalTypical > 0 ? totalTypical : totalStories;

                ShowDesignNotes(floorConfigs, totalTypical, seismicZone, gradeSchedule, foundationHeight);

                // StoryManager v6.0:
                //   Basement1 ETABS height = foundationHeight → Plan View Z = 1.5m ✓
                //   storyBaseElevations[0] = 0.0  (geometry starts at base)
                //   storyTopElevations[0]  = foundationHeight + wallHeight = 5.0m
                Debug.WriteLine("📐 Creating stories (StoryManager v6.2)...");
                storyManager.DefineStoriesWithCustomNames(storyHeights, rawHeights, storyNames, foundationHeight);

                Debug.WriteLine("🔍 Validating CAD alignment...");
                if (!ValidateCADCoordinates(floorConfigs)) return false;

                WallThicknessCalculator.LoadAvailableWallSections(sapModel);
                ColumnImporter.ClearSectionCache();

                int currentStoryIndex = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    Debug.WriteLine($"\n┌─ FLOOR TYPE: {floorConfig.Name.ToUpper()}");

                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
                    if (dxfDoc == null)
                    {
                        MessageBox.Show(
                            $"Failed to load CAD: {floorConfig.CADFilePath}",
                            "CAD Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    Debug.WriteLine($"│  ✓ {System.IO.Path.GetFileName(floorConfig.CADFilePath)}");

                    var beamImporter = new BeamImporterEnhanced(
                        sapModel, dxfDoc, seismicZone, wallRef,
                        floorConfig.BeamDepths, gradeSchedule, floorConfig.BeamWidthOverrides);

                    var slabImporter = new SlabImporterEnhanced(
                        sapModel, dxfDoc, floorConfig.SlabThicknesses, gradeSchedule);

                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        if (currentStoryIndex >= totalStories)
                        {
                            MessageBox.Show(
                                $"Story index {currentStoryIndex} out of bounds (total={totalStories}).",
                                "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        // geomBase = 0.0 for basement (v6.0), or cumulative base for normal stories
                        // geomTop  = foundationHeight + wallHeight for basement, or cumulative top
                        // wallHt   = geomTop - geomBase = full geometry height
                        double geomBase = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double geomTop = storyManager.GetStoryTopElevation(currentStoryIndex);
                        double slabZ = storyManager.GetStoryPlanViewZ(currentStoryIndex);  // ETABS Plan View Z
                        double wallHt = geomTop - geomBase;   // raw wall height
                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
                        string storyName = storyManager.GetStoryNameByIndex(currentStoryIndex);
                        bool isBasement = floorConfig.IsIndividualBasement;
                        bool isTerrace = storyName.Equals("Terrace", StringComparison.OrdinalIgnoreCase);

                        Debug.WriteLine($"│");
                        Debug.WriteLine($"│  [{floor + 1}/{floorConfig.Count}] {storyName}  (idx={currentStoryIndex})");
                        Debug.WriteLine($"│    GeomBase={geomBase:F3}  GeomTop={geomTop:F3}  WallHt={wallHt:F3}");
                        Debug.WriteLine($"│    Grade={wallGrade}  NTA={floorConfig.NtaWallThickness}mm");

                        // Foundation zone applies to:
                        //   (a) Basement floors (always) when foundationHeight > 0
                        //   (b) First story (index=0) when no basement present and foundationHeight > 0
                        //       e.g. no basement → Podium1 is first floor, foundation walls drawn at 0→fdn
                        bool isFirstStoryNoBasement = (currentStoryIndex == 0 && !isBasement && foundationHeight > 0);
                        if ((isBasement || isFirstStoryNoBasement) && foundationHeight > 0)
                        {
                            // ── BASEMENT WITH FOUNDATION ZONE ────────────────
                            //
                            // StoryManager v6.0 gives us:
                            //   geomBase = 0.0
                            //   geomTop  = foundationHeight + userWallHeight = 5.0m
                            //   wallHt   = 5.0m (total)
                            //
                            // Step A — Foundation walls: Z=0 → foundationHeight (1.5m)
                            // Step B — Basement walls:   Z=foundationHeight → geomTop (1.5 → 5.0m)
                            // Slab/Beams:                Z=foundationHeight + 0.005 = 1.505m
                            // Columns:                   Z=foundationHeight → geomTop

                            double basementWallHeight = wallHt - foundationHeight; // = 3.5m

                            // ── STEP A: Foundation walls ──────────────────────
                            Debug.WriteLine($"│    [A] Foundation walls  Z={geomBase:F3} → {foundationHeight:F3}m");

                            var foundWallImporter = new WallImporterEnhanced(
                                sapModel, dxfDoc,
                                foundationHeight,        // height = 1.5m
                                wallRef, seismicZone, gradeSchedule,
                                floorConfig.NtaWallThickness,
                                floorConfig.WallThicknessOverrides);

                            foundWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                geomBase,               // placed at Z=0.0
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Foundation walls: {geomBase:F3} → {foundationHeight:F3}m");

                            // ── STEP B: Basement walls ────────────────────────
                            Debug.WriteLine($"│    [B] Basement walls    Z={foundationHeight:F3} → {geomTop:F3}m  (height={basementWallHeight:F3}m)");

                            var basementWallImporter = new WallImporterEnhanced(
                                sapModel, dxfDoc,
                                basementWallHeight,      // height = 3.5m
                                wallRef, seismicZone, gradeSchedule,
                                floorConfig.NtaWallThickness,
                                floorConfig.WallThicknessOverrides);

                            basementWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                foundationHeight,        // placed at Z=1.5m
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Basement walls: {foundationHeight:F3} → {geomTop:F3}m");

                            // ── Slab / Beams ──────────────────────────────────
                            // Basement slab sits at foundationHeight = Plan View Z of Basement1
                            double slabElev = slabZ;  // = foundationHeight = Plan View Z ✓
                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m  (Plan View Z={slabZ:F3}m)");
                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                        }
                        else
                        {
                            // ── NORMAL STORY ──────────────────────────────────
                            // Slab/beams placed at Plan View Z (ETABS story top elevation)
                            // This matches exactly where ETABS expects the slab for each story
                            double slabElev = slabZ;  // Plan View Z ✓

                            Debug.WriteLine($"│    Walls  Z={geomBase:F3} → {geomTop:F3}m");
                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m");

                            if (!isTerrace)
                            {
                                var wallImporter = new WallImporterEnhanced(
                                    sapModel, dxfDoc,
                                    wallHt,
                                    wallRef, seismicZone, gradeSchedule,
                                    floorConfig.NtaWallThickness,
                                    floorConfig.WallThicknessOverrides);

                                wallImporter.ImportWalls(
                                    floorConfig.LayerMapping, geomBase, currentStoryIndex);
                            }
                            else
                            {
                                Debug.WriteLine("│    ⛔ Terrace — walls skipped");
                            }

                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                        }

                        // ── COLUMNS ───────────────────────────────────────────
                        // Basement: columns sit above foundation zone (Z=foundationHeight)
                        //   height = basementWallHeight = wallHt - foundationHeight
                        // Normal:   columns start at geomBase, height = wallHt
                        var columnLayers = floorConfig.LayerMapping
                            .Where(kv => kv.Value.Equals("Column", StringComparison.OrdinalIgnoreCase))
                            .Select(kv => kv.Key)
                            .ToList();

                        if (columnLayers.Count > 0 && floorConfig.ColumnB > 0 && floorConfig.ColumnD > 0)
                        {
                            double colBaseZ, colHeight;

                            // Foundation zone applies to:
                            //   (a) Basement floors (always) when foundationHeight > 0
                            //   (b) First story (index=0) when no basement present and foundationHeight > 0
                            //       e.g. no basement → Podium1 is first floor, foundation walls drawn at 0→fdn
                            isFirstStoryNoBasement = (currentStoryIndex == 0 && !isBasement && foundationHeight > 0);
                            if ((isBasement || isFirstStoryNoBasement) && foundationHeight > 0)
                            {
                                colBaseZ = foundationHeight;              // Z=1.5m
                                colHeight = wallHt - foundationHeight;     // 3.5m
                            }
                            else
                            {
                                colBaseZ = geomBase;
                                colHeight = wallHt;
                            }

                            Debug.WriteLine($"│");
                            Debug.WriteLine($"│    Columns  Z={colBaseZ:F3} → {colBaseZ + colHeight:F3}m" +
                                $"  B={floorConfig.ColumnB}mm  D={floorConfig.ColumnD}mm" +
                                $"  layers=[{string.Join(", ", columnLayers)}]");

                            var colImporter = new ColumnImporter(
                                sapModel, dxfDoc, gradeSchedule, currentStoryIndex);

                            colImporter.ImportColumns(
                                columnLayers,
                                floorConfig.ColumnB,
                                floorConfig.ColumnD,
                                colBaseZ,
                                colHeight,
                                storyName);

                            Debug.WriteLine($"│    ✅ Columns placed={colImporter.ColumnsCreated}" +
                                $"  failed={colImporter.ColumnsFailed}");
                        }
                        else if (columnLayers.Count == 0)
                        {
                            Debug.WriteLine("│    ⚠ No Column layers mapped — columns skipped.");
                        }
                        else
                        {
                            Debug.WriteLine("│    ⚠ ColumnB or ColumnD is 0 — columns skipped.");
                        }

                        currentStoryIndex++;
                        if ((floor + 1) % 5 == 0) sapModel.View.RefreshView(0, false);
                    }

                    Debug.WriteLine($"└─ {floorConfig.Name} complete");
                    sapModel.View.RefreshView(0, false);
                }

                sapModel.View.RefreshView(0, true);
                ShowImportSummary(floorConfigs, totalStories,
                    storyManager.GetTotalBuildingHeight(),
                    totalTypical, seismicZone, gradeSchedule, foundationHeight);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n❌ {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Import failed:\n\n{ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private bool ValidateCADCoordinates(List<FloorTypeConfig> floorConfigs)
        {
            const double TOL = 500.0;
            var c = new Dictionary<string, (double cx, double cy, string n)>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in floorConfigs)
            {
                if (string.IsNullOrEmpty(f.CADFilePath) || c.ContainsKey(f.CADFilePath)) continue;
                try
                {
                    var doc = DxfDocument.Load(f.CADFilePath);
                    if (doc == null) continue;
                    double x0 = double.MaxValue, y0 = double.MaxValue,
                           x1 = double.MinValue, y1 = double.MinValue;
                    bool g = false;
                    foreach (var l in doc.Entities.Lines)
                    {
                        x0 = Math.Min(x0, Math.Min(l.StartPoint.X, l.EndPoint.X));
                        y0 = Math.Min(y0, Math.Min(l.StartPoint.Y, l.EndPoint.Y));
                        x1 = Math.Max(x1, Math.Max(l.StartPoint.X, l.EndPoint.X));
                        y1 = Math.Max(y1, Math.Max(l.StartPoint.Y, l.EndPoint.Y));
                        g = true;
                    }
                    foreach (var p in doc.Entities.Polylines2D)
                        foreach (var v in p.Vertexes)
                        {
                            x0 = Math.Min(x0, v.Position.X); y0 = Math.Min(y0, v.Position.Y);
                            x1 = Math.Max(x1, v.Position.X); y1 = Math.Max(y1, v.Position.Y);
                            g = true;
                        }
                    if (g) c[f.CADFilePath] = ((x0 + x1) / 2, (y0 + y1) / 2, f.Name);
                }
                catch { }
            }
            if (c.Count < 2) return true;
            var list = c.Values.ToList();
            var r = list[0];
            var bad = new List<string>();
            foreach (var e in list.Skip(1))
            {
                double d = Math.Sqrt(Math.Pow(e.cx - r.cx, 2) + Math.Pow(e.cy - r.cy, 2));
                if (d > TOL) bad.Add($"  • {e.n}: {d:F0}mm");
            }
            if (bad.Count == 0) return true;
            var sb = new StringBuilder("⚠️ CAD MISALIGNMENT\n");
            bad.ForEach(b => sb.AppendLine(b));
            sb.AppendLine("\nFix: AutoCAD → MOVE → select all → ref point → 0,0\n\nContinue anyway?");
            return MessageBox.Show(sb.ToString(), "Alignment Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> c)
        {
            int n = 0;
            foreach (var f in c)
                if (f.Name == "Typical" || f.Name == "Refuge") n += f.Count;
            return n;
        }

        private void ShowDesignNotes(List<FloorTypeConfig> fc, int typ, string zone,
            GradeScheduleManager g, double fh)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Zone: {zone}");
            if (fh > 0)
            {
                sb.AppendLine($"\nFOUNDATION + BASEMENT:");
                sb.AppendLine($"  Foundation walls : Z=0 → {fh:F2}m");
                sb.AppendLine($"  Basement1 slab   : Z={fh:F2}m  (Plan View Z={fh:F2}m ✓)");
            }
            sb.AppendLine("\nFLOORS:");
            foreach (var c in fc)
                sb.AppendLine(c.IsIndividualBasement
                    ? $"  Basement{c.BasementNumber}: wall height={c.Height:F2}m  slab at Z={fh:F2}m"
                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");
            sb.AppendLine("\nGRADES:");
            foreach (var r in g.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.WallGrade}/{r.BeamSlabGrade}");
            if (MessageBox.Show(sb.ToString(), "Confirm Import",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                throw new Exception("Import cancelled");
        }

        private void ShowImportSummary(List<FloorTypeConfig> configs, int total,
            double height, int typ, string zone, GradeScheduleManager g, double fh)
        {
            var sb = new StringBuilder();
            sb.AppendLine("✅ IMPORT SUCCESSFUL");
            sb.AppendLine($"Stories       : {total}");
            sb.AppendLine($"Building top  : {height:F2}m");
            if (fh > 0)
            {
                sb.AppendLine($"Foundation    : Z=0 → {fh:F2}m");
                sb.AppendLine($"Basement slab : Z={fh:F2}m  (Plan View Z={fh:F2}m ✓)");
            }
            sb.AppendLine($"Zone          : {zone}");
            sb.AppendLine("\nFLOORS:");
            foreach (var c in configs)
                sb.AppendLine(c.IsIndividualBasement
                    ? $"  Basement{c.BasementNumber}: wall={c.Height:F2}m  slab@{fh:F2}m"
                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");
            sb.AppendLine("\nGRADES:");
            foreach (var r in g.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1}-{r.EndFloor + 1}: {r.WallGrade}/{r.BeamSlabGrade}");
            Debug.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
// ============================================================================
// END OF FILE
// ============================================================================
