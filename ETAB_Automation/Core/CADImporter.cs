




//// ============================================================================
//// FILE: Core/CADImporter.cs — VERSION 5.2
////
//// COLUMN PLACEMENT — v5.1 (definitive fix):
////   globalColumnDxf resolved once before loop from first config with columns.
////   Per-floor loop places one column segment per story (geomBase → geomTop).
////
////   FOUNDATION SPLIT FIX:
////   For the deepest basement (needsFoundationSplit=true), geomTop includes
////   foundationHeight + rawWallHeight. A single column spanning 0→geomTop
////   crosses the ETABS story boundary causing wrong-story assignment.
////   Two segments are placed to match the wall split:
////     Segment A: Z = 0              → foundationHeight  (foundation zone)
////     Segment B: Z = foundationHeight → geomTop          (basement zone)
////   All other stories: one segment geomBase → geomTop as normal.
//// ============================================================================
////
//// COLUMN PLACEMENT — v4.9:
////   Columns placed per-floor (one segment per story) inside the main loop.
////   Each segment: geomBase → geomTop, assigned to exact ETABS storyName.
////   ColumnImporter bug fixed: storyName (not storyName+"_COL") passed to
////   AddByCoord so columns appear under the correct story in ETABS hierarchy.
//// ============================================================================

//using ETAB_Automation.Models;
//using ETABS_CAD_Automation.Importers;
//using ETABS_CAD_Automation.Models;
//using ETABSv1;
//using netDxf;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

//namespace ETAB_Automation.Core
//{
//    public class CADImporterEnhanced
//    {
//        private cSapModel sapModel;
//        private StoryManager storyManager;

//        public CADImporterEnhanced(cSapModel model)
//        {
//            sapModel = model;
//            storyManager = new StoryManager(model);
//        }

//        public bool ImportMultiFloorTypeCAD(
//            List<FloorTypeConfig> floorConfigs,
//            List<double> storyHeights,
//            List<double> rawHeights,
//            List<string> storyNames,
//            string seismicZone,
//            List<string> wallGrades,
//            List<int> floorsPerGrade,
//            double foundationHeight = 0.0)
//        {
//            try
//            {
//                Debug.WriteLine("\n╔═════════════════════════════════════╗");
//                Debug.WriteLine("║  CAD IMPORTER v4.7                 ║");
//                Debug.WriteLine($"║  foundationHeight = {foundationHeight:F3}m           ║");
//                Debug.WriteLine("╚═════════════════════════════════════╝\n");

//                sapModel.SetModelIsLocked(false);
//                sapModel.SetPresentUnits(eUnits.N_m_C);

//                var gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);
//                int totalStories = storyHeights.Count;

//                if (!gradeSchedule.ValidateTotalFloors(totalStories))
//                {
//                    MessageBox.Show(
//                        $"Grade schedule count mismatch!\nExpected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
//                        "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    return false;
//                }

//                int totalTypical = CalculateTotalTypicalFloors(floorConfigs);
//                int wallRef = totalTypical > 0 ? totalTypical : totalStories;

//                ShowDesignNotes(floorConfigs, totalTypical, seismicZone, gradeSchedule, foundationHeight);

//                Debug.WriteLine("📐 Creating stories (StoryManager v6.2)...");
//                storyManager.DefineStoriesWithCustomNames(storyHeights, rawHeights, storyNames, foundationHeight);

//                Debug.WriteLine("🔍 Validating CAD alignment...");
//                if (!ValidateCADCoordinates(floorConfigs)) return false;

//                WallThicknessCalculator.LoadAvailableWallSections(sapModel);
//                SectionDefiner.ResetCache();
//                ColumnImporter.ClearSectionCache();

//                // ── Resolve column CAD once — used for EVERY story ────────────
//                // Columns share the same plan geometry across all floor types.
//                // Find the first floor config that has Column layers mapped and
//                // use its CAD file for all stories (basement through terrace).
//                List<string> globalColumnLayers = null;
//                DxfDocument globalColumnDxf = null;

//                foreach (var fc in floorConfigs)
//                {
//                    var layers = fc.LayerMapping
//                        .Where(kv => kv.Value.Equals("Column", StringComparison.OrdinalIgnoreCase))
//                        .Select(kv => kv.Key)
//                        .ToList();
//                    if (layers.Count > 0)
//                    {
//                        globalColumnLayers = layers;
//                        globalColumnDxf = DxfDocument.Load(fc.CADFilePath);
//                        Debug.WriteLine($"✓ Column CAD resolved: {System.IO.Path.GetFileName(fc.CADFilePath)}" +
//                            $"  layers=[{string.Join(", ", layers)}]");
//                        break;
//                    }
//                }

