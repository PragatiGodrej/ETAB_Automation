//// ============================================================================
//// FILE: UI/ImportConfigForm.cs (PART 1 - Main Form)
//// ============================================================================
//// PURPOSE: Main configuration form class with core logic
//// AUTHOR: ETAB Automation Team
//// VERSION: 2.1 (Split into 2 parts)
//// ============================================================================

//using ETAB_Automation.Importers;
//using ETAB_Automation.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows.Forms;

//namespace ETAB_Automation
//{
//    /// <summary>
//    /// Main configuration form for importing CAD files
//    /// Part 1: Core logic, validation, and event handlers
//    /// Part 2: UI initialization (ImportConfigFormUI.cs)
//    /// </summary>
//    public partial class ImportConfigForm : Form
//    {
//        // ====================================================================
//        // PUBLIC PROPERTIES
//        // ====================================================================

//        public List<FloorTypeConfig> FloorConfigs { get; private set; }
//        public string SeismicZone { get; private set; }
//        public List<string> WallGrades { get; private set; }
//        public List<int> FloorsPerGrade { get; private set; }

//        // ====================================================================
//        // UI CONTROLS (declared here, initialized in Part 2)
//        // ====================================================================

//        internal TabControl tabControl;
//        internal Button btnImport;
//        internal Button btnCancel;

//        // Building config
//        internal CheckBox chkBasement;
//        internal CheckBox chkPodium;
//        internal CheckBox chkEDeck; // Always enabled, but can be toggled for import
//        internal CheckBox chkTypical; // Always enabled, but can be toggled for import

//        internal CheckBox chkTerrace;
//        internal CheckBox chkFoundation;
//        internal NumericUpDown numBasementLevels;
//        internal NumericUpDown numPodiumLevels;
//        internal NumericUpDown numTypicalLevels;
//        internal NumericUpDown numBasementHeight;
//        internal NumericUpDown numPodiumHeight;
//        internal NumericUpDown numEDeckHeight;
//        internal NumericUpDown numTypicalHeight;
//        internal NumericUpDown numTerraceheight;
//        internal NumericUpDown numFoundationHeight;
//        internal ComboBox cmbSeismicZone;

//        // Grade schedule
//        internal DataGridView dgvGradeSchedule;
//        internal NumericUpDown numTotalFloors;
//        internal Button btnAddGradeRow;
//        internal Button btnRemoveGradeRow;
//        internal Label lblGradeTotal;

//        // CAD Import (dynamic per floor type)
//        internal Dictionary<string, TextBox> cadPathTextBoxes;
//        internal Dictionary<string, ListBox> availableLayerListBoxes;
//        internal Dictionary<string, ListBox> mappedLayerListBoxes;
//        internal Dictionary<string, ComboBox> elementTypeComboBoxes;

//        // Per-floor beam depths
//        internal Dictionary<string, NumericUpDown> numInternalGravityDepthPerFloor;
//        internal Dictionary<string, NumericUpDown> numCantileverGravityDepthPerFloor;
//        internal Dictionary<string, NumericUpDown> numCoreMainDepthPerFloor;
//        internal Dictionary<string, NumericUpDown> numPeripheralDeadMainDepthPerFloor;
//        internal Dictionary<string, NumericUpDown> numPeripheralPortalMainDepthPerFloor;
//        internal Dictionary<string, NumericUpDown> numInternalMainDepthPerFloor;

//        // Per-floor slab thicknesses
//        internal Dictionary<string, NumericUpDown> numLobbySlabThicknessPerFloor;
//        internal Dictionary<string, NumericUpDown> numStairSlabThicknessPerFloor;

//        // ====================================================================
//        // CONSTRUCTOR
//        // ====================================================================

//        public ImportConfigForm()
//        {
//            InitializeComponent();

//            // Initialize data collections
//            FloorConfigs = new List<FloorTypeConfig>();
//            WallGrades = new List<string>();
//            FloorsPerGrade = new List<int>();

//            // Initialize control dictionaries
//            cadPathTextBoxes = new Dictionary<string, TextBox>();
//            availableLayerListBoxes = new Dictionary<string, ListBox>();
//            mappedLayerListBoxes = new Dictionary<string, ListBox>();
//            elementTypeComboBoxes = new Dictionary<string, ComboBox>();
//            numInternalGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numCantileverGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numCoreMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numPeripheralDeadMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numPeripheralPortalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numInternalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
//            numLobbySlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();
//            numStairSlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();

//            // Build UI (calls Part 2)
//            InitializeControlsUI();
//        }

//        // ====================================================================
//        // CAD FILE LOADING
//        // ====================================================================

//        internal void BtnLoadCAD_Click(string floorType)
//        {
//            OpenFileDialog ofd = new OpenFileDialog
//            {
//                Filter = "AutoCAD Files (*.dxf;*.dwg)|*.dxf;*.dwg|DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
//                Title = $"Select CAD File for {floorType}"
//            };

//            if (ofd.ShowDialog() != DialogResult.OK) return;

//            cadPathTextBoxes[floorType].Text = ofd.FileName;
//            string extension = System.IO.Path.GetExtension(ofd.FileName).ToLower();

//            if (extension == ".dwg")
//            {
//                MessageBox.Show(
//                    "DWG files are not directly supported.\n\n" +
//                    "Please convert to DXF format:\n" +
//                    "1. Open in AutoCAD\n" +
//                    "2. Save As → DXF\n" +
//                    "3. Load the DXF file here",
//                    "DWG Not Supported",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Warning);
//                return;
//            }

