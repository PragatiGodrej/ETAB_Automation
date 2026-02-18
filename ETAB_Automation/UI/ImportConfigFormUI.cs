//// ============================================================================
//// FILE: UI/ImportConfigFormUI.cs (PART 2 - UI Initialization)
//// ============================================================================
//// PURPOSE: UI initialization and tab creation for ImportConfigForm
//// AUTHOR: ETAB Automation Team
//// VERSION: 2.1 (Split into 2 parts)
//// ============================================================================

//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;

//namespace ETAB_Automation
//{
//    /// <summary>
//    /// Part 2: UI initialization methods
//    /// This is a partial class that extends ImportConfigForm
//    /// </summary>
//    public partial class ImportConfigForm
//    {
//        // ====================================================================
//        // TOOLTIP COMPONENT
//        // ====================================================================

//        private ToolTip toolTip;

//        // ====================================================================
//        // MAIN UI INITIALIZATION
//        // ====================================================================

//        /// <summary>
//        /// Initialize all UI controls and tabs
//        /// Called from constructor in Part 1
//        /// </summary>
//        internal void InitializeControlsUI()
//        {
//            // Initialize tooltip
//            toolTip = new ToolTip
//            {
//                AutoPopDelay = 5000,
//                InitialDelay = 500,
//                ReshowDelay = 200,
//                ShowAlways = true
//            };

//            // Set form properties
//            this.Size = new System.Drawing.Size(900, 750);
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.Text = "Import CAD & Configure Building - Multi-Floor Types";

//            // Create main tab control
//            tabControl = new TabControl
//            {
//                Location = new System.Drawing.Point(10, 10),
//                Size = new System.Drawing.Size(870, 630)
//            };
//            this.Controls.Add(tabControl);

//            // Tab 1: Building Configuration
//            TabPage tabBuilding = new TabPage("Building Configuration");
//            tabControl.TabPages.Add(tabBuilding);
//            InitializeBuildingConfigTab(tabBuilding);

//            // Tab 2: Concrete Grade Schedule
//            TabPage tabGradeSchedule = new TabPage("Concrete Grades");
//            tabControl.TabPages.Add(tabGradeSchedule);
//            InitializeGradeScheduleTab(tabGradeSchedule);

//            // Action buttons
//            btnImport = new Button
//            {
//                Text = "Import to ETABS",
//                Location = new System.Drawing.Point(600, 660),
//                Size = new System.Drawing.Size(140, 40),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
//            };
//            btnImport.Click += BtnImport_Click;
//            this.Controls.Add(btnImport);

//            btnCancel = new Button
//            {
//                Text = "Cancel",
//                Location = new System.Drawing.Point(750, 660),
//                Size = new System.Drawing.Size(130, 40),
//                DialogResult = DialogResult.Cancel
//            };
//            this.Controls.Add(btnCancel);
//            this.CancelButton = btnCancel;
//        }

//        // ====================================================================
//        // BUILDING CONFIGURATION TAB
//        // ====================================================================

//        private void InitializeBuildingConfigTab(TabPage tab)
//        {
//            tab.AutoScroll = true;
//            int y = 20;

//            // Header
//            AddLabel(tab, "📋 Define building structure: Basement → Podium → E-Deck → Typical", 
//                20, y, 800, 25, bold: true, color: System.Drawing.Color.DarkBlue);
//            y += 35;


//            //Foundation
//            var grpFoundation = AddGroupBox(tab, "Foundation", 20, y, 820, 90);
//            chkFoundation = AddCheckBox(grpFoundation, "Include Basement to Foundation height", 20, 25);
//            chkFoundation.CheckedChanged += ChkFoundation_CheckedChanged;
//            AddLabel(grpFoundation, "Foundation Height (m):", 40, 52);
//            numFoundationHeight = AddNumeric(grpFoundation, 200, 50, 0.5M, 5.0M, 1.5M, decimals: 2, enabled: false);
//            AddLabel(grpFoundation, "(Distance from Basement bottom to foundation level)", 290, 52, 500,20,
//                italic: true, color: System.Drawing.Color.Gray);
//            y += 100;


//            // Basement
//            var grpBasement = AddGroupBox(tab, "Basement Floors", 20, y, 820, 120);
//            chkBasement = AddCheckBox(grpBasement, "Include Basement Floors", 20, 25);
//            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;

//            AddLabel(grpBasement, "Number of Basements(1-5):", 40, 52);

//            numBasementLevels = AddNumeric(grpBasement, 230, 50, 1, 5,1, enabled: false);
//            numBasementLevels.ValueChanged += NumBasementLevels_ValueChanged;
//            AddLabel(grpBasement, "Each Basement Height (m):", 340, 52);
//            numBasementHeight = AddNumeric(grpBasement, 530, 50, 2.5M, 6.0M, 3.5M, 
//                decimals: 2, enabled: false);
//            y += 100;

//            // Podium
//            var grpPodium = AddGroupBox(tab, "Podium Floors", 20, y, 820, 90);
//            chkPodium = AddCheckBox(grpPodium, "Include Podium Floors", 20, 25);
//            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
//            AddLabel(grpPodium, "Number of Podiums:", 40, 52);
//            numPodiumLevels = AddNumeric(grpPodium, 200, 50, 1, 5, 1, enabled: false);
//            AddLabel(grpPodium, "Podium Height (m):", 320, 52);
//            numPodiumHeight = AddNumeric(grpPodium, 480, 50, 3.0M, 8.0M, 4.5M, 
//                decimals: 2, enabled: false);
//            y += 100;

//            // E-Deck
//            var grpEDeck = AddGroupBox(tab, "E-Deck (Ground Floor)", 20, y, 820, 70);
//            AddLabel(grpEDeck, "E-Deck Height (m):", 40, 32);
//            numEDeckHeight = AddNumeric(grpEDeck, 200, 30, 3.0M, 10.0M, 4.5M, decimals: 2);
//            AddLabel(grpEDeck, "(Ground floor is mandatory)", 290, 32, 
//                italic: true, color: System.Drawing.Color.Gray);
//            y += 80;