//                if (globalColumnDxf == null)
//                    Debug.WriteLine("⚠ No Column layers found in any floor config — columns will be skipped.");

//                int currentStoryIndex = 0;

//                foreach (var floorConfig in floorConfigs)
//                {
//                    Debug.WriteLine($"\n┌─ FLOOR TYPE: {floorConfig.Name.ToUpper()}");

//                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
//                    if (dxfDoc == null)
//                    {
//                        MessageBox.Show(
//                            $"Failed to load CAD: {floorConfig.CADFilePath}",
//                            "CAD Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                        return false;
//                    }
//                    Debug.WriteLine($"│  ✓ {System.IO.Path.GetFileName(floorConfig.CADFilePath)}");

//                    var beamImporter = new BeamImporterEnhanced(
//                        sapModel, dxfDoc, seismicZone, wallRef,
//                        floorConfig.BeamDepths, gradeSchedule, floorConfig.BeamWidthOverrides,
//                        floorConfig.BeamWallLoadSets, floorConfig.BeamWallLoadMagnitudes);

//                    var slabImporter = new SlabImporterEnhanced(
//                        sapModel, dxfDoc, floorConfig.SlabThicknesses, gradeSchedule,
//                        floorConfig.SlabIndividualLoads,
//                        floorConfig.SlabAreaRules,
//                        floorConfig.SlabCantileverRules);

//                    for (int floor = 0; floor < floorConfig.Count; floor++)
//                    {
//                        if (currentStoryIndex >= totalStories)
//                        {
//                            MessageBox.Show(
//                                $"Story index {currentStoryIndex} out of bounds (total={totalStories}).",
//                                "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                            return false;
//                        }

//                        double geomBase = storyManager.GetStoryBaseElevation(currentStoryIndex);
//                        double geomTop = storyManager.GetStoryTopElevation(currentStoryIndex);
//                        double slabZ = storyManager.GetStoryPlanViewZ(currentStoryIndex);
//                        double wallHt = geomTop - geomBase;
//                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
//                        string storyName = storyManager.GetStoryNameByIndex(currentStoryIndex);
//                        bool isBasement = floorConfig.IsIndividualBasement;
//                        bool isTerrace = storyName.Equals("Terrace", StringComparison.OrdinalIgnoreCase);

//                        Debug.WriteLine($"│");
//                        Debug.WriteLine($"│  [{floor + 1}/{floorConfig.Count}] {storyName}  (idx={currentStoryIndex})");
//                        Debug.WriteLine($"│    GeomBase={geomBase:F3}  GeomTop={geomTop:F3}  WallHt={wallHt:F3}");
//                        Debug.WriteLine($"│    Grade={wallGrade}  NTA={floorConfig.NtaWallThickness}mm");

//                        // Foundation-zone two-part wall treatment applies ONLY to:
//                        //   (a) The DEEPEST basement — currentStoryIndex==0 AND isBasement
//                        //       (geomBase=0, geomTop=foundationHeight+wallHeight, StoryManager v6.5)
//                        //   (b) The first story when NO basements exist at all and foundationHeight>0
//                        //
//                        // Shallower basements (idx>0) are stacked normally by StoryManager v6.5
//                        // and get normal wall treatment — no foundation split.
//                        // FIX: Match StoryManager v6.5 logic exactly — no !anyBasementInBuild guard.
//                        // StoryManager assigns geomBase=0 to idx=0 if !isBasement && foundationHeight>0,
//                        // regardless of whether other basements exist. CADImporter must agree.
//                        bool isDeepestBasement = (currentStoryIndex == 0 && isBasement && foundationHeight > 0);
//                        bool isFirstStoryNoBasement = (currentStoryIndex == 0 && !isBasement && foundationHeight > 0);
//                        bool needsFoundationSplit = isDeepestBasement || isFirstStoryNoBasement;

//                        if (needsFoundationSplit)
//                        {
//                            // ── DEEPEST BASEMENT / FIRST STORY WITH FOUNDATION ZONE ──
//                            //
//                            // geomBase = 0.0,  geomTop = foundationHeight + userWallHeight
//                            // Step A — Foundation walls : Z=0        → foundationHeight
//                            // Step B — Basement walls   : Z=fdn      → geomTop
//                            // Slab/Beams                : Z=slabZ    (= Plan View Z = foundationHeight)
//                            // Columns                   : Z=geomBase → geomTop  (full height, from 0.0)

