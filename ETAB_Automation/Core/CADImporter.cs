


//// ============================================================================
//// FILE: Core/CADImporterEnhanced.cs — VERSION 4.2
////
//// ELEVATION MODEL (definitive):
////
////   ETABS Base = 0m. Stories stack from 0 using user-supplied heights.
////
////   Every story (including Basement1) treated identically:
////     wallPlacementElevation = storyBaseElevation
////     wallHeight             = storyHeight
////     slabBeamElevation      = storyBaseElevation + 0.005
////
////   Example (Basement1 height=1.5, Podium1 height=3.5):
////     Basement1: base=0,   top=1.5  walls: 0→1.5   slab: 0.005m
////     Podium1:   base=1.5, top=5.0  walls: 1.5→5.0 slab: 1.505m
////     Ground:    base=5.0, top=9.0  walls: 5.0→9.0 slab: 5.005m
////
////   foundationHeight parameter accepted for API compatibility but ignored.
////   The caller must include Basement1 in storyHeights with height=1.5.
////
//// CHANGES from v4.1:
////   - All foundationHeight / isBasement special casing removed entirely.
////   - Separate foundation wall import block removed.
////   - Every story uses the same simple formula: base + 0.005 for slab,
////     base elevation for walls, story height for wall height.
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
//            List<string> storyNames,
//            string seismicZone,
//            List<string> wallGrades,
//            List<int> floorsPerGrade,
//            double foundationHeight = 0.0)   // ignored — kept for API compat only
//        {
//            try
//            {
//                Debug.WriteLine("\n╔════════════════════════════════════════════════════╗");
//                Debug.WriteLine("║  CAD IMPORTER v4.2                                ║");
//                Debug.WriteLine("╚════════════════════════════════════════════════════╝\n");

//                sapModel.SetModelIsLocked(false);
//                sapModel.SetPresentUnits(eUnits.N_m_C);

//                var gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);
//                int totalStories = storyHeights.Count;

//                if (!gradeSchedule.ValidateTotalFloors(totalStories))
//                {
//                    MessageBox.Show(
//                        $"❌ Grade schedule floor count mismatch!\nExpected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
//                        "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                    return false;
//                }

//                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);
//                ShowDesignNotes(floorConfigs, totalTypicalFloors, seismicZone, gradeSchedule);

//                // StoryManager v3.2: base=0, all stories stack simply from 0
//                Debug.WriteLine("\n📐 Creating building stories...");
//                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

//                Debug.WriteLine("\n🔍 Validating CAD coordinate alignment...");
//                if (!ValidateCADCoordinates(floorConfigs))
//                    return false;

//                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

//                // ============================================================
//                // IMPORT EACH FLOOR TYPE — identical logic for every story
//                // ============================================================
//                int currentStoryIndex = 0;

//                foreach (var floorConfig in floorConfigs)
//                {
//                    Debug.WriteLine($"\n╔══════════════════════════════════════════╗");
//                    Debug.WriteLine($"║  FLOOR TYPE: {floorConfig.Name.ToUpper().PadRight(29)}║");
//                    Debug.WriteLine($"╚══════════════════════════════════════════╝");

//                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
//                    if (dxfDoc == null)
//                    {
//                        MessageBox.Show(
//                            $"❌ Failed to load CAD file for {floorConfig.Name}\nFile: {floorConfig.CADFilePath}",
//                            "CAD Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                        return false;
//                    }
//                    Debug.WriteLine($"✓ Loaded: {System.IO.Path.GetFileName(floorConfig.CADFilePath)}");

//                    var beamImporter = new BeamImporterEnhanced(
//                        sapModel, dxfDoc, seismicZone,
//                        totalTypicalFloors > 0 ? totalTypicalFloors : totalStories,
//                        floorConfig.BeamDepths, gradeSchedule,
//                        floorConfig.BeamWidthOverrides);

//                    var slabImporter = new SlabImporterEnhanced(
//                        sapModel, dxfDoc,
//                        floorConfig.SlabThicknesses, gradeSchedule);

//                    Debug.WriteLine($"\n📥 Importing {floorConfig.Count} floor(s) of {floorConfig.Name}:");

