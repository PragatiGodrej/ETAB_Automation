
// ============================================================================
// FILE: Models/FloorTypeConfig.cs

// ============================================================================
using System.Collections.Generic;

namespace ETAB_Automation.Models
{
    public class FloorTypeConfig
    {
        // ── Basic ─────────────────────────────────────────────────────────
        public string Name { get; set; }
        public int Count { get; set; }
        public double Height { get; set; }
        public string CADFilePath { get; set; }

        // ── Basement ──────────────────────────────────────────────────────
        public bool IsIndividualBasement { get; set; } = false;
        public int BasementNumber { get; set; } = 0;

        // ── Podium (individual tabs, parallel to Basement) ────────────────
        public bool IsIndividualPodium { get; set; } = false;
        public int PodiumNumber { get; set; } = 0;

        
        // ── Layer mapping: layer name → "Beam" / "Wall" / "Slab" / "Column"
        public Dictionary<string, string> LayerMapping { get; set; }
            = new Dictionary<string, string>();

        // ====================================================================
        // BEAM DEPTHS (mm) — all user-defined
        // ====================================================================
        // Gravity keys : InternalGravity, CantileverGravity, NoLoadGravity,
        //                EdeckGravity, PodiumGravity, GroundGravity, BasementGravity
        // Main keys    : CoreMain, PeripheralDeadMain,
        //                PeripheralPortalMain, InternalMain
        public Dictionary<string, int> BeamDepths { get; set; }
            = new Dictionary<string, int>();

        // ====================================================================
        // BEAM WIDTH OVERRIDES (mm) — 0 = use auto rule
        // ====================================================================
        // Per-variant gravity widths (each overridable independently):
        //   InternalGravityWidth, CantileverGravityWidth, NoLoadGravityWidth,
        //   EdeckGravityWidth, PodiumGravityWidth, GroundGravityWidth,
        //   BasementGravityWidth
        //   Auto rule: 200 mm (Zone II/III) | 240 mm (Zone IV/V)
        //
        // Main beam widths (auto = matching wall thickness):
        //   CoreMainWidth, PeripheralDeadMainWidth,
        //   PeripheralPortalMainWidth, InternalMainWidth
        public Dictionary<string, int> BeamWidthOverrides { get; set; }
            = new Dictionary<string, int>();

        // ====================================================================
        // BEAM WALL LOAD SETS — "WALL LOAD" case (user-named load patterns)
        // ====================================================================
        // One entry per beam layer key.  The value is the ETABS load pattern
        // name to apply as wall/dead load on that beam type.
        // B-No Load Gravity beams always get 0 (no wall load) — store empty
        // string or "0" to indicate this.
        //
        // Keys mirror BeamDepths gravity + main keys:
        //   InternalGravity, CantileverGravity, NoLoadGravity (→ "0"),
        //   EdeckGravity, PodiumGravity, GroundGravity, BasementGravity,
        //   CoreMain, PeripheralDeadMain, PeripheralPortalMain, InternalMain
        public Dictionary<string, string> BeamWallLoadSets { get; set; }
            = new Dictionary<string, string>();

        // ====================================================================
        // SLAB THICKNESSES — YELLOW layers (mm, user input)
        // ====================================================================
        // Keys: Lobby, Stair, FireTender, OHT, TerraceFire,
        //       UGT, Landscape, Swimming, DG, STP
        // (All other layers use auto area-rule or cantilever-span rule.)
        public Dictionary<string, int> SlabThicknesses { get; set; }
            = new Dictionary<string, int>();

