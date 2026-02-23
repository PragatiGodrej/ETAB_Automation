
//// ============================================================================
//// FILE: UI/ImportConfigFormUI.cs (PART 2 - UI Initialization)
//// ============================================================================
//// PURPOSE: UI initialization and tab creation for ImportConfigForm
//// VERSION: 2.6 — Individual Podium tabs (like Basements) + Refuge floor tab
//// ============================================================================

//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;

//namespace ETAB_Automation
//{
//    public partial class ImportConfigForm
//    {
//        private ToolTip toolTip;

//        // ====================================================================
//        // LAYOUT CONSTANTS
//        // ====================================================================
//        private const int C1L = 20, C1N = 210;
//        private const int C2L = 320, C2N = 510;
//        private const int C3L = 620, C3N = 800;
//        private const int NW = 85;
//        private const int NH = 25;
//        private const int RH = 32;

//        // ====================================================================
//        // MAIN UI INITIALIZATION
//        // ====================================================================

//        internal void InitializeControlsUI()
//        {
//            toolTip = new ToolTip
//            { AutoPopDelay = 5000, InitialDelay = 500, ReshowDelay = 200, ShowAlways = true };

//            this.Size = new System.Drawing.Size(980, 840);
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.Text = "ETABS CAD Import Configuration v2.6";

//            tabControl = new TabControl
//            {
//                Location = new System.Drawing.Point(10, 10),
//                Size = new System.Drawing.Size(950, 730)
//            };
//            this.Controls.Add(tabControl);

//            var tabBuilding = new TabPage("Building Config");
//            tabControl.TabPages.Add(tabBuilding);
//            InitializeBuildingConfigTab(tabBuilding);

//            var tabGrade = new TabPage("Concrete Grades");
//            tabControl.TabPages.Add(tabGrade);
//            InitializeGradeScheduleTab(tabGrade);

//            btnImport = new Button
//            {
//                Text = "▶  Import to ETABS",
//                Location = new System.Drawing.Point(680, 752),
//                Size = new System.Drawing.Size(155, 42),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
//                BackColor = System.Drawing.Color.LightGreen
//            };
//            btnImport.Click += BtnImport_Click;
//            this.Controls.Add(btnImport);

//            btnCancel = new Button
//            {
//                Text = "Cancel",
//                Location = new System.Drawing.Point(845, 752),
//                Size = new System.Drawing.Size(110, 42),
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

//            AddLabel(tab, "📋 Define building structure from bottom to top (all floors are optional)",
//                20, y, 900, 25, bold: true, color: System.Drawing.Color.DarkBlue);
//            y += 35;

//            // Foundation
//            var grpF = AddGroupBox(tab, "Foundation", 20, y, 910, 85);
//            chkFoundation = AddCheckBox(grpF, "Include Foundation to Basement height", 15, 25);
//            chkFoundation.CheckedChanged += ChkFoundation_CheckedChanged;
//            AddLabel(grpF, "Foundation Height (m):", 35, 52, 160, 20);
//            numFoundationHeight = AddNumericCtrl(grpF, 200, 50, 0.5M, 5.0M, 1.5M, decimals: 2, enabled: false);
//            AddLabel(grpF, "(Distance from basement bottom to foundation level)",
//                295, 52, 580, 20, italic: true, color: System.Drawing.Color.Gray);
//            y += 95;

//            // Basement
//            var grpB = AddGroupBox(tab, "Basement Floors  (each floor gets its own CAD tab)", 20, y, 910, 108);
//            chkBasement = AddCheckBox(grpB, "Include Basement Floors", 15, 25);
//            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;
//            AddLabel(grpB, "Number of Basements (1–5):", 35, 52, 200, 20);
//            numBasementLevels = AddNumericCtrl(grpB, 240, 50, 1, 5, 1, enabled: false);
//            numBasementLevels.ValueChanged += NumBasementLevels_ValueChanged;
//            AddLabel(grpB, "Each Basement Height (m):", 345, 52, 195, 20);
//            numBasementHeight = AddNumericCtrl(grpB, 545, 50, 2.5M, 6.0M, 3.5M, decimals: 2, enabled: false);
//            AddLabel(grpB, "⚠️ One CAD tab will be created per basement floor (B1, B2, ...)",
//                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
//            y += 118;

//            // Podium  ── NOW INDIVIDUAL TABS LIKE BASEMENTS ──
//            var grpP = AddGroupBox(tab,
//                "Podium Floors  (each floor gets its own CAD tab — like Basements)", 20, y, 910, 108);
//            chkPodium = AddCheckBox(grpP, "Include Podium Floors", 15, 25);
//            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
//            AddLabel(grpP, "Number of Podiums (1–5):", 35, 52, 200, 20);
//            numPodiumLevels = AddNumericCtrl(grpP, 240, 50, 1, 5, 1, enabled: false);
//            numPodiumLevels.ValueChanged += NumPodiumLevels_ValueChanged;
//            AddLabel(grpP, "Each Podium Height (m):", 345, 52, 185, 20);
//            numPodiumHeight = AddNumericCtrl(grpP, 535, 50, 3.0M, 8.0M, 4.5M, decimals: 2, enabled: false);
//            AddLabel(grpP, "⚠️ One CAD tab will be created per podium floor (P1, P2, ...)",
//                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
//            y += 118;

//            // Ground
//            var grpGr = AddGroupBox(tab, "Ground Floor", 20, y, 910, 82);
//            chkGround = AddCheckBox(grpGr, "Include Ground Floor", 15, 25);
//            chkGround.CheckedChanged += ChkGround_CheckedChanged;
//            AddLabel(grpGr, "Ground Floor Height (m):", 35, 52, 180, 20);
//            numGroundHeight = AddNumericCtrl(grpGr, 220, 50, 3.0M, 10.0M, 4.0M, decimals: 2, enabled: false);
//            y += 92;

//            // E-Deck
//            var grpE = AddGroupBox(tab, "E-Deck Floor", 20, y, 910, 82);
//            chkEDeck = AddCheckBox(grpE, "Include E-Deck Floor", 15, 25);
//            chkEDeck.CheckedChanged += ChkEDeck_CheckedChanged;
//            AddLabel(grpE, "E-Deck Height (m):", 35, 52, 150, 20);
//            numEDeckHeight = AddNumericCtrl(grpE, 190, 50, 3.0M, 10.0M, 4.5M, decimals: 2, enabled: false);
//            y += 92;

//            // Typical
//            var grpT = AddGroupBox(tab, "Typical Floors", 20, y, 910, 82);
//            chkTypical = AddCheckBox(grpT, "Include Typical Floors", 15, 25);
//            chkTypical.CheckedChanged += ChkTypical_CheckedChanged;
//            AddLabel(grpT, "Number of Typical Floors:", 35, 52, 190, 20);
//            numTypicalLevels = AddNumericCtrl(grpT, 230, 50, 1, 100, 10, enabled: false);
//            numTypicalLevels.ValueChanged += NumTypicalLevels_ValueChanged;
//            AddLabel(grpT, "Typical Floor Height (m):", 335, 52, 190, 20);
//            numTypicalHeight = AddNumericCtrl(grpT, 530, 50, 2.8M, 5.0M, 3.0M, decimals: 2, enabled: false);
//            y += 92;

//            // Refuge ── NEW ──────────────────────────────────────────────────
//            var grpR = AddGroupBox(tab,
//                "Refuge Floors  (auto-inserted at every absolute floor position that is a multiple of 5)",
//                20, y, 910, 82);
//            chkRefuge = AddCheckBox(grpR, "Include Refuge Floors (shared single CAD plan)", 15, 25);
//            AddLabel(grpR,
//                "Refuge floors use the same height as Typical floors.  " +
//                "Terrace is always the topmost floor and is never replaced by a Refuge floor.",
//                35, 52, 840, 20, italic: true, color: System.Drawing.Color.DarkOrange, fontSize: 8);
//            y += 92;

//            // Terrace
//            var grpTr = AddGroupBox(tab, "Terrace Floor  (always pinned as the topmost floor)", 20, y, 910, 82);
//            chkTerrace = AddCheckBox(grpTr, "Include Terrace Floor", 15, 25);
//            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
//            AddLabel(grpTr, "Terrace Height (m):", 35, 52, 150, 20);
//            numTerraceheight = AddNumericCtrl(grpTr, 190, 50, 0.0M, 5.0M, 0.0M, decimals: 2, enabled: false);
//            AddLabel(grpTr, "(0 = reference level only)", 285, 52, 300, 20,
//                italic: true, color: System.Drawing.Color.Gray);
//            y += 92;

