
// ============================================================================
// FILE: UI/ImportConfigForm.cs (PART 1 - Main Form)
// ============================================================================
// PURPOSE: Main configuration form class with core logic
// VERSION: 2.5 — Individual Podium tabs (like Basements) + Refuge floor support
// ============================================================================

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
        internal CheckBox chkRefuge;   // NEW

        internal NumericUpDown numBasementLevels;
        internal NumericUpDown numPodiumLevels;
        internal NumericUpDown numTypicalLevels;
        internal NumericUpDown numBasementHeight;
        internal NumericUpDown numPodiumHeight;   // kept for backward compat (shared height)
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

        // ── Per-floor beam WIDTH overrides (0 = auto) ────────────────────
        internal Dictionary<string, NumericUpDown> numGravityWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numCoreMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralDeadMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeripheralPortalMainWidthOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numInternalMainWidthOverridePerFloor;

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

        // ── Per-floor wall thickness overrides ──────────────────────────
        internal Dictionary<string, NumericUpDown> numCoreWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeriphDeadWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numPeriphPortalWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numInternalWallOverridePerFloor;
        internal Dictionary<string, NumericUpDown> numNtaWallThicknessPerFloor;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public ImportConfigForm()
        {
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

            // Width override dicts
            numGravityWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numCoreMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralDeadMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeripheralPortalMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numInternalMainWidthOverridePerFloor = new Dictionary<string, NumericUpDown>();

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

            // Wall override dicts
            numCoreWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeriphDeadWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numPeriphPortalWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numInternalWallOverridePerFloor = new Dictionary<string, NumericUpDown>();
            numNtaWallThicknessPerFloor = new Dictionary<string, NumericUpDown>();

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
            // Refuge floors are carved out of the existing counts — they do NOT
            // add to the total floor count.  The total is already correct.
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
            numTerraceheight.Enabled = chkTerrace.Checked;
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
        // Building sequence (bottom → top):
        //   Basements (individual) → Podiums (individual) → Ground → EDeck
        //   → Typical/Refuge interleaved → Terrace (always last)
        //
        // Refuge is inserted at every absolute position that is a multiple of 5.
        // Terrace is always pinned as the final floor and is exempt from the
        // refuge pattern regardless of whether that position is a multiple of 5.
        // ====================================================================

        private bool CollectFloorConfigs()
        {
            FloorConfigs.Clear();

            // ── Step 1: Build the ordered sequence of floor type names ──────
            // This represents every floor slot from bottom to top (excluding
            // Terrace which is appended last).
            var sequence = new List<string>();

            if (chkBasement.Checked)
            {
                int cnt = (int)numBasementLevels.Value;
                for (int i = 1; i <= cnt; i++)
                    sequence.Add($"Basement{i}");
            }

            if (chkPodium.Checked)
            {
                int cnt = (int)numPodiumLevels.Value;
                for (int i = 1; i <= cnt; i++)
                    sequence.Add($"Podium{i}");
            }

            if (chkGround.Checked) sequence.Add("Ground");
            if (chkEDeck.Checked) sequence.Add("EDeck");

            // Typical floors — will be replaced by Refuge at multiples-of-5
            if (chkTypical.Checked)
            {
                int cnt = (int)numTypicalLevels.Value;
                for (int i = 0; i < cnt; i++)
                    sequence.Add("Typical");
            }

            // ── Step 2: Replace multiples-of-5 with Refuge ──────────────────
            // Position index is 1-based (position 1 = first slot in sequence).
            // Terrace (position = sequence.Count + 1) is always exempt.
            bool hasRefuge = chkRefuge.Checked;

            if (hasRefuge)
            {
                for (int i = 0; i < sequence.Count; i++)
                {
                    int absolutePos = i + 1;  // 1-based
                    if (absolutePos % 5 == 0)
                        sequence[i] = "Refuge";
                }
            }

            // ── Step 3: Terrace always last ───────────────────────────────
            if (chkTerrace.Checked) sequence.Add("Terrace");

            // ── Step 4: Validate CAD configs exist for all required types ──
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

            // ── Step 5: Collapse consecutive same-type floors into one config
            // For shared types (Typical, Refuge, Ground, EDeck, Terrace) we group
            // consecutive runs into one FloorTypeConfig with Count > 1.
            // For individual types (BasementN, PodiumN) every slot is Count=1.
            // ────────────────────────────────────────────────────────────────
            // Individual types (always Count=1, no collapsing)
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
                    // Individual — one config per slot
                    double h = GetHeightForFloorType(ft);
                    if (!AddFloorConfig(ft, 1, h)) return false;
                    idx2++;
                }
                else
                {
                    // Shared type — count consecutive run
                    int run = 1;
                    while (idx2 + run < sequence.Count && sequence[idx2 + run] == ft)
                        run++;

                    double h = GetHeightForFloorType(ft);
                    if (!AddFloorConfig(ft, run, h)) return false;
                    idx2 += run;
                }
            }

            return true;
        }

        /// <summary>Returns the height (m) for a given floor type key.</summary>
        private double GetHeightForFloorType(string ft)
        {
            if (ft.StartsWith("Basement")) return (double)numBasementHeight.Value;
            if (ft.StartsWith("Podium")) return (double)numPodiumHeight.Value;
            if (ft == "Ground") return (double)numGroundHeight.Value;
            if (ft == "EDeck") return (double)numEDeckHeight.Value;
            if (ft == "Typical") return (double)numTypicalHeight.Value;
            if (ft == "Refuge") return (double)numTypicalHeight.Value;  // same height as typical
            if (ft == "Terrace") return (double)numTerraceheight.Value;
            return 3.0;  // fallback
        }

        private bool AddFloorConfig(string name, int count, double height)
        {
            if (!ValidateFloorConfig(name))
            {
                MessageBox.Show($"Please configure {name} CAD file and layer mappings.", "Validation Error");
                return false;
            }

            // Determine if individual basement
            bool isBasement = false;
            int bNum = 0;
            if (name.StartsWith("Basement") && name.Length > 8)
                isBasement = int.TryParse(name.Substring(8), out bNum);

            FloorConfigs.Add(new FloorTypeConfig
            {
                Name = name,
                Count = count,
                Height = height,
                IsIndividualBasement = isBasement,
                BasementNumber = bNum,
                CADFilePath = cadPathTextBoxes[name].Text,
                LayerMapping = GetLayerMapping(name),
                BeamDepths = GetBeamDepthsForFloor(name),
                BeamWidthOverrides = GetBeamWidthOverridesForFloor(name),
                SlabThicknesses = GetSlabThicknessesForFloor(name),
                WallThicknessOverrides = GetWallThicknessOverridesForFloor(name),
                NtaWallThickness = (int)numNtaWallThicknessPerFloor[name].Value
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

        private Dictionary<string, int> GetBeamWidthOverridesForFloor(string ft)
        {
            return new Dictionary<string, int>
            {
                ["GravityWidth"] = (int)numGravityWidthOverridePerFloor[ft].Value,
                ["CoreMainWidth"] = (int)numCoreMainWidthOverridePerFloor[ft].Value,
                ["PeripheralDeadMainWidth"] = (int)numPeripheralDeadMainWidthOverridePerFloor[ft].Value,
                ["PeripheralPortalMainWidth"] = (int)numPeripheralPortalMainWidthOverridePerFloor[ft].Value,
                ["InternalMainWidth"] = (int)numInternalMainWidthOverridePerFloor[ft].Value,
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

            sb.AppendLine($"{totalStories}F | {totalHeight:F2}m | {SeismicZone}" +
                          (chkFoundation.Checked ? $" | Fdn: {FoundationHeight:F2}m" : ""));
            sb.AppendLine($"Types: {string.Join(", ", FloorConfigs.Select(c => $"{c.Name}×{c.Count}"))}");

            if (chkRefuge.Checked)
            {
                int p = 0;
                sb.AppendLine($"Refuge @ {string.Join(", ", FloorConfigs.SelectMany(c =>
                    Enumerable.Range(0, c.Count).Select(_ => (pos: ++p, refuge: c.Name == "Refuge")))
                    .Where(x => x.refuge).Select(x => x.pos))}");
            }

            sb.AppendLine("\nFLOORS:");
            foreach (var cfg in FloorConfigs)
            {
                int gw = cfg.BeamWidthOverrides.GetValueOrDefault("GravityWidth", 0) is > 0 and int ov ? ov : GetAutoGravityWidth();
                sb.AppendLine($"  {cfg.Name}: G={gw}×{cfg.BeamDepths["InternalGravity"]} C={gw}×{cfg.BeamDepths["CantileverGravity"]} " +
                              $"MB={cfg.BeamDepths["CoreMain"]} NTA={cfg.NtaWallThickness} " +
                              $"Slabs={cfg.SlabThicknesses["Lobby"]}/{cfg.SlabThicknesses["Stair"]}/{cfg.SlabThicknesses["UGT"]}/{cfg.SlabThicknesses["Swimming"]}mm");
            }

            sb.AppendLine("\nGRADES:");
            int f = 1;
            for (int i = 0; i < WallGrades.Count; i++)
            {
                int end = f + FloorsPerGrade[i] - 1;
                sb.AppendLine($"  F{f}-{end}: {WallGrades[i]}/{CalculateBeamSlabGrade(WallGrades[i])}");
                f = end + 1;
            }

            if (MessageBox.Show(sb.ToString(), "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