//            if (extension != ".dxf")
//            {
//                MessageBox.Show(
//                    "Please select a DXF file.\n\n" +
//                    "Supported format: .dxf",
//                    "Invalid File Type",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Warning);
//                return;
//            }

//            try
//            {
//                CADLayerReader reader = new CADLayerReader();
//                List<string> layers = reader.GetLayerNamesFromFile(ofd.FileName);

//                if (layers.Count == 0)
//                {
//                    MessageBox.Show(
//                        "No layers found in CAD file.",
//                        "No Layers Found",
//                        MessageBoxButtons.OK,
//                        MessageBoxIcon.Warning);
//                    return;
//                }

//                availableLayerListBoxes[floorType].Items.Clear();
//                foreach (string layer in layers)
//                {
//                    availableLayerListBoxes[floorType].Items.Add(layer);
//                }

//                AutoMapLayers(floorType, layers);

//                MessageBox.Show(
//                    $"✓ CAD file loaded!\n\n" +
//                    $"Layers: {layers.Count}\n\n" +
//                    "Auto-mapped based on naming conventions.",
//                    "Layers Loaded",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(
//                    $"Error reading CAD file:\n\n{ex.Message}",
//                    "CAD Read Error",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//            }
//        }

//        private void AutoMapLayers(string floorType, List<string> layers)
//        {
//            mappedLayerListBoxes[floorType].Items.Clear();

//            foreach (string layer in layers)
//            {
//                string elementType = null;

//                if (layer.StartsWith("B-") || layer.Contains("Beam") ||
//                    layer.Contains("BEAM") || layer.Contains("beam"))
//                    elementType = "Beam";
//                else if (layer.Contains("wall") || layer.Contains("Wall") ||
//                         layer.Contains("WALL"))
//                    elementType = "Wall";
//                else if (layer.StartsWith("S-") || layer.Contains("Slab") ||
//                         layer.Contains("SLAB"))
//                    elementType = "Slab";

//                if (elementType != null)
//                {
//                    mappedLayerListBoxes[floorType].Items.Add($"{layer} → {elementType}");
//                }
//            }
//        }

//        internal void BtnAddMapping_Click(string floorType)
//        {
//            if (availableLayerListBoxes[floorType].SelectedItem == null)
//            {
//                MessageBox.Show("Please select a layer to map.", "Info");
//                return;
//            }

//            string layerName = availableLayerListBoxes[floorType].SelectedItem.ToString();
//            string elementType = elementTypeComboBoxes[floorType].SelectedItem.ToString();

//            if (elementType == "Ignore") return;

//            string mapping = $"{layerName} → {elementType}";
//            if (!mappedLayerListBoxes[floorType].Items.Contains(mapping))
//            {
//                mappedLayerListBoxes[floorType].Items.Add(mapping);
//            }
//            else
//            {
//                MessageBox.Show("Layer already mapped.", "Info");
//            }
//        }

//        internal void BtnRemoveMapping_Click(string floorType)
//        {
//            if (mappedLayerListBoxes[floorType].SelectedItem == null)
//            {
//                MessageBox.Show("Please select a mapping to remove.", "Info");
//                return;
//            }

//            mappedLayerListBoxes[floorType].Items.Remove(
//                mappedLayerListBoxes[floorType].SelectedItem);
//        }

//        // ====================================================================
//        // GRADE SCHEDULE HANDLERS
//        // ====================================================================

//        private void BtnAddGradeRow_Click(object sender, EventArgs e)
//        {
//            int rowIndex = dgvGradeSchedule.Rows.Add();
//            var row = dgvGradeSchedule.Rows[rowIndex];

//            row.Cells["Index"].Value = rowIndex;
//            row.Cells["WallGrade"].Value = "M40";
//            row.Cells["FloorsCount"].Value = "1";
//            row.Cells["BeamSlabGrade"].Value = "M30";
//            row.Cells["FloorRange"].Value = "";

//            UpdateGradeTotals();
//        }

//        private void BtnRemoveGradeRow_Click(object sender, EventArgs e)
//        {
//            if (dgvGradeSchedule.SelectedRows.Count > 0)
//            {
//                dgvGradeSchedule.Rows.RemoveAt(dgvGradeSchedule.SelectedRows[0].Index);
//                ReindexRows();
//                UpdateGradeTotals();
//            }
//        }

//        private void DgvGradeSchedule_CellValueChanged(object sender, DataGridViewCellEventArgs e)
//        {
//            if (e.RowIndex < 0) return;

//            var row = dgvGradeSchedule.Rows[e.RowIndex];

//            if (e.ColumnIndex == dgvGradeSchedule.Columns["WallGrade"].Index)
//            {
//                string wallGrade = row.Cells["WallGrade"].Value?.ToString();
//                if (!string.IsNullOrEmpty(wallGrade))
//                {
//                    row.Cells["BeamSlabGrade"].Value = CalculateBeamSlabGrade(wallGrade);
//                }
//            }

//            if (e.ColumnIndex == dgvGradeSchedule.Columns["FloorsCount"].Index)
//            {
//                UpdateGradeTotals();
//            }
//        }

