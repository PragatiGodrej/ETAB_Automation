
//// ============================================================================
//// FILE: Models/FloorTypeConfig.cs
//// VERSION: 2.1 — Added ISCodeVersion property
//// ============================================================================

//using System.Collections.Generic;
//using ETAB_Automation.Core;

//namespace ETAB_Automation.Models
//{
//    public class FloorTypeConfig
//    {
//        // ── Basic ─────────────────────────────────────────────────────────
//        public string Name { get; set; }
//        public int Count { get; set; }
//        public double Height { get; set; }
//        public string CADFilePath { get; set; }

//        // ── Basement ──────────────────────────────────────────────────────
//        public bool IsIndividualBasement { get; set; } = false;
//        public int BasementNumber { get; set; } = 0;

//        // ── Podium ────────────────────────────────────────────────────────
//        public bool IsIndividualPodium { get; set; } = false;
//        public int PodiumNumber { get; set; } = 0;

//        // ── IS Code Edition ───────────────────────────────────────────────
//        /// <summary>
//        /// IS 1893 edition used for wall thickness table lookups.
//        /// IS2016 = IS 1893:2016 (TDD/PKO)
//        /// IS2025 = IS 1893:2025 (TDD/MSO)  ← default
//        /// Set once globally from the UI and propagated to every FloorTypeConfig.
//        /// </summary>
//        public WallThicknessCalculator.ISCodeVersion ISCodeVersion { get; set; }
//            = WallThicknessCalculator.ISCodeVersion.IS2025;

//        // ── Layer mapping: layer name → "Beam" / "Wall" / "Slab" / "Column"
//        public Dictionary<string, string> LayerMapping { get; set; }
//            = new Dictionary<string, string>();

//        // ── Beam depths (mm) ──────────────────────────────────────────────
//        public Dictionary<string, int> BeamDepths { get; set; }
//            = new Dictionary<string, int>();

//        // ── Beam width overrides (mm, 0 = auto) ───────────────────────────
//        public Dictionary<string, int> BeamWidthOverrides { get; set; }
//            = new Dictionary<string, int>();

//        // ── Beam wall load sets ────────────────────────────────────────────
//        public Dictionary<string, string> BeamWallLoadSets { get; set; }
//            = new Dictionary<string, string>();

//        // ── Column dimensions ─────────────────────────────────────────────
//        public int ColumnB { get; set; } = 300;
//        public int ColumnD { get; set; } = 450;

//        // ── Slab thicknesses (mm, YELLOW layers) ──────────────────────────
//        public Dictionary<string, int> SlabThicknesses { get; set; }
//            = new Dictionary<string, int>();

//        // ── Slab load sets ────────────────────────────────────────────────
//        public Dictionary<string, string> SlabLoadSets { get; set; }
//            = new Dictionary<string, string>();

//        // ── Wall thickness overrides (mm, 0 = use IS table) ───────────────
//        public Dictionary<string, int> WallThicknessOverrides { get; set; }
//            = new Dictionary<string, int>();

//        // ── W-NTA: non-structural wall (always user-defined) ──────────────
//        public int NtaWallThickness { get; set; } = 200;

//        // ====================================================================
//        // STATIC DEFAULT LOAD SET TABLES
//        // ====================================================================

