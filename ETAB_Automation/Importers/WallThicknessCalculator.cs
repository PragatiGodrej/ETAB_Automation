
//// ============================================================================
//// FILE: Core/WallThicknessCalculator.cs (FIXED FOR W16M30 FORMAT)
//// ============================================================================
//using ETABSv1;
//using System;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;
//using System.Xml.Linq;

//namespace ETAB_Automation.Core
//{
//    /// <summary>
//    /// Calculates wall thickness based on TDD/PKO design standards
//    /// Reads existing wall sections from ETABS template (e.g., W16M30 = 160mm, M30)
//    /// </summary>
//    public class WallThicknessCalculator
//    {
//        public enum WallType
//        {
//            CoreWall,
//            PeripheralDeadWall,
//            PeripheralPortalWall,
//            InternalWall
//        }

//        public enum ConstructionType
//        {
//            TypeI,
//            TypeII
//        }

//        // Store available wall sections: Key = section name, Value = thickness in meters
//        private static Dictionary<string, double> availableWallSections = new Dictionary<string, double>();

//        // Store by thickness for quick lookup: Key = thickness in mm, Value = list of section names
//        private static Dictionary<int, List<string>> wallSectionsByThickness = new Dictionary<int, List<string>>();

//        /// <summary>
//        /// Load available wall sections from ETABS model
//        /// Parses section names like W16M30, W20M40, W47.5M40, etc.
//        /// Format: W[thickness_cm]M[grade]
//        /// Example: W16M30 = 16cm = 160mm thick, M30 grade
//        /// </summary>
//        public static void LoadAvailableWallSections(cSapModel sapModel)
//        {
//            try
//            {
//                availableWallSections.Clear();
//                wallSectionsByThickness.Clear();

//                int numSections = 0;
//                string[] sectionNames = null;

//                int ret = sapModel.PropArea.GetNameList(ref numSections, ref sectionNames);

//                if (ret == 0 && sectionNames != null)
//                {
//                    // Regex to parse wall section names: W16M30, W20M40, W47.5M40, etc.
//                    // Pattern: W followed by thickness in CM (decimal allowed), then M and grade
//                    Regex wallPattern = new Regex(@"^W(\d+(?:\.\d+)?)M(\d+)", RegexOptions.IgnoreCase);

//                    foreach (string sectionName in sectionNames)
//                    {
//                        Match match = wallPattern.Match(sectionName);

//                        if (match.Success)
//                        {
//                            // Extract thickness in centimeters and convert to millimeters
//                            double thicknessCm = double.Parse(match.Groups[1].Value);
//                            int thicknessMm = (int)Math.Round(thicknessCm * 10);

//                            // Extract concrete grade
//                            string grade = match.Groups[2].Value;

//                            // Get actual thickness from ETABS (this is in meters)
//                            eWallPropType wallType = eWallPropType.Specified;
//                            eShellType shellType = eShellType.ShellThin;
//                            string matProp = "";
//                            double thicknessMeters = 0;
//                            int color = 0;
//                            string notes = "";
//                            string guid = "";

//                            ret = sapModel.PropArea.GetWall(
//                                sectionName,
//                                ref wallType,
//                                ref shellType,
//                                ref matProp,
//                                ref thicknessMeters,
//                                ref color,
//                                ref notes,
//                                ref guid);

//                            if (ret == 0 && thicknessMeters > 0)
//                            {
//                                availableWallSections[sectionName] = thicknessMeters;

//                                // Store by thickness for quick lookup
//                                if (!wallSectionsByThickness.ContainsKey(thicknessMm))
//                                {
//                                    wallSectionsByThickness[thicknessMm] = new List<string>();
//                                }
//                                wallSectionsByThickness[thicknessMm].Add(sectionName);

//                                System.Diagnostics.Debug.WriteLine(
//                                    $"Loaded: {sectionName} = {thicknessMm}mm ({thicknessCm}cm) M{grade} concrete");
//                            }
//                        }
//                    }
//                }

