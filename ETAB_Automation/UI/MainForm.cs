
// ============================================================================
// FILE: UI/MainForm.cs (UPDATED FOR PER-FLOOR BEAM/SLAB CONFIGURATION)
// ============================================================================
using ETAB_Automation.Core;
using ETAB_Automation.Importers;
using ETAB_Automation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
                        "• Define concrete grade schedules\n" +
                        "• Set per-floor beam and slab specifications\n\n" +
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
                    $"Stack Trace:\n{ex.StackTrace}",
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
                    // COLLECT CONFIGURATION DATA FROM FORM
                    // ============================================================

                    var floorConfigs = importForm.FloorConfigs;
                    string seismicZone = importForm.SeismicZone;
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
                    // VALIDATE FLOOR CONFIGURATIONS
                    // ============================================================

                    foreach (var config in floorConfigs)
                    {
                        if (!ValidateFloorConfig(config))
                        {
                            MessageBox.Show(
                                $"❌ Invalid configuration for {config.Name}\n\n" +
                                $"Please ensure:\n" +
                                $"• CAD file is selected\n" +
                                $"• Layer mappings are defined\n" +
                                $"• Beam depths are configured\n" +
                                $"• Slab thicknesses are configured",
                                "Validation Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // ============================================================
                    // SHOW FINAL CONFIRMATION
                    // ============================================================

                    string confirmationMsg = BuildConfirmationMessage(
                        floorConfigs, totalStories, totalHeight, seismicZone,
                        wallGrades, floorsPerGrade);

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

                    // Create importer with enhanced per-floor configuration support
                    CADImporterEnhanced importer = new CADImporterEnhanced(etabs.SapModel);

                    bool success = importer.ImportMultiFloorTypeCAD(
                        floorConfigs,
                        storyHeights,
                        storyNames,
                        seismicZone,
                        wallGrades,
                        floorsPerGrade);

                    // ============================================================
                    // SHOW RESULTS
                    // ============================================================

                    if (success)
                    {
                        ShowSuccessMessage(
                            floorConfigs, totalStories, totalHeight, seismicZone,
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
                    return $"story{index + 1:D2}"; // story01, story02, story03, etc.

                case "Terrace":
                    return "Terrace";

                default:
                    return $"{floorType}{index + 1}";
            }
        }

        /// <summary>
        /// Validate that a floor configuration has all required data
        /// </summary>
        private bool ValidateFloorConfig(FloorTypeConfig config)
        {
            // Check CAD file
            if (string.IsNullOrEmpty(config.CADFilePath))
                return false;

            // Check layer mappings
            if (config.LayerMapping == null || config.LayerMapping.Count == 0)
                return false;

            // Check beam depths
            if (config.BeamDepths == null || config.BeamDepths.Count == 0)
                return false;

            // Check slab thicknesses
            if (config.SlabThicknesses == null || config.SlabThicknesses.Count == 0)
                return false;

            // Validate required beam depth keys
            string[] requiredBeamKeys = {
                "InternalGravity", "CantileverGravity", "CoreMain",
                "PeripheralDeadMain", "PeripheralPortalMain", "InternalMain"
            };

            foreach (string key in requiredBeamKeys)
            {
                if (!config.BeamDepths.ContainsKey(key))
                    return false;
            }

            // Validate required slab thickness keys
            string[] requiredSlabKeys = { "Lobby", "Stair" };

            foreach (string key in requiredSlabKeys)
            {
                if (!config.SlabThicknesses.ContainsKey(key))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Build confirmation message with all configuration details
        /// </summary>
        //private string BuildConfirmationMessage(
        //    List<FloorTypeConfig> floorConfigs,
        //    int totalStories,
        //    double totalHeight,
        //    string seismicZone,
        //    List<string> wallGrades,
        //    List<int> floorsPerGrade)
        //{
        //    var msg = new System.Text.StringBuilder();

        //    msg.AppendLine("═══════════════════════════════════════════════");
        //    msg.AppendLine("           IMPORT CONFIGURATION REVIEW");
        //    msg.AppendLine("═══════════════════════════════════════════════\n");

        //    // Building summary
        //    msg.AppendLine("🏢 BUILDING STRUCTURE:");
        //    msg.AppendLine($"   Total Stories: {totalStories}");
        //    msg.AppendLine($"   Total Height: {totalHeight:F2}m");
        //    msg.AppendLine($"   Seismic Zone: {seismicZone}\n");

        //    // Floor breakdown
        //    msg.AppendLine("📊 FLOOR BREAKDOWN:");
        //    foreach (var config in floorConfigs)
        //    {
        //        msg.AppendLine($"   • {config.Name}: {config.Count} floor(s) × {config.Height:F2}m = {config.Count * config.Height:F2}m");
        //    }
        //    msg.AppendLine();

        //    // Per-floor configurations
        //    msg.AppendLine("🔧 PER-FLOOR TYPE CONFIGURATIONS:");
        //    int gravityWidth = (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;

        //    foreach (var config in floorConfigs)
        //    {
        //        msg.AppendLine($"\n   {config.Name}:");

        //        // Beam configuration for this floor
        //        msg.AppendLine($"      Gravity Beams ({gravityWidth}mm width):");
        //        msg.AppendLine($"         Internal: {config.BeamDepths["InternalGravity"]}mm depth");
        //        msg.AppendLine($"         Cantilever: {config.BeamDepths["CantileverGravity"]}mm depth");

        //        msg.AppendLine($"      Main Beams (Seismic):");
        //        msg.AppendLine($"         Core: {config.BeamDepths["CoreMain"]}mm");
        //        msg.AppendLine($"         Peripheral Dead: {config.BeamDepths["PeripheralDeadMain"]}mm");
        //        msg.AppendLine($"         Peripheral Portal: {config.BeamDepths["PeripheralPortalMain"]}mm");
        //        msg.AppendLine($"         Internal: {config.BeamDepths["InternalMain"]}mm");

        //        // Slab configuration for this floor
        //        msg.AppendLine($"      Slabs:");
        //        msg.AppendLine($"         Lobby: {config.SlabThicknesses["Lobby"]}mm");
        //        msg.AppendLine($"         Stair: {config.SlabThicknesses["Stair"]}mm");
        //        msg.AppendLine($"         Regular: 125-250mm (area-based)");
        //    }
        //    msg.AppendLine();

        //    // Concrete grade schedule
        //    msg.AppendLine("🏗️ CONCRETE GRADE SCHEDULE:");
        //    int floorStart = 1;
        //    for (int i = 0; i < wallGrades.Count; i++)
        //    {
        //        int floorEnd = floorStart + floorsPerGrade[i] - 1;
        //        string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);
        //        msg.AppendLine($"   Floors {floorStart:D2}-{floorEnd:D2}: Wall {wallGrades[i]}, Beam/Slab {beamSlabGrade}");
        //        floorStart = floorEnd + 1;
        //    }

        //    msg.AppendLine("\n═══════════════════════════════════════════════");
        //    msg.AppendLine("         Proceed with ETABS import?");
        //    msg.AppendLine("═══════════════════════════════════════════════");

        //    return msg.ToString();
        //}
        private string BuildConfirmationMessage(List<FloorTypeConfig> floorConfigs, int totalStories,
            double totalHeight, string seismicZone, List<string> wallGrades, List<int> floorsPerGrade)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"{totalStories} floors | {totalHeight:F2}m | Zone {seismicZone}\n");

            foreach (var c in floorConfigs)
                msg.AppendLine($"{c.Name}: {c.Count}×{c.Height:F2}m | " +
                    $"Beams {c.BeamDepths["InternalGravity"]}/{c.BeamDepths["CoreMain"]}mm | " +
                    $"Slab {c.SlabThicknesses["Lobby"]}/{c.SlabThicknesses["Stair"]}mm");

            msg.AppendLine();
            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                msg.AppendLine($"F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{CalculateBeamSlabGrade(wallGrades[i])}");
                floorStart = floorEnd + 1;
            }

            msg.AppendLine("\nProceed with ETABS import?");
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
        //private void ShowSuccessMessage(
        //    List<FloorTypeConfig> floorConfigs,
        //    int totalStories,
        //    double totalHeight,
        //    string seismicZone,
        //    List<string> wallGrades,
        //    List<int> floorsPerGrade)
        //{
        //    var msg = new System.Text.StringBuilder();

        //    msg.AppendLine("═══════════════════════════════════════════════");
        //    msg.AppendLine("        ✅ IMPORT COMPLETED SUCCESSFULLY!");
        //    msg.AppendLine("═══════════════════════════════════════════════\n");

        //    msg.AppendLine("🏢 BUILDING STRUCTURE CREATED:");
        //    msg.AppendLine($"   • Total Stories: {totalStories}");
        //    msg.AppendLine($"   • Building Height: {totalHeight:F2}m");
        //    msg.AppendLine($"   • Seismic Zone: {seismicZone}\n");

        //    msg.AppendLine("✅ FLOOR TYPES IMPORTED:");
        //    foreach (var config in floorConfigs)
        //    {
        //        msg.AppendLine($"   • {config.Name}: {config.Count} floor(s) with custom beam/slab config");
        //    }
        //    msg.AppendLine();

        //    msg.AppendLine("🏗️ CONCRETE GRADE SCHEDULE APPLIED:");
        //    int floorStart = 1;
        //    for (int i = 0; i < wallGrades.Count; i++)
        //    {
        //        int floorEnd = floorStart + floorsPerGrade[i] - 1;
        //        string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);
        //        msg.AppendLine($"   F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{beamSlabGrade}");
        //        floorStart = floorEnd + 1;
        //    }

        //    msg.AppendLine("\n📋 NEXT STEPS:");
        //    msg.AppendLine("   1. Check ETABS window for your model");
        //    msg.AppendLine("   2. Use story dropdown to navigate floors");
        //    msg.AppendLine("   3. Review sections and materials");
        //    msg.AppendLine("   4. Verify per-floor beam and slab assignments");
        //    msg.AppendLine("   5. Run analysis when ready\n");

        //    msg.AppendLine("═══════════════════════════════════════════════");

        //    MessageBox.Show(
        //        msg.ToString(),
        //        "Import Success",
        //        MessageBoxButtons.OK,
        //        MessageBoxIcon.Information);
        //}
        private void ShowSuccessMessage(List<FloorTypeConfig> floorConfigs, int totalStories,
    double totalHeight, string seismicZone, List<string> wallGrades, List<int> floorsPerGrade)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"✅ {totalStories} floors | {totalHeight:F2}m | Zone {seismicZone}\n");

            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                msg.AppendLine($"F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{CalculateBeamSlabGrade(wallGrades[i])}");
                floorStart = floorEnd + 1;
            }

            MessageBox.Show(msg.ToString(), "Import Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
