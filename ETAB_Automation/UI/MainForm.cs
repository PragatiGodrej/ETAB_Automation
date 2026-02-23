

//// ============================================================================
//// END OF FILE
//// ============================================================================
// ============================================================================
// FILE: UI/MainForm.cs
// VERSION: 2.1 — Fixed Podium story name generation
// FIXES:
//   [FIX-1] GenerateStoryName: Podium1/Podium2 individual floors now correctly
//           returned as-is (was falling to default and doubling the digit e.g. "Podium11")
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
                        "• Set per-floor beam and slab specifications\n" +
                        "• Configure foundation walls\n\n" +
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
                using (ImportConfigForm importForm = new ImportConfigForm())
                {
                    if (importForm.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show("Import cancelled by user.", "Cancelled",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // ── Collect from form ────────────────────────────────────
                    var floorConfigs = importForm.FloorConfigs;
                    string seismicZone = importForm.SeismicZone;
                    var wallGrades = importForm.WallGrades;
                    var floorsPerGrade = importForm.FloorsPerGrade;
                    double foundationHeight = importForm.FoundationHeight;

                    // ── Build story lists ────────────────────────────────────
                    var storyHeights = new List<double>();
                    var storyNames = new List<string>();
                    int totalStories = 0;

                    foreach (var config in floorConfigs)
                    {
                        for (int i = 0; i < config.Count; i++)
                        {
                            storyHeights.Add(config.Height);
                            storyNames.Add(GenerateStoryName(config.Name, i, config.Count));
                            totalStories++;
                        }
                    }

                    double totalHeight = storyHeights.Sum();

                    // ── Grade schedule validation ────────────────────────────
                    int gradeTotal = floorsPerGrade.Sum();
                    if (gradeTotal != totalStories)
                    {
                        MessageBox.Show(
                            $"❌ Grade Schedule Validation Failed\n\n" +
                            $"Building has {totalStories} floors\n" +
                            $"Grade schedule covers {gradeTotal} floors\n\n" +
                            $"These must match. Please check your configuration.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // ── Floor config validation ──────────────────────────────
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
                                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // ── Final confirmation ───────────────────────────────────
                    string confirmMsg = BuildConfirmationMessage(
                        floorConfigs, totalStories, totalHeight,
                        seismicZone, wallGrades, floorsPerGrade, foundationHeight);

                    if (MessageBox.Show(confirmMsg, "⚠️ Final Confirmation — Review Before Import",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        MessageBox.Show("Import cancelled.", "Cancelled");
                        return;
                    }

                    // ── Execute import ───────────────────────────────────────
                    System.Diagnostics.Debug.WriteLine("\n========== STARTING CAD IMPORT ==========");
                    System.Diagnostics.Debug.WriteLine($"Total Stories : {totalStories}");
                    System.Diagnostics.Debug.WriteLine($"Total Height  : {totalHeight:F2}m");
                    System.Diagnostics.Debug.WriteLine($"Foundation    : {foundationHeight:F2}m");
                    System.Diagnostics.Debug.WriteLine($"Seismic Zone  : {seismicZone}");

                    // Debug: print story name list so we can verify there are no duplicates
                    System.Diagnostics.Debug.WriteLine("\nStory list (bottom → top):");
                    for (int i = 0; i < storyNames.Count; i++)
                        System.Diagnostics.Debug.WriteLine(
                            $"  [{i}] {storyNames[i]}  h={storyHeights[i]:F2}m");

                    var importer = new CADImporterEnhanced(etabs.SapModel);
                    bool success = importer.ImportMultiFloorTypeCAD(
                        floorConfigs, storyHeights, storyNames,
                        seismicZone, wallGrades, floorsPerGrade, foundationHeight);

                    if (success)
                        ShowSuccessMessage(floorConfigs, totalStories, totalHeight,
                            seismicZone, wallGrades, floorsPerGrade, foundationHeight);
                    else
                        MessageBox.Show(
                            "⚠️ Import Completed with Warnings\n\n" +
                            "Some elements may not have been created.\n\n" +
                            "Please review:\n" +
                            "• ETABS model for missing elements\n" +
                            "• Debug output window for error details\n" +
                            "• Section availability in template",
                            "Import Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ====================================================================
        // EXIT
        // ====================================================================

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you want to exit?\n\nNote: ETABS will remain open.",
                    "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show(
                        "Are you sure you want to exit?\n\nNote: ETABS will remain open.",
                        "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        // ====================================================================
        // GENERATE STORY NAME
        // [FIX-1] Individual Podium floors (Podium1, Podium2…) now returned
        //         as-is, matching how CollectFloorConfigs names them.
        //         Previously they fell to the default case and returned "Podium11".
        // ====================================================================

        private string GenerateStoryName(string floorType, int index, int totalCount)
        {
            // ── Individual basement floors already fully named (e.g. "Basement1") ──
            if (floorType.StartsWith("Basement") && floorType.Length > 8
                && char.IsDigit(floorType[floorType.Length - 1]))
                return floorType;

            // ── [FIX-1] Individual podium floors already fully named (e.g. "Podium1") ──
            if (floorType.StartsWith("Podium") && floorType.Length > 6
                && char.IsDigit(floorType[floorType.Length - 1]))
                return floorType;

            // ── Legacy / shared types ────────────────────────────────────────
            switch (floorType)
            {
                case "Basement": return totalCount > 1 ? $"Basement{index + 1}" : "Basement";
                case "Podium": return totalCount > 1 ? $"Podium{index + 1}" : "Podium";
                case "Ground": return "Ground";
                case "EDeck": return "EDeck";
                case "Typical": return $"Story{index + 1:D2}";
                //case "Refuge": return $"Refuge{index + 1:D2}";   // refuge floors get unique names
                case "Terrace": return "Terrace";
                default: return $"{floorType}{index + 1}";
            }
        }

        // ====================================================================
        // VALIDATION
        // ====================================================================

        private bool ValidateFloorConfig(FloorTypeConfig config)
        {
            if (string.IsNullOrEmpty(config.CADFilePath)) return false;
            if (config.LayerMapping == null || config.LayerMapping.Count == 0) return false;
            if (config.BeamDepths == null || config.BeamDepths.Count == 0) return false;
            if (config.SlabThicknesses == null || config.SlabThicknesses.Count == 0) return false;

            string[] requiredBeamKeys = {
                "InternalGravity", "CantileverGravity", "CoreMain",
                "PeripheralDeadMain", "PeripheralPortalMain", "InternalMain"
            };
            foreach (string key in requiredBeamKeys)
                if (!config.BeamDepths.ContainsKey(key)) return false;

            string[] requiredSlabKeys = { "Lobby", "Stair" };
            foreach (string key in requiredSlabKeys)
                if (!config.SlabThicknesses.ContainsKey(key)) return false;

            return true;
        }

        // ====================================================================
        // CONFIRMATION / SUCCESS MESSAGES
        // ====================================================================

        private string BuildConfirmationMessage(
            List<FloorTypeConfig> floorConfigs, int totalStories,
            double totalHeight, string seismicZone,
            List<string> wallGrades, List<int> floorsPerGrade,
            double foundationHeight)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"{totalStories} floors | {totalHeight:F2}m | Zone {seismicZone}");

            if (foundationHeight > 0)
                msg.AppendLine($"Foundation: {foundationHeight:F2}m (uses Basement1 wall properties)");

            msg.AppendLine();

            int basementCount = floorConfigs.Count(c => c.IsIndividualBasement);
            if (basementCount > 0)
            {
                msg.AppendLine($"Individual Basements: {basementCount}");
                foreach (var c in floorConfigs.Where(f => f.IsIndividualBasement))
                    msg.AppendLine($"  {c.Name}: {c.Height:F2}m");
            }

            foreach (var c in floorConfigs.Where(f => !f.IsIndividualBasement))
                msg.AppendLine($"{c.Name}: {c.Count}×{c.Height:F2}m | " +
                    $"Beams {c.BeamDepths["InternalGravity"]}/{c.BeamDepths["CoreMain"]}mm | " +
                    $"Slab {c.SlabThicknesses["Lobby"]}/{c.SlabThicknesses["Stair"]}mm");

            msg.AppendLine();
            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                msg.AppendLine(
                    $"F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{CalculateBeamSlabGrade(wallGrades[i])}");
                floorStart = floorEnd + 1;
            }

            msg.AppendLine("\nProceed with ETABS import?");
            return msg.ToString();
        }

        private void ShowSuccessMessage(
            List<FloorTypeConfig> floorConfigs, int totalStories,
            double totalHeight, string seismicZone,
            List<string> wallGrades, List<int> floorsPerGrade,
            double foundationHeight)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"✅ {totalStories} floors | {totalHeight:F2}m | Zone {seismicZone}");

            if (foundationHeight > 0)
                msg.AppendLine($"Foundation: {foundationHeight:F2}m walls created");

            msg.AppendLine();
            int floorStart = 1;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                int floorEnd = floorStart + floorsPerGrade[i] - 1;
                msg.AppendLine(
                    $"F{floorStart:D2}-F{floorEnd:D2}: {wallGrades[i]}/{CalculateBeamSlabGrade(wallGrades[i])}");
                floorStart = floorEnd + 1;
            }

            MessageBox.Show(msg.ToString(), "Import Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string CalculateBeamSlabGrade(string wallGrade)
        {
            try
            {
                int v = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());
                int bsv = (int)(Math.Ceiling((v * 0.7) / 5.0) * 5);
                return $"M{Math.Max(bsv, 30)}";
            }
            catch { return "M30"; }
        }
    }
}
// ============================================================================
// END OF FILE
// ============================================================================