//                System.Diagnostics.Debug.WriteLine(
//                    $"\n✓ Loaded {availableWallSections.Count} wall sections from template");
//                System.Diagnostics.Debug.WriteLine(
//                    $"✓ Available thicknesses: {string.Join(", ", GetAvailableThicknesses())}mm");
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Error loading wall sections: {ex.Message}");
//                throw;
//            }
//        }

//        /// <summary>
//        /// Get list of available thicknesses in mm
//        /// </summary>
//        private static List<int> GetAvailableThicknesses()
//        {
//            List<int> thicknesses = new List<int>(wallSectionsByThickness.Keys);
//            thicknesses.Sort();
//            return thicknesses;
//        }

//        /// <summary>
//        /// Get the closest matching wall section from template
//        /// </summary>
//        /// <param name="requiredThicknessMm">Required thickness in millimeters</param>
//        /// <param name="preferredGrade">Preferred concrete grade (e.g., "M40"), null for any</param>
//        /// <returns>Section name from template (e.g., W16M30 for 160mm)</returns>
//        private static string GetClosestWallSection(int requiredThicknessMm, string preferredGrade = null)
//        {
//            if (availableWallSections.Count == 0)
//            {
//                throw new InvalidOperationException(
//                    "No wall sections loaded. Call LoadAvailableWallSections first.");
//            }

//            // First try: exact match with preferred grade
//            if (!string.IsNullOrEmpty(preferredGrade) && wallSectionsByThickness.ContainsKey(requiredThicknessMm))
//            {
//                foreach (string section in wallSectionsByThickness[requiredThicknessMm])
//                {
//                    if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
//                    {
//                        return section;
//                    }
//                }
//            }

//            // Second try: exact thickness match (any grade)
//            if (wallSectionsByThickness.ContainsKey(requiredThicknessMm))
//            {
//                // Return first available section with this thickness
//                return wallSectionsByThickness[requiredThicknessMm][0];
//            }

//            // Third try: find closest thickness
//            string closestSection = null;
//            int minDifference = int.MaxValue;

//            foreach (var kvp in wallSectionsByThickness)
//            {
//                int thicknessMm = kvp.Key;
//                int difference = Math.Abs(thicknessMm - requiredThicknessMm);

//                if (difference < minDifference)
//                {
//                    minDifference = difference;

//                    // Try to find preferred grade in this thickness
//                    if (!string.IsNullOrEmpty(preferredGrade))
//                    {
//                        foreach (string section in kvp.Value)
//                        {
//                            if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
//                            {
//                                closestSection = section;
//                                break;
//                            }
//                        }
//                    }

//                    // If no preferred grade found, take first available
//                    if (closestSection == null)
//                    {
//                        closestSection = kvp.Value[0];
//                    }
//                }
//            }

//            if (closestSection == null)
//            {
//                throw new InvalidOperationException(
//                    $"No suitable wall section found for {requiredThicknessMm}mm thickness");
//            }

//            return closestSection;
//        }

//        /// <summary>
//        /// Calculate recommended wall thickness and return matching section name
//        /// </summary>
//        public static string GetRecommendedWallSection(
//            int numTypicalFloors,
//            WallType wallType,
//            string seismicZone,
//            double wallLength = 2.0,
//            bool isFloatingWall = false,
//            ConstructionType constructionType = ConstructionType.TypeII,
//            string preferredGrade = null)
//        {
//            // Get required thickness from design standards
//            int requiredThickness = GetRecommendedThickness(
//                numTypicalFloors,
//                wallType,
//                seismicZone,
//                wallLength,
//                isFloatingWall,
//                constructionType);

//            // Find closest matching section from template
//            string sectionName = GetClosestWallSection(requiredThickness, preferredGrade);

//            double actualThicknessMm = availableWallSections[sectionName] * 1000;

//            System.Diagnostics.Debug.WriteLine(
//                $"  Required: {requiredThickness}mm → Using: {sectionName} ({actualThicknessMm:F0}mm)");

//            return sectionName;
//        }

//        /// <summary>
//        /// Get wall section for specific thickness and grade
//        /// </summary>
//        public static string GetWallSectionByThicknessAndGrade(int thicknessMm, string grade)
//        {
//            if (wallSectionsByThickness.ContainsKey(thicknessMm))
//            {
//                foreach (string section in wallSectionsByThickness[thicknessMm])
//                {
//                    if (section.ToUpperInvariant().Contains(grade.ToUpperInvariant()))
//                    {
//                        return section;
//                    }
//                }