//                    for (int floor = 0; floor < floorConfig.Count; floor++)
//                    {
//                        if (currentStoryIndex >= totalStories)
//                        {
//                            MessageBox.Show(
//                                $"❌ Story index {currentStoryIndex} out of bounds (total: {totalStories}).\n" +
//                                $"Check that all floorConfig.Count values sum to {totalStories}.",
//                                "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                            return false;
//                        }

//                        double baseElevation = storyManager.GetStoryBaseElevation(currentStoryIndex);
//                        double topElevation = storyManager.GetStoryTopElevation(currentStoryIndex);
//                        double storyHeight = topElevation - baseElevation;
//                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
//                        string beamSlabGrade = gradeSchedule.GetBeamSlabGrade(currentStoryIndex);
//                        string storyName = storyManager.GetStoryNameByIndex(currentStoryIndex);
//                        bool isTerrace = storyName.Equals("Terrace", StringComparison.OrdinalIgnoreCase);

//                        // ── Uniform placement logic for ALL stories ───────────
//                        // Walls: start at story base, height = story height
//                        // Slab/beams: base + 5mm (just inside story boundary)
//                        double wallPlacementElevation = baseElevation;
//                        double slabBeamElevation = baseElevation + 0.005;

//                        Debug.WriteLine($"\n   [{floor + 1}/{floorConfig.Count}] {storyName} (idx {currentStoryIndex})");
//                        Debug.WriteLine($"       Grade     : Wall={wallGrade}, Beam/Slab={beamSlabGrade}");
//                        Debug.WriteLine($"       Base      : {baseElevation:F3}m");
//                        Debug.WriteLine($"       Top       : {topElevation:F3}m");
//                        Debug.WriteLine($"       Height    : {storyHeight:F3}m");
//                        Debug.WriteLine($"       Walls     : {wallPlacementElevation:F3}m → {wallPlacementElevation + storyHeight:F3}m");
//                        Debug.WriteLine($"       Slab/Beam : {slabBeamElevation:F3}m");
//                        Debug.WriteLine($"       NTA wall  : {floorConfig.NtaWallThickness}mm");

//                        var wallImporter = new WallImporterEnhanced(
//                            sapModel, dxfDoc,
//                            storyHeight,
//                            totalTypicalFloors > 0 ? totalTypicalFloors : totalStories,
//                            seismicZone,
//                            gradeSchedule,
//                            floorConfig.NtaWallThickness,
//                            floorConfig.WallThicknessOverrides);

//                        if (!isTerrace)
//                        {
//                            wallImporter.ImportWalls(
//                                floorConfig.LayerMapping,
//                                wallPlacementElevation,
//                                currentStoryIndex);
//                        }
//                        else
//                        {
//                            Debug.WriteLine("⛔ Skipping walls at Terrace story");
//                        }

//                        beamImporter.ImportBeams(
//                            floorConfig.LayerMapping,
//                            slabBeamElevation,
//                            currentStoryIndex);

//                        slabImporter.ImportSlabs(
//                            floorConfig.LayerMapping,
//                            slabBeamElevation,
//                            currentStoryIndex);

//                        currentStoryIndex++;

//                        if ((floor + 1) % 5 == 0)
//                            sapModel.View.RefreshView(0, false);
//                    }

//                    Debug.WriteLine($"   ✓ Completed {floorConfig.Name}");
//                    sapModel.View.RefreshView(0, false);
//                }

//                Debug.WriteLine("\n🔄 Final model refresh...");
//                sapModel.View.RefreshView(0, true);

//                ShowImportSummary(floorConfigs, totalStories,
//                    storyManager.GetTotalBuildingHeight(),
//                    totalTypicalFloors, seismicZone, gradeSchedule);

//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"\n❌ IMPORT FAILED: {ex.Message}\n{ex.StackTrace}");
//                MessageBox.Show(
//                    $"❌ Import failed:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
//                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return false;
//            }
//        }

//        // ====================================================================
//        // HELPERS
//        // ====================================================================

//        private bool ValidateCADCoordinates(List<FloorTypeConfig> floorConfigs)
//        {
//            const double COORD_TOLERANCE = 500.0;
//            var centroids = new Dictionary<string, (double cx, double cy, string name)>(StringComparer.OrdinalIgnoreCase);