        // ====================================================================
        // SLAB LOAD SETS — ETABS load pattern name per slab layer
        // ====================================================================
        // Every slab layer (WHITE, CYAN, and YELLOW) has an assigned load set.
        // The value is the ETABS load pattern name (e.g. "AMENITIES",
        // "BALCONY", "FIRE TENDER", "LOBBY", "OHT", "UGT", "SWIMMING", …).
        // Matches the "Slab load set assigned" column in the reference table.
        //
        // Full key list (use exact CAD layer name as key, minus the "S-" prefix
        // for brevity, or use the full layer name — be consistent):
        //   Amenities, Balcony, Chajja, ChajjaODU, Driveway, FireTender,
        //   FireWaterTank, GarbageRoom, GardenDining, Gymnasium, IndoorSports,
        //   KitchenSink, LMR, LMRTop, Lobby, MeterRoom, MultipurposeHall,
        //   OHT, OHTTop, Parking, ParkingToilet, PumpRoom, Refuge,
        //   Residential, Retail, RetailMazzanine, RetailToilet, ServiceSlab,
        //   SocietyRoom, StackParking, Staircase, Terrace, TerracePumpRoom,
        //   Toilet, UGT, Landscape, Swimming, DG, STP, Utility
        public Dictionary<string, string> SlabLoadSets { get; set; }
            = new Dictionary<string, string>();

        // ====================================================================
        // WALL THICKNESS OVERRIDES (mm) — 0 = use GPL IS 1893-2025 table
        // ====================================================================
        // Keys: CoreWall, PeriphDeadWall, PeriphPortalWall, InternalWall
        public Dictionary<string, int> WallThicknessOverrides { get; set; }
            = new Dictionary<string, int>();

        // ── W-NTA: always user-defined, never from GPL table ──────────────
        public int NtaWallThickness { get; set; } = 200;

        // ====================================================================
        // STATIC DEFAULT LOAD SET TABLES
        // ====================================================================
        // These provide the baseline load set names shown in the reference
        // table (Image 1 / Image 2).  The UI pre-fills from these; users can
        // override per floor type.

        /// <summary>
        /// Default slab load set names, keyed by the short slab layer name
        /// (i.e. the part after "S-" in the CAD layer, normalised to PascalCase).
        /// Source: "Slab load set assigned" column in the reference table.
        /// CYAN layers (cantilever) are marked with prefix "CYAN:" so the
        /// importer knows to apply a span-based thickness instead of a fixed one.
        /// </summary>
        public static readonly Dictionary<string, string> DefaultSlabLoadSets
            = new Dictionary<string, string>
            {
                // ── WHITE layers (area rule thickness) ────────────────────────
                ["Amenities"] = "AMENITIES",
                ["Driveway"] = "DRIVEWAY",
                ["FireWaterTank"] = "FIRE WATER TANK",
                ["GarbageRoom"] = "GARBAGE ROOM",
                ["GardenDining"] = "GARDEN/DINING AREA",
                ["Gymnasium"] = "GYMNASIUM",
                ["IndoorSports"] = "INDOOR SPORTS",
                ["KitchenSink"] = "KITCHEN SUNK",
                ["LMR"] = "LMR",
                ["LMRTop"] = "LMRTOP",
                ["MeterRoom"] = "METER ROOM",
                ["MultipurposeHall"] = "MULTIPURPOSE HALL",
                ["OHTTop"] = "OHT TOP",
                ["Parking"] = "PARKING",
                ["ParkingToilet"] = "PARKING TOILET",
                ["PumpRoom"] = "PUMP ROOM",
                ["Refuge"] = "REFUGE",
                ["Residential"] = "RESIDENTIAL",
                ["Retail"] = "RETAIL",
                ["RetailMazzanine"] = "RETAIL MAZZANINE",
                ["RetailToilet"] = "RETAIL TOILET",
                ["ServiceSlab"] = "SERVICE SLAB",
                ["SocietyRoom"] = "SOCIETY ROOM",
                ["StackParking"] = "STACK PARKING",
                ["Terrace"] = "TERRACE",
                ["TerracePumpRoom"] = "TERRACE PUMP ROOM",
                ["Toilet"] = "TOILET",
                ["Utility"] = "UTILITY",

                // ── CYAN layers (cantilever span rule) ────────────────────────
                ["Balcony"] = "BALCONY",
                ["Chajja"] = "CHAJJA",
                ["ChajjaODU"] = "CHAJJA+ODU",

                // ── YELLOW layers (user-input fixed thickness) ─────────────────
                ["FireTender"] = "FIRE TENDER",
                ["Lobby"] = "LOBBY",
                ["OHT"] = "OHT",
                ["Staircase"] = "STAIRCASE",
                ["TerraceFire"] = "TERRACE FIRE TANK",
                ["UGT"] = "UGT",
                ["Landscape"] = "LANDSCAPE",
                ["Swimming"] = "SWIMMING",
                ["DG"] = "DG",
                ["STP"] = "STP",
            };