//                // If exact grade not found, return first available with this thickness
//                return wallSectionsByThickness[thicknessMm][0];
//            }

//            return null;
//        }

//        /// <summary>
//        /// Calculate recommended wall thickness (in mm) based on design standards
//        /// </summary>



//        public static int GetRecommendedThickness(
//            int numTypicalFloors,
//            WallType wallType,
//            string seismicZone,
//            double wallLength = 2.0,
//            bool isFloatingWall = false,
//            ConstructionType constructionType = ConstructionType.TypeII)
//        {
//            if (numTypicalFloors < 1 || numTypicalFloors > 50)
//                throw new ArgumentException("Number of floors must be between 1 and 50");

//            bool isShortWall = wallLength < 1.8;

//            switch (seismicZone)
//            {
//                case "Zone II (Bangalore, Hyderabad)":
//                    return GetZone2Thickness(numTypicalFloors, wallType, isShortWall, isFloatingWall);
//                case "Zone III":
//                case "Zone III (MMR, Pune)":
//                    return GetZone3Thickness(numTypicalFloors, wallType, isShortWall);
//                case "Zone IV":
//                case "Zone IV (Ahmedabad & Kolkata)":
//                    return GetZone4Thickness(numTypicalFloors, wallType, isShortWall);
//                case "Zone IV (NCR)":
//                case "Zone V":
//                    return GetZone4NCRThickness(numTypicalFloors, wallType, isShortWall);
//                default:
//                    throw new ArgumentException($"Invalid seismic zone: {seismicZone}");
//            }
//        }

//        // ====================================================================
//        // ZONE II — Bangalore, Hyderabad  (IS 1893-2025)
//        // Legend:
//        //   * = partial floating shear walls in unit area
//        //   # = wall length < 1.8m
//        //   Values shown as "normal / special*" or "normal / short#"
//        // ====================================================================
//        private static int GetZone2Thickness(
//            int floors, WallType wallType, bool isShortWall, bool isFloatingWall)
//        {
//            switch (wallType)
//            {
//                case WallType.CoreWall:
//                    // < 20: 160 / 200*
//                    // 21-25: 200 / 250*
//                    // 26-30: 200 / 250*
//                    // 31-35: 225 / 300*
//                    // 36-40: 250 / 350*
//                    // 41-45: 300 / 350*
//                    // 46-50: 325 / 400*
//                    if (floors <= 20) return isFloatingWall ? 200 : 160;
//                    if (floors <= 25) return isFloatingWall ? 250 : 200;
//                    if (floors <= 30) return isFloatingWall ? 250 : 200;
//                    if (floors <= 35) return isFloatingWall ? 300 : 225;
//                    if (floors <= 40) return isFloatingWall ? 350 : 250;
//                    if (floors <= 45) return isFloatingWall ? 350 : 300;
//                    return isFloatingWall ? 400 : 325;

//                case WallType.PeripheralDeadWall:
//                    // < 20: 160 / 200*
//                    // 21-25: 160 / 200#  (# = short wall)
//                    // 26-30: 160 / 250#
//                    // 31-35: 200 / 275#
//                    // 36-40: 200 / 325#
//                    // 41-45: 225 / 325#
//                    // 46-50: 275 / 400#
//                    if (floors <= 20) return isFloatingWall ? 200 : 160;
//                    if (floors <= 25) return isShortWall ? 200 : 160;
//                    if (floors <= 30) return isShortWall ? 250 : 160;
//                    if (floors <= 35) return isShortWall ? 275 : 200;
//                    if (floors <= 40) return isShortWall ? 325 : 200;
//                    if (floors <= 45) return isShortWall ? 325 : 225;
//                    return isShortWall ? 400 : 275;