//                            double basementWallHeight = wallHt - foundationHeight;

//                            // ── STEP A: Foundation walls ──────────────────────
//                            Debug.WriteLine($"│    [A] Foundation walls  Z={geomBase:F3} → {foundationHeight:F3}m");

//                            var foundWallImporter = new WallImporterEnhanced(
//                                sapModel, dxfDoc,
//                                foundationHeight,
//                                wallRef, seismicZone, gradeSchedule,
//                                floorConfig.NtaWallThickness,
//                                floorConfig.WallThicknessOverrides);

//                            foundWallImporter.ImportWalls(
//                                floorConfig.LayerMapping,
//                                geomBase,
//                                currentStoryIndex);

//                            Debug.WriteLine($"│    ✓ Foundation walls: {geomBase:F3} → {foundationHeight:F3}m");

//                            // ── STEP B: Basement walls ────────────────────────
//                            Debug.WriteLine($"│    [B] Basement walls    Z={foundationHeight:F3} → {geomTop:F3}m  (height={basementWallHeight:F3}m)");

//                            var basementWallImporter = new WallImporterEnhanced(
//                                sapModel, dxfDoc,
//                                basementWallHeight,
//                                wallRef, seismicZone, gradeSchedule,
//                                floorConfig.NtaWallThickness,
//                                floorConfig.WallThicknessOverrides);

//                            basementWallImporter.ImportWalls(
//                                floorConfig.LayerMapping,
//                                foundationHeight,
//                                currentStoryIndex);

//                            Debug.WriteLine($"│    ✓ Basement walls: {foundationHeight:F3} → {geomTop:F3}m");

//                            // ── Slab / Beams ──────────────────────────────────
//                            double slabElev = slabZ;
//                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m  (Plan View Z={slabZ:F3}m)");
//                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
//                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
//                        }
//                        else
//                        {
//                            // ── NORMAL STORY ──────────────────────────────────
//                            double slabElev = slabZ;

//                            Debug.WriteLine($"│    Walls  Z={geomBase:F3} → {geomTop:F3}m");
//                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m");

//                            if (!isTerrace)
//                            {
//                                var wallImporter = new WallImporterEnhanced(
//                                    sapModel, dxfDoc,
//                                    wallHt,
//                                    wallRef, seismicZone, gradeSchedule,
//                                    floorConfig.NtaWallThickness,
//                                    floorConfig.WallThicknessOverrides);

//                                wallImporter.ImportWalls(
//                                    floorConfig.LayerMapping, geomBase, currentStoryIndex);
//                            }
//                            else
//                            {
//                                Debug.WriteLine("│    ⛔ Terrace — walls skipped");
//                            }

//                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
//                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
//                        }

//                        // ── COLUMNS — one segment per story using global column CAD ──
//                        // Column base = previous story's Plan View Z (or 0 for first story).
//                        // Column top  = this story's Plan View Z (= ETABS cumulative height).
//                        // This ensures columns stay within ETABS story boundaries exactly.
//                        //
//                        // For foundation-split basement: two segments matching wall split:
//                        //   Segment A: Z = 0              → foundationHeight
//                        //   Segment B: Z = foundationHeight → slabZ (Plan View Z)
//                        if (globalColumnDxf != null && globalColumnLayers != null && globalColumnLayers.Count > 0)
//                        {
//                            // Building top = ETABS cumulative of last story (Plan View Z of Terrace).
//                            // Hard-cap colTop here so columns NEVER exceed the ETABS model boundary.
//                            // Without this cap: when foundationHeight>0 the shift orphans Terrace's
//                            // raw height (e.g. 3m) causing colTop = 42.5+3.0 = 45.5m instead of 42.5m.
//                            double buildingTop = storyManager.GetTotalBuildingHeight();

//                            // Column top = Plan View Z of this story, capped at building top
//                            double colTop = Math.Min(slabZ, buildingTop);
//                            // Column base = Plan View Z of previous story (0 for first story)
//                            double colBase = (currentStoryIndex == 0)
//                                ? 0.0
//                                : storyManager.GetStoryPlanViewZ(currentStoryIndex - 1);
//                            double colHt = colTop - colBase;

