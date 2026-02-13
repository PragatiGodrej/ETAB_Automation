
// ============================================================================
// FILE: Importers/CADImporterEnhanced.cs (UPDATED - PER-FLOOR-TYPE CONFIG)
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
        //private MaterialManager materialManager;
        private StoryManager storyManager;

        public CADImporterEnhanced(cSapModel model)
        {
            sapModel = model;
            //materialManager = new MaterialManager(model);
            storyManager = new StoryManager(model);
        }

        public bool ImportMultiFloorTypeCAD(
            List<FloorTypeConfig> floorConfigs,
            List<double> storyHeights,
            List<string> storyNames,
            string seismicZone,
            Dictionary<string, int> beamDepths,
            Dictionary<string, int> slabThicknesses,
            List<string> wallGrades,
            List<int> floorsPerGrade)
        {
            try
            {
                sapModel.SetModelIsLocked(false);

                // Set unit context
                eUnits previousUnits = sapModel.GetPresentUnits();
                Debug.WriteLine($"\nSetting units from {previousUnits} to N_m_C");
                sapModel.SetPresentUnits(eUnits.N_m_C);

                // Create grade schedule manager
                GradeScheduleManager gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);

                // Validate grade schedule
                int totalStories = storyHeights.Count;
                if (!gradeSchedule.ValidateTotalFloors(totalStories))
                {
                    MessageBox.Show(
                        $"Grade schedule floor count doesn't match!\n\n" +
                        $"Expected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // Calculate total typical floors
                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);

                // Show design notes (COMPACT VERSION)
                ShowDesignNotes(totalTypicalFloors, seismicZone, beamDepths, slabThicknesses, gradeSchedule);

                // Define materials
                // materialManager.DefineMaterials();

                // Define stories
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

                // Load wall sections
                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

                // Import each floor type
                int currentStoryIndex = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    // Load CAD file
                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
                    if (dxfDoc == null)
                    {
                        MessageBox.Show($"Failed to load CAD file for {floorConfig.Name}", "Error");
                        return false;
                    }

                    // Create importers
                    BeamImporterEnhanced beamImporter = new BeamImporterEnhanced(
                        sapModel, dxfDoc, seismicZone, totalTypicalFloors,
                        beamDepths, gradeSchedule);

                    WallImporterEnhanced wallImporter = new WallImporterEnhanced(
                        sapModel, dxfDoc, floorConfig.Height, totalTypicalFloors,
                        seismicZone, gradeSchedule);

                    SlabImporterEnhanced slabImporter = new SlabImporterEnhanced(
                        sapModel, dxfDoc, slabThicknesses, gradeSchedule);

                    // Import floors
                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        double baseElevation = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double topElevation = storyManager.GetStoryTopElevation(currentStoryIndex);

                        wallImporter.ImportWalls(floorConfig.LayerMapping, baseElevation, currentStoryIndex);
                        beamImporter.ImportBeams(floorConfig.LayerMapping, topElevation, currentStoryIndex);
                        slabImporter.ImportSlabs(floorConfig.LayerMapping, topElevation, currentStoryIndex);

                        currentStoryIndex++;
                    }

                    sapModel.View.RefreshView(0, false);
                }



                sapModel.View.RefreshView(0, true);

                MessageBox.Show(
                    $"✅ Import completed!\n\n" +
                    BuildImportSummary(floorConfigs, totalStories,
                        storyManager.GetTotalBuildingHeight(), totalTypicalFloors, seismicZone,
                        beamDepths, slabThicknesses),
                    "Import Success");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed:\n{ex.Message}", "Error");
                return false;
            }
        }

        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> configs)
        {
            foreach (var config in configs)
            {
                if (config.Name == "Typical")
                    return config.Count;
            }
            return configs.Sum(c => c.Count);
        }

        private void ShowDesignNotes(
            int totalTypicalFloors,
            string seismicZone,
            Dictionary<string, int> beamDepths,
            Dictionary<string, int> slabThicknesses,
            GradeScheduleManager gradeSchedule)
        {
            StringBuilder notes = new StringBuilder();

            notes.AppendLine("DESIGN CONFIGURATION SUMMARY");
            notes.AppendLine("═══════════════════════════════════════\n");

            notes.AppendLine($"Seismic Zone: {seismicZone}");
            notes.AppendLine($"Total Typical Floors: {totalTypicalFloors}\n");

            notes.AppendLine("CONCRETE GRADES:");
            foreach (var range in gradeSchedule.GetGradeRanges())
            {
                notes.AppendLine($"  Floors {range.StartFloor}-{range.EndFloor}: " +
                    $"Wall={range.WallGrade}, Beam/Slab={range.BeamSlabGrade}");
            }
            notes.AppendLine();

            int gravityWidth = seismicZone == "Zone II" || seismicZone == "Zone III" ? 200 : 240;
            notes.AppendLine("BEAM DEPTHS:");
            notes.AppendLine($"  Gravity: {gravityWidth}x{beamDepths["InternalGravity"]}mm, " +
                $"Cantilever: {gravityWidth}x{beamDepths["CantileverGravity"]}mm");
            notes.AppendLine($"  Main: Core={beamDepths["CoreMain"]}mm, " +
                $"Peripheral={beamDepths["PeripheralDeadMain"]}mm\n");

            notes.AppendLine("SLAB THICKNESSES:");
            notes.AppendLine($"  Lobby={slabThicknesses["Lobby"]}mm, " +
                $"Stair={slabThicknesses["Stair"]}mm, Regular=125-250mm\n");

            int coreThick = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, WallThicknessCalculator.WallType.CoreWall, seismicZone, 2.0, false);
            int periThick = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, seismicZone, 2.0, false);

            notes.AppendLine("WALL THICKNESSES:");
            notes.AppendLine($"  Core={coreThick}mm, Peripheral={periThick}mm\n");

            notes.AppendLine("═══════════════════════════════════════");
            notes.AppendLine("Units: All dimensions in METERS (N_m_C)");

            var result = MessageBox.Show(
                notes.ToString() + "\n\nProceed with import?",
                "Confirm Design Parameters",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                throw new Exception("Import cancelled by user");
            }
        }

        private string BuildImportSummary(
            List<FloorTypeConfig> configs,
            int totalStories,
            double totalHeight,
            int typicalFloors,
            string seismicZone,
            Dictionary<string, int> beamDepths,
            Dictionary<string, int> slabThicknesses)
        {
            StringBuilder summary = new StringBuilder();

            foreach (var config in configs)
            {
                summary.AppendLine($"- {config.Name}: {config.Count} floor(s) × {config.Height:F2}m");
            }

            summary.AppendLine($"\nTotal Stories: {totalStories}");
            summary.AppendLine($"Total Height: {totalHeight:F2}m");
            summary.AppendLine($"Typical Floors: {typicalFloors}");
            summary.AppendLine($"Seismic Zone: {seismicZone}");

            int gravityWidth = seismicZone == "Zone II" || seismicZone == "Zone III" ? 200 : 240;
            summary.AppendLine($"\n✓ Gravity beams: {gravityWidth}mm width");
            summary.AppendLine($"✓ Slabs: Lobby {slabThicknesses["Lobby"]}mm, Stair {slabThicknesses["Stair"]}mm");

            return summary.ToString();
        }
    }
}
