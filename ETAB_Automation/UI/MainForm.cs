
//// ============================================================================
//// FILE: UI/MainForm.cs (WITH GRADE SCHEDULE SUPPORT)
//// ============================================================================
//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;
//using ETAB_Automation.Core;
////using ETABS_Automation.Importers;
////using ETABS_Automation.Models;

//namespace ETAB_Automation
//{
//    public partial class MainForm : Form
//    {
//        private ETABSController etabs;

//        public MainForm()
//        {
//            InitializeComponent();
//            etabs = new ETABSController();
//        }

//        private void btnStartETABS_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                if (etabs.Connect())
//                {
//                    MessageBox.Show(
//                        "ETABS Connected Successfully!\n\nYou can now import CAD files.",
//                        "Success",
//                        MessageBoxButtons.OK,
//                        MessageBoxIcon.Information);
//                }
//                else
//                {
//                    MessageBox.Show(
//                        "ETABS Connection Failed.\n\nPlease ensure:\n" +
//                        "1. ETABS is installed\n" +
//                        "2. ETABS is running\n" +
//                        "3. You have proper permissions",
//                        "Connection Error",
//                        MessageBoxButtons.OK,
//                        MessageBoxIcon.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(
//                    $"Error connecting to ETABS:\n{ex.Message}",
//                    "Error",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//            }
//        }

//        private void btnImportCAD_Click(object sender, EventArgs e)
//        {
//            if (etabs.SapModel == null)
//            {
//                MessageBox.Show(
//                    "Please connect to ETABS first.",
//                    "Not Connected",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Warning);
//                return;
//            }

//            try
//            {
//                using (ImportConfigForm importForm = new ImportConfigForm())
//                {
//                    if (importForm.ShowDialog() == DialogResult.OK)
//                    {
//                        // ⭐ GET ALL CONFIGURATION DATA FROM FORM
//                        var floorConfigs = importForm.FloorConfigs;
//                        string seismicZone = importForm.SeismicZone;
//                        var beamDepths = importForm.BeamDepths;
//                        var slabThicknesses = importForm.SlabThicknesses;
//                        var wallGrades = importForm.WallGrades;           // ⭐ NEW
//                        var floorsPerGrade = importForm.FloorsPerGrade;   // ⭐ NEW

//                        // Calculate total stories and heights
//                        int totalStories = 0;
//                        List<double> storyHeights = new List<double>();
//                        List<string> storyNames = new List<string>();

//                        foreach (var config in floorConfigs)
//                        {
//                            System.Diagnostics.Debug.WriteLine($"DEBUG: {config.Name} height = {config.Height}");

//                            for (int i = 0; i < config.Count; i++)
//                            {
//                                storyHeights.Add(config.Height);

//                                string storyName = "";
//                                if (config.Name == "Basement")
//                                    storyName = $"Basement{i + 1}";
//                                else if (config.Name == "Podium")
//                                    storyName = $"Podium{i + 1}";
//                                else if (config.Name == "EDeck")
//                                    storyName = "EDeck";
//                                else if (config.Name == "Typical")
//                                    storyName = $"F{i + 1:D2}";
//                                else if (config.Name == "Terrace")
//                                    storyName = "Terrace";

//                                storyNames.Add(storyName);
//                                totalStories++;
//                            }
//                        }

//                        double totalHeight = CalculateTotalHeight(storyHeights);

//                        // Build confirmation message
//                        string heightBreakdown = BuildHeightBreakdown(storyHeights, storyNames);
//                        string beamConfig = BuildBeamConfigSummary(seismicZone, beamDepths);
//                        string slabConfig = BuildSlabConfigSummary(slabThicknesses);
//                        //string gradeConfig = BuildGradeSummary(wallGrades, floorsPerGrade); // ⭐ NEW