//            // Typical
//            var grpTypical = AddGroupBox(tab, "Typical Floors (Above E-Deck)", 20, y, 820, 90);
//            AddLabel(grpTypical, "Number of Typical Floors:", 40, 32);
//            numTypicalLevels = AddNumeric(grpTypical, 210, 30, 1, 100, 10);
//            AddLabel(grpTypical, "Typical Floor Height (m):", 320, 32);
//            numTypicalHeight = AddNumeric(grpTypical, 490, 30, 2.8M, 5.0M, 3.0M, decimals: 2);
//            y += 100;

//            // Terrace
//            var grpTerrace = AddGroupBox(tab, "Terrace Floor", 20, y, 820, 90);
//            chkTerrace = AddCheckBox(grpTerrace, "Include Terrace Floor", 20, 25);
//            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
//            AddLabel(grpTerrace, "Terrace Height (m):", 40, 52);
//            numTerraceheight = AddNumeric(grpTerrace, 200, 50, 2.8M, 5.0M, 3.0M, 
//                decimals: 2, enabled: false);
//            y += 100;

//            // Seismic Zone
//            var grpSeismic = AddGroupBox(tab, "Seismic Parameters", 20, y, 820, 70);
//            AddLabel(grpSeismic, "Seismic Zone:", 40, 32, 120, 20);

//            cmbSeismicZone = new ComboBox
//            {
//                Location = new System.Drawing.Point(170, 30),
//                Size = new System.Drawing.Size(150, 25),
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cmbSeismicZone.Items.AddRange(new object[] { "Zone II", "Zone III", "Zone IV", "Zone V" });
//            cmbSeismicZone.SelectedIndex = 2;
//            grpSeismic.Controls.Add(cmbSeismicZone);

//            AddLabel(grpSeismic, "Affects gravity beam width: Zone II/III = 200mm, Zone IV/V = 240mm", 
//                330, 32, 470, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);
//            y += 80;

//            // Generate Tabs Button
//            Button btnGen = new Button
//            {
//                Text = "Generate CAD Import Tabs →",
//                Location = new System.Drawing.Point(320, y),
//                Size = new System.Drawing.Size(200, 40),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
//                BackColor = System.Drawing.Color.LightGreen
//            };
//            btnGen.Click += BtnGenerateTabs_Click;
//            tab.Controls.Add(btnGen);
//        }

//        // ====================================================================
//        // GRADE SCHEDULE TAB
//        // ====================================================================

//        private void InitializeGradeScheduleTab(TabPage tab)
//        {
//            tab.AutoScroll = true;
//            int y = 20;

//            // Header
//            AddLabel(tab, "🏗️ CONCRETE GRADE SCHEDULE - Define wall grades from bottom to top", 
//                20, y, 800, 25, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 10);
//            y += 35;

//            // Note
//            AddLabel(tab, "⚠️ Total floors in grade schedule MUST equal total building floors\n" +
//                "Beam/Slab grades are auto-calculated as 0.7× wall grade (rounded to nearest 5)", 
//                20, y, 800, 35, italic: true, color: System.Drawing.Color.DarkRed);
//            y += 50;

//            // Total floors
//            AddLabel(tab, "Total Building Floors:", 20, y, bold: true);
//            numTotalFloors = new NumericUpDown
//            {
//                Location = new System.Drawing.Point(180, y),
//                Size = new System.Drawing.Size(80, 25),
//                ReadOnly = true,
//                Enabled = false,
//                Value = 0,
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
//            };
//            tab.Controls.Add(numTotalFloors);
//            AddLabel(tab, "(Auto-calculated from Building Configuration tab)", 
//                270, y + 2, italic: true, color: System.Drawing.Color.Gray);
//            y += 40;

//            // DataGrid
//            dgvGradeSchedule = new DataGridView
//            {
//                Location = new System.Drawing.Point(20, y),
//                Size = new System.Drawing.Size(820, 300),
//                AllowUserToAddRows = false,
//                AllowUserToDeleteRows = false,
//                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                MultiSelect = false,
//                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
//            };

//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "Index",
//                HeaderText = "Row",
//                ReadOnly = true,
//                Width = 60
//            });

//            dgvGradeSchedule.Columns.Add(new DataGridViewComboBoxColumn
//            {
//                Name = "WallGrade",
//                HeaderText = "Wall Concrete Grade from bottom",
//                DataSource = new List<string> { "M20", "M25", "M30", "M35", "M40", "M45", "M50", "M55", "M60" },
//                Width = 200
//            });

//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "FloorsCount",
//                HeaderText = "No. of floors Concrete Grade from bottom",
//                Width = 250
//            });

//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "BeamSlabGrade",
//                HeaderText = "Beam/Slab Grade (Auto)",
//                ReadOnly = true,
//                Width = 150
//            });

//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "FloorRange",
//                HeaderText = "Floor Range",
//                ReadOnly = true,
//                Width = 150
//            });

//            dgvGradeSchedule.CellValueChanged += DgvGradeSchedule_CellValueChanged;
//            dgvGradeSchedule.CurrentCellDirtyStateChanged += (s, e) =>
//            {
//                if (dgvGradeSchedule.IsCurrentCellDirty)
//                    dgvGradeSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
//            };

//            tab.Controls.Add(dgvGradeSchedule);
//            y += 310;

//            // Buttons
//            btnAddGradeRow = new Button
//            {
//                Text = "➕ Add Grade Row",
//                Location = new System.Drawing.Point(20, y),
//                Size = new System.Drawing.Size(150, 35),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
//            };
//            btnAddGradeRow.Click += BtnAddGradeRow_Click;
//            tab.Controls.Add(btnAddGradeRow);

