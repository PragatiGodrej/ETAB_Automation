
// ============================================================================
// FILE: Core/WallThicknessCalculator.cs (FIXED FOR W16M30 FORMAT)
// ============================================================================
using ETABSv1;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ETAB_Automation.Core
{
    /// <summary>
    /// Calculates wall thickness based on TDD/PKO design standards
    /// Reads existing wall sections from ETABS template (e.g., W16M30 = 160mm, M30)
    /// </summary>
    public class WallThicknessCalculator
    {
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

        // Store available wall sections: Key = section name, Value = thickness in meters
        private static Dictionary<string, double> availableWallSections = new Dictionary<string, double>();

        // Store by thickness for quick lookup: Key = thickness in mm, Value = list of section names
        private static Dictionary<int, List<string>> wallSectionsByThickness = new Dictionary<int, List<string>>();

        /// <summary>
        /// Load available wall sections from ETABS model
        /// Parses section names like W16M30, W20M40, W47.5M40, etc.
        /// Format: W[thickness_cm]M[grade]
        /// Example: W16M30 = 16cm = 160mm thick, M30 grade
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
                    // Regex to parse wall section names: W16M30, W20M40, W47.5M40, etc.
                    // Pattern: W followed by thickness in CM (decimal allowed), then M and grade
                    Regex wallPattern = new Regex(@"^W(\d+(?:\.\d+)?)M(\d+)", RegexOptions.IgnoreCase);

                    foreach (string sectionName in sectionNames)
                    {
                        Match match = wallPattern.Match(sectionName);

                        if (match.Success)
                        {
                            // Extract thickness in centimeters and convert to millimeters
                            double thicknessCm = double.Parse(match.Groups[1].Value);
                            int thicknessMm = (int)Math.Round(thicknessCm * 10);

                            // Extract concrete grade
                            string grade = match.Groups[2].Value;

                            // Get actual thickness from ETABS (this is in meters)
                            eWallPropType wallType = eWallPropType.Specified;
                            eShellType shellType = eShellType.ShellThin;
                            string matProp = "";
                            double thicknessMeters = 0;
                            int color = 0;
                            string notes = "";
                            string guid = "";

                            ret = sapModel.PropArea.GetWall(
                                sectionName,
                                ref wallType,
                                ref shellType,
                                ref matProp,
                                ref thicknessMeters,
                                ref color,
                                ref notes,
                                ref guid);

                            if (ret == 0 && thicknessMeters > 0)
                            {
                                availableWallSections[sectionName] = thicknessMeters;

                                // Store by thickness for quick lookup
                                if (!wallSectionsByThickness.ContainsKey(thicknessMm))
                                {
                                    wallSectionsByThickness[thicknessMm] = new List<string>();
                                }
                                wallSectionsByThickness[thicknessMm].Add(sectionName);

                                System.Diagnostics.Debug.WriteLine(
                                    $"Loaded: {sectionName} = {thicknessMm}mm ({thicknessCm}cm) M{grade} concrete");
                            }
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

        /// <summary>
        /// Get list of available thicknesses in mm
        /// </summary>
        private static List<int> GetAvailableThicknesses()
        {
            List<int> thicknesses = new List<int>(wallSectionsByThickness.Keys);
            thicknesses.Sort();
            return thicknesses;
        }

        /// <summary>
        /// Get the closest matching wall section from template
        /// </summary>
        /// <param name="requiredThicknessMm">Required thickness in millimeters</param>
        /// <param name="preferredGrade">Preferred concrete grade (e.g., "M40"), null for any</param>
        /// <returns>Section name from template (e.g., W16M30 for 160mm)</returns>
        private static string GetClosestWallSection(int requiredThicknessMm, string preferredGrade = null)
        {
            if (availableWallSections.Count == 0)
            {
                throw new InvalidOperationException(
                    "No wall sections loaded. Call LoadAvailableWallSections first.");
            }

            // First try: exact match with preferred grade
            if (!string.IsNullOrEmpty(preferredGrade) && wallSectionsByThickness.ContainsKey(requiredThicknessMm))
            {
                foreach (string section in wallSectionsByThickness[requiredThicknessMm])
                {
                    if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
                    {
                        return section;
                    }
                }
            }

            // Second try: exact thickness match (any grade)
            if (wallSectionsByThickness.ContainsKey(requiredThicknessMm))
            {
                // Return first available section with this thickness
                return wallSectionsByThickness[requiredThicknessMm][0];
            }

            // Third try: find closest thickness
            string closestSection = null;
            int minDifference = int.MaxValue;

            foreach (var kvp in wallSectionsByThickness)
            {
                int thicknessMm = kvp.Key;
                int difference = Math.Abs(thicknessMm - requiredThicknessMm);

                if (difference < minDifference)
                {
                    minDifference = difference;

                    // Try to find preferred grade in this thickness
                    if (!string.IsNullOrEmpty(preferredGrade))
                    {
                        foreach (string section in kvp.Value)
                        {
                            if (section.ToUpperInvariant().Contains(preferredGrade.ToUpperInvariant()))
                            {
                                closestSection = section;
                                break;
                            }
                        }
                    }

                    // If no preferred grade found, take first available
                    if (closestSection == null)
                    {
                        closestSection = kvp.Value[0];
                    }
                }
            }

            if (closestSection == null)
            {
                throw new InvalidOperationException(
                    $"No suitable wall section found for {requiredThicknessMm}mm thickness");
            }

            return closestSection;
        }

        /// <summary>
        /// Calculate recommended wall thickness and return matching section name
        /// </summary>
        public static string GetRecommendedWallSection(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            double wallLength = 2.0,
            bool isFloatingWall = false,
            ConstructionType constructionType = ConstructionType.TypeII,
            string preferredGrade = null)
        {
            // Get required thickness from design standards
            int requiredThickness = GetRecommendedThickness(
                numTypicalFloors,
                wallType,
                seismicZone,
                wallLength,
                isFloatingWall,
                constructionType);

            // Find closest matching section from template
            string sectionName = GetClosestWallSection(requiredThickness, preferredGrade);

            double actualThicknessMm = availableWallSections[sectionName] * 1000;

            System.Diagnostics.Debug.WriteLine(
                $"  Required: {requiredThickness}mm → Using: {sectionName} ({actualThicknessMm:F0}mm)");

            return sectionName;
        }

        /// <summary>
        /// Get wall section for specific thickness and grade
        /// </summary>
        public static string GetWallSectionByThicknessAndGrade(int thicknessMm, string grade)
        {
            if (wallSectionsByThickness.ContainsKey(thicknessMm))
            {
                foreach (string section in wallSectionsByThickness[thicknessMm])
                {
                    if (section.ToUpperInvariant().Contains(grade.ToUpperInvariant()))
                    {
                        return section;
                    }
                }

                // If exact grade not found, return first available with this thickness
                return wallSectionsByThickness[thicknessMm][0];
            }

            return null;
        }

        /// <summary>
        /// Calculate recommended wall thickness (in mm) based on design standards
        /// </summary>
        public static int GetRecommendedThickness(
            int numTypicalFloors,
            WallType wallType,
            string seismicZone,
            double wallLength = 2.0,
            bool isFloatingWall = false,
            ConstructionType constructionType = ConstructionType.TypeII)
        {
            if (numTypicalFloors < 1 || numTypicalFloors > 50)
                throw new ArgumentException("Number of floors must be between 1 and 50");

            bool isShortWall = wallLength < 1.8;

            switch (seismicZone)
            {
                case "Zone II":
                    return GetZone2Thickness(numTypicalFloors, wallType, isShortWall, isFloatingWall);
                case "Zone III":
                    return GetZone3Thickness(numTypicalFloors, wallType, isShortWall);
                case "Zone IV":
                case "Zone V":
                    return GetZone4Thickness(numTypicalFloors, wallType, isShortWall, constructionType);
                default:
                    throw new ArgumentException($"Invalid seismic zone: {seismicZone}");
            }
        }

        // [Keep all your existing Zone thickness methods exactly as they are]
        private static int GetZone2Thickness(int floors, WallType wallType, bool isShortWall, bool isFloatingWall)
        {
            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20) return isFloatingWall ? 200 : 160;
                    else if (floors <= 25) return isFloatingWall ? 250 : 200;
                    else if (floors <= 30) return isFloatingWall ? 250 : 200;
                    else if (floors <= 35) return isFloatingWall ? 300 : 200;
                    else if (floors <= 40) return isFloatingWall ? 300 : 200;
                    else if (floors <= 45) return isFloatingWall ? 325 : 200;
                    else return isFloatingWall ? 350 : 300;

                case WallType.PeripheralDeadWall:
                    if (floors <= 20) return isFloatingWall ? 200 : 160;
                    else if (floors <= 25) return isFloatingWall ? 250 : 200;
                    else if (floors <= 30) return isFloatingWall ? 250 : 200;
                    else if (floors <= 35) return isFloatingWall ? 250 : 200;
                    else if (floors <= 40) return isFloatingWall ? 250 : 200;
                    else if (floors <= 45) return isFloatingWall ? 300 : 250;
                    else return isFloatingWall ? 350 : 300;

                case WallType.PeripheralPortalWall:
                    if (floors <= 40) return 200;
                    else if (floors <= 45) return 250;
                    else return 300;

                case WallType.InternalWall:
                    if (floors <= 20) return isShortWall ? 200 : 160;
                    else if (floors <= 25) return isShortWall ? 200 : 160;
                    else if (floors <= 30) return isShortWall ? 250 : 160;
                    else if (floors <= 35) return isShortWall ? 300 : 200;
                    else if (floors <= 40) return isShortWall ? 300 : 200;
                    else if (floors <= 45) return isShortWall ? 325 : 225;
                    else return isShortWall ? 350 : 250;

                default:
                    return 200;
            }
        }

        private static int GetZone3Thickness(int floors, WallType wallType, bool isShortWall)
        {
            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20) return 200;
                    else if (floors <= 25) return 300;
                    else if (floors <= 30) return 350;
                    else if (floors <= 35) return 375;
                    else if (floors <= 40) return 400;
                    else if (floors <= 45) return 425;
                    else return 450;

                case WallType.PeripheralDeadWall:
                    if (floors <= 20) return 200;
                    else if (floors <= 25) return 200;
                    else if (floors <= 30) return 250;
                    else if (floors <= 35) return 300;
                    else if (floors <= 40) return 325;
                    else if (floors <= 45) return 350;
                    else return 400;

                case WallType.PeripheralPortalWall:
                    if (floors <= 20) return 300;
                    else if (floors <= 25) return 350;
                    else if (floors <= 30) return 400;
                    else if (floors <= 40) return 400;
                    else if (floors <= 45) return 400;
                    else return 450;

                case WallType.InternalWall:
                    if (floors <= 20) return isShortWall ? 300 : 200;
                    else if (floors <= 25) return isShortWall ? 300 : 200;
                    else if (floors <= 30) return isShortWall ? 300 : 200;
                    else if (floors <= 35) return isShortWall ? 350 : 225;
                    else if (floors <= 40) return isShortWall ? 400 : 250;
                    else if (floors <= 45) return isShortWall ? 450 : 275;
                    else return isShortWall ? 500 : 300;

                default:
                    return 200;
            }
        }

        private static int GetZone4Thickness(int floors, WallType wallType, bool isShortWall, ConstructionType constructionType)
        {
            switch (wallType)
            {
                case WallType.CoreWall:
                    if (floors <= 20) return 300;
                    else if (floors <= 25) return 350;
                    else if (floors <= 30) return 375;
                    else if (floors <= 35) return 400;
                    else if (floors <= 40) return 425;
                    else if (floors <= 45) return 450;
                    else return 500;

                case WallType.PeripheralDeadWall:
                    if (floors <= 25) return 240;
                    else if (floors <= 30) return 275;
                    else if (floors <= 35) return 300;
                    else if (floors <= 40) return 325;
                    else if (floors <= 45) return 350;
                    else return 400;

                case WallType.PeripheralPortalWall:
                    if (floors <= 20) return 300;
                    else if (floors <= 25) return 350;
                    else if (floors <= 30) return 400;
                    else if (floors <= 40) return 400;
                    else if (floors <= 45) return 400;
                    else return 450;

                case WallType.InternalWall:
                    if (floors <= 20) return isShortWall ? 300 : 240;
                    else if (floors <= 25) return isShortWall ? 300 : 240;
                    else if (floors <= 30) return isShortWall ? 300 : 240;
                    else if (floors <= 35) return isShortWall ? 350 : 240;
                    else if (floors <= 40) return isShortWall ? 400 : 240;
                    else if (floors <= 45) return isShortWall ? 450 : 275;
                    else return isShortWall ? 500 : 300;

                default:
                    return 240;
            }
        }

        public static WallType ClassifyWallFromLayerName(string layerName)
        {
            string upper = layerName.ToUpperInvariant();

            // Core walls - highest priority
            if (upper.Equals("CORE WALL", StringComparison.OrdinalIgnoreCase) ||
                upper.Contains("CORE") || upper.Contains("LIFT") || upper.Contains("ELEVATOR") ||
                upper.Contains("SHAFT") || upper.Contains("STAIRCASE") || upper.Contains("STAIR"))
                return WallType.CoreWall;

            // Peripheral Portal walls
            if (upper.Equals("PERIPHERAL PORTAL WALL", StringComparison.OrdinalIgnoreCase) ||
                upper.Contains("PORTAL") || upper.Contains("FRAME"))
                return WallType.PeripheralPortalWall;

            // Peripheral Dead walls
            if (upper.Equals("PERIPHERAL DEAD WALL", StringComparison.OrdinalIgnoreCase) ||
                upper.Contains("PERIPHERAL") || upper.Contains("EXTERNAL") || upper.Contains("EXTERIOR") ||
                upper.Contains("OUTER") || upper.Contains("BOUNDARY") || upper.Contains("PERIMETER") ||
                upper.Contains("FACADE"))
                return WallType.PeripheralDeadWall;

            // Internal walls (default)
            return WallType.InternalWall;
        }

        public static string GetDesignNotes(int numTypicalFloors, string seismicZone)
        {
            string notes = "=== WALL THICKNESS DESIGN NOTES ===\n\n";
            //notes += $"Configuration: {numTypicalFloors} typical floors in {seismicZone}\n\n";
            //notes += "Wall Sections from Template:\n";

            var thicknesses = GetAvailableThicknesses();
            foreach (int thickness in thicknesses)
            {
                var sections = wallSectionsByThickness[thickness];
                notes += $"  {thickness}mm ({thickness / 10.0:F1}cm): {string.Join(", ", sections)}\n";
            }


            if (numTypicalFloors > 50)
            {
                notes += "⚠️ WARNING: Building exceeds 50 floors - manual review required!\n";
            }

            return notes;
        }
    }
}