//                        //var result = MessageBox.Show(
//                        //    $"Final Import Configuration:\n\n" +
//                        //    $"Total Stories: {totalStories}\n" +
//                        //    $"Total Building Height: {totalHeight:F2}m\n" +
//                        //    $"Seismic Zone: {seismicZone}\n\n" +
//                        //    heightBreakdown + "\n" +
//                        //    //beamConfig + "\n" +
//                        //    //slabConfig + "\n" +
//                        //    gradeConfig + "\n" +  // ⭐ NEW
//                        //    "Proceed with import?",
//                        //    "⚠️ Final Confirmation",
//                        //    MessageBoxButtons.YesNo,
//                        //    MessageBoxIcon.Question);

//                        //if (result != DialogResult.Yes)
//                        //{
//                        //    MessageBox.Show("Import cancelled.", "Cancelled");
//                        //    return;
//                        //}

//                        //// ⭐ IMPORT WITH GRADE SCHEDULE
//                        //CADImporterEnhanced importer = new CADImporterEnhanced(etabs.SapModel);
//                        //bool success = importer.ImportMultiFloorTypeCAD(
//                        //    floorConfigs,
//                        //    storyHeights,
//                        //    storyNames,
//                        //    seismicZone,
//                        //    beamDepths,
//                        //    slabThicknesses,
//                        //    wallGrades,      // ⭐ PASS WALL GRADES
//                        //    floorsPerGrade); // ⭐ PASS FLOOR COUNTS