//                            if (needsFoundationSplit)
//                            {
//                                // Segment A — foundation zone: 0 → foundationHeight
//                                double htA = foundationHeight - colBase;  // colBase=0 for idx=0
//                                Debug.WriteLine($"│    Columns [A] Z={colBase:F3} → {foundationHeight:F3}m  [{storyName}]");
//                                var colA = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
//                                colA.ImportColumns(globalColumnLayers, colBase, htA, storyName);
//                                Debug.WriteLine($"│    ✅ Col-A placed={colA.ColumnsCreated} failed={colA.ColumnsFailed}");

//                                // Segment B — basement zone: foundationHeight → slabZ
//                                double htB = colTop - foundationHeight;
//                                Debug.WriteLine($"│    Columns [B] Z={foundationHeight:F3} → {colTop:F3}m  [{storyName}]");
//                                var colB = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
//                                colB.ImportColumns(globalColumnLayers, foundationHeight, htB, storyName);
//                                Debug.WriteLine($"│    ✅ Col-B placed={colB.ColumnsCreated} failed={colB.ColumnsFailed}");
//                            }
//                            else
//                            {
//                                Debug.WriteLine($"│    Columns Z={colBase:F3} → {colTop:F3}m  [{storyName}]");
//                                var colImporter = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
//                                colImporter.ImportColumns(globalColumnLayers, colBase, colHt, storyName);
//                                Debug.WriteLine($"│    ✅ Columns placed={colImporter.ColumnsCreated} failed={colImporter.ColumnsFailed}");
//                            }
//                        }

//                        currentStoryIndex++;
//                        if ((floor + 1) % 5 == 0) sapModel.View.RefreshView(0, false);
//                    }

//                    Debug.WriteLine($"└─ {floorConfig.Name} complete");
//                    sapModel.View.RefreshView(0, false);
//                }

//                sapModel.View.RefreshView(0, true);
//                ShowImportSummary(floorConfigs, totalStories,
//                    storyManager.GetTotalBuildingHeight(),
//                    totalTypical, seismicZone, gradeSchedule, foundationHeight);
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"\n❌ {ex.Message}\n{ex.StackTrace}");
//                MessageBox.Show($"Import failed:\n\n{ex.Message}", "Import Error",
//                    MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return false;
//            }
//        }

//        // ====================================================================
//        // HELPERS
//        // ====================================================================

//        private bool ValidateCADCoordinates(List<FloorTypeConfig> floorConfigs)
//        {
//            const double TOL = 500.0;
//            var c = new Dictionary<string, (double cx, double cy, string n)>(StringComparer.OrdinalIgnoreCase);
//            foreach (var f in floorConfigs)
//            {
//                if (string.IsNullOrEmpty(f.CADFilePath) || c.ContainsKey(f.CADFilePath)) continue;
//                try
//                {
//                    var doc = DxfDocument.Load(f.CADFilePath);
//                    if (doc == null) continue;
//                    double x0 = double.MaxValue, y0 = double.MaxValue,
//                           x1 = double.MinValue, y1 = double.MinValue;
//                    bool g = false;
//                    foreach (var l in doc.Entities.Lines)
//                    {
//                        x0 = Math.Min(x0, Math.Min(l.StartPoint.X, l.EndPoint.X));
//                        y0 = Math.Min(y0, Math.Min(l.StartPoint.Y, l.EndPoint.Y));
//                        x1 = Math.Max(x1, Math.Max(l.StartPoint.X, l.EndPoint.X));
//                        y1 = Math.Max(y1, Math.Max(l.StartPoint.Y, l.EndPoint.Y));
//                        g = true;
//                    }
//                    foreach (var p in doc.Entities.Polylines2D)
//                        foreach (var v in p.Vertexes)
//                        {
//                            x0 = Math.Min(x0, v.Position.X); y0 = Math.Min(y0, v.Position.Y);
//                            x1 = Math.Max(x1, v.Position.X); y1 = Math.Max(y1, v.Position.Y);
//                            g = true;
//                        }
//                    if (g) c[f.CADFilePath] = ((x0 + x1) / 2, (y0 + y1) / 2, f.Name);
//                }
//                catch { }
//            }
//            if (c.Count < 2) return true;
//            var list = c.Values.ToList();
//            var r = list[0];
//            var bad = new List<string>();
//            foreach (var e in list.Skip(1))
//            {
//                double d = Math.Sqrt(Math.Pow(e.cx - r.cx, 2) + Math.Pow(e.cy - r.cy, 2));
//                if (d > TOL) bad.Add($"  • {e.n}: {d:F0}mm");
//            }
//            if (bad.Count == 0) return true;
//            var sb = new StringBuilder("⚠️ CAD MISALIGNMENT\n");
//            bad.ForEach(b => sb.AppendLine(b));
//            sb.AppendLine("\nFix: AutoCAD → MOVE → select all → ref point → 0,0\n\nContinue anyway?");
//            return MessageBox.Show(sb.ToString(), "Alignment Warning",
//                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
//                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
//        }