//                case WallType.PeripheralPortalWall:
//                    // < 20: 200 / 250*
//                    // 21-25: 200 / 250*
//                    // 26-30: 200 / 250*
//                    // 31-35: 225 / 300*
//                    // 36-40: 250 / 300*
//                    // 41-45: 300 / 350*
//                    // 46-50: 325 / 400*
//                    if (floors <= 20) return isFloatingWall ? 250 : 200;
//                    if (floors <= 25) return isFloatingWall ? 250 : 200;
//                    if (floors <= 30) return isFloatingWall ? 250 : 200;
//                    if (floors <= 35) return isFloatingWall ? 300 : 225;
//                    if (floors <= 40) return isFloatingWall ? 300 : 250;
//                    if (floors <= 45) return isFloatingWall ? 350 : 300;
//                    return isFloatingWall ? 400 : 325;

//                case WallType.InternalWall:
//                    // < 20: 200
//                    // 21-25: 200
//                    // 26-30: 225 / 300*
//                    // 31-35: 225 / 300*
//                    // 36-40: 250 / 350*
//                    // 41-45: 300 / 350*
//                    // 46-50: 350 / 400*  (using floating flag for * case)
//                    if (floors <= 20) return 200;
//                    if (floors <= 25) return 200;
//                    if (floors <= 30) return isFloatingWall ? 300 : 225;
//                    if (floors <= 35) return isFloatingWall ? 300 : 225;
//                    if (floors <= 40) return isFloatingWall ? 350 : 250;
//                    if (floors <= 45) return isFloatingWall ? 350 : 300;
//                    return isFloatingWall ? 400 : 350;

//                default: return 200;
//            }
//        }

//        // ====================================================================
//        // ZONE III — MMR & Pune  (IS 1893-2025)
//        // # = wall length < 1.8m and coupled shear wall in internal/core
//        // ====================================================================
//        private static int GetZone3Thickness(int floors, WallType wallType, bool isShortWall)
//        {
//            switch (wallType)
//            {
//                case WallType.CoreWall:
//                    // < 20: 200
//                    // 21-25: 300
//                    // 26-30: 375
//                    // 31-35: 400
//                    // 36-40: 425
//                    // 41-45: 450
//                    // 46-50: 450
//                    if (floors <= 20) return 200;
//                    if (floors <= 25) return 300;
//                    if (floors <= 30) return 375;
//                    if (floors <= 35) return 400;
//                    if (floors <= 40) return 425;
//                    if (floors <= 45) return 450;
//                    return 450;

//                case WallType.PeripheralDeadWall:
//                    // < 20: 200
//                    // 21-25: 250
//                    // 26-30: 300
//                    // 31-35: 325
//                    // 36-40: 350
//                    // 41-45: 375
//                    // 46-50: 400
//                    if (floors <= 20) return 200;
//                    if (floors <= 25) return 250;
//                    if (floors <= 30) return 300;
//                    if (floors <= 35) return 325;
//                    if (floors <= 40) return 350;
//                    if (floors <= 45) return 375;
//                    return 400;

//                case WallType.PeripheralPortalWall:
//                    // < 20: 300
//                    // 21-25: 350
//                    // 26-30: 400
//                    // 31-35: 400
//                    // 36-40: 425
//                    // 41-45: 425
//                    // 46-50: 450
//                    if (floors <= 20) return 300;
//                    if (floors <= 25) return 350;
//                    if (floors <= 30) return 400;
//                    if (floors <= 35) return 400;
//                    if (floors <= 40) return 425;
//                    if (floors <= 45) return 425;
//                    return 450;

//                case WallType.InternalWall:
//                    // < 20: 200 / 300#
//                    // 21-25: 200 / 300#
//                    // 26-30: 200 / 300#
//                    // 31-35: 225 / 350#
//                    // 36-40: 250 / 400#
//                    // 41-45: 275 / 450#
//                    // 46-50: 300 / 500#
//                    if (floors <= 20) return isShortWall ? 300 : 200;
//                    if (floors <= 25) return isShortWall ? 300 : 200;
//                    if (floors <= 30) return isShortWall ? 300 : 200;
//                    if (floors <= 35) return isShortWall ? 350 : 225;
//                    if (floors <= 40) return isShortWall ? 400 : 250;
//                    if (floors <= 45) return isShortWall ? 450 : 275;
//                    return isShortWall ? 500 : 300;

//                default: return 200;
//            }
//        }

