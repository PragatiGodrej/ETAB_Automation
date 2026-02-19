
// ============================================================================
// FILE: Core/CADImporterEnhanced.cs — VERSION 3.3
// ============================================================================

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
            sapModel     = model;
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
                Debug.WriteLine("\n╔════════════════════════════════════════════════════╗");
                Debug.WriteLine("║  CAD IMPORTER v3.3 — TERRACE + SETSTORIES FIXED   ║");
                Debug.WriteLine("╚════════════════════════════════════════════════════╝\n");

                sapModel.SetModelIsLocked(false);
                sapModel.SetPresentUnits(eUnits.N_m_C);

                var gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);
                int totalStories  = storyHeights.Count;

                if (!gradeSchedule.ValidateTotalFloors(totalStories))
                {
                    MessageBox.Show(
                        $"❌ Grade schedule floor count doesn't match!\n\n" +
                        $"Expected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
                        "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);

                ShowDesignNotes(floorConfigs, totalTypicalFloors, seismicZone,
                                gradeSchedule, foundationHeight);

                Debug.WriteLine("\n📐 Creating building stories...");
                // StoryManager v2.5: similar[]=null fix + duplicate name guard + diagnostic dump
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames, foundationHeight);

                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

                // ============================================================
                // FOUNDATION WALLS (walls only — no beams, no slabs)
                // ============================================================
                if (foundationHeight > 0)
                {
                    Debug.WriteLine($"\n╔═══════════════════════════════════════╗");
                    Debug.WriteLine(($"║  FOUNDATION WALLS: {foundationHeight:F2}m").PadRight(41) + "║");
                    Debug.WriteLine($"╚═══════════════════════════════════════╝");

                    var firstBasement = floorConfigs.FirstOrDefault(
                        c => c.IsIndividualBasement && c.BasementNumber == 1);

                    if (firstBasement == null)
                    {
                        MessageBox.Show(
                            "⚠️ Foundation height specified but no Basement1 found.\n\n" +
                            "Foundation walls require a Basement1 floor.",
                            "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        DxfDocument foundationDxf = DxfDocument.Load(firstBasement.CADFilePath);
                        if (foundationDxf == null)
                        {
                            MessageBox.Show(
                                $"❌ Failed to load CAD file for foundation walls\n\nFile: {firstBasement.CADFilePath}",
                                "CAD Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        var foundationWallImporter = new WallImporterEnhanced(
                            sapModel,
                            foundationDxf,
                            foundationHeight,
                            totalTypicalFloors > 0 ? totalTypicalFloors : totalStories,
                            seismicZone,
                            gradeSchedule);

                        foundationWallImporter.ImportWalls(
                            firstBasement.LayerMapping,
                            0.0,   // base of building (below foundation)
                            0);    // story index 0 for grade lookup

                        Debug.WriteLine("   ✓ Foundation walls created");
                        sapModel.View.RefreshView(0, false);
                    }
                }

                // ============================================================
                // IMPORT EACH FLOOR TYPE
                // ============================================================
                int currentStoryIndex = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    Debug.WriteLine($"\n╔═══════════════════════════════════════╗");
                    Debug.WriteLine($"║  FLOOR TYPE: {floorConfig.Name.ToUpper().PadRight(27)}║");
                    Debug.WriteLine($"╚═══════════════════════════════════════╝");

                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
                    if (dxfDoc == null)
                    {
                        MessageBox.Show(
                            $"❌ Failed to load CAD file for {floorConfig.Name}\n\nFile: {floorConfig.CADFilePath}",
                            "CAD Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    Debug.WriteLine($"✓ Loaded: {System.IO.Path.GetFileName(floorConfig.CADFilePath)}");

                    var beamImporter = new BeamImporterEnhanced(
                        sapModel, dxfDoc, seismicZone,
                        totalTypicalFloors > 0 ? totalTypicalFloors : totalStories,
                        floorConfig.BeamDepths, gradeSchedule,
                        floorConfig.BeamWidthOverrides);

                    var slabImporter = new SlabImporterEnhanced(
                        sapModel, dxfDoc,
                        floorConfig.SlabThicknesses, gradeSchedule);

                    Debug.WriteLine($"\n📥 Importing {floorConfig.Count} floor(s) of {floorConfig.Name}:");

                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        if (currentStoryIndex >= totalStories)
                        {
                            MessageBox.Show(
                                $"❌ Story index out of bounds!\n\n" +
                                $"Story {currentStoryIndex} does not exist (total: {totalStories}).\n" +
                                $"Floor config: {floorConfig.Name} floor {floor + 1}/{floorConfig.Count}",
                                "Index Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        double baseElevation  = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double topElevation   = storyManager.GetStoryTopElevation(currentStoryIndex);
                        double thisFloorHeight = topElevation - baseElevation;

                        string wallGrade     = gradeSchedule.GetWallGrade(currentStoryIndex);
                        string beamSlabGrade = gradeSchedule.GetBeamSlabGrade(currentStoryIndex);

                        // --------------------------------------------------------
                        // FIX: Terrace elements must be placed at the ETABS-actual
                        // elevation (120.001m), not the computed top (120.000m).
                        // ETABS assigns Terrace top = base + 0.001m nominal height,
                        // so elements placed at 120.000m end up in Story34's plan,
                        // not the Terrace plan view.
                        // For all other stories, use baseElevation (slab-at-base convention).
                        // --------------------------------------------------------
                        bool isTerraceStory = storyNames[currentStoryIndex]
                            .Equals("Terrace", StringComparison.OrdinalIgnoreCase);

                        double placementElevation = isTerraceStory
                            ? storyManager.GetETABSStoryElevation(storyNames[currentStoryIndex])
                            : baseElevation;

                        Debug.WriteLine($"\n   [{floor + 1}/{floorConfig.Count}] " +
                                        $"{storyNames[currentStoryIndex]} (idx {currentStoryIndex})");
                        Debug.WriteLine($"       Grade      : Wall={wallGrade}, Beam/Slab={beamSlabGrade}");
                        Debug.WriteLine($"       Base       : {baseElevation:F3}m");
                        Debug.WriteLine($"       Top        : {topElevation:F3}m");
                        Debug.WriteLine($"       Height     : {thisFloorHeight:F3}m");
                        Debug.WriteLine($"       IsTerrace  : {isTerraceStory}");
                        Debug.WriteLine($"\n       Placement:");
                        Debug.WriteLine($"         Walls  → base={placementElevation:F3}m, " +
                                        $"height={thisFloorHeight:F3}m");
                        Debug.WriteLine($"         Beams  → {placementElevation:F3}m");
                        Debug.WriteLine($"         Slabs  → {placementElevation:F3}m");

                        // Wall importer is per-floor (thisFloorHeight changes each story)
                        var wallImporter = new WallImporterEnhanced(
                            sapModel, dxfDoc,
                            thisFloorHeight,
                            totalTypicalFloors > 0 ? totalTypicalFloors : totalStories,
                            seismicZone,
                            gradeSchedule);

                        wallImporter.ImportWalls(
                            floorConfig.LayerMapping,
                            placementElevation,
                            currentStoryIndex);

                        beamImporter.ImportBeams(
                            floorConfig.LayerMapping,
                            placementElevation,
                            currentStoryIndex);

                        slabImporter.ImportSlabs(
                            floorConfig.LayerMapping,
                            placementElevation,
                            currentStoryIndex);

                        currentStoryIndex++;

                        if ((floor + 1) % 5 == 0)
                            sapModel.View.RefreshView(0, false);
                    }

                    Debug.WriteLine($"   ✓ Completed {floorConfig.Name}");
                    sapModel.View.RefreshView(0, false);
                }

                Debug.WriteLine("\n🔄 Final model refresh...");
                sapModel.View.RefreshView(0, true);

                ShowImportSummary(floorConfigs, totalStories,
                    storyManager.GetTotalBuildingHeight(), totalTypicalFloors,
                    seismicZone, gradeSchedule, foundationHeight);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n❌ IMPORT FAILED: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"❌ Import failed:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> configs)
        {
            foreach (var c in configs)
                if (c.Name == "Typical") return c.Count;
            return 0;
        }

        private void ShowDesignNotes(
            List<FloorTypeConfig> floorConfigs, int totalTypicalFloors,
            string seismicZone, GradeScheduleManager gradeSchedule,
            double foundationHeight)
        {
            var msg = new StringBuilder();
            msg.AppendLine("═══════════════════════════════════════");
            msg.AppendLine($"Zone {seismicZone} | Building Configuration");
            msg.AppendLine("═══════════════════════════════════════\n");

            if (foundationHeight > 0)
            {
                msg.AppendLine($"🏗️ FOUNDATION:");
                msg.AppendLine($"  Height: {foundationHeight:F2}m  (walls only — no beams/slabs)");
                msg.AppendLine($"  CAD:    Using Basement1 plan");
                msg.AppendLine();
            }

            msg.AppendLine("FLOOR BREAKDOWN:");
            int basementCount = 0;
            foreach (var c in floorConfigs)
            {
                if (c.IsIndividualBasement)
                {
                    basementCount++;
                    msg.AppendLine($"  B{c.BasementNumber} — {c.Name}: {c.Height:F2}m  (1 floor)");
                }
                else
                    msg.AppendLine($"  {c.Name}: {c.Count} × {c.Height:F2}m");
            }
            if (basementCount > 0)
                msg.AppendLine($"\n  Total Individual Basements: {basementCount}");

            msg.AppendLine("\nCONCRETE GRADES:");
            foreach (var r in gradeSchedule.GetGradeRanges())
                msg.AppendLine($"  F{r.StartFloor + 1:D2}-F{r.EndFloor + 1:D2}: {r.WallGrade} / {r.BeamSlabGrade}");

            msg.AppendLine("\nBEAM/SLAB CONFIG:");
            foreach (var c in floorConfigs)
            {
                string prefix = c.IsIndividualBasement ? $"B{c.BasementNumber}" : c.Name;
                msg.AppendLine($"  {prefix}: " +
                    $"Grav {c.BeamDepths["InternalGravity"]}/{c.BeamDepths["CantileverGravity"]}mm | " +
                    $"Main {c.BeamDepths["CoreMain"]}/{c.BeamDepths["PeripheralPortalMain"]}mm | " +
                    $"Slab {c.SlabThicknesses["Lobby"]}/{c.SlabThicknesses["Stair"]}mm");
            }

            int refFloors = totalTypicalFloors > 0 ? totalTypicalFloors : floorConfigs.Sum(c => c.Count);
            int coreThick = WallThicknessCalculator.GetRecommendedThickness(
                refFloors, WallThicknessCalculator.WallType.CoreWall, seismicZone, 2.0, false);
            int periThick = WallThicknessCalculator.GetRecommendedThickness(
                refFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, seismicZone, 2.0, false);
            msg.AppendLine($"\nRECOMMENDED WALLS: Core ~{coreThick}mm | Peripheral ~{periThick}mm");
            msg.AppendLine("\n═══════════════════════════════════════");

            if (MessageBox.Show(msg.ToString() + "\n\nProceed with import?",
                "Confirm Configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
                throw new Exception("Import cancelled by user");
        }

        private void ShowImportSummary(
            List<FloorTypeConfig> configs, int totalStories, double totalHeight,
            int typicalFloors, string seismicZone, GradeScheduleManager gradeSchedule,
            double foundationHeight)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("✅ IMPORT SUCCESSFUL");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Total Stories:    {totalStories}");
            sb.AppendLine($"Building Height:  {totalHeight:F2}m");
            if (foundationHeight > 0)
                sb.AppendLine($"Foundation:       {foundationHeight:F2}m (walls only)");
            sb.AppendLine($"Seismic Zone:     {seismicZone}");
            if (typicalFloors > 0)
                sb.AppendLine($"Typical Floors:   {typicalFloors}");
            sb.AppendLine();
            sb.AppendLine("FLOOR SUMMARY:");
            foreach (var c in configs)
            {
                if (c.IsIndividualBasement)
                    sb.AppendLine($"  Basement {c.BasementNumber}: 1 × {c.Height:F2}m");
                else
                    sb.AppendLine($"  {c.Name}: {c.Count} × {c.Height:F2}m");
            }
            sb.AppendLine("\nGRADE SCHEDULE:");
            foreach (var r in gradeSchedule.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor + 1:D2}-F{r.EndFloor + 1:D2}: {r.WallGrade} / {r.BeamSlabGrade}");
            sb.AppendLine("═══════════════════════════════════════");

            Debug.WriteLine("\n" + sb.ToString());
            MessageBox.Show(sb.ToString(), "Import Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
