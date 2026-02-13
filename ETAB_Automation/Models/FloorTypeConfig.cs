// ============================================================================
// FILE: Models/FloorTypeConfig.cs (COMPLETE WITH COMMENTS)
// ============================================================================
// PURPOSE: Data model for floor type configuration in ETAB automation system
//          Represents a single floor type (e.g., Basement, Typical, Terrace)
//          with its structural parameters and CAD import settings
// AUTHOR: ETAB Automation Team
// VERSION: 2.0 (Corrected)
// ============================================================================

using System.Collections.Generic;

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Configuration for a specific floor type in a multi-story building
    /// Each floor type (Basement, Podium, E-Deck, Typical, Terrace) has:
    /// - Physical properties (count, height)
    /// - CAD file path and layer mappings
    /// - Optional structural parameters (beam depths, slab thicknesses)
    /// - Optional concrete grade schedule
    /// </summary>
    public class FloorTypeConfig
    {
        // ====================================================================
        // CORE PROPERTIES (Always Required)
        // ====================================================================

        /// <summary>
        /// Floor type identifier (e.g., "Basement", "Podium", "EDeck", "Typical", "Terrace")
        /// Used to identify and organize different floor types in the building
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Number of floors of this type in the building
        /// Examples:
        /// - Basement: 2 (two basement levels)
        /// - Typical: 20 (twenty typical floors)
        /// - E-Deck: 1 (one ground floor)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Height per floor in meters
        /// Examples:
        /// - Basement: 3.5m (typical parking height)
        /// - Podium: 4.5m (retail/commercial height)
        /// - Typical: 3.0m (residential height)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Full path to the CAD file containing geometry for this floor type
        /// Supported formats: DWG, DXF
        /// Example: "C:\Projects\Building\Basement.dwg"
        /// </summary>
        public string CADFilePath { get; set; }

        // ====================================================================
        // CAD IMPORT CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Mapping of CAD layer names to structural element types
        /// Key: CAD layer name (e.g., "B-Core Main Beam", "W-Shear Wall")
        /// Value: Element type ("Beam", "Wall", "Slab")
        /// 
        /// Example:
        /// {
        ///     "B-Core Main Beam" => "Beam",
        ///     "B-Internal gravity beams" => "Beam",
        ///     "W-Core Walls" => "Wall",
        ///     "S-Flat Slab" => "Slab"
        /// }
        /// </summary>
        public Dictionary<string, string> LayerMapping { get; set; }

        // ====================================================================
        // OPTIONAL STRUCTURAL PARAMETERS
        // ====================================================================

        /// <summary>
        /// Beam depths in millimeters for different beam types
        /// Key: Beam type identifier
        /// Value: Depth in mm
        /// 
        /// Standard beam types:
        /// - "InternalGravity": Internal gravity beams (non-seismic)
        /// - "CantileverGravity": Cantilever gravity beams
        /// - "CoreMain": Core main beams (seismic)
        /// - "PeripheralDeadMain": Peripheral dead main beams
        /// - "PeripheralPortalMain": Peripheral portal main beams
        /// - "InternalMain": Internal main beams (seismic)
        /// 
        /// Example:
        /// {
        ///     "CoreMain" => 600,
        ///     "InternalGravity" => 450
        /// }
        /// </summary>
        public Dictionary<string, int> BeamDepths { get; set; }

        /// <summary>
        /// Slab thicknesses in millimeters for special slab types
        /// Key: Slab type identifier
        /// Value: Thickness in mm
        /// 
        /// Standard slab types:
        /// - "Lobby": Lobby slab thickness
        /// - "Stair": Stair slab thickness
        /// 
        /// Note: Regular slabs use automatic area/span-based rules
        /// 
        /// Example:
        /// {
        ///     "Lobby" => 160,
        ///     "Stair" => 175
        /// }
        /// </summary>
        public Dictionary<string, int> SlabThicknesses { get; set; }

        /// <summary>
        /// Optional floor-specific concrete grade schedule
        /// Allows different floors of the same type to have different grades
        /// 
        /// Note: Typically, grade schedule is defined globally for the entire building
        /// This property is for special cases where a specific floor type needs
        /// different grading rules
        /// </summary>
        public GradeSchedule GradeSchedule { get; set; }

        // ====================================================================
        // CONSTRUCTORS
        // ====================================================================

        /// <summary>
        /// Default constructor - initializes empty collections
        /// Use this when building configuration step-by-step
        /// </summary>
        public FloorTypeConfig()
        {
            LayerMapping = new Dictionary<string, string>();
            BeamDepths = new Dictionary<string, int>();
            SlabThicknesses = new Dictionary<string, int>();
        }

        /// <summary>
        /// Parameterized constructor - creates a floor config with core properties
        /// Use this for quick initialization with known values
        /// </summary>
        /// <param name="name">Floor type name (e.g., "Basement")</param>
        /// <param name="count">Number of floors of this type</param>
        /// <param name="height">Height per floor in meters</param>
        /// <param name="cadFilePath">Path to CAD file</param>
        public FloorTypeConfig(string name, int count, double height, string cadFilePath)
        {
            Name = name;
            Count = count;
            Height = height;
            CADFilePath = cadFilePath;
            LayerMapping = new Dictionary<string, string>();
            BeamDepths = new Dictionary<string, int>();
            SlabThicknesses = new Dictionary<string, int>();
        }

        // ====================================================================
        // CONFIGURATION METHODS
        // ====================================================================

        /// <summary>
        /// Add or update a CAD layer mapping
        /// Maps a CAD layer to a structural element type
        /// </summary>
        /// <param name="layerName">CAD layer name (e.g., "B-Core Main Beam")</param>
        /// <param name="elementType">Element type: "Beam", "Wall", or "Slab"</param>
        /// <example>
        /// config.AddLayerMapping("B-Core Main Beam", "Beam");
        /// config.AddLayerMapping("W-Shear Wall", "Wall");
        /// </example>
        public void AddLayerMapping(string layerName, string elementType)
        {
            LayerMapping[layerName] = elementType;
        }

        /// <summary>
        /// Set beam depth for a specific beam type
        /// Overwrites existing value if already set
        /// </summary>
        /// <param name="beamType">Beam type identifier (e.g., "CoreMain", "InternalGravity")</param>
        /// <param name="depthMm">Beam depth in millimeters (e.g., 600, 450)</param>
        /// <example>
        /// config.SetBeamDepth("CoreMain", 600);
        /// config.SetBeamDepth("InternalGravity", 450);
        /// </example>
        public void SetBeamDepth(string beamType, int depthMm)
        {
            BeamDepths[beamType] = depthMm;
        }

        /// <summary>
        /// Set slab thickness for a specific slab type
        /// Overwrites existing value if already set
        /// </summary>
        /// <param name="slabType">Slab type identifier (e.g., "Lobby", "Stair")</param>
        /// <param name="thicknessMm">Slab thickness in millimeters (e.g., 160, 175)</param>
        /// <example>
        /// config.SetSlabThickness("Lobby", 160);
        /// config.SetSlabThickness("Stair", 175);
        /// </example>
        public void SetSlabThickness(string slabType, int thicknessMm)
        {
            SlabThicknesses[slabType] = thicknessMm;
        }

        // ====================================================================
        // COMPUTED PROPERTIES
        // ====================================================================

        /// <summary>
        /// Calculate total vertical height for all floors of this type
        /// Formula: Total Height = Height per floor × Number of floors
        /// </summary>
        /// <returns>Total height in meters</returns>
        /// <example>
        /// If Height = 3.0m and Count = 10:
        /// GetTotalHeight() returns 30.0m
        /// </example>
        public double GetTotalHeight()
        {
            return Height * Count;
        }

        // ====================================================================
        // VALIDATION
        // ====================================================================

        /// <summary>
        /// Validate that this floor configuration is complete and ready for import
        /// 
        /// Validation checks:
        /// 1. Name is not empty
        /// 2. Count is greater than 0
        /// 3. Height is greater than 0
        /// 4. CAD file path is not empty
        /// 5. CAD file exists on disk
        /// 6. At least one layer mapping is defined
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        /// <example>
        /// if (!config.IsValid())
        /// {
        ///     MessageBox.Show("Configuration incomplete!");
        ///     return;
        /// }
        /// </example>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) &&
                   Count > 0 &&
                   Height > 0 &&
                   !string.IsNullOrEmpty(CADFilePath) &&
                   System.IO.File.Exists(CADFilePath) &&
                   LayerMapping.Count > 0;
        }

        // ====================================================================
        // STRING REPRESENTATION
        // ====================================================================

        /// <summary>
        /// Get a human-readable string representation of this floor configuration
        /// Format: "Name: Count floor(s) × Height m = Total height m total"
        /// </summary>
        /// <returns>Formatted string description</returns>
        /// <example>
        /// Output: "Typical: 10 floor(s) × 3.00m = 30.00m total"
        /// Output: "Basement: 2 floor(s) × 3.50m = 7.00m total"
        /// </example>
        public override string ToString()
        {
            return $"{Name}: {Count} floor(s) × {Height:F2}m = {GetTotalHeight():F2}m total";
        }
    }

}