//            btnRemoveGradeRow = new Button
//            {
//                Text = "➖ Remove Selected Row",
//                Location = new System.Drawing.Point(180, y),
//                Size = new System.Drawing.Size(170, 35)
//            };
//            btnRemoveGradeRow.Click += BtnRemoveGradeRow_Click;
//            tab.Controls.Add(btnRemoveGradeRow);

//            lblGradeTotal = new Label
//            {
//                Text = "Total floors in schedule: 0 / 0",
//                Location = new System.Drawing.Point(370, y + 8),
//                Size = new System.Drawing.Size(400, 25),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
//                ForeColor = System.Drawing.Color.DarkRed
//            };
//            tab.Controls.Add(lblGradeTotal);

//            UpdateTotalFloorsForGradeSchedule();
//        }

//        // ====================================================================
//        // DYNAMIC CAD IMPORT TAB CREATION
//        // ====================================================================

//        internal void CreateCADImportTab(string floorType, string description)
//        {
//            TabPage tab = new TabPage($"{floorType} - CAD Import");
//            tab.AutoScroll = true;
//            tabControl.TabPages.Add(tab);

//            int y = 10;

//            AddLabel(tab, $"📐 {description}", 20, y, 800, 25, bold: true, 
//                color: System.Drawing.Color.DarkGreen);
//            y += 35;

//            // CAD File
//            AddLabel(tab, "CAD File:", 20, y);
//            TextBox txtCAD = new TextBox
//            {
//                Location = new System.Drawing.Point(120, y - 2),
//                Size = new System.Drawing.Size(540, 25),
//                ReadOnly = true
//            };
//            tab.Controls.Add(txtCAD);
//            cadPathTextBoxes[floorType] = txtCAD;

//            Button btnLoad = new Button
//            {
//                Text = "Browse...",
//                Location = new System.Drawing.Point(670, y - 4),
//                Size = new System.Drawing.Size(120, 28)
//            };
//            btnLoad.Click += (s, ev) => BtnLoadCAD_Click(floorType);
//            tab.Controls.Add(btnLoad);
//            y += 40;

//            // Layer Mapping
//            AddLayerMappingUI(tab, floorType, ref y);

//            // Beam Depths
//            AddBeamDepthsUI(tab, floorType, ref y);

//            // Slab Thicknesses
//            AddSlabThicknessesUI(tab, floorType, ref y);
//        }

//        private void AddLayerMappingUI(TabPage tab, string floorType, ref int y)
//        {
//            AddLabel(tab, "Available CAD Layers:", 20, y);

//            ListBox lstAvail = new ListBox
//            {
//                Location = new System.Drawing.Point(20, y + 25),
//                Size = new System.Drawing.Size(280, 250)
//            };
//            tab.Controls.Add(lstAvail);
//            availableLayerListBoxes[floorType] = lstAvail;

//            AddLabel(tab, "Assign as:", 320, y + 25);

//            ComboBox cboElem = new ComboBox
//            {
//                Location = new System.Drawing.Point(320, y + 50),
//                Size = new System.Drawing.Size(140, 25),
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cboElem.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Ignore" });
//            cboElem.SelectedIndex = 0;
//            tab.Controls.Add(cboElem);
//            elementTypeComboBoxes[floorType] = cboElem;

//            Button btnAdd = new Button
//            {
//                Text = "Add →",
//                Location = new System.Drawing.Point(320, y + 85),
//                Size = new System.Drawing.Size(140, 35)
//            };
//            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
//            tab.Controls.Add(btnAdd);

//            Button btnRem = new Button
//            {
//                Text = "← Remove",
//                Location = new System.Drawing.Point(320, y + 130),
//                Size = new System.Drawing.Size(140, 35)
//            };
//            btnRem.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
//            tab.Controls.Add(btnRem);

//            AddLabel(tab, "Layer Mappings:", 480, y);

//            ListBox lstMap = new ListBox
//            {
//                Location = new System.Drawing.Point(480, y + 25),
//                Size = new System.Drawing.Size(310, 250)
//            };
//            tab.Controls.Add(lstMap);
//            mappedLayerListBoxes[floorType] = lstMap;

//            y += 290;
//        }

//		private void AddBeamDepthsUI(TabPage tab, string floorType, ref int y)
//		{
//			var grp = AddGroupBox(tab, $"🔧 Beam Depths for {floorType} (mm)", 20, y, 820, 180);

//			int gw = (cmbSeismicZone.SelectedItem?.ToString() == "Zone II" ||
//					 cmbSeismicZone.SelectedItem?.ToString() == "Zone III") ? 200 : 240;

//			AddLabel(grp, $"Gravity Beam Width: {gw}mm (auto) | Main Beam Width: Matches wall",
//				15, 20, 790, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

//			// Column 1 - Gravity beams
//			var lblIntGrav = new Label
//			{
//				Text = "Internal Gravity:",
//				Location = new System.Drawing.Point(20, 50),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblIntGrav);
//			numInternalGravityDepthPerFloor[floorType] =
//				AddNumeric(grp, 175, 48, 200, 1000, 450, increment: 25);

//			var lblCantGrav = new Label
//			{
//				Text = "Cantilever Gravity:",
//				Location = new System.Drawing.Point(20, 85),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblCantGrav);
//			numCantileverGravityDepthPerFloor[floorType] =
//				AddNumeric(grp, 175, 83, 200, 1000, 500, increment: 25);

//			// Column 2 - Main beams (part 1)
//			var lblCoreMain = new Label
//			{
//				Text = "Core Main:",
//				Location = new System.Drawing.Point(300, 50),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblCoreMain);
//			numCoreMainDepthPerFloor[floorType] =
//				AddNumeric(grp, 425, 48, 300, 1500, 600, increment: 25);

//			var lblPeriDead = new Label
//			{
//				Text = "Peripheral Dead:",
//				Location = new System.Drawing.Point(300, 85),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblPeriDead);
//			numPeripheralDeadMainDepthPerFloor[floorType] =
//				AddNumeric(grp, 425, 83, 300, 1500, 600, increment: 25);

