//// ============================================================================
//// FILE: Models/FloorTypeConfig.cs
//// ============================================================================
//// PURPOSE: Data model for storing floor type configuration including CAD file,
////          layer mappings, beam depths, and slab thicknesses
//// AUTHOR: ETAB Automation Team
//// VERSION: 2.1
//// ============================================================================

//using System.Collections.Generic;

//namespace ETAB_Automation.Models
//{
//    /// <summary>
//    /// Configuration for a specific floor type (Basement, Podium, E-Deck, Typical, Terrace)
//    /// Contains all data needed to import and configure that floor type in ETABS
//    /// </summary>
//    public class FloorTypeConfig
//    {
//        /// <summary>
//        /// Floor type name (e.g., "Basement", "Podium", "EDeck", "Typical", "Terrace")
//        /// </summary>
//        public string Name { get; set; }

//        /// <summary>
//        /// Number of floors of this type (e.g., 3 basements, 10 typical floors)
//        /// </summary>
//        public int Count { get; set; }

//        /// <summary>
//        /// Height of each floor in meters (e.g., 3.0m for typical, 4.5m for E-Deck)
//        /// </summary>
//        public double Height { get; set; }

//        /// <summary>
//        /// Full path to the CAD file (.dxf) for this floor type
//        /// </summary>
//        public string CADFilePath { get; set; }

//        /// <summary>
//        /// Layer name to element type mapping
//        /// Key: Layer name from CAD file (e.g., "B-Core Main Beam")
//        /// Value: Element type (e.g., "Beam", "Wall", "Slab")
//        /// </summary>
//        public Dictionary<string, string> LayerMapping { get; set; }

//        /// <summary>
//        /// Beam depths in millimeters for different beam types
//        /// Keys: InternalGravity, CantileverGravity, CoreMain, PeripheralDeadMain, 
//        ///       PeripheralPortalMain, InternalMain
//        /// Values: Depth in mm (e.g., 450, 600, 650)
//        /// </summary>
//        public Dictionary<string, int> BeamDepths { get; set; }

//        /// <summary>
//        /// Slab thicknesses in millimeters for special slab types
//        /// Keys: Lobby, Stair
//        /// Values: Thickness in mm (e.g., 160, 175)
//        /// Note: Regular slabs use area/span-based automatic rules
//        /// </summary>
//        public Dictionary<string, int> SlabThicknesses { get; set; }

//        /// <summary>
//        /// Indicates if this floor represents an individual basement level
//        /// True for: Basement1, Basement2, Basement3, Basement4, Basement5
//        /// False for: Podium, Ground, EDeck, Typical, Terrace
//        /// </summary>
//        public bool IsIndividualBasement
//        {
//            get
//            {
//                return Name != null && Name.StartsWith("Basement") &&
//                       Name.Length > 8 && char.IsDigit(Name[8]);
//            }
//        }
//        /// <summary>
//        /// Gets the basement floor number if this is an individual basement
//        /// Returns: 1-5 for Basement1-Basement5, 0 otherwise
//        /// </summary>
//        public int BasementNumber
//        {
//            get
//            {
//                if (IsIndividualBasement && Name.Length > 8)
//                {
//                    if (int.TryParse(Name.Substring(8), out int num))
//                        return num;
//                }
//                return 0;
//            }
//        }
//        /// <summary>
//        /// Default constructor - initializes empty collections
//        /// </summary>
//        public FloorTypeConfig()
//        {
//            LayerMapping = new Dictionary<string, string>();
//            BeamDepths = new Dictionary<string, int>();
//            SlabThicknesses = new Dictionary<string, int>();
//        }

//        /// <summary>
//        /// Get a summary string for this floor configuration
//        /// </summary>
//        public override string ToString()
//        {
//            return $"{Name} ({Count} floors @ {Height}m each)";
//        }
//    }
//}

//// ============================================================================
//// END OF FILE
//// ============================================================================
// ============================================================================
// FILE: Models/FloorTypeConfig.cs
// VERSION: 2.3 — Added WallThicknessOverrides, BeamWidthOverrides, NtaWallThickness
// ============================================================================

using System.Collections.Generic;

namespace ETAB_Automation.Models
{
    public class FloorTypeConfig
    {
        // ── Basic ─────────────────────────────────────────────────────────
        public string Name { get; set; }
        public int Count { get; set; }
        public double Height { get; set; }   // in metres
        public string CADFilePath { get; set; }

        // ── Basement ──────────────────────────────────────────────────────────
        /// <summary>
        /// True when this config represents a single individually-modelled
        /// basement floor (e.g. B1, B2) rather than a repeated floor type.
        /// </summary>
        public bool IsIndividualBasement { get; set; } = false;

        /// <summary>
        /// 1-based basement number (1 = B1, 2 = B2, …).
        /// Only meaningful when IsIndividualBasement is true.
        /// </summary>
        public int BasementNumber { get; set; } = 0;

        // ── Layer mapping: layer name → "Beam" / "Wall" / "Slab" / "Column"
        public Dictionary<string, string> LayerMapping { get; set; }
            = new Dictionary<string, string>();

        // ── Beam depths (mm) — all user-defined
        // Keys: InternalGravity, CantileverGravity, NoLoadGravity, EdeckGravity,
        //       PodiumGravity, GroundGravity, BasementGravity,
        //       CoreMain, PeripheralDeadMain, PeripheralPortalMain, InternalMain
        public Dictionary<string, int> BeamDepths { get; set; }
            = new Dictionary<string, int>();

        // ── Beam width overrides (mm) — 0 = use auto rule
        // Keys: GravityWidth, CoreMainWidth, PeripheralDeadMainWidth,
        //       PeripheralPortalMainWidth, InternalMainWidth
        public Dictionary<string, int> BeamWidthOverrides { get; set; }
            = new Dictionary<string, int>();

        // ── Slab thicknesses for YELLOW layers (mm)
        // Keys: Lobby, Stair, FireTender, OHT, TerraceFire,
        //       UGT, Landscape, Swimming, DG, STP
        public Dictionary<string, int> SlabThicknesses { get; set; }
            = new Dictionary<string, int>();

        // ── Wall thickness overrides (mm) — 0 = use GPL IS 1893-2025 table
        // Keys: CoreWall, PeriphDeadWall, PeriphPortalWall, InternalWall
        public Dictionary<string, int> WallThicknessOverrides { get; set; }
            = new Dictionary<string, int>();

        // ── W-NTA wall: always user-defined, never from GPL table
        public int NtaWallThickness { get; set; } = 200;

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>Returns beam depth for the given key, with safe fallback.</summary>
        public int GetBeamDepth(string key, int fallback = 450)
            => BeamDepths.TryGetValue(key, out int v) ? v : fallback;

        /// <summary>
        /// Returns beam width override, or 0 if not set (meaning "use auto").
        /// </summary>
        public int GetBeamWidthOverride(string key)
            => BeamWidthOverrides.TryGetValue(key, out int v) ? v : 0;

        /// <summary>Returns slab thickness for the given key, with safe fallback.</summary>
        public int GetSlabThickness(string key, int fallback = 150)
            => SlabThicknesses.TryGetValue(key, out int v) ? v : fallback;

        /// <summary>
        /// Returns wall thickness override. 0 means "use GPL table".
        /// </summary>
        public int GetWallThicknessOverride(string key)
            => WallThicknessOverrides.TryGetValue(key, out int v) ? v : 0;
    }
}