//        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> c)
//        {
//            int n = 0;
//            foreach (var f in c)
//                if (f.Name == "Typical" || f.Name == "Refuge") n += f.Count;
//            return n;
//        }

//        private void ShowDesignNotes(List<FloorTypeConfig> fc, int typ, string zone,
//            GradeScheduleManager g, double fh)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine($"Zone: {zone}");
//            if (fh > 0)
//            {
//                sb.AppendLine($"\nFOUNDATION + BASEMENT:");
//                sb.AppendLine($"  Foundation walls : Z=0 → {fh:F2}m");
//                sb.AppendLine($"  Basement1 slab   : Z={fh:F2}m  (Plan View Z={fh:F2}m ✓)");
//                sb.AppendLine($"  Columns          : Z=0 → geomTop  (always from base ✓)");
//            }
//            sb.AppendLine("\nFLOORS:");
//            foreach (var c in fc)
//                sb.AppendLine(c.IsIndividualBasement
//                    ? $"  Basement{c.BasementNumber}: wall height={c.Height:F2}m  slab at Z={fh:F2}m"
//                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");

//            sb.AppendLine("\nWALL GRADES (1 floor shifted — ETABS wall convention):");
//            foreach (var r in g.GetWallGradeRanges())
//                sb.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.WallGrade}");

//            sb.AppendLine("\nBEAM/SLAB GRADES:");
//            foreach (var r in g.GetGradeRanges())
//                sb.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.BeamSlabGrade}");

//            if (MessageBox.Show(sb.ToString(), "Confirm Import",
//                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
//                throw new Exception("Import cancelled");
//        }

//        private void ShowImportSummary(List<FloorTypeConfig> configs, int total,
//            double height, int typ, string zone, GradeScheduleManager g, double fh)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("✅ IMPORT SUCCESSFUL");
//            sb.AppendLine($"Stories       : {total}");
//            sb.AppendLine($"Building top  : {height:F2}m");
//            if (fh > 0)
//            {
//                sb.AppendLine($"Foundation    : Z=0 → {fh:F2}m");
//                sb.AppendLine($"Basement slab : Z={fh:F2}m  (Plan View Z={fh:F2}m ✓)");
//                sb.AppendLine($"Columns       : Z=0 → geomTop  (always from base ✓)");
//            }
//            sb.AppendLine($"Zone          : {zone}");
//            sb.AppendLine("\nFLOORS:");
//            foreach (var c in configs)
//                sb.AppendLine(c.IsIndividualBasement
//                    ? $"  Basement{c.BasementNumber}: wall={c.Height:F2}m  slab@{fh:F2}m"
//                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");

//            sb.AppendLine("\nWALL GRADES (1 floor shifted — ETABS wall convention):");
//            foreach (var r in g.GetWallGradeRanges())
//                sb.AppendLine($"  F{r.StartFloor + 1}-{r.EndFloor + 1}: {r.WallGrade}");

//            sb.AppendLine("\nBEAM/SLAB GRADES:");
//            foreach (var r in g.GetGradeRanges())
//                sb.AppendLine($"  F{r.StartFloor + 1}-{r.EndFloor + 1}: {r.BeamSlabGrade}");

//            Debug.WriteLine(sb.ToString());
//            MessageBox.Show(sb.ToString(), "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
//        }
//    }
//}
//// ============================================================================
//// END OF FILE
//// ============================================================================