//        public static readonly Dictionary<string, string> DefaultSlabLoadSets
//            = new Dictionary<string, string>
//            {
//                // ── WHITE layers (area rule) ──────────────────────────────
//                ["Amenities"] = "AMENITIES",
//                ["Driveway"] = "DRIVEWAY",
//                ["FireWaterTank"] = "WATER TANK",
//                ["GarbageRoom"] = "GARBAGE ROOM",
//                ["GardenDining"] = "GARDEN DINING",
//                ["Gymnasium"] = "GYMNASIUM",
//                ["IndoorSports"] = "INDOOR SPORTS",
//                ["KitchenSink"] = "KITCHEN SINK",
//                ["LMR"] = "LMR",
//                ["LMRTop"] = "LMR TOP",
//                ["MeterRoom"] = "METER ROOM",
//                ["MultipurposeHall"] = "MULTIPURPOSE HALL",
//                ["OHTTop"] = "OHT TOP",
//                ["Parking"] = "PARKING",
//                ["ParkingToilet"] = "PARKING TOILET",
//                ["PumpRoom"] = "PUMP ROOM",
//                ["Refuge"] = "REFUGE",
//                ["Residential"] = "RESIDENTIAL",
//                ["Retail"] = "RETAIL",
//                ["RetailMazzanine"] = "RETAIL MAZZANINE",
//                ["RetailToilet"] = "RETAIL TOILET",
//                ["ServiceSlab"] = "SERVICE SLAB",
//                ["SocietyRoom"] = "SOCIETY ROOM",
//                ["StackParking"] = "STACK PARKING",
//                ["Terrace"] = "TERRACE",
//                ["TerracePumpRoom"] = "TERRACE PUMP ROOM",
//                ["Toilet"] = "TOILET",
//                ["Utility"] = "UTILITY",
//                // ── CYAN layers (cantilever span rule) ────────────────────
//                ["Balcony"] = "BALCONY",
//                ["Chajja"] = "CHAJJA",
//                ["ChajjaODU"] = "CHAJJA+ODU",
//                // ── YELLOW layers (user fixed thickness) ──────────────────
//                ["FireTender"] = "FIRE TENDER",
//                ["Lobby"] = "LOBBY",
//                ["OHT"] = "OHT",
//                ["Staircase"] = "STAIRCASE",
//                ["TerraceFire"] = "TERRACE FIRE TANK",
//                ["UGT"] = "UGT",
//                ["Landscape"] = "LANDSCAPE",
//                ["Swimming"] = "SWIMMING",
//                ["DG"] = "DG",
//                ["STP"] = "STP",
//            };

//        public static readonly Dictionary<string, string> DefaultBeamWallLoadSets
//            = new Dictionary<string, string>
//            {
//                ["InternalGravity"] = "WALL LOAD",
//                ["CantileverGravity"] = "WALL LOAD",
//                ["NoLoadGravity"] = "",
//                ["EdeckGravity"] = "WALL LOAD",
//                ["PodiumGravity"] = "WALL LOAD",
//                ["GroundGravity"] = "WALL LOAD",
//                ["BasementGravity"] = "WALL LOAD",
//                ["CoreMain"] = "WALL LOAD",
//                ["PeripheralDeadMain"] = "WALL LOAD",
//                ["PeripheralPortalMain"] = "WALL LOAD",
//                ["InternalMain"] = "WALL LOAD",
//            };

//        // ====================================================================
//        // HELPERS
//        // ====================================================================

//        public int GetBeamDepth(string key, int fallback = 450)
//            => BeamDepths.TryGetValue(key, out int v) ? v : fallback;

//        public int GetBeamWidthOverride(string key)
//        {
//            if (BeamWidthOverrides.TryGetValue(key, out int v)) return v;
//            if (key.EndsWith("GravityWidth") &&
//                BeamWidthOverrides.TryGetValue("GravityWidth", out int legacy))
//                return legacy;
//            return 0;
//        }

//        public string GetBeamWallLoadSet(string key)
//        {
//            if (BeamWallLoadSets.TryGetValue(key, out string v)) return v;
//            if (DefaultBeamWallLoadSets.TryGetValue(key, out string def)) return def;
//            return "WALL LOAD";
//        }

//        public int GetSlabThickness(string key, int fallback = 150)
//            => SlabThicknesses.TryGetValue(key, out int v) ? v : fallback;

//        public string GetSlabLoadSet(string key)
//        {
//            if (SlabLoadSets.TryGetValue(key, out string v)) return v;
//            if (DefaultSlabLoadSets.TryGetValue(key, out string def)) return def;
//            return key.ToUpperInvariant();
//        }

//        public int GetWallThicknessOverride(string key)
//            => WallThicknessOverrides.TryGetValue(key, out int v) ? v : 0;