//            foreach (var config in floorConfigs)
//            {
//                if (string.IsNullOrEmpty(config.CADFilePath) || centroids.ContainsKey(config.CADFilePath))
//                    continue;
//                try
//                {
//                    var doc = DxfDocument.Load(config.CADFilePath);
//                    if (doc == null) continue;
//                    double minX = double.MaxValue, minY = double.MaxValue;
//                    double maxX = double.MinValue, maxY = double.MinValue;
//                    bool hasGeom = false;
//                    foreach (var l in doc.Entities.Lines)
//                    {
//                        minX = Math.Min(minX, Math.Min(l.StartPoint.X, l.EndPoint.X));
//                        minY = Math.Min(minY, Math.Min(l.StartPoint.Y, l.EndPoint.Y));
//                        maxX = Math.Max(maxX, Math.Max(l.StartPoint.X, l.EndPoint.X));
//                        maxY = Math.Max(maxY, Math.Max(l.StartPoint.Y, l.EndPoint.Y));
//                        hasGeom = true;
//                    }
//                    foreach (var p in doc.Entities.Polylines2D)
//                        foreach (var v in p.Vertexes)
//                        {
//                            minX = Math.Min(minX, v.Position.X); minY = Math.Min(minY, v.Position.Y);
//                            maxX = Math.Max(maxX, v.Position.X); maxY = Math.Max(maxY, v.Position.Y);
//                            hasGeom = true;
//                        }
//                    if (!hasGeom) continue;
//                    centroids[config.CADFilePath] = ((minX + maxX) / 2, (minY + maxY) / 2, config.Name);
//                    Debug.WriteLine($"  {config.Name.PadRight(14)}: centroid=({(minX + maxX) / 2:F0},{(minY + maxY) / 2:F0})mm");
//                }
//                catch (Exception ex) { Debug.WriteLine($"  ⚠ {config.Name}: {ex.Message}"); }
//            }

//            if (centroids.Count < 2) return true;

//            var entries = centroids.Values.ToList();
//            var reference = entries[0];
//            var misaligned = new List<string>();
//            foreach (var e in entries.Skip(1))
//            {
//                double dist = Math.Sqrt(Math.Pow(e.cx - reference.cx, 2) + Math.Pow(e.cy - reference.cy, 2));
//                if (dist > COORD_TOLERANCE)
//                    misaligned.Add($"  • {e.name}: dist={dist:F0}mm from {reference.name}");
//            }
//            if (misaligned.Count == 0) { Debug.WriteLine("  ✅ All CAD files aligned."); return true; }

//            var msg = new StringBuilder();
//            msg.AppendLine("⚠️ CAD COORDINATE MISALIGNMENT");
//            misaligned.ForEach(m => msg.AppendLine(m));
//            msg.AppendLine("\nFix in AutoCAD: MOVE → select all → reference point → 0,0");
//            msg.AppendLine("\nContinue anyway?");
//            return MessageBox.Show(msg.ToString(), "⚠️ Alignment Warning",
//                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
//                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
//        }

//        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> configs)
//        {
//            int total = 0;
//            foreach (var c in configs)
//                if (c.Name == "Typical" || c.Name == "Refuge") total += c.Count;
//            return total;
//        }

//        private void ShowDesignNotes(
//            List<FloorTypeConfig> floorConfigs, int totalTypicalFloors,
//            string seismicZone, GradeScheduleManager gradeSchedule)
//        {
//            var msg = new StringBuilder();
//            msg.AppendLine($"Zone: {seismicZone}");
//            msg.AppendLine("\nFLOORS:");
//            foreach (var c in floorConfigs)
//                msg.AppendLine(c.IsIndividualBasement
//                    ? $"  Basement{c.BasementNumber}: height={c.Height:F2}m"
//                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");
//            msg.AppendLine("\nGRADES:");
//            foreach (var r in gradeSchedule.GetGradeRanges())
//                msg.AppendLine($"  F{r.StartFloor + 1:D2}-{r.EndFloor + 1:D2}: {r.WallGrade}/{r.BeamSlabGrade}");
//            if (MessageBox.Show(msg.ToString(), "Confirm Import",
//                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
//                throw new Exception("Import cancelled by user");
//        }

