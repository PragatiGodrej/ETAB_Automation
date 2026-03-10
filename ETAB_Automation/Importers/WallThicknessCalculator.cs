


// ============================================================================
// FILE: Core/WallThicknessCalculator.cs
// VERSION: 2.1 — Dual IS Code Support (IS 1893:2016 & IS 1893:2025)
//               All values verified against IS tables (images cross-checked)
// ============================================================================
// ZONE NAME CANONICAL STRINGS (must match UI exactly):
//   "Zone II (Bangalore, Hyderabad)"
//   "Zone III (MMR, Ahmedabad, Kolkata, Pune)"   ← IS 2016 only
//   "Zone III (MMR & Pune)"                       ← IS 2025 only
//   "Zone III"                                    ← accepted by both (fallthrough)
//   "Zone IV (Ahmedabad & Kolkata)"               ← IS 2025 only (no IS 2016 table)
//   "Zone IV (NCR)"
//   "Zone V"                                      ← uses Zone IV NCR per IS note
// ============================================================================

using ETABSv1;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ETAB_Automation.Core
{
    public class WallThicknessCalculator
    {
        // ====================================================================
        // ENUMS
        // ====================================================================

        public enum WallType
        {
            CoreWall,
            PeripheralDeadWall,
            PeripheralPortalWall,
            InternalWall
        }

        public enum ConstructionType
        {
            TypeI,
            TypeII
        }

        /// <summary>
        /// IS 1893 edition to use for thickness lookup.
        /// IS2016 = IS 1893:2016 (TDD/PKO)
        /// IS2025 = IS 1893:2025 (TDD/MSO)
        /// </summary>
        public enum ISCodeVersion
        {
            IS2016,
            IS2025
        }

        // ====================================================================
        // SECTION CACHE
        // ====================================================================

        private static Dictionary<string, double> availableWallSections =
            new Dictionary<string, double>();

        private static Dictionary<int, List<string>> wallSectionsByThickness =
            new Dictionary<int, List<string>>();

        // ====================================================================
        // SECTION LOADING
        // ====================================================================

        /// <summary>
        /// Load wall sections from ETABS model.
        /// Parses names like W16M30, W20M40, W47.5M40
        /// Format: W[thickness_cm]M[grade]
        /// </summary>
        public static void LoadAvailableWallSections(cSapModel sapModel)
        {
            try
            {
                availableWallSections.Clear();
                wallSectionsByThickness.Clear();

                int numSections = 0;
                string[] sectionNames = null;

                int ret = sapModel.PropArea.GetNameList(ref numSections, ref sectionNames);

                if (ret == 0 && sectionNames != null)
                {
                    Regex wallPattern = new Regex(
                        @"^W(\d+(?:\.\d+)?)M(\d+)", RegexOptions.IgnoreCase);

                    foreach (string sectionName in sectionNames)
                    {
                        Match match = wallPattern.Match(sectionName);
                        if (!match.Success) continue;

                        double thicknessCm = double.Parse(match.Groups[1].Value);
                        int thicknessMm = (int)Math.Round(thicknessCm * 10);

                        eWallPropType wallType = eWallPropType.Specified;
                        eShellType shellType = eShellType.ShellThin;
                        string matProp = "";
                        double thicknessMeters = 0;
                        int color = 0;
                        string notes = "", guid = "";

                        ret = sapModel.PropArea.GetWall(
                            sectionName, ref wallType, ref shellType,
                            ref matProp, ref thicknessMeters,
                            ref color, ref notes, ref guid);

                        if (ret == 0 && thicknessMeters > 0)
                        {
                            availableWallSections[sectionName] = thicknessMeters;

                            if (!wallSectionsByThickness.ContainsKey(thicknessMm))
                                wallSectionsByThickness[thicknessMm] = new List<string>();
                            wallSectionsByThickness[thicknessMm].Add(sectionName);

                            System.Diagnostics.Debug.WriteLine(
                                $"Loaded: {sectionName} = {thicknessMm}mm M{match.Groups[2].Value}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"\n✓ Loaded {availableWallSections.Count} wall sections from template");
                System.Diagnostics.Debug.WriteLine(
                    $"✓ Available thicknesses: {string.Join(", ", GetAvailableThicknesses())}mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading wall sections: {ex.Message}");
                throw;
            }
        }

        private static List<int> GetAvailableThicknesses()
        {
            var list = new List<int>(wallSectionsByThickness.Keys);
            list.Sort();
            return list;
        }

        // ====================================================================
        // SECTION SELECTION
        // ====================================================================

        private static string GetClosestWallSection(
            int requiredThicknessMm, string preferredGrade = null)
        {
            if (availableWallSections.Count == 0)
                throw new InvalidOperationException(
                    "No wall sections loaded. Call LoadAvailableWallSections first.");

            // 1. Exact thickness + preferred grade
            if (!string.IsNullOrEmpty(preferredGrade) &&
                wallSectionsByThickness.ContainsKey(requiredThicknessMm))
            {
                foreach (string section in wallSectionsByThickness[requiredThicknessMm])
                    if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
                        return section;
            }

            // 2. Exact thickness, any grade
            if (wallSectionsByThickness.ContainsKey(requiredThicknessMm))
                return wallSectionsByThickness[requiredThicknessMm][0];

            // 3. Closest thickness
            string closestSection = null;
            int minDiff = int.MaxValue;

            foreach (var kvp in wallSectionsByThickness)
            {
                int diff = Math.Abs(kvp.Key - requiredThicknessMm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestSection = null;

                    if (!string.IsNullOrEmpty(preferredGrade))
                        foreach (string section in kvp.Value)
                            if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
                            { closestSection = section; break; }

                    if (closestSection == null)
                        closestSection = kvp.Value[0];
                }
            }

            if (closestSection == null)
                throw new InvalidOperationException(
                    $"No suitable wall section found for {requiredThicknessMm}mm thickness");

            return closestSection;
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Get recommended wall section name from ETABS template.
        /// </summary>
        public static string GetRecommendedWallSection(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            double wallLength = 2.0,
            bool isFloatingWall = false,
            ConstructionType constructionType = ConstructionType.TypeII,
            string preferredGrade = null,
            ISCodeVersion isCode = ISCodeVersion.IS2025)
        {
            int requiredThickness = GetRecommendedThickness(
                numTypicalFloors, wallType, seismicZone,
                wallLength, isFloatingWall, constructionType, isCode);

            string sectionName = GetClosestWallSection(requiredThickness, preferredGrade);
            double actualMm = availableWallSections[sectionName] * 1000;

            System.Diagnostics.Debug.WriteLine(
                $"  [{isCode}] Required: {requiredThickness}mm → Using: {sectionName} ({actualMm:F0}mm)");

            return sectionName;
        }

        /// <summary>
        /// Get recommended wall thickness (mm).
        /// Full overload — dispatches to IS 2016 or IS 2025 tables.
        /// </summary>
        public static int GetRecommendedThickness(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            double wallLength = 2.0,
            bool isFloatingWall = false,
            ConstructionType constructionType = ConstructionType.TypeII,
            ISCodeVersion isCode = ISCodeVersion.IS2025)
        {
            if (numTypicalFloors < 1 || numTypicalFloors > 50)
                throw new ArgumentException("Number of floors must be between 1 and 50");

            bool isShortWall = wallLength < 1.8;

            return isCode == ISCodeVersion.IS2016
                ? GetThickness2016(numTypicalFloors, wallType, seismicZone,
                    isShortWall, isFloatingWall, constructionType)
                : GetThickness2025(numTypicalFloors, wallType, seismicZone,
                    isShortWall, isFloatingWall);
        }

        /// <summary>
        /// Convenience overload — no ConstructionType or wall-length needed.
        /// </summary>
        public static int GetRecommendedThickness(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            ISCodeVersion isCode = ISCodeVersion.IS2025)
            => GetRecommendedThickness(numTypicalFloors, wallType, seismicZone,
               2.0, false, ConstructionType.TypeII, isCode);

        public static string GetWallSectionByThicknessAndGrade(int thicknessMm, string grade)
        {
            if (wallSectionsByThickness.ContainsKey(thicknessMm))
            {
                foreach (string section in wallSectionsByThickness[thicknessMm])
                    if (section.ToUpperInvariant().Contains(grade.ToUpperInvariant()))
                        return section;
                return wallSectionsByThickness[thicknessMm][0];
            }
            return null;
        }

        public static WallType ClassifyWallFromLayerName(string layerName)
        {
            string upper = layerName.ToUpperInvariant();

            if (upper.Contains("CORE") || upper.Contains("LIFT") ||
                upper.Contains("ELEVATOR") || upper.Contains("SHAFT") ||
                upper.Contains("STAIRCASE") || upper.Contains("STAIR"))
                return WallType.CoreWall;

            if (upper.Contains("PORTAL") || upper.Contains("FRAME"))
                return WallType.PeripheralPortalWall;

            if (upper.Contains("PERIPHERAL") || upper.Contains("EXTERNAL") ||
                upper.Contains("EXTERIOR") || upper.Contains("OUTER") ||
                upper.Contains("BOUNDARY") || upper.Contains("PERIMETER") ||
                upper.Contains("FACADE"))
                return WallType.PeripheralDeadWall;

            return WallType.InternalWall;
        }

        public static string GetDesignNotes(
            int numTypicalFloors, string seismicZone,
            ISCodeVersion isCode = ISCodeVersion.IS2025)
        {
            string edition = isCode == ISCodeVersion.IS2016
                ? "IS 1893:2016 (TDD/PKO)"
                : "IS 1893:2025 (TDD/MSO)";

            string notes = $"=== WALL THICKNESS — {edition} ===\n\n";

            foreach (int t in GetAvailableThicknesses())
                notes += $"  {t}mm ({t / 10.0:F1}cm): {string.Join(", ", wallSectionsByThickness[t])}\n";

            if (numTypicalFloors > 50)
                notes += "\n⚠️ WARNING: Building exceeds 50 floors — manual review required!\n";

            return notes;
        }

        // ====================================================================
        // IS 1893 : 2025  (TDD / MSO)
        // Zones (UI string → method):
        //   "Zone II (Bangalore, Hyderabad)"      → GetZone2_2025
        //   "Zone III" / "Zone III (MMR & Pune)"  → GetZone3_2025
        //   "Zone IV (Ahmedabad & Kolkata)"        → GetZone4AK_2025
        //   "Zone IV (NCR)"                        → GetZone4NCR_2025
        //   "Zone V"                               → GetZone4NCR_2025  (IS note: same as Zone IV NCR)
        // ====================================================================

        private static int GetThickness2025(
            int floors, WallType wallType, string seismicZone,
            bool isShortWall, bool isFloatingWall)
        {
            switch (seismicZone)
            {
                case "Zone II (Bangalore, Hyderabad)":
                    return GetZone2_2025(floors, wallType, isShortWall, isFloatingWall);

                case "Zone III":
                case "Zone III (MMR & Pune)":
                case "Zone III (MMR, Pune)":
                    return GetZone3_2025(floors, wallType, isShortWall);

                case "Zone IV (Ahmedabad & Kolkata)":
                    return GetZone4AK_2025(floors, wallType, isShortWall);

                case "Zone IV (NCR)":
                case "Zone V":
                case "Zone VI":
                    // IS 2025 note: Zone V & VI use same as Zone IV NCR
                    return GetZone4NCR_2025(floors, wallType, isShortWall);

                default:
                    throw new ArgumentException(
                        $"IS 1893:2025 — Unrecognised seismic zone: '{seismicZone}'. " +
                        $"Valid: Zone II (Bangalore, Hyderabad) | Zone III (MMR & Pune) | " +
                        $"Zone IV (Ahmedabad & Kolkata) | Zone IV (NCR) | Zone V");
            }
        }

        // ── IS 2025 · Zone II (Bangalore, Hyderabad) ─────────────────────
        // Table source: IS 1893:2025 image — verified cell by cell
        // * = partial floating shear walls in unit area → isFloatingWall
        // # = wall length < 1.8 m                      → isShortWall
        //
        // Floors  Core       PeriphDead   PeriphPortal  Internal
        // <20     160/200*   160/200*     200           160/200#
        // 21-25   200/250*   200/250*     200           160/250#
        // 26-30   200/250*   200/250*     200           200/275#
        // 31-35   225/300*   225/300*     225           200/325#
        // 35-40   250/350*   250/300*     250           225/325#
        // 40-45   300/350*   300/350*     300           250/350#
        // 45-50   325/400*   325/400*     350           275/400#
        private static int GetZone2_2025(int f, WallType w, bool sh, bool fl)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 300 : 225;
                    if (f <= 40) return fl ? 350 : 250;
                    if (f <= 45) return fl ? 350 : 300;
                    return fl ? 400 : 325;          // 45-50

                case WallType.PeripheralDeadWall:
                    // PeriphDead uses * (floating) flag, NOT short-wall flag
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 300 : 225;
                    if (f <= 40) return fl ? 300 : 250;  // 250/300*
                    if (f <= 45) return fl ? 350 : 300;  // 300/350*
                    return fl ? 400 : 325;               // 325/400*

                case WallType.PeripheralPortalWall:
                    // No * or # for Portal in 2025 Zone II — single value
                    if (f <= 20) return 200;
                    if (f <= 25) return 200;
                    if (f <= 30) return 200;
                    if (f <= 35) return 225;
                    if (f <= 40) return 250;
                    if (f <= 45) return 300;
                    return 350;                     // 45-50

                case WallType.InternalWall:
                    // Internal uses # (short wall) flag
                    if (f <= 20) return sh ? 200 : 160;
                    if (f <= 25) return sh ? 250 : 160;
                    if (f <= 30) return sh ? 275 : 200;
                    if (f <= 35) return sh ? 325 : 200;
                    if (f <= 40) return sh ? 325 : 225;
                    if (f <= 45) return sh ? 350 : 250;
                    return sh ? 400 : 275;          // 45-50

                default: return 160;
            }
        }

        // ── IS 2025 · Zone III (MMR & Pune) ──────────────────────────────
        // Table source: IS 1893:2025 image — verified cell by cell
        // # = wall length < 1.8 m (Internal only)
        //
        // Floors  Core  PeriphDead  PeriphPortal  Internal
        // <20     200   200         300           200/300#
        // 21-25   300   250         350           200/300#
        // 26-30   375   300         400           200/300#
        // 31-35   400   325         400           225/350#
        // 35-40   425   350         425           250/400#
        // 40-45   450   375         425           275/450#
        // 45-50   450   400         450           300/500#
        private static int GetZone3_2025(int f, WallType w, bool sh)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 300;
                    if (f <= 30) return 375;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 450;                     // 45-50

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 250;
                    if (f <= 30) return 300;
                    if (f <= 35) return 325;
                    if (f <= 40) return 350;
                    if (f <= 45) return 375;
                    return 400;                     // 45-50

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 425;
                    return 450;                     // 45-50

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 200;
                    if (f <= 30) return sh ? 300 : 200;
                    if (f <= 35) return sh ? 350 : 225;
                    if (f <= 40) return sh ? 400 : 250;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;          // 45-50

                default: return 200;
            }
        }

        // ── IS 2025 · Zone IV (Ahmedabad & Kolkata) ───────────────────────
        // Table source: IS 1893:2025 image — verified cell by cell
        // # = wall length < 1.8 m (Internal only)
        //
        // Floors  Core  PeriphDead  PeriphPortal  Internal
        // <20     300   200         300           200/300#
        // 21-25   350   250         350           225/300#
        // 26-30   375   275         400           250/350#
        // 31-35   400   300         400           275/400#
        // 35-40   425   325         425           300/450#
        // 40-45   450   375         450           300/475#
        // 45-50   475   400         500           325/500#
        private static int GetZone4AK_2025(int f, WallType w, bool sh)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 375;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 475;                     // 45-50

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 250;
                    if (f <= 30) return 275;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 375;
                    return 400;                     // 45-50

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 500;                     // 45-50

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 225;
                    if (f <= 30) return sh ? 350 : 250;
                    if (f <= 35) return sh ? 400 : 275;
                    if (f <= 40) return sh ? 450 : 300;
                    if (f <= 45) return sh ? 475 : 300;
                    return sh ? 500 : 325;          // 45-50

                default: return 200;
            }
        }

        // ── IS 2025 · Zone IV NCR / Zone V / Zone VI ─────────────────────
        // Table source: IS 1893:2025 image — verified cell by cell
        // IS note: Zone V, VI → use same as Zone IV (NCR)
        // # = wall length < 1.8 m (Internal only)
        //
        // Floors  Core  PeriphDead  PeriphPortal  Internal
        // <20     325   240         300           240/300#
        // 21-25   375   240         350           240/300#
        // 26-30   400   300         400           250/350#
        // 31-35   425   325         400           275/400#
        // 35-40   450   350         425           300/450#
        // 40-45   475   400         450           300/475#
        // 45-50   500   425         500           350/500#
        private static int GetZone4NCR_2025(int f, WallType w, bool sh)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return 325;
                    if (f <= 25) return 375;
                    if (f <= 30) return 400;
                    if (f <= 35) return 425;
                    if (f <= 40) return 450;
                    if (f <= 45) return 475;
                    return 500;                     // 45-50

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 240;
                    if (f <= 25) return 240;
                    if (f <= 30) return 300;
                    if (f <= 35) return 325;
                    if (f <= 40) return 350;
                    if (f <= 45) return 400;
                    return 425;                     // 45-50

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 500;                     // 45-50

                case WallType.InternalWall:
                    // Min 240mm per fire rating (IS note)
                    if (f <= 20) return sh ? 300 : 240;
                    if (f <= 25) return sh ? 300 : 240;
                    if (f <= 30) return sh ? 350 : 250;
                    if (f <= 35) return sh ? 400 : 275;
                    if (f <= 40) return sh ? 450 : 300;
                    if (f <= 45) return sh ? 475 : 300;
                    return sh ? 500 : 350;          // 45-50

                default: return 240;
            }
        }

        // ====================================================================
        // IS 1893 : 2016  (TDD / PKO)
        // Zones (UI string → method):
        //   "Zone II (Bangalore, Hyderabad)"               → GetZone2_2016
        //   "Zone III" / "Zone III (MMR, Ahmedabad, ...)"  → GetZone3_2016
        //   "Zone IV (Ahmedabad & Kolkata)"                → GetZone3_2016
        //      (No IS 2016 separate table for Ahm/Kol; they are listed under Zone III)
        //   "Zone IV (NCR)"                                → GetZone4NCR_2016
        //   "Zone V"                                       → GetZone4NCR_2016 (IS note: same)
        //
        // Legend:
        //   * = partial floating shear walls in unit area → isFloatingWall
        //   # = wall length < 1.8 m                      → isShortWall
        // ====================================================================

        private static int GetThickness2016(
           int floors, WallType wallType, string seismicZone,
           bool isShortWall, bool isFloatingWall, ConstructionType constructionType)
        {
            switch (seismicZone)
            {
                case "Zone II (Bangalore, Hyderabad)":
                    return GetZone2_2016(floors, wallType, isShortWall, isFloatingWall);

                case "Zone III":
                case "Zone III (MMR & Pune)":
                case "Zone III (MMR, Pune)":
                case "Zone III (MMR, Ahmedabad, Kolkata, Pune)":
                    return GetZone3_2016(floors, wallType, isShortWall);

                // IS 2016 lists Ahmedabad & Kolkata under Zone III.
                // The UI also shows this as "Zone IV (Ahmedabad & Kolkata)" for IS 2025.
                // When IS 2016 is selected with this zone string, use Zone III table.
                case "Zone IV (Ahmedabad & Kolkata)":
                    return GetZone3_2016(floors, wallType, isShortWall);

                case "Zone IV (NCR)":
                case "Zone V":
                case "Zone VI":
                    // IS 2016 note: Zone V → use same as Zone IV (NCR)
                    return GetZone4NCR_2016(floors, wallType, isShortWall);

                default:
                    throw new ArgumentException(
                        $"IS 1893:2016 — Unrecognised seismic zone: '{seismicZone}'. " +
                        $"Valid: Zone II (Bangalore, Hyderabad) | " +
                        $"Zone III (MMR, Ahmedabad, Kolkata, Pune) | " +
                        $"Zone IV (NCR) | Zone V");
            }
        }

        // ── IS 2016 · Zone II (Bangalore, Hyderabad) ─────────────────────
        // Table source: IS 1893:2016 image — verified cell by cell
        // * = floating walls, # = short wall (<1.8m)
        //
        // Floors  Core       PeriphDead   PeriphPortal  Internal
        // <20     160/200*   160/200*     200           160/200#
        // 21-25   200/250*   200/250*     200           160/200#
        // 26-30   200/250*   200/250*     200           160/250#
        // 31-35   200/300*   200/250*     200           200/300#
        // 35-40   200/300*   200/250*     200           200/300#
        // 40-45   200/325*   250/300*     250           225/325#
        // 45-50   300/350*   300/350*     300           250/350#
        private static int GetZone2_2016(int f, WallType w, bool sh, bool fl)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 300 : 200;
                    if (f <= 40) return fl ? 300 : 200;
                    if (f <= 45) return fl ? 325 : 200;
                    return fl ? 350 : 300;          // 45-50

                case WallType.PeripheralDeadWall:
                    // PeriphDead uses * (floating) flag per IS 2016 table
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 250 : 200;
                    if (f <= 40) return fl ? 250 : 200;
                    if (f <= 45) return fl ? 300 : 250;
                    return fl ? 350 : 300;          // 45-50

                case WallType.PeripheralPortalWall:
                    // No * or # for Portal in 2016 Zone II — single value
                    if (f <= 40) return 200;
                    if (f <= 45) return 250;
                    return 300;                     // 45-50

                case WallType.InternalWall:
                    // Internal uses # (short wall) flag
                    if (f <= 20) return sh ? 200 : 160;
                    if (f <= 25) return sh ? 200 : 160;
                    if (f <= 30) return sh ? 250 : 160;
                    if (f <= 35) return sh ? 300 : 200;
                    if (f <= 40) return sh ? 300 : 200;
                    if (f <= 45) return sh ? 325 : 225;
                    return sh ? 350 : 250;          // 45-50

                default: return 160;
            }
        }

        // ── IS 2016 · Zone III (MMR, Ahmedabad, Kolkata, Pune) ────────────
        // Table source: IS 1893:2016 image — verified cell by cell
        // # = wall length < 1.8 m (Internal only)
        //
        // Floors  Core  PeriphDead  PeriphPortal  Internal
        // <20     200   200         300           200/300#
        // 21-25   300   200         350           200/300#
        // 26-30   350   250         400           200/300#
        // 31-35   375   300         400           225/350#
        // 35-40   400   325         400           250/400#
        // 40-45   425   350         400           275/450#
        // 45-50   450   400         450           300/500#
        private static int GetZone3_2016(int f, WallType w, bool sh)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 300;
                    if (f <= 30) return 350;
                    if (f <= 35) return 375;
                    if (f <= 40) return 400;
                    if (f <= 45) return 425;
                    return 450;                     // 45-50

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 200;
                    if (f <= 30) return 250;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 350;
                    return 400;                     // 45-50

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 400;
                    if (f <= 45) return 400;
                    return 450;                     // 45-50

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 200;
                    if (f <= 30) return sh ? 300 : 200;
                    if (f <= 35) return sh ? 350 : 225;
                    if (f <= 40) return sh ? 400 : 250;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;          // 45-50

                default: return 200;
            }
        }

        // ── IS 2016 · Zone IV NCR (Type-I construction) / Zone V ─────────
        // Table source: IS 1893:2016 image — verified cell by cell
        // IS note: Zone V → use same as Zone IV (NCR)
        // # = wall length < 1.8 m (Internal only)
        //
        // Floors  Core  PeriphDead  PeriphPortal  Internal
        // <20     300   240         300           240/300#
        // 21-25   350   240         350           240/300#
        // 26-30   375   275         400           240/300#
        // 31-35   400   300         400           240/350#
        // 35-40   425   325         400           240/400#
        // 40-45   450   350         400           275/450#
        // 45-50   500   400         450           300/500#
        private static int GetZone4NCR_2016(int f, WallType w, bool sh)
        {
            switch (w)
            {
                case WallType.CoreWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 375;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 500;                     // 45-50

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 240;
                    if (f <= 25) return 240;
                    if (f <= 30) return 275;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 350;
                    return 400;                     // 45-50

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 400;
                    if (f <= 45) return 400;
                    return 450;                     // 45-50

                case WallType.InternalWall:
                    // Min 240mm per fire rating
                    if (f <= 20) return sh ? 300 : 240;
                    if (f <= 25) return sh ? 300 : 240;
                    if (f <= 30) return sh ? 300 : 240;
                    if (f <= 35) return sh ? 350 : 240;
                    if (f <= 40) return sh ? 400 : 240;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;          // 45-50

                default: return 240;
            }
        }
    }
}
