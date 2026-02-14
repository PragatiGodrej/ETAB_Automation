
// ============================================================================
// FILE: Importers/BeamImporterEnhanced.cs (WITH GRADE SCHEDULE SUPPORT)
// ============================================================================
using ETAB_Automation.Core;

using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ETABS_CAD_Automation.Importers
{
    /// <summary>
    /// Enhanced beam importer with grade schedule support
    /// Beam grades are 0.7x wall grade (rounded to nearest 5)
    /// </summary>
    public class BeamImporterEnhanced
    {
        private readonly cSapModel sapModel;
        private readonly DxfDocument dxfDoc;
        private readonly string seismicZone;
        private readonly int totalTypicalFloors;
        private readonly Dictionary<string, int> beamDepths;
        private readonly GradeScheduleManager gradeSchedule;  // NEW: Grade schedule

        // Convert DXF coordinates to meters
        // Z elevations come from StoryManager and are ALREADY in meters - NO CONVERSION NEEDED
        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;

        private double MX(double xValue) => xValue * X_TO_M;
        private double MY(double yValue) => yValue * Y_TO_M;

        // Store available beam sections from template
        private static Dictionary<string, BeamSectionInfo> availableBeamSections =
            new Dictionary<string, BeamSectionInfo>();

        private class BeamSectionInfo
        {
            public string SectionName { get; set; }
            public int WidthMm { get; set; }
            public int DepthMm { get; set; }
            public string Grade { get; set; }
        }

        public BeamImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            string zone,
            int typicalFloors,
            Dictionary<string, int> depths,
            GradeScheduleManager gradeManager = null)  // NEW: Optional grade manager
        {
            sapModel = model;
            dxfDoc = doc;
            seismicZone = zone;
            totalTypicalFloors = typicalFloors;
            beamDepths = depths;
            gradeSchedule = gradeManager;  // NEW

            // Load available beam sections from template
            LoadAvailableBeamSections();
        }

        /// <summary>
        /// Load available beam sections from ETABS template
        /// Parses section names like B20X75M35, B20X40M30, etc.
        /// Format: B[Width_cm]X[Depth_cm]M[Grade]
        /// </summary>
        private void LoadAvailableBeamSections()
        {
            if (availableBeamSections.Count > 0) return; // Already loaded

            try
            {
                availableBeamSections.Clear();

                int numSections = 0;
                string[] sectionNames = null;

                int ret = sapModel.PropFrame.GetNameList(ref numSections, ref sectionNames);

                if (ret == 0 && sectionNames != null)
                {
                    // Regex to parse beam section names: B20X75M35, B20X67.5M40, etc.
                    // Pattern: B followed by width, X, depth, M, and grade
                    // Width and depth are in centimeters (can have decimals like 67.5)
                    Regex beamPattern = new Regex(@"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
                        RegexOptions.IgnoreCase);

                    foreach (string sectionName in sectionNames)
                    {
                        Match match = beamPattern.Match(sectionName);

                        if (match.Success)
                        {
                            // Extract dimensions in centimeters
                            double widthCm = double.Parse(match.Groups[1].Value);
                            double depthCm = double.Parse(match.Groups[2].Value);
                            string grade = match.Groups[3].Value;

                            // Convert to millimeters
                            int widthMm = (int)Math.Round(widthCm * 10);
                            int depthMm = (int)Math.Round(depthCm * 10);

                            availableBeamSections[sectionName] = new BeamSectionInfo
                            {
                                SectionName = sectionName,
                                WidthMm = widthMm,
                                DepthMm = depthMm,
                                Grade = grade
                            };

                            System.Diagnostics.Debug.WriteLine(
                                $"Loaded beam: {sectionName} = {widthMm}x{depthMm}mm (M{grade})");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"\n✓ Loaded {availableBeamSections.Count} beam sections from template");

                if (availableBeamSections.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No beam sections found in template. Please ensure template has beam sections defined (e.g., B20X75M35).");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading beam sections: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get gravity beam width based on seismic zone
        /// Zone II/III: 200mm
        /// Zone IV/V: 240mm
        /// </summary>
        private int GetGravityBeamWidth()
        {
            return (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;
        }

        /// <summary>
        /// Get main beam width based on adjacent wall thickness
        /// </summary>
        private int GetMainBeamWidth(WallThicknessCalculator.WallType wallType)
        {
            // Get wall thickness for this wall type
            int wallThickness = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors,
                wallType,
                seismicZone,
                2.0, // normal wall length
                false); // not floating

            return wallThickness;
        }

        /// <summary>
        /// Find closest matching beam section from template
        /// </summary>
        private string GetClosestBeamSection(int requiredWidth, int requiredDepth, string preferredGrade = null)
        {
            if (availableBeamSections.Count == 0)
            {
                throw new InvalidOperationException(
                    "No beam sections loaded from template. Ensure template has beam sections defined.");
            }

            string bestMatch = null;
            int minDifference = int.MaxValue;

            // ⭐ CRITICAL FIX: Remove "M" prefix from preferredGrade for comparison
            // GradeScheduleManager returns "M30", but section.Grade is stored as "30"
            string gradeToMatch = preferredGrade?.Replace("M", "").Replace("m", "").Trim();

            // First try to find match with preferred grade
            if (!string.IsNullOrEmpty(gradeToMatch))
            {
                foreach (var kvp in availableBeamSections)
                {
                    var section = kvp.Value;

                    // Only consider sections with the preferred grade
                    if (section.Grade == gradeToMatch)
                    {
                        // Calculate difference (prioritize depth match over width)
                        int depthDiff = Math.Abs(section.DepthMm - requiredDepth);
                        int widthDiff = Math.Abs(section.WidthMm - requiredWidth);
                        int totalDiff = (depthDiff * 2) + widthDiff; // Depth is more important

                        if (totalDiff < minDifference)
                        {
                            minDifference = totalDiff;
                            bestMatch = kvp.Key;
                        }
                    }
                }

                if (bestMatch != null)
                {
                    var matchedSection = availableBeamSections[bestMatch];
                    System.Diagnostics.Debug.WriteLine(
                        $"  Required: {requiredWidth}x{requiredDepth}mm M{gradeToMatch} → Using: {bestMatch} " +
                        $"({matchedSection.WidthMm}x{matchedSection.DepthMm}mm M{matchedSection.Grade})");
                    return bestMatch;
                }
            }

            // Fallback: Find closest match without grade preference
            minDifference = int.MaxValue;
            foreach (var kvp in availableBeamSections)
            {
                var section = kvp.Value;

                // Calculate difference (prioritize depth match over width)
                int depthDiff = Math.Abs(section.DepthMm - requiredDepth);
                int widthDiff = Math.Abs(section.WidthMm - requiredWidth);
                int totalDiff = (depthDiff * 2) + widthDiff; // Depth is more important

                if (totalDiff < minDifference)
                {
                    minDifference = totalDiff;
                    bestMatch = kvp.Key;
                }
            }

            if (bestMatch == null)
            {
                // If still no match, list available sections
                System.Diagnostics.Debug.WriteLine($"⚠️ No beam section found for {requiredWidth}x{requiredDepth}mm");
                System.Diagnostics.Debug.WriteLine("Available sections:");
                foreach (var kvp in availableBeamSections.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value.WidthMm}x{kvp.Value.DepthMm}mm");
                }

                throw new InvalidOperationException(
                    $"No suitable beam section found for {requiredWidth}x{requiredDepth}mm. " +
                    $"Available widths: {string.Join(", ", availableBeamSections.Select(s => s.Value.WidthMm).Distinct().OrderBy(w => w))}mm");
            }

            var matchedSection2 = availableBeamSections[bestMatch];
            System.Diagnostics.Debug.WriteLine(
                $"  Required: {requiredWidth}x{requiredDepth}mm → Using: {bestMatch} " +
                $"({matchedSection2.WidthMm}x{matchedSection2.DepthMm}mm M{matchedSection2.Grade})");

            return bestMatch;
        }

        /// <summary>
        /// Determine beam section based on layer name and beam configuration
        /// </summary>
        private string DetermineBeamSection(string layerName, string preferredGrade)
        {
            string upper = layerName.ToUpperInvariant();

            // GRAVITY BEAMS (Width based on seismic zone)
            int gravityWidth = GetGravityBeamWidth();

            // B-Internal gravity beams
            if (upper.Contains("INTERNAL") && upper.Contains("GRAVITY"))
            {
                return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
            }

            // B-Cantilever Gravity Beams
            if (upper.Contains("CANTILEVER") && upper.Contains("GRAVITY"))
            {
                return GetClosestBeamSection(gravityWidth, beamDepths["CantileverGravity"], preferredGrade);
            }

            // MAIN BEAMS (Width based on adjacent wall thickness)

            // B-Core Main Beam
            if (upper.Contains("CORE") && upper.Contains("MAIN"))
            {
                int coreWallWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.CoreWall);
                return GetClosestBeamSection(coreWallWidth, beamDepths["CoreMain"], preferredGrade);
            }

            // B-Peripheral dead Main Beams
            if (upper.Contains("PERIPHERAL") && upper.Contains("DEAD") && upper.Contains("MAIN"))
            {
                int peripheralDeadWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.PeripheralDeadWall);
                return GetClosestBeamSection(peripheralDeadWidth, beamDepths["PeripheralDeadMain"], preferredGrade);
            }

            // B-Peripheral Portal Main Beams
            if (upper.Contains("PERIPHERAL") && upper.Contains("PORTAL") && upper.Contains("MAIN"))
            {
                int peripheralPortalWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.PeripheralPortalWall);
                return GetClosestBeamSection(peripheralPortalWidth, beamDepths["PeripheralPortalMain"], preferredGrade);
            }

            // B-Internal Main beams
            if (upper.Contains("INTERNAL") && upper.Contains("MAIN"))
            {
                int internalWallWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.InternalWall);
                return GetClosestBeamSection(internalWallWidth, beamDepths["InternalMain"], preferredGrade);
            }

            // Generic beam detection (fallback)
            if (upper.Contains("BEAM") || upper.StartsWith("B-"))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Generic beam layer '{layerName}', using default gravity beam");
                return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
            }

            // Default fallback - use gravity beam
            System.Diagnostics.Debug.WriteLine($"⚠️ Unknown beam layer '{layerName}', using default gravity beam");
            return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
        }

        public void ImportBeams(Dictionary<string, string> layerMapping, double elevation, int story)
        {
            var beamLayers = layerMapping
                .Where(x => x.Value == "Beam")
                .Select(x => x.Key)
                .ToList();

            if (beamLayers.Count == 0) return;

            // NEW: Get beam grade for this story (0.7x wall grade)
            string beamGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

            System.Diagnostics.Debug.WriteLine($"\n========== IMPORTING BEAMS - Story {story} ==========");
            System.Diagnostics.Debug.WriteLine($"Seismic Zone: {seismicZone}");
            System.Diagnostics.Debug.WriteLine($"Gravity Beam Width: {GetGravityBeamWidth()}mm");

            if (!string.IsNullOrEmpty(beamGrade))
            {
                System.Diagnostics.Debug.WriteLine($"Beam Concrete Grade: {beamGrade}");
            }

            System.Diagnostics.Debug.WriteLine($"Beam Elevation: {elevation:F3}m (already in meters - no conversion)");

            int totalBeamsCreated = 0;

            foreach (string layerName in beamLayers)
            {
                string section = DetermineBeamSection(layerName, beamGrade);
                int beamCount = 0;

                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName}");

                foreach (netDxf.Entities.Line line in dxfDoc.Entities.Lines
                    .Where(l => l.Layer.Name == layerName))
                {
                    CreateBeamFromLine(line, elevation, section, story);
                    beamCount++;
                }

                foreach (Polyline2D poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    beamCount += CreateBeamFromPolyline(poly, elevation, section, story);
                }

                System.Diagnostics.Debug.WriteLine($"  ✓ Created {beamCount} beams");
                totalBeamsCreated += beamCount;
            }

            System.Diagnostics.Debug.WriteLine($"\nTotal beams created: {totalBeamsCreated}");
            System.Diagnostics.Debug.WriteLine($"=========================================\n");
        }

        private void CreateBeamFromLine(netDxf.Entities.Line line, double elevation,
            string section, int story)
        {
            string frameName = "";
            string storyName = GetStoryName(story);

            // CORRECTED: elevation is ALREADY in meters - NO CONVERSION
            sapModel.FrameObj.AddByCoord(
                MX(line.StartPoint.X),
                MY(line.StartPoint.Y),
                elevation,  // ← No conversion
                MX(line.EndPoint.X),
                MY(line.EndPoint.Y),
                elevation,  // ← No conversion
                ref frameName, section, storyName);
        }

        private int CreateBeamFromPolyline(Polyline2D poly, double elevation,
            string section, int story)
        {
            string storyName = GetStoryName(story);
            var vertices = poly.Vertexes;
            int count = 0;

            // CORRECTED: elevation is ALREADY in meters - NO CONVERSION
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                string frameName = "";
                sapModel.FrameObj.AddByCoord(
                    MX(vertices[i].Position.X), MY(vertices[i].Position.Y), elevation,
                    MX(vertices[i + 1].Position.X), MY(vertices[i + 1].Position.Y), elevation,
                    ref frameName, section, storyName);
                count++;
            }

            if (poly.IsClosed && vertices.Count > 2)
            {
                string frameName = "";
                sapModel.FrameObj.AddByCoord(
                    MX(vertices[vertices.Count - 1].Position.X),
                    MY(vertices[vertices.Count - 1].Position.Y), elevation,
                    MX(vertices[0].Position.X), MY(vertices[0].Position.Y), elevation,
                    ref frameName, section, storyName);
                count++;
            }

            return count;
        }

        //private string GetStoryName(int story)
        //{
        //    try
        //    {
        //        int numStories = 0;
        //        string[] storyNames = null;

        //        int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

        //        if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
        //        {
        //            return storyNames[story];
        //        }
        //    }
        //    catch { }

        //    return story == 0 ? "Base" : $"Story{story + 1}";
        //}

        private string GetStoryName(int story)
        {
            try
            {
                int numStories = 0;
                string[] storyNames = null;

                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

                // ✅ FIXED: Use story index directly (0-based)
                if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
                {
                    return storyNames[story];  // No subtraction
                }
            }
            catch { }

            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }
}

