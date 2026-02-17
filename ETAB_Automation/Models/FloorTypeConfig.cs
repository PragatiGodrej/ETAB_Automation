// ============================================================================
// FILE: Models/FloorTypeConfig.cs
// ============================================================================
// PURPOSE: Data model for storing floor type configuration including CAD file,
//          layer mappings, beam depths, and slab thicknesses
// AUTHOR: ETAB Automation Team
// VERSION: 2.1
// ============================================================================

using System.Collections.Generic;

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Configuration for a specific floor type (Basement, Podium, E-Deck, Typical, Terrace)
    /// Contains all data needed to import and configure that floor type in ETABS
    /// </summary>
    public class FloorTypeConfig
    {
        /// <summary>
        /// Floor type name (e.g., "Basement", "Podium", "EDeck", "Typical", "Terrace")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Number of floors of this type (e.g., 3 basements, 10 typical floors)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Height of each floor in meters (e.g., 3.0m for typical, 4.5m for E-Deck)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Full path to the CAD file (.dxf) for this floor type
        /// </summary>
        public string CADFilePath { get; set; }

        /// <summary>
        /// Layer name to element type mapping
        /// Key: Layer name from CAD file (e.g., "B-Core Main Beam")
        /// Value: Element type (e.g., "Beam", "Wall", "Slab")
        /// </summary>
        public Dictionary<string, string> LayerMapping { get; set; }

        /// <summary>
        /// Beam depths in millimeters for different beam types
        /// Keys: InternalGravity, CantileverGravity, CoreMain, PeripheralDeadMain, 
        ///       PeripheralPortalMain, InternalMain
        /// Values: Depth in mm (e.g., 450, 600, 650)
        /// </summary>
        public Dictionary<string, int> BeamDepths { get; set; }

        /// <summary>
        /// Slab thicknesses in millimeters for special slab types
        /// Keys: Lobby, Stair
        /// Values: Thickness in mm (e.g., 160, 175)
        /// Note: Regular slabs use area/span-based automatic rules
        /// </summary>
        public Dictionary<string, int> SlabThicknesses { get; set; }

        /// <summary>
        /// Default constructor - initializes empty collections
        /// </summary>
        public FloorTypeConfig()
        {
            LayerMapping = new Dictionary<string, string>();
            BeamDepths = new Dictionary<string, int>();
            SlabThicknesses = new Dictionary<string, int>();
        }

        /// <summary>
        /// Get a summary string for this floor configuration
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({Count} floors @ {Height}m each)";
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