//            // Seismic
//            var grpS = AddGroupBox(tab, "Seismic Parameters", 20, y, 910, 70);
//            AddLabel(grpS, "Seismic Zone:", 35, 32, 115, 20);
//            cmbSeismicZone = new ComboBox
//            {
//                Location = new System.Drawing.Point(155, 29),
//                Size = new System.Drawing.Size(260, 25),
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cmbSeismicZone.Items.AddRange(new object[]
//            {
//                "Zone II (Bangalore, Hyderabad)",
//                "Zone III",
//                "Zone IV (Ahmedabad & Kolkata)",
//                "Zone IV (NCR)",
//                "Zone V"
//            });
//            cmbSeismicZone.SelectedIndex = 2;
//            grpS.Controls.Add(cmbSeismicZone);
//            AddLabel(grpS, "Zone II / III → gravity beam 200 mm   |   Zone IV / V → 240 mm",
//                425, 32, 470, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);
//            y += 80;

//            var btnGen = new Button
//            {
//                Text = "▶  Generate CAD Import Tabs",
//                Location = new System.Drawing.Point(340, y),
//                Size = new System.Drawing.Size(240, 42),
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

//            AddLabel(tab, "🏗️ CONCRETE GRADE SCHEDULE — define wall grades from bottom to top",
//                20, y, 900, 25, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 10);
//            y += 35;

//            AddLabel(tab,
//                "⚠️ Total floors in schedule MUST equal total building floors.\n" +
//                "Beam/Slab grade = 0.7 × Wall grade (rounded to nearest 5, minimum M30).",
//                20, y, 900, 35, italic: true, color: System.Drawing.Color.DarkRed);
//            y += 50;

//            AddLabel(tab, "Total Building Floors:", 20, y, bold: true);
//            numTotalFloors = new NumericUpDown
//            {
//                Location = new System.Drawing.Point(190, y),
//                Size = new System.Drawing.Size(85, 25),
//                ReadOnly = true,
//                Enabled = false,
//                Value = 0,
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
//            };
//            tab.Controls.Add(numTotalFloors);
//            AddLabel(tab, "(Auto-calculated from Building Config tab)",
//                285, y + 2, 440, 20, italic: true, color: System.Drawing.Color.Gray);
//            y += 40;

//            dgvGradeSchedule = new DataGridView
//            {
//                Location = new System.Drawing.Point(20, y),
//                Size = new System.Drawing.Size(900, 300),
//                AllowUserToAddRows = false,
//                AllowUserToDeleteRows = false,
//                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                MultiSelect = false,
//                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
//            };

//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            { Name = "Index", HeaderText = "#", ReadOnly = true, Width = 40 });
//            dgvGradeSchedule.Columns.Add(new DataGridViewComboBoxColumn
//            {
//                Name = "WallGrade",
//                HeaderText = "Wall Concrete Grade (bottom → top)",
//                DataSource = new System.Collections.Generic.List<string>
//                    { "M20","M25","M30","M35","M40","M45","M50","M55","M60" },
//                Width = 200
//            });
//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            { Name = "FloorsCount", HeaderText = "No. of Floors", Width = 120 });
//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            { Name = "BeamSlabGrade", HeaderText = "Beam/Slab Grade (Auto)", ReadOnly = true, Width = 160 });
//            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
//            { Name = "FloorRange", HeaderText = "Floor Range", ReadOnly = true, Width = 130 });

//            dgvGradeSchedule.CellValueChanged += DgvGradeSchedule_CellValueChanged;
//            dgvGradeSchedule.CurrentCellDirtyStateChanged += (s, ev) =>
//            {
//                if (dgvGradeSchedule.IsCurrentCellDirty)
//                    dgvGradeSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
//            };
//            tab.Controls.Add(dgvGradeSchedule);
//            y += 315;

//            btnAddGradeRow = new Button
//            {
//                Text = "➕ Add Row",
//                Location = new System.Drawing.Point(20, y),
//                Size = new System.Drawing.Size(130, 35),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
//            };
//            btnAddGradeRow.Click += BtnAddGradeRow_Click;
//            tab.Controls.Add(btnAddGradeRow);

//            btnRemoveGradeRow = new Button
//            {
//                Text = "➖ Remove Selected",
//                Location = new System.Drawing.Point(160, y),
//                Size = new System.Drawing.Size(160, 35)
//            };
//            btnRemoveGradeRow.Click += BtnRemoveGradeRow_Click;
//            tab.Controls.Add(btnRemoveGradeRow);

//            lblGradeTotal = new Label
//            {
//                Text = "Total floors in schedule: 0 / 0",
//                Location = new System.Drawing.Point(335, y + 8),
//                Size = new System.Drawing.Size(550, 25),
//                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
//                ForeColor = System.Drawing.Color.DarkRed
//            };
//            tab.Controls.Add(lblGradeTotal);

//            UpdateTotalFloorsForGradeSchedule();
//        }

//        // ====================================================================
//        // DYNAMIC CAD IMPORT TAB
//        // ====================================================================

//        internal void CreateCADImportTab(string floorType, string description,
//            List<(string label, string dictKey)> namedGravityBeams = null)
//        {
//            var tab = new TabPage(floorType);
//            tab.AutoScroll = true;
//            tabControl.TabPages.Add(tab);
//            int y = 10;

//            AddLabel(tab, $"📐 {description}", 20, y, 900, 25,
//                bold: true, color: System.Drawing.Color.DarkGreen);
//            y += 35;

//            // CAD File row
//            AddLabel(tab, "CAD File:", 20, y, 85, 25);
//            var txtCAD = new TextBox
//            {
//                Location = new System.Drawing.Point(110, y - 2),
//                Size = new System.Drawing.Size(610, 25),
//                ReadOnly = true
//            };
//            tab.Controls.Add(txtCAD);
//            cadPathTextBoxes[floorType] = txtCAD;

//            var btnLoad = new Button
//            {
//                Text = "Browse...",
//                Location = new System.Drawing.Point(730, y - 4),
//                Size = new System.Drawing.Size(110, 28)
//            };
//            btnLoad.Click += (s, ev) => BtnLoadCAD_Click(floorType);
//            tab.Controls.Add(btnLoad);
//            y += 42;

//            int numFloors = chkTypical.Checked ? (int)numTypicalLevels.Value : 20;
//            string seisZone = cmbSeismicZone.SelectedItem?.ToString()
//                              ?? "Zone IV (Ahmedabad & Kolkata)";

//            AddLayerMappingUI(tab, floorType, ref y);
//            AddWallThicknessUI(tab, floorType, numFloors, seisZone, ref y);
//            AddBeamDepthsUI(tab, floorType, namedGravityBeams, ref y);
//            AddSlabThicknessesUI(tab, floorType, ref y);
//        }

//        // ====================================================================
//        // LAYER MAPPING UI
//        // ====================================================================

//        private void AddLayerMappingUI(TabPage tab, string floorType, ref int y)
//        {
//            AddLabel(tab, "Available CAD Layers:", 20, y, 200, 20);

//            var lstAvail = new ListBox
//            { Location = new System.Drawing.Point(20, y + 22), Size = new System.Drawing.Size(305, 215) };
//            tab.Controls.Add(lstAvail);
//            availableLayerListBoxes[floorType] = lstAvail;

//            AddLabel(tab, "Assign as:", 342, y + 22, 90, 20);
//            var cboElem = new ComboBox
//            {
//                Location = new System.Drawing.Point(342, y + 44),
//                Size = new System.Drawing.Size(145, 25),
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cboElem.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Column", "Ignore" });
//            cboElem.SelectedIndex = 0;
//            tab.Controls.Add(cboElem);
//            elementTypeComboBoxes[floorType] = cboElem;

//            var btnAdd = new Button
//            { Text = "Add  →", Location = new System.Drawing.Point(342, y + 78), Size = new System.Drawing.Size(145, 32) };
//            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
//            tab.Controls.Add(btnAdd);

//            var btnRem = new Button
//            { Text = "←  Remove", Location = new System.Drawing.Point(342, y + 120), Size = new System.Drawing.Size(145, 32) };
//            btnRem.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
//            tab.Controls.Add(btnRem);

//            AddLabel(tab, "Layer Mappings:", 502, y, 200, 20);
//            var lstMap = new ListBox
//            { Location = new System.Drawing.Point(502, y + 22), Size = new System.Drawing.Size(338, 215) };
//            tab.Controls.Add(lstMap);
//            mappedLayerListBoxes[floorType] = lstMap;

//            y += 252;
//        }

//        // ====================================================================
//        // WALL THICKNESS UI
//        // ====================================================================

//        private void AddWallThicknessUI(TabPage tab, string floorType,
//            int numFloors, string seisZone, ref int y)
//        {
//            int gplCore = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.CoreWall, seisZone);
//            int gplPerDead = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralDeadWall, seisZone);
//            int gplPerPortal = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralPortalWall, seisZone);
//            int gplInternal = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.InternalWall, seisZone);

//            const int grpH = 195;
//            var grp = AddGroupBox(tab,
//                "🧱 Wall Thicknesses — GPL Table (IS 1893-2025)  |  Values pre-filled from GPL; edit to override",
//                20, y, 920, grpH);

//            AddLabel(grp,
//                $"Values shown are from GPL table for {numFloors} floors / {seisZone}.  " +
//                "Edit any value to override for this floor type.",
//                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

//            const int ry1 = 42;
//            AddLabel(grp, "Core Wall (mm):", C1L, ry1, C1N - C1L - 5, 20);
//            numCoreWallOverridePerFloor[floorType] =
//                AddNumericCtrl(grp, C1N, ry1 - 2, 100, 700, gplCore, increment: 25);
//            AddLabel(grp, $"GPL: {gplCore}", C1N + NW + 4, ry1 + 2, 62, 16,
//                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

//            AddLabel(grp, "Periph. Dead Wall (mm):", C2L, ry1, C2N - C2L - 5, 20);
//            numPeriphDeadWallOverridePerFloor[floorType] =
//                AddNumericCtrl(grp, C2N, ry1 - 2, 100, 700, gplPerDead, increment: 25);
//            AddLabel(grp, $"GPL: {gplPerDead}", C2N + NW + 4, ry1 + 2, 62, 16,
//                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

//            AddLabel(grp, "Periph. Portal Wall (mm):", C3L, ry1, C3N - C3L - 5, 20);
//            numPeriphPortalWallOverridePerFloor[floorType] =
//                AddNumericCtrl(grp, C3N, ry1 - 2, 100, 700, gplPerPortal, increment: 25);
//            toolTip.SetToolTip(numPeriphPortalWallOverridePerFloor[floorType],
//                $"GPL table value for {numFloors} floors: {gplPerPortal} mm");

//            const int ry2 = 80;
//            AddLabel(grp, "Internal Wall (mm):", C1L, ry2, C1N - C1L - 5, 20);
//            numInternalWallOverridePerFloor[floorType] =
//                AddNumericCtrl(grp, C1N, ry2 - 2, 100, 700, gplInternal, increment: 25);
//            AddLabel(grp, $"GPL: {gplInternal}", C1N + NW + 4, ry2 + 2, 62, 16,
//                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);
//            AddLabel(grp,
//                "(short wall < 1.8 m may require thicker — see GPL table for coupled shear wall cases)",
//                C2L, ry2 + 2, 480, 16,
//                italic: true, color: System.Drawing.Color.Gray, fontSize: 7.5f);

//            const int ry3 = 110;
//            var ntaPanel = new System.Windows.Forms.Panel
//            {
//                Location = new System.Drawing.Point(C1L, ry3),
//                Size = new System.Drawing.Size(880, 46),
//                BackColor = System.Drawing.Color.FromArgb(255, 255, 200),
//                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
//            };
//            grp.Controls.Add(ntaPanel);
//            AddLabel(ntaPanel, "W-NTA Wall — Non-structural  (always user defined, mm):",
//                8, 5, 340, 20, bold: true, fontSize: 8.5f);
//            numNtaWallThicknessPerFloor[floorType] =
//                AddNumericCtrl(ntaPanel, 355, 4, 100, 500, 200, increment: 25);
//            AddLabel(ntaPanel,
//                "Not in GPL table — enter actual partition / non-structural wall thickness for this floor.",
//                8, 25, 855, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

//            AddLabel(grp,
//                "Wall pier labels in ETABS: P1, P2, P3 … (auto-assigned per wall element)",
//                C1L, 162, 880, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

//            y += grpH + 8;
//        }

//        private static int SafeGetGPL(int floors,
//            Core.WallThicknessCalculator.WallType wallType, string seisZone)
//        {
//            try
//            {
//                int f = Math.Max(1, Math.Min(50, floors));
//                return Core.WallThicknessCalculator.GetRecommendedThickness(f, wallType, seisZone);
//            }
//            catch
//            {
//                return wallType == Core.WallThicknessCalculator.WallType.CoreWall ? 300 : 200;
//            }
//        }

//        // ====================================================================
//        // BEAM DEPTHS UI
//        // ====================================================================

//        private void AddBeamDepthsUI(TabPage tab, string floorType,
//            List<(string label, string dictKey)> namedGravityBeams, ref int y)
//        {
//            int extraRows = namedGravityBeams?.Count ?? 0;
//            int grpHeight = 26 + 24
//                          + RH + RH
//                          + (RH * extraRows)
//                          + 22 + 26
//                          + RH + RH + RH + RH
//                          + 30 + 10;

//            var grp = AddGroupBox(tab,
//                "🔧 Beam Configuration — Depth (mm, user input) | Width (mm, auto or override)",
//                20, y, 920, grpHeight);

//            int gw = GetAutoGravityWidthFromUI();
//            AddLabel(grp,
//                $"Auto gravity width: {gw} mm (seismic zone)   |   Main beam (MB) width = matching wall thickness   |   Width override 0 = auto",
//                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

//            int gy = 46;

//            // ── GRAVITY BEAMS ─────────────────────────────────────────────
//            AddLabel(grp, "─── GRAVITY BEAMS ───", C1L, gy, 260, 18, bold: true, fontSize: 8.5f);
//            gy += 24;

//            AddLabel(grp, "B-Internal Gravity Beams depth:", C1L, gy, C1N - C1L - 5, 20);
//            numInternalGravityDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 200, 1200, 450, increment: 25);
//            AddLabel(grp, "B-Cantilever Gravity Beams depth:", C2L, gy, C2N - C2L - 5, 20);
//            numCantileverGravityDepthPerFloor[floorType] = AddNumericCtrl(grp, C2N, gy - 2, 200, 1200, 500, increment: 25);
//            AddLabel(grp, "Gravity Width override (0=auto):", C3L, gy, C3N - C3L - 5, 20);
//            numGravityWidthOverridePerFloor[floorType] = AddNumericCtrl(grp, C3N, gy - 2, 0, 500, 0, increment: 10);
//            gy += RH;

//            AddLabel(grp, "B-No Load Gravity Beams depth:", C1L, gy, C1N - C1L - 5, 20);
//            numNoLoadGravityDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 200, 1200, 450, increment: 25);
//            AddLabel(grp, "(Wall load = 0 kN/m; depth user input)", C2L, gy, 400, 18,
//                italic: true, color: System.Drawing.Color.Gray, fontSize: 7.5f);
//            gy += RH;

//            if (namedGravityBeams != null)
//            {
//                foreach (var (beamLabel, dictKey) in namedGravityBeams)
//                {
//                    AddLabel(grp, $"{beamLabel} depth:", C1L, gy, C1N - C1L - 5, 20);
//                    var num = AddNumericCtrl(grp, C1N, gy - 2, 200, 1200, 450, increment: 25);

//                    switch (dictKey)
//                    {
//                        case "EDeck": numEDeckGravityDepthPerFloor[floorType] = num; break;
//                        case "Podium": numPodiumGravityDepthPerFloor[floorType] = num; break;
//                        case "Ground": numGroundGravityDepthPerFloor[floorType] = num; break;
//                        case "Basement": numBasementGravityDepthPerFloor[floorType] = num; break;
//                    }
//                    gy += RH;
//                }
//            }

//            AddLabel(grp,
//                "(Named gravity variants above use their own depth. Beam load assigned per CAD layer.)",
//                C1L, gy, 880, 16, italic: true, color: System.Drawing.Color.Gray, fontSize: 7.5f);
//            gy += 22;

//            // ── MAIN BEAMS ────────────────────────────────────────────────
//            AddLabel(grp, "─── MAIN BEAMS (MB__ sections) ───", C1L, gy, 320, 18,
//                bold: true, fontSize: 8.5f);
//            gy += 26;

//            AddLabel(grp, "B-Core Main Beams depth:", C1L, gy, C1N - C1L - 5, 20);
//            numCoreMainDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 300, 1500, 600, increment: 25);
//            AddLabel(grp, "B-Core Main Beams width override:", C2L, gy, C2N - C2L - 5, 20);
//            numCoreMainWidthOverridePerFloor[floorType] = AddNumericCtrl(grp, C2N, gy - 2, 0, 600, 0, increment: 25);
//            gy += RH;

//            AddLabel(grp, "B-Peripheral Dead Main depth:", C1L, gy, C1N - C1L - 5, 20);
//            numPeripheralDeadMainDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 300, 1500, 600, increment: 25);
//            AddLabel(grp, "B-Peripheral Dead width override:", C2L, gy, C2N - C2L - 5, 20);
//            numPeripheralDeadMainWidthOverridePerFloor[floorType] = AddNumericCtrl(grp, C2N, gy - 2, 0, 600, 0, increment: 25);
//            gy += RH;

//            AddLabel(grp, "B-Peripheral Portal Main depth:", C1L, gy, C1N - C1L - 5, 20);
//            numPeripheralPortalMainDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 300, 1500, 650, increment: 25);
//            AddLabel(grp, "B-Peripheral Portal width override:", C2L, gy, C2N - C2L - 5, 20);
//            numPeripheralPortalMainWidthOverridePerFloor[floorType] = AddNumericCtrl(grp, C2N, gy - 2, 0, 600, 0, increment: 25);
//            gy += RH;

//            AddLabel(grp, "B-Internal Main Beams depth:", C1L, gy, C1N - C1L - 5, 20);
//            numInternalMainDepthPerFloor[floorType] = AddNumericCtrl(grp, C1N, gy - 2, 300, 1500, 550, increment: 25);
//            AddLabel(grp, "B-Internal Main width override:", C2L, gy, C2N - C2L - 5, 20);
//            numInternalMainWidthOverridePerFloor[floorType] = AddNumericCtrl(grp, C2N, gy - 2, 0, 600, 0, increment: 25);
//            gy += RH;

//            AddLabel(grp,
//                "💡 Width override = 0 → auto (wall thickness for MB; zone width for gravity). MB sections must start with 'MB' in ETABS template.",
//                C1L, gy, 890, 26, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

//            y += grpHeight + 12;
//        }

//        // ====================================================================
//        // SLAB THICKNESSES UI
//        // ====================================================================

//        private void AddSlabThicknessesUI(TabPage tab, string floorType, ref int y)
//        {
//            var grp = AddGroupBox(tab,
//                "🔧 Slab Thicknesses — YELLOW layers (user input, mm) | CYAN = auto-span | WHITE = auto-area",
//                20, y, 920, 210);

//            AddLabel(grp,
//                "WHITE layers: auto from polygon area (14 → 70 m²  =  125 → 250 mm).\n" +
//                "CYAN layers:  auto from cantilever span (1.0 – 5.0 m  =  125 – 200 mm).\n" +
//                "YELLOW layers below: fixed user input (structural special-purpose slabs).",
//                15, 20, 880, 46, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

//            const int s1l = 20, s1n = 140;
//            const int s2l = 250, s2n = 370;
//            const int s3l = 490, s3n = 610;
//            const int s4l = 730, s4n = 828;
//            const int sr = 32;

//            int sy = 72;

//            // Row 1
//            AddLabel(grp, "S-LOBBY:", s1l, sy, 115, 20);
//            numLobbySlabThicknessPerFloor[floorType] = AddNumericCtrl(grp, s1n, sy - 2, 100, 400, 160, increment: 5);
//            AddLabel(grp, "S-STAIRCASE:", s2l, sy, 115, 20);
//            numStairSlabThicknessPerFloor[floorType] = AddNumericCtrl(grp, s2n, sy - 2, 100, 400, 175, increment: 5);
//            AddLabel(grp, "S-FIRE TENDER:", s3l, sy, 115, 20);
//            numFireTenderSlabPerFloor[floorType] = AddNumericCtrl(grp, s3n, sy - 2, 100, 500, 200, increment: 5);
//            AddLabel(grp, "S-OHT:", s4l, sy, 90, 20);
//            numOHTSlabPerFloor[floorType] = AddNumericCtrl(grp, s4n, sy - 2, 100, 600, 200, increment: 5);
//            sy += sr;

//            // Row 2
//            AddLabel(grp, "S-TERRACE FIRE:", s1l, sy, 115, 20);
//            numTerraceFireSlabPerFloor[floorType] = AddNumericCtrl(grp, s1n, sy - 2, 100, 600, 200, increment: 5);
//            AddLabel(grp, "S-UGT:", s2l, sy, 115, 20);
//            numUGTSlabPerFloor[floorType] = AddNumericCtrl(grp, s2n, sy - 2, 100, 600, 250, increment: 5);
//            AddLabel(grp, "S-LANDSCAPE:", s3l, sy, 115, 20);
//            numLandscapeSlabPerFloor[floorType] = AddNumericCtrl(grp, s3n, sy - 2, 100, 500, 175, increment: 5);
//            AddLabel(grp, "S-SWIMMING:", s4l, sy, 90, 20);
//            numSwimmingSlabPerFloor[floorType] = AddNumericCtrl(grp, s4n, sy - 2, 100, 500, 250, increment: 5);
//            sy += sr;

//            // Row 3
//            AddLabel(grp, "S-DG:", s1l, sy, 115, 20);
//            numDGSlabPerFloor[floorType] = AddNumericCtrl(grp, s1n, sy - 2, 100, 500, 200, increment: 5);
//            AddLabel(grp, "S-STP:", s2l, sy, 115, 20);
//            numSTPSlabPerFloor[floorType] = AddNumericCtrl(grp, s2n, sy - 2, 100, 500, 200, increment: 5);
//            sy += sr;

//            AddLabel(grp,
//                "All slabs will be auto-meshed and assigned in ETABS after import.",
//                15, sy + 2, 880, 18, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

//            y += 222;
//        }

//        // ====================================================================
//        // GENERATE TABS
//        // ====================================================================

//        private void BtnGenerateTabs_Click(object sender, EventArgs e)
//        {
//            if (!chkBasement.Checked && !chkPodium.Checked && !chkGround.Checked &&
//                !chkEDeck.Checked && !chkTypical.Checked && !chkTerrace.Checked)
//            {
//                MessageBox.Show("Please select at least one floor type!", "No Floors Selected",
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }

//            // Remove all existing floor tabs (keep Building Config + Concrete Grades)
//            while (tabControl.TabPages.Count > 2)
//                tabControl.TabPages.RemoveAt(2);

//            ClearAllFloorDicts();

//            int tabCount = 0;

//            // ── Basements — one tab each ──────────────────────────────────
//            if (chkBasement.Checked)
//            {
//                int cnt = (int)numBasementLevels.Value;
//                for (int i = 1; i <= cnt; i++)
//                {
//                    CreateCADImportTab($"Basement{i}", $"Basement {i} Floor Plan",
//                        new List<(string, string)> { ($"B-Basement{i} Gravity Beams", "Basement") });
//                    tabCount++;
//                }
//            }

//            // ── Podiums — one tab each (individual, like Basements) ───────
//            if (chkPodium.Checked)
//            {
//                int cnt = (int)numPodiumLevels.Value;
//                for (int i = 1; i <= cnt; i++)
//                {
//                    CreateCADImportTab($"Podium{i}", $"Podium {i} Floor Plan",
//                        new List<(string, string)> { ($"B-Podium{i} Gravity Beams", "Podium") });
//                    tabCount++;
//                }
//            }

//            // ── Ground ───────────────────────────────────────────────────
//            if (chkGround.Checked)
//            {
//                CreateCADImportTab("Ground", "Ground Floor Plan",
//                    new List<(string, string)> { ("B-Ground Gravity Beams", "Ground") });
//                tabCount++;
//            }