//        private void ShowImportSummary(
//            List<FloorTypeConfig> configs, int totalStories, double totalHeight,
//            int typicalFloors, string seismicZone, GradeScheduleManager gradeSchedule)
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("═══════════════════════════════════");
//            sb.AppendLine("✅ IMPORT SUCCESSFUL");
//            sb.AppendLine("═══════════════════════════════════");
//            sb.AppendLine($"Total Stories  : {totalStories}");
//            sb.AppendLine($"Building Height: {totalHeight:F2}m");
//            sb.AppendLine($"Seismic Zone   : {seismicZone}");
//            if (typicalFloors > 0) sb.AppendLine($"Typical+Refuge : {typicalFloors}");
//            sb.AppendLine("\nFLOOR SUMMARY:");
//            foreach (var c in configs)
//                sb.AppendLine(c.IsIndividualBasement
//                    ? $"  Basement{c.BasementNumber}: 1×{c.Height:F2}m"
//                    : $"  {c.Name}: {c.Count}×{c.Height:F2}m");
//            sb.AppendLine("\nGRADE SCHEDULE:");
//            foreach (var r in gradeSchedule.GetGradeRanges())
//                sb.AppendLine($"  F{r.StartFloor + 1}-F{r.EndFloor + 1}: {r.WallGrade}/{r.BeamSlabGrade}");
//            sb.AppendLine("═══════════════════════════════════");
//            Debug.WriteLine("\n" + sb);
//            MessageBox.Show(sb.ToString(), "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
//        }
//    }
//}

