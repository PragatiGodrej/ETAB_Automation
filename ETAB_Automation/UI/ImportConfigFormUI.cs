





using ETAB_Automation.Core;
using ETAB_Automation.Models;
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
            this.Text = "ETABS CAD Import Configuration v3.0";

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

            var tabLoadSets = new TabPage("Loads & Load Sets");
            tabControl.TabPages.Add(tabLoadSets);
            InitializeLoadSetsTab(tabLoadSets);

            var tabSlabRules = new TabPage("Slab Thickness Rules");
            tabControl.TabPages.Add(tabSlabRules);
            InitializeSlabThicknessRulesTab(tabSlabRules);

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
            AddLabel(grpF, "m", 290, 52, 25, 20, color: System.Drawing.Color.DimGray);
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
            AddLabel(grpB, "m", 635, 52, 25, 20, color: System.Drawing.Color.DimGray);
            AddLabel(grpB, "⚠️ One CAD tab will be created per basement floor (B1, B2, ...)",
                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
            y += 118;

            var grpGr = AddGroupBox(tab, "Ground Floor", 20, y, 910, 82);
            chkGround = AddCheckBox(grpGr, "Include Ground Floor", 15, 25);
            chkGround.CheckedChanged += ChkGround_CheckedChanged;
            AddLabel(grpGr, "Ground Floor Height (m):", 35, 52, 180, 20);
            numGroundHeight = AddNumericCtrl(grpGr, 220, 50, 3.0M, 10.0M, 4.0M, decimals: 2, enabled: false);
            AddLabel(grpGr, "m", 310, 52, 25, 20, color: System.Drawing.Color.DimGray);
            y += 92;

            var grpP = AddGroupBox(tab,
                "Podium Floors  (each floor gets its own CAD tab — like Basements)", 20, y, 910, 108);
            chkPodium = AddCheckBox(grpP, "Include Podium Floors", 15, 25);
            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
            AddLabel(grpP, "Number of Podiums (1–5):", 35, 52, 200, 20);
            numPodiumLevels = AddNumericCtrl(grpP, 240, 50, 1, 5, 1, enabled: false);
            numPodiumLevels.ValueChanged += NumPodiumLevels_ValueChanged;
            AddLabel(grpP, "Each Podium Height (m):", 345, 52, 185, 20);
            numPodiumHeight = AddNumericCtrl(grpP, 535, 50, 3.0M, 8.0M, 4.5M, decimals: 2, enabled: false);
            AddLabel(grpP, "m", 625, 52, 25, 20, color: System.Drawing.Color.DimGray);
            AddLabel(grpP, "⚠️ One CAD tab will be created per podium floor (P1, P2, ...)",
                35, 80, 840, 20, italic: true, color: System.Drawing.Color.DarkRed, fontSize: 8);
            y += 118;

            var grpE = AddGroupBox(tab, "E-Deck Floor", 20, y, 910, 82);
            chkEDeck = AddCheckBox(grpE, "Include E-Deck Floor", 15, 25);
            chkEDeck.CheckedChanged += ChkEDeck_CheckedChanged;
            AddLabel(grpE, "E-Deck Height (m):", 35, 52, 150, 20);
            numEDeckHeight = AddNumericCtrl(grpE, 190, 50, 3.0M, 10.0M, 4.5M, decimals: 2, enabled: false);
            AddLabel(grpE, "m", 280, 52, 25, 20, color: System.Drawing.Color.DimGray);
            y += 92;

            var grpT = AddGroupBox(tab, "Typical Floors", 20, y, 910, 82);
            chkTypical = AddCheckBox(grpT, "Include Typical Floors", 15, 25);
            chkTypical.CheckedChanged += ChkTypical_CheckedChanged;
            AddLabel(grpT, "Number of Typical Floors:", 35, 52, 190, 20);
            numTypicalLevels = AddNumericCtrl(grpT, 230, 50, 1, 100, 10, enabled: false);
            numTypicalLevels.ValueChanged += NumTypicalLevels_ValueChanged;
            AddLabel(grpT, "Typical Floor Height (m):", 335, 52, 190, 20);
            numTypicalHeight = AddNumericCtrl(grpT, 530, 50, 2.8M, 5.0M, 3.0M, decimals: 2, enabled: false);
            AddLabel(grpT, "m", 620, 52, 25, 20, color: System.Drawing.Color.DimGray);
            y += 92;

            var grpTr = AddGroupBox(tab, "Terrace Floor  (always pinned as the topmost floor)", 20, y, 910, 52);
            chkTerrace = AddCheckBox(grpTr, "Include Terrace Floor", 15, 20);
            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
            y += 62;

            // ── Seismic Parameters group ─────────────────────────────────
            var grpS = AddGroupBox(tab, "Seismic Parameters", 20, y, 910, 110);

            // ROW 1 — IS Code edition
            AddLabel(grpS, "IS Code Edition:", 35, 28, 120, 20, bold: true);
            cmbISCode = new ComboBox
            {
                Location = new System.Drawing.Point(160, 25),
                Size = new System.Drawing.Size(195, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            cmbISCode.Items.AddRange(new object[]
            {
                "IS 1893 : 2016  (TDD / PKO)",
                "IS 1893 : 2025  (TDD / MSO)"
            });
            cmbISCode.SelectedIndex = 0;   // default = IS 1893:2016
            grpS.Controls.Add(cmbISCode);

            var lblCodeNote = new Label
            {
                Text = "2016 = default standard (TDD/PKO). Switch to 2025 for latest projects.",
                Location = new System.Drawing.Point(365, 28),
                Size = new System.Drawing.Size(535, 18),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkOrange
            };
            grpS.Controls.Add(lblCodeNote);

            // Wire up both code and zone dropdowns to refresh GPL values
            cmbISCode.SelectedIndexChanged += (s, ev) =>
            {
                UpdateSeismicZoneDropdown();
                RefreshWallThicknessDefaults();
            };

            // ROW 2 — Seismic Zone
            AddLabel(grpS, "Seismic Zone:", 35, 62, 115, 20);
            cmbSeismicZone = new ComboBox
            {
                Location = new System.Drawing.Point(155, 59),
                Size = new System.Drawing.Size(310, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Initially populate for IS 2016 (default)
            PopulateSeismicZones2016();
            cmbSeismicZone.SelectedIndex = 0;
            cmbSeismicZone.SelectedIndexChanged += (s, ev) => RefreshWallThicknessDefaults();
            grpS.Controls.Add(cmbSeismicZone);

            AddLabel(grpS, "Zone II/III → gravity beam 200 mm  |  Zone IV/V/VI → 240 mm",
                475, 62, 420, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            y += 120;

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
        // SEISMIC ZONE DROPDOWN — populated per IS code edition
        // ====================================================================

        /// <summary>
        /// Called when cmbISCode changes — repopulates the zone dropdown with
        /// the correct zone strings for the selected IS code edition.
        /// These strings MUST match the case strings in WallThicknessCalculator.
        /// </summary>
        private void UpdateSeismicZoneDropdown()
        {
            if (cmbISCode == null || cmbSeismicZone == null) return;

            string currentZone = cmbSeismicZone.SelectedItem?.ToString() ?? "";
            cmbSeismicZone.Items.Clear();

            if (cmbISCode.SelectedIndex == 0)
                PopulateSeismicZones2016();
            else
                PopulateSeismicZones2025();

            // Try to keep the same zone selected after switch
            int idx = cmbSeismicZone.FindStringExact(currentZone);
            cmbSeismicZone.SelectedIndex = (idx >= 0) ? idx : 0;
        }

        private void PopulateSeismicZones2025()
        {
            // Zone strings MUST exactly match WallThicknessCalculator GetThickness2025 switch
            cmbSeismicZone.Items.AddRange(new object[]
            {
                "Zone II (Bangalore, Hyderabad)",
                "Zone III (MMR & Pune)",
                "Zone IV (Ahmedabad & Kolkata)",
                "Zone IV (NCR)",
                "Zone V"
            });
        }

        private void PopulateSeismicZones2016()
        {
            // Zone strings MUST exactly match WallThicknessCalculator GetThickness2016 switch
            cmbSeismicZone.Items.AddRange(new object[]
            {
                "Zone II (Bangalore, Hyderabad)",
                "Zone III (MMR, Ahmedabad, Kolkata, Pune)",
                "Zone IV (NCR)",
                "Zone V"
            });
            // Note: "Zone IV (Ahmedabad & Kolkata)" is intentionally excluded for IS 2016
            // because IS 2016 lists those cities under Zone III. The calculator handles
            // this string gracefully by routing to GetZone3_2016 if it arrives, but the
            // UI should not offer it as a separate selection for IS 2016.
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
                    { "M20","M25","M30","M35","M40","M45","M50","M55","M60","M65","M70" },
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
                              ?? "Zone II (Bangalore, Hyderabad)";

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
            var isCode = (cmbISCode?.SelectedIndex == 0)
                ? WallThicknessCalculator.ISCodeVersion.IS2016
                : WallThicknessCalculator.ISCodeVersion.IS2025;

            string codeTag = isCode == WallThicknessCalculator.ISCodeVersion.IS2016
                ? "IS 1893:2016" : "IS 1893:2025";

            int gplCore = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.CoreWall, seisZone, isCode);
            int gplPerDead = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, seisZone, isCode);
            int gplPerPortal = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralPortalWall, seisZone, isCode);
            int gplInternal = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.InternalWall, seisZone, isCode);

            const int grpH = 195;
            var grp = AddGroupBox(tab,
                $"🧱 Wall Thicknesses — GPL Table ({codeTag})  |  Values pre-filled from GPL; edit to override",
                20, y, 920, grpH);

            AddLabel(grp,
                $"Values shown are from {codeTag} GPL table for {numFloors} floors / {seisZone}.  " +
                "Edit any value to override for this floor type.",
                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            const int ry1 = 42;
            AddLabel(grp, "Core Wall  (mm):", C1L, ry1, C1N - C1L - 5, 20);
            numCoreWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C1N, ry1 - 2, 100, 700, gplCore, increment: 25);
            AddLabel(grp, $"GPL: {gplCore}", C1N + NW + 4, ry1 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

            AddLabel(grp, "Periph. Dead Wall  (mm):", C2L, ry1, C2N - C2L - 5, 20);
            numPeriphDeadWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C2N, ry1 - 2, 100, 700, gplPerDead, increment: 25);
            AddLabel(grp, $"GPL: {gplPerDead}", C2N + NW + 4, ry1 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);

            AddLabel(grp, "Periph. Portal Wall  (mm):", C3L, ry1, C3N - C3L - 5, 20);
            numPeriphPortalWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C3N, ry1 - 2, 100, 700, gplPerPortal, increment: 25);
            toolTip.SetToolTip(numPeriphPortalWallOverridePerFloor[floorType],
                $"GPL table value for {numFloors} floors, {seisZone}: {gplPerPortal} mm");

            const int ry2 = 80;
            AddLabel(grp, "Internal Wall  (mm):", C1L, ry2, C1N - C1L - 5, 20);
            numInternalWallOverridePerFloor[floorType] =
                AddNumericCtrl(grp, C1N, ry2 - 2, 100, 700, gplInternal, increment: 25);
            AddLabel(grp, $"GPL: {gplInternal}", C1N + NW + 4, ry2 + 2, 62, 16,
                italic: true, color: System.Drawing.Color.DimGray, fontSize: 7.5f);
            AddLabel(grp, "", C2L, ry2 + 2, 480, 16,
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

        // ── SafeGetGPL ───────────────────────────────────────────────────
        private static int SafeGetGPL(int floors,
            WallThicknessCalculator.WallType wallType, string seisZone,
            WallThicknessCalculator.ISCodeVersion isCode = WallThicknessCalculator.ISCodeVersion.IS2025)
        {
            try
            {
                int f = Math.Max(1, Math.Min(50, floors));
                return WallThicknessCalculator.GetRecommendedThickness(
                    f, wallType, seisZone, 2.0, false,
                    WallThicknessCalculator.ConstructionType.TypeII, isCode);
            }
            catch
            {
                // Fallback — should not normally occur if zone names are kept in sync
                return wallType == WallThicknessCalculator.WallType.CoreWall ? 300 :
                       wallType == WallThicknessCalculator.WallType.PeripheralPortalWall ? 300 : 200;
            }
        }

        // ── RefreshWallThicknessDefaults ─────────────────────────────────
        /// <summary>
        /// Re-reads selected IS code + zone and updates all pre-filled GPL
        /// NumericUpDown values on every existing floor tab.
        /// Called when cmbISCode or cmbSeismicZone changes.
        /// </summary>
        private void RefreshWallThicknessDefaults()
        {
            var isCode = (cmbISCode?.SelectedIndex == 0)
                ? WallThicknessCalculator.ISCodeVersion.IS2016
                : WallThicknessCalculator.ISCodeVersion.IS2025;

            string zone = cmbSeismicZone?.SelectedItem?.ToString()
                ?? "Zone II (Bangalore, Hyderabad)";

            int numFloors = chkTypical.Checked
                ? (int)numTypicalLevels.Value : 20;

            foreach (string ft in numCoreWallOverridePerFloor.Keys)
            {
                int gplCore = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.CoreWall, zone, isCode);
                int gplPerDead = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, zone, isCode);
                int gplPerPort = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralPortalWall, zone, isCode);
                int gplInt = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.InternalWall, zone, isCode);

                void SetNud(Dictionary<string, NumericUpDown> dict, int val)
                {
                    if (!dict.ContainsKey(ft)) return;
                    var nud = dict[ft];
                    nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, val));
                }

                SetNud(numCoreWallOverridePerFloor, gplCore);
                SetNud(numPeriphDeadWallOverridePerFloor, gplPerDead);
                SetNud(numPeriphPortalWallOverridePerFloor, gplPerPort);
                SetNud(numInternalWallOverridePerFloor, gplInt);
            }
        }

        // ====================================================================
        // BEAM DEPTHS + WIDTH OVERRIDES UI
        // ====================================================================

        private void AddBeamDepthsUI(TabPage tab, string floorType,
            List<(string label, string dictKey)> namedGravityBeams,
            int numFloors, string seisZone, ref int y)
        {
            int extraRows = namedGravityBeams?.Count ?? 0;

            int grpHeight = 46
                          + 26
                          + (3 * RH)
                          + (extraRows * RH)
                          + 20
                          + 26
                          + (4 * RH)
                          + 32
                          + 10;

            var grp = AddGroupBox(tab,
                "🔧 Beam Configuration — Depth | Width | Wall Load Set (ETABS pattern name)",
                20, y, 920, grpHeight);

            var isCode = (cmbISCode?.SelectedIndex == 0)
                ? WallThicknessCalculator.ISCodeVersion.IS2016
                : WallThicknessCalculator.ISCodeVersion.IS2025;

            int gw = GetAutoGravityWidthFromUI();

            int wCore = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.CoreWall, seisZone, isCode);
            int wPeriphDead = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralDeadWall, seisZone, isCode);
            int wPeriphPort = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.PeripheralPortalWall, seisZone, isCode);
            int wInternal = SafeGetGPL(numFloors, WallThicknessCalculator.WallType.InternalWall, seisZone, isCode);

            AddLabel(grp,
                $"Gravity width default: {gw} mm (zone-based, editable)  |  " +
                "Main beam width default = GPL wall thickness (editable)",
                15, 20, 890, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 9);

            const int beamLabelW = 220;
            int hDepthX = C1L + beamLabelW + 6;
            int hNudGap = NW + 10;

            AddLabel(grp, "Depth (mm)", hDepthX, 40, NW, 18, bold: true, fontSize: 8.5f,
                color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grp, "Width (mm)", hDepthX + hNudGap, 40, NW, 18, bold: true, fontSize: 8.5f,
                color: System.Drawing.Color.DarkSlateGray);
            //AddLabel(grp, "Wall Load Pattern", hDepthX + hNudGap * 2 + 10, 40, 180, 18, bold: true, fontSize: 8.5f,
            //    color: System.Drawing.Color.DarkSlateGray);

            int gy = 64;

            AddLabel(grp, "─── GRAVITY BEAMS ───", C1L, gy, 260, 18, bold: true, fontSize: 9.5f);
            gy += 26;

            AddBeamRow(grp, floorType, gy,
                "B-Internal Gravity:",
                numInternalGravityDepthPerFloor, 450,
                numInternalGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.");
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Cantilever Gravity:",
                numCantileverGravityDepthPerFloor, 500,
                numCantileverGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.");
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-No Load Gravity:",
                numNoLoadGravityDepthPerFloor, 450,
                numNoLoadGravityWidthPerFloor, gw,
                $"Default {gw} mm (zone). Editable.");
            gy += RH;

            if (namedGravityBeams != null)
            {
                foreach (var (beamLabel, dictKey) in namedGravityBeams)
                {
                    Dictionary<string, NumericUpDown> depthDict, widthDict;
                    switch (dictKey)
                    {
                        case "EDeck":
                            depthDict = numEDeckGravityDepthPerFloor;
                            widthDict = numEDeckGravityWidthPerFloor; break;
                        case "Podium":
                            depthDict = numPodiumGravityDepthPerFloor;
                            widthDict = numPodiumGravityWidthPerFloor; break;
                        case "Ground":
                            depthDict = numGroundGravityDepthPerFloor;
                            widthDict = numGroundGravityWidthPerFloor; break;
                        default:
                            depthDict = numBasementGravityDepthPerFloor;
                            widthDict = numBasementGravityWidthPerFloor; break;
                    }
                    AddBeamRow(grp, floorType, gy,
                        $"{beamLabel}:",
                        depthDict, 450,
                        widthDict, gw,
                        $"Default {gw} mm (zone). Editable.");
                    gy += RH;
                }
            }

            AddLabel(grp,
                "(Named gravity variants use their own depth/width per CAD layer.)",
                C1L, gy, 880, 16, italic: true, color: System.Drawing.Color.Gray, fontSize: 8.5f);
            gy += 22;

            AddLabel(grp,
                "─── MAIN BEAMS (MB__ sections) — width defaults from GPL wall thickness; editable ───",
                C1L, gy, 720, 18, bold: true, fontSize: 9.5f);
            gy += 26;

            AddBeamRow(grp, floorType, gy,
                "B-Core Main:",
                numCoreMainDepthPerFloor, 600,
                numCoreMainWidthOverridePerFloor, wCore,
                $"Default = Core Wall GPL ({wCore} mm). Editable.");
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Periph. Dead Main:",
                numPeripheralDeadMainDepthPerFloor, 600,
                numPeripheralDeadMainWidthOverridePerFloor, wPeriphDead,
                $"Default = Peripheral Dead Wall GPL ({wPeriphDead} mm). Editable.");
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Periph. Portal Main:",
                numPeripheralPortalMainDepthPerFloor, 650,
                numPeripheralPortalMainWidthOverridePerFloor, wPeriphPort,
                $"Default = Peripheral Portal Wall GPL ({wPeriphPort} mm). Editable.");
            gy += RH;

            AddBeamRow(grp, floorType, gy,
                "B-Internal Main:",
                numInternalMainDepthPerFloor, 550,
                numInternalMainWidthOverridePerFloor, wInternal,
                $"Default = Internal Wall GPL ({wInternal} mm). Editable.");
            gy += RH;

            AddLabel(grp,
                "💡 Gravity width = zone-based (200 mm Zone II/III, 240 mm Zone IV/V).  " +
                "Main beam width = GPL wall thickness.  Both user-editable per floor type.",
                C1L, gy, 890, 30, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            y += grpHeight + 12;
        }

        // ====================================================================
        // ADD BEAM ROW
        // ====================================================================

        private void AddBeamRow(
            Control parent, string floorType, int gy,
            string label,
            Dictionary<string, NumericUpDown> depthDict, int defaultDepth,
            Dictionary<string, NumericUpDown> widthDict, int defaultWidth,
            string widthTooltip,
            bool isMain = false)
        {
            int depthMax = isMain ? 1500 : 1200;
            int depthMin = isMain ? 300 : 200;
            const int beamLabelW = 220;
            int depthX = C1L + beamLabelW + 6;
            int nudGap = NW + 10;

            AddLabel(parent, label, C1L, gy, beamLabelW, 20, fontSize: 9f);
            var numDepth = AddNumericCtrl(parent, depthX, gy - 2, depthMin, depthMax, defaultDepth, increment: 25);
            depthDict[floorType] = numDepth;

            int widthX = depthX + nudGap;
            int wClamped = Math.Max(100, Math.Min(600, defaultWidth));
            var numWidth = AddNumericCtrl(parent, widthX, gy - 2, 100, 600, wClamped, increment: 10);
            widthDict[floorType] = numWidth;
            toolTip.SetToolTip(numWidth, widthTooltip);
        }

        // ====================================================================
        // SLAB THICKNESSES UI
        // ====================================================================

        private void AddSlabThicknessesUI(TabPage tab, string floorType, ref int y)
        {
            const int yellowH = 260;
            var grpYellow = AddGroupBox(tab,
                "🟡 YELLOW Slabs — Fixed User Thickness",
                20, y, 920, yellowH);

            AddLabel(grpYellow,
                "Enter slab thickness (mm). These layers use a fixed user-defined thickness.",
                15, 18, 880, 16, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 8);

            const int tCol = 140;
            AddLabel(grpYellow, "Thickness (mm)", tCol - 10, 2, NW + 30, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpYellow, "Thickness (mm)", tCol + 380, 2, NW + 30, 14, bold: true, fontSize: 7.5f, color: System.Drawing.Color.DarkSlateGray);

            const int sr = 32;
            int sy = 36;

            void AddYellowRow(Control p, string lbl, string slabKey,
                int lx, int tx, int rowY,
                Dictionary<string, NumericUpDown> thkDict, int defThk)
            {
                AddLabel(p, lbl, lx, rowY, tx - lx - 4, 20);
                var nud = AddNumericCtrl(p, tx, rowY - 2, 100, 600, defThk, increment: 5);
                thkDict[floorType] = nud;
            }

            const int l1 = 20, t1 = tCol;
            AddYellowRow(grpYellow, "S-LOBBY:", "Lobby", l1, t1, sy, numLobbySlabThicknessPerFloor, 160); sy += sr;
            AddYellowRow(grpYellow, "S-STAIRCASE:", "Staircase", l1, t1, sy, numStairSlabThicknessPerFloor, 175); sy += sr;
            AddYellowRow(grpYellow, "S-FIRE TENDER:", "FireTender", l1, t1, sy, numFireTenderSlabPerFloor, 200); sy += sr;
            AddYellowRow(grpYellow, "S-OHT:", "OHT", l1, t1, sy, numOHTSlabPerFloor, 200); sy += sr;
            AddYellowRow(grpYellow, "S-TERRACE FIRE:", "TerraceFire", l1, t1, sy, numTerraceFireSlabPerFloor, 200); sy += sr;

            sy = 36;
            const int l2 = 410, t2 = tCol + 390;
            AddYellowRow(grpYellow, "S-UGT:", "UGT", l2, t2, sy, numUGTSlabPerFloor, 250); sy += sr;
            AddYellowRow(grpYellow, "S-LANDSCAPE:", "Landscape", l2, t2, sy, numLandscapeSlabPerFloor, 175); sy += sr;
            AddYellowRow(grpYellow, "S-SWIMMING:", "Swimming", l2, t2, sy, numSwimmingSlabPerFloor, 250); sy += sr;
            AddYellowRow(grpYellow, "S-DG:", "DG", l2, t2, sy, numDGSlabPerFloor, 200); sy += sr;
            AddYellowRow(grpYellow, "S-STP:", "STP", l2, t2, sy, numSTPSlabPerFloor, 200); sy += sr;

            AddLabel(grpYellow,
                "Thickness is fixed (user-defined).",
                15, sy, 880, 16, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 7.5f);

            y += yellowH + 6;
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

            // Keep the 4 fixed tabs: Building Config, Concrete Grades,
            // Loads & Load Sets, Slab Thickness Rules (indices 0-3).
            // Remove only the dynamically-generated CAD Import tabs (index 4+).
            while (tabControl.TabPages.Count > 4)
                tabControl.TabPages.RemoveAt(4);

            ClearAllFloorDicts();

            int tabCount = 0;

            if (chkBasement.Checked)
            {
                int cnt = (int)numBasementLevels.Value;
                // Inverted: deepest basement = BasementN (bottom), shallowest = Basement1 (near ground)
                for (int i = cnt; i >= 1; i--)
                {
                    CreateCADImportTab($"Basement{i}", $"Basement {i} Floor Plan",
                        new List<(string, string)> { ($"B-Basement{i} Gravity", "Basement") });
                    tabCount++;
                }
            }
            if (chkGround.Checked)
            {
                CreateCADImportTab("Ground", "Ground Floor Plan",
                    new List<(string, string)> { ("B-Ground Gravity", "Ground") });
                tabCount++;
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

            string codeLabel = (cmbISCode?.SelectedIndex == 0) ? "IS 1893:2016" : "IS 1893:2025";
            string zoneLabel = cmbSeismicZone?.SelectedItem?.ToString() ?? "";

            MessageBox.Show(
                $"✓ {tabCount} CAD Import tab(s) generated!\n\n" +
                notes.ToString() +
                $"\nWall thicknesses pre-filled from GPL table ({codeLabel} / {zoneLabel}).\n" +
                "Gravity beam widths pre-filled from zone (200 mm Zone II/III, 240 mm Zone IV/V) — editable.\n" +
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

            numCoreWallOverridePerFloor.Clear();
            numPeriphDeadWallOverridePerFloor.Clear();
            numPeriphPortalWallOverridePerFloor.Clear();
            numInternalWallOverridePerFloor.Clear();
            numNtaWallThicknessPerFloor.Clear();

            numColumnBPerFloor.Clear();
            numColumnDPerFloor.Clear();
        }

        // ====================================================================
        // UI HELPER METHODS
        // ====================================================================

        /// <summary>
        /// Returns default gravity beam width based on seismic zone.
        /// Zone II / Zone III → 200 mm
        /// Zone IV / Zone V / Zone VI → 240 mm
        /// </summary>
        private int GetAutoGravityWidthFromUI()
        {
            string zone = cmbSeismicZone?.SelectedItem?.ToString() ?? "";
            // Zone II and Zone III → 200 mm; everything else (IV, V, VI) → 240 mm
            if (zone.StartsWith("Zone II") || zone.StartsWith("Zone III"))
                return 200;
            return 240;
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

        // ====================================================================
        // LOAD SETS TAB
        // ====================================================================

        private void InitializeLoadSetsTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 15;

            AddLabel(tab,
                "📋 Beam load pattern names  |  Slab loads in kN/m²  |  Beam wall UDL magnitude (kN/m) — multiplied ×1000 → N/m in ETABS",
                20, y, 900, 24, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 11);
            y += 32;

            // ── BEAM WALL LOAD SETS ──────────────────────────────────────
            int beamRows = 10;
            var grpBeam = AddGroupBox(tab, "Beam Wall Load Sets  &  Wall Load Magnitude (kN/m)", 20, y, 910, 40 + beamRows * 32 + 36);
            AddLabel(grpBeam,
                "Enter ETABS load pattern name and wall UDL magnitude (kN/m). Dir=10 (Gravity). Value ×1000 → N/m internally.",
                15, 18, 880, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 9);

            // Column headers
            // AFTER
            const int bLblW = 230, bTxX = 245, bTxW = 210;  // ← increased from 175 to 210
            const int magNudX = 470, magNudW = 90, bRH = 32; // ← shift magNudX right by 35 to match
            int by = 40;

            AddLabel(grpBeam, "Beam Type", 20, by - 16, bLblW, 16, bold: true, fontSize: 8.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpBeam, "ETABS Load Pattern Name", bTxX, by - 16, bTxW, 16, bold: true, fontSize: 8.5f, color: System.Drawing.Color.DarkSlateGray);
            AddLabel(grpBeam, "Load (kN/m)  [×1000 = N/m]", magNudX, by - 16, 200, 16, bold: true, fontSize: 8.5f, color: System.Drawing.Color.DarkRed);

            // Helper: creates one TextBox + one NumericUpDown for a beam row, advances by
            void AddBeamLoadRow(string rowLabel, string defaultPattern, double defaultKNm,
                out System.Windows.Forms.TextBox txOut, out NumericUpDown nudOut)
            {
                AddLabel(grpBeam, rowLabel, 20, by, bLblW, 22, fontSize: 9f);

                //txOut = new System.Windows.Forms.TextBox
                //{
                //    Location = new System.Drawing.Point(bTxX, by - 2),
                //    Size = new System.Drawing.Size(bTxW, 26),
                //    Text = defaultPattern,
                //    Font = new System.Drawing.Font("Segoe UI", 9F),
                //    BackColor = System.Drawing.Color.FromArgb(255, 255, 220)
                //};
                //grpBeam.Controls.Add(txOut);
                // In your AddBeamLoadRow helper, change the TextBox creation:
                txOut = new System.Windows.Forms.TextBox
                {
                    Location = new System.Drawing.Point(bTxX, by - 2),
                    Size = new System.Drawing.Size(bTxW, 26),
                    Text = defaultPattern,
                    Font = new System.Drawing.Font("Segoe UI", 9F),
                    BackColor = System.Drawing.Color.FromArgb(255, 255, 220),
                    TextAlign = HorizontalAlignment.Left,  // explicit
                };
                grpBeam.Controls.Add(txOut);

                //// Add this AFTER adding to Controls — sets internal left margin of 4px
                //SendMessage(txOut.Handle, 0xD3 /*EM_SETMARGINS*/, 1 /*EC_LEFTMARGIN*/, 6);
                toolTip.SetToolTip(txOut, $"ETABS load pattern name for {rowLabel.TrimEnd(':')}.");

                nudOut = new NumericUpDown
                {
                    Location = new System.Drawing.Point(magNudX, by - 2),
                    Size = new System.Drawing.Size(magNudW, 26),
                    Minimum = 0,
                    Maximum = 999,
                    DecimalPlaces = 2,
                    Increment = 0.5M,
                    Value = (decimal)defaultKNm,
                    Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold),
                    BackColor = System.Drawing.Color.FromArgb(200, 230, 255)
                };
                grpBeam.Controls.Add(nudOut);
                toolTip.SetToolTip(nudOut,
                    $"Wall UDL for {rowLabel.TrimEnd(':')} in kN/m. " +
                    "Multiplied ×1000 → N/m before ETABS SetLoadDistributed. Dir=10 (Gravity). " +
                    "Example: enter 6 → 6000 N/m.");
                AddLabel(grpBeam, "kN/m", magNudX + magNudW + 4, by + 3, 45, 18,
                    color: System.Drawing.Color.DimGray, fontSize: 8.5f);

                by += bRH;
            }

            // Declare temp NUD variables, then assign to instance fields
            NumericUpDown _nud;
            System.Windows.Forms.TextBox _tx;

            AddBeamLoadRow("B-Internal Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedInternalGravityLoadSet = _tx;
            nudSharedInternalGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Cantilever Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedCantileverGravityLoadSet = _tx;
            nudSharedCantileverGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-EDeck Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedEDeckGravityLoadSet = _tx;
            nudSharedEDeckGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Podium Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedPodiumGravityLoadSet = _tx;
            nudSharedPodiumGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Ground Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedGroundGravityLoadSet = _tx;
            nudSharedGroundGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Basement Gravity:", "WALL LOAD", 6.0, out _tx, out _nud);
            txtSharedBasementGravityLoadSet = _tx;
            nudSharedBasementGravityLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Core Main:", "WALL LOAD", 8.0, out _tx, out _nud);
            txtSharedCoreMainLoadSet = _tx;
            nudSharedCoreMainLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Periph. Dead Main:", "WALL LOAD", 8.0, out _tx, out _nud);
            txtSharedPeripheralDeadMainLoadSet = _tx;
            nudSharedPeripheralDeadMainLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Periph. Portal Main:", "WALL LOAD", 8.0, out _tx, out _nud);
            txtSharedPeripheralPortalMainLoadSet = _tx;
            nudSharedPeripheralPortalMainLoadMag = new[] { _nud };

            AddBeamLoadRow("B-Internal Main:", "WALL LOAD", 8.0, out _tx, out _nud);
            txtSharedInternalMainLoadSet = _tx;
            nudSharedInternalMainLoadMag = new[] { _nud };

            // Info note
            AddLabel(grpBeam,
                "💡 Magnitude in kN/m.  ×1000 → N/m in ETABS API.  Dir=10 (Gravity, downward).  Uniform UDL: Dist1=0.0, Dist2=1.0.",
                15, by + 2, 880, 18, italic: true, color: System.Drawing.Color.DarkBlue, fontSize: 8.5f);

            y += 40 + beamRows * 32 + 44;

            // ── SLAB INDIVIDUAL LOADS ────────────────────────────────────
            var allSlabs = new (string lbl, string key)[]
            {
                ("S-AMENITIES",            "Amenities"),
                ("S-Cantilever BALCONY",   "Balcony"),
                ("S-Cantilever CHAJJA",    "Chajja"),
                ("S-Cantilever CHAJJA+ODU","ChajjaODU"),
                ("S-DRIVEWAY",             "Driveway"),
                ("S-FIRE TENDER",          "FireTender"),
                ("S-FIRE WATER TANK",      "FireWaterTank"),
                ("S-GARBAGE ROOM",         "GarbageRoom"),
                ("S-GARDEN/DINING AREA",   "GardenDining"),
                ("S-GYMNASIUM",            "Gymnasium"),
                ("S-INDOOR SPORTS",        "IndoorSports"),
                ("S-KITCHEN SUNK",         "KitchenSink"),
                ("S-LMR",                  "LMR"),
                ("S-LMR TOP",              "LMRTop"),
                ("S-LOBBY",                "Lobby"),
                ("S-METER ROOM",           "MeterRoom"),
                ("S-MULTIPURPOSE HALL",    "MultipurposeHall"),
                ("S-OHT",                  "OHT"),
                ("S-OHT TOP",              "OHTTop"),
                ("S-PARKING",              "Parking"),
                ("S-PARKING TOILET",       "ParkingToilet"),
                ("S-PUMP ROOM",            "PumpRoom"),
                ("S-REFUGE",               "Refuge"),
                ("S-RESIDENTIAL",          "Residential"),
                ("S-RETAIL",               "Retail"),
                ("S-RETAIL MAZZANINE",     "RetailMazzanine"),
                ("S-RETAIL TOILET",        "RetailToilet"),
                ("S-SERVICE SLAB",         "ServiceSlab"),
                ("S-SOCIETY ROOM",         "SocietyRoom"),
                ("S-STACK PARKING",        "StackParking"),
                ("S-STAIRCASE",            "Staircase"),
                ("S-TERRACE",              "Terrace"),
                ("S-TERRACE FIRE TANK",    "TerraceFire"),
                ("S-TERRACE PUMP ROOM",    "TerracePumpRoom"),
                ("S-TOILET",               "Toilet"),
                ("S-UGT",                  "UGT"),
                ("S-LANDSCAPE",            "Landscape"),
                ("S-SWIMMING",             "Swimming"),
                ("S-DG",                   "DG"),
                ("S-STP",                  "STP"),
                ("S-UTILITY",              "Utility"),
            };

            const int lblW = 178;
            const int nudW = 72;
            const int nudGp = 5;
            const int rowH = 32;
            const int subtitleY = 20;
            const int hdrY = 40;
            const int dataY = 62;

            int totalCols = 9;
            int totalW = lblW + (nudW + nudGp) * totalCols + 30;
            int totalH = dataY + allSlabs.Length * rowH + 36;

            var grpSlab = AddGroupBox(tab,
                "Slab Individual Loads — all values in kN/m²  (edit per slab layer, applied per load pattern)",
                20, y, Math.Max(totalW + 20, 960), totalH);

            AddLabel(grpSlab,
                "Values are assigned directly to ETABS load patterns. Zero = pattern skipped.",
                15, subtitleY, 880, 16, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 9f);

            string[] hdrs = { "FF", "FILLING", "ASDL", "LL", "LL>3", "FIRE", "TREE", "MACH", "WATER" };
            string[] hdrTooltips = {
                "Floor Finish (kN/m²)",
                "Filling (kN/m²)",
                "ASDL — Additional Superimposed Dead Load (kN/m²)",
                "Live Load ≤ 3m height (kN/m²)",
                "Live Load > 3m height (kN/m²)",
                "Fire Tender Load (kN/m²)",
                "Tree Load (kN/m²)",
                "Machine Room Load (kN/m²)",
                "Water Tank Load (kN/m²)"
            };

            for (int ci = 0; ci < hdrs.Length; ci++)
            {
                int hx = lblW + 8 + ci * (nudW + nudGp);
                var hdrLbl = new Label
                {
                    Text = hdrs[ci],
                    Location = new System.Drawing.Point(hx, hdrY),
                    Size = new System.Drawing.Size(nudW, 18),
                    Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.DarkSlateBlue,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    AutoSize = false
                };
                grpSlab.Controls.Add(hdrLbl);
                toolTip.SetToolTip(hdrLbl, hdrTooltips[ci]);
            }

            var colBg = new System.Drawing.Color[]
            {
                System.Drawing.Color.FromArgb(255, 245, 200),
                System.Drawing.Color.FromArgb(255, 235, 180),
                System.Drawing.Color.FromArgb(220, 235, 255),
                System.Drawing.Color.FromArgb(200, 255, 200),
                System.Drawing.Color.FromArgb(180, 245, 180),
                System.Drawing.Color.FromArgb(255, 210, 210),
                System.Drawing.Color.FromArgb(210, 255, 235),
                System.Drawing.Color.FromArgb(235, 215, 255),
                System.Drawing.Color.FromArgb(195, 235, 255),
            };

            for (int ri = 0; ri < allSlabs.Length; ri++)
            {
                var (sLbl, sKey) = allSlabs[ri];
                int ry = dataY + ri * rowH;

                AddLabel(grpSlab, sLbl + ":", 8, ry + 4, lblW, 20, fontSize: 9f);

                FloorTypeConfig.DefaultSlabIndividualLoads.TryGetValue(sKey,
                    out SlabLoads def);
                def = def ?? new SlabLoads(0, 0, 1, 2);

                double[] defaults = {
                    def.FF, def.Filling, def.ASDL,
                    def.LL, def.LL3, def.FireTender,
                    def.TreeLoad, def.MachineRoom, def.WaterTank
                };

                var nuds = new NumericUpDown[9];
                for (int ci = 0; ci < 9; ci++)
                {
                    int nx = lblW + 8 + ci * (nudW + nudGp);
                    var nud = new NumericUpDown
                    {
                        Location = new System.Drawing.Point(nx, ry + 2),
                        Size = new System.Drawing.Size(nudW, rowH - 6),
                        Minimum = 0,
                        Maximum = 200,
                        DecimalPlaces = 2,
                        Increment = 0.05M,
                        Value = (decimal)defaults[ci],
                        BackColor = colBg[ci],
                        Font = new System.Drawing.Font("Segoe UI", 9F),
                        BorderStyle = BorderStyle.FixedSingle,
                    };
                    grpSlab.Controls.Add(nud);
                    nuds[ci] = nud;

                    string[] colNames = {
                        "Floor Finish (kN/m²)", "Filling (kN/m²)", "ASDL (kN/m²)",
                        "Live Load ≤3m (kN/m²)", "Live Load >3m (kN/m²)",
                        "Fire Tender (kN/m²)", "Tree Load (kN/m²)",
                        "Machine Room (kN/m²)", "Water Tank (kN/m²)"
                    };
                    toolTip.SetToolTip(nud, $"{sLbl} — {colNames[ci]}");
                }
                sharedSlabIndividualLoadControls[sKey] = nuds;
            }
        }

        // ====================================================================
        // SLAB THICKNESS RULES TAB
        // WHITE slabs  → area-based rules  (m²  → mm thickness)
        // CYAN slabs   → cantilever span rules (m → mm thickness)
        // YELLOW slabs → fixed user thickness (already on per-floor CAD tabs)
        // ====================================================================

        private void InitializeSlabThicknessRulesTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 15;

            AddLabel(tab,
                "📐 Slab Thickness Rules — edit thresholds, changes apply to all floors",
                20, y, 900, 24, bold: true, color: System.Drawing.Color.DarkBlue, fontSize: 11);
            y += 30;

            AddLabel(tab,
                "WHITE layers use area-based rules. CYAN (cantilever) layers use span-based rules. " +
                "YELLOW layers use fixed thicknesses defined in the CAD Import tab per floor.",
                20, y, 900, 18, italic: true, color: System.Drawing.Color.DarkGreen, fontSize: 9f);
            y += 28;

            // ── AREA RULES (WHITE slabs) ──────────────────────────────────────
            const int colT = 30;         // thickness column x
            const int colA = 180;        // area/span column x
            const int nudW = 120;
            const int nudH = 28;
            const int rowH = 36;
            const int btnW = 110;

            var defaultAreaRules = FloorTypeConfig.DefaultSlabAreaRules;
            int areaTableH = 60 + defaultAreaRules.Count * rowH + 50;
            var grpArea = AddGroupBox(tab, "⬜ WHITE Slab — Area Rules  (slab area m²  →  thickness mm)", 20, y, 500, areaTableH);

            AddLabel(grpArea, "Thickness (mm)", colT, 22, 145, 18,
                bold: true, color: System.Drawing.Color.DarkSlateBlue, fontSize: 9f);
            AddLabel(grpArea, "Max Area (m²)", colA, 22, 145, 18,
                bold: true, color: System.Drawing.Color.DarkSlateBlue, fontSize: 9f);
            AddLabel(grpArea,
                "If slab area ≤ Max Area → use this Thickness. Rows evaluated top-to-bottom.",
                colT, 40, 450, 16, italic: true, color: System.Drawing.Color.Gray, fontSize: 8.5f);

            int ary = 60;
            slabAreaRuleControls.Clear();

            void AddAreaRow(int thickDef, double areaDef)
            {
                var nudT = new NumericUpDown
                {
                    Location = new System.Drawing.Point(colT, ary),
                    Size = new System.Drawing.Size(nudW, nudH),
                    Minimum = 50,
                    Maximum = 500,
                    Increment = 5,
                    Value = thickDef,
                    Font = new System.Drawing.Font("Segoe UI", 9.5F),
                    BackColor = System.Drawing.Color.FromArgb(255, 255, 200),
                    BorderStyle = BorderStyle.FixedSingle,
                    DecimalPlaces = 0
                };
                var nudA = new NumericUpDown
                {
                    Location = new System.Drawing.Point(colA, ary),
                    Size = new System.Drawing.Size(nudW, nudH),
                    Minimum = 0,
                    Maximum = 5000,
                    Increment = 1,
                    DecimalPlaces = 1,
                    Value = (decimal)areaDef,
                    Font = new System.Drawing.Font("Segoe UI", 9.5F),
                    BackColor = System.Drawing.Color.FromArgb(200, 240, 255),
                    BorderStyle = BorderStyle.FixedSingle
                };
                grpArea.Controls.Add(nudT);
                grpArea.Controls.Add(nudA);
                toolTip.SetToolTip(nudT, "Slab thickness in mm assigned when area ≤ Max Area.");
                toolTip.SetToolTip(nudA, "Upper area limit (m²). Slabs with area ≤ this value get the thickness on the left.");
                slabAreaRuleControls.Add(new[] { nudT, nudA });
                ary += rowH;
            }

            foreach (var r in defaultAreaRules)
                AddAreaRow(r.thickness, r.maxArea);

            // Add / Remove row buttons
            var btnAddArea = new Button
            {
                Text = "+ Add Row",
                Location = new System.Drawing.Point(colT, ary + 4),
                Size = new System.Drawing.Size(btnW, 28),
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                BackColor = System.Drawing.Color.FromArgb(200, 230, 200)
            };
            var btnRemArea = new Button
            {
                Text = "− Remove Last",
                Location = new System.Drawing.Point(colT + btnW + 8, ary + 4),
                Size = new System.Drawing.Size(btnW + 10, 28),
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                BackColor = System.Drawing.Color.FromArgb(255, 210, 210)
            };

            btnAddArea.Click += (s, e) =>
            {
                int newY = 60 + slabAreaRuleControls.Count * rowH;
                // Shift buttons down
                btnAddArea.Top += rowH;
                btnRemArea.Top += rowH;
                grpArea.Height += rowH;
                // Shift cantilever group and everything below
                // (AutoScroll handles overflow; just grow the group)
                ary = newY;
                AddAreaRow(150, 22);
            };
            btnRemArea.Click += (s, e) =>
            {
                if (slabAreaRuleControls.Count <= 1) return;
                var last = slabAreaRuleControls[slabAreaRuleControls.Count - 1];
                foreach (var n in last) grpArea.Controls.Remove(n);
                slabAreaRuleControls.RemoveAt(slabAreaRuleControls.Count - 1);
                btnAddArea.Top -= rowH;
                btnRemArea.Top -= rowH;
                grpArea.Height -= rowH;
                ary -= rowH;
            };

            grpArea.Controls.Add(btnAddArea);
            grpArea.Controls.Add(btnRemArea);

            y += areaTableH + 20;

            // ── CANTILEVER RULES (CYAN slabs) ─────────────────────────────────
            var defaultCantRules = FloorTypeConfig.DefaultSlabCantileverRules;
            int cantTableH = 60 + defaultCantRules.Count * rowH + 50;
            var grpCant = AddGroupBox(tab,
                "🔵 CYAN Slab (Cantilever) — Span Rules  (cantilever span m  →  thickness mm)",
                20, y, 500, cantTableH);

            AddLabel(grpCant, "Thickness (mm)", colT, 22, 145, 18,
                bold: true, color: System.Drawing.Color.DarkSlateBlue, fontSize: 9f);
            AddLabel(grpCant, "Max Span (m)", colA, 22, 145, 18,
                bold: true, color: System.Drawing.Color.DarkSlateBlue, fontSize: 9f);
            AddLabel(grpCant,
                "If shortest edge ≤ Max Span → use this Thickness. Rows evaluated top-to-bottom.",
                colT, 40, 450, 16, italic: true, color: System.Drawing.Color.Gray, fontSize: 8.5f);

            int cry = 60;
            slabCantileverRuleControls.Clear();

            void AddCantRow(int thickDef, double spanDef)
            {
                var nudT = new NumericUpDown
                {
                    Location = new System.Drawing.Point(colT, cry),
                    Size = new System.Drawing.Size(nudW, nudH),
                    Minimum = 50,
                    Maximum = 500,
                    Increment = 5,
                    Value = thickDef,
                    Font = new System.Drawing.Font("Segoe UI", 9.5F),
                    BackColor = System.Drawing.Color.FromArgb(255, 255, 200),
                    BorderStyle = BorderStyle.FixedSingle,
                    DecimalPlaces = 0
                };
                var nudS = new NumericUpDown
                {
                    Location = new System.Drawing.Point(colA, cry),
                    Size = new System.Drawing.Size(nudW, nudH),
                    Minimum = 0,
                    Maximum = 50,
                    Increment = 0.1M,
                    DecimalPlaces = 2,
                    Value = (decimal)spanDef,
                    Font = new System.Drawing.Font("Segoe UI", 9.5F),
                    BackColor = System.Drawing.Color.FromArgb(200, 230, 255),
                    BorderStyle = BorderStyle.FixedSingle
                };
                grpCant.Controls.Add(nudT);
                grpCant.Controls.Add(nudS);
                toolTip.SetToolTip(nudT, "Slab thickness in mm for cantilever slabs with span ≤ Max Span.");
                toolTip.SetToolTip(nudS, "Maximum cantilever span (m). Slabs with span ≤ this value get the thickness on the left.");
                slabCantileverRuleControls.Add(new[] { nudT, nudS });
                cry += rowH;
            }

            foreach (var r in defaultCantRules)
                AddCantRow(r.thickness, r.maxSpan);

            var btnAddCant = new Button
            {
                Text = "+ Add Row",
                Location = new System.Drawing.Point(colT, cry + 4),
                Size = new System.Drawing.Size(btnW, 28),
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                BackColor = System.Drawing.Color.FromArgb(200, 230, 200)
            };
            var btnRemCant = new Button
            {
                Text = "− Remove Last",
                Location = new System.Drawing.Point(colT + btnW + 8, cry + 4),
                Size = new System.Drawing.Size(btnW + 10, 28),
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                BackColor = System.Drawing.Color.FromArgb(255, 210, 210)
            };

            btnAddCant.Click += (s, e) =>
            {
                int newY = 60 + slabCantileverRuleControls.Count * rowH;
                btnAddCant.Top += rowH;
                btnRemCant.Top += rowH;
                grpCant.Height += rowH;
                cry = newY;
                AddCantRow(200, 2.0);
            };
            btnRemCant.Click += (s, e) =>
            {
                if (slabCantileverRuleControls.Count <= 1) return;
                var last = slabCantileverRuleControls[slabCantileverRuleControls.Count - 1];
                foreach (var n in last) grpCant.Controls.Remove(n);
                slabCantileverRuleControls.RemoveAt(slabCantileverRuleControls.Count - 1);
                btnAddCant.Top -= rowH;
                btnRemCant.Top -= rowH;
                grpCant.Height -= rowH;
                cry -= rowH;
            };

            grpCant.Controls.Add(btnAddCant);
            grpCant.Controls.Add(btnRemCant);

            y += cantTableH + 20;

            // ── YELLOW reminder ───────────────────────────────────────────────
            var grpYellow = AddGroupBox(tab,
                "🟡 YELLOW Slab — Fixed User Thickness  (not affected by rules above)",
                20, y, 500, 60);
            AddLabel(grpYellow,
                "Lobby, Staircase, Fire Tender, OHT, Terrace Fire Tank, UGT, Landscape, Swimming, DG, STP\n" +
                "Thickness for these is set per-floor in the CAD Import tab.",
                colT, 20, 460, 34, italic: false, color: System.Drawing.Color.DimGray, fontSize: 9f);
        }
    }
}



