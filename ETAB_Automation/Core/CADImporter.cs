


// ============================================================================
// FILE: Importers/CADImporterEnhanced.cs (UPDATED - PER-FLOOR-TYPE CONFIG)
// ============================================================================
// PURPOSE: Enhanced CAD importer with per-floor-type beam and slab configuration
//          Each floor type (Basement, Podium, EDeck, Typical, Terrace) can have
//          its own unique beam depths and slab thicknesses
// VERSION: 3.0 (Per-Floor Configuration Support)
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
    /// <summary>
    /// Enhanced CAD importer that supports:
    /// - Multiple floor types with different configurations
    /// - Per-floor-type beam depths and slab thicknesses
    /// - Concrete grade schedules
    /// - Seismic zone-based design rules
    /// </summary>
    public class CADImporterEnhanced
    {
        private cSapModel sapModel;
        private StoryManager storyManager;

        public CADImporterEnhanced(cSapModel model)
        {
            sapModel = model;
            storyManager = new StoryManager(model);
        }

        /// <summary>
        /// Import multiple floor types with per-floor-type configurations
        /// </summary>
        /// <param name="floorConfigs">List of floor configurations (each contains beam/slab data)</param>
        /// <param name="storyHeights">Story heights in meters</param>
        /// <param name="storyNames">Story names</param>
        /// <param name="seismicZone">Seismic zone (Zone II, III, IV, or V)</param>
        /// <param name="wallGrades">Wall concrete grades</param>
        /// <param name="floorsPerGrade">Floors per grade segment</param>
        /// <returns>True if import successful</returns>
        public bool ImportMultiFloorTypeCAD(
            List<FloorTypeConfig> floorConfigs,
            List<double> storyHeights,
            List<string> storyNames,
            string seismicZone,
            List<string> wallGrades,
            List<int> floorsPerGrade)
        {
            try
            {
                Debug.WriteLine("\n╔════════════════════════════════════════════════════╗");
                Debug.WriteLine("║     CAD IMPORTER - PER-FLOOR CONFIGURATION         ║");
                Debug.WriteLine("╚════════════════════════════════════════════════════╝\n");

                // Unlock model
                sapModel.SetModelIsLocked(false);

                // Set unit context to N_m_C (Newtons, meters, Celsius)
                eUnits previousUnits = sapModel.GetPresentUnits();
                Debug.WriteLine($"⚙️  Setting units from {previousUnits} to N_m_C");
                sapModel.SetPresentUnits(eUnits.N_m_C);

                // Create grade schedule manager
                GradeScheduleManager gradeSchedule = new GradeScheduleManager(wallGrades, floorsPerGrade);

                // Validate grade schedule
                int totalStories = storyHeights.Count;
                if (!gradeSchedule.ValidateTotalFloors(totalStories))
                {
                    MessageBox.Show(
                        $"❌ Grade schedule floor count doesn't match!\n\n" +
                        $"Expected: {totalStories}\nGot: {floorsPerGrade.Sum()}",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // Calculate total typical floors
                int totalTypicalFloors = CalculateTotalTypicalFloors(floorConfigs);

                // Show design notes
                ShowDesignNotes(floorConfigs, totalTypicalFloors, seismicZone, gradeSchedule);

                // Define stories
                Debug.WriteLine("\n📐 Creating building stories...");
                storyManager.DefineStoriesWithCustomNames(storyHeights, storyNames);

                // Load wall sections
                Debug.WriteLine("🧱 Loading available wall sections...");
                WallThicknessCalculator.LoadAvailableWallSections(sapModel);

                // ============================================================
                // IMPORT EACH FLOOR TYPE WITH ITS UNIQUE CONFIGURATION
                // ============================================================

                int currentStoryIndex = 0;

                foreach (var floorConfig in floorConfigs)
                {
                    Debug.WriteLine($"\n╔════════════════════════════════════════════════════╗");
                    Debug.WriteLine($"║  FLOOR TYPE: {floorConfig.Name.ToUpper().PadRight(40)} ║");
                    Debug.WriteLine($"╚════════════════════════════════════════════════════╝");

                    // Load CAD file for this floor type
                    DxfDocument dxfDoc = DxfDocument.Load(floorConfig.CADFilePath);
                    if (dxfDoc == null)
                    {
                        MessageBox.Show(
                            $"❌ Failed to load CAD file for {floorConfig.Name}\n\n" +
                            $"File: {floorConfig.CADFilePath}",
                            "CAD Load Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    Debug.WriteLine($"✓ CAD file loaded: {System.IO.Path.GetFileName(floorConfig.CADFilePath)}");
                    Debug.WriteLine($"   Floors: {floorConfig.Count}");
                    Debug.WriteLine($"   Height: {floorConfig.Height:F2}m each");

                    // Display beam configuration for this floor type
                    Debug.WriteLine($"\n🔧 Beam Configuration for {floorConfig.Name}:");
                    int gravityWidth = (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;
                    Debug.WriteLine($"   Gravity Beams ({gravityWidth}mm width):");
                    Debug.WriteLine($"      - Internal: {floorConfig.BeamDepths["InternalGravity"]}mm");
                    Debug.WriteLine($"      - Cantilever: {floorConfig.BeamDepths["CantileverGravity"]}mm");
                    Debug.WriteLine($"   Main Beams (width matches wall):");
                    Debug.WriteLine($"      - Core: {floorConfig.BeamDepths["CoreMain"]}mm");
                    Debug.WriteLine($"      - Peripheral Dead: {floorConfig.BeamDepths["PeripheralDeadMain"]}mm");
                    Debug.WriteLine($"      - Peripheral Portal: {floorConfig.BeamDepths["PeripheralPortalMain"]}mm");
                    Debug.WriteLine($"      - Internal: {floorConfig.BeamDepths["InternalMain"]}mm");

                    // Display slab configuration for this floor type
                    Debug.WriteLine($"\n📐 Slab Configuration for {floorConfig.Name}:");
                    Debug.WriteLine($"   - Lobby: {floorConfig.SlabThicknesses["Lobby"]}mm");
                    Debug.WriteLine($"   - Stair: {floorConfig.SlabThicknesses["Stair"]}mm");
                    Debug.WriteLine($"   - Regular: 125-250mm (area-based)");

                    // Create importers with THIS FLOOR TYPE'S configuration
                    BeamImporterEnhanced beamImporter = new BeamImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        seismicZone,
                        totalTypicalFloors,
                        floorConfig.BeamDepths,  // ⭐ USE FLOOR-SPECIFIC BEAM DEPTHS
                        gradeSchedule);

                    WallImporterEnhanced wallImporter = new WallImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        floorConfig.Height,
                        totalTypicalFloors,
                        seismicZone,
                        gradeSchedule);

                    SlabImporterEnhanced slabImporter = new SlabImporterEnhanced(
                        sapModel,
                        dxfDoc,
                        floorConfig.SlabThicknesses,  // ⭐ USE FLOOR-SPECIFIC SLAB THICKNESSES
                        gradeSchedule);

                    // Import each floor of this type
                    Debug.WriteLine($"\n📥 Importing {floorConfig.Count} floor(s) of {floorConfig.Name}:");

                    for (int floor = 0; floor < floorConfig.Count; floor++)
                    {
                        // ✅ BOUNDS CHECK
                        if (currentStoryIndex >= totalStories)
                        {
                            MessageBox.Show(
                                $"❌ ERROR: Story index out of bounds!\n\n" +
                                $"Trying to access story {currentStoryIndex} but only {totalStories} stories exist.\n" +
                                $"Floor config: {floorConfig.Name}, floor {floor + 1}/{floorConfig.Count}",
                                "Index Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return false;
                        }

                        string currentStoryName = storyNames[currentStoryIndex];
                        Debug.WriteLine($"   [{floor + 1}/{floorConfig.Count}] {currentStoryName} (Index: {currentStoryIndex})");

                        // Get elevations
                        double baseElevation = storyManager.GetStoryBaseElevation(currentStoryIndex);
                        double topElevation = storyManager.GetStoryTopElevation(currentStoryIndex);

                        // Get concrete grade for this floor
                        string wallGrade = gradeSchedule.GetWallGrade(currentStoryIndex);
                        string beamSlabGrade = gradeSchedule.GetBeamSlabGrade(currentStoryIndex);
                        Debug.WriteLine($"       Concrete: Wall={wallGrade}, Beam/Slab={beamSlabGrade}");
                        Debug.WriteLine($"       Elevation: {baseElevation:F2}m - {topElevation:F2}m");

                        // Import structural elements
                        wallImporter.ImportWalls(
                            floorConfig.LayerMapping,
                            baseElevation,
                            currentStoryIndex);

                        beamImporter.ImportBeams(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex);

                        slabImporter.ImportSlabs(
                            floorConfig.LayerMapping,
                            topElevation,
                            currentStoryIndex);

                        currentStoryIndex++;

                        // Refresh view periodically
                        if ((floor + 1) % 5 == 0)
                        {
                            sapModel.View.RefreshView(0, false);
                        }
                    }

                    Debug.WriteLine($"   ✓ Completed {floorConfig.Name}");
                    sapModel.View.RefreshView(0, false);
                }

                // Final view refresh
                Debug.WriteLine("\n🔄 Final model refresh...");
                sapModel.View.RefreshView(0, true);

                // Show success summary
                Debug.WriteLine("\n╔════════════════════════════════════════════════════╗");
                Debug.WriteLine("║            IMPORT COMPLETED SUCCESSFULLY           ║");
                Debug.WriteLine("╚════════════════════════════════════════════════════╝\n");

                ShowImportSummary(floorConfigs, totalStories,
                    storyManager.GetTotalBuildingHeight(), totalTypicalFloors,
                    seismicZone, gradeSchedule);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"\n❌ IMPORT FAILED: {ex.Message}");
                Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");

                MessageBox.Show(
                    $"❌ Import failed:\n\n{ex.Message}\n\n" +
                    $"Stack Trace:\n{ex.StackTrace}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Calculate total number of typical floors
        /// </summary>
        private int CalculateTotalTypicalFloors(List<FloorTypeConfig> configs)
        {
            foreach (var config in configs)
            {
                if (config.Name == "Typical")
                    return config.Count;
            }
            // Fallback: sum all floors
            return configs.Sum(c => c.Count);
        }

        /// <summary>
        /// Show design configuration notes before import
        /// </summary>
        //private void ShowDesignNotes(
        //    List<FloorTypeConfig> floorConfigs,
        //    int totalTypicalFloors,
        //    string seismicZone,
        //    GradeScheduleManager gradeSchedule)
        //{
        //    StringBuilder notes = new StringBuilder();

        //    notes.AppendLine("╔════════════════════════════════════════════════════╗");
        //    notes.AppendLine("║       DESIGN CONFIGURATION SUMMARY                 ║");
        //    notes.AppendLine("╚════════════════════════════════════════════════════╝\n");

        //    notes.AppendLine($"🏗️  Seismic Zone: {seismicZone}");
        //    notes.AppendLine($"📊  Total Typical Floors: {totalTypicalFloors}\n");

        //    // Concrete grade schedule
        //    notes.AppendLine("🏗️ CONCRETE GRADE SCHEDULE:");
        //    foreach (var range in gradeSchedule.GetGradeRanges())
        //    {
        //        notes.AppendLine($"   Floors {range.StartFloor:D2}-{range.EndFloor:D2}: " +
        //            $"Wall={range.WallGrade}, Beam/Slab={range.BeamSlabGrade}");
        //    }
        //    notes.AppendLine();

        //    // Per-floor-type configurations
        //    notes.AppendLine("🔧 PER-FLOOR-TYPE CONFIGURATIONS:\n");

        //    int gravityWidth = seismicZone == "Zone II" || seismicZone == "Zone III" ? 200 : 240;

        //    foreach (var config in floorConfigs)
        //    {
        //        notes.AppendLine($"   {config.Name} ({config.Count} floor(s)):");

        //        notes.AppendLine($"      Beams:");
        //        notes.AppendLine($"         Gravity ({gravityWidth}mm): Internal={config.BeamDepths["InternalGravity"]}mm, " +
        //            $"Cantilever={config.BeamDepths["CantileverGravity"]}mm");
        //        notes.AppendLine($"         Main: Core={config.BeamDepths["CoreMain"]}mm, " +
        //            $"Peripheral={config.BeamDepths["PeripheralDeadMain"]}mm");

        //        notes.AppendLine($"      Slabs:");
        //        notes.AppendLine($"         Lobby={config.SlabThicknesses["Lobby"]}mm, " +
        //            $"Stair={config.SlabThicknesses["Stair"]}mm, Regular=125-250mm\n");
        //    }

        //    // Wall thicknesses (estimated)
        //    int coreThick = WallThicknessCalculator.GetRecommendedThickness(
        //        totalTypicalFloors, WallThicknessCalculator.WallType.CoreWall,
        //        seismicZone, 2.0, false);
        //    int periThick = WallThicknessCalculator.GetRecommendedThickness(
        //        totalTypicalFloors, WallThicknessCalculator.WallType.PeripheralDeadWall,
        //        seismicZone, 2.0, false);

        //    notes.AppendLine("🧱 ESTIMATED WALL THICKNESSES:");
        //    notes.AppendLine($"   Core Walls: ~{coreThick}mm");
        //    notes.AppendLine($"   Peripheral Walls: ~{periThick}mm");
        //    notes.AppendLine($"   (Actual thickness determined by wall length)\n");

        //    notes.AppendLine("╚════════════════════════════════════════════════════╝");
        //    notes.AppendLine("   Units: All dimensions in METERS (N_m_C)");
        //    notes.AppendLine("╚════════════════════════════════════════════════════╝");

        //    var result = MessageBox.Show(
        //        notes.ToString() + "\n\nProceed with import?",
        //        "⚠️ Confirm Design Parameters",
        //        MessageBoxButtons.YesNo,
        //        MessageBoxIcon.Question);

        //    if (result != DialogResult.Yes)
        //    {
        //        throw new Exception("Import cancelled by user");
        //    }
        //}
        private void ShowDesignNotes(List<FloorTypeConfig> floorConfigs, int totalTypicalFloors,
    string seismicZone, GradeScheduleManager gradeSchedule)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"Zone {seismicZone} | {totalTypicalFloors} typical floors\n");

            foreach (var r in gradeSchedule.GetGradeRanges())
                msg.AppendLine($"F{r.StartFloor:D2}-F{r.EndFloor:D2}: {r.WallGrade}/{r.BeamSlabGrade}");

            msg.AppendLine();
            foreach (var c in floorConfigs)
                msg.AppendLine($"{c.Name} ({c.Count}f): " +
                    $"Gravity {c.BeamDepths["InternalGravity"]}/{c.BeamDepths["CantileverGravity"]}mm | " +
                    $"Main {c.BeamDepths["CoreMain"]}/{c.BeamDepths["PeripheralDeadMain"]}mm | " +
                    $"Slab {c.SlabThicknesses["Lobby"]}/{c.SlabThicknesses["Stair"]}mm");

            int coreThick = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, WallThicknessCalculator.WallType.CoreWall, seismicZone, 2.0, false);
            int periThick = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, seismicZone, 2.0, false);

            msg.AppendLine($"\nWalls: Core ~{coreThick}mm | Peripheral ~{periThick}mm");

            if (MessageBox.Show(msg.ToString() + "\n\nProceed?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                throw new Exception("Import cancelled by user");
        }
        /// <summary>
        /// Show import summary after successful completion
        /// </summary>
        //private void ShowImportSummary(
        //    List<FloorTypeConfig> configs,
        //    int totalStories,
        //    double totalHeight,
        //    int typicalFloors,
        //    string seismicZone,
        //    GradeScheduleManager gradeSchedule)
        //{
        //    StringBuilder summary = new StringBuilder();

        //    summary.AppendLine("╔════════════════════════════════════════════════════╗");
        //    summary.AppendLine("║         IMPORT COMPLETED SUCCESSFULLY              ║");
        //    summary.AppendLine("╚════════════════════════════════════════════════════╝\n");

        //    summary.AppendLine("🏢 BUILDING STRUCTURE:");
        //    summary.AppendLine($"   Total Stories: {totalStories}");
        //    summary.AppendLine($"   Total Height: {totalHeight:F2}m");
        //    summary.AppendLine($"   Seismic Zone: {seismicZone}\n");

        //    summary.AppendLine("📊 FLOOR TYPE BREAKDOWN:");
        //    foreach (var config in configs)
        //    {
        //        double typeHeight = config.Count * config.Height;
        //        summary.AppendLine($"   • {config.Name}: {config.Count} floor(s) × {config.Height:F2}m = {typeHeight:F2}m");
        //    }
        //    summary.AppendLine();

        //    summary.AppendLine("✅ IMPORTED WITH PER-FLOOR CONFIGURATION:");
        //    summary.AppendLine($"   • {configs.Count} different floor types");
        //    summary.AppendLine($"   • Each with unique beam depths");
        //    summary.AppendLine($"   • Each with unique slab thicknesses");
        //    summary.AppendLine($"   • Concrete grades applied per schedule\n");

        //    summary.AppendLine("🏗️ CONCRETE GRADE RANGES:");
        //    foreach (var range in gradeSchedule.GetGradeRanges())
        //    {
        //        summary.AppendLine($"   F{range.StartFloor:D2}-F{range.EndFloor:D2}: " +
        //            $"{range.WallGrade}/{range.BeamSlabGrade}");
        //    }

        //    summary.AppendLine("\n╚════════════════════════════════════════════════════╝");

        //    // Log to debug output
        //    Debug.WriteLine(summary.ToString());

        //    // Note: Don't show MessageBox here - MainForm will show its own success message
        //}

        private void ShowImportSummary(List<FloorTypeConfig> configs, int totalStories, double totalHeight,int typicalfloors,
            string seismicZone, GradeScheduleManager gradeSchedule)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"✅ Import OK | {totalStories} floors | {totalHeight:F2}m | Zone {seismicZone}");

            foreach (var c in configs)
                sb.AppendLine($"  {c.Name}: {c.Count}×{c.Height:F2}m");

            foreach (var r in gradeSchedule.GetGradeRanges())
                sb.AppendLine($"  F{r.StartFloor:D2}-F{r.EndFloor:D2}: {r.WallGrade}/{r.BeamSlabGrade}");

            Debug.WriteLine(sb.ToString());
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