//        private string CalculateBeamSlabGrade(string wallGrade)
//        {
//            try
//            {
//                int wallValue = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());
//                int beamSlabValue = (int)(Math.Ceiling((wallValue * 0.7) / 5.0) * 5);
//                if (beamSlabValue < 30) beamSlabValue = 30;
//                return $"M{beamSlabValue}";
//            }
//            catch
//            {
//                return "M30";
//            }
//        }

//        private void ReindexRows()
//        {
//            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
//            {
//                dgvGradeSchedule.Rows[i].Cells["Index"].Value = i;
//            }
//            UpdateFloorRanges();
//        }

//        private void UpdateFloorRanges()
//        {
//            int currentFloor = 1;

//            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
//            {
//                var row = dgvGradeSchedule.Rows[i];
//                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();

//                if (int.TryParse(floorsStr, out int floorCount) && floorCount > 0)
//                {
//                    int endFloor = currentFloor + floorCount - 1;
//                    row.Cells["FloorRange"].Value = $"{currentFloor}-{endFloor}";
//                    currentFloor = endFloor + 1;
//                }
//                else
//                {
//                    row.Cells["FloorRange"].Value = "";
//                }
//            }
//        }

//        internal void UpdateGradeTotals()
//        {
//            int totalInSchedule = 0;

//            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
//            {
//                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();
//                if (int.TryParse(floorsStr, out int floors))
//                {
//                    totalInSchedule += floors;
//                }
//            }

//            int requiredTotal = (int)numTotalFloors.Value;
//            bool isValid = totalInSchedule == requiredTotal;

//            lblGradeTotal.Text = $"Total floors in schedule: {totalInSchedule} / {requiredTotal}";
//            lblGradeTotal.ForeColor = isValid ?
//                System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed;

//            if (isValid)
//                lblGradeTotal.Text += " ✓ VALID";
//            else if (totalInSchedule > requiredTotal)
//                lblGradeTotal.Text += " ❌ TOO MANY";
//            else
//                lblGradeTotal.Text += " ❌ TOO FEW";

//            UpdateFloorRanges();
//        }

//        internal void UpdateTotalFloorsForGradeSchedule()
//        {
//            int total = 0;

//            if (chkBasement.Checked)
//                total += (int)numBasementLevels.Value;
//            if (chkPodium.Checked)
//                total += (int)numPodiumLevels.Value;

//            total += 1; // E-Deck
//            total += (int)numTypicalLevels.Value;

//            if (chkTerrace.Checked)
//                total += 1;

//            numTotalFloors.Value = total;
//            UpdateGradeTotals();
//        }

//        // ====================================================================
//        // BUILDING CONFIG HANDLERS
//        // ====================================================================

//        private void ChkBasement_CheckedChanged(object sender, EventArgs e)
//        {
//            numBasementLevels.Enabled = chkBasement.Checked;
//            numBasementHeight.Enabled = chkBasement.Checked;
//        }

//        private void ChkTerrace_CheckedChanged(object sender, EventArgs e)
//        {
//            numTerraceheight.Enabled = chkTerrace.Checked;
//        }

//        private void ChkPodium_CheckedChanged(object sender, EventArgs e)
//        {
//            numPodiumLevels.Enabled = chkPodium.Checked;
//            numPodiumHeight.Enabled = chkPodium.Checked;
//        }

//        // ====================================================================
//        // IMPORT VALIDATION AND EXECUTION
//        // ====================================================================

//        private void BtnImport_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                // Validate grade schedule
//                if (!ValidateGradeSchedule())
//                    return;

//                // Collect floor configs
//                if (!CollectFloorConfigs())
//                    return;

//                SeismicZone = cmbSeismicZone.SelectedItem?.ToString() ?? "Zone IV";

//                ShowConfirmation();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(
//                    $"Error preparing import:\n\n{ex.Message}",
//                    "Import Error",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//            }
//        }

//        private bool ValidateGradeSchedule()
//        {
//            if (dgvGradeSchedule.Rows.Count == 0)
//            {
//                MessageBox.Show(
//                    "No concrete grades defined!\n\n" +
//                    "Please add at least one grade row.",
//                    "Grade Schedule Empty",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//                tabControl.SelectedIndex = 1;
//                return false;
//            }

//            WallGrades.Clear();
//            FloorsPerGrade.Clear();

//            int totalInSchedule = 0;
//            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
//            {
//                string wallGrade = row.Cells["WallGrade"].Value?.ToString();
//                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();

//                if (string.IsNullOrEmpty(wallGrade) || !int.TryParse(floorsStr, out int floors))
//                {
//                    MessageBox.Show(
//                        $"Invalid grade schedule at row {row.Index}.",
//                        "Validation Error",
//                        MessageBoxButtons.OK,
//                        MessageBoxIcon.Error);
//                    tabControl.SelectedIndex = 1;
//                    return false;
//                }

//                WallGrades.Add(wallGrade);
//                FloorsPerGrade.Add(floors);
//                totalInSchedule += floors;
//            }

//            int requiredFloors = (int)numTotalFloors.Value;
//            if (totalInSchedule != requiredFloors)
//            {
//                MessageBox.Show(
//                    $"Grade schedule mismatch!\n\n" +
//                    $"Building: {requiredFloors} floors\n" +
//                    $"Schedule: {totalInSchedule} floors",
//                    "Validation Error",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//                tabControl.SelectedIndex = 1;
//                return false;
//            }