//        // ====================================================================
//        // ZONE IV — Ahmedabad & Kolkata  (IS 1893-2025)
//        // # = wall length < 1.8m and coupled shear wall in internal/core
//        // ====================================================================
//        private static int GetZone4Thickness(int floors, WallType wallType, bool isShortWall)
//        {
//            switch (wallType)
//            {
//                case WallType.CoreWall:
//                    // < 20: 300
//                    // 21-25: 350
//                    // 26-30: 375
//                    // 31-35: 400
//                    // 36-40: 425
//                    // 41-45: 450
//                    // 46-50: 475
//                    if (floors <= 20) return 300;
//                    if (floors <= 25) return 350;
//                    if (floors <= 30) return 375;
//                    if (floors <= 35) return 400;
//                    if (floors <= 40) return 425;
//                    if (floors <= 45) return 450;
//                    return 475;

//                case WallType.PeripheralDeadWall:
//                    // < 20: 200
//                    // 21-25: 250
//                    // 26-30: 275
//                    // 31-35: 300
//                    // 36-40: 325
//                    // 41-45: 375
//                    // 46-50: 400
//                    if (floors <= 20) return 200;
//                    if (floors <= 25) return 250;
//                    if (floors <= 30) return 275;
//                    if (floors <= 35) return 300;
//                    if (floors <= 40) return 325;
//                    if (floors <= 45) return 375;
//                    return 400;

//                case WallType.PeripheralPortalWall:
//                    // < 20: 300
//                    // 21-25: 350
//                    // 26-30: 400
//                    // 31-35: 400
//                    // 36-40: 425
//                    // 41-45: 450
//                    // 46-50: 500
//                    if (floors <= 20) return 300;
//                    if (floors <= 25) return 350;
//                    if (floors <= 30) return 400;
//                    if (floors <= 35) return 400;
//                    if (floors <= 40) return 425;
//                    if (floors <= 45) return 450;
//                    return 500;

//                case WallType.InternalWall:
//                    // < 20: 200 / 300#
//                    // 21-25: 225 / 300#
//                    // 26-30: 250 / 350#
//                    // 31-35: 275 / 400#
//                    // 36-40: 300 / 450#
//                    // 41-45: 300 / 475#
//                    // 46-50: 325 / 500#
//                    if (floors <= 20) return isShortWall ? 300 : 200;
//                    if (floors <= 25) return isShortWall ? 300 : 225;
//                    if (floors <= 30) return isShortWall ? 350 : 250;
//                    if (floors <= 35) return isShortWall ? 400 : 275;
//                    if (floors <= 40) return isShortWall ? 450 : 300;
//                    if (floors <= 45) return isShortWall ? 475 : 300;
//                    return isShortWall ? 500 : 325;

//                default: return 240;
//            }
//        }

//        // ====================================================================
//        // ZONE IV NCR — NCR (also used for Zone V)  (IS 1893-2025)
//        // Min 240mm everywhere due to fire rating requirements
//        // # = wall length < 1.8m and coupled shear wall
//        // ====================================================================
//        private static int GetZone4NCRThickness(int floors, WallType wallType, bool isShortWall)
//        {
//            switch (wallType)
//            {
//                case WallType.CoreWall:
//                    // < 20: 325
//                    // 21-25: 375
//                    // 26-30: 400
//                    // 31-35: 425
//                    // 36-40: 450
//                    // 41-45: 475
//                    // 46-50: 500
//                    if (floors <= 20) return 325;
//                    if (floors <= 25) return 375;
//                    if (floors <= 30) return 400;
//                    if (floors <= 35) return 425;
//                    if (floors <= 40) return 450;
//                    if (floors <= 45) return 475;
//                    return 500;

//                case WallType.PeripheralDeadWall:
//                    // < 20: 240
//                    // 21-25: 240
//                    // 26-30: 300
//                    // 31-35: 325
//                    // 36-40: 350
//                    // 41-45: 400
//                    // 46-50: 425
//                    if (floors <= 20) return 240;
//                    if (floors <= 25) return 240;
//                    if (floors <= 30) return 300;
//                    if (floors <= 35) return 325;
//                    if (floors <= 40) return 350;
//                    if (floors <= 45) return 400;
//                    return 425;