//            // ── E-Deck ───────────────────────────────────────────────────
//            if (chkEDeck.Checked)
//            {
//                CreateCADImportTab("EDeck", "E-Deck Floor Plan",
//                    new List<(string, string)> { ("B-Edeck Gravity Beams", "EDeck") });
//                tabCount++;
//            }

//            // ── Typical ──────────────────────────────────────────────────
//            if (chkTypical.Checked)
//            {
//                CreateCADImportTab("Typical", "Typical Floor Plan (replicated for all typical floors)");
//                tabCount++;
//            }

//            // ── Refuge — one shared tab ────────────────────────────────
//            if (chkRefuge.Checked)
//            {
//                CreateCADImportTab("Refuge",
//                    "Refuge Floor Plan (shared — auto-inserted at every absolute floor position divisible by 5)");
//                tabCount++;
//            }

//            // ── Terrace ──────────────────────────────────────────────────
//            if (chkTerrace.Checked)
//            {
//                CreateCADImportTab("Terrace", "Terrace Floor Plan");
//                tabCount++;
//            }

//            UpdateTotalFloorsForGradeSchedule();

//            // Build info note
//            var notes = new System.Text.StringBuilder();
//            if (chkBasement.Checked)
//                notes.AppendLine($"• {(int)numBasementLevels.Value} individual basement tab(s): B1, B2, ...");
//            if (chkPodium.Checked)
//                notes.AppendLine($"• {(int)numPodiumLevels.Value} individual podium tab(s): P1, P2, ...");
//            if (chkRefuge.Checked)
//            {
//                // Preview which absolute positions will be Refuge
//                var refugePreview = PreviewRefugePositions();
//                notes.AppendLine($"• Refuge floors at absolute positions: {string.Join(", ", refugePreview)}");
//            }

//            MessageBox.Show(
//                $"✓ {tabCount} CAD Import tab(s) generated!\n\n" +
//                notes.ToString() +
//                "\nWall thicknesses are pre-filled from the GPL table (IS 1893-2025).\n" +
//                "Edit any value to override for that specific floor type.\n\n" +
//                "For each floor tab:\n" +
//                "  1. Browse & load DXF file\n" +
//                "  2. Verify auto-mapped layers, add / remove as needed\n" +
//                "  3. Check / adjust wall thicknesses (GPL values pre-filled)\n" +
//                "  4. Set all gravity & main beam depths; adjust width overrides if needed\n" +
//                "  5. Set YELLOW slab thicknesses\n\n" +
//                "Then complete the Concrete Grades schedule.",
//                "Tabs Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
//        }

//        /// <summary>
//        /// Calculates which absolute floor positions (1-based) will be Refuge floors,
//        /// based on current building config — for preview in the info dialog.
//        /// Terrace (last position) is exempt.
//        /// </summary>
//        private List<int> PreviewRefugePositions()
//        {
//            int total = 0;
//            if (chkBasement.Checked) total += (int)numBasementLevels.Value;
//            if (chkPodium.Checked) total += (int)numPodiumLevels.Value;
//            if (chkGround.Checked) total += 1;
//            if (chkEDeck.Checked) total += 1;
//            if (chkTypical.Checked) total += (int)numTypicalLevels.Value;
//            // Terrace adds 1 but is exempt — it occupies position total+1

//            var positions = new List<int>();
//            for (int i = 1; i <= total; i++)
//                if (i % 5 == 0) positions.Add(i);
//            return positions;
//        }

//        // ====================================================================
//        // CLEAR ALL FLOOR DICTS
//        // ====================================================================

//        private void ClearAllFloorDicts()
//        {
//            cadPathTextBoxes.Clear();
//            availableLayerListBoxes.Clear();
//            mappedLayerListBoxes.Clear();
//            elementTypeComboBoxes.Clear();

//            numInternalGravityDepthPerFloor.Clear();
//            numCantileverGravityDepthPerFloor.Clear();
//            numNoLoadGravityDepthPerFloor.Clear();
//            numEDeckGravityDepthPerFloor.Clear();
//            numPodiumGravityDepthPerFloor.Clear();
//            numGroundGravityDepthPerFloor.Clear();
//            numBasementGravityDepthPerFloor.Clear();

//            numCoreMainDepthPerFloor.Clear();
//            numPeripheralDeadMainDepthPerFloor.Clear();
//            numPeripheralPortalMainDepthPerFloor.Clear();
//            numInternalMainDepthPerFloor.Clear();

//            numGravityWidthOverridePerFloor.Clear();
//            numCoreMainWidthOverridePerFloor.Clear();
//            numPeripheralDeadMainWidthOverridePerFloor.Clear();
//            numPeripheralPortalMainWidthOverridePerFloor.Clear();
//            numInternalMainWidthOverridePerFloor.Clear();

//            numLobbySlabThicknessPerFloor.Clear();
//            numStairSlabThicknessPerFloor.Clear();
//            numFireTenderSlabPerFloor.Clear();
//            numOHTSlabPerFloor.Clear();
//            numTerraceFireSlabPerFloor.Clear();
//            numUGTSlabPerFloor.Clear();
//            numLandscapeSlabPerFloor.Clear();
//            numSwimmingSlabPerFloor.Clear();
//            numDGSlabPerFloor.Clear();
//            numSTPSlabPerFloor.Clear();

//            numCoreWallOverridePerFloor.Clear();
//            numPeriphDeadWallOverridePerFloor.Clear();
//            numPeriphPortalWallOverridePerFloor.Clear();
//            numInternalWallOverridePerFloor.Clear();
//            numNtaWallThicknessPerFloor.Clear();
//        }

//        // ====================================================================
//        // UI HELPER METHODS
//        // ====================================================================

//        private int GetAutoGravityWidthFromUI()
//        {
//            string zone = cmbSeismicZone.SelectedItem?.ToString() ?? "";
//            return (zone.Contains("II") || zone.Contains("III")) ? 200 : 240;
//        }

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
//            if (color.HasValue) lbl.ForeColor = color.Value;
//            parent.Controls.Add(lbl);
//            return lbl;
//        }

//        private GroupBox AddGroupBox(Control parent, string text, int x, int y, int width, int height)
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
//                Size = new System.Drawing.Size(420, 20)
//            };
//            parent.Controls.Add(chk);
//            return chk;
//        }