//                        //if (success)
//                        //{
//                        //    MessageBox.Show(
//                        //        "✅ Import completed successfully!\n\n" +
//                        //        "Building Structure Created:\n" +
//                        //        $"- Total Stories: {totalStories}\n" +
//                        //        $"- Building Height: {totalHeight:F2}m\n" +
//                        //        $"- Seismic Zone: {seismicZone}\n\n" +
//                        //        "Beam Configuration Applied:\n" +
//                        //        //BuildBeamConfigSummary(seismicZone, beamDepths) + "\n\n" +
//                        //        //"Slab Configuration Applied:\n" +
//                        //        //BuildSlabConfigSummary(slabThicknesses) + "\n\n" +
//                        //        //"Concrete Grade Schedule Applied:\n" +  // ⭐ NEW
//                        //        BuildGradeSummary(wallGrades, floorsPerGrade) + "\n\n" +
//                        //        "View Your Building:\n" +
//                        //        "1. Check bottom of ETABS window\n" +
//                        //        "2. Use story dropdown to navigate floors\n" +
//                        //        "3. Each floor type has its own unique layout",
//                        //        "Import Success!",
//                        //        MessageBoxButtons.OK,
//                        //        MessageBoxIcon.Information);
//                        //}
//                        //else
//                        //{
//                        //    MessageBox.Show(
//                        //        "Import completed but some elements may not have been created.\n" +
//                        //        "Please review the ETABS model.",
//                        //        "Warning",
//                        //        MessageBoxButtons.OK,
//                        //        MessageBoxIcon.Warning);
//                        //}
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(
//                    $"Error during import:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
//                    "Import Error",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//            }
//        }

//        private void btnExit_Click(object sender, EventArgs e)
//        {
//            var result = MessageBox.Show(
//                "Are you sure you want to exit?\n\nNote: ETABS will remain open.",
//                "Confirm Exit",
//                MessageBoxButtons.YesNo,
//                MessageBoxIcon.Question);

//            if (result == DialogResult.Yes)
//            {
//                Application.Exit();
//            }
//        }

//        protected override void OnFormClosing(FormClosingEventArgs e)
//        {
//            // Only show confirmation if user is closing (not programmatic close)
//            if (e.CloseReason == CloseReason.UserClosing)
//            {
//                var result = MessageBox.Show(
//                    "Are you sure you want to exit?\n\nNote: ETABS will remain open.",
//                    "Confirm Exit",
//                    MessageBoxButtons.YesNo,
//                    MessageBoxIcon.Question);

//                if (result != DialogResult.Yes)
//                {
//                    e.Cancel = true;
//                    return;
//                }
//            }

//            base.OnFormClosing(e);
//        }

//        // ====================================================================
//        // HELPER METHODS
//        // ====================================================================

//        private string BuildSlabConfigSummary(Dictionary<string, int> slabThicknesses)
//        {
//            string summary = "Slab Configuration:\n";
//            summary += $"  - Lobby: {slabThicknesses["Lobby"]}mm\n";
//            summary += $"  - Stair: {slabThicknesses["Stair"]}mm\n";
//            summary += $"  - Regular: 125-250mm (area-based)\n";
//            summary += $"  - Cantilever: 125-200mm (span-based)\n";

//            return summary;
//        }

//        private string BuildHeightBreakdown(List<double> storyHeights, List<string> storyNames)
//        {
//            string breakdown = "Story Height Breakdown:\n";
//            double cumulativeHeight = 0;

//            for (int i = 0; i < storyHeights.Count; i++)
//            {
//                cumulativeHeight += storyHeights[i];
//                breakdown += $"{storyNames[i]}: {storyHeights[i]:F2}m (Elevation: {cumulativeHeight:F2}m)\n";
//            }

//            return breakdown;
//        }

//        private string BuildBeamConfigSummary(string seismicZone, Dictionary<string, int> beamDepths)
//        {
//            int gravityWidth = (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;

//            string summary = "Beam Configuration:\n";
//            summary += $"Gravity Beams (Width: {gravityWidth}mm):\n";
//            summary += $"  - Internal Gravity: {gravityWidth}x{beamDepths["InternalGravity"]}mm\n";
//            summary += $"  - Cantilever Gravity: {gravityWidth}x{beamDepths["CantileverGravity"]}mm\n";
//            summary += $"Main Beams (Width: matches wall):\n";
//            summary += $"  - Core Main: {beamDepths["CoreMain"]}mm depth\n";
//            summary += $"  - Peripheral Dead Main: {beamDepths["PeripheralDeadMain"]}mm depth\n";
//            summary += $"  - Peripheral Portal Main: {beamDepths["PeripheralPortalMain"]}mm depth\n";
//            summary += $"  - Internal Main: {beamDepths["InternalMain"]}mm depth\n";

//            return summary;
//        }

//        //// ⭐ NEW: Build grade schedule summary
//        //private string BuildGradeSummary(List<string> wallGrades, List<int> floorsPerGrade)
//        //{
//        //    if (wallGrades == null || floorsPerGrade == null || wallGrades.Count == 0)
//        //        return "Concrete Grade Configuration:\n  - Default grades applied\n";

//        //    string summary = "Concrete Grade Configuration:\n";
//        //    int floorStart = 1;

//        //    for (int i = 0; i < wallGrades.Count; i++)
//        //    {
//        //        int floorEnd = floorStart + floorsPerGrade[i] - 1;
//        //        string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);

//        //        summary += $"  - Floors {floorStart}-{floorEnd}: Wall {wallGrades[i]}, Beam/Slab {beamSlabGrade}\n";
//        //        floorStart = floorEnd + 1;
//        //    }

//        //    return summary;
//        //}

//        //// ⭐ NEW: Calculate beam/slab grade from wall grade
//        //private string CalculateBeamSlabGrade(string wallGrade)
//        //{
//        //    try
//        //    {
//        //        // Extract numeric value (e.g., "M50" → 50)
//        //        int wallValue = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());

//        //        // Calculate 0.7x and round to nearest 5
//        //        int beamSlabValue = (int)(Math.Round((wallValue * 0.7) / 5.0) * 5);

//        //        // Minimum M20
//        //        if (beamSlabValue < 20)
//        //            beamSlabValue = 20;

//        //        return $"M{beamSlabValue}";
//        //    }
//        //    catch
//        //    {
//        //        return "M30"; // Fallback
//        //    }
//        //}

//        private double CalculateTotalHeight(List<double> storyHeights)
//        {
//            double total = 0;
//            foreach (double height in storyHeights)
//            {
//                total += height;
//            }
//            return total;
//        }
//    }
//}
// ============================================================================
// FILE: UI/MainForm.cs (UPDATED WITH GRADE SCHEDULE INTEGRATION)
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ETAB_Automation.Core;
using ETAB_Automation.Importers;
using ETAB_Automation.Models;

namespace ETAB_Automation
{
    public partial class MainForm : Form
    {
        private ETABSController etabs;

        public MainForm()
        {
            InitializeComponent();
            etabs = new ETABSController();
        }

        // ====================================================================
        // ETABS CONNECTION
        // ====================================================================

        private void btnStartETABS_Click(object sender, EventArgs e)
        {
            try
            {
                if (etabs.Connect())
                {
                    MessageBox.Show(
                        "✅ ETABS Connected Successfully!\n\n" +
                        "You can now:\n" +
                        "• Import CAD files\n" +
                        "• Configure building parameters\n" +
                        "• Define concrete grade schedules\n\n" +
                        "Click 'Import CAD Files' to begin.",
                        "Connection Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "❌ ETABS Connection Failed\n\n" +
                        "Please ensure:\n" +
                        "1. ETABS is installed on your system\n" +
                        "2. ETABS is currently running\n" +
                        "3. You have proper administrator permissions\n" +
                        "4. Template file is accessible\n\n" +
                        "Try starting ETABS manually first, then connect.",
                        "Connection Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error connecting to ETABS:\n\n{ex.Message}\n\n" +
                    "Stack Trace:\n{ex.StackTrace}",
                    "Connection Exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        // CAD IMPORT
        // ====================================================================

        private void btnImportCAD_Click(object sender, EventArgs e)
        {
            // Check ETABS connection
            if (etabs.SapModel == null)
            {
                MessageBox.Show(
                    "⚠️ Not Connected to ETABS\n\n" +
                    "Please click 'Start ETABS' button first to establish connection.",
                    "Not Connected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Show import configuration form
                using (ImportConfigForm importForm = new ImportConfigForm())
                {
                    if (importForm.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show(
                            "Import cancelled by user.",
                            "Cancelled",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    // ============================================================
                    // COLLECT CONFIGURATION DATA
                    // ============================================================

                    var floorConfigs = importForm.FloorConfigs;
                    string seismicZone = importForm.SeismicZone;
                    var beamDepths = importForm.BeamDepths;
                    var slabThicknesses = importForm.SlabThicknesses;
                    var wallGrades = importForm.WallGrades;
                    var floorsPerGrade = importForm.FloorsPerGrade;

                    // ============================================================
                    // PREPARE STORY DATA
                    // ============================================================

                    List<double> storyHeights = new List<double>();
                    List<string> storyNames = new List<string>();
                    int totalStories = 0;

                    foreach (var config in floorConfigs)
                    {
                        for (int i = 0; i < config.Count; i++)
                        {
                            storyHeights.Add(config.Height);

                            // Generate story names based on floor type
                            string storyName = GenerateStoryName(config.Name, i, config.Count);
                            storyNames.Add(storyName);
                            totalStories++;
                        }
                    }

                    double totalHeight = storyHeights.Sum();

                    // ============================================================
                    // VALIDATE GRADE SCHEDULE
                    // ============================================================

                    int gradeScheduleTotal = floorsPerGrade.Sum();
                    if (gradeScheduleTotal != totalStories)
                    {
                        MessageBox.Show(
                            $"❌ Grade Schedule Validation Failed\n\n" +
                            $"Building has {totalStories} floors\n" +
                            $"Grade schedule covers {gradeScheduleTotal} floors\n\n" +
                            $"These must match. Please check your configuration.",
                            "Validation Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // ============================================================
                    // SHOW FINAL CONFIRMATION
                    // ============================================================

                    string confirmationMsg = BuildConfirmationMessage(
                        totalStories, totalHeight, seismicZone,
                        beamDepths, slabThicknesses,
                        wallGrades, floorsPerGrade, floorConfigs);

                    var confirmResult = MessageBox.Show(
                        confirmationMsg,
                        "⚠️ Final Confirmation - Review Before Import",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmResult != DialogResult.Yes)
                    {
                        MessageBox.Show("Import cancelled.", "Cancelled");
                        return;
                    }

                    // ============================================================
                    // EXECUTE IMPORT
                    // ============================================================

                    System.Diagnostics.Debug.WriteLine("\n========== STARTING CAD IMPORT ==========");
                    System.Diagnostics.Debug.WriteLine($"Total Stories: {totalStories}");
                    System.Diagnostics.Debug.WriteLine($"Total Height: {totalHeight:F2}m");
                    System.Diagnostics.Debug.WriteLine($"Seismic Zone: {seismicZone}");

                    // Create importer with grade schedule support
                    CADImporterEnhanced importer = new CADImporterEnhanced(etabs.SapModel);

                    bool success = importer.ImportMultiFloorTypeCAD(
                        floorConfigs,
                        storyHeights,
                        storyNames,
                        seismicZone,
                        beamDepths,
                        slabThicknesses,
                        wallGrades,
                        floorsPerGrade);

                    // ============================================================
                    // SHOW RESULTS
                    // ============================================================

                    if (success)
                    {
                        ShowSuccessMessage(
                            totalStories, totalHeight, seismicZone,
                            beamDepths, slabThicknesses,
                            wallGrades, floorsPerGrade);
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠️ Import Completed with Warnings\n\n" +
                            "The import process finished, but some elements may not have been created.\n\n" +
                            "Please review:\n" +
                            "• ETABS model for missing elements\n" +
                            "• Debug output window for error details\n" +
                            "• Section availability in template",
                            "Import Warning",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error during import:\n\n{ex.Message}\n\n" +
                    $"Stack Trace:\n{ex.StackTrace}\n\n" +
                    "Please check:\n" +
                    "1. All CAD files are valid DXF format\n" +
                    "2. Layer mappings are correct\n" +
                    "3. ETABS template has required sections\n" +
                    "4. Concrete materials are defined in template",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        // EXIT
        // ====================================================================

        private void btnExit_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit?\n\n" +
                "Note: ETABS will remain open with your imported model.",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to exit?\n\n" +
                    "Note: ETABS will remain open with your imported model.",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        /// <summary>
        /// Generate appropriate story name based on floor type
        /// </summary>
        private string GenerateStoryName(string floorType, int index, int totalCount)
        {
            switch (floorType)
            {
                case "Basement":
                    return totalCount > 1 ? $"Basement{index + 1}" : "Basement";

                case "Podium":
                    return totalCount > 1 ? $"Podium{index + 1}" : "Podium";

                case "EDeck":
                    return "EDeck";

                case "Typical":
                    return $"story{index + 1:D2}"; // F01, F02, F03, etc.

                case "Terrace":
                    return "Terrace";

                default:
                    return $"{floorType}{index + 1}";
            }
        }

        /// <summary>
        /// Build confirmation message with all configuration details
        /// </summary>
        private string BuildConfirmationMessage(
            int totalStories, double totalHeight, string seismicZone,
            Dictionary<string, int> beamDepths, Dictionary<string, int> slabThicknesses,
            List<string> wallGrades, List<int> floorsPerGrade,
            List<FloorTypeConfig> floorConfigs)
        {
            var msg = new System.Text.StringBuilder();

            msg.AppendLine("═══════════════════════════════════════════════");
            msg.AppendLine("           IMPORT CONFIGURATION REVIEW");
            msg.AppendLine("═══════════════════════════════════════════════\n");

            // Building summary
            msg.AppendLine("🏢 BUILDING STRUCTURE:");
            msg.AppendLine($"   Total Stories: {totalStories}");
            msg.AppendLine($"   Total Height: {totalHeight:F2}m");
            msg.AppendLine($"   Seismic Zone: {seismicZone}\n");

            // Floor breakdown
            msg.AppendLine("📊 FLOOR BREAKDOWN:");
            foreach (var config in floorConfigs)
            {
                msg.AppendLine($"   • {config.Name}: {config.Count} floor(s) × {config.Height:F2}m = {config.Count * config.Height:F2}m");
            }
            msg.AppendLine();

            // Beam configuration
            int gravityWidth = (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;
            msg.AppendLine("🔨 BEAM CONFIGURATION:");
            msg.AppendLine($"   Gravity Beams (Width: {gravityWidth}mm):");
            msg.AppendLine($"      • Internal: {gravityWidth}×{beamDepths["InternalGravity"]}mm");
            msg.AppendLine($"      • Cantilever: {gravityWidth}×{beamDepths["CantileverGravity"]}mm");
            msg.AppendLine($"   Main Beams (Seismic):");
            msg.AppendLine($"      • Core: {beamDepths["CoreMain"]}mm depth");
            msg.AppendLine($"      • Peripheral: {beamDepths["PeripheralDeadMain"]}mm depth\n");

            // Slab configuration
            msg.AppendLine("📐 SLAB CONFIGURATION:");
            msg.AppendLine($"   • Lobby: {slabThicknesses["Lobby"]}mm");
            msg.AppendLine($"   • Stair: {slabThicknesses["Stair"]}mm");
            msg.AppendLine($"   • Regular: 125-250mm (area-based)\n");

            // Concrete grade schedule
            msg.AppendLine("🏗️ CONCRETE GRADE SCHEDULE:");
            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);
                msg.AppendLine($"   Floors {floorStart:D2}-{floorEnd:D2}: Wall {wallGrades[i]}, Beam/Slab {beamSlabGrade}");
                floorStart = floorEnd + 1;
            }

            msg.AppendLine("\n═══════════════════════════════════════════════");
            msg.AppendLine("         Proceed with ETABS import?");
            msg.AppendLine("═══════════════════════════════════════════════");

            return msg.ToString();
        }

        /// <summary>
        /// Calculate beam/slab grade from wall grade (0.7× formula)
        /// </summary>
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            try
            {
                int wallValue = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());
                int beamSlabValue = (int)(Math.Ceiling((wallValue * 0.7) / 5.0) * 5);
                if (beamSlabValue < 30) beamSlabValue = 30;
                return $"M{beamSlabValue}";
            }
            catch
            {
                return "M30";
            }
        }

        /// <summary>
        /// Show success message with import summary
        /// </summary>
        private void ShowSuccessMessage(
            int totalStories, double totalHeight, string seismicZone,
            Dictionary<string, int> beamDepths, Dictionary<string, int> slabThicknesses,
            List<string> wallGrades, List<int> floorsPerGrade)
        {
            var msg = new System.Text.StringBuilder();

            msg.AppendLine("═══════════════════════════════════════════════");
            msg.AppendLine("        ✅ IMPORT COMPLETED SUCCESSFULLY!");
            msg.AppendLine("═══════════════════════════════════════════════\n");

            msg.AppendLine("🏢 BUILDING STRUCTURE CREATED:");
            msg.AppendLine($"   • Total Stories: {totalStories}");
            msg.AppendLine($"   • Building Height: {totalHeight:F2}m");
            msg.AppendLine($"   • Seismic Zone: {seismicZone}\n");

            msg.AppendLine("✅ STRUCTURAL ELEMENTS IMPORTED:");
            msg.AppendLine("   • Walls with auto-thickness calculation");
            msg.AppendLine("   • Beams with zone-based sizing");
            msg.AppendLine("   • Slabs with area/span-based rules\n");

            msg.AppendLine("🏗️ CONCRETE GRADE SCHEDULE APPLIED:");
            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);
                msg.AppendLine($"   F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{beamSlabGrade}");
                floorStart = floorEnd + 1;
            }

            msg.AppendLine("\n📋 NEXT STEPS:");
            msg.AppendLine("   1. Check ETABS window for your model");
            msg.AppendLine("   2. Use story dropdown to navigate floors");
            msg.AppendLine("   3. Review sections and materials");
            msg.AppendLine("   4. Run analysis when ready\n");

            msg.AppendLine("═══════════════════════════════════════════════");

            MessageBox.Show(
                msg.ToString(),
                "Import Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