//                case WallType.PeripheralPortalWall:
//                    // < 20: 300
//                    // 21-25: 350
//                    // 26-30: 400
//                    // 31-35: 400
//                    // 36-40: 425
//                    // 41-45: 450
//                    // 46-50: 500
//                    if (floors <= 20) return 300;
//                    if (floors <= 25) return 350;
//                    if (floors <= 30) return 400;
//                    if (floors <= 35) return 400;
//                    if (floors <= 40) return 425;
//                    if (floors <= 45) return 450;
//                    return 500;

//                case WallType.InternalWall:
//                    // < 20: 240 / 300#
//                    // 21-25: 240 / 300#
//                    // 26-30: 250 / 350#
//                    // 31-35: 275 / 400#
//                    // 36-40: 300 / 450#
//                    // 41-45: 300 / 475#
//                    // 46-50: 350 / 500#
//                    if (floors <= 20) return isShortWall ? 300 : 240;
//                    if (floors <= 25) return isShortWall ? 300 : 240;
//                    if (floors <= 30) return isShortWall ? 350 : 250;
//                    if (floors <= 35) return isShortWall ? 400 : 275;
//                    if (floors <= 40) return isShortWall ? 450 : 300;
//                    if (floors <= 45) return isShortWall ? 475 : 300;
//                    return isShortWall ? 500 : 350;

//                default: return 240;
//            }
//        }
//        public static WallType ClassifyWallFromLayerName(string layerName)
//        {
//            string upper = layerName.ToUpperInvariant();

//            // Core walls - highest priority
//            if (upper.Equals("CORE WALL", StringComparison.OrdinalIgnoreCase) ||
//                upper.Contains("CORE") || upper.Contains("LIFT") || upper.Contains("ELEVATOR") ||
//                upper.Contains("SHAFT") || upper.Contains("STAIRCASE") || upper.Contains("STAIR"))
//                return WallType.CoreWall;

//            // Peripheral Portal walls
//            if (upper.Equals("PERIPHERAL PORTAL WALL", StringComparison.OrdinalIgnoreCase) ||
//                upper.Contains("PORTAL") || upper.Contains("FRAME"))
//                return WallType.PeripheralPortalWall;

//            // Peripheral Dead walls
//            if (upper.Equals("PERIPHERAL DEAD WALL", StringComparison.OrdinalIgnoreCase) ||
//                upper.Contains("PERIPHERAL") || upper.Contains("EXTERNAL") || upper.Contains("EXTERIOR") ||
//                upper.Contains("OUTER") || upper.Contains("BOUNDARY") || upper.Contains("PERIMETER") ||
//                upper.Contains("FACADE"))
//                return WallType.PeripheralDeadWall;

//            // Internal walls (default)
//            return WallType.InternalWall;
//        }

//        public static string GetDesignNotes(int numTypicalFloors, string seismicZone)
//        {
//            string notes = "=== WALL THICKNESS DESIGN NOTES ===\n\n";
//            //notes += $"Configuration: {numTypicalFloors} typical floors in {seismicZone}\n\n";
//            //notes += "Wall Sections from Template:\n";

//            var thicknesses = GetAvailableThicknesses();
//            foreach (int thickness in thicknesses)
//            {
//                var sections = wallSectionsByThickness[thickness];
//                notes += $"  {thickness}mm ({thickness / 10.0:F1}cm): {string.Join(", ", sections)}\n";
//            }


//            if (numTypicalFloors > 50)
//            {
//                notes += "⚠️ WARNING: Building exceeds 50 floors - manual review required!\n";
//            }