        /// <summary>
        /// Default beam wall load set names per beam-type key.
        /// Source: "Beam Wall load set assigned" column in Image 2.
        /// Empty string = no wall load (B-No Load Gravity Beams).
        /// The actual ETABS load pattern name is user-defined; these are
        /// sensible starting defaults shown in the UI.
        /// </summary>
        public static readonly Dictionary<string, string> DefaultBeamWallLoadSets
            = new Dictionary<string, string>
            {
                ["InternalGravity"] = "WALL LOAD",   // user input — typical UDL
                ["CantileverGravity"] = "WALL LOAD",   // user input
                ["NoLoadGravity"] = "",             // 0 — no wall load
                ["EdeckGravity"] = "WALL LOAD",   // user input
                ["PodiumGravity"] = "WALL LOAD",   // user input
                ["GroundGravity"] = "WALL LOAD",   // user input
                ["BasementGravity"] = "WALL LOAD",   // user input
                ["CoreMain"] = "WALL LOAD",   // user input
                ["PeripheralDeadMain"] = "WALL LOAD",   // user input
                ["PeripheralPortalMain"] = "WALL LOAD",   // user input
                ["InternalMain"] = "WALL LOAD",   // user input
            };

        // ====================================================================
        // HELPERS
        // ====================================================================

        public int GetBeamDepth(string key, int fallback = 450)
            => BeamDepths.TryGetValue(key, out int v) ? v : fallback;

        /// <summary>
        /// Returns the width override for a beam variant.
        /// Falls back to the legacy shared "GravityWidth" key so configs
        /// saved before v2.4 (single shared gravity width) still load correctly.
        /// </summary>
        public int GetBeamWidthOverride(string key)
        {
            if (BeamWidthOverrides.TryGetValue(key, out int v)) return v;
            // Legacy fallback for any gravity variant
            if (key.EndsWith("GravityWidth") &&
                BeamWidthOverrides.TryGetValue("GravityWidth", out int legacy))
                return legacy;
            return 0;   // 0 = auto
        }

        /// <summary>
        /// Returns the wall load set name for a beam type.
        /// Falls back to the static default table if not explicitly overridden.
        /// Returns empty string for B-No Load beams (no wall load).
        /// </summary>
        public string GetBeamWallLoadSet(string key)
        {
            if (BeamWallLoadSets.TryGetValue(key, out string v)) return v;
            if (DefaultBeamWallLoadSets.TryGetValue(key, out string def)) return def;
            return "WALL LOAD";
        }

        public int GetSlabThickness(string key, int fallback = 150)
            => SlabThicknesses.TryGetValue(key, out int v) ? v : fallback;

        /// <summary>
        /// Returns the ETABS load pattern name for a slab layer.
        /// Falls back to the static default table if not explicitly overridden.
        /// </summary>
        public string GetSlabLoadSet(string key)
        {
            if (SlabLoadSets.TryGetValue(key, out string v)) return v;
            if (DefaultSlabLoadSets.TryGetValue(key, out string def)) return def;
            return key.ToUpperInvariant();  // last-resort: use layer name itself
        }

        public int GetWallThicknessOverride(string key)
            => WallThicknessOverrides.TryGetValue(key, out int v) ? v : 0;

        // ── Convenience type flags ────────────────────────────────────────
        public bool IsBasementType => IsIndividualBasement;
        public bool IsPodiumType => IsIndividualPodium;
        public bool IsTypicalType => Name == "Typical";
        public bool IsTerraceType => Name == "Terrace";
        public bool IsGroundType => Name == "Ground";
        public bool IsEDeckType => Name == "EDeck";
    }
}
// ============================================================================
// END OF FILE
// ============================================================================