//            return true;
//        }

//        private bool CollectFloorConfigs()
//        {
//            FloorConfigs.Clear();

//            if (chkBasement.Checked)
//            {
//                if (!AddFloorConfig("Basement", (int)numBasementLevels.Value,
//                    (double)numBasementHeight.Value))
//                    return false;
//            }

//            if (chkPodium.Checked)
//            {
//                if (!AddFloorConfig("Podium", (int)numPodiumLevels.Value,
//                    (double)numPodiumHeight.Value))
//                    return false;
//            }

//            if (!AddFloorConfig("EDeck", 1, (double)numEDeckHeight.Value))
//                return false;

//            if (!AddFloorConfig("Typical", (int)numTypicalLevels.Value,
//                (double)numTypicalHeight.Value))
//                return false;

//            if (chkTerrace.Checked)
//            {
//                if (!AddFloorConfig("Terrace", 1, (double)numTerraceheight.Value))
//                    return false;
//            }

//            return true;
//        }

//        private bool AddFloorConfig(string name, int count, double height)
//        {
//            if (!ValidateFloorConfig(name))
//            {
//                MessageBox.Show(
//                    $"Please configure {name} CAD file and layer mappings.",
//                    "Validation Error");
//                return false;
//            }

//            FloorConfigs.Add(new FloorTypeConfig
//            {
//                Name = name,
//                Count = count,
//                Height = height,
//                CADFilePath = cadPathTextBoxes[name].Text,
//                LayerMapping = GetLayerMapping(name),
//                BeamDepths = GetBeamDepthsForFloor(name),
//                SlabThicknesses = GetSlabThicknessesForFloor(name)
//            });

//            return true;
//        }

//        private Dictionary<string, int> GetBeamDepthsForFloor(string floorType)
//        {
//            return new Dictionary<string, int>
//            {
//                ["InternalGravity"] = (int)numInternalGravityDepthPerFloor[floorType].Value,
//                ["CantileverGravity"] = (int)numCantileverGravityDepthPerFloor[floorType].Value,
//                ["CoreMain"] = (int)numCoreMainDepthPerFloor[floorType].Value,
//                ["PeripheralDeadMain"] = (int)numPeripheralDeadMainDepthPerFloor[floorType].Value,
//                ["PeripheralPortalMain"] = (int)numPeripheralPortalMainDepthPerFloor[floorType].Value,
//                ["InternalMain"] = (int)numInternalMainDepthPerFloor[floorType].Value
//            };
//        }

//        private Dictionary<string, int> GetSlabThicknessesForFloor(string floorType)
//        {
//            return new Dictionary<string, int>
//            {
//                ["Lobby"] = (int)numLobbySlabThicknessPerFloor[floorType].Value,
//                ["Stair"] = (int)numStairSlabThicknessPerFloor[floorType].Value
//            };
//        }

//        private bool ValidateFloorConfig(string floorType)
//        {
//            return cadPathTextBoxes.ContainsKey(floorType) &&
//                   !string.IsNullOrEmpty(cadPathTextBoxes[floorType].Text) &&
//                   mappedLayerListBoxes.ContainsKey(floorType) &&
//                   mappedLayerListBoxes[floorType].Items.Count > 0;
//        }

//        private Dictionary<string, string> GetLayerMapping(string floorType)
//        {
//            Dictionary<string, string> mapping = new Dictionary<string, string>();

//            if (mappedLayerListBoxes.ContainsKey(floorType))
//            {
//                foreach (var item in mappedLayerListBoxes[floorType].Items)
//                {
//                    string[] parts = item.ToString().Split(new[] { " → " },
//                        StringSplitOptions.None);
//                    if (parts.Length == 2)
//                    {
//                        mapping[parts[0]] = parts[1];
//                    }
//                }
//            }

//            return mapping;
//        }

//        private void ShowConfirmation()
//        {
//            int totalStories = FloorConfigs.Sum(c => c.Count);
//            double totalHeight = FloorConfigs.Sum(c => c.Height * c.Count);

//            var msg = new System.Text.StringBuilder();
//            msg.AppendLine("CONFIRM IMPORT");
//            msg.AppendLine("═══════════════════════════════════════\n");

//            msg.AppendLine($"Building: {totalStories} floors, {totalHeight:F2}m, {SeismicZone}");
//            msg.AppendLine($"Types: {string.Join(", ", FloorConfigs.Select(c => $"{c.Name}({c.Count})"))}\n");

//            msg.AppendLine("FLOOR CONFIGS:");
//            foreach (var config in FloorConfigs)
//            {
//                int gw = (SeismicZone == "Zone II" || SeismicZone == "Zone III") ? 200 : 240;
//                msg.AppendLine($"\n{config.Name}:");
//                msg.AppendLine($"  Beams: Grav {gw}x{config.BeamDepths["InternalGravity"]}, " +
//                    $"Core {config.BeamDepths["CoreMain"]}");
//                msg.AppendLine($"  Slabs: Lobby {config.SlabThicknesses["Lobby"]}mm, " +
//                    $"Stair {config.SlabThicknesses["Stair"]}mm");
//            }

//            msg.AppendLine("\n\nGRADES:");
//            int floorStart = 1;
//            for (int i = 0; i < WallGrades.Count; i++)
//            {
//                string bsg = CalculateBeamSlabGrade(WallGrades[i]);
//                int floorEnd = floorStart + FloorsPerGrade[i] - 1;
//                msg.AppendLine($"  F{floorStart}-{floorEnd}: {WallGrades[i]}/{bsg}");
//                floorStart = floorEnd + 1;
//            }