// ============================================================================
// FILE: Core/CADImporter.cs — VERSION 5.2
//
// COLUMN PLACEMENT — v5.1 (definitive fix):
//   globalColumnDxf resolved once before loop from first config with columns.
//   Per-floor loop places one column segment per story (geomBase → geomTop).
//
//   FOUNDATION SPLIT FIX:
//   For the deepest basement (needsFoundationSplit=true), geomTop includes
//   foundationHeight + rawWallHeight. A single column spanning 0→geomTop
//   crosses the ETABS story boundary causing wrong-story assignment.
//   Two segments are placed to match the wall split:
//     Segment A: Z = 0              → foundationHeight  (foundation zone)
//     Segment B: Z = foundationHeight → geomTop          (basement zone)
//   All other stories: one segment geomBase → geomTop as normal.
// ============================================================================
//
// COLUMN PLACEMENT — v4.9:
//   Columns placed per-floor (one segment per story) inside the main loop.
//   Each segment: geomBase → geomTop, assigned to exact ETABS storyName.
//   ColumnImporter bug fixed: storyName (not storyName+"_COL") passed to
//   AddByCoord so columns appear under the correct story in ETABS hierarchy.
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
                Debug.WriteLine("║  CAD IMPORTER v4.7                 ║");
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

                Debug.WriteLine("📐 Creating stories (StoryManager v6.2)...");
                storyManager.DefineStoriesWithCustomNames(storyHeights, rawHeights, storyNames, foundationHeight);

                Debug.WriteLine("🔍 Validating CAD alignment...");
                if (!ValidateCADCoordinates(floorConfigs)) return false;

                WallThicknessCalculator.LoadAvailableWallSections(sapModel);
                SectionDefiner.ResetCache();
                ColumnImporter.ClearSectionCache();

                // ── Resolve column CAD once — used for EVERY story ────────────
                // Columns share the same plan geometry across all floor types.
                // Find the first floor config that has Column layers mapped and
                // use its CAD file for all stories (basement through terrace).
                List<string> globalColumnLayers = null;
                DxfDocument globalColumnDxf = null;

                foreach (var fc in floorConfigs)
                {
                    var layers = fc.LayerMapping
                        .Where(kv => kv.Value.Equals("Column", StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Key)
                        .ToList();
                    if (layers.Count > 0)
                    {
                        globalColumnLayers = layers;
                        globalColumnDxf = DxfDocument.Load(fc.CADFilePath);
                        Debug.WriteLine($"✓ Column CAD resolved: {System.IO.Path.GetFileName(fc.CADFilePath)}" +
                            $"  layers=[{string.Join(", ", layers)}]");
                        break;
                    }
                }

                if (globalColumnDxf == null)
                    Debug.WriteLine("⚠ No Column layers found in any floor config — columns will be skipped.");

                int currentStoryIndex = 0;

                // ── ONE shared wall importer for ALL stories ──────────────────
                // Previously a new importer was created per story, so _allCreatedWalls
                // was always discarded empty. Now one instance accumulates every wall
                // across all floor types; AssignAllPiers() is called once at the end.
                var firstDxf = DxfDocument.Load(floorConfigs[0].CADFilePath);
                var sharedWallImporter = new WallImporterEnhanced(
                    sapModel, firstDxf,
                    floorConfigs[0].Height,
                    wallRef, seismicZone, gradeSchedule,
                    floorConfigs[0].NtaWallThickness,
                    floorConfigs[0].WallThicknessOverrides);
                sharedWallImporter.ResetPiers();

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
                        floorConfig.BeamDepths, gradeSchedule, floorConfig.BeamWidthOverrides,
                        floorConfig.BeamWallLoadSets, floorConfig.BeamWallLoadMagnitudes);

                    var slabImporter = new SlabImporterEnhanced(
                        sapModel, dxfDoc, floorConfig.SlabThicknesses, gradeSchedule,
                        floorConfig.SlabIndividualLoads,
                        floorConfig.SlabAreaRules,
                        floorConfig.SlabCantileverRules);

                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        if (currentStoryIndex >= totalStories)
                        {
                            MessageBox.Show(
                                $"Story index {currentStoryIndex} out of bounds (total={totalStories}).",
                                "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        double geomBase = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double geomTop = storyManager.GetStoryTopElevation(currentStoryIndex);
                        double slabZ = storyManager.GetStoryPlanViewZ(currentStoryIndex);
                        double wallHt = geomTop - geomBase;
                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
                        string storyName = storyManager.GetStoryNameByIndex(currentStoryIndex);
                        bool isBasement = floorConfig.IsIndividualBasement;
                        bool isTerrace = storyName.Equals("Terrace", StringComparison.OrdinalIgnoreCase);

                        Debug.WriteLine($"│");
                        Debug.WriteLine($"│  [{floor + 1}/{floorConfig.Count}] {storyName}  (idx={currentStoryIndex})");
                        Debug.WriteLine($"│    GeomBase={geomBase:F3}  GeomTop={geomTop:F3}  WallHt={wallHt:F3}");
                        Debug.WriteLine($"│    Grade={wallGrade}  NTA={floorConfig.NtaWallThickness}mm");

                        // Foundation-zone two-part wall treatment applies ONLY to:
                        //   (a) The DEEPEST basement — currentStoryIndex==0 AND isBasement
                        //       (geomBase=0, geomTop=foundationHeight+wallHeight, StoryManager v6.5)
                        //   (b) The first story when NO basements exist at all and foundationHeight>0
                        //
                        // Shallower basements (idx>0) are stacked normally by StoryManager v6.5
                        // and get normal wall treatment — no foundation split.
                        // FIX: Match StoryManager v6.5 logic exactly — no !anyBasementInBuild guard.
                        // StoryManager assigns geomBase=0 to idx=0 if !isBasement && foundationHeight>0,
                        // regardless of whether other basements exist. CADImporter must agree.
                        bool isDeepestBasement = (currentStoryIndex == 0 && isBasement && foundationHeight > 0);
                        bool isFirstStoryNoBasement = (currentStoryIndex == 0 && !isBasement && foundationHeight > 0);
                        bool needsFoundationSplit = isDeepestBasement || isFirstStoryNoBasement;

                        if (needsFoundationSplit)
                        {
                            // ── DEEPEST BASEMENT / FIRST STORY WITH FOUNDATION ZONE ──
                            //
                            // geomBase = 0.0,  geomTop = foundationHeight + userWallHeight
                            // Step A — Foundation walls : Z=0        → foundationHeight
                            // Step B — Basement walls   : Z=fdn      → geomTop
                            // Slab/Beams                : Z=slabZ    (= Plan View Z = foundationHeight)
                            // Columns                   : Z=geomBase → geomTop  (full height, from 0.0)

                            double basementWallHeight = wallHt - foundationHeight;

                            // ── STEP A: Foundation walls ──────────────────────
                            Debug.WriteLine($"│    [A] Foundation walls  Z={geomBase:F3} → {foundationHeight:F3}m");

                            sharedWallImporter.UpdateDxfAndHeight(dxfDoc, foundationHeight,
                                floorConfig.NtaWallThickness, floorConfig.WallThicknessOverrides);
                            sharedWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                geomBase,
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Foundation walls: {geomBase:F3} → {foundationHeight:F3}m");

                            // ── STEP B: Basement walls ────────────────────────
                            Debug.WriteLine($"│    [B] Basement walls    Z={foundationHeight:F3} → {geomTop:F3}m  (height={basementWallHeight:F3}m)");

                            sharedWallImporter.UpdateDxfAndHeight(dxfDoc, basementWallHeight,
                                floorConfig.NtaWallThickness, floorConfig.WallThicknessOverrides);
                            sharedWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                foundationHeight,
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Basement walls: {foundationHeight:F3} → {geomTop:F3}m");

                            // ── Slab / Beams ──────────────────────────────────
                            double slabElev = slabZ;
                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m  (Plan View Z={slabZ:F3}m)");
                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                        }
                        else
                        {
                            // ── NORMAL STORY ──────────────────────────────────
                            double slabElev = slabZ;

                            Debug.WriteLine($"│    Walls  Z={geomBase:F3} → {geomTop:F3}m");
                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m");

                            if (!isTerrace)
                            {
                                sharedWallImporter.UpdateDxfAndHeight(dxfDoc, wallHt,
                                    floorConfig.NtaWallThickness, floorConfig.WallThicknessOverrides);
                                sharedWallImporter.ImportWalls(
                                    floorConfig.LayerMapping, geomBase, currentStoryIndex);
                            }
                            else
                            {
                                Debug.WriteLine("│    ⛔ Terrace — walls skipped");
                            }

                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                        }

                        // ── COLUMNS — one segment per story using global column CAD ──
                        // Column base = previous story's Plan View Z (or 0 for first story).
                        // Column top  = this story's Plan View Z (= ETABS cumulative height).
                        // This ensures columns stay within ETABS story boundaries exactly.
                        //
                        // For foundation-split basement: two segments matching wall split:
                        //   Segment A: Z = 0              → foundationHeight
                        //   Segment B: Z = foundationHeight → slabZ (Plan View Z)
                        if (globalColumnDxf != null && globalColumnLayers != null && globalColumnLayers.Count > 0)
                        {
                            // Building top = ETABS cumulative of last story (Plan View Z of Terrace).
                            // Hard-cap colTop here so columns NEVER exceed the ETABS model boundary.
                            // Without this cap: when foundationHeight>0 the shift orphans Terrace's
                            // raw height (e.g. 3m) causing colTop = 42.5+3.0 = 45.5m instead of 42.5m.
                            double buildingTop = storyManager.GetTotalBuildingHeight();

                            // Column top = Plan View Z of this story, capped at building top
                            double colTop = Math.Min(slabZ, buildingTop);
                            // Column base = Plan View Z of previous story (0 for first story)
                            double colBase = (currentStoryIndex == 0)
                                ? 0.0
                                : storyManager.GetStoryPlanViewZ(currentStoryIndex - 1);
                            double colHt = colTop - colBase;

                            if (needsFoundationSplit)
                            {
                                // Segment A — foundation zone: 0 → foundationHeight
                                double htA = foundationHeight - colBase;  // colBase=0 for idx=0
                                Debug.WriteLine($"│    Columns [A] Z={colBase:F3} → {foundationHeight:F3}m  [{storyName}]");
                                var colA = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
                                colA.ImportColumns(globalColumnLayers, colBase, htA, storyName);
                                Debug.WriteLine($"│    ✅ Col-A placed={colA.ColumnsCreated} failed={colA.ColumnsFailed}");

                                // Segment B — basement zone: foundationHeight → slabZ
                                double htB = colTop - foundationHeight;
                                Debug.WriteLine($"│    Columns [B] Z={foundationHeight:F3} → {colTop:F3}m  [{storyName}]");
                                var colB = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
                                colB.ImportColumns(globalColumnLayers, foundationHeight, htB, storyName);
                                Debug.WriteLine($"│    ✅ Col-B placed={colB.ColumnsCreated} failed={colB.ColumnsFailed}");
                            }
                            else
                            {
                                Debug.WriteLine($"│    Columns Z={colBase:F3} → {colTop:F3}m  [{storyName}]");
                                var colImporter = new ColumnImporter(sapModel, globalColumnDxf, gradeSchedule, currentStoryIndex);
                                colImporter.ImportColumns(globalColumnLayers, colBase, colHt, storyName);
                                Debug.WriteLine($"│    ✅ Columns placed={colImporter.ColumnsCreated} failed={colImporter.ColumnsFailed}");
                            }
                        }

                        currentStoryIndex++;
                        if ((floor + 1) % 5 == 0) sapModel.View.RefreshView(0, false);
                    }

                    Debug.WriteLine($"└─ {floorConfig.Name} complete");
                    sapModel.View.RefreshView(0, false);
                }

                // ── PASS 2: assign pier labels to all created walls ──────────
                Debug.WriteLine("\n📌 Assigning pier labels...");
                sharedWallImporter.AssignAllPiers();

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
                sb.AppendLine($"  Columns          : Z=0 → geomTop  (always from base ✓)");
            }
            sb.AppendLine("\nFLOORS:");
            foreach (var c in fc)
                sb.AppendLine(c.IsIndividualBasement
                    ? $"  Basement{c.BasementNumber}: wall height={c.Height:F2}m  slab at Z={fh:F2}m"
                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");

            sb.AppendLine("\nWALL GRADES (1 floor shifted — ETABS wall convention):");
            foreach (var r in g.GetWallGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.WallGrade}");

            sb.AppendLine("\nBEAM/SLAB GRADES:");
            foreach (var r in g.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.BeamSlabGrade}");

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
                sb.AppendLine($"Columns       : Z=0 → geomTop  (always from base ✓)");
            }
            sb.AppendLine($"Zone          : {zone}");
            sb.AppendLine("\nFLOORS:");
            foreach (var c in configs)
                sb.AppendLine(c.IsIndividualBasement
                    ? $"  Basement{c.BasementNumber}: wall={c.Height:F2}m  slab@{fh:F2}m"
                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");

            sb.AppendLine("\nWALL GRADES (1 floor shifted — ETABS wall convention):");
            foreach (var r in g.GetWallGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1}-{r.EndFloor + 1}: {r.WallGrade}");

            sb.AppendLine("\nBEAM/SLAB GRADES:");
            foreach (var r in g.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1}-{r.EndFloor + 1}: {r.BeamSlabGrade}");

            Debug.WriteLine(sb.ToString());
            MessageBox.Show(sb.ToString(), "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
// ============================================================================
// END OF FILE
// ============================================================================