// ============================================================================
// FILE: Core/CADImporterEnhanced.cs — VERSION 4.4
//
// PLACEMENT RULES (per story type):
//
//  BASEMENT (IsIndividualBasement == true):
//    Step A — Foundation walls (separate pass, before basement walls):
//      elevation = 0.0
//      height    = foundationHeight  (e.g. 1.5m)
//      CAD plan  = same Basement1 CAD file
//      Sections  = same grade/thickness as Basement1  ← identical WallImporter call
//
//    Step B — Basement walls:
//      elevation = foundationHeight  (e.g. 1.5m)
//      height    = storyHeight       (e.g. 3.5m)
//
//    Slab/beams:
//      elevation = foundationHeight + 0.005  (e.g. 1.505m)
//
//  ALL OTHER STORIES (Podium, Ground, Typical...):
//      wall elevation = storyBaseElevation
//      wall height    = storyHeight
//      slab elevation = storyBaseElevation + 0.005
//
//  ETABS story table (from StoryManager v5.0):
//      Base      = 0.0m
//      Basement1 = height 5.0m (foundationHeight+wallHeight)  top = 5.0m
//      Podium1   = height 4.5m                                top = 9.5m
//
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
            List<string> storyNames,
            string seismicZone,
            List<string> wallGrades,
            List<int> floorsPerGrade,
            double foundationHeight = 0.0)
        {
            try
            {
                Debug.WriteLine("\n╔═════════════════════════════════════╗");
                Debug.WriteLine("║  CAD IMPORTER v4.4                 ║");
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

                // StoryManager v5.0:
                //   Base=0, Basement1 ETABS height = foundationHeight + wallHeight
                //   storyBaseElevations[0] = foundationHeight (for geometry)
                Debug.WriteLine("📐 Creating stories...");
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames, foundationHeight);

                Debug.WriteLine("🔍 Validating CAD alignment...");
                if (!ValidateCADCoordinates(floorConfigs)) return false;

                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

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

                        double geomBase = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double geomTop = storyManager.GetStoryTopElevation(currentStoryIndex);
                        double wallHt = geomTop - geomBase;    // user-supplied wall height
                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
                        string storyName = storyManager.GetStoryNameByIndex(currentStoryIndex);
                        bool isBasement = floorConfig.IsIndividualBasement;
                        bool isTerrace = storyName.Equals("Terrace", StringComparison.OrdinalIgnoreCase);

                        Debug.WriteLine($"│");
                        Debug.WriteLine($"│  [{floor + 1}/{floorConfig.Count}] {storyName}  (idx={currentStoryIndex})");
                        Debug.WriteLine($"│    GeomBase={geomBase:F3}  GeomTop={geomTop:F3}  WallHt={wallHt:F3}");
                        Debug.WriteLine($"│    Grade={wallGrade}  NTA={floorConfig.NtaWallThickness}mm");

                        if (isBasement && foundationHeight > 0)
                        {
                            // ── STEP A: Foundation walls ──────────────────────
                            // Z=0 → foundationHeight, same CAD, same sections as Basement
                            Debug.WriteLine($"│");
                            Debug.WriteLine($"│    [A] Foundation walls  Z=0 → {foundationHeight:F3}m");

                            var foundWallImporter = new WallImporterEnhanced(
                                sapModel, dxfDoc,
                                foundationHeight,   // height = 1.5m
                                wallRef, seismicZone, gradeSchedule,
                                floorConfig.NtaWallThickness,
                                floorConfig.WallThicknessOverrides);

                            foundWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                0.0,                // placed at Z=0
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Foundation walls placed: 0 → {foundationHeight:F3}m");

                            // ── STEP B: Basement walls ────────────────────────
                            // Z=foundationHeight → geomTop (1.5 → 5.0)
                            Debug.WriteLine($"│    [B] Basement walls    Z={foundationHeight:F3} → {geomTop:F3}m");

                            var basementWallImporter = new WallImporterEnhanced(
                                sapModel, dxfDoc,
                                wallHt,             // height = 3.5m (user wall height)
                                wallRef, seismicZone, gradeSchedule,
                                floorConfig.NtaWallThickness,
                                floorConfig.WallThicknessOverrides);

                            basementWallImporter.ImportWalls(
                                floorConfig.LayerMapping,
                                foundationHeight,   // placed at Z=1.5m
                                currentStoryIndex);

                            Debug.WriteLine($"│    ✓ Basement walls placed: {foundationHeight:F3} → {geomTop:F3}m");

                            // ── Slab / beams ──────────────────────────────────
                            double slabElev = foundationHeight + 0.005;
                            Debug.WriteLine($"│    Slab/Beam Z={slabElev:F3}m");
                            beamImporter.ImportBeams(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                            slabImporter.ImportSlabs(floorConfig.LayerMapping, slabElev, currentStoryIndex);
                        }
                        else
                        {
                            // ── NORMAL STORY ──────────────────────────────────
                            double slabElev = geomBase + 0.005;
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
                    double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue; bool g = false;
                    foreach (var l in doc.Entities.Lines) { x0 = Math.Min(x0, Math.Min(l.StartPoint.X, l.EndPoint.X)); y0 = Math.Min(y0, Math.Min(l.StartPoint.Y, l.EndPoint.Y)); x1 = Math.Max(x1, Math.Max(l.StartPoint.X, l.EndPoint.X)); y1 = Math.Max(y1, Math.Max(l.StartPoint.Y, l.EndPoint.Y)); g = true; }
                    foreach (var p in doc.Entities.Polylines2D) foreach (var v in p.Vertexes) { x0 = Math.Min(x0, v.Position.X); y0 = Math.Min(y0, v.Position.Y); x1 = Math.Max(x1, v.Position.X); y1 = Math.Max(y1, v.Position.Y); g = true; }
                    if (g) c[f.CADFilePath] = ((x0 + x1) / 2, (y0 + y1) / 2, f.Name);
                }
                catch { }
            }
            if (c.Count < 2) return true;
            var list = c.Values.ToList(); var r = list[0]; var bad = new List<string>();
            foreach (var e in list.Skip(1)) { double d = Math.Sqrt(Math.Pow(e.cx - r.cx, 2) + Math.Pow(e.cy - r.cy, 2)); if (d > TOL) bad.Add($"  • {e.n}: {d:F0}mm"); }
            if (bad.Count == 0) return true;
            var sb = new StringBuilder("⚠️ CAD MISALIGNMENT\n"); bad.ForEach(b => sb.AppendLine(b));
            sb.AppendLine("\nFix: AutoCAD → MOVE → select all → ref point → 0,0\n\nContinue anyway?");
            return MessageBox.Show(sb.ToString(), "Alignment Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> c)
        {
            int n = 0; foreach (var f in c) if (f.Name == "Typical" || f.Name == "Refuge") n += f.Count; return n;
        }

        private void ShowDesignNotes(List<FloorTypeConfig> fc, int typ, string zone,
            GradeScheduleManager g, double fh)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Zone: {zone}");
            if (fh > 0)
            {
                sb.AppendLine($"\nFOUNDATION WALLS:");
                sb.AppendLine($"  Z = 0 → {fh:F2}m  (no story, same sections as Basement1)");
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
                sb.AppendLine($"Foundation    : 0 → {fh:F2}m  (same sections as Basement1)");
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