//        private NumericUpDown AddNumericCtrl(Control parent, int x, int y,
//            decimal min, decimal max, decimal value,
//            int decimals = 0, decimal increment = 1, bool enabled = true)
//        {
//            var num = new NumericUpDown
//            {
//                Location = new System.Drawing.Point(x, y),
//                Size = new System.Drawing.Size(NW, NH),
//                Minimum = min,
//                Maximum = max,
//                Value = Math.Max(min, Math.Min(max, value)),
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
// FILE: UI/ImportConfigForm.UI.cs (PART 2 - UI Initialization)
// ============================================================================
// PURPOSE: UI initialization and tab creation for ImportConfigForm
// VERSION: 2.8 — Fixed grpHeight overlap + zone/wall-thickness default widths
// ============================================================================

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ETAB_Automation
{
    public partial class ImportConfigForm
    {
        private ToolTip toolTip;

        // ====================================================================
        // LAYOUT CONSTANTS
        // ====================================================================
        private const int C1L = 20, C1N = 210;
        private const int C2L = 320, C2N = 510;
        private const int C3L = 620, C3N = 800;
        private const int NW = 85;
        private const int NH = 25;
        private const int RH = 32;

        // Width of the load-set TextBox next to each beam / slab row
        private const int LW = 140;

        // ====================================================================
        // MAIN UI INITIALIZATION
        // ====================================================================

        internal void InitializeControlsUI()
        {
            toolTip = new ToolTip
            { AutoPopDelay = 5000, InitialDelay = 500, ReshowDelay = 200, ShowAlways = true };

            this.Size = new System.Drawing.Size(980, 840);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ETABS CAD Import Configuration v2.8";

            tabControl = new TabControl
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(950, 730)
            };
            this.Controls.Add(tabControl);

            var tabBuilding = new TabPage("Building Config");
            tabControl.TabPages.Add(tabBuilding);
            InitializeBuildingConfigTab(tabBuilding);

            var tabGrade = new TabPage("Concrete Grades");
            tabControl.TabPages.Add(tabGrade);
            InitializeGradeScheduleTab(tabGrade);

            btnImport = new Button
            {
                Text = "▶  Import to ETABS",
                Location = new System.Drawing.Point(680, 752),
                Size = new System.Drawing.Size(155, 42),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGreen
            };
            btnImport.Click += BtnImport_Click;
            this.Controls.Add(btnImport);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(845, 752),
                Size = new System.Drawing.Size(110, 42),
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

            AddLabel(tab, "📋 Define building structure from bottom to top (all floors are optional)",
                20, y, 900, 25, bold: true, color: System.Drawing.Color.DarkBlue);
            y += 35;

            var grpF = AddGroupBox(tab, "Foundation", 20, y, 910, 85);
            chkFoundation = AddCheckBox(grpF, "Include Foundation to Basement height", 15, 25);
            chkFoundation.CheckedChanged += ChkFoundation_CheckedChanged;
            AddLabel(grpF, "Foundation Height (m):", 35, 52, 160, 20);
            numFoundationHeight = AddNumericCtrl(grpF, 200, 50, 0.5M, 5.0M, 1.5M, decimals: 2, enabled: false);
            AddLabel(grpF, "(Distance from basement bottom to foundation level)",
                295, 52, 580, 20, italic: true, color: System.Drawing.Color.Gray);
            y += 95;

            var grpB = AddGroupBox(tab, "Basement Floors  (each floor gets its own CAD tab)", 20, y, 910, 108);
            chkBasement = AddCheckBox(grpB, "Include Basement Floors", 15, 25);
            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;
            AddLabel(grpB, "Number of Basements (1–5):", 35, 52, 200, 20);
            numBasementLevels = AddNumericCtrl(grpB, 240, 50, 1, 5, 1, enabled: false);
            numBasementLevels.ValueChanged += NumBasementLevels_ValueChanged;
            AddLabel(grpB, "Each Basement Height (m):", 345, 52, 195, 20);
            numBasementHeight = AddNumericCtrl(grpB, 545, 50, 2.5M, 6.0M, 3.5M, decimals: 2, enabled: false);
            AddLabel(grpB, "⚠️ One CAD tab will be created per basement floor (B1, B2, ...)",
                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
            y += 118;

            var grpP = AddGroupBox(tab,
                "Podium Floors  (each floor gets its own CAD tab — like Basements)", 20, y, 910, 108);
            chkPodium = AddCheckBox(grpP, "Include Podium Floors", 15, 25);
            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
            AddLabel(grpP, "Number of Podiums (1–5):", 35, 52, 200, 20);
            numPodiumLevels = AddNumericCtrl(grpP, 240, 50, 1, 5, 1, enabled: false);
            numPodiumLevels.ValueChanged += NumPodiumLevels_ValueChanged;
            AddLabel(grpP, "Each Podium Height (m):", 345, 52, 185, 20);
            numPodiumHeight = AddNumericCtrl(grpP, 535, 50, 3.0M, 8.0M, 4.5M, decimals: 2, enabled: false);
            AddLabel(grpP, "⚠️ One CAD tab will be created per podium floor (P1, P2, ...)",
                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
            y += 118;

            var grpGr = AddGroupBox(tab, "Ground Floor", 20, y, 910, 82);
            chkGround = AddCheckBox(grpGr, "Include Ground Floor", 15, 25);
            chkGround.CheckedChanged += ChkGround_CheckedChanged;
            AddLabel(grpGr, "Ground Floor Height (m):", 35, 52, 180, 20);
            numGroundHeight = AddNumericCtrl(grpGr, 220, 50, 3.0M, 10.0M, 4.0M, decimals: 2, enabled: false);
            y += 92;

            var grpE = AddGroupBox(tab, "E-Deck Floor", 20, y, 910, 82);
            chkEDeck = AddCheckBox(grpE, "Include E-Deck Floor", 15, 25);
            chkEDeck.CheckedChanged += ChkEDeck_CheckedChanged;
            AddLabel(grpE, "E-Deck Height (m):", 35, 52, 150, 20);
            numEDeckHeight = AddNumericCtrl(grpE, 190, 50, 3.0M, 10.0M, 4.5M, decimals: 2, enabled: false);
            y += 92;

            var grpT = AddGroupBox(tab, "Typical Floors", 20, y, 910, 82);
            chkTypical = AddCheckBox(grpT, "Include Typical Floors", 15, 25);
            chkTypical.CheckedChanged += ChkTypical_CheckedChanged;
            AddLabel(grpT, "Number of Typical Floors:", 35, 52, 190, 20);
            numTypicalLevels = AddNumericCtrl(grpT, 230, 50, 1, 100, 10, enabled: false);
            numTypicalLevels.ValueChanged += NumTypicalLevels_ValueChanged;
            AddLabel(grpT, "Typical Floor Height (m):", 335, 52, 190, 20);
            numTypicalHeight = AddNumericCtrl(grpT, 530, 50, 2.8M, 5.0M, 3.0M, decimals: 2, enabled: false);
            y += 92;

            //var grpR = AddGroupBox(tab,
            //    "Refuge Floors  (auto-inserted at every absolute floor position that is a multiple of 5)",
            //    20, y, 910, 82);
            //chkRefuge = AddCheckBox(grpR, "Include Refuge Floors (shared single CAD plan)", 15, 25);
            //AddLabel(grpR,
            //    "Refuge floors use the same height as Typical floors.  " +
            //    "Terrace is always the topmost floor and is never replaced by a Refuge floor.",
            //    35, 52, 840, 20, italic: true, color: System.Drawing.Color.DarkOrange, fontSize: 8);
            //y += 92;

            var grpTr = AddGroupBox(tab, "Terrace Floor  (always pinned as the topmost floor)", 20, y, 910, 82);
            chkTerrace = AddCheckBox(grpTr, "Include Terrace Floor", 15, 25);
            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
            AddLabel(grpTr, "Terrace Height (m):", 35, 52, 150, 20);
            numTerraceheight = AddNumericCtrl(grpTr, 190, 50, 0.0M, 5.0M, 0.0M, decimals: 2, enabled: false);
            AddLabel(grpTr, "(0 = reference level only)", 285, 52, 300, 20,
                italic: true, color: System.Drawing.Color.Gray);
            y += 92;

            var grpS = AddGroupBox(tab, "Seismic Parameters", 20, y, 910, 70);
            AddLabel(grpS, "Seismic Zone:", 35, 32, 115, 20);
            cmbSeismicZone = new ComboBox
            {
                Location = new System.Drawing.Point(155, 29),
                Size = new System.Drawing.Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSeismicZone.Items.AddRange(new object[]
            {
                "Zone II (Bangalore, Hyderabad)",
                "Zone III",
                "Zone IV (Ahmedabad & Kolkata)",
                "Zone IV (NCR)",
                "Zone V"
            });
            cmbSeismicZone.SelectedIndex = 2;
            grpS.Controls.Add(cmbSeismicZone);
            AddLabel(grpS, "Zone II / III → gravity beam 200 mm   |   Zone IV / V → 240 mm",
                425, 32, 470, 20, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);
            y += 80;

            var btnGen = new Button
            {
                Text = "▶  Generate CAD Import Tabs",
                Location = new System.Drawing.Point(340, y),
                Size = new System.Drawing.Size(240, 42),
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

            AddLabel(tab, "🏗️ CONCRETE GRADE SCHEDULE — define wall grades from bottom to top",
                20, y, 900, 25, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 10);
            y += 35;

            AddLabel(tab,
                "⚠️ Total floors in schedule MUST equal total building floors.\n" +
                "Beam/Slab grade = 0.7 × Wall grade (rounded to nearest 5, minimum M30).",
                20, y, 900, 35, italic: true, color: System.Drawing.Color.DarkRed);
            y += 50;

            AddLabel(tab, "Total Building Floors:", 20, y, bold: true);
            numTotalFloors = new NumericUpDown
            {
                Location = new System.Drawing.Point(190, y),
                Size = new System.Drawing.Size(85, 25),
                ReadOnly = true,
                Enabled = false,
                Value = 0,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(numTotalFloors);
            AddLabel(tab, "(Auto-calculated from Building Config tab)",
                285, y + 2, 440, 20, italic: true, color: System.Drawing.Color.Gray);
            y += 40;

            dgvGradeSchedule = new DataGridView
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(900, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Index", HeaderText = "#", ReadOnly = true, Width = 40 });
            dgvGradeSchedule.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "WallGrade",
                HeaderText = "Wall Concrete Grade (bottom → top)",
                DataSource = new System.Collections.Generic.List<string>
                    { "M20","M25","M30","M35","M40","M45","M50","M55","M60" },
                Width = 200
            });
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "FloorsCount", HeaderText = "No. of Floors", Width = 120 });
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "BeamSlabGrade", HeaderText = "Beam/Slab Grade (Auto)", ReadOnly = true, Width = 160 });
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "FloorRange", HeaderText = "Floor Range", ReadOnly = true, Width = 130 });

            dgvGradeSchedule.CellValueChanged += DgvGradeSchedule_CellValueChanged;
            dgvGradeSchedule.CurrentCellDirtyStateChanged += (s, ev) =>
            {
                if (dgvGradeSchedule.IsCurrentCellDirty)
                    dgvGradeSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            tab.Controls.Add(dgvGradeSchedule);
            y += 315;

            btnAddGradeRow = new Button
            {
                Text = "➕ Add Row",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(130, 35),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            btnAddGradeRow.Click += BtnAddGradeRow_Click;
            tab.Controls.Add(btnAddGradeRow);

            btnRemoveGradeRow = new Button
            {
                Text = "➖ Remove Selected",
                Location = new System.Drawing.Point(160, y),
                Size = new System.Drawing.Size(160, 35)
            };
            btnRemoveGradeRow.Click += BtnRemoveGradeRow_Click;
            tab.Controls.Add(btnRemoveGradeRow);

            lblGradeTotal = new Label
            {
                Text = "Total floors in schedule: 0 / 0",
                Location = new System.Drawing.Point(335, y + 8),
                Size = new System.Drawing.Size(550, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkRed
            };
            tab.Controls.Add(lblGradeTotal);

            UpdateTotalFloorsForGradeSchedule();
        }

        // ====================================================================
        // DYNAMIC CAD IMPORT TAB
        // ====================================================================

        internal void CreateCADImportTab(string floorType, string description,
            List<(string label, string dictKey)> namedGravityBeams = null)
        {
            var tab = new TabPage(floorType);
            tab.AutoScroll = true;
            tabControl.TabPages.Add(tab);
            int y = 10;

            AddLabel(tab, $"📐 {description}", 20, y, 900, 25,
                bold: true, color: System.Drawing.Color.DarkGreen);
            y += 35;

            AddLabel(tab, "CAD File:", 20, y, 85, 25);
            var txtCAD = new TextBox
            {
                Location = new System.Drawing.Point(110, y - 2),
                Size = new System.Drawing.Size(610, 25),
                ReadOnly = true
            };
            tab.Controls.Add(txtCAD);
            cadPathTextBoxes[floorType] = txtCAD;

            var btnLoad = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(730, y - 4),
                Size = new System.Drawing.Size(110, 28)
            };
            btnLoad.Click += (s, ev) => BtnLoadCAD_Click(floorType);
            tab.Controls.Add(btnLoad);
            y += 42;

            int numFloors = chkTypical.Checked ? (int)numTypicalLevels.Value : 20;
            string seisZone = cmbSeismicZone.SelectedItem?.ToString()
                              ?? "Zone IV (Ahmedabad & Kolkata)";

            if (!slabLoadSetTextBoxesPerFloor.ContainsKey(floorType))
                slabLoadSetTextBoxesPerFloor[floorType] = new Dictionary<string, TextBox>();

            AddLayerMappingUI(tab, floorType, ref y);
            AddWallThicknessUI(tab, floorType, numFloors, seisZone, ref y);
            AddBeamDepthsUI(tab, floorType, namedGravityBeams, numFloors, seisZone, ref y);
            AddSlabThicknessesUI(tab, floorType, ref y);
        }

        // ====================================================================
        // LAYER MAPPING UI
        // ====================================================================

        private void AddLayerMappingUI(TabPage tab, string floorType, ref int y)
        {
            AddLabel(tab, "Available CAD Layers:", 20, y, 200, 20);

            var lstAvail = new ListBox
            { Location = new System.Drawing.Point(20, y + 22), Size = new System.Drawing.Size(305, 215) };
            tab.Controls.Add(lstAvail);
            availableLayerListBoxes[floorType] = lstAvail;

            AddLabel(tab, "Assign as:", 342, y + 22, 90, 20);
            var cboElem = new ComboBox
            {
                Location = new System.Drawing.Point(342, y + 44),
                Size = new System.Drawing.Size(145, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboElem.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Column", "Ignore" });
            cboElem.SelectedIndex = 0;
            tab.Controls.Add(cboElem);
            elementTypeComboBoxes[floorType] = cboElem;

            var btnAdd = new Button
            { Text = "Add  →", Location = new System.Drawing.Point(342, y + 78), Size = new System.Drawing.Size(145, 32) };
            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
            tab.Controls.Add(btnAdd);

            var btnRem = new Button
            { Text = "←  Remove", Location = new System.Drawing.Point(342, y + 120), Size = new System.Drawing.Size(145, 32) };
            btnRem.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
            tab.Controls.Add(btnRem);

            AddLabel(tab, "Layer Mappings:", 502, y, 200, 20);
            var lstMap = new ListBox
            { Location = new System.Drawing.Point(502, y + 22), Size = new System.Drawing.Size(338, 215) };
            tab.Controls.Add(lstMap);
            mappedLayerListBoxes[floorType] = lstMap;

            y += 252;
        }

        // ====================================================================
        // WALL THICKNESS UI
        // ====================================================================

        private void AddWallThicknessUI(TabPage tab, string floorType,
            int numFloors, string seisZone, ref int y)
        {
            int gplCore = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.CoreWall, seisZone);
            int gplPerDead = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralDeadWall, seisZone);
            int gplPerPortal = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralPortalWall, seisZone);
            int gplInternal = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.InternalWall, seisZone);

            const int grpH = 195;
            var grp = AddGroupBox(tab,
                "🧱 Wall Thicknesses — GPL Table (IS 1893-2025)  |  Values pre-filled from GPL; edit to override",
                20, y, 920, grpH);

            AddLabel(grp,
                $"Values shown are from GPL table for {numFloors} floors / {seisZone}.  " +
                "Edit any value to override for this floor type.",
                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            const int ry1 = 42;
            AddLabel(grp, "Core Wall (mm):", C1L, ry1, C1N - C1L - 5, 20);
            numCoreWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C1N, ry1 - 2, 100, 700, gplCore, increment: 25);
            AddLabel(grp, $"GPL: {gplCore}", C1N + NW + 4, ry1 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

            AddLabel(grp, "Periph. Dead Wall (mm):", C2L, ry1, C2N - C2L - 5, 20);
            numPeriphDeadWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C2N, ry1 - 2, 100, 700, gplPerDead, increment: 25);
            AddLabel(grp, $"GPL: {gplPerDead}", C2N + NW + 4, ry1 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

            AddLabel(grp, "Periph. Portal Wall (mm):", C3L, ry1, C3N - C3L - 5, 20);
            numPeriphPortalWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C3N, ry1 - 2, 100, 700, gplPerPortal, increment: 25);
            toolTip.SetToolTip(numPeriphPortalWallOverridePerFloor[floorType],
                $"GPL table value for {numFloors} floors: {gplPerPortal} mm");

            const int ry2 = 80;
            AddLabel(grp, "Internal Wall (mm):", C1L, ry2, C1N - C1L - 5, 20);
            numInternalWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C1N, ry2 - 2, 100, 700, gplInternal, increment: 25);
            AddLabel(grp, $"GPL: {gplInternal}", C1N + NW + 4, ry2 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);
            AddLabel(grp,
                "(short wall < 1.8 m may require thicker — see GPL table for coupled shear wall cases)",
                C2L, ry2 + 2, 480, 16,
                italic: true, color: System.Drawing.Color.Gray, fontSize: 7.5f);

            const int ry3 = 110;
            var ntaPanel = new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(C1L, ry3),
                Size = new System.Drawing.Size(880, 46),
                BackColor = System.Drawing.Color.FromArgb(255, 255, 200),
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            };
            grp.Controls.Add(ntaPanel);
            AddLabel(ntaPanel, "W-NTA Wall — Non-structural  (always user defined, mm):",
                8, 5, 340, 20, bold: true, fontSize: 8.5f);
            numNtaWallThicknessPerFloor[floorType] =
                AddNumericCtrl(ntaPanel, 355, 4, 100, 500, 200, increment: 25);
            AddLabel(ntaPanel,
                "Not in GPL table — enter actual partition / non-structural wall thickness for this floor.",
                8, 25, 855, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            AddLabel(grp,
                "Wall pier labels in ETABS: P1, P2, P3 … (auto-assigned per wall element)",
                C1L, 162, 880, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            y += grpH + 8;
        }

        private static int SafeGetGPL(int floors,
            Core.WallThicknessCalculator.WallType wallType, string seisZone)
        {
            try
            {
                int f = Math.Max(1, Math.Min(50, floors));
                return Core.WallThicknessCalculator.GetRecommendedThickness(f, wallType, seisZone);
            }
            catch
            {
                return wallType == Core.WallThicknessCalculator.WallType.CoreWall ? 300 : 200;
            }
        }

        // ====================================================================
        // BEAM DEPTHS + WIDTH OVERRIDES + BEAM WALL LOAD SETS UI
        // ====================================================================
        // Width default rules (from reference table Image 2):
        //   Gravity beams  → zone width: 200 (Zone II/III) or 240 (Zone IV/V)  — user editable
        //   Main beams     → GPL wall thickness for that beam type              — user editable
        //   B-No Load      → zone width (same as gravity); load set locked "0"
        //
        // FIX: grpHeight now correctly includes the 46-px header block that
        //      sits above the first gy row, eliminating the clipping overlap.
        // ====================================================================

        private void AddBeamDepthsUI(TabPage tab, string floorType,
            List<(string label, string dictKey)> namedGravityBeams,
            int numFloors, string seisZone, ref int y)
        {
            int extraRows = namedGravityBeams?.Count ?? 0;

            // ── Accurate group-box height ──────────────────────────────────
            // Header block (green note + column-header row) occupies 46 px
            // before the first gy-row.  Every section label adds 26 px.
            // Each beam row = RH (32). Footer note = 32. Bottom pad = 10.
            int grpHeight = 46                // green note (18) + col-headers row (18) + gap (10)
                          + 26               // "─── GRAVITY BEAMS ───"
                          + (3 * RH)         // Internal / Cantilever / NoLoad gravity
                          + (extraRows * RH) // named gravity variants
                          + 20              // named-variants note line
                          + 26              // "─── MAIN BEAMS ───"
                          + (4 * RH)        // Core / PeriphDead / PeriphPortal / Internal main
                          + 32              // footer note (2 lines)
                          + 10;             // bottom padding

            var grp = AddGroupBox(tab,
                "🔧 Beam Configuration — Depth | Width | Wall Load Set (ETABS pattern name)",
                20, y, 920, grpHeight);

            // ── Derive defaults ────────────────────────────────────────────
            // Gravity beams  → zone width
            int gw = GetAutoGravityWidthFromUI();   // 200 or 240 mm

            // Main beams → GPL wall thickness per type (user editable)
            int wCore = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.CoreWall, seisZone);
            int wPeriphDead = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralDeadWall, seisZone);
            int wPeriphPort = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.PeripheralPortalWall, seisZone);
            int wInternal = SafeGetGPL(numFloors, Core.WallThicknessCalculator.WallType.InternalWall, seisZone);

            AddLabel(grp,
                $"Gravity width default: {gw} mm (zone, editable)  |  " +
                "Main beam width default = GPL wall thickness (editable)  |  " +
                "Load set = ETABS wall-load pattern name",
                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            // ── Column headers — must align with AddBeamRow positions ──────
            //   label: C1L → C1L+220, depthX = C1L+226, widthX = depthX+93, lsX = widthX+93
            const int beamLabelW = 220;
            int hDepthX = C1L + beamLabelW + 6;   // 246
            int hNudGap = NW + 8;                  // 93

            AddLabel(grp, "Depth (mm)", hDepthX, 4, NW, 16, bold: true, fontSize: 7.5f,
                color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grp, "Width (mm)", hDepthX + hNudGap, 4, NW, 16, bold: true, fontSize: 7.5f,
                color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grp, "Wall Load Set", hDepthX + hNudGap * 2, 4, LW, 16, bold: true, fontSize: 7.5f,
                color: System.Drawing.Color.DarkSlateGray);

            int gy = 46;   // ← must match top of grpHeight formula above

            // ── GRAVITY BEAMS ─────────────────────────────────────────────
            AddLabel(grp, "─── GRAVITY BEAMS ───", C1L, gy, 260, 18, bold: true, fontSize: 8.5f);
            gy += 26;

            AddBeamRow(grp, floorType, gy,
                "B-Internal Gravity:",
                numInternalGravityDepthPerFloor, 450,
                numInternalGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.",
                txtInternalGravityLoadSetPerFloor, "WALL LOAD", noLoad: false);
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Cantilever Gravity:",
                numCantileverGravityDepthPerFloor, 500,
                numCantileverGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.",
                txtCantileverGravityLoadSetPerFloor, "WALL LOAD", noLoad: false);
            gy += RH;

            // B-No Load: zone width, load set locked "0"
            AddBeamRow(grp, floorType, gy,
                "B-No Load Gravity:",
                numNoLoadGravityDepthPerFloor, 450,
                numNoLoadGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.",
                txtNoLoadGravityLoadSetPerFloor, "0", noLoad: true);
            gy += RH;

            // Named gravity variants
            if (namedGravityBeams != null)
            {
                foreach (var (beamLabel, dictKey) in namedGravityBeams)
                {
                    Dictionary<string, NumericUpDown> depthDict, widthDict;
                    Dictionary<string, TextBox> lsDict;
                    switch (dictKey)
                    {
                        case "EDeck":
                            depthDict = numEDeckGravityDepthPerFloor;
                            widthDict = numEDeckGravityWidthPerFloor;
                            lsDict = txtEDeckGravityLoadSetPerFloor; break;
                        case "Podium":
                            depthDict = numPodiumGravityDepthPerFloor;
                            widthDict = numPodiumGravityWidthPerFloor;
                            lsDict = txtPodiumGravityLoadSetPerFloor; break;
                        case "Ground":
                            depthDict = numGroundGravityDepthPerFloor;
                            widthDict = numGroundGravityWidthPerFloor;
                            lsDict = txtGroundGravityLoadSetPerFloor; break;
                        default: // Basement
                            depthDict = numBasementGravityDepthPerFloor;
                            widthDict = numBasementGravityWidthPerFloor;
                            lsDict = txtBasementGravityLoadSetPerFloor; break;
                    }
                    AddBeamRow(grp, floorType, gy,
                        $"{beamLabel}:",
                        depthDict, 450,
                        widthDict, gw,
                        $"Default {gw} mm (zone). Editable.",
                        lsDict, "WALL LOAD", noLoad: false);
                    gy += RH;
                }
            }

            AddLabel(grp,
                "(Named gravity variants use their own depth/width/load-set. Load assigned per CAD layer.)",
                C1L, gy, 880, 16, italic: true, color: System.Drawing.Color.Gray, fontSize: 7.5f);
            gy += 20;

            // ── MAIN BEAMS ────────────────────────────────────────────────
            // Width default = GPL wall thickness per type; user editable
            AddLabel(grp,
                "─── MAIN BEAMS (MB__ sections) — width defaults from GPL wall thickness; editable ───",
                C1L, gy, 720, 18, bold: true, fontSize: 8.5f);
            gy += 26;

            AddBeamRow(grp, floorType, gy,
                "B-Core Main:",
                numCoreMainDepthPerFloor, 600,
                numCoreMainWidthOverridePerFloor, wCore,
                $"Default = Core Wall GPL ({wCore} mm). Editable.",
                txtCoreMainLoadSetPerFloor, "WALL LOAD", noLoad: false, isMain: true);
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Periph. Dead Main:",
                numPeripheralDeadMainDepthPerFloor, 600,
                numPeripheralDeadMainWidthOverridePerFloor, wPeriphDead,
                $"Default = Peripheral Dead Wall GPL ({wPeriphDead} mm). Editable.",
                txtPeripheralDeadMainLoadSetPerFloor, "WALL LOAD", noLoad: false, isMain: true);
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Periph. Portal Main:",
                numPeripheralPortalMainDepthPerFloor, 650,
                numPeripheralPortalMainWidthOverridePerFloor, wPeriphPort,
                $"Default = Peripheral Portal Wall GPL ({wPeriphPort} mm). Editable.",
                txtPeripheralPortalMainLoadSetPerFloor, "WALL LOAD", noLoad: false, isMain: true);
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Internal Main:",
                numInternalMainDepthPerFloor, 550,
                numInternalMainWidthOverridePerFloor, wInternal,
                $"Default = Internal Wall GPL ({wInternal} mm). Editable.",
                txtInternalMainLoadSetPerFloor, "WALL LOAD", noLoad: false, isMain: true);
            gy += RH;

            AddLabel(grp,
                "💡 Gravity width = zone (200/240 mm).  Main beam width = GPL wall thickness.  " +
                "Both are user-editable per floor type.  Load Set = ETABS pattern name for WALL LOAD case.",
                C1L, gy, 890, 30, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            y += grpHeight + 12;
        }

        // ====================================================================
        // ADD BEAM ROW
        // ====================================================================
        /// <summary>
        /// Renders: [Label 220px] [Depth NUD] [Width NUD — pre-filled, editable] [Load-Set TextBox]
        /// noLoad=true  → load-set pre-filled "0" and ReadOnly (grey).
        /// isMain=false → gravity beam ranges (200–1200 mm depth).
        /// isMain=true  → main beam ranges   (300–1500 mm depth).
        /// </summary>
        private void AddBeamRow(
            Control parent, string floorType, int gy,
            string label,
            Dictionary<string, NumericUpDown> depthDict, int defaultDepth,
            Dictionary<string, NumericUpDown> widthDict, int defaultWidth,
            string widthTooltip,
            Dictionary<string, TextBox> loadSetDict, string defaultLoadSet,
            bool noLoad,
            bool isMain = false)
        {
            int depthMax = isMain ? 1500 : 1200;
            int depthMin = isMain ? 300 : 200;

            const int beamLabelW = 220;
            int depthX = C1L + beamLabelW + 6;   // 246
            int nudGap = NW + 8;                  // 93

            // Label
            AddLabel(parent, label, C1L, gy, beamLabelW, 20);

            // Depth NUD
            var numDepth = AddNumericCtrl(parent, depthX, gy - 2, depthMin, depthMax, defaultDepth, increment: 25);
            depthDict[floorType] = numDepth;

            // Width NUD — pre-filled with zone width (gravity) or GPL wall thickness (main).
            // Minimum 100 mm so the actual value is always visible; user can edit freely.
            int widthX = depthX + nudGap;
            int wClamped = Math.Max(100, Math.Min(600, defaultWidth));
            var numWidth = AddNumericCtrl(parent, widthX, gy - 2, 100, 600, wClamped, increment: 10);
            widthDict[floorType] = numWidth;
            toolTip.SetToolTip(numWidth, widthTooltip);

            // Load Set TextBox
            int lsX = widthX + nudGap;
            var txtLS = new TextBox
            {
                Location = new System.Drawing.Point(lsX, gy - 2),
                Size = new System.Drawing.Size(LW, NH),
                Text = defaultLoadSet,
                ReadOnly = noLoad,
                BackColor = noLoad
                    ? System.Drawing.Color.FromArgb(230, 230, 230)
                    : System.Drawing.Color.FromArgb(255, 255, 220)
            };
            parent.Controls.Add(txtLS);
            loadSetDict[floorType] = txtLS;

            toolTip.SetToolTip(txtLS, noLoad
                ? "B-No Load Gravity beams carry zero wall load — locked to '0'."
                : "Enter the ETABS load pattern name for the wall UDL (WALL LOAD case).");
        }

        // ====================================================================
        // SLAB THICKNESSES + SLAB LOAD SETS UI
        // ====================================================================

        private void AddSlabThicknessesUI(TabPage tab, string floorType, ref int y)
        {
            // ── Section A: YELLOW layers ──────────────────────────────────
            const int yellowH = 260;
            var grpYellow = AddGroupBox(tab,
                "🟡 YELLOW Slabs — Fixed User Thickness + Load Set (structural special-purpose)",
                20, y, 920, yellowH);

            AddLabel(grpYellow,
                "Enter slab thickness (mm) and the ETABS load pattern name for each YELLOW layer.",
                15, 18, 880, 16, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            const int tCol = 140, lsCol = 240;
            AddLabel(grpYellow, "Thickness", tCol, 2, NW, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpYellow, "Load Set", lsCol, 2, LW, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpYellow, "Thickness", tCol + 390, 2, NW, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpYellow, "Load Set", lsCol + 390, 2, LW, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);

            const int sr = 32;
            int sy = 36;

            void AddYellowRow(Control p, string lbl, string slabKey,
                int lx, int tx, int lsx, int rowY,
                Dictionary<string, NumericUpDown> thkDict, int defThk, string defLS)
            {
                AddLabel(p, lbl, lx, rowY, tx - lx - 4, 20);
                var nud = AddNumericCtrl(p, tx, rowY - 2, 100, 600, defThk, increment: 5);
                thkDict[floorType] = nud;
                var txtLS = new TextBox
                {
                    Location = new System.Drawing.Point(lsx, rowY - 2),
                    Size = new System.Drawing.Size(LW, NH),
                    Text = defLS,
                    BackColor = System.Drawing.Color.FromArgb(255, 255, 220)
                };
                p.Controls.Add(txtLS);
                slabLoadSetTextBoxesPerFloor[floorType][slabKey] = txtLS;
                toolTip.SetToolTip(txtLS, $"ETABS load pattern name for {lbl.TrimEnd(':')} slab.");
            }

            const int l1 = 20, t1 = tCol, ls1 = lsCol;
            AddYellowRow(grpYellow, "S-LOBBY:", "Lobby", l1, t1, ls1, sy, numLobbySlabThicknessPerFloor, 160, "LOBBY"); sy += sr;
            AddYellowRow(grpYellow, "S-STAIRCASE:", "Staircase", l1, t1, ls1, sy, numStairSlabThicknessPerFloor, 175, "STAIRCASE"); sy += sr;
            AddYellowRow(grpYellow, "S-FIRE TENDER:", "FireTender", l1, t1, ls1, sy, numFireTenderSlabPerFloor, 200, "FIRE TENDER"); sy += sr;
            AddYellowRow(grpYellow, "S-OHT:", "OHT", l1, t1, ls1, sy, numOHTSlabPerFloor, 200, "OHT"); sy += sr;
            AddYellowRow(grpYellow, "S-TERRACE FIRE:", "TerraceFire", l1, t1, ls1, sy, numTerraceFireSlabPerFloor, 200, "TERRACE FIRE TANK"); sy += sr;

            sy = 36;
            const int l2 = 410, t2 = tCol + 390, ls2 = lsCol + 390;
            AddYellowRow(grpYellow, "S-UGT:", "UGT", l2, t2, ls2, sy, numUGTSlabPerFloor, 250, "UGT"); sy += sr;
            AddYellowRow(grpYellow, "S-LANDSCAPE:", "Landscape", l2, t2, ls2, sy, numLandscapeSlabPerFloor, 175, "LANDSCAPE"); sy += sr;
            AddYellowRow(grpYellow, "S-SWIMMING:", "Swimming", l2, t2, ls2, sy, numSwimmingSlabPerFloor, 250, "SWIMMING"); sy += sr;
            AddYellowRow(grpYellow, "S-DG:", "DG", l2, t2, ls2, sy, numDGSlabPerFloor, 200, "DG"); sy += sr;
            AddYellowRow(grpYellow, "S-STP:", "STP", l2, t2, ls2, sy, numSTPSlabPerFloor, 200, "STP"); sy += sr;

            AddLabel(grpYellow,
                "Thickness is fixed (user-defined). ETABS auto-meshes and assigns load after import.",
                15, sy, 880, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            y += yellowH + 6;

            // ── Section B: WHITE layers ───────────────────────────────────
            var whiteKeys = new (string lbl, string key, string defLS)[]
            {
                ("S-AMENITIES",         "Amenities",        "AMENITIES"),
                ("S-DRIVEWAY",          "Driveway",         "DRIVEWAY"),
                ("S-FIRE WATER TANK",   "FireWaterTank",    "FIRE WATER TANK"),
                ("S-GARBAGE ROOM",      "GarbageRoom",      "GARBAGE ROOM"),
                ("S-GARDEN/DINING",     "GardenDining",     "GARDEN/DINING AREA"),
                ("S-GYMNASIUM",         "Gymnasium",        "GYMNASIUM"),
                ("S-INDOOR SPORTS",     "IndoorSports",     "INDOOR SPORTS"),
                ("S-KITCHEN SUNK",      "KitchenSink",      "KITCHEN SUNK"),
                ("S-LMR",               "LMR",              "LMR"),
                ("S-LMRTOP",            "LMRTop",           "LMRTOP"),
                ("S-METER ROOM",        "MeterRoom",        "METER ROOM"),
                ("S-MULTIPURPOSE HALL", "MultipurposeHall", "MULTIPURPOSE HALL"),
                ("S-OHT TOP",           "OHTTop",           "OHT TOP"),
                ("S-PARKING",           "Parking",          "PARKING"),
                ("S-PARKING TOILET",    "ParkingToilet",    "PARKING TOILET"),
                ("S-PUMP ROOM",         "PumpRoom",         "PUMP ROOM"),
                ("S-REFUGE",            "Refuge",           "REFUGE"),
                ("S-RESIDENTIAL",       "Residential",      "RESIDENTIAL"),
                ("S-RETAIL",            "Retail",           "RETAIL"),
                ("S-RETAIL MAZZANINE",  "RetailMazzanine",  "RETAIL MAZZANINE"),
                ("S-RETAIL TOILET",     "RetailToilet",     "RETAIL TOILET"),
                ("S-SERVICE SLAB",      "ServiceSlab",      "SERVICE SLAB"),
                ("S-SOCIETY ROOM",      "SocietyRoom",      "SOCIETY ROOM"),
                ("S-STACK PARKING",     "StackParking",     "STACK PARKING"),
                ("S-TERRACE",           "Terrace",          "TERRACE"),
                ("S-TERRACE PUMP ROOM", "TerracePumpRoom",  "TERRACE PUMP ROOM"),
                ("S-TOILET",            "Toilet",           "TOILET"),
                ("S-UTILITY",           "Utility",          "UTILITY"),
            };

            int whiteRows = (int)Math.Ceiling(whiteKeys.Length / 3.0);
            int whiteH = 36 + (whiteRows * 26) + 22;
            var grpWhite = AddGroupBox(tab,
                "⬜ WHITE Slabs — Auto Thickness (area rule 14–70 m² → 125–250 mm) | Edit Load Set per floor",
                20, y, 920, whiteH);

            AddLabel(grpWhite,
                "Thickness is computed automatically from polygon area. " +
                "Only the ETABS load pattern name is editable here.",
                15, 18, 880, 16, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            const int wColW = 300, wLabelW = 150, wLsW = 140;
            for (int i = 0; i < whiteKeys.Length; i++)
            {
                var (wLbl, wKey, wLS) = whiteKeys[i];
                int col = i % 3, row = i / 3;
                int cx = 20 + col * wColW;
                int cy = 36 + row * 26;
                AddLabel(grpWhite, wLbl + ":", cx, cy, wLabelW, 20, fontSize: 8);
                var txtLS = new TextBox
                {
                    Location = new System.Drawing.Point(cx + wLabelW + 4, cy - 1),
                    Size = new System.Drawing.Size(wLsW, 22),
                    Text = wLS,
                    Font = new System.Drawing.Font("Segoe UI", 8F),
                    BackColor = System.Drawing.Color.FromArgb(235, 245, 255)
                };
                grpWhite.Controls.Add(txtLS);
                slabLoadSetTextBoxesPerFloor[floorType][wKey] = txtLS;
            }
            y += whiteH + 6;

            // ── Section C: CYAN layers ────────────────────────────────────
            var cyanKeys = new (string lbl, string key, string defLS)[]
            {
                ("S-BALCONY",    "Balcony",   "BALCONY"),
                ("S-CHAJJA",     "Chajja",    "CHAJJA"),
                ("S-CHAJJA+ODU", "ChajjaODU", "CHAJJA+ODU"),
            };

            int cyanH = 20 + 20 + 8 + (cyanKeys.Length * 28) + 20;
            var grpCyan = AddGroupBox(tab,
                "🩵 CYAN Slabs — Auto Thickness (cantilever span 1.0–5.0 m → 125–200 mm) | Edit Load Set per floor",
                20, y, 920, cyanH);

            AddLabel(grpCyan,
                "Thickness is computed automatically from cantilever span. " +
                "Only the ETABS load pattern name is editable here.",
                15, 18, 880, 16, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            int cy2 = 40;
            foreach (var (cLbl, cKey, cLS) in cyanKeys)
            {
                AddLabel(grpCyan, cLbl + ":", 20, cy2, 160, 22, fontSize: 8);
                var txtLS = new TextBox
                {
                    Location = new System.Drawing.Point(185, cy2 - 1),
                    Size = new System.Drawing.Size(LW, 22),
                    Text = cLS,
                    Font = new System.Drawing.Font("Segoe UI", 8F),
                    BackColor = System.Drawing.Color.FromArgb(200, 240, 255)
                };
                grpCyan.Controls.Add(txtLS);
                slabLoadSetTextBoxesPerFloor[floorType][cKey] = txtLS;
                cy2 += 28;
            }

            // Spacer forces AutoScroll to reveal full bottom margin
            tab.Controls.Add(new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(0, y + cyanH + 12),
                Size = new System.Drawing.Size(1, 1),
                BackColor = System.Drawing.Color.Transparent
            });
            y += cyanH + 12;
        }

        // ====================================================================
        // GENERATE TABS
        // ====================================================================

        private void BtnGenerateTabs_Click(object sender, EventArgs e)
        {
            if (!chkBasement.Checked && !chkPodium.Checked && !chkGround.Checked &&
                !chkEDeck.Checked && !chkTypical.Checked && !chkTerrace.Checked)
            {
                MessageBox.Show("Please select at least one floor type!", "No Floors Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            while (tabControl.TabPages.Count > 2)
                tabControl.TabPages.RemoveAt(2);

            ClearAllFloorDicts();

            int tabCount = 0;

            if (chkBasement.Checked)
            {
                int cnt = (int)numBasementLevels.Value;
                for (int i = 1; i <= cnt; i++)
                {
                    CreateCADImportTab($"Basement{i}", $"Basement {i} Floor Plan",
                        new List<(string, string)> { ($"B-Basement{i} Gravity", "Basement") });
                    tabCount++;
                }
            }
            if (chkPodium.Checked)
            {
                int cnt = (int)numPodiumLevels.Value;
                for (int i = 1; i <= cnt; i++)
                {
                    CreateCADImportTab($"Podium{i}", $"Podium {i} Floor Plan",
                        new List<(string, string)> { ($"B-Podium{i} Gravity", "Podium") });
                    tabCount++;
                }
            }
            if (chkGround.Checked)
            {
                CreateCADImportTab("Ground", "Ground Floor Plan",
                    new List<(string, string)> { ("B-Ground Gravity", "Ground") });
                tabCount++;
            }
            if (chkEDeck.Checked)
            {
                CreateCADImportTab("EDeck", "E-Deck Floor Plan",
                    new List<(string, string)> { ("B-Edeck Gravity", "EDeck") });
                tabCount++;
            }
            if (chkTypical.Checked)
            {
                CreateCADImportTab("Typical", "Typical Floor Plan (replicated for all typical floors)");
                tabCount++;
            }
       
            if (chkTerrace.Checked)
            {
                CreateCADImportTab("Terrace", "Terrace Floor Plan");
                tabCount++;
            }

            UpdateTotalFloorsForGradeSchedule();

            var notes = new System.Text.StringBuilder();
            if (chkBasement.Checked)
                notes.AppendLine($"• {(int)numBasementLevels.Value} individual basement tab(s): B1, B2, ...");
            if (chkPodium.Checked)
                notes.AppendLine($"• {(int)numPodiumLevels.Value} individual podium tab(s): P1, P2, ...");
          

            MessageBox.Show(
                $"✓ {tabCount} CAD Import tab(s) generated!\n\n" +
                notes.ToString() +
                "\nWall thicknesses pre-filled from GPL table (IS 1893-2025).\n" +
                "Gravity beam widths pre-filled from zone (200/240 mm) — editable.\n" +
                "Main beam widths pre-filled from GPL wall thickness — editable.\n\n" +
                "For each floor tab:\n" +
                "  1. Browse & load DXF file\n" +
                "  2. Verify / adjust layer mappings\n" +
                "  3. Check wall thicknesses (GPL values pre-filled)\n" +
                "  4. Adjust beam depths, widths, and load set names as needed\n" +
                "  5. Set YELLOW slab thicknesses and verify all load set names\n\n" +
                "Then complete the Concrete Grades schedule.",
                "Tabs Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<int> PreviewRefugePositions()
        {
            int total = 0;
            if (chkBasement.Checked) total += (int)numBasementLevels.Value;
            if (chkPodium.Checked) total += (int)numPodiumLevels.Value;
            if (chkGround.Checked) total += 1;
            if (chkEDeck.Checked) total += 1;
            if (chkTypical.Checked) total += (int)numTypicalLevels.Value;
            var positions = new List<int>();
            for (int i = 1; i <= total; i++)
                if (i % 5 == 0) positions.Add(i);
            return positions;
        }

        // ====================================================================
        // CLEAR ALL FLOOR DICTS
        // ====================================================================

        private void ClearAllFloorDicts()
        {
            cadPathTextBoxes.Clear();
            availableLayerListBoxes.Clear();
            mappedLayerListBoxes.Clear();
            elementTypeComboBoxes.Clear();

            numInternalGravityDepthPerFloor.Clear();
            numCantileverGravityDepthPerFloor.Clear();
            numNoLoadGravityDepthPerFloor.Clear();
            numEDeckGravityDepthPerFloor.Clear();
            numPodiumGravityDepthPerFloor.Clear();
            numGroundGravityDepthPerFloor.Clear();
            numBasementGravityDepthPerFloor.Clear();

            numCoreMainDepthPerFloor.Clear();
            numPeripheralDeadMainDepthPerFloor.Clear();
            numPeripheralPortalMainDepthPerFloor.Clear();
            numInternalMainDepthPerFloor.Clear();

            numInternalGravityWidthPerFloor.Clear();
            numCantileverGravityWidthPerFloor.Clear();
            numNoLoadGravityWidthPerFloor.Clear();
            numEDeckGravityWidthPerFloor.Clear();
            numPodiumGravityWidthPerFloor.Clear();
            numGroundGravityWidthPerFloor.Clear();
            numBasementGravityWidthPerFloor.Clear();

            numCoreMainWidthOverridePerFloor.Clear();
            numPeripheralDeadMainWidthOverridePerFloor.Clear();
            numPeripheralPortalMainWidthOverridePerFloor.Clear();
            numInternalMainWidthOverridePerFloor.Clear();

            txtInternalGravityLoadSetPerFloor.Clear();
            txtCantileverGravityLoadSetPerFloor.Clear();
            txtNoLoadGravityLoadSetPerFloor.Clear();
            txtEDeckGravityLoadSetPerFloor.Clear();
            txtPodiumGravityLoadSetPerFloor.Clear();
            txtGroundGravityLoadSetPerFloor.Clear();
            txtBasementGravityLoadSetPerFloor.Clear();
            txtCoreMainLoadSetPerFloor.Clear();
            txtPeripheralDeadMainLoadSetPerFloor.Clear();
            txtPeripheralPortalMainLoadSetPerFloor.Clear();
            txtInternalMainLoadSetPerFloor.Clear();

            numLobbySlabThicknessPerFloor.Clear();
            numStairSlabThicknessPerFloor.Clear();
            numFireTenderSlabPerFloor.Clear();
            numOHTSlabPerFloor.Clear();
            numTerraceFireSlabPerFloor.Clear();
            numUGTSlabPerFloor.Clear();
            numLandscapeSlabPerFloor.Clear();
            numSwimmingSlabPerFloor.Clear();
            numDGSlabPerFloor.Clear();
            numSTPSlabPerFloor.Clear();

            slabLoadSetTextBoxesPerFloor.Clear();

            numCoreWallOverridePerFloor.Clear();
            numPeriphDeadWallOverridePerFloor.Clear();
            numPeriphPortalWallOverridePerFloor.Clear();
            numInternalWallOverridePerFloor.Clear();
            numNtaWallThicknessPerFloor.Clear();
        }

        // ====================================================================
        // UI HELPER METHODS
        // ====================================================================

        private int GetAutoGravityWidthFromUI()
        {
            string zone = cmbSeismicZone.SelectedItem?.ToString() ?? "";
            return (zone.Contains("II") || zone.Contains("III")) ? 200 : 240;
        }

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
            if (color.HasValue) lbl.ForeColor = color.Value;
            parent.Controls.Add(lbl);
            return lbl;
        }

        private GroupBox AddGroupBox(Control parent, string text, int x, int y, int width, int height)
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
                Size = new System.Drawing.Size(420, 20)
            };
            parent.Controls.Add(chk);
            return chk;
        }

        private NumericUpDown AddNumericCtrl(Control parent, int x, int y,
            decimal min, decimal max, decimal value,
            int decimals = 0, decimal increment = 1, bool enabled = true)
        {
            var num = new NumericUpDown
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(NW, NH),
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, value)),
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
