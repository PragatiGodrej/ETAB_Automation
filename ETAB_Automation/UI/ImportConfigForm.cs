
// ============================================================================
// FILE: UI/ImportConfigForm.cs (PART 1 - Main Form)
// ============================================================================
// PURPOSE: Main configuration form class with core logic
// VERSION: 2.7 — IS 1893 Code Edition selector (2016 / 2025)
// ============================================================================

using ETAB_Automation.Core;
using ETAB_Automation.Importers;
using ETAB_Automation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ETAB_Automation
{
    public partial class ImportConfigForm : Form
    {
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        public List<FloorTypeConfig> FloorConfigs { get; private set; }
        public string SeismicZone { get; private set; }

        // ── 1a. New public property for selected IS Code ──────────────────
        public WallThicknessCalculator.ISCodeVersion SelectedISCode { get; private set; }

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

        // ── 1b. New ComboBox for IS Code edition ─────────────────────────
        internal ComboBox cmbISCode;

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

        // ── Per-floor GRAVITY beam depths ────────────────────────────────
        internal Dictionary<string, NumericUpDown> numInternalGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numCantileverGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numNoLoadGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numEDeckGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numPodiumGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numGroundGravityDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numBasementGravityDepthPerFloor;

        // ── Per-floor MAIN beam depths ───────────────────────────────────
        internal Dictionary<string, NumericUpDown> numCoreMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralDeadMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralPortalMainDepthPerFloor;
        internal Dictionary<string, NumericUpDown> numInternalMainDepthPerFloor;

        // ── Per-floor beam WIDTH overrides — per variant (0 = auto) ──────
        // Gravity variants (each independently overridable)
        internal Dictionary<string, NumericUpDown> numInternalGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numCantileverGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numNoLoadGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numEDeckGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numPodiumGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numGroundGravityWidthPerFloor;
        internal Dictionary<string, NumericUpDown> numBasementGravityWidthPerFloor;
        // Main beam widths
        internal Dictionary<string, NumericUpDown> numCoreMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralDeadMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralPortalMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numInternalMainWidthOverridePerFloor;

        // ── SHARED BEAM WALL LOAD SETS (one for all floor plans) ─────────
        internal TextBox txtSharedInternalGravityLoadSet;
        internal TextBox txtSharedCantileverGravityLoadSet;
        internal TextBox txtSharedEDeckGravityLoadSet;
        internal TextBox txtSharedPodiumGravityLoadSet;
        internal TextBox txtSharedGroundGravityLoadSet;
        internal TextBox txtSharedBasementGravityLoadSet;
        internal TextBox txtSharedCoreMainLoadSet;
        internal TextBox txtSharedPeripheralDeadMainLoadSet;
        internal TextBox txtSharedPeripheralPortalMainLoadSet;
        internal TextBox txtSharedInternalMainLoadSet;

        // ── Per-floor slab thicknesses — YELLOW layers ───────────────────
        internal Dictionary<string, NumericUpDown> numLobbySlabThicknessPerFloor;
        internal Dictionary<string, NumericUpDown> numStairSlabThicknessPerFloor;
        internal Dictionary<string, NumericUpDown> numFireTenderSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numOHTSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numTerraceFireSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numUGTSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numLandscapeSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numSwimmingSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numDGSlabPerFloor;
        internal Dictionary<string, NumericUpDown> numSTPSlabPerFloor;

        // ── SHARED SLAB INDIVIDUAL LOADS (one set for all floor plans) ──────
        // Key = short slab key (e.g. "Lobby", "Residential", "Balcony")
        // Value = 9 NumericUpDown controls for [FF, Fill, ASDL, LL, LL>3,
        //         FireTender, TreeLoad, MachineRoom, WaterTank]
        internal Dictionary<string, NumericUpDown[]> sharedSlabIndividualLoadControls;

        // ── Per-floor wall thickness overrides ──────────────────────────
        internal Dictionary<string, NumericUpDown> numCoreWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeriphDeadWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeriphPortalWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numInternalWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numNtaWallThicknessPerFloor;

        // ── Per-floor COLUMN dimensions (B × D) ─────────────────────────────
        internal Dictionary<string, NumericUpDown> numColumnBPerFloor;
        internal Dictionary<string, NumericUpDown> numColumnDPerFloor;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public ImportConfigForm()
        {
            // numTerraceheight has no UI control — must initialize BEFORE InitializeComponent
            // because ChkTerrace_CheckedChanged fires during init and references this field
            numTerraceheight = new NumericUpDown { Value = 3.0M };

            InitializeComponent();

            FloorConfigs = new List<FloorTypeConfig>();
            WallGrades = new List<string>();
            FloorsPerGrade = new List<int>();

            // Core dicts
            cadPathTextBoxes = new Dictionary<string, TextBox>();
            availableLayerListBoxes = new Dictionary<string, ListBox>();
            mappedLayerListBoxes = new Dictionary<string, ListBox>();
            elementTypeComboBoxes = new Dictionary<string, ComboBox>();

            // Gravity beam depth dicts
            numInternalGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numCantileverGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numNoLoadGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numEDeckGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numPodiumGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numGroundGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numBasementGravityDepthPerFloor = new Dictionary<string, NumericUpDown>();

            // Main beam depth dicts
            numCoreMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralDeadMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralPortalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();
            numInternalMainDepthPerFloor = new Dictionary<string, NumericUpDown>();

            // Per-variant gravity width dicts
            numInternalGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numCantileverGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numNoLoadGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numEDeckGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numPodiumGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numGroundGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();
            numBasementGravityWidthPerFloor = new Dictionary<string, NumericUpDown>();

            // Main beam width override dicts
            numCoreMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralDeadMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralPortalMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numInternalMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();

            // Beam wall load set dicts
            // Shared slab individual load controls — created by InitializeLoadSetsTab()
            sharedSlabIndividualLoadControls = new Dictionary<string, NumericUpDown[]>();

            // Slab thickness dicts
            numLobbySlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();
            numStairSlabThicknessPerFloor = new Dictionary<string, NumericUpDown>();
            numFireTenderSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numOHTSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numTerraceFireSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numUGTSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numLandscapeSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numSwimmingSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numDGSlabPerFloor = new Dictionary<string, NumericUpDown>();
            numSTPSlabPerFloor = new Dictionary<string, NumericUpDown>();

            // sharedSlabIndividualLoadControls initialized above

            // Wall override dicts
            numCoreWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeriphDeadWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeriphPortalWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numInternalWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numNtaWallThicknessPerFloor = new Dictionary<string, NumericUpDown>();

            // Column data
            numColumnBPerFloor = new Dictionary<string, NumericUpDown>();
            numColumnDPerFloor = new Dictionary<string, NumericUpDown>();

            InitializeControlsUI();
        }

        // ====================================================================
        // CAD FILE LOADING
        // ====================================================================

        internal void BtnLoadCAD_Click(string floorType)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "AutoCAD Files (*.dxf;*.dwg)|*.dxf;*.dwg|DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
                Title = $"Select CAD File for {floorType}"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            cadPathTextBoxes[floorType].Text = ofd.FileName;
            string ext = System.IO.Path.GetExtension(ofd.FileName).ToLower();

            if (ext == ".dwg")
            {
                MessageBox.Show("DWG files are not directly supported.\n\nPlease convert to DXF first.",
                    "DWG Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (ext != ".dxf")
            {
                MessageBox.Show("Please select a DXF file.", "Invalid File Type",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var reader = new CADLayerReader();
                var layers = reader.GetLayerNamesFromFile(ofd.FileName);

                if (layers.Count == 0)
                {
                    MessageBox.Show("No layers found in CAD file.", "No Layers Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                availableLayerListBoxes[floorType].Items.Clear();
                foreach (string layer in layers)
                    availableLayerListBoxes[floorType].Items.Add(layer);

                AutoMapLayers(floorType, layers);

                MessageBox.Show($"✓ CAD file loaded!\nLayers: {layers.Count}\n\nAuto-mapped by naming convention.",
                    "Layers Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CAD file:\n\n{ex.Message}", "CAD Read Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AutoMapLayers(string floorType, List<string> layers)
        {
            mappedLayerListBoxes[floorType].Items.Clear();
            foreach (string layer in layers)
            {
                string u = layer.ToUpperInvariant();
                string elementType = null;
                if (u.StartsWith("B-") || u.Contains("BEAM")) elementType = "Beam";
                else if (u.StartsWith("W-") || u.Contains("WALL")) elementType = "Wall";
                else if (u.StartsWith("S-") || u.Contains("SLAB")) elementType = "Slab";
                else if (u.StartsWith("C-") || u.Contains("COLUMN")) elementType = "Column";
                if (elementType != null)
                    mappedLayerListBoxes[floorType].Items.Add($"{layer} → {elementType}");
            }
        }

        internal void BtnAddMapping_Click(string floorType)
        {
            if (availableLayerListBoxes[floorType].SelectedItem == null)
            { MessageBox.Show("Please select a layer to map.", "Info"); return; }

            string layerName = availableLayerListBoxes[floorType].SelectedItem.ToString();
            string elementType = elementTypeComboBoxes[floorType].SelectedItem.ToString();
            if (elementType == "Ignore") return;

            string mapping = $"{layerName} → {elementType}";
            if (!mappedLayerListBoxes[floorType].Items.Contains(mapping))
                mappedLayerListBoxes[floorType].Items.Add(mapping);
            else
                MessageBox.Show("Layer already mapped.", "Info");
        }

        internal void BtnRemoveMapping_Click(string floorType)
        {
            if (mappedLayerListBoxes[floorType].SelectedItem == null)
            { MessageBox.Show("Please select a mapping to remove.", "Info"); return; }
            mappedLayerListBoxes[floorType].Items.Remove(mappedLayerListBoxes[floorType].SelectedItem);
        }

        // ====================================================================
        // GRADE SCHEDULE HANDLERS
        // ====================================================================

        private void BtnAddGradeRow_Click(object sender, EventArgs e)
        {
            int idx = dgvGradeSchedule.Rows.Add();
            var row = dgvGradeSchedule.Rows[idx];
            row.Cells["Index"].Value = idx;
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
                string wg = row.Cells["WallGrade"].Value?.ToString();
                if (!string.IsNullOrEmpty(wg))
                    row.Cells["BeamSlabGrade"].Value = CalculateBeamSlabGrade(wg);
            }
            if (e.ColumnIndex == dgvGradeSchedule.Columns["FloorsCount"].Index)
                UpdateGradeTotals();
        }

        private string CalculateBeamSlabGrade(string wallGrade)
        {
            try
            {
                int wv = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());
                int bsv = (int)(Math.Ceiling((wv * 0.7) / 5.0) * 5);
                return $"M{Math.Max(bsv, 30)}";
            }
            catch { return "M30"; }
        }

        private void ReindexRows()
        {
            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
                dgvGradeSchedule.Rows[i].Cells["Index"].Value = i;
            UpdateFloorRanges();
        }

        private void UpdateFloorRanges()
        {
            int cur = 1;
            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
            {
                var row = dgvGradeSchedule.Rows[i];
                if (int.TryParse(row.Cells["FloorsCount"].Value?.ToString(), out int fc) && fc > 0)
                { row.Cells["FloorRange"].Value = $"{cur}-{cur + fc - 1}"; cur += fc; }
                else
                    row.Cells["FloorRange"].Value = "";
            }
        }

        internal void UpdateGradeTotals()
        {
            int total = 0;
            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
                if (int.TryParse(row.Cells["FloorsCount"].Value?.ToString(), out int f)) total += f;

            int req = (int)numTotalFloors.Value;
            bool ok = total == req;
            lblGradeTotal.Text = $"Total floors in schedule: {total} / {req}";
            lblGradeTotal.ForeColor = ok ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed;
            lblGradeTotal.Text += ok ? " ✓ VALID" : (total > req ? " ❌ TOO MANY" : " ❌ TOO FEW");
            UpdateFloorRanges();
        }

        internal void UpdateTotalFloorsForGradeSchedule()
        {
            int total = 0;
            if (chkBasement.Checked) total += (int)numBasementLevels.Value;
            if (chkPodium.Checked) total += (int)numPodiumLevels.Value;
            if (chkGround.Checked) total += 1;
            if (chkEDeck.Checked) total += 1;
            if (chkTypical.Checked) total += (int)numTypicalLevels.Value;
            if (chkTerrace.Checked) total += 1;
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
        private void NumBasementLevels_ValueChanged(object sender, EventArgs e) =>
            UpdateTotalFloorsForGradeSchedule();

        private void ChkPodium_CheckedChanged(object sender, EventArgs e)
        {
            numPodiumLevels.Enabled = chkPodium.Checked;
            numPodiumHeight.Enabled = chkPodium.Checked;
            UpdateTotalFloorsForGradeSchedule();
        }
        private void NumPodiumLevels_ValueChanged(object sender, EventArgs e) =>
            UpdateTotalFloorsForGradeSchedule();

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
        private void NumTypicalLevels_ValueChanged(object sender, EventArgs e) =>
            UpdateTotalFloorsForGradeSchedule();

        private void ChkTerrace_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTotalFloorsForGradeSchedule();
        }

        private void ChkFoundation_CheckedChanged(object sender, EventArgs e) =>
            numFoundationHeight.Enabled = chkFoundation.Checked;

        // ====================================================================
        // IMPORT VALIDATION AND EXECUTION
        // ====================================================================

        private void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                if (!chkBasement.Checked && !chkPodium.Checked && !chkGround.Checked &&
                    !chkEDeck.Checked && !chkTypical.Checked && !chkTerrace.Checked)
                {
                    MessageBox.Show("Please select at least one floor type!",
                        "No Floors Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabControl.SelectedIndex = 0;
                    return;
                }
                if (!ValidateGradeSchedule()) return;
                if (!CollectFloorConfigs()) return;

                SeismicZone = cmbSeismicZone.SelectedItem?.ToString() ?? "Zone IV (Ahmedabad & Kolkata)";

                // ── 1c. Capture the selected IS code ─────────────────────
                SelectedISCode = (cmbISCode.SelectedIndex == 0)
                    ? WallThicknessCalculator.ISCodeVersion.IS2016
                    : WallThicknessCalculator.ISCodeVersion.IS2025;

                FoundationHeight = chkFoundation.Checked ? (double)numFoundationHeight.Value : 0;

                ShowConfirmation();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing import:\n\n{ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateGradeSchedule()
        {
            if (dgvGradeSchedule.Rows.Count == 0)
            {
                MessageBox.Show("No concrete grades defined!\nPlease add at least one grade row.",
                    "Grade Schedule Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabControl.SelectedIndex = 1;
                return false;
            }

            WallGrades.Clear(); FloorsPerGrade.Clear();
            int totalInSchedule = 0;

            foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
            {
                string wg = row.Cells["WallGrade"].Value?.ToString();
                string fs = row.Cells["FloorsCount"].Value?.ToString();
                if (string.IsNullOrEmpty(wg) || !int.TryParse(fs, out int floors))
                {
                    MessageBox.Show($"Invalid grade schedule at row {row.Index}.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 1;
                    return false;
                }
                WallGrades.Add(wg); FloorsPerGrade.Add(floors); totalInSchedule += floors;
            }

            if (totalInSchedule != (int)numTotalFloors.Value)
            {
                MessageBox.Show(
                    $"Grade schedule mismatch!\n\nBuilding: {numTotalFloors.Value} floors\nSchedule: {totalInSchedule} floors",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tabControl.SelectedIndex = 1;
                return false;
            }
            return true;
        }

        // ====================================================================
        // COLLECT FLOOR CONFIGS
        // ====================================================================

        private bool CollectFloorConfigs()
        {
            FloorConfigs.Clear();

            var sequence = new List<string>();

            if (chkBasement.Checked)
            {
                int cnt = (int)numBasementLevels.Value;
                // Inverted numbering: deepest basement = BasementN, shallowest = Basement1
                // Sequence is bottom→top, so first added is deepest → gets highest number
                for (int i = cnt; i >= 1; i--)
                    sequence.Add($"Basement{i}");
            }
            if (chkGround.Checked) sequence.Add("Ground");
            if (chkPodium.Checked)
            {
                int cnt = (int)numPodiumLevels.Value;
                for (int i = 1; i <= cnt; i++)
                    sequence.Add($"Podium{i}");
            }
            if (chkEDeck.Checked) sequence.Add("EDeck");

            if (chkTypical.Checked)
            {
                int cnt = (int)numTypicalLevels.Value;
                for (int i = 0; i < cnt; i++)
                    sequence.Add("Typical");
            }

            if (chkTerrace.Checked) sequence.Add("Terrace");

            var requiredTypes = new HashSet<string>(sequence);
            foreach (string ft in requiredTypes)
            {
                if (!ValidateFloorConfig(ft))
                {
                    MessageBox.Show($"Please configure CAD file and layer mappings for: {ft}",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            var individualTypes = new HashSet<string>();
            if (chkBasement.Checked)
                for (int i = 1; i <= (int)numBasementLevels.Value; i++)
                    individualTypes.Add($"Basement{i}");
            if (chkPodium.Checked)
                for (int i = 1; i <= (int)numPodiumLevels.Value; i++)
                    individualTypes.Add($"Podium{i}");

            int idx2 = 0;
            while (idx2 < sequence.Count)
            {
                string ft = sequence[idx2];
                if (individualTypes.Contains(ft))
                {
                    if (!AddFloorConfig(ft, 1, GetHeightForFloorType(ft))) return false;
                    idx2++;
                }
                else
                {
                    int run = 1;
                    while (idx2 + run < sequence.Count && sequence[idx2 + run] == ft)
                        run++;
                    if (!AddFloorConfig(ft, run, GetHeightForFloorType(ft))) return false;
                    idx2 += run;
                }
            }
            return true;
        }

        private double GetHeightForFloorType(string ft)
        {
            if (ft.StartsWith("Basement")) return (double)numBasementHeight.Value;
            if (ft.StartsWith("Podium")) return (double)numPodiumHeight.Value;
            if (ft == "Ground") return (double)numGroundHeight.Value;
            if (ft == "EDeck") return (double)numEDeckHeight.Value;
            if (ft == "Typical") return (double)numTypicalHeight.Value;
            if (ft == "Terrace") return 3.0;   // fixed height — no UI control
            return 3.0;
        }

        private bool AddFloorConfig(string name, int count, double height)
        {
            if (!ValidateFloorConfig(name))
            {
                MessageBox.Show($"Please configure {name} CAD file and layer mappings.", "Validation Error");
                return false;
            }

            bool isBasement = false, isPodium = false;
            int bNum = 0, pNum = 0;
            if (name.StartsWith("Basement") && name.Length > 8)
                isBasement = int.TryParse(name.Substring(8), out bNum);
            if (name.StartsWith("Podium") && name.Length > 6)
                isPodium = int.TryParse(name.Substring(6), out pNum);

            FloorConfigs.Add(new FloorTypeConfig
            {
                Name = name,
                Count = count,
                Height = height,
                IsIndividualBasement = isBasement,
                BasementNumber = bNum,
                IsIndividualPodium = isPodium,
                PodiumNumber = pNum,
                CADFilePath = cadPathTextBoxes[name].Text,
                LayerMapping = GetLayerMapping(name),
                BeamDepths = GetBeamDepthsForFloor(name),
                BeamWidthOverrides = GetBeamWidthOverridesForFloor(name),
                BeamWallLoadSets = GetBeamWallLoadSetsForFloor(name),
                SlabThicknesses = GetSlabThicknessesForFloor(name),
                SlabIndividualLoads = GetSlabIndividualLoadsForFloor(name),
                WallThicknessOverrides = GetWallThicknessOverridesForFloor(name),
                NtaWallThickness = (int)numNtaWallThicknessPerFloor[name].Value,
                ColumnB = numColumnBPerFloor.ContainsKey(name) ? (int)numColumnBPerFloor[name].Value : 300,
                ColumnD = numColumnDPerFloor.ContainsKey(name) ? (int)numColumnDPerFloor[name].Value : 450
            });
            return true;
        }

        // ====================================================================
        // DATA COLLECTION HELPERS
        // ====================================================================

        private int SafeGetDepth(Dictionary<string, NumericUpDown> dict, string ft, int fallback)
            => dict.ContainsKey(ft) ? (int)dict[ft].Value : fallback;


        private Dictionary<string, int> GetBeamDepthsForFloor(string ft)
        {
            int gravDepth = (int)numInternalGravityDepthPerFloor[ft].Value;
            return new Dictionary<string, int>
            {
                ["InternalGravity"] = gravDepth,
                ["CantileverGravity"] = (int)numCantileverGravityDepthPerFloor[ft].Value,
                ["NoLoadGravity"] = SafeGetDepth(numNoLoadGravityDepthPerFloor, ft, gravDepth),
                ["EdeckGravity"] = SafeGetDepth(numEDeckGravityDepthPerFloor, ft, gravDepth),
                ["PodiumGravity"] = SafeGetDepth(numPodiumGravityDepthPerFloor, ft, gravDepth),
                ["GroundGravity"] = SafeGetDepth(numGroundGravityDepthPerFloor, ft, gravDepth),
                ["BasementGravity"] = SafeGetDepth(numBasementGravityDepthPerFloor, ft, gravDepth),
                ["CoreMain"] = (int)numCoreMainDepthPerFloor[ft].Value,
                ["PeripheralDeadMain"] = (int)numPeripheralDeadMainDepthPerFloor[ft].Value,
                ["PeripheralPortalMain"] = (int)numPeripheralPortalMainDepthPerFloor[ft].Value,
                ["InternalMain"] = (int)numInternalMainDepthPerFloor[ft].Value,
            };
        }

        /// <summary>
        /// Collects per-variant beam width overrides.
        /// Each gravity beam type has its own width control; 0 = auto (zone default).
        /// Main beams: 0 = use matching wall thickness.
        /// </summary>
        private Dictionary<string, int> GetBeamWidthOverridesForFloor(string ft)
        {
            int gw = GetAutoGravityWidth(); // zone default — used as label hint only
            return new Dictionary<string, int>
            {
                // Gravity variants — each independently overridable
                ["InternalGravityWidth"] = SafeGetWidth(numInternalGravityWidthPerFloor, ft),
                ["CantileverGravityWidth"] = SafeGetWidth(numCantileverGravityWidthPerFloor, ft),
                ["NoLoadGravityWidth"] = SafeGetWidth(numNoLoadGravityWidthPerFloor, ft),
                ["EdeckGravityWidth"] = SafeGetWidth(numEDeckGravityWidthPerFloor, ft),
                ["PodiumGravityWidth"] = SafeGetWidth(numPodiumGravityWidthPerFloor, ft),
                ["GroundGravityWidth"] = SafeGetWidth(numGroundGravityWidthPerFloor, ft),
                ["BasementGravityWidth"] = SafeGetWidth(numBasementGravityWidthPerFloor, ft),
                // Main beams
                ["CoreMainWidth"] = SafeGetWidth(numCoreMainWidthOverridePerFloor, ft),
                ["PeripheralDeadMainWidth"] = SafeGetWidth(numPeripheralDeadMainWidthOverridePerFloor, ft),
                ["PeripheralPortalMainWidth"] = SafeGetWidth(numPeripheralPortalMainWidthOverridePerFloor, ft),
                ["InternalMainWidth"] = SafeGetWidth(numInternalMainWidthOverridePerFloor, ft),
            };
        }

        private int SafeGetWidth(Dictionary<string, NumericUpDown> dict, string ft)
            => dict.ContainsKey(ft) ? (int)dict[ft].Value : 0;

        /// <summary>
        /// Collects beam wall load set names (ETABS load pattern) per beam type.
        /// B-No Load Gravity is always empty string (no wall load).
        /// </summary>
        private Dictionary<string, string> GetBeamWallLoadSetsForFloor(string ft)
        {
            // Shared across all floors — read from Load Sets tab
            string T(TextBox tb) => tb?.Text?.Trim() ?? "WALL LOAD";
            return new Dictionary<string, string>
            {
                ["InternalGravity"] = T(txtSharedInternalGravityLoadSet),
                ["CantileverGravity"] = T(txtSharedCantileverGravityLoadSet),
                ["NoLoadGravity"] = "",
                ["EdeckGravity"] = T(txtSharedEDeckGravityLoadSet),
                ["PodiumGravity"] = T(txtSharedPodiumGravityLoadSet),
                ["GroundGravity"] = T(txtSharedGroundGravityLoadSet),
                ["BasementGravity"] = T(txtSharedBasementGravityLoadSet),
                ["CoreMain"] = T(txtSharedCoreMainLoadSet),
                ["PeripheralDeadMain"] = T(txtSharedPeripheralDeadMainLoadSet),
                ["PeripheralPortalMain"] = T(txtSharedPeripheralPortalMainLoadSet),
                ["InternalMain"] = T(txtSharedInternalMainLoadSet),
            };
        }

        private Dictionary<string, int> GetSlabThicknessesForFloor(string ft)
        {
            return new Dictionary<string, int>
            {
                ["Lobby"] = (int)numLobbySlabThicknessPerFloor[ft].Value,
                ["Stair"] = (int)numStairSlabThicknessPerFloor[ft].Value,
                ["FireTender"] = (int)numFireTenderSlabPerFloor[ft].Value,
                ["OHT"] = (int)numOHTSlabPerFloor[ft].Value,
                ["TerraceFire"] = (int)numTerraceFireSlabPerFloor[ft].Value,
                ["UGT"] = (int)numUGTSlabPerFloor[ft].Value,
                ["Landscape"] = (int)numLandscapeSlabPerFloor[ft].Value,
                ["Swimming"] = (int)numSwimmingSlabPerFloor[ft].Value,
                ["DG"] = (int)numDGSlabPerFloor[ft].Value,
                ["STP"] = (int)numSTPSlabPerFloor[ft].Value,
            };
        }

        /// <summary>
        /// Collects individual load-pattern magnitudes for all slab layers.
        /// Reads from sharedSlabIndividualLoadControls populated in the Load Sets tab.
        /// Falls back to FloorTypeConfig.DefaultSlabIndividualLoads if no UI entry.
        /// </summary>
        private Dictionary<string, SlabLoads> GetSlabIndividualLoadsForFloor(string ft)
        {
            var result = new Dictionary<string, SlabLoads>();
            // Seed defaults
            foreach (var kv in FloorTypeConfig.DefaultSlabIndividualLoads)
                result[kv.Key] = kv.Value;
            // Override with user-edited controls from Load Sets tab
            foreach (var kv in sharedSlabIndividualLoadControls)
            {
                var c = kv.Value; // [FF, Fill, ASDL, LL, LL>3, FireTender, Tree, Machine, WaterTank]
                result[kv.Key] = new SlabLoads(
                    (double)c[0].Value,  // FF
                    (double)c[1].Value,  // Filling
                    (double)c[2].Value,  // ASDL
                    (double)c[3].Value,  // LL
                    (double)c[4].Value,  // LL3
                    (double)c[5].Value,  // FireTender
                    (double)c[6].Value,  // TreeLoad
                    (double)c[7].Value,  // MachineRoom
                    (double)c[8].Value   // WaterTank
                );
            }
            return result;
        }

        private Dictionary<string, int> GetWallThicknessOverridesForFloor(string ft)
        {
            return new Dictionary<string, int>
            {
                ["CoreWall"] = (int)numCoreWallOverridePerFloor[ft].Value,
                ["PeriphDeadWall"] = (int)numPeriphDeadWallOverridePerFloor[ft].Value,
                ["PeriphPortalWall"] = (int)numPeriphPortalWallOverridePerFloor[ft].Value,
                ["InternalWall"] = (int)numInternalWallOverridePerFloor[ft].Value,
            };
        }

        private bool ValidateFloorConfig(string floorType)
        {
            return cadPathTextBoxes.ContainsKey(floorType)
                && !string.IsNullOrEmpty(cadPathTextBoxes[floorType].Text)
                && mappedLayerListBoxes.ContainsKey(floorType)
                && mappedLayerListBoxes[floorType].Items.Count > 0;
        }

        private Dictionary<string, string> GetLayerMapping(string floorType)
        {
            var mapping = new Dictionary<string, string>();
            if (!mappedLayerListBoxes.ContainsKey(floorType)) return mapping;
            foreach (var item in mappedLayerListBoxes[floorType].Items)
            {
                string[] parts = item.ToString().Split(new[] { " → " }, StringSplitOptions.None);
                if (parts.Length == 2) mapping[parts[0]] = parts[1];
            }
            return mapping;
        }

        // ====================================================================
        // CONFIRMATION DIALOG
        // ====================================================================

        private void ShowConfirmation()
        {
            int totalStories = FloorConfigs.Sum(c => c.Count);
            double totalHeight = FloorConfigs.Sum(c => c.Height * c.Count);
            var sb = new System.Text.StringBuilder();

            // ── 1d. Include IS code label in the summary header ───────────
            string codeLabel = SelectedISCode == WallThicknessCalculator.ISCodeVersion.IS2016
                ? "IS 1893:2016" : "IS 1893:2025";

            sb.AppendLine($"{totalStories}F | {totalHeight:F2}m | {SeismicZone} | {codeLabel}" +
                          (chkFoundation.Checked ? $" | Fdn: {FoundationHeight:F2}m" : ""));
            sb.AppendLine($"Types: {string.Join(", ", FloorConfigs.Select(c => $"{c.Name}×{c.Count}"))}");

            sb.AppendLine("\nFLOORS:");
            foreach (var cfg in FloorConfigs)
            {
                // Resolve widths outside the interpolation to avoid ternary ':' conflicts
                int intGravOv = cfg.GetBeamWidthOverride("InternalGravityWidth");
                int cantGravOv = cfg.GetBeamWidthOverride("CantileverGravityWidth");
                int autoGw = GetAutoGravityWidth();
                int gw = intGravOv > 0 ? intGravOv : autoGw;
                int cw = cantGravOv > 0 ? cantGravOv : gw;

                sb.AppendLine(
                    $"  {cfg.Name}: G={gw}×{cfg.BeamDepths["InternalGravity"]} " +
                    $"C={cw}×{cfg.BeamDepths["CantileverGravity"]} " +
                    $"MB={cfg.BeamDepths["CoreMain"]} NTA={cfg.NtaWallThickness} " +
                    $"Col={cfg.ColumnB}×{cfg.ColumnD}mm " +
                    $"Slabs={cfg.SlabThicknesses["Lobby"]}/{cfg.SlabThicknesses["Stair"]}/" +
                    $"{cfg.SlabThicknesses["UGT"]}/{cfg.SlabThicknesses["Swimming"]}mm");
            }

            sb.AppendLine("\nGRADES:");
            int f = 1;
            for (int i = 0; i < WallGrades.Count; i++)
            {
                int end = f + FloorsPerGrade[i] - 1;
                sb.AppendLine($"  F{f}-{end}: {WallGrades[i]}/{CalculateBeamSlabGrade(WallGrades[i])}");
                f = end + 1;
            }

            if (MessageBox.Show(sb.ToString(), "Confirm Import",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { this.DialogResult = DialogResult.OK; this.Close(); }
        }

        private int GetAutoGravityWidth()
        {
            string zone = cmbSeismicZone.SelectedItem?.ToString() ?? "";
            return (zone.Contains("II") || zone.Contains("III")) ? 200 : 240;
        }
    }
}
// ============================================================================
// END OF PART 1
// ============================================================================
