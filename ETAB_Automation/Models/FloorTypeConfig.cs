
// ============================================================================
// FILE: Models/FloorTypeConfig.cs
// VERSION: 2.3 — Added WallThicknessOverrides, BeamWidthOverrides, NtaWallThickness
// (No changes in this version — included for completeness)
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
        public int GetBeamDepth(string key, int fallback = 450)
            => BeamDepths.TryGetValue(key, out int v) ? v : fallback;

        public int GetBeamWidthOverride(string key)
            => BeamWidthOverrides.TryGetValue(key, out int v) ? v : 0;

        public int GetSlabThickness(string key, int fallback = 150)
            => SlabThicknesses.TryGetValue(key, out int v) ? v : fallback;

        public int GetWallThicknessOverride(string key)
            => WallThicknessOverrides.TryGetValue(key, out int v) ? v : 0;
    }
}