//        // ── Convenience type flags ────────────────────────────────────────
//        public bool IsBasementType => IsIndividualBasement;
//        public bool IsPodiumType => IsIndividualPodium;
//        public bool IsTypicalType => Name == "Typical";
//        public bool IsTerraceType => Name == "Terrace";
//        public bool IsGroundType => Name == "Ground";
//        public bool IsEDeckType => Name == "EDeck";
//    }
//}

// ============================================================================
// FILE: Models/FloorTypeConfig.cs
// VERSION: 3.0 — Individual load pattern values per slab layer (no Load Sets)
// ============================================================================

using System.Collections.Generic;
using ETAB_Automation.Core;

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Holds individual load-pattern magnitudes (kN/m²) for one slab layer.
    /// Each field maps to a specific ETABS load pattern.
    /// Zero means the load is not applicable → that pattern is skipped.
    /// </summary>
    public class SlabLoads
    {
        public double FF { get; set; }   // → ETABS pattern "FLOOR FINISH" (SDL)
        public double Filling { get; set; }   // → ETABS pattern "FILLING"      (SDL)
        public double ASDL { get; set; }   // → ETABS pattern "ASDL"         (SDL)
        public double LL { get; set; }   // → ETABS pattern "LL"
        public double LL3 { get; set; }   // → ETABS pattern "LL>3"
        public double FireTender { get; set; }   // → ETABS pattern "FIRE TENDER"
        public double TreeLoad { get; set; }   // → ETABS pattern "TREE LOAD"
        public double MachineRoom { get; set; }   // → ETABS pattern "MACHINE ROOM"
        public double WaterTank { get; set; }   // → ETABS pattern "WATER TANK"

        /// <summary>Quick constructor for the most common fields.</summary>
        public SlabLoads() { }
        public SlabLoads(double ff, double fill, double asdl, double ll,
            double ll3 = 0, double ft = 0, double tree = 0,
            double mach = 0, double wt = 0)
        {
            FF = ff; Filling = fill; ASDL = asdl; LL = ll;
            LL3 = ll3; FireTender = ft; TreeLoad = tree;
            MachineRoom = mach; WaterTank = wt;
        }

        public SlabLoads Clone() => new SlabLoads(FF, Filling, ASDL, LL,
            LL3, FireTender, TreeLoad, MachineRoom, WaterTank);
    }

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

        // ── Podium ────────────────────────────────────────────────────────
        public bool IsIndividualPodium { get; set; } = false;
        public int PodiumNumber { get; set; } = 0;

        // ── IS Code Edition ───────────────────────────────────────────────
        /// <summary>
        /// IS 1893 edition used for wall thickness table lookups.
        /// IS2016 = IS 1893:2016 (TDD/PKO)
        /// IS2025 = IS 1893:2025 (TDD/MSO)  ← default
        /// Set once globally from the UI and propagated to every FloorTypeConfig.
        /// </summary>
        public WallThicknessCalculator.ISCodeVersion ISCodeVersion { get; set; }
            = WallThicknessCalculator.ISCodeVersion.IS2025;

        // ── Layer mapping: layer name → "Beam" / "Wall" / "Slab" / "Column"
        public Dictionary<string, string> LayerMapping { get; set; }
            = new Dictionary<string, string>();

        // ── Beam depths (mm) ──────────────────────────────────────────────
        public Dictionary<string, int> BeamDepths { get; set; }
            = new Dictionary<string, int>();

        // ── Beam width overrides (mm, 0 = auto) ───────────────────────────
        public Dictionary<string, int> BeamWidthOverrides { get; set; }
            = new Dictionary<string, int>();

        // ── Beam wall load sets ────────────────────────────────────────────
        public Dictionary<string, string> BeamWallLoadSets { get; set; }
            = new Dictionary<string, string>();

        // ── Column dimensions ─────────────────────────────────────────────
        public int ColumnB { get; set; } = 300;
        public int ColumnD { get; set; } = 450;

        // ── Slab thicknesses (mm, YELLOW layers) ──────────────────────────
        public Dictionary<string, int> SlabThicknesses { get; set; }
            = new Dictionary<string, int>();

        // ── Slab individual loads (kN/m² per load pattern per layer) ────────
        public Dictionary<string, SlabLoads> SlabIndividualLoads { get; set; }
            = new Dictionary<string, SlabLoads>();

        // ── Wall thickness overrides (mm, 0 = use IS table) ───────────────
        public Dictionary<string, int> WallThicknessOverrides { get; set; }
            = new Dictionary<string, int>();

        // ── W-NTA: non-structural wall (always user-defined) ──────────────
        public int NtaWallThickness { get; set; } = 200;

        // ====================================================================
        // STATIC DEFAULT INDIVIDUAL LOAD TABLES (kN/m²)
        // constructor args: ff, fill, asdl, ll, ll3, fireTender, treeLoad, machineRoom, waterTank
        // Zero = not applicable (that ETABS pattern is skipped).
        // ====================================================================

        public static readonly Dictionary<string, SlabLoads> DefaultSlabIndividualLoads
            = new Dictionary<string, SlabLoads>
            {
                //                              ff     fill  asdl  ll    ll3   ft    tree mach  wt
                ["Amenities"] = new SlabLoads(1.55, 0, 1, 0, 5, 0, 0, 0, 0),
                ["Balcony"] = new SlabLoads(1.55, 1, 1, 3, 0, 0, 0, 0, 0),
                ["Chajja"] = new SlabLoads(1.2, 0, 1, 0.75, 0, 0, 0, 0, 0),
                ["ChajjaODU"] = new SlabLoads(1.2, 0, 1, 1, 0, 0, 0, 0, 0),
                ["Driveway"] = new SlabLoads(2.5, 0, 1, 2.5, 0, 0, 0, 0, 0),
                ["FireTender"] = new SlabLoads(6, 0, 1, 0, 4, 15, 0, 0, 0),
                ["FireWaterTank"] = new SlabLoads(0, 0, 1, 0, 0, 0, 0, 0, 30),
                ["GarbageRoom"] = new SlabLoads(2, 0, 1, 0, 5, 0, 0, 0, 0),
                ["GardenDining"] = new SlabLoads(3.6, 20, 1, 0, 5, 0, 2.5, 0, 0),
                ["Gymnasium"] = new SlabLoads(1.55, 0, 1, 0, 5, 0, 0, 0, 0),
                ["IndoorSports"] = new SlabLoads(1.55, 0, 1, 0, 5, 0, 0, 0, 0),
                ["Kitchen"] = new SlabLoads(1.55, 0, 1, 2, 0, 0, 0, 0, 0),
                ["KitchenSink"] = new SlabLoads(1.55, 1.5, 1, 2, 0, 0, 0, 0, 0),
                ["LMR"] = new SlabLoads(1.55, 0, 1, 0, 0, 0, 0, 10, 0),
                ["LMRTop"] = new SlabLoads(6, 0, 1, 2, 0, 0, 0, 0, 0),
                ["Lobby"] = new SlabLoads(1.55, 0, 1, 3, 0, 0, 0, 0, 0),
                ["MeterRoom"] = new SlabLoads(2, 0, 1, 0, 5, 0, 0, 0, 0),
                ["MultipurposeHall"] = new SlabLoads(1.55, 0, 1, 0, 5, 0, 0, 0, 0),
                ["OHT"] = new SlabLoads(0, 0, 1, 0, 0, 0, 0, 0, 30),
                ["OHTTop"] = new SlabLoads(6, 0, 1, 2, 0, 0, 0, 0, 0),
                ["Parking"] = new SlabLoads(2.5, 0, 1, 2.5, 0, 0, 0, 0, 0),
                ["ParkingToilet"] = new SlabLoads(1.55, 3, 1, 2, 0, 0, 0, 0, 0),
                ["PumpRoom"] = new SlabLoads(1.55, 0, 1, 0, 15, 0, 0, 0, 0),
                ["Refuge"] = new SlabLoads(3.6, 0, 1, 3, 0, 0, 0, 0, 0),
                ["Residential"] = new SlabLoads(1.55, 0, 1, 2, 0, 0, 0, 0, 0),
                ["Retail"] = new SlabLoads(5, 0, 1, 0, 4, 0, 0, 0, 0),
                ["RetailMazzanine"] = new SlabLoads(5, 0, 1, 0, 7.5, 0, 0, 0, 0),
                ["RetailToilet"] = new SlabLoads(1.55, 4.8, 1, 2, 0, 0, 0, 0, 0),
                ["ServiceSlab"] = new SlabLoads(1.25, 0, 1, 1, 0, 0, 0, 0, 0),
                ["SocietyRoom"] = new SlabLoads(1.55, 0, 1, 3, 0, 0, 0, 0, 0),
                ["StackParking"] = new SlabLoads(2.5, 0, 1, 0, 5, 0, 0, 0, 0),
                ["Staircase"] = new SlabLoads(4.7, 0, 1, 3, 0, 0, 0, 0, 0),
                ["Terrace"] = new SlabLoads(6.55, 0, 1, 3, 0, 0, 0, 0, 0),
                ["TerraceFire"] = new SlabLoads(0, 0, 1, 0, 0, 0, 0, 0, 30),
                ["TerracePumpRoom"] = new SlabLoads(6, 0, 1, 0, 5, 0, 0, 0, 0),
                ["Toilet"] = new SlabLoads(1.55, 1.8, 1, 2, 0, 0, 0, 0, 0),
                ["UGT"] = new SlabLoads(0, 0, 1, 0, 0, 0, 0, 0, 50),
                ["Landscape"] = new SlabLoads(0, 24, 1, 0, 4, 0, 0, 0, 0),
                ["Swimming"] = new SlabLoads(6, 17, 1, 0, 5, 0, 0, 0, 0),
                ["DG"] = new SlabLoads(1.55, 0, 1, 0, 20, 0, 0, 0, 0),
                ["STP"] = new SlabLoads(0, 0, 1, 0, 0, 0, 0, 0, 60),
                ["Utility"] = new SlabLoads(1.55, 0, 1, 2, 0, 0, 0, 0, 0),
            };

        // Alias used by SlabImporter
        public static Dictionary<string, SlabLoads> DefaultSlabLoads => DefaultSlabIndividualLoads;

        public static readonly Dictionary<string, string> DefaultBeamWallLoadSets
            = new Dictionary<string, string>
            {
                ["InternalGravity"] = "WALL LOAD",
                ["CantileverGravity"] = "WALL LOAD",
                ["NoLoadGravity"] = "",
                ["EdeckGravity"] = "WALL LOAD",
                ["PodiumGravity"] = "WALL LOAD",
                ["GroundGravity"] = "WALL LOAD",
                ["BasementGravity"] = "WALL LOAD",
                ["CoreMain"] = "WALL LOAD",
                ["PeripheralDeadMain"] = "WALL LOAD",
                ["PeripheralPortalMain"] = "WALL LOAD",
                ["InternalMain"] = "WALL LOAD",
            };

        // ====================================================================
        // HELPERS
        // ====================================================================

        public int GetBeamDepth(string key, int fallback = 450)
            => BeamDepths.TryGetValue(key, out int v) ? v : fallback;

        public int GetBeamWidthOverride(string key)
        {
            if (BeamWidthOverrides.TryGetValue(key, out int v)) return v;
            if (key.EndsWith("GravityWidth") &&
                BeamWidthOverrides.TryGetValue("GravityWidth", out int legacy))
                return legacy;
            return 0;
        }

        public string GetBeamWallLoadSet(string key)
        {
            if (BeamWallLoadSets.TryGetValue(key, out string v)) return v;
            if (DefaultBeamWallLoadSets.TryGetValue(key, out string def)) return def;
            return "WALL LOAD";
        }

        public int GetSlabThickness(string key, int fallback = 150)
            => SlabThicknesses.TryGetValue(key, out int v) ? v : fallback;

        public SlabLoads GetSlabIndividualLoads(string key)
        {
            if (SlabIndividualLoads.TryGetValue(key, out SlabLoads v)) return v;
            if (DefaultSlabIndividualLoads.TryGetValue(key, out SlabLoads def)) return def;
            return new SlabLoads(0, 0, 1, 2); // safe fallback: ASDL=1, LL=2
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