//			// Column 3 - Main beams (part 2)
//			var lblPeriPortal = new Label
//			{
//				Text = "Peripheral Portal:",
//				Location = new System.Drawing.Point(550, 50),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblPeriPortal);
//			numPeripheralPortalMainDepthPerFloor[floorType] =
//				AddNumeric(grp, 675, 48, 300, 1500, 650, increment: 25);

//			var lblIntMain = new Label
//			{
//				Text = "Internal Main:",
//				Location = new System.Drawing.Point(550, 85),
//				AutoSize = true
//			};
//			grp.Controls.Add(lblIntMain);
//			numInternalMainDepthPerFloor[floorType] =
//				AddNumeric(grp, 675, 83, 300, 1500, 550, increment: 25);

//			AddLabel(grp, "💡 All depths in mm. Widths auto-set based on type and seismic zone.",
//				20, 130, 780, 35, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 8);

//			y += 190;
//		}
//		private void AddSlabThicknessesUI(TabPage tab, string floorType, ref int y)
//        {
//            var grp = AddGroupBox(tab, $"🔧 Slab Thicknesses for {floorType} (mm)", 20, y, 820, 120);

//            AddLabel(grp, "Regular slabs auto-determined by area (14-70 m²). Configure special cases below:", 
//                15, 20, 790, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

//            AddLabel(grp, "Lobby Slab:", 40, 50);
//            numLobbySlabThicknessPerFloor[floorType] = 
//                AddNumeric(grp, 195, 48, 100, 300, 160, increment: 5);

//            AddLabel(grp, "Stair Slab:", 320, 50);
//            numStairSlabThicknessPerFloor[floorType] = 
//                AddNumeric(grp, 475, 48, 125, 250, 175, increment: 5);

//            AddLabel(grp, "💡 Cantilever slabs use span-based rules (1.0-5.0m → 125-200mm)", 
//                40, 85, 750, 20, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 8);
//        }

//        private void BtnGenerateTabs_Click(object sender, EventArgs e)
//        {
//            // Remove old tabs
//            while (tabControl.TabPages.Count > 2)
//                tabControl.TabPages.RemoveAt(2);

//            // Clear dictionaries
//            cadPathTextBoxes.Clear();
//            availableLayerListBoxes.Clear();
//            mappedLayerListBoxes.Clear();
//            elementTypeComboBoxes.Clear();
//            numInternalGravityDepthPerFloor.Clear();
//            numCantileverGravityDepthPerFloor.Clear();
//            numCoreMainDepthPerFloor.Clear();
//            numPeripheralDeadMainDepthPerFloor.Clear();
//            numPeripheralPortalMainDepthPerFloor.Clear();
//            numInternalMainDepthPerFloor.Clear();
//            numLobbySlabThicknessPerFloor.Clear();
//            numStairSlabThicknessPerFloor.Clear();

//            // Generate tabs
//            if (chkBasement.Checked)
//                CreateCADImportTab("Basement", "Basement Floor Plan");

//            if (chkPodium.Checked)
//                CreateCADImportTab("Podium", "Podium Floor Plan");

//            CreateCADImportTab("EDeck", "E-Deck (Ground) Floor Plan");
//            CreateCADImportTab("Typical", "Typical Floor Plan (Will be replicated)");

//            if (chkTerrace.Checked)
//                CreateCADImportTab("Terrace", "Terrace Floor Plan");

//            UpdateTotalFloorsForGradeSchedule();

//            MessageBox.Show(
//                "CAD Import tabs generated!\n\n" +
//                "Each floor type has its own beam and slab configuration.\n\n" +
//                "Please:\n" +
//                "1. Upload CAD files and map layers\n" +
//                "2. Configure beam depths and slab thicknesses\n" +
//                "3. Complete Concrete Grades schedule",
//                "Tabs Generated",
//                MessageBoxButtons.OK,
//                MessageBoxIcon.Information);
//        }

//        // ====================================================================
//        // UI HELPER METHODS
//        // ====================================================================

//        private Label AddLabel(Control parent, string text, int x, int y, 
//            int width = 150, int height = 20, bool bold = false, bool italic = false, 
//            System.Drawing.Color? color = null, float fontSize = 9F)
//        {
//            var style = System.Drawing.FontStyle.Regular;
//            if (bold) style |= System.Drawing.FontStyle.Bold;
//            if (italic) style |= System.Drawing.FontStyle.Italic;

//            var lbl = new Label
//            {
//                Text = text,
//                Location = new System.Drawing.Point(x, y),
//                Size = new System.Drawing.Size(width, height),
//                Font = new System.Drawing.Font("Segoe UI", fontSize, style)
//            };

//            if (color.HasValue)
//                lbl.ForeColor = color.Value;

//            parent.Controls.Add(lbl);
//            return lbl;
//        }

//        private GroupBox AddGroupBox(Control parent, string text, int x, int y, 
//            int width, int height)
//        {
//            var grp = new GroupBox
//            {
//                Text = text,
//                Location = new System.Drawing.Point(x, y),
//                Size = new System.Drawing.Size(width, height)
//            };
//            parent.Controls.Add(grp);
//            return grp;
//        }

//        private CheckBox AddCheckBox(Control parent, string text, int x, int y)
//        {
//            var chk = new CheckBox
//            {
//                Text = text,
//                Location = new System.Drawing.Point(x, y),
//                Size = new System.Drawing.Size(200, 20)
//            };
//            parent.Controls.Add(chk);
//            return chk;
//        }

