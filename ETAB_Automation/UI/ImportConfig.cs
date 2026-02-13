// ============================================================================
// FILE: UI/ImportConfigForm.cs (COMPLETE WITH ALL FIXES AND COMMENTS)
// ============================================================================
// PURPOSE: Windows Forms UI for configuring multi-floor building CAD imports
//          into ETABS structural analysis software
// AUTHOR: ETAB Automation Team
// VERSION: 2.0 (Corrected)
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
    /// Main configuration form for importing CAD files and configuring building parameters
    /// Supports multi-floor types: Basement, Podium, E-Deck (Ground), Typical, Terrace
    /// </summary>
    public partial class ImportConfigForm : Form
    {
        // ====================================================================
        // PUBLIC PROPERTIES - Accessible after form closes with DialogResult.OK
        // ====================================================================

        /// <summary>
        /// Collection of floor configurations for each floor type in the building
        /// </summary>
        public List<FloorTypeConfig> FloorConfigs { get; private set; }

        /// <summary>
        /// Selected seismic zone (Zone II, III, IV, or V)
        /// Affects gravity beam widths: Zone II/III = 200mm, Zone IV/V = 240mm
        /// </summary>
        public string SeismicZone { get; private set; }

        /// <summary>
        /// Beam depths in millimeters for different beam types
        /// Keys: InternalGravity, CantileverGravity, CoreMain, PeripheralDeadMain, 
        ///       PeripheralPortalMain, InternalMain
        /// </summary>
        public Dictionary<string, int> BeamDepths { get; private set; }

        /// <summary>
        /// Slab thicknesses in millimeters for special slab types
        /// Keys: Lobby, Stair
        /// (Regular slabs use area/span-based automatic rules)
        /// </summary>
        public Dictionary<string, int> SlabThicknesses { get; private set; }

        /// <summary>
        /// Concrete wall grades from building bottom to top (e.g., M50, M45, M40, M30)
        /// </summary>
        public List<string> WallGrades { get; private set; }

        /// <summary>
        /// Number of floors for each grade segment (must sum to total building floors)
        /// </summary>
        public List<int> FloorsPerGrade { get; private set; }

        // ====================================================================
        // PRIVATE UI CONTROLS
        // ====================================================================

        // Main tab control containing all configuration tabs
        private TabControl tabControl;

        // Action buttons
        private Button btnImport;
        private Button btnCancel;

        // Building configuration controls
        private CheckBox chkBasement;
        private CheckBox chkPodium;
        private CheckBox chkTerrace;
        private NumericUpDown numBasementLevels;
        private NumericUpDown numPodiumLevels;
        private NumericUpDown numTypicalLevels;
        private NumericUpDown numBasementHeight;
        private NumericUpDown numPodiumHeight;
        private NumericUpDown numEDeckHeight;
        private NumericUpDown numTypicalHeight;
        private NumericUpDown numTerraceheight;
        private ComboBox cmbSeismicZone;

        // Concrete grade schedule controls
        private DataGridView dgvGradeSchedule;
        private NumericUpDown numTotalFloors;
        private Button btnAddGradeRow;
        private Button btnRemoveGradeRow;
        private Label lblGradeTotal;

        // Beam depth controls
        private NumericUpDown numInternalGravityDepth;
        private NumericUpDown numCantileverGravityDepth;
        private NumericUpDown numCoreMainDepth;
        private NumericUpDown numPeripheralDeadMainDepth;
        private NumericUpDown numPeripheralPortalMainDepth;
        private NumericUpDown numInternalMainDepth;
        private Label lblGravityWidthInfo;
        private Label lblMainBeamWidthInfo;

        // Slab thickness controls
        private NumericUpDown numLobbySlabThickness;
        private NumericUpDown numStairSlabThickness;

        // CAD Import controls (dynamic per floor type)
        // These dictionaries store controls for each floor type (Basement, Podium, etc.)
        private Dictionary<string, TextBox> cadPathTextBoxes;
        private Dictionary<string, ListBox> availableLayerListBoxes;
        private Dictionary<string, ListBox> mappedLayerListBoxes;
        private Dictionary<string, ComboBox> elementTypeComboBoxes;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Initialize the ImportConfigForm with default values and empty collections
        /// </summary>
        public ImportConfigForm()
        {
            InitializeComponent();

            // Initialize data collections
            FloorConfigs = new List<FloorTypeConfig>();
            BeamDepths = new Dictionary<string, int>();
            SlabThicknesses = new Dictionary<string, int>();
            WallGrades = new List<string>();
            FloorsPerGrade = new List<int>();

            // Initialize control dictionaries for dynamic CAD import tabs
            cadPathTextBoxes = new Dictionary<string, TextBox>();
            availableLayerListBoxes = new Dictionary<string, ListBox>();
            mappedLayerListBoxes = new Dictionary<string, ListBox>();
            elementTypeComboBoxes = new Dictionary<string, ComboBox>();

            // Build the UI
            InitializeControls();
        }



        // ====================================================================
        // MAIN UI INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize all controls and tabs for the form
        /// Creates: Building Config, Beam Depths, Slab Thicknesses, Concrete Grades tabs
        /// </summary>
        private void InitializeControls()
        {
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

            // Tab 1: Building Configuration (floor types, heights, seismic zone)
            TabPage tabBuilding = new TabPage("Building Configuration");
            tabControl.TabPages.Add(tabBuilding);
            InitializeBuildingConfigTab(tabBuilding);

            // Tab 2: Beam Depth Configuration (gravity and main beams)
            TabPage tabBeamDepth = new TabPage("Beam Depths");
            tabControl.TabPages.Add(tabBeamDepth);
            InitializeBeamDepthTab(tabBeamDepth);

            // Tab 3: Slab Thickness Configuration (lobby, stair, and automatic rules)
            TabPage tabSlabConfig = new TabPage("Slab Thicknesses");
            tabControl.TabPages.Add(tabSlabConfig);
            InitializeSlabConfigTab(tabSlabConfig);

            // Tab 4: Concrete Grade Schedule (wall and beam/slab grades by floor)
            TabPage tabGradeSchedule = new TabPage("Concrete Grades");
            tabControl.TabPages.Add(tabGradeSchedule);
            InitializeGradeScheduleTab(tabGradeSchedule);

            // Action Buttons at bottom of form
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
        // TAB 1: BUILDING CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Initialize the Building Configuration tab
        /// Allows user to define: Basement, Podium, E-Deck, Typical, Terrace floors
        /// </summary>
        /// <param name="tab">The tab page to populate</param>
        private void InitializeBuildingConfigTab(TabPage tab)
        {
            tab.AutoScroll = true; // Enable scrolling if content exceeds visible area
            int yPos = 20;

            // Instructions header
            Label lblInstructions = new Label
            {
                Text = "📋 Define building structure: Basement → Podium → E-Deck (Ground) → Typical Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInstructions);
            yPos += 35;

            // ----------------------------------------------------------------
            // BASEMENT SECTION (Optional)
            // ----------------------------------------------------------------
            GroupBox grpBasement = new GroupBox
            {
                Text = "Basement Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpBasement);

            chkBasement = new CheckBox
            {
                Text = "Include Basement Floors",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(200, 20)
            };
            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;
            grpBasement.Controls.Add(chkBasement);

            Label lblBasementCount = new Label
            {
                Text = "Number of Basements:",
                Location = new System.Drawing.Point(40, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpBasement.Controls.Add(lblBasementCount);

            numBasementLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 50),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 10,
                Value = 2,
                Enabled = false // Disabled until checkbox is checked
            };
            grpBasement.Controls.Add(numBasementLevels);

            Label lblBasementHeight = new Label
            {
                Text = "Basement Height (m):",
                Location = new System.Drawing.Point(320, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpBasement.Controls.Add(lblBasementHeight);

            numBasementHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(480, 50),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 2.5M,
                Maximum = 6.0M,
                Value = 3.5M,
                Increment = 0.1M,
                Enabled = false
            };
            grpBasement.Controls.Add(numBasementHeight);

            yPos += 100;

            // ----------------------------------------------------------------
            // PODIUM SECTION (Optional)
            // ----------------------------------------------------------------
            GroupBox grpPodium = new GroupBox
            {
                Text = "Podium Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpPodium);

            chkPodium = new CheckBox
            {
                Text = "Include Podium Floors",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(200, 20)
            };
            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
            grpPodium.Controls.Add(chkPodium);

            Label lblPodiumCount = new Label
            {
                Text = "Number of Podiums:",
                Location = new System.Drawing.Point(40, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpPodium.Controls.Add(lblPodiumCount);

            numPodiumLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 50),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 5,
                Value = 1,
                Enabled = false
            };
            grpPodium.Controls.Add(numPodiumLevels);

            Label lblPodiumHeight = new Label
            {
                Text = "Podium Height (m):",
                Location = new System.Drawing.Point(320, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpPodium.Controls.Add(lblPodiumHeight);

            numPodiumHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(480, 50),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 3.0M,
                Maximum = 8.0M,
                Value = 4.5M,
                Increment = 0.1M,
                Enabled = false
            };
            grpPodium.Controls.Add(numPodiumHeight);

            yPos += 100;

            // ----------------------------------------------------------------
            // E-DECK (GROUND FLOOR) SECTION (Mandatory)
            // ----------------------------------------------------------------
            GroupBox grpEDeck = new GroupBox
            {
                Text = "E-Deck (Ground Floor)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 70)
            };
            tab.Controls.Add(grpEDeck);

            Label lblEDeckHeight = new Label
            {
                Text = "E-Deck Height (m):",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(150, 20)
            };
            grpEDeck.Controls.Add(lblEDeckHeight);

            numEDeckHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 30),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 3.0M,
                Maximum = 10.0M,
                Value = 4.5M,
                Increment = 0.1M
            };
            grpEDeck.Controls.Add(numEDeckHeight);

            Label lblEDeckNote = new Label
            {
                Text = "(Ground floor is mandatory)",
                Location = new System.Drawing.Point(290, 32),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpEDeck.Controls.Add(lblEDeckNote);

            yPos += 80;

            // ----------------------------------------------------------------
            // TYPICAL FLOORS SECTION (Mandatory)
            // ----------------------------------------------------------------
            GroupBox grpTypical = new GroupBox
            {
                Text = "Typical Floors (Above E-Deck)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpTypical);

            Label lblTypicalCount = new Label
            {
                Text = "Number of Typical Floors:",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(160, 20)
            };
            grpTypical.Controls.Add(lblTypicalCount);

            numTypicalLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(210, 30),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 100,
                Value = 10
            };
            grpTypical.Controls.Add(numTypicalLevels);

            Label lblTypicalHeight = new Label
            {
                Text = "Typical Floor Height (m):",
                Location = new System.Drawing.Point(320, 32),
                Size = new System.Drawing.Size(160, 20)
            };
            grpTypical.Controls.Add(lblTypicalHeight);

            numTypicalHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(490, 30),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 2.8M,
                Maximum = 5.0M,
                Value = 3.0M,
                Increment = 0.1M
            };
            grpTypical.Controls.Add(numTypicalHeight);

            yPos += 100;

            // ----------------------------------------------------------------
            // TERRACE SECTION (Optional)
            // ----------------------------------------------------------------
            GroupBox grpTerrace = new GroupBox
            {
                Text = "Terrace Floor",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpTerrace);

            chkTerrace = new CheckBox
            {
                Text = "Include Terrace Floor",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(200, 20)
            };
            chkTerrace.CheckedChanged += ChkTerrace_CheckedChanged;
            grpTerrace.Controls.Add(chkTerrace);

            Label lblTerraceHeight = new Label
            {
                Text = "Terrace Height (m):",
                Location = new System.Drawing.Point(40, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpTerrace.Controls.Add(lblTerraceHeight);

            numTerraceheight = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 50),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 2.8M,
                Maximum = 5.0M,
                Value = 3.0M,
                Increment = 0.1M,
                Enabled = false
            };
            grpTerrace.Controls.Add(numTerraceheight);

            Label lblTerraceNote = new Label
            {
                Text = "(Terrace will be placed at top of building)",
                Location = new System.Drawing.Point(290, 52),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpTerrace.Controls.Add(lblTerraceNote);

            yPos += 100;

            // ----------------------------------------------------------------
            // SEISMIC ZONE SELECTION
            // ----------------------------------------------------------------
            GroupBox grpSeismic = new GroupBox
            {
                Text = "Seismic Parameters",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 70)
            };
            tab.Controls.Add(grpSeismic);

            Label lblSeismic = new Label
            {
                Text = "Seismic Zone:",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(120, 20)
            };
            grpSeismic.Controls.Add(lblSeismic);

            cmbSeismicZone = new ComboBox
            {
                Location = new System.Drawing.Point(170, 30),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSeismicZone.Items.AddRange(new object[] { "Zone II", "Zone III", "Zone IV", "Zone V" });
            cmbSeismicZone.SelectedIndex = 2; // Default: Zone IV
            cmbSeismicZone.SelectedIndexChanged += CmbSeismicZone_SelectedIndexChanged;
            grpSeismic.Controls.Add(cmbSeismicZone);

            yPos += 80;

            // ----------------------------------------------------------------
            // GENERATE CAD IMPORT TABS BUTTON
            // ----------------------------------------------------------------
            Button btnGenerateTabs = new Button
            {
                Text = "Generate CAD Import Tabs →",
                Location = new System.Drawing.Point(320, yPos),
                Size = new System.Drawing.Size(200, 40),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGreen
            };
            btnGenerateTabs.Click += BtnGenerateTabs_Click;
            tab.Controls.Add(btnGenerateTabs);
        }

        // ====================================================================
        // TAB 2: BEAM DEPTHS CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Initialize the Beam Depths tab
        /// Configures depths for gravity beams and main (seismic) beams
        /// </summary>
        /// <param name="tab">The tab page to populate</param>
        private void InitializeBeamDepthTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int yPos = 20;

            // Instructions
            Label lblInstructions = new Label
            {
                Text = "📐 Configure beam depths for different beam types (depths in millimeters)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInstructions);
            yPos += 40;

            // ----------------------------------------------------------------
            // GRAVITY BEAMS SECTION (Non-Seismic)
            // ----------------------------------------------------------------
            GroupBox grpGravity = new GroupBox
            {
                Text = "Gravity Beams (Non-Seismic)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 180)
            };
            tab.Controls.Add(grpGravity);

            // Width info - auto-set based on seismic zone
            lblGravityWidthInfo = new Label
            {
                Text = "Width: 200mm (Zone II/III) | 240mm (Zone IV/V) - Auto-set based on seismic zone",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(750, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            grpGravity.Controls.Add(lblGravityWidthInfo);

            // Internal Gravity Beams
            Label lblInternalGravity = new Label
            {
                Text = "Internal Gravity Beam Depth (mm):",
                Location = new System.Drawing.Point(40, 60),
                Size = new System.Drawing.Size(250, 20)
            };
            grpGravity.Controls.Add(lblInternalGravity);

            numInternalGravityDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, 58),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 200,
                Maximum = 1000,
                Value = 450,
                Increment = 25
            };
            grpGravity.Controls.Add(numInternalGravityDepth);

            Label lblInternalGravityNote = new Label
            {
                Text = "Layer: B-Internal gravity beams",
                Location = new System.Drawing.Point(420, 60),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpGravity.Controls.Add(lblInternalGravityNote);

            // Cantilever Gravity Beams
            Label lblCantileverGravity = new Label
            {
                Text = "Cantilever Gravity Beam Depth (mm):",
                Location = new System.Drawing.Point(40, 95),
                Size = new System.Drawing.Size(250, 20)
            };
            grpGravity.Controls.Add(lblCantileverGravity);

            numCantileverGravityDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, 93),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 200,
                Maximum = 1000,
                Value = 500,
                Increment = 25
            };
            grpGravity.Controls.Add(numCantileverGravityDepth);

            Label lblCantileverGravityNote = new Label
            {
                Text = "Layer: B-Cantilever Gravity Beams",
                Location = new System.Drawing.Point(420, 95),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpGravity.Controls.Add(lblCantileverGravityNote);

            // Example sections
            Label lblGravityExample = new Label
            {
                Text = "Example sections: B20x450 (Zone II/III), B24x450 (Zone IV/V)",
                Location = new System.Drawing.Point(40, 130),
                Size = new System.Drawing.Size(500, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            grpGravity.Controls.Add(lblGravityExample);

            yPos += 190;

            // ----------------------------------------------------------------
            // MAIN BEAMS SECTION (Seismic - Lateral Load Resisting)
            // ----------------------------------------------------------------
            GroupBox grpMain = new GroupBox
            {
                Text = "Main Beams (Seismic - Lateral Load Resisting)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 270)
            };
            tab.Controls.Add(grpMain);

            // Width info - matches wall thickness
            lblMainBeamWidthInfo = new Label
            {
                Text = "Width: Matches adjacent wall thickness (auto-determined from wall design)",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(750, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            grpMain.Controls.Add(lblMainBeamWidthInfo);

            int mainYPos = 60;

            // Core Main Beam
            Label lblCoreMain = new Label
            {
                Text = "Core Main Beam Depth (mm):",
                Location = new System.Drawing.Point(40, mainYPos),
                Size = new System.Drawing.Size(250, 20)
            };
            grpMain.Controls.Add(lblCoreMain);

            numCoreMainDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, mainYPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 300,
                Maximum = 1500,
                Value = 600,
                Increment = 25
            };
            grpMain.Controls.Add(numCoreMainDepth);

            Label lblCoreMainNote = new Label
            {
                Text = "Layer: B-Core Main Beam",
                Location = new System.Drawing.Point(420, mainYPos),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpMain.Controls.Add(lblCoreMainNote);
            mainYPos += 40;

            // Peripheral Dead Main Beams
            Label lblPeripheralDead = new Label
            {
                Text = "Peripheral Dead Main Beam Depth (mm):",
                Location = new System.Drawing.Point(40, mainYPos),
                Size = new System.Drawing.Size(250, 20)
            };
            grpMain.Controls.Add(lblPeripheralDead);

            numPeripheralDeadMainDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, mainYPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 300,
                Maximum = 1500,
                Value = 600,
                Increment = 25
            };
            grpMain.Controls.Add(numPeripheralDeadMainDepth);

            Label lblPeripheralDeadNote = new Label
            {
                Text = "Layer: B-Peripheral dead Main Beams",
                Location = new System.Drawing.Point(420, mainYPos),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpMain.Controls.Add(lblPeripheralDeadNote);
            mainYPos += 40;

            // Peripheral Portal Main Beams
            Label lblPeripheralPortal = new Label
            {
                Text = "Peripheral Portal Main Beam Depth (mm):",
                Location = new System.Drawing.Point(40, mainYPos),
                Size = new System.Drawing.Size(250, 20)
            };
            grpMain.Controls.Add(lblPeripheralPortal);

            numPeripheralPortalMainDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, mainYPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 300,
                Maximum = 1500,
                Value = 650,
                Increment = 25
            };
            grpMain.Controls.Add(numPeripheralPortalMainDepth);

            Label lblPeripheralPortalNote = new Label
            {
                Text = "Layer: B-Peripheral Portal Main Beams",
                Location = new System.Drawing.Point(420, mainYPos),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpMain.Controls.Add(lblPeripheralPortalNote);
            mainYPos += 40;

            // Internal Main Beams
            Label lblInternalMain = new Label
            {
                Text = "Internal Main Beam Depth (mm):",
                Location = new System.Drawing.Point(40, mainYPos),
                Size = new System.Drawing.Size(250, 20)
            };
            grpMain.Controls.Add(lblInternalMain);

            numInternalMainDepth = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, mainYPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 300,
                Maximum = 1500,
                Value = 550,
                Increment = 25
            };
            grpMain.Controls.Add(numInternalMainDepth);

            Label lblInternalMainNote = new Label
            {
                Text = "Layer: B-Internal Main beams",
                Location = new System.Drawing.Point(420, mainYPos),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpMain.Controls.Add(lblInternalMainNote);
            mainYPos += 50;

            // Example sections
            Label lblMainExample = new Label
            {
                Text = "Example sections: Core wall 300mm → B30x600, Peripheral wall 240mm → B24x600",
                Location = new System.Drawing.Point(40, mainYPos),
                Size = new System.Drawing.Size(700, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            grpMain.Controls.Add(lblMainExample);

            yPos += 280;

            // Design notes
            Label lblDesignNotes = new Label
            {
                Text = "📝 Design Notes:\n" +
                       "• Gravity beam widths are auto-set based on seismic zone\n" +
                       "• Main beam widths match adjacent wall thickness\n" +
                       "• Beam sections will be matched from ETABS template (e.g., B30x600M40)\n" +
                       "• If exact section not found, closest available section will be used",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 80),
                Font = new System.Drawing.Font("Segoe UI", 8F),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblDesignNotes);
        }

        // ====================================================================
        // TAB 3: SLAB THICKNESSES CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Initialize the Slab Thicknesses tab
        /// Configures special slab types and displays automatic rules
        /// </summary>
        /// <param name="tab">The tab page to populate</param>
        private void InitializeSlabConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int yPos = 20;

            Label lblInstructions = new Label
            {
                Text = "📐 Configure slab thicknesses for special cases (in millimeters)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInstructions);
            yPos += 40;

            Label lblNote = new Label
            {
                Text = "Note: Regular slabs are auto-determined based on area (14-70 m²) as per design rules",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(750, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            tab.Controls.Add(lblNote);
            yPos += 35;

            // Lobby slab thickness
            Label lblLobby = new Label
            {
                Text = "Lobby Slab Thickness (mm):",
                Location = new System.Drawing.Point(40, yPos),
                Size = new System.Drawing.Size(250, 20)
            };
            tab.Controls.Add(lblLobby);

            numLobbySlabThickness = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, yPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 100,
                Maximum = 300,
                Value = 160,
                Increment = 5
            };
            tab.Controls.Add(numLobbySlabThickness);
            yPos += 40;

            // Stair slab thickness
            Label lblStair = new Label
            {
                Text = "Stair Slab Thickness (mm):",
                Location = new System.Drawing.Point(40, yPos),
                Size = new System.Drawing.Size(250, 20)
            };
            tab.Controls.Add(lblStair);

            numStairSlabThickness = new NumericUpDown
            {
                Location = new System.Drawing.Point(300, yPos - 2),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 125,
                Maximum = 250,
                Value = 175,
                Increment = 5
            };
            tab.Controls.Add(numStairSlabThickness);
            yPos += 50;

            // Display automatic thickness rules
            GroupBox grpRules = new GroupBox
            {
                Text = "Automatic Thickness Rules",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 350)
            };
            tab.Controls.Add(grpRules);

            Label lblRules = new Label
            {
                Text =
                    "📋 REGULAR SLAB RULES (Based on Area):\n\n" +
                    "Area ≤ 14 m²  → 125mm\n" +
                    "Area ≤ 17 m²  → 135mm\n" +
                    "Area ≤ 22 m²  → 150mm\n" +
                    "Area ≤ 25 m²  → 160mm\n" +
                    "Area ≤ 32 m²  → 175mm\n" +
                    "Area ≤ 42 m²  → 200mm\n" +
                    "Area ≤ 70 m²  → 250mm\n\n" +
                    "📋 CANTILEVER SLAB RULES (Based on Span):\n\n" +
                    "Span ≤ 1.0m  → 125mm\n" +
                    "Span ≤ 1.5m  → 160mm\n" +
                    "Span ≤ 1.8m  → 180mm\n" +
                    "Span ≤ 5.0m  → 200mm\n\n" +
                    "✓ Closest section from template will be automatically selected\n" +
                    "✓ Applies to layers: balcony, cantilever, chajja",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(780, 310),
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            grpRules.Controls.Add(lblRules);
        }

        // ====================================================================
        // TAB 4: CONCRETE GRADE SCHEDULE
        // ====================================================================

        /// <summary>
        /// Initialize the Concrete Grade Schedule tab
        /// Defines concrete grades for walls, beams, and slabs from bottom to top
        /// </summary>
        /// <param name="tab">The tab page to populate</param>
        private void InitializeGradeScheduleTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int yPos = 20;

            // Instructions
            Label lblInstructions = new Label
            {
                Text = "🏗️ CONCRETE GRADE SCHEDULE - Define wall grades from bottom to top",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInstructions);
            yPos += 35;

            // Important Note
            Label lblNote = new Label
            {
                Text = "⚠️ Total floors in grade schedule MUST equal total building floors\n" +
                       "Beam/Slab grades are auto-calculated as 0.7× wall grade (rounded to nearest 5)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 35),
                Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkRed
            };
            tab.Controls.Add(lblNote);
            yPos += 50;

            // Total floors display (auto-calculated from Building Configuration)
            Label lblTotalFloorsLabel = new Label
            {
                Text = "Total Building Floors:",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblTotalFloorsLabel);

            numTotalFloors = new NumericUpDown
            {
                Location = new System.Drawing.Point(180, yPos),
                Size = new System.Drawing.Size(80, 25),
                ReadOnly = true,
                Enabled = false,
                Value = 0,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(numTotalFloors);

            Label lblAutoCalculated = new Label
            {
                Text = "(Auto-calculated from Building Configuration tab)",
                Location = new System.Drawing.Point(270, yPos + 2),
                Size = new System.Drawing.Size(400, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            tab.Controls.Add(lblAutoCalculated);
            yPos += 40;

            // ----------------------------------------------------------------
            // GRADE SCHEDULE DATA GRID
            // ----------------------------------------------------------------
            dgvGradeSchedule = new DataGridView
            {
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Column 1: Row index (read-only)
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Index",
                HeaderText = "Row",
                ReadOnly = true,
                Width = 60
            });

            // Column 2: Wall Grade (dropdown selection)
            var wallGradeColumn = new DataGridViewComboBoxColumn
            {
                Name = "WallGrade",
                HeaderText = "Wall Concrete Grade from bottom",
                DataSource = new List<string> { "M20", "M25", "M30", "M35", "M40", "M45", "M50", "M55", "M60" },
                Width = 200
            };
            dgvGradeSchedule.Columns.Add(wallGradeColumn);

            // Column 3: Number of floors for this grade
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FloorsCount",
                HeaderText = "No. of floors Concrete Grade from bottom",
                Width = 250
            });

            // Column 4: Auto-calculated beam/slab grade (read-only)
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BeamSlabGrade",
                HeaderText = "Beam/Slab Grade (Auto)",
                ReadOnly = true,
                Width = 150
            });

            // Column 5: Floor range display (read-only)
            dgvGradeSchedule.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FloorRange",
                HeaderText = "Floor Range",
                ReadOnly = true,
                Width = 150
            });

            // Event handlers for auto-calculation
            dgvGradeSchedule.CellValueChanged += DgvGradeSchedule_CellValueChanged;
            dgvGradeSchedule.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvGradeSchedule.IsCurrentCellDirty)
                {
                    dgvGradeSchedule.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            tab.Controls.Add(dgvGradeSchedule);
            yPos += 310;

            // ----------------------------------------------------------------
            // GRADE SCHEDULE BUTTONS
            // ----------------------------------------------------------------
            btnAddGradeRow = new Button
            {
                Text = "➕ Add Grade Row",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 35),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            btnAddGradeRow.Click += BtnAddGradeRow_Click;
            tab.Controls.Add(btnAddGradeRow);

            btnRemoveGradeRow = new Button
            {
                Text = "➖ Remove Selected Row",
                Location = new System.Drawing.Point(180, yPos),
                Size = new System.Drawing.Size(170, 35),
                Font = new System.Drawing.Font("Segoe UI", 9F)
            };
            btnRemoveGradeRow.Click += BtnRemoveGradeRow_Click;
            tab.Controls.Add(btnRemoveGradeRow);

            // Total validation label (shows if schedule matches building)
            lblGradeTotal = new Label
            {
                Text = "Total floors in schedule: 0 / 0",
                Location = new System.Drawing.Point(370, yPos + 8),
                Size = new System.Drawing.Size(400, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkRed
            };
            tab.Controls.Add(lblGradeTotal);
            yPos += 50;

            // ----------------------------------------------------------------
            // EXAMPLE CONFIGURATION
            // ----------------------------------------------------------------
            GroupBox grpExample = new GroupBox
            {
                Text = "Example Configuration",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 120)
            };
            tab.Controls.Add(grpExample);

            Label lblExample = new Label
            {
                Text =
                    "For a 39-story building:\n\n" +
                    "Row 0: M50 → 11 floors → Beam/Slab: M35 → Floors 1-11\n" +
                    "Row 1: M45 → 10 floors → Beam/Slab: M30 → Floors 12-21\n" +
                    "Row 2: M40 → 10 floors → Beam/Slab: M30 → Floors 22-31\n" +
                    "Row 3: M30 →  8 floors → Beam/Slab: M20 → Floors 32-39\n\n" +
                    "✓ Total: 11 + 10 + 10 + 8 = 39 floors (matches building)",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(780, 85),
                Font = new System.Drawing.Font("Consolas", 8.5F)
            };
            grpExample.Controls.Add(lblExample);

            // Sync total floors when tab is first created
            UpdateTotalFloorsForGradeSchedule();
        }

        // ====================================================================
        // GRADE SCHEDULE EVENT HANDLERS
        // ====================================================================

        /// <summary>
        /// Add a new grade row with default values (M40, 1 floor)
        /// </summary>
        private void BtnAddGradeRow_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvGradeSchedule.Rows.Add();
            var row = dgvGradeSchedule.Rows[rowIndex];

            row.Cells["Index"].Value = rowIndex;
            row.Cells["WallGrade"].Value = "M40";
            row.Cells["FloorsCount"].Value = "1";
            row.Cells["BeamSlabGrade"].Value = "M30"; // 0.7 × 40 = 28 → 30
            row.Cells["FloorRange"].Value = "";

            UpdateGradeTotals();
        }

        /// <summary>
        /// Remove the selected grade row and reindex remaining rows
        /// </summary>
        private void BtnRemoveGradeRow_Click(object sender, EventArgs e)
        {
            if (dgvGradeSchedule.SelectedRows.Count > 0)
            {
                dgvGradeSchedule.Rows.RemoveAt(dgvGradeSchedule.SelectedRows[0].Index);
                ReindexRows();
                UpdateGradeTotals();
            }
        }

        /// <summary>
        /// Handle cell value changes in the grade schedule
        /// Auto-calculates beam/slab grade when wall grade changes
        /// Updates totals when floor count changes
        /// </summary>
        private void DgvGradeSchedule_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvGradeSchedule.Rows[e.RowIndex];

            // Auto-calculate beam/slab grade when wall grade changes
            if (e.ColumnIndex == dgvGradeSchedule.Columns["WallGrade"].Index)
            {
                string wallGrade = row.Cells["WallGrade"].Value?.ToString();
                if (!string.IsNullOrEmpty(wallGrade))
                {
                    row.Cells["BeamSlabGrade"].Value = CalculateBeamSlabGrade(wallGrade);
                }
            }

            // Update totals when floor count changes
            if (e.ColumnIndex == dgvGradeSchedule.Columns["FloorsCount"].Index)
            {
                UpdateGradeTotals();
            }
        }

        /// <summary>
        /// Calculate beam and slab concrete grade based on wall grade
        /// Formula: Beam/Slab grade = 0.7 × Wall grade, rounded to nearest 5
        /// Minimum: M30 (enforced for structural integrity)
        /// </summary>
        /// <param name="wallGrade">Wall concrete grade (e.g., "M50")</param>
        /// <returns>Calculated beam/slab grade (e.g., "M35")</returns>
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            try
            {
                // Extract numeric value (e.g., "M50" → 50)
                int wallValue = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());

                // Calculate 0.7x and round to nearest 5
                // Example: M50 → 50 × 0.7 = 35.0 → 35 (already multiple of 5)
                // Example: M45 → 45 × 0.7 = 31.5 → 30 (rounded to nearest 5)
                int beamSlabValue = (int)(Math.Round((wallValue * 0.7) / 5.0) * 5);

                // FIX: Enforce minimum M30 for structural integrity
                // Lower grades (M20, M25) may not provide adequate strength for beams/slabs
                if (beamSlabValue < 30)
                    beamSlabValue = 30;

                return $"M{beamSlabValue}";
            }
            catch
            {
                // Default to M30 if parsing fails
                return "M30";
            }
        }

        /// <summary>
        /// Reindex all rows after deletion (Row 0, 1, 2, 3...)
        /// Also updates floor ranges
        /// </summary>
        private void ReindexRows()
        {
            for (int i = 0; i < dgvGradeSchedule.Rows.Count; i++)
            {
                dgvGradeSchedule.Rows[i].Cells["Index"].Value = i;
            }
            UpdateFloorRanges();
        }

        /// <summary>
        /// Update floor range display for each grade row
        /// Example: Row with 11 floors starting at floor 1 shows "1-11"
        /// </summary>
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

        /// <summary>
        /// Update grade totals and validate against total building floors
        /// Shows visual feedback: Green ✓ VALID or Red ❌ TOO MANY/TOO FEW
        /// </summary>
        private void UpdateGradeTotals()
        {
            int totalInSchedule = 0;

            // Sum all floors in grade schedule
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

            // Update label text and color based on validation
            lblGradeTotal.Text = $"Total floors in schedule: {totalInSchedule} / {requiredTotal}";
            lblGradeTotal.ForeColor = isValid ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkRed;

            if (isValid)
            {
                lblGradeTotal.Text += " ✓ VALID";
            }
            else if (totalInSchedule > requiredTotal)
            {
                lblGradeTotal.Text += " ❌ TOO MANY";
            }
            else
            {
                lblGradeTotal.Text += " ❌ TOO FEW";
            }

            UpdateFloorRanges();
        }

        /// <summary>
        /// Update total floors display for grade schedule
        /// Called when building configuration changes
        /// </summary>
        private void UpdateTotalFloorsForGradeSchedule()
        {
            int total = 0;

            // Count all floors from building configuration
            if (chkBasement.Checked)
                total += (int)numBasementLevels.Value;

            if (chkPodium.Checked)
                total += (int)numPodiumLevels.Value;

            total += 1; // E-Deck (always present)
            total += (int)numTypicalLevels.Value;

            if (chkTerrace.Checked)
                total += 1;

            numTotalFloors.Value = total;
            UpdateGradeTotals();
        }

        // ====================================================================
        // DYNAMIC CAD IMPORT TABS
        // ====================================================================

        /// <summary>
        /// Create a CAD import tab for a specific floor type
        /// Includes CAD file selection and layer mapping interface
        /// </summary>
        /// <param name="floorType">Floor type identifier (e.g., "Basement", "Typical")</param>
        /// <param name="description">Human-readable description</param>
        private void CreateCADImportTab(string floorType, string description)
        {
            TabPage tab = new TabPage($"{floorType} - CAD Import");
            tabControl.TabPages.Add(tab);

            // Description header
            Label lblDesc = new Label
            {
                Text = $"📐 {description}",
                Location = new System.Drawing.Point(20, 10),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            tab.Controls.Add(lblDesc);

            // ----------------------------------------------------------------
            // CAD FILE SELECTION
            // ----------------------------------------------------------------
            Label lblCAD = new Label
            {
                Text = "CAD File:",
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(100, 20)
            };
            tab.Controls.Add(lblCAD);

            TextBox txtCADPath = new TextBox
            {
                Location = new System.Drawing.Point(120, 43),
                Size = new System.Drawing.Size(540, 25),
                ReadOnly = true
            };
            tab.Controls.Add(txtCADPath);
            cadPathTextBoxes[floorType] = txtCADPath;

            Button btnLoadCAD = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(670, 41),
                Size = new System.Drawing.Size(120, 28)
            };
            btnLoadCAD.Click += (s, ev) => BtnLoadCAD_Click(floorType);
            tab.Controls.Add(btnLoadCAD);

            // ----------------------------------------------------------------
            // AVAILABLE LAYERS (Left side)
            // ----------------------------------------------------------------
            Label lblAvailable = new Label
            {
                Text = "Available CAD Layers:",
                Location = new System.Drawing.Point(20, 85),
                Size = new System.Drawing.Size(200, 20)
            };
            tab.Controls.Add(lblAvailable);

            ListBox lstAvailable = new ListBox
            {
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(280, 400)
            };
            tab.Controls.Add(lstAvailable);
            availableLayerListBoxes[floorType] = lstAvailable;

            // ----------------------------------------------------------------
            // ELEMENT TYPE SELECTION (Middle)
            // ----------------------------------------------------------------
            Label lblType = new Label
            {
                Text = "Assign as:",
                Location = new System.Drawing.Point(320, 110),
                Size = new System.Drawing.Size(100, 20)
            };
            tab.Controls.Add(lblType);

            ComboBox cboElement = new ComboBox
            {
                Location = new System.Drawing.Point(320, 135),
                Size = new System.Drawing.Size(140, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboElement.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Ignore" });
            cboElement.SelectedIndex = 0; // Default: Beam
            tab.Controls.Add(cboElement);
            elementTypeComboBoxes[floorType] = cboElement;

            // Add/Remove mapping buttons
            Button btnAdd = new Button
            {
                Text = "Add →",
                Location = new System.Drawing.Point(320, 170),
                Size = new System.Drawing.Size(140, 35)
            };
            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
            tab.Controls.Add(btnAdd);

            Button btnRemove = new Button
            {
                Text = "← Remove",
                Location = new System.Drawing.Point(320, 215),
                Size = new System.Drawing.Size(140, 35)
            };
            btnRemove.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
            tab.Controls.Add(btnRemove);

            // ----------------------------------------------------------------
            // MAPPED LAYERS (Right side)
            // ----------------------------------------------------------------
            Label lblMapped = new Label
            {
                Text = "Layer Mappings:",
                Location = new System.Drawing.Point(480, 85),
                Size = new System.Drawing.Size(200, 20)
            };
            tab.Controls.Add(lblMapped);

            ListBox lstMapped = new ListBox
            {
                Location = new System.Drawing.Point(480, 110),
                Size = new System.Drawing.Size(310, 400)
            };
            tab.Controls.Add(lstMapped);
            mappedLayerListBoxes[floorType] = lstMapped;
        }

      
        /// <summary>
        /// Load CAD file and populate available layers
        /// Reads actual layers from DXF files using netDxf library
        /// </summary>
        /// <param name="floorType">Floor type for this CAD file</param>
        private void BtnLoadCAD_Click(string floorType)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "AutoCAD Files (*.dxf;*.dwg)|*.dxf;*.dwg|DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
                Title = $"Select CAD File for {floorType}"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            cadPathTextBoxes[floorType].Text = ofd.FileName;

            // Check file extension
            string extension = System.IO.Path.GetExtension(ofd.FileName).ToLower();

            if (extension == ".dwg")
            {
                MessageBox.Show(
                    "DWG files are not directly supported by netDxf library.\n\n" +
                    "Please convert your DWG file to DXF format:\n" +
                    "1. Open the file in AutoCAD or any CAD software\n" +
                    "2. Save As → DXF format\n" +
                    "3. Then load the DXF file here",
                    "DWG Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (extension != ".dxf")
            {
                MessageBox.Show(
                    "Please select a DXF file.\n\n" +
                    "Supported format: .dxf\n" +
                    "To convert DWG → DXF, use AutoCAD or another CAD program.",
                    "Invalid File Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Read layers from DXF file
            try
            {
                CADLayerReader reader = new CADLayerReader();
                List<string> layers = reader.GetLayerNamesFromFile(ofd.FileName);

                if (layers.Count == 0)
                {
                    MessageBox.Show(
                        "No layers found in the CAD file.\n\n" +
                        "The file may be empty or corrupted.",
                        "No Layers Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Populate available layers list
                availableLayerListBoxes[floorType].Items.Clear();
                foreach (string layer in layers)
                {
                    availableLayerListBoxes[floorType].Items.Add(layer);
                }

                // Auto-map layers based on naming conventions
                AutoMapLayers(floorType, layers);

                MessageBox.Show(
                    $"✓ CAD file loaded successfully!\n\n" +
                    $"File: {System.IO.Path.GetFileName(ofd.FileName)}\n" +
                    $"Layers found: {layers.Count}\n\n" +
                    $"Layers have been auto-mapped based on naming conventions.\n" +
                    $"Review the mappings and adjust if needed.",
                    "Layers Loaded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error reading CAD file:\n\n{ex.Message}\n\n" +
                    "Please ensure:\n" +
                    "• The file is a valid DXF file\n" +
                    "• The file is not corrupted\n" +
                    "• You have read permissions",
                    "CAD Read Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Automatically map layers based on naming conventions
        /// B-* or *Beam* → Beam
        /// *Wall* → Wall
        /// S-* or *Slab* → Slab
        /// </summary>
        /// <param name="floorType">Floor type identifier</param>
        /// <param name="layers">List of layer names from CAD file</param>
        private void AutoMapLayers(string floorType, List<string> layers)
        {
            mappedLayerListBoxes[floorType].Items.Clear();

            foreach (string layer in layers)
            {
                string elementType = null;

                // Smart layer name detection
                if (layer.StartsWith("B-") || layer.Contains("Beam") || layer.Contains("BEAM") ||
                    layer.Contains("beam"))
                    elementType = "Beam";
                else if (layer.Contains("wall") || layer.Contains("Wall") || layer.Contains("WALL"))
                    elementType = "Wall";
                else if (layer.StartsWith("S-") || layer.Contains("Slab") || layer.Contains("SLAB"))
                    elementType = "Slab";

                if (elementType != null)
                {
                    mappedLayerListBoxes[floorType].Items.Add($"{layer} → {elementType}");
                }
            }
        }

        /// <summary>
        /// Add selected layer to mapping list
        /// </summary>
        private void BtnAddMapping_Click(string floorType)
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

        /// <summary>
        /// Remove selected layer mapping
        /// </summary>
        private void BtnRemoveMapping_Click(string floorType)
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
        // EVENT HANDLERS FOR BUILDING CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Update gravity beam width info when seismic zone changes
        /// Zone II/III: 200mm, Zone IV/V: 240mm
        /// </summary>
        private void CmbSeismicZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lblGravityWidthInfo != null && cmbSeismicZone.SelectedItem != null)
            {
                string zone = cmbSeismicZone.SelectedItem.ToString();
                int gravityWidth = (zone == "Zone II" || zone == "Zone III") ? 200 : 240;

                lblGravityWidthInfo.Text =
                    $"Width: {gravityWidth}mm (Auto-set for {zone}) - Gravity beams are non-seismic elements";
            }
        }

        /// <summary>
        /// Enable/disable basement controls when checkbox changes
        /// </summary>
        private void ChkBasement_CheckedChanged(object sender, EventArgs e)
        {
            numBasementLevels.Enabled = chkBasement.Checked;
            numBasementHeight.Enabled = chkBasement.Checked;
        }

        /// <summary>
        /// Enable/disable terrace controls when checkbox changes
        /// </summary>
        private void ChkTerrace_CheckedChanged(object sender, EventArgs e)
        {
            numTerraceheight.Enabled = chkTerrace.Checked;
        }

        /// <summary>
        /// Enable/disable podium controls when checkbox changes
        /// </summary>
        private void ChkPodium_CheckedChanged(object sender, EventArgs e)
        {
            numPodiumLevels.Enabled = chkPodium.Checked;
            numPodiumHeight.Enabled = chkPodium.Checked;
        }

        /// <summary>
        /// Generate CAD import tabs based on selected floor types
        /// Removes old tabs (keeps first 4 config tabs) and creates new ones
        /// </summary>
        private void BtnGenerateTabs_Click(object sender, EventArgs e)
        {
            // Remove old CAD import tabs (keep first 4 tabs: Building, Beams, Slabs, Grades)
            while (tabControl.TabPages.Count > 4)
            {
                tabControl.TabPages.RemoveAt(4);
            }

            // Clear dictionaries
            cadPathTextBoxes.Clear();
            availableLayerListBoxes.Clear();
            mappedLayerListBoxes.Clear();
            elementTypeComboBoxes.Clear();

            // Generate tabs based on configuration
            if (chkBasement.Checked)
            {
                CreateCADImportTab("Basement", "Basement Floor Plan");
            }

            if (chkPodium.Checked)
            {
                CreateCADImportTab("Podium", "Podium Floor Plan");
            }

            CreateCADImportTab("EDeck", "E-Deck (Ground) Floor Plan");
            CreateCADImportTab("Typical", "Typical Floor Plan (Will be replicated)");

            if (chkTerrace.Checked)
            {
                CreateCADImportTab("Terrace", "Terrace Floor Plan");
            }

            // Update total floors for grade schedule
            UpdateTotalFloorsForGradeSchedule();

            MessageBox.Show(
                "CAD Import tabs generated!\n\n" +
                "Please upload CAD files and map layers for each floor type.\n\n" +
                "⚠️ Don't forget to configure Concrete Grades tab!",
                "Tabs Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ====================================================================
        // IMPORT BUTTON - FINAL VALIDATION AND DATA COLLECTION
        // ====================================================================

        /// <summary>
        /// Validate all configurations and collect data for ETABS import
        /// Called when user clicks "Import to ETABS" button
        /// </summary>
        private void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                // ============================================================
                // COLLECT BEAM DEPTHS
                // ============================================================
                BeamDepths.Clear();
                BeamDepths["InternalGravity"] = (int)numInternalGravityDepth.Value;
                BeamDepths["CantileverGravity"] = (int)numCantileverGravityDepth.Value;
                BeamDepths["CoreMain"] = (int)numCoreMainDepth.Value;
                BeamDepths["PeripheralDeadMain"] = (int)numPeripheralDeadMainDepth.Value;
                BeamDepths["PeripheralPortalMain"] = (int)numPeripheralPortalMainDepth.Value;
                BeamDepths["InternalMain"] = (int)numInternalMainDepth.Value;

                // ============================================================
                // COLLECT SLAB THICKNESSES
                // ============================================================
                SlabThicknesses.Clear();
                SlabThicknesses["Lobby"] = (int)numLobbySlabThickness.Value;
                SlabThicknesses["Stair"] = (int)numStairSlabThickness.Value;

                // ============================================================
                // COLLECT AND VALIDATE CONCRETE GRADE SCHEDULE
                // ============================================================

                // Check for empty grade schedule
                if (dgvGradeSchedule.Rows.Count == 0)
                {
                    MessageBox.Show(
                        "No concrete grades defined!\n\n" +
                        "Please add at least one grade row in the Concrete Grades tab.",
                        "Grade Schedule Empty",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 3; // Switch to Grades tab
                    return;
                }

                WallGrades.Clear();
                FloorsPerGrade.Clear();

                int totalInSchedule = 0;
                foreach (DataGridViewRow row in dgvGradeSchedule.Rows)
                {
                    string wallGrade = row.Cells["WallGrade"].Value?.ToString();
                    string floorsStr = row.Cells["FloorsCount"].Value?.ToString();

                    // Validate each row has valid data
                    if (string.IsNullOrEmpty(wallGrade) || !int.TryParse(floorsStr, out int floors))
                    {
                        MessageBox.Show(
                            $"Invalid grade schedule at row {row.Index}.\n" +
                            "Please ensure all rows have valid grades and floor counts.",
                            "Validation Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        tabControl.SelectedIndex = 3; // Switch to Grades tab
                        return;
                    }

                    WallGrades.Add(wallGrade);
                    FloorsPerGrade.Add(floors);
                    totalInSchedule += floors;
                }

                // Validate grade schedule totals match building
                int requiredFloors = (int)numTotalFloors.Value;
                if (totalInSchedule != requiredFloors)
                {
                    MessageBox.Show(
                        $"Grade schedule floor count mismatch!\n\n" +
                        $"Building has {requiredFloors} floors\n" +
                        $"Grade schedule covers {totalInSchedule} floors\n\n" +
                        $"Please adjust the grade schedule to match total floors.",
                        "Grade Schedule Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    tabControl.SelectedIndex = 3; // Switch to Grades tab
                    return;
                }

                // ============================================================
                // VALIDATE AND COLLECT FLOOR CONFIGURATIONS
                // ============================================================
                FloorConfigs.Clear();

                // Basement
                if (chkBasement.Checked)
                {
                    if (!ValidateFloorConfig("Basement"))
                    {
                        MessageBox.Show("Please configure Basement CAD file and layer mappings.",
                            "Validation Error");
                        return;
                    }

                    FloorConfigs.Add(new FloorTypeConfig
                    {
                        Name = "Basement",
                        Count = (int)numBasementLevels.Value,
                        Height = (double)numBasementHeight.Value,
                        CADFilePath = cadPathTextBoxes["Basement"].Text,
                        LayerMapping = GetLayerMapping("Basement")
                    });
                }

                // Podium
                if (chkPodium.Checked)
                {
                    if (!ValidateFloorConfig("Podium"))
                    {
                        MessageBox.Show("Please configure Podium CAD file and layer mappings.",
                            "Validation Error");
                        return;
                    }

                    FloorConfigs.Add(new FloorTypeConfig
                    {
                        Name = "Podium",
                        Count = (int)numPodiumLevels.Value,
                        Height = (double)numPodiumHeight.Value,
                        CADFilePath = cadPathTextBoxes["Podium"].Text,
                        LayerMapping = GetLayerMapping("Podium")
                    });
                }

                // E-Deck (Ground) - Always required
                if (!ValidateFloorConfig("EDeck"))
                {
                    MessageBox.Show("Please configure E-Deck (Ground) CAD file and layer mappings.",
                        "Validation Error");
                    return;
                }

                FloorConfigs.Add(new FloorTypeConfig
                {
                    Name = "EDeck",
                    Count = 1,
                    Height = (double)numEDeckHeight.Value,
                    CADFilePath = cadPathTextBoxes["EDeck"].Text,
                    LayerMapping = GetLayerMapping("EDeck")
                });

                // Typical - Always required
                if (!ValidateFloorConfig("Typical"))
                {
                    MessageBox.Show("Please configure Typical floor CAD file and layer mappings.",
                        "Validation Error");
                    return;
                }

                FloorConfigs.Add(new FloorTypeConfig
                {
                    Name = "Typical",
                    Count = (int)numTypicalLevels.Value,
                    Height = (double)numTypicalHeight.Value,
                    CADFilePath = cadPathTextBoxes["Typical"].Text,
                    LayerMapping = GetLayerMapping("Typical")
                });

                // Terrace
                if (chkTerrace.Checked)
                {
                    if (!ValidateFloorConfig("Terrace"))
                    {
                        MessageBox.Show("Please configure Terrace floor CAD file and layer mappings.",
                            "Validation Error");
                        return;
                    }

                    FloorConfigs.Add(new FloorTypeConfig
                    {
                        Name = "Terrace",
                        Count = 1,
                        Height = (double)numTerraceheight.Value,
                        CADFilePath = cadPathTextBoxes["Terrace"].Text,
                        LayerMapping = GetLayerMapping("Terrace")
                    });
                }

                // Collect seismic zone
                SeismicZone = cmbSeismicZone.SelectedItem?.ToString() ?? "Zone IV";

                // ============================================================
                // SHOW CONFIRMATION AND CLOSE
                // ============================================================
                ShowConfirmation();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error preparing import configuration:\n\n{ex.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Validate that a floor type has CAD file and layer mappings
        /// </summary>
        /// <param name="floorType">Floor type to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidateFloorConfig(string floorType)
        {
            return cadPathTextBoxes.ContainsKey(floorType) &&
                   !string.IsNullOrEmpty(cadPathTextBoxes[floorType].Text) &&
                   mappedLayerListBoxes.ContainsKey(floorType) &&
                   mappedLayerListBoxes[floorType].Items.Count > 0;
        }

        /// <summary>
        /// Get layer mappings for a floor type
        /// Converts "LayerName → ElementType" strings to dictionary
        /// </summary>
        /// <param name="floorType">Floor type identifier</param>
        /// <returns>Dictionary of layer name to element type</returns>
        private Dictionary<string, string> GetLayerMapping(string floorType)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();

            if (mappedLayerListBoxes.ContainsKey(floorType))
            {
                foreach (var item in mappedLayerListBoxes[floorType].Items)
                {
                    string[] parts = item.ToString().Split(new[] { " → " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        mapping[parts[0]] = parts[1];
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// Show confirmation dialog with complete configuration summary
        /// User can review before final import
        /// </summary>
        private void ShowConfirmation()
        {
            int totalStories = FloorConfigs.Sum(c => c.Count);
            double totalHeight = FloorConfigs.Sum(c => c.Height * c.Count);
            int gravityWidth = (SeismicZone == "Zone II" || SeismicZone == "Zone III") ? 200 : 240;

            System.Text.StringBuilder breakdown = new System.Text.StringBuilder();
            breakdown.AppendLine("CONFIRM IMPORT");
            breakdown.AppendLine("═══════════════════════════════════════\n");

            // Building summary
            breakdown.AppendLine($"Building: {totalStories} floors, {totalHeight:F2}m, {SeismicZone}");
            breakdown.AppendLine($"Types: {string.Join(", ", FloorConfigs.Select(c => $"{c.Name}({c.Count})"))}\n");

            // Beam configuration
            breakdown.AppendLine("BEAMS:");
            breakdown.AppendLine($"  Gravity {gravityWidth}x{BeamDepths["InternalGravity"]}, Cantilever {gravityWidth}x{BeamDepths["CantileverGravity"]}");
            breakdown.AppendLine($"  Main: Core={BeamDepths["CoreMain"]}, Peri={BeamDepths["PeripheralDeadMain"]}\n");

            // Slab configuration
            breakdown.AppendLine("SLABS:");
            breakdown.AppendLine($"  Lobby={SlabThicknesses["Lobby"]}mm, Stair={SlabThicknesses["Stair"]}mm\n");

            // Grade schedule
            breakdown.AppendLine("GRADES:");
            int floorStart = 1;
            for (int i = 0; i < WallGrades.Count; i++)
            {
                string beamSlabGrade = CalculateBeamSlabGrade(WallGrades[i]);
                int floorEnd = floorStart + FloorsPerGrade[i] - 1;
                breakdown.AppendLine($"  F{floorStart}-{floorEnd}: {WallGrades[i]}/{beamSlabGrade}");
                floorStart = floorEnd + 1;
            }

            breakdown.AppendLine("\n═══════════════════════════════════════");
            breakdown.AppendLine("Ready to import?");

            var result = MessageBox.Show(
                breakdown.ToString(),
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
// END OF FILE
// ============================================================================