//            msg.AppendLine("\n═══════════════════════════════════════");
//            msg.AppendLine("Ready to import?");

//            var result = MessageBox.Show(
//                msg.ToString(),
//                "Confirm Import",
//                MessageBoxButtons.YesNo,
//                MessageBoxIcon.Question);

//            if (result == DialogResult.Yes)
//            {
//                this.DialogResult = DialogResult.OK;
//                this.Close();
//            }
//        }
//    }
//}

//// ============================================================================
//// END OF PART 1
//// ============================================================================
// ============================================================================
// FILE: UI/ImportConfigForm.cs (PART 1 - Main Form)
// ============================================================================
// PURPOSE: Main configuration form class with core logic
// AUTHOR: ETAB Automation Team
// VERSION: 2.2 (Updated for individual basement floors and ground floor)
// ============================================================================

using ETAB_Automation.Importers;
using ETAB_Automation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ETAB_Automation
{
    /// <summary>
    /// Main configuration form for importing CAD files
    /// Part 1: Core logic, validation, and event handlers
    /// Part 2: UI initialization (ImportConfigFormUI.cs)
    /// </summary>
    public partial class ImportConfigForm : Form
    {
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        public List<FloorTypeConfig> FloorConfigs { get; private set; }
        public string SeismicZone { get; private set; }
        public List<string> WallGrades { get; private set; }
        public List<int> FloorsPerGrade { get; private set; }
        public double FoundationHeight { get; private set; }

        // ====================================================================
        // UI CONTROLS (declared here, initialized in Part 2)
        // ====================================================================

        internal TabControl tabControl;
        internal Button btnImport;
        internal Button btnCancel;

        // Building config
        internal CheckBox chkBasement;
        internal CheckBox chkPodium;
        internal CheckBox chkGround;
        internal CheckBox chkEDeck;
        internal CheckBox chkTypical;
        internal CheckBox chkTerrace;
        internal CheckBox chkFoundation;
        internal NumericUpDown numBasementLevels;
        internal NumericUpDown numPodiumLevels;
        internal NumericUpDown numTypicalLevels;
        internal NumericUpDown numBasementHeight;
        internal NumericUpDown numPodiumHeight;
        internal NumericUpDown numGroundHeight;
        internal NumericUpDown numEDeckHeight;
        internal NumericUpDown numTypicalHeight;
        internal NumericUpDown numTerraceheight;
        internal NumericUpDown numFoundationHeight;
        internal ComboBox cmbSeismicZone;

        // Grade schedule
        internal DataGridView dgvGradeSchedule;
        internal NumericUpDown numTotalFloors;
        internal Button btnAddGradeRow;
        internal Button btnRemoveGradeRow;
        internal Label lblGradeTotal;

        // CAD Import (dynamic per floor type)
        internal Dictionary<string, TextBox> cadPathTextBoxes;
        internal Dictionary<string, ListBox> availableLayerListBoxes;
        internal Dictionary<string, ListBox> mappedLayerListBoxes;
        internal Dictionary<string, ComboBox> elementTypeComboBoxes;

        // Per-floor beam depths
        internal Dictionary<string, NumericUpDown> numInternalGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numCantileverGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numCoreMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralDeadMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralPortalMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numInternalMainDepthPerFloor;

        // Per-floor slab thicknesses
        internal Dictionary<string, NumericUpDown> numLobbySlabThicknessPerFloor;
        internal Dictionary<string, NumericUpDown> numStairSlabThicknessPerFloor;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public ImportConfigForm()
        {
            InitializeComponent();

            // Initialize data collections
            FloorConfigs = new List<FloorTypeConfig>();
            WallGrades = new List<string>();
            FloorsPerGrade = new List<int>();

            // Initialize control dictionaries
            cadPathTextBoxes = new Dictionary<string, TextBox>();
            availableLayerListBoxes = new Dictionary<string, ListBox>();
            mappedLayerListBoxes = new Dictionary<string, ListBox>();
            elementTypeComboBoxes = new Dictionary<string, ComboBox>();
            numInternalGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numCantileverGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numCoreMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralDeadMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralPortalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numInternalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numLobbySlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();
            numStairSlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();

            // Build UI (calls Part 2)
            InitializeControlsUI();
        }

        // ====================================================================
        // CAD FILE LOADING
        // ====================================================================

        internal void BtnLoadCAD_Click(string floorType)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "AutoCAD Files (*.dxf;*.dwg)|*.dxf;*.dwg|DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
                Title = $"Select CAD File for {floorType}"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            cadPathTextBoxes[floorType].Text = ofd.FileName;
            string extension = System.IO.Path.GetExtension(ofd.FileName).ToLower();

            if (extension == ".dwg")
            {
                MessageBox.Show(
                    "DWG files are not directly supported.\n\n" +
                    "Please convert to DXF format:\n" +
                    "1. Open in AutoCAD\n" +
                    "2. Save As → DXF\n" +
                    "3. Load the DXF file here",
                    "DWG Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (extension != ".dxf")
            {
                MessageBox.Show(
                    "Please select a DXF file.\n\n" +
                    "Supported format: .dxf",
                    "Invalid File Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CADLayerReader reader = new CADLayerReader();
                List<string> layers = reader.GetLayerNamesFromFile(ofd.FileName);

                if (layers.Count == 0)
                {
                    MessageBox.Show(
                        "No layers found in CAD file.",
                        "No Layers Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                availableLayerListBoxes[floorType].Items.Clear();
                foreach (string layer in layers)
                {
                    availableLayerListBoxes[floorType].Items.Add(layer);
                }

                AutoMapLayers(floorType, layers);

                MessageBox.Show(
                    $"✓ CAD file loaded!\n\n" +
                    $"Layers: {layers.Count}\n\n" +
                    "Auto-mapped based on naming conventions.",
                    "Layers Loaded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error reading CAD file:\n\n{ex.Message}",
                    "CAD Read Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AutoMapLayers(string floorType, List<string> layers)
        {
            mappedLayerListBoxes[floorType].Items.Clear();

            foreach (string layer in layers)
            {
                string elementType = null;

                if (layer.StartsWith("B-") || layer.Contains("Beam") ||
                    layer.Contains("BEAM") || layer.Contains("beam"))
                    elementType = "Beam";
                else if (layer.Contains("wall") || layer.Contains("Wall") ||
                         layer.Contains("WALL"))
                    elementType = "Wall";
                else if (layer.StartsWith("S-") || layer.Contains("Slab") ||
                         layer.Contains("SLAB"))
                    elementType = "Slab";

                if (elementType != null)
                {
                    mappedLayerListBoxes[floorType].Items.Add($"{layer} → {elementType}");
                }
            }
        }

        internal void BtnAddMapping_Click(string floorType)
        {
            if (availableLayerListBoxes[floorType].SelectedItem == null)
            {
                MessageBox.Show("Please select a layer to map.", "Info");
                return;
            }

            string layerName = availableLayerListBoxes[floorType].SelectedItem.ToString();
            string elementType = elementTypeComboBoxes[floorType].SelectedItem.ToString();

            if (elementType == "Ignore") return;

            string mapping = $"{layerName} → {elementType}";
            if (!mappedLayerListBoxes[floorType].Items.Contains(mapping))
            {
                mappedLayerListBoxes[floorType].Items.Add(mapping);
            }
            else
            {
                MessageBox.Show("Layer already mapped.", "Info");
            }
        }

        internal void BtnRemoveMapping_Click(string floorType)
        {
            if (mappedLayerListBoxes[floorType].SelectedItem == null)
            {
                MessageBox.Show("Please select a mapping to remove.", "Info");
                return;
            }

            mappedLayerListBoxes[floorType].Items.Remove(
                mappedLayerListBoxes[floorType].SelectedItem);
        }

        // ====================================================================
        // GRADE SCHEDULE HANDLERS
        // ====================================================================

        private void BtnAddGradeRow_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvGradeSchedule.Rows.Add();
            var row = dgvGradeSchedule.Rows[rowIndex];

            row.Cells["Index"].Value = rowIndex;
            row.Cells["WallGrade"].Value = "M40";
            row.Cells["FloorsCount"].Value = "1";
            row.Cells["BeamSlabGrade"].Value = "M30";
            row.Cells["FloorRange"].Value = "";

            UpdateGradeTotals();
        }

        private void BtnRemoveGradeRow_Click(object sender, EventArgs e)
        {
            if (dgvGradeSchedule.SelectedRows.Count > 0)
            {
                dgvGradeSchedule.Rows.RemoveAt(dgvGradeSchedule.SelectedRows[0].Index);
                ReindexRows();
                UpdateGradeTotals();
            }
        }

        private void DgvGradeSchedule_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvGradeSchedule.Rows[e.RowIndex];

            if (e.ColumnIndex == dgvGradeSchedule.Columns["WallGrade"].Index)
            {
                string wallGrade = row.Cells["WallGrade"].Value?.ToString();
                if (!string.IsNullOrEmpty(wallGrade))
                {
                    row.Cells["BeamSlabGrade"].Value = CalculateBeamSlabGrade(wallGrade);
                }
            }

            if (e.ColumnIndex == dgvGradeSchedule.Columns["FloorsCount"].Index)
            {
                UpdateGradeTotals();
            }
        }

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

        private void ReindexRows()
        {
            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
            {
                dgvGradeSchedule.Rows[i].Cells["Index"].Value = i;
            }
            UpdateFloorRanges();
        }

        private void UpdateFloorRanges()
        {
            int currentFloor = 1;

            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
            {
                var row = dgvGradeSchedule.Rows[i];
                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();

                if (int.TryParse(floorsStr, out int floorCount) && floorCount > 0)
                {
                    int endFloor = currentFloor + floorCount - 1;
                    row.Cells["FloorRange"].Value = $"{currentFloor}-{endFloor}";
                    currentFloor = endFloor + 1;
                }
                else
                {
                    row.Cells["FloorRange"].Value = "";
                }
            }
        }

        internal void UpdateGradeTotals()
        {
            int totalInSchedule = 0;

            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
            {
                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();
                if (int.TryParse(floorsStr, out int floors))
                {
                    totalInSchedule += floors;
                }
            }

            int requiredTotal = (int)numTotalFloors.Value;
            bool isValid = totalInSchedule == requiredTotal;

            lblGradeTotal.Text = $"Total floors in schedule: {totalInSchedule} / {requiredTotal}";
            lblGradeTotal.ForeColor = isValid ?
                System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed;

            if (isValid)
                lblGradeTotal.Text += " ✓ VALID";
            else if (totalInSchedule > requiredTotal)
                lblGradeTotal.Text += " ❌ TOO MANY";
            else
                lblGradeTotal.Text += " ❌ TOO FEW";

            UpdateFloorRanges();
        }

        internal void UpdateTotalFloorsForGradeSchedule()
        {
            int total = 0;

            // Count individual basement floors
            if (chkBasement.Checked)
                total += (int)numBasementLevels.Value;

            if (chkPodium.Checked)
                total += (int)numPodiumLevels.Value;

            if (chkGround.Checked)
                total += 1;

            if (chkEDeck.Checked)
                total += 1;

            if (chkTypical.Checked)
                total += (int)numTypicalLevels.Value;

            if (chkTerrace.Checked)
                total += 1;

            numTotalFloors.Value = total;
            UpdateGradeTotals();
        }

        // ====================================================================
        // BUILDING CONFIG HANDLERS
        // ====================================================================

        private void ChkBasement_CheckedChanged(object sender, EventArgs e)
        {
            numBasementLevels.Enabled = chkBasement.Checked;
            numBasementHeight.Enabled = chkBasement.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void NumBasementLevels_ValueChanged(object sender, EventArgs e)
        {
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkPodium_CheckedChanged(object sender, EventArgs e)
        {
            numPodiumLevels.Enabled = chkPodium.Checked;
            numPodiumHeight.Enabled = chkPodium.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void NumPodiumLevels_ValueChanged(object sender, EventArgs e)
        {
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkGround_CheckedChanged(object sender, EventArgs e)
        {
            numGroundHeight.Enabled = chkGround.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkEDeck_CheckedChanged(object sender, EventArgs e)
        {
            numEDeckHeight.Enabled = chkEDeck.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkTypical_CheckedChanged(object sender, EventArgs e)
        {
            numTypicalLevels.Enabled = chkTypical.Checked;
            numTypicalHeight.Enabled = chkTypical.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void NumTypicalLevels_ValueChanged(object sender, EventArgs e)
        {
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkTerrace_CheckedChanged(object sender, EventArgs e)
        {
            numTerraceheight.Enabled = chkTerrace.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkFoundation_CheckedChanged(object sender, EventArgs e)
        {
            numFoundationHeight.Enabled = chkFoundation.Checked;
        }

        // ====================================================================
        // IMPORT VALIDATION AND EXECUTION
        // ====================================================================

        private void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate at least one floor type is selected
                if (!chkBasement.Checked && !chkPodium.Checked && !chkGround.Checked &&
                    !chkEDeck.Checked && !chkTypical.Checked && !chkTerrace.Checked)
                {
                    MessageBox.Show(
                        "Please select at least one floor type!",
                        "No Floors Selected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    tabControl.SelectedIndex = 0;
                    return;
                }

                // Validate grade schedule
                if (!ValidateGradeSchedule())
                    return;

                // Collect floor configs
                if (!CollectFloorConfigs())
                    return;

                SeismicZone = cmbSeismicZone.SelectedItem?.ToString() ?? "Zone IV";
                FoundationHeight = chkFoundation.Checked ? (double)numFoundationHeight.Value : 0;

                ShowConfirmation();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error preparing import:\n\n{ex.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool ValidateGradeSchedule()
        {
            if (dgvGradeSchedule.Rows.Count == 0)
            {
                MessageBox.Show(
                    "No concrete grades defined!\n\n" +
                    "Please add at least one grade row.",
                    "Grade Schedule Empty",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                tabControl.SelectedIndex = 1;
                return false;
            }

            WallGrades.Clear();
            FloorsPerGrade.Clear();

            int totalInSchedule = 0;
            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
            {
                string wallGrade = row.Cells["WallGrade"].Value?.ToString();
                string floorsStr = row.Cells["FloorsCount"].Value?.ToString();

                if (string.IsNullOrEmpty(wallGrade) || !int.TryParse(floorsStr, out int floors))
                {
                    MessageBox.Show(
                        $"Invalid grade schedule at row {row.Index}.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 1;
                    return false;
                }

                WallGrades.Add(wallGrade);
                FloorsPerGrade.Add(floors);
                totalInSchedule += floors;
            }

            int requiredFloors = (int)numTotalFloors.Value;
            if (totalInSchedule != requiredFloors)
            {
                MessageBox.Show(
                    $"Grade schedule mismatch!\n\n" +
                    $"Building: {requiredFloors} floors\n" +
                    $"Schedule: {totalInSchedule} floors",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                tabControl.SelectedIndex = 1;
                return false;
            }

            return true;
        }

        private bool CollectFloorConfigs()
        {
            FloorConfigs.Clear();

            // Individual basement floors (B1, B2, B3, B4, B5)
            if (chkBasement.Checked)
            {
                int basementCount = (int)numBasementLevels.Value;
                for (int i = 1; i <= basementCount; i++)
                {
                    string basementName = $"Basement{i}";
                    if (!AddFloorConfig(basementName, 1, (double)numBasementHeight.Value))
                        return false;
                }
            }

            if (chkPodium.Checked)
            {
                if (!AddFloorConfig("Podium", (int)numPodiumLevels.Value,
                    (double)numPodiumHeight.Value))
                    return false;
            }

            if (chkGround.Checked)
            {
                if (!AddFloorConfig("Ground", 1, (double)numGroundHeight.Value))
                    return false;
            }

            if (chkEDeck.Checked)
            {
                if (!AddFloorConfig("EDeck", 1, (double)numEDeckHeight.Value))
                    return false;
            }

            if (chkTypical.Checked)
            {
                if (!AddFloorConfig("Typical", (int)numTypicalLevels.Value,
                    (double)numTypicalHeight.Value))
                    return false;
            }

            if (chkTerrace.Checked)
            {
                if (!AddFloorConfig("Terrace", 1, (double)numTerraceheight.Value))
                    return false;
            }

            return true;
        }

        private bool AddFloorConfig(string name, int count, double height)
        {
            if (!ValidateFloorConfig(name))
            {
                MessageBox.Show(
                    $"Please configure {name} CAD file and layer mappings.",
                    "Validation Error");
                return false;
            }

            FloorConfigs.Add(new FloorTypeConfig
            {
                Name = name,
                Count = count,
                Height = height,
                CADFilePath = cadPathTextBoxes[name].Text,
                LayerMapping = GetLayerMapping(name),
                BeamDepths = GetBeamDepthsForFloor(name),
                SlabThicknesses = GetSlabThicknessesForFloor(name)
            });

            return true;
        }

        private Dictionary<string, int> GetBeamDepthsForFloor(string floorType)
        {
            return new Dictionary<string, int>
            {
                ["InternalGravity"] = (int)numInternalGravityDepthPerFloor[floorType].Value,
                ["CantileverGravity"] = (int)numCantileverGravityDepthPerFloor[floorType].Value,
                ["CoreMain"] = (int)numCoreMainDepthPerFloor[floorType].Value,
                ["PeripheralDeadMain"] = (int)numPeripheralDeadMainDepthPerFloor[floorType].Value,
                ["PeripheralPortalMain"] = (int)numPeripheralPortalMainDepthPerFloor[floorType].Value,
                ["InternalMain"] = (int)numInternalMainDepthPerFloor[floorType].Value
            };
        }

        private Dictionary<string, int> GetSlabThicknessesForFloor(string floorType)
        {
            return new Dictionary<string, int>
            {
                ["Lobby"] = (int)numLobbySlabThicknessPerFloor[floorType].Value,
                ["Stair"] = (int)numStairSlabThicknessPerFloor[floorType].Value
            };
        }

        private bool ValidateFloorConfig(string floorType)
        {
            return cadPathTextBoxes.ContainsKey(floorType) &&
                   !string.IsNullOrEmpty(cadPathTextBoxes[floorType].Text) &&
                   mappedLayerListBoxes.ContainsKey(floorType) &&
                   mappedLayerListBoxes[floorType].Items.Count > 0;
        }

        private Dictionary<string, string> GetLayerMapping(string floorType)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();

            if (mappedLayerListBoxes.ContainsKey(floorType))
            {
                foreach (var item in mappedLayerListBoxes[floorType].Items)
                {
                    string[] parts = item.ToString().Split(new[] { " → " },
                        StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        mapping[parts[0]] = parts[1];
                    }
                }
            }

            return mapping;
        }

        private void ShowConfirmation()
        {
            int totalStories = FloorConfigs.Sum(c => c.Count);
            double totalHeight = FloorConfigs.Sum(c => c.Height * c.Count);

            var msg = new System.Text.StringBuilder();
            msg.AppendLine("CONFIRM IMPORT");
            msg.AppendLine("═══════════════════════════════════════\n");

            msg.AppendLine($"Building: {totalStories} floors, {totalHeight:F2}m, {SeismicZone}");
            if (chkFoundation.Checked)
                msg.AppendLine($"Foundation Height: {FoundationHeight:F2}m");
            msg.AppendLine($"Types: {string.Join(", ", FloorConfigs.Select(c => $"{c.Name}({c.Count})"))}\n");

            msg.AppendLine("FLOOR CONFIGS:");
            foreach (var config in FloorConfigs)
            {
                int gw = (SeismicZone == "Zone II" || SeismicZone == "Zone III") ? 200 : 240;
                msg.AppendLine($"\n{config.Name}:");
                msg.AppendLine($"  Beams: Grav {gw}x{config.BeamDepths["InternalGravity"]}, " +
                    $"Core {config.BeamDepths["CoreMain"]}");
                msg.AppendLine($"  Slabs: Lobby {config.SlabThicknesses["Lobby"]}mm, " +
                    $"Stair {config.SlabThicknesses["Stair"]}mm");
            }

            msg.AppendLine("\n\nGRADES:");
            int floorStart = 1;
            for (int i = 0; i < WallGrades.Count; i++)
            {
                string bsg = CalculateBeamSlabGrade(WallGrades[i]);
                int floorEnd = floorStart + FloorsPerGrade[i] - 1;
                msg.AppendLine($"  F{floorStart}-{floorEnd}: {WallGrades[i]}/{bsg}");
                floorStart = floorEnd + 1;
            }

            msg.AppendLine("\n═══════════════════════════════════════");
            msg.AppendLine("Ready to import?");

            var result = MessageBox.Show(
                msg.ToString(),
                "Confirm Import",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}

// ============================================================================
// END OF PART 1
// ============================================================================