//            return notes;
//        }
//    }
//}
// ============================================================================
// FILE: Core/WallThicknessCalculator.cs
// VERSION: 2.0 — Dual IS Code Support (IS 1893:2016 & IS 1893:2025)
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
                case "Zone III (MMR, Pune)":
                    return GetZone3_2025(floors, wallType, isShortWall);
                case "Zone IV":
                case "Zone IV (Ahmedabad & Kolkata)":
                    return GetZone4_2025(floors, wallType, isShortWall);
                case "Zone IV (NCR)":
                case "Zone V":
                    return GetZone4NCR_2025(floors, wallType, isShortWall);
                default:
                    throw new ArgumentException($"Invalid seismic zone: {seismicZone}");
            }
        }

        // ── IS 2025 · Zone II ─────────────────────────────────────────────
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
                    return fl ? 400 : 325;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return sh ? 200 : 160;
                    if (f <= 30) return sh ? 250 : 160;
                    if (f <= 35) return sh ? 275 : 200;
                    if (f <= 40) return sh ? 325 : 200;
                    if (f <= 45) return sh ? 325 : 225;
                    return sh ? 400 : 275;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return fl ? 250 : 200;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 300 : 225;
                    if (f <= 40) return fl ? 300 : 250;
                    if (f <= 45) return fl ? 350 : 300;
                    return fl ? 400 : 325;

                case WallType.InternalWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 200;
                    if (f <= 30) return fl ? 300 : 225;
                    if (f <= 35) return fl ? 300 : 225;
                    if (f <= 40) return fl ? 350 : 250;
                    if (f <= 45) return fl ? 350 : 300;
                    return fl ? 400 : 350;

                default: return 200;
            }
        }

        // ── IS 2025 · Zone III (MMR & Pune) ──────────────────────────────
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
                    return 450;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 250;
                    if (f <= 30) return 300;
                    if (f <= 35) return 325;
                    if (f <= 40) return 350;
                    if (f <= 45) return 375;
                    return 400;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 425;
                    return 450;

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 200;
                    if (f <= 30) return sh ? 300 : 200;
                    if (f <= 35) return sh ? 350 : 225;
                    if (f <= 40) return sh ? 400 : 250;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;

                default: return 200;
            }
        }

        // ── IS 2025 · Zone IV (Ahmedabad & Kolkata) ───────────────────────
        private static int GetZone4_2025(int f, WallType w, bool sh)
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
                    return 475;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 250;
                    if (f <= 30) return 275;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 375;
                    return 400;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 500;

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 225;
                    if (f <= 30) return sh ? 350 : 250;
                    if (f <= 35) return sh ? 400 : 275;
                    if (f <= 40) return sh ? 450 : 300;
                    if (f <= 45) return sh ? 475 : 300;
                    return sh ? 500 : 325;

                default: return 240;
            }
        }

        // ── IS 2025 · Zone IV NCR / Zone V ────────────────────────────────
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
                    return 500;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 240;
                    if (f <= 25) return 240;
                    if (f <= 30) return 300;
                    if (f <= 35) return 325;
                    if (f <= 40) return 350;
                    if (f <= 45) return 400;
                    return 425;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 425;
                    if (f <= 45) return 450;
                    return 500;

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 240;
                    if (f <= 25) return sh ? 300 : 240;
                    if (f <= 30) return sh ? 350 : 250;
                    if (f <= 35) return sh ? 400 : 275;
                    if (f <= 40) return sh ? 450 : 300;
                    if (f <= 45) return sh ? 475 : 300;
                    return sh ? 500 : 350;

                default: return 240;
            }
        }

        // ====================================================================
        // IS 1893 : 2016  (TDD / PKO)
        // Zone II  = Bangalore, Hyderabad
        // Zone III = MMR, Ahmedabad, Kolkata, Pune
        // Zone IV  = NCR  (Type-I construction)
        // Zone V   = same as Zone IV per source document
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
                case "Zone III (MMR, Pune)":
                    // IS 2016 Zone III: MMR, Ahmedabad, Kolkata, Pune
                    return GetZone3_2016(floors, wallType, isShortWall);

                case "Zone IV":
                case "Zone IV (Ahmedabad & Kolkata)":
                    // Ahmedabad & Kolkata were Zone III in IS 2016
                    return GetZone3_2016(floors, wallType, isShortWall);

                case "Zone IV (NCR)":
                case "Zone V":
                    return GetZone4NCR_2016(floors, wallType, isShortWall);

                default:
                    throw new ArgumentException($"Invalid seismic zone: {seismicZone}");
            }
        }

        // ── IS 2016 · Zone II (Bangalore, Hyderabad) ─────────────────────
        //
        // Floors    Core        PeriphDead    PeriphPortal    Internal
        // <20      160/200*    160/200*          200          160/200#
        // 21-25    200/250*    200/250*          200          160/200#
        // 26-30    200/250*    200/250*          200          160/250#
        // 31-35    200/300*    200/250*          200          200/300#
        // 36-40    200/300*    200/250*          200          200/300#
        // 41-45    200/325*    250/300*          250          225/325#
        // 46-50    300/350*    300/350*          300          250/350#
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
                    return fl ? 350 : 300;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return fl ? 200 : 160;
                    if (f <= 25) return fl ? 250 : 200;
                    if (f <= 30) return fl ? 250 : 200;
                    if (f <= 35) return fl ? 250 : 200;
                    if (f <= 40) return fl ? 250 : 200;
                    if (f <= 45) return fl ? 300 : 250;
                    return fl ? 350 : 300;

                case WallType.PeripheralPortalWall:
                    // No * split in 2016 Zone II portal wall
                    if (f <= 40) return 200;
                    if (f <= 45) return 250;
                    return 300;

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 200 : 160;
                    if (f <= 25) return sh ? 200 : 160;
                    if (f <= 30) return sh ? 250 : 160;
                    if (f <= 35) return sh ? 300 : 200;
                    if (f <= 40) return sh ? 300 : 200;
                    if (f <= 45) return sh ? 325 : 225;
                    return sh ? 350 : 250;

                default: return 200;
            }
        }

        // ── IS 2016 · Zone III (MMR, Ahmedabad, Kolkata, Pune) ────────────
        //
        // Floors    Core    PeriphDead    PeriphPortal    Internal
        // <20       200        200            300          200/300#
        // 21-25     300        200            350          200/300#
        // 26-30     350        250            400          200/300#
        // 31-35     375        300            400          225/350#
        // 36-40     400        325            400          250/400#
        // 41-45     425        350            400          275/450#
        // 46-50     450        400            450          300/500#
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
                    return 450;

                case WallType.PeripheralDeadWall:
                    if (f <= 20) return 200;
                    if (f <= 25) return 200;
                    if (f <= 30) return 250;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 350;
                    return 400;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 400;
                    if (f <= 45) return 400;
                    return 450;

                case WallType.InternalWall:
                    if (f <= 20) return sh ? 300 : 200;
                    if (f <= 25) return sh ? 300 : 200;
                    if (f <= 30) return sh ? 300 : 200;
                    if (f <= 35) return sh ? 350 : 225;
                    if (f <= 40) return sh ? 400 : 250;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;

                default: return 200;
            }
        }

        // ── IS 2016 · Zone IV NCR (Type-I construction, also Zone V) ─────
        //
        // Floors    Core    PeriphDead    PeriphPortal    Internal
        // <20       300        240            300          240/300#
        // 21-25     350        240            350          240/300#
        // 26-30     375        275            400          240/300#
        // 31-35     400        300            400          240/350#
        // 36-40     425        325            400          240/400#
        // 41-45     450        350            400          275/450#
        // 46-50     500        400            450          300/500#
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
                    return 500;

                case WallType.PeripheralDeadWall:
                    if (f <= 25) return 240;
                    if (f <= 30) return 275;
                    if (f <= 35) return 300;
                    if (f <= 40) return 325;
                    if (f <= 45) return 350;
                    return 400;

                case WallType.PeripheralPortalWall:
                    if (f <= 20) return 300;
                    if (f <= 25) return 350;
                    if (f <= 30) return 400;
                    if (f <= 35) return 400;
                    if (f <= 40) return 400;
                    if (f <= 45) return 400;
                    return 450;

                case WallType.InternalWall:
                    // Min 240mm fire rating
                    if (f <= 20) return sh ? 300 : 240;
                    if (f <= 25) return sh ? 300 : 240;
                    if (f <= 30) return sh ? 300 : 240;
                    if (f <= 35) return sh ? 350 : 240;
                    if (f <= 40) return sh ? 400 : 240;
                    if (f <= 45) return sh ? 450 : 275;
                    return sh ? 500 : 300;

                default: return 240;
            }
        }
    }
}