//        private NumericUpDown AddNumeric(Control parent, int x, int y, 
//            decimal min, decimal max, decimal value, int decimals = 0, 
//            decimal increment = 1, bool enabled = true)
//        {
//            var num = new NumericUpDown
//            {
//                Location = new System.Drawing.Point(x, y),
//                Size = new System.Drawing.Size(80, 25),
//                Minimum = min,
//                Maximum = max,
//                Value = value,
//                DecimalPlaces = decimals,
//                Increment = increment,
//                Enabled = enabled
//            };
//            parent.Controls.Add(num);
//            return num;
//        }
//    }
//}

//// ============================================================================
//// END OF PART 2
//// ============================================================================
// ============================================================================
// FILE: UI/ImportConfigFormUI.cs (PART 2 - UI Initialization)
// ============================================================================
// PURPOSE: UI initialization and tab creation for ImportConfigForm
// AUTHOR: ETAB Automation Team
// VERSION: 2.2 (Updated for individual basement floors and ground floor)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ETAB_Automation
{
    /// <summary>
    /// Part 2: UI initialization methods
    /// This is a partial class that extends ImportConfigForm
    /// </summary>
    public partial class ImportConfigForm
    {
        // ====================================================================
        // TOOLTIP COMPONENT
        // ====================================================================

        private ToolTip toolTip;

        // ====================================================================
        // MAIN UI INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize all UI controls and tabs
        /// Called from constructor in Part 1
        /// </summary>
        internal void InitializeControlsUI()
        {
            // Initialize tooltip
            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 200,
                ShowAlways = true
            };

            // Set form properties
            this.Size = new System.Drawing.Size(900, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Import CAD & Configure Building - Multi-Floor Types";

            // Create main tab control
            tabControl = new TabControl
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(870, 630)
            };
            this.Controls.Add(tabControl);

            // Tab 1: Building Configuration
            TabPage tabBuilding = new TabPage("Building Configuration");
            tabControl.TabPages.Add(tabBuilding);
            InitializeBuildingConfigTab(tabBuilding);

            // Tab 2: Concrete Grade Schedule
            TabPage tabGradeSchedule = new TabPage("Concrete Grades");
            tabControl.TabPages.Add(tabGradeSchedule);
            InitializeGradeScheduleTab(tabGradeSchedule);

            // Action buttons
            btnImport = new Button
            {
                Text = "Import to ETABS",
                Location = new System.Drawing.Point(600, 660),
                Size = new System.Drawing.Size(140, 40),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            btnImport.Click += BtnImport_Click;
            this.Controls.Add(btnImport);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(750, 660),
                Size = new System.Drawing.Size(130, 40),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;
        }

        // ====================================================================
        // BUILDING CONFIGURATION TAB
        // ====================================================================

        private void InitializeBuildingConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;

            // Header
            AddLabel(tab, "📋 Define building structure from bottom to top (all floors are optional)",
                20, y, 800, 25, bold: true, color: System.Drawing.Color.DarkBlue);
            y += 35;

            // Foundation
            var grpFoundation = AddGroupBox(tab, "Foundation", 20, y, 820, 90);
            chkFoundation = AddCheckBox(grpFoundation, "Include Basement to Foundation Height", 20, 25);
            chkFoundation.CheckedChanged += ChkFoundation_CheckedChanged;
            AddLabel(grpFoundation, "Foundation Height (m):", 40, 52);
            numFoundationHeight = AddNumeric(grpFoundation, 200, 50, 0.5M, 5.0M, 1.5M,
                decimals: 2, enabled: false);
            AddLabel(grpFoundation, "(Distance from basement bottom to foundation level)",
                290, 52, 500, 20, italic: true, color: System.Drawing.Color.Gray);
            y += 100;

            // Basement (Individual floors)
            var grpBasement = AddGroupBox(tab, "Basement Floors (Individual Plans)", 20, y, 820, 120);
            chkBasement = AddCheckBox(grpBasement, "Include Basement Floors", 20, 25);
            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;
            AddLabel(grpBasement, "Number of Basements (1-5):", 40, 52);
            numBasementLevels = AddNumeric(grpBasement, 230, 50, 1, 5, 1, enabled: false);
            numBasementLevels.ValueChanged += NumBasementLevels_ValueChanged;
            AddLabel(grpBasement, "Each Basement Height (m):", 340, 52);
            numBasementHeight = AddNumeric(grpBasement, 530, 50, 2.5M, 6.0M, 3.5M,
                decimals: 2, enabled: false);
            AddLabel(grpBasement, "⚠️ Each basement floor requires its own CAD drawing (B1, B2, B3, etc.)",
                40, 85, 750, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
            y += 130;

            // Ground Floor
            var grpGround = AddGroupBox(tab, "Ground Floor", 20, y, 820, 90);
            chkGround = AddCheckBox(grpGround, "Include Ground Floor", 20, 25);
            chkGround.CheckedChanged += ChkGround_CheckedChanged;
            AddLabel(grpGround, "Ground Floor Height (m):", 40, 52);
            numGroundHeight = AddNumeric(grpGround, 230, 50, 3.0M, 10.0M, 4.0M,
                decimals: 2, enabled: false);
            AddLabel(grpGround, "(Separate from E-Deck if both are used)",
                320, 52, 480, 20, italic: true, color: System.Drawing.Color.Gray);
            y += 100;

            // Podium
            var grpPodium = AddGroupBox(tab, "Podium Floors", 20, y, 820, 90);
            chkPodium = AddCheckBox(grpPodium, "Include Podium Floors", 20, 25);
            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
            AddLabel(grpPodium, "Number of Podiums:", 40, 52);
            numPodiumLevels = AddNumeric(grpPodium, 200, 50, 1, 5, 1, enabled: false);
            numPodiumLevels.ValueChanged += NumPodiumLevels_ValueChanged;
            AddLabel(grpPodium, "Podium Height (m):", 320, 52);
            numPodiumHeight = AddNumeric(grpPodium, 480, 50, 3.0M, 8.0M, 4.5M,
                decimals: 2, enabled: false);
            y += 100;

            // E-Deck
            var grpEDeck = AddGroupBox(tab, "E-Deck Floor", 20, y, 820, 90);
            chkEDeck = AddCheckBox(grpEDeck, "Include E-Deck Floor", 20, 25);
            chkEDeck.CheckedChanged += ChkEDeck_CheckedChanged;
            AddLabel(grpEDeck, "E-Deck Height (m):", 40, 52);
            numEDeckHeight = AddNumeric(grpEDeck, 200, 50, 3.0M, 10.0M, 4.5M,
                decimals: 2, enabled: false);
            AddLabel(grpEDeck, "(Can be used separately or with Ground Floor)",
                290, 52, 500, 20, italic: true, color: System.Drawing.Color.Gray);
            y += 100;

            // Typical
            var grpTypical = AddGroupBox(tab, "Typical Floors", 20, y, 820, 90);
            chkTypical = AddCheckBox(grpTypical, "Include Typical Floors", 20, 25);
            chkTypical.CheckedChanged += ChkTypical_CheckedChanged;
            AddLabel(grpTypical, "Number of Typical Floors:", 40, 52);
            numTypicalLevels = AddNumeric(grpTypical, 210, 50, 1, 100, 10, enabled: false);
            numTypicalLevels.ValueChanged += NumTypicalLevels_ValueChanged;
            AddLabel(grpTypical, "Typical Floor Height (m):", 320, 52);
            numTypicalHeight = AddNumeric(grpTypical, 490, 50, 2.8M, 5.0M, 3.0M,
                decimals: 2, enabled: false);
            y += 100;

            // Terrace
            var grpTerrace = AddGroupBox(tab, "Terrace Floor", 20, y, 820, 90);
            chkTerrace = AddCheckBox(grpTerrace, "Include Terrace Floor", 20, 25);
            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
            AddLabel(grpTerrace, "Terrace Height (m):", 40, 52);
            numTerraceheight = AddNumeric(grpTerrace, 200, 50, 0M, 5.0M, 0M,
                decimals: 2, enabled: false);
            y += 100;

            // Seismic Zone
            var grpSeismic = AddGroupBox(tab, "Seismic Parameters", 20, y, 820, 70);
            AddLabel(grpSeismic, "Seismic Zone:", 40, 32, 120, 20);

            cmbSeismicZone = new ComboBox
            {
                Location = new System.Drawing.Point(170, 30),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSeismicZone.Items.AddRange(new object[] { "Zone II", "Zone III", "Zone IV", "Zone V" });
            cmbSeismicZone.SelectedIndex = 2;
            grpSeismic.Controls.Add(cmbSeismicZone);

            AddLabel(grpSeismic, "Affects gravity beam width: Zone II/III = 200mm, Zone IV/V = 240mm",
                330, 32, 470, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);
            y += 80;

            // Generate Tabs Button
            Button btnGen = new Button
            {
                Text = "Generate CAD Import Tabs →",
                Location = new System.Drawing.Point(320, y),
                Size = new System.Drawing.Size(200, 40),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGreen
            };
            btnGen.Click += BtnGenerateTabs_Click;
            tab.Controls.Add(btnGen);
        }

        // ====================================================================
        // GRADE SCHEDULE TAB
        // ====================================================================

        private void InitializeGradeScheduleTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;

            // Header
            AddLabel(tab, "🏗️ CONCRETE GRADE SCHEDULE - Define wall grades from bottom to top",
                20, y, 800, 25, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 10);
            y += 35;

            // Note
            AddLabel(tab, "⚠️ Total floors in grade schedule MUST equal total building floors\n" +
                "Beam/Slab grades are auto-calculated as 0.7× wall grade (rounded to nearest 5)",
                20, y, 800, 35, italic: true, color: System.Drawing.Color.DarkRed);
            y += 50;

            // Total floors
            AddLabel(tab, "Total Building Floors:", 20, y, bold: true);
            numTotalFloors = new NumericUpDown
            {
                Location = new System.Drawing.Point(180, y),
                Size = new System.Drawing.Size(80, 25),
                ReadOnly = true,
                Enabled = false,
                Value = 0,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(numTotalFloors);
            AddLabel(tab, "(Auto-calculated from Building Configuration tab)",
                270, y + 2, italic: true, color: System.Drawing.Color.Gray);
            y += 40;

            // DataGrid
            dgvGradeSchedule = new DataGridView
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(820, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Index",
                HeaderText = "Row",
                ReadOnly = true,
                Width = 60
            });

            dgvGradeSchedule.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "WallGrade",
                HeaderText = "Wall Concrete Grade from bottom",
                DataSource = new List<string> { "M20", "M25", "M30", "M35", "M40", "M45", "M50", "M55", "M60" },
                Width = 200
            });

            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FloorsCount",
                HeaderText = "No. of floors Concrete Grade from bottom",
                Width = 250
            });

            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BeamSlabGrade",
                HeaderText = "Beam/Slab Grade (Auto)",
                ReadOnly = true,
                Width = 150
            });

            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FloorRange",
                HeaderText = "Floor Range",
                ReadOnly = true,
                Width = 150
            });

            dgvGradeSchedule.CellValueChanged += DgvGradeSchedule_CellValueChanged;
            dgvGradeSchedule.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvGradeSchedule.IsCurrentCellDirty)
                    dgvGradeSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            tab.Controls.Add(dgvGradeSchedule);
            y += 310;

            // Buttons
            btnAddGradeRow = new Button
            {
                Text = "➕ Add Grade Row",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(150, 35),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            btnAddGradeRow.Click += BtnAddGradeRow_Click;
            tab.Controls.Add(btnAddGradeRow);

            btnRemoveGradeRow = new Button
            {
                Text = "➖ Remove Selected Row",
                Location = new System.Drawing.Point(180, y),
                Size = new System.Drawing.Size(170, 35)
            };
            btnRemoveGradeRow.Click += BtnRemoveGradeRow_Click;
            tab.Controls.Add(btnRemoveGradeRow);

            lblGradeTotal = new Label
            {
                Text = "Total floors in schedule: 0 / 0",
                Location = new System.Drawing.Point(370, y + 8),
                Size = new System.Drawing.Size(400, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkRed
            };
            tab.Controls.Add(lblGradeTotal);

            UpdateTotalFloorsForGradeSchedule();
        }

        // ====================================================================
        // DYNAMIC CAD IMPORT TAB CREATION
        // ====================================================================

        internal void CreateCADImportTab(string floorType, string description)
        {
            TabPage tab = new TabPage($"{floorType} - CAD Import");
            tab.AutoScroll = true;
            tabControl.TabPages.Add(tab);

            int y = 10;

            AddLabel(tab, $"📐 {description}", 20, y, 800, 25, bold: true,
                color: System.Drawing.Color.DarkGreen);
            y += 35;

            // CAD File
            AddLabel(tab, "CAD File:", 20, y);
            TextBox txtCAD = new TextBox
            {
                Location = new System.Drawing.Point(120, y - 2),
                Size = new System.Drawing.Size(540, 25),
                ReadOnly = true
            };
            tab.Controls.Add(txtCAD);
            cadPathTextBoxes[floorType] = txtCAD;

            Button btnLoad = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(670, y - 4),
                Size = new System.Drawing.Size(120, 28)
            };
            btnLoad.Click += (s, ev) => BtnLoadCAD_Click(floorType);
            tab.Controls.Add(btnLoad);
            y += 40;

            // Layer Mapping
            AddLayerMappingUI(tab, floorType, ref y);

            // Beam Depths
            AddBeamDepthsUI(tab, floorType, ref y);

            // Slab Thicknesses
            AddSlabThicknessesUI(tab, floorType, ref y);
        }

        private void AddLayerMappingUI(TabPage tab, string floorType, ref int y)
        {
            AddLabel(tab, "Available CAD Layers:", 20, y);

            ListBox lstAvail = new ListBox
            {
                Location = new System.Drawing.Point(20, y + 25),
                Size = new System.Drawing.Size(280, 250)
            };
            tab.Controls.Add(lstAvail);
            availableLayerListBoxes[floorType] = lstAvail;

            AddLabel(tab, "Assign as:", 320, y + 25);

            ComboBox cboElem = new ComboBox
            {
                Location = new System.Drawing.Point(320, y + 50),
                Size = new System.Drawing.Size(140, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboElem.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Ignore" });
            cboElem.SelectedIndex = 0;
            tab.Controls.Add(cboElem);
            elementTypeComboBoxes[floorType] = cboElem;

            Button btnAdd = new Button
            {
                Text = "Add →",
                Location = new System.Drawing.Point(320, y + 85),
                Size = new System.Drawing.Size(140, 35)
            };
            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
            tab.Controls.Add(btnAdd);

            Button btnRem = new Button
            {
                Text = "← Remove",
                Location = new System.Drawing.Point(320, y + 130),
                Size = new System.Drawing.Size(140, 35)
            };
            btnRem.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
            tab.Controls.Add(btnRem);

            AddLabel(tab, "Layer Mappings:", 480, y);

            ListBox lstMap = new ListBox
            {
                Location = new System.Drawing.Point(480, y + 25),
                Size = new System.Drawing.Size(310, 250)
            };
            tab.Controls.Add(lstMap);
            mappedLayerListBoxes[floorType] = lstMap;

            y += 290;
        }

        private void AddBeamDepthsUI(TabPage tab, string floorType, ref int y)
        {
            var grp = AddGroupBox(tab, $"🔧 Beam Depths for {floorType} (mm)", 20, y, 820, 180);

            int gw = (cmbSeismicZone.SelectedItem?.ToString() == "Zone II" ||
                     cmbSeismicZone.SelectedItem?.ToString() == "Zone III") ? 200 : 240;

            AddLabel(grp, $"Gravity Beam Width: {gw}mm (auto) | Main Beam Width: Matches wall",
                15, 20, 790, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            // Column 1 - Gravity beams
            var lblIntGrav = new Label
            {
                Text = "Internal Gravity:",
                Location = new System.Drawing.Point(20, 50),
                AutoSize = true
            };
            grp.Controls.Add(lblIntGrav);
            numInternalGravityDepthPerFloor[floorType] =
                AddNumeric(grp, 175, 48, 200, 1000, 450, increment: 25);

            var lblCantGrav = new Label
            {
                Text = "Cantilever Gravity:",
                Location = new System.Drawing.Point(20, 85),
                AutoSize = true
            };
            grp.Controls.Add(lblCantGrav);
            numCantileverGravityDepthPerFloor[floorType] =
                AddNumeric(grp, 175, 83, 200, 1000, 500, increment: 25);

            // Column 2 - Main beams (part 1)
            var lblCoreMain = new Label
            {
                Text = "Core Main:",
                Location = new System.Drawing.Point(300, 50),
                AutoSize = true
            };
            grp.Controls.Add(lblCoreMain);
            numCoreMainDepthPerFloor[floorType] =
                AddNumeric(grp, 425, 48, 300, 1500, 600, increment: 25);

            var lblPeriDead = new Label
            {
                Text = "Peripheral Dead:",
                Location = new System.Drawing.Point(300, 85),
                AutoSize = true
            };
            grp.Controls.Add(lblPeriDead);
            numPeripheralDeadMainDepthPerFloor[floorType] =
                AddNumeric(grp, 425, 83, 300, 1500, 600, increment: 25);

            // Column 3 - Main beams (part 2)
            var lblPeriPortal = new Label
            {
                Text = "Peripheral Portal:",
                Location = new System.Drawing.Point(550, 50),
                AutoSize = true
            };
            grp.Controls.Add(lblPeriPortal);
            numPeripheralPortalMainDepthPerFloor[floorType] =
                AddNumeric(grp, 675, 48, 300, 1500, 650, increment: 25);

            var lblIntMain = new Label
            {
                Text = "Internal Main:",
                Location = new System.Drawing.Point(550, 85),
                AutoSize = true
            };
            grp.Controls.Add(lblIntMain);
            numInternalMainDepthPerFloor[floorType] =
                AddNumeric(grp, 675, 83, 300, 1500, 550, increment: 25);

            AddLabel(grp, "💡 All depths in mm. Widths auto-set based on type and seismic zone.",
                20, 130, 780, 35, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 8);

            y += 190;
        }

        private void AddSlabThicknessesUI(TabPage tab, string floorType, ref int y)
        {
            var grp = AddGroupBox(tab, $"🔧 Slab Thicknesses for {floorType} (mm)", 20, y, 820, 120);

            AddLabel(grp, "Regular slabs auto-determined by area (14-70 m²). Configure special cases below:",
                15, 20, 790, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            AddLabel(grp, "Lobby Slab:", 40, 50);
            numLobbySlabThicknessPerFloor[floorType] =
                AddNumeric(grp, 195, 48, 100, 300, 160, increment: 5);

            AddLabel(grp, "Stair Slab:", 320, 50);
            numStairSlabThicknessPerFloor[floorType] =
                AddNumeric(grp, 475, 48, 125, 250, 175, increment: 5);

            AddLabel(grp, "💡 Cantilever slabs use span-based rules (1.0-5.0m → 125-200mm)",
                40, 85, 750, 20, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 8);

            y += 130;
        }

        private void BtnGenerateTabs_Click(object sender, EventArgs e)
        {
            // Validate at least one floor type is selected
            if (!chkBasement.Checked && !chkPodium.Checked && !chkGround.Checked &&
                !chkEDeck.Checked && !chkTypical.Checked && !chkTerrace.Checked)
            {
                MessageBox.Show(
                    "Please select at least one floor type before generating tabs!",
                    "No Floors Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Remove old tabs
            while (tabControl.TabPages.Count > 2)
                tabControl.TabPages.RemoveAt(2);

            // Clear dictionaries
            cadPathTextBoxes.Clear();
            availableLayerListBoxes.Clear();
            mappedLayerListBoxes.Clear();
            elementTypeComboBoxes.Clear();
            numInternalGravityDepthPerFloor.Clear();
            numCantileverGravityDepthPerFloor.Clear();
            numCoreMainDepthPerFloor.Clear();
            numPeripheralDeadMainDepthPerFloor.Clear();
            numPeripheralPortalMainDepthPerFloor.Clear();
            numInternalMainDepthPerFloor.Clear();
            numLobbySlabThicknessPerFloor.Clear();
            numStairSlabThicknessPerFloor.Clear();

            int tabCount = 0;

            // Generate tabs for individual basement floors
            if (chkBasement.Checked)
            {
                int basementCount = (int)numBasementLevels.Value;
                for (int i = 1; i <= basementCount; i++)
                {
                    CreateCADImportTab($"Basement{i}", $"Basement {i} Floor Plan");
                    tabCount++;
                }
            }

            if (chkPodium.Checked)
            {
                CreateCADImportTab("Podium", "Podium Floor Plan");
                tabCount++;
            }

            if (chkGround.Checked)
            {
                CreateCADImportTab("Ground", "Ground Floor Plan");
                tabCount++;
            }

            if (chkEDeck.Checked)
            {
                CreateCADImportTab("EDeck", "E-Deck Floor Plan");
                tabCount++;
            }

            if (chkTypical.Checked)
            {
                CreateCADImportTab("Typical", "Typical Floor Plan (Will be replicated)");
                tabCount++;
            }

            if (chkTerrace.Checked)
            {
                CreateCADImportTab("Terrace", "Terrace Floor Plan");
                tabCount++;
            }

            UpdateTotalFloorsForGradeSchedule();

            string basementNote = chkBasement.Checked
                ? $"\n• {(int)numBasementLevels.Value} individual basement floor tabs created (B1, B2, etc.)"
                : "";

            MessageBox.Show(
                $"✓ {tabCount} CAD Import tab(s) generated!\n" +
                basementNote + "\n\n" +
                "Each floor type has its own beam and slab configuration.\n\n" +
                "Please:\n" +
                "1. Upload CAD files and map layers for each floor\n" +
                "2. Configure beam depths and slab thicknesses\n" +
                "3. Complete Concrete Grades schedule",
                "Tabs Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ====================================================================
        // UI HELPER METHODS
        // ====================================================================

        private Label AddLabel(Control parent, string text, int x, int y,
            int width = 150, int height = 20, bool bold = false, bool italic = false,
            System.Drawing.Color? color = null, float fontSize = 9F)
        {
            var style = System.Drawing.FontStyle.Regular;
            if (bold) style |= System.Drawing.FontStyle.Bold;
            if (italic) style |= System.Drawing.FontStyle.Italic;

            var lbl = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                Font = new System.Drawing.Font("Segoe UI", fontSize, style)
            };

            if (color.HasValue)
                lbl.ForeColor = color.Value;

            parent.Controls.Add(lbl);
            return lbl;
        }

        private GroupBox AddGroupBox(Control parent, string text, int x, int y,
            int width, int height)
        {
            var grp = new GroupBox
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height)
            };
            parent.Controls.Add(grp);
            return grp;
        }

        private CheckBox AddCheckBox(Control parent, string text, int x, int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(250, 20)
            };
            parent.Controls.Add(chk);
            return chk;
        }

        private NumericUpDown AddNumeric(Control parent, int x, int y,
            decimal min, decimal max, decimal value, int decimals = 0,
            decimal increment = 1, bool enabled = true)
        {
            var num = new NumericUpDown
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(80, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                DecimalPlaces = decimals,
                Increment = increment,
                Enabled = enabled
            };
            parent.Controls.Add(num);
            return num;
        }
    }
}

// ============================================================================
// END OF PART 2
// ============================================================================
