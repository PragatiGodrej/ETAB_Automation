
////// ============================================================================
////// FILE: Importers/BeamImporterEnhanced.cs (WITH GRADE SCHEDULE SUPPORT)
////// ============================================================================
////using ETAB_Automation.Core;

////using ETABSv1;
////using netDxf;
////using netDxf.Entities;
////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Text.RegularExpressions;

////namespace ETABS_CAD_Automation.Importers
////{
////    /// <summary>
////    /// Enhanced beam importer with grade schedule support
////    /// Beam grades are 0.7x wall grade (rounded to nearest 5)
////    /// </summary>
////    public class BeamImporterEnhanced
////    {
////        private readonly cSapModel sapModel;
////        private readonly DxfDocument dxfDoc;
////        private readonly string seismicZone;
////        private readonly int totalTypicalFloors;
////        private readonly Dictionary<string, int> beamDepths;
////        private readonly GradeScheduleManager gradeSchedule;  // NEW: Grade schedule

////        // Convert DXF coordinates to meters
////        // Z elevations come from StoryManager and are ALREADY in meters - NO CONVERSION NEEDED
////        private const double X_TO_M = 0.001;
////        private const double Y_TO_M = 0.001;

////        private double MX(double xValue) => xValue * X_TO_M;
////        private double MY(double yValue) => yValue * Y_TO_M;

////        // Store available beam sections from template
////        private static Dictionary<string, BeamSectionInfo> availableBeamSections =
////            new Dictionary<string, BeamSectionInfo>();

////        private class BeamSectionInfo
////        {
////            public string SectionName { get; set; }
////            public int WidthMm { get; set; }
////            public int DepthMm { get; set; }
////            public string Grade { get; set; }
////        }

////        public BeamImporterEnhanced(
////            cSapModel model,
////            DxfDocument doc,
////            string zone,
////            int typicalFloors,
////            Dictionary<string, int> depths,
////            GradeScheduleManager gradeManager = null)  // NEW: Optional grade manager
////        {
////            sapModel = model;
////            dxfDoc = doc;
////            seismicZone = zone;
////            totalTypicalFloors = typicalFloors;
////            beamDepths = depths;
////            gradeSchedule = gradeManager;  // NEW

////            // Load available beam sections from template
////            LoadAvailableBeamSections();
////        }

////        /// <summary>
////        /// Load available beam sections from ETABS template
////        /// Parses section names like B20X75M35, B20X40M30, etc.
////        /// Format: B[Width_cm]X[Depth_cm]M[Grade]
////        /// </summary>
////        private void LoadAvailableBeamSections()
////        {
////            if (availableBeamSections.Count > 0) return; // Already loaded

////            try
////            {
////                availableBeamSections.Clear();

////                int numSections = 0;
////                string[] sectionNames = null;

////                int ret = sapModel.PropFrame.GetNameList(ref numSections, ref sectionNames);

////                if (ret == 0 && sectionNames != null)
////                {
////                    // Regex to parse beam section names: B20X75M35, B20X67.5M40, etc.
////                    // Pattern: B followed by width, X, depth, M, and grade
////                    // Width and depth are in centimeters (can have decimals like 67.5)
////                    Regex beamPattern = new Regex(@"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
////                        RegexOptions.IgnoreCase);

////                    foreach (string sectionName in sectionNames)
////                    {
////                        Match match = beamPattern.Match(sectionName);

////                        if (match.Success)
////                        {
////                            // Extract dimensions in centimeters
////                            double widthCm = double.Parse(match.Groups[1].Value);
////                            double depthCm = double.Parse(match.Groups[2].Value);
////                            string grade = match.Groups[3].Value;

////                            // Convert to millimeters
////                            int widthMm = (int)Math.Round(widthCm * 10);
////                            int depthMm = (int)Math.Round(depthCm * 10);

////                            availableBeamSections[sectionName] = new BeamSectionInfo
////                            {
////                                SectionName = sectionName,
////                                WidthMm = widthMm,
////                                DepthMm = depthMm,
////                                Grade = grade
////                            };

////                            System.Diagnostics.Debug.WriteLine(
////                                $"Loaded beam: {sectionName} = {widthMm}x{depthMm}mm (M{grade})");
////                        }
////                    }
////                }

////                System.Diagnostics.Debug.WriteLine(
////                    $"\n✓ Loaded {availableBeamSections.Count} beam sections from template");

////                if (availableBeamSections.Count == 0)
////                {
////                    throw new InvalidOperationException(
////                        "No beam sections found in template. Please ensure template has beam sections defined (e.g., B20X75M35).");
////                }
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"❌ Error loading beam sections: {ex.Message}");
////                throw;
////            }
////        }

////        /// <summary>
////        /// Get gravity beam width based on seismic zone
////        /// Zone II/III: 200mm
////        /// Zone IV/V: 240mm
////        /// </summary>
////        private int GetGravityBeamWidth()
////        {
////            return (seismicZone == "Zone II" || seismicZone == "Zone III") ? 200 : 240;
////        }

////        /// <summary>
////        /// Get main beam width based on adjacent wall thickness
////        /// </summary>
////        private int GetMainBeamWidth(WallThicknessCalculator.WallType wallType)
////        {
////            // Get wall thickness for this wall type
////            int wallThickness = WallThicknessCalculator.GetRecommendedThickness(
////                totalTypicalFloors,
////                wallType,
////                seismicZone,
////                2.0, // normal wall length
////                false); // not floating

////            return wallThickness;
////        }

////        /// <summary>
////        /// Find closest matching beam section from template
////        /// </summary>
////        private string GetClosestBeamSection(int requiredWidth, int requiredDepth, string preferredGrade = null)
////        {
////            if (availableBeamSections.Count == 0)
////            {
////                throw new InvalidOperationException(
////                    "No beam sections loaded from template. Ensure template has beam sections defined.");
////            }

////            string bestMatch = null;
////            int minDifference = int.MaxValue;

////            // ⭐ CRITICAL FIX: Remove "M" prefix from preferredGrade for comparison
////            // GradeScheduleManager returns "M30", but section.Grade is stored as "30"
////            string gradeToMatch = preferredGrade?.Replace("M", "").Replace("m", "").Trim();

////            // First try to find match with preferred grade
////            if (!string.IsNullOrEmpty(gradeToMatch))
////            {
////                foreach (var kvp in availableBeamSections)
////                {
////                    var section = kvp.Value;

////                    // Only consider sections with the preferred grade
////                    if (section.Grade == gradeToMatch)
////                    {
////                        // Calculate difference (prioritize depth match over width)
////                        int depthDiff = Math.Abs(section.DepthMm - requiredDepth);
////                        int widthDiff = Math.Abs(section.WidthMm - requiredWidth);
////                        int totalDiff = (depthDiff * 2) + widthDiff; // Depth is more important

////                        if (totalDiff < minDifference)
////                        {
////                            minDifference = totalDiff;
////                            bestMatch = kvp.Key;
////                        }
////                    }
////                }

////                if (bestMatch != null)
////                {
////                    var matchedSection = availableBeamSections[bestMatch];
////                    System.Diagnostics.Debug.WriteLine(
////                        $"  Required: {requiredWidth}x{requiredDepth}mm M{gradeToMatch} → Using: {bestMatch} " +
////                        $"({matchedSection.WidthMm}x{matchedSection.DepthMm}mm M{matchedSection.Grade})");
////                    return bestMatch;
////                }
////            }

////            // Fallback: Find closest match without grade preference
////            minDifference = int.MaxValue;
////            foreach (var kvp in availableBeamSections)
////            {
////                var section = kvp.Value;

////                // Calculate difference (prioritize depth match over width)
////                int depthDiff = Math.Abs(section.DepthMm - requiredDepth);
////                int widthDiff = Math.Abs(section.WidthMm - requiredWidth);
////                int totalDiff = (depthDiff * 2) + widthDiff; // Depth is more important

////                if (totalDiff < minDifference)
////                {
////                    minDifference = totalDiff;
////                    bestMatch = kvp.Key;
////                }
////            }

////            if (bestMatch == null)
////            {
////                // If still no match, list available sections
////                System.Diagnostics.Debug.WriteLine($"⚠️ No beam section found for {requiredWidth}x{requiredDepth}mm");
////                System.Diagnostics.Debug.WriteLine("Available sections:");
////                foreach (var kvp in availableBeamSections.Take(5))
////                {
////                    System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value.WidthMm}x{kvp.Value.DepthMm}mm");
////                }

////                throw new InvalidOperationException(
////                    $"No suitable beam section found for {requiredWidth}x{requiredDepth}mm. " +
////                    $"Available widths: {string.Join(", ", availableBeamSections.Select(s => s.Value.WidthMm).Distinct().OrderBy(w => w))}mm");
////            }

////            var matchedSection2 = availableBeamSections[bestMatch];
////            System.Diagnostics.Debug.WriteLine(
////                $"  Required: {requiredWidth}x{requiredDepth}mm → Using: {bestMatch} " +
////                $"({matchedSection2.WidthMm}x{matchedSection2.DepthMm}mm M{matchedSection2.Grade})");

////            return bestMatch;
////        }

////        /// <summary>
////        /// Determine beam section based on layer name and beam configuration
////        /// </summary>
////        private string DetermineBeamSection(string layerName, string preferredGrade)
////        {
////            string upper = layerName.ToUpperInvariant();

////            // GRAVITY BEAMS (Width based on seismic zone)
////            int gravityWidth = GetGravityBeamWidth();

////            // B-Internal gravity beams
////            if (upper.Contains("INTERNAL") && upper.Contains("GRAVITY"))
////            {
////                return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
////            }

////            // B-Cantilever Gravity Beams
////            if (upper.Contains("CANTILEVER") && upper.Contains("GRAVITY"))
////            {
////                return GetClosestBeamSection(gravityWidth, beamDepths["CantileverGravity"], preferredGrade);
////            }

////            // MAIN BEAMS (Width based on adjacent wall thickness)

////            // B-Core Main Beam
////            if (upper.Contains("CORE") && upper.Contains("MAIN"))
////            {
////                int coreWallWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.CoreWall);
////                return GetClosestBeamSection(coreWallWidth, beamDepths["CoreMain"], preferredGrade);
////            }

////            // B-Peripheral dead Main Beams
////            if (upper.Contains("PERIPHERAL") && upper.Contains("DEAD") && upper.Contains("MAIN"))
////            {
////                int peripheralDeadWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.PeripheralDeadWall);
////                return GetClosestBeamSection(peripheralDeadWidth, beamDepths["PeripheralDeadMain"], preferredGrade);
////            }

////            // B-Peripheral Portal Main Beams
////            if (upper.Contains("PERIPHERAL") && upper.Contains("PORTAL") && upper.Contains("MAIN"))
////            {
////                int peripheralPortalWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.PeripheralPortalWall);
////                return GetClosestBeamSection(peripheralPortalWidth, beamDepths["PeripheralPortalMain"], preferredGrade);
////            }

////            // B-Internal Main beams
////            if (upper.Contains("INTERNAL") && upper.Contains("MAIN"))
////            {
////                int internalWallWidth = GetMainBeamWidth(WallThicknessCalculator.WallType.InternalWall);
////                return GetClosestBeamSection(internalWallWidth, beamDepths["InternalMain"], preferredGrade);
////            }

////            // Generic beam detection (fallback)
////            if (upper.Contains("BEAM") || upper.StartsWith("B-"))
////            {
////                System.Diagnostics.Debug.WriteLine($"⚠️ Generic beam layer '{layerName}', using default gravity beam");
////                return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
////            }

////            // Default fallback - use gravity beam
////            System.Diagnostics.Debug.WriteLine($"⚠️ Unknown beam layer '{layerName}', using default gravity beam");
////            return GetClosestBeamSection(gravityWidth, beamDepths["InternalGravity"], preferredGrade);
////        }

////        public void ImportBeams(Dictionary<string, string> layerMapping, double elevation, int story)
////        {
////            var beamLayers = layerMapping
////                .Where(x => x.Value == "Beam")
////                .Select(x => x.Key)
////                .ToList();

////            if (beamLayers.Count == 0) return;

////            // NEW: Get beam grade for this story (0.7x wall grade)
////            string beamGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

////            System.Diagnostics.Debug.WriteLine($"\n========== IMPORTING BEAMS - Story {story} ==========");
////            System.Diagnostics.Debug.WriteLine($"Seismic Zone: {seismicZone}");
////            System.Diagnostics.Debug.WriteLine($"Gravity Beam Width: {GetGravityBeamWidth()}mm");

////            if (!string.IsNullOrEmpty(beamGrade))
////            {
////                System.Diagnostics.Debug.WriteLine($"Beam Concrete Grade: {beamGrade}");
////            }

////            System.Diagnostics.Debug.WriteLine($"Beam Elevation: {elevation:F3}m (already in meters - no conversion)");

////            int totalBeamsCreated = 0;

////            foreach (string layerName in beamLayers)
////            {
////                string section = DetermineBeamSection(layerName, beamGrade);
////                int beamCount = 0;

////                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName}");

////                foreach (netDxf.Entities.Line line in dxfDoc.Entities.Lines
////                    .Where(l => l.Layer.Name == layerName))
////                {
////                    CreateBeamFromLine(line, elevation, section, story);
////                    beamCount++;
////                }

////                foreach (Polyline2D poly in dxfDoc.Entities.Polylines2D
////                    .Where(p => p.Layer.Name == layerName))
////                {
////                    beamCount += CreateBeamFromPolyline(poly, elevation, section, story);
////                }

////                System.Diagnostics.Debug.WriteLine($"  ✓ Created {beamCount} beams");
////                totalBeamsCreated += beamCount;
////            }

////            System.Diagnostics.Debug.WriteLine($"\nTotal beams created: {totalBeamsCreated}");
////            System.Diagnostics.Debug.WriteLine($"=========================================\n");
////        }

////        private void CreateBeamFromLine(netDxf.Entities.Line line, double elevation,
////            string section, int story)
////        {
////            string frameName = "";
////            string storyName = GetStoryName(story);

////            // CORRECTED: elevation is ALREADY in meters - NO CONVERSION
////            sapModel.FrameObj.AddByCoord(
////                MX(line.StartPoint.X),
////                MY(line.StartPoint.Y),
////                elevation,  // ← No conversion
////                MX(line.EndPoint.X),
////                MY(line.EndPoint.Y),
////                elevation,  // ← No conversion
////                ref frameName, section, storyName);
////        }

////        private int CreateBeamFromPolyline(Polyline2D poly, double elevation,
////            string section, int story)
////        {
////            string storyName = GetStoryName(story);
////            var vertices = poly.Vertexes;
////            int count = 0;

////            // CORRECTED: elevation is ALREADY in meters - NO CONVERSION
////            for (int i = 0; i < vertices.Count - 1; i++)
////            {
////                string frameName = "";
////                sapModel.FrameObj.AddByCoord(
////                    MX(vertices[i].Position.X), MY(vertices[i].Position.Y), elevation,
////                    MX(vertices[i + 1].Position.X), MY(vertices[i + 1].Position.Y), elevation,
////                    ref frameName, section, storyName);
////                count++;
////            }

////            if (poly.IsClosed && vertices.Count > 2)
////            {
////                string frameName = "";
////                sapModel.FrameObj.AddByCoord(
////                    MX(vertices[vertices.Count - 1].Position.X),
////                    MY(vertices[vertices.Count - 1].Position.Y), elevation,
////                    MX(vertices[0].Position.X), MY(vertices[0].Position.Y), elevation,
////                    ref frameName, section, storyName);
////                count++;
////            }

////            return count;
////        }



////        private string GetStoryName(int story)
////        {
////            try
////            {
////                int numStories = 0;
////                string[] storyNames = null;

////                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

////                // ✅ FIXED: Use story index directly (0-based)
////                if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
////                {
////                    return storyNames[story];  // No subtraction
////                }
////            }
////            catch { }

////            return story == 0 ? "Base" : $"Story{story + 1}";
////        }
////    }
////}

//// ============================================================================
//// FILE: Importers/BeamImporterEnhanced.cs
//// VERSION: 3.0 — Full beam layer catalogue per specification
//// ============================================================================
//// Beam layers:
////   B-Internal Gravity Beams      → gravity, width 200/240
////   B-Cantilever Gravity Beams    → gravity, width 200/240
////   B-No load Gravity Beams       → gravity, width 200/240, wall load = 0
////   B-Edeck Gravity Beams         → gravity, width 200/240
////   B-Podium Gravity Beams        → gravity, width 200/240
////   B-Ground Gravity Beams        → gravity, width 200/240
////   B-Basement Gravity Beams      → gravity, width 200/240
////   B-Core Main Beams             → main, width = Core wall thickness  (MB prefix)
////   B-Peripheral dead Main Beams  → main, width = Periph dead wall     (MB prefix)
////   B-Peripheral Portal Main Beams→ main, width = Periph portal wall   (MB prefix)
////   B-Internal Main Beams         → main, width = Internal wall        (MB prefix)
////
//// Note: Main beam sections in ETABS start with "MB"
//// ============================================================================

//using ETAB_Automation.Core;
//using ETABSv1;
//using netDxf;
//using netDxf.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace ETABS_CAD_Automation.Importers
//{
//    public class BeamImporterEnhanced
//    {
//        private readonly cSapModel sapModel;
//        private readonly DxfDocument dxfDoc;
//        private readonly string seismicZone;
//        private readonly int totalTypicalFloors;
//        private readonly Dictionary<string, int> beamDepths;
//        private readonly GradeScheduleManager gradeSchedule;

//        private const double X_TO_M = 0.001;
//        private const double Y_TO_M = 0.001;
//        private double MX(double x) => x * X_TO_M;
//        private double MY(double y) => y * Y_TO_M;

//        // ====================================================================
//        // SECTION CACHE  (loaded once from ETABS template)
//        // ====================================================================

//        // Gravity beam sections: prefix "B" e.g. B20X75M35
//        private static Dictionary<string, GravityBeamInfo> gravityBeamSections =
//            new Dictionary<string, GravityBeamInfo>();

//        // Main beam sections: prefix "MB" e.g. MB25X75M35
//        private static Dictionary<string, MainBeamInfo> mainBeamSections =
//            new Dictionary<string, MainBeamInfo>();

//        private class GravityBeamInfo
//        {
//            public string SectionName { get; set; }
//            public int WidthMm { get; set; }
//            public int DepthMm { get; set; }
//            public string Grade { get; set; }
//        }

//        private class MainBeamInfo
//        {
//            public string SectionName { get; set; }
//            public int WidthMm { get; set; }
//            public int DepthMm { get; set; }
//            public string Grade { get; set; }
//        }

//        // ====================================================================
//        // CONSTRUCTOR
//        // ====================================================================

//        public BeamImporterEnhanced(
//            cSapModel model,
//            DxfDocument doc,
//            string zone,
//            int typicalFloors,
//            Dictionary<string, int> depths,
//            GradeScheduleManager gradeManager = null)
//        {
//            sapModel = model;
//            dxfDoc = doc;
//            seismicZone = zone;
//            totalTypicalFloors = typicalFloors;
//            beamDepths = depths;
//            gradeSchedule = gradeManager;
//            LoadBeamSections();
//        }

//        // ====================================================================
//        // SECTION LOADING
//        // ====================================================================

//        private void LoadBeamSections()
//        {
//            if (gravityBeamSections.Count > 0 && mainBeamSections.Count > 0) return;

//            gravityBeamSections.Clear();
//            mainBeamSections.Clear();

//            try
//            {
//                int n = 0; string[] names = null;
//                int ret = sapModel.PropFrame.GetNameList(ref n, ref names);
//                if (ret != 0 || names == null) return;

//                // Gravity beams: B20X75M35  (B + width_cm + X + depth_cm + M + grade)
//                var grav = new Regex(@"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
//                    RegexOptions.IgnoreCase);

//                // Main beams: MB25X75M35  (MB + width_cm + X + depth_cm + M + grade)
//                var main = new Regex(@"^MB(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
//                    RegexOptions.IgnoreCase);

//                foreach (string name in names)
//                {
//                    var mg = main.Match(name);
//                    if (mg.Success)
//                    {
//                        mainBeamSections[name] = new MainBeamInfo
//                        {
//                            SectionName = name,
//                            WidthMm = (int)Math.Round(double.Parse(mg.Groups[1].Value) * 10),
//                            DepthMm = (int)Math.Round(double.Parse(mg.Groups[2].Value) * 10),
//                            Grade = mg.Groups[3].Value
//                        };
//                        continue;
//                    }

//                    var gg = grav.Match(name);
//                    if (gg.Success)
//                    {
//                        gravityBeamSections[name] = new GravityBeamInfo
//                        {
//                            SectionName = name,
//                            WidthMm = (int)Math.Round(double.Parse(gg.Groups[1].Value) * 10),
//                            DepthMm = (int)Math.Round(double.Parse(gg.Groups[2].Value) * 10),
//                            Grade = gg.Groups[3].Value
//                        };
//                    }
//                }

//                System.Diagnostics.Debug.WriteLine(
//                    $"✓ Loaded {gravityBeamSections.Count} gravity + " +
//                    $"{mainBeamSections.Count} main beam sections");

//                if (gravityBeamSections.Count == 0 && mainBeamSections.Count == 0)
//                    throw new InvalidOperationException(
//                        "No beam sections found in template. " +
//                        "Expected B__X__M__ (gravity) and MB__X__M__ (main) format.");
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ LoadBeamSections: {ex.Message}");
//                throw;
//            }
//        }

//        // ====================================================================
//        // SECTION SELECTION HELPERS
//        // ====================================================================

//        private int GravityBeamWidth()
//            => (seismicZone.Contains("II") || seismicZone.Contains("III")) ? 200 : 240;

//        private int WallThickness(WallThicknessCalculator.WallType wt)
//            => WallThicknessCalculator.GetRecommendedThickness(
//                totalTypicalFloors, wt, seismicZone, 2.0, false);

//        private string BestGravitySection(int reqWidth, int reqDepth, string grade)
//        {
//            string gradeNum = grade?.Replace("M", "").Replace("m", "").Trim();
//            string best = null; int minDiff = int.MaxValue;

//            // Grade-matched first
//            if (!string.IsNullOrEmpty(gradeNum))
//            {
//                foreach (var kvp in gravityBeamSections)
//                {
//                    if (kvp.Value.Grade != gradeNum) continue;
//                    int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                             + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                    if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
//                }
//                if (best != null) return best;
//            }

//            minDiff = int.MaxValue;
//            foreach (var kvp in gravityBeamSections)
//            {
//                int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                         + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
//            }

//            return best ?? throw new InvalidOperationException(
//                $"No gravity beam section for {reqWidth}x{reqDepth}mm");
//        }

//        private string BestMainSection(int reqWidth, int reqDepth, string grade)
//        {
//            string gradeNum = grade?.Replace("M", "").Replace("m", "").Trim();
//            string best = null; int minDiff = int.MaxValue;

//            if (!string.IsNullOrEmpty(gradeNum))
//            {
//                foreach (var kvp in mainBeamSections)
//                {
//                    if (kvp.Value.Grade != gradeNum) continue;
//                    int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                             + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                    if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
//                }
//                if (best != null) return best;
//            }

//            minDiff = int.MaxValue;
//            foreach (var kvp in mainBeamSections)
//            {
//                int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                         + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
//            }

//            // Fallback to gravity if no main beam sections loaded
//            if (best == null && gravityBeamSections.Count > 0)
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    $"⚠ No MB sections found – falling back to gravity sections for main beam");
//                return BestGravitySection(reqWidth, reqDepth, grade);
//            }

//            return best ?? throw new InvalidOperationException(
//                $"No main beam section (MB) for {reqWidth}x{reqDepth}mm");
//        }

//        // ====================================================================
//        // LAYER → SECTION MAPPING
//        // ====================================================================

//        private enum BeamCategory { Gravity, Main }

//        private (string section, BeamCategory cat) DetermineBeamSection(
//            string layerName, string grade)
//        {
//            string u = layerName.ToUpperInvariant();
//            int gw = GravityBeamWidth();

//            // ── MAIN BEAMS (MB sections) ──────────────────────────────────

//            if ((u.Contains("CORE") && u.Contains("MAIN")) ||
//                 u.Contains("B-CORE MAIN"))
//            {
//                int w = WallThickness(WallThicknessCalculator.WallType.CoreWall);
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("CoreMain", 600),
//                    grade), BeamCategory.Main);
//            }

//            if (u.Contains("PERIPHERAL") && u.Contains("DEAD") && u.Contains("MAIN"))
//            {
//                int w = WallThickness(WallThicknessCalculator.WallType.PeripheralDeadWall);
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralDeadMain", 600),
//                    grade), BeamCategory.Main);
//            }

//            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL") && u.Contains("MAIN"))
//            {
//                int w = WallThickness(WallThicknessCalculator.WallType.PeripheralPortalWall);
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralPortalMain", 650),
//                    grade), BeamCategory.Main);
//            }

//            if (u.Contains("INTERNAL") && u.Contains("MAIN"))
//            {
//                int w = WallThickness(WallThicknessCalculator.WallType.InternalWall);
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("InternalMain", 550),
//                    grade), BeamCategory.Main);
//            }

//            // ── GRAVITY BEAMS (B sections) ────────────────────────────────

//            // Cantilever gravity
//            if (u.Contains("CANTILEVER") && u.Contains("GRAVITY"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("CantileverGravity", 500), grade),
//                    BeamCategory.Gravity);

//            // All named gravity variants
//            if (u.Contains("INTERNAL") && u.Contains("GRAVITY"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
//                    BeamCategory.Gravity);

//            if (u.Contains("NO LOAD") || u.Contains("NOLOAD"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
//                    BeamCategory.Gravity);

//            if (u.Contains("EDECK") || u.Contains("E-DECK") || u.Contains("E DECK"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("EdeckGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                    BeamCategory.Gravity);

//            if (u.Contains("PODIUM"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("PodiumGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                    BeamCategory.Gravity);

//            if (u.Contains("GROUND"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("GroundGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                    BeamCategory.Gravity);

//            if (u.Contains("BASEMENT"))
//                return (BestGravitySection(gw,
//                    beamDepths.GetValueOrDefault("BasementGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                    BeamCategory.Gravity);

//            // Generic fallback
//            System.Diagnostics.Debug.WriteLine(
//                $"⚠ Unknown beam layer '{layerName}' — using internal gravity");
//            return (BestGravitySection(gw,
//                beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
//                BeamCategory.Gravity);
//        }

//        // ====================================================================
//        // PUBLIC IMPORT METHOD
//        // ====================================================================

//        public void ImportBeams(Dictionary<string, string> layerMapping,
//            double elevation, int story)
//        {
//            var beamLayers = layerMapping
//                .Where(x => x.Value == "Beam")
//                .Select(x => x.Key)
//                .ToList();

//            if (beamLayers.Count == 0) return;

//            string beamGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

//            System.Diagnostics.Debug.WriteLine(
//                $"\n========== IMPORTING BEAMS - Story {story} ==========");
//            System.Diagnostics.Debug.WriteLine(
//                $"Zone: {seismicZone} | Gravity width: {GravityBeamWidth()}mm | " +
//                $"Grade: {beamGrade ?? "default"} | Elev: {elevation:F3}m");

//            int total = 0;

//            foreach (string layerName in beamLayers)
//            {
//                var (section, cat) = DetermineBeamSection(layerName, beamGrade);
//                int cnt = 0;

//                System.Diagnostics.Debug.WriteLine(
//                    $"\nLayer: {layerName} [{cat}] → {section}");

//                foreach (var line in dxfDoc.Entities.Lines
//                    .Where(l => l.Layer.Name == layerName))
//                {
//                    CreateBeamFromLine(line, elevation, section, story);
//                    cnt++;
//                }

//                foreach (var poly in dxfDoc.Entities.Polylines2D
//                    .Where(p => p.Layer.Name == layerName))
//                    cnt += CreateBeamFromPolyline(poly, elevation, section, story);

//                System.Diagnostics.Debug.WriteLine($"  ✓ {cnt} beams");
//                total += cnt;
//            }

//            System.Diagnostics.Debug.WriteLine($"\nTotal beams: {total}");
//        }

//        // ====================================================================
//        // GEOMETRY CREATION
//        // ====================================================================

//        private void CreateBeamFromLine(netDxf.Entities.Line line,
//            double elevation, string section, int story)
//        {
//            string name = "";
//            sapModel.FrameObj.AddByCoord(
//                MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation,
//                MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation,
//                ref name, section, GetStoryName(story));
//        }

//        private int CreateBeamFromPolyline(Polyline2D poly,
//            double elevation, string section, int story)
//        {
//            string storyName = GetStoryName(story);
//            var verts = poly.Vertexes;
//            int cnt = 0;

//            for (int i = 0; i < verts.Count - 1; i++)
//            {
//                string name = "";
//                sapModel.FrameObj.AddByCoord(
//                    MX(verts[i].Position.X), MY(verts[i].Position.Y), elevation,
//                    MX(verts[i + 1].Position.X), MY(verts[i + 1].Position.Y), elevation,
//                    ref name, section, storyName);
//                cnt++;
//            }

//            if (poly.IsClosed && verts.Count > 2)
//            {
//                string name = "";
//                sapModel.FrameObj.AddByCoord(
//                    MX(verts[verts.Count - 1].Position.X),
//                    MY(verts[verts.Count - 1].Position.Y), elevation,
//                    MX(verts[0].Position.X), MY(verts[0].Position.Y), elevation,
//                    ref name, section, storyName);
//                cnt++;
//            }

//            return cnt;
//        }

//        private string GetStoryName(int story)
//        {
//            try
//            {
//                int n = 0; string[] names = null;
//                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
//                    names != null && story >= 0 && story < names.Length)
//                    return names[story];
//            }
//            catch { }
//            return story == 0 ? "Base" : $"Story{story + 1}";
//        }
//    }

//    // ====================================================================
//    // EXTENSION HELPER
//    // ====================================================================

//    internal static class DictExtensions
//    {
//        public static TValue GetValueOrDefault<TKey, TValue>(
//            this Dictionary<TKey, TValue> dict, TKey key,
//            TValue defaultValue = default)
//        {
//            return dict.TryGetValue(key, out TValue val) ? val : defaultValue;
//        }
//    }
//}
// ============================================================================
// FILE: Importers/BeamImporterEnhanced.cs
// VERSION: 3.1 — Width overrides wired in; NoLoadGravity depth key fixed
// ============================================================================
//
// CHANGES FROM v3.0:
//   [FIX-1] Constructor now accepts beamWidthOverrides (was silently ignored)
//   [FIX-2] GravityBeamWidth() checks widthOverrides["GravityWidth"] > 0 first
//   [FIX-3] Main beam width: checks per-type override before GPL calc
//             CoreMainWidth, PeripheralDeadMainWidth, PeripheralPortalMainWidth,
//             InternalMainWidth — all wired in
//   [FIX-4] B-No Load layer now reads "NoLoadGravity" key (was falling back to
//             "InternalGravity" — silently ignoring the user's NoLoad depth input)
//   (CADImporterEnhanced must also pass floorConfig.BeamWidthOverrides — see that file)
//
// BEAM LAYERS:
//   GRAVITY (B__ sections):
//     B-Internal Gravity Beams       → depth key: InternalGravity
//     B-Cantilever Gravity Beams     → depth key: CantileverGravity
//     B-No Load Gravity Beams        → depth key: NoLoadGravity      ← FIXED
//     B-Edeck Gravity Beams          → depth key: EdeckGravity
//     B-Podium Gravity Beams         → depth key: PodiumGravity
//     B-Ground Gravity Beams         → depth key: GroundGravity
//     B-BasementN Gravity Beams      → depth key: BasementGravity
//   Width = GravityWidth override if >0, else zone auto (200/240mm)
//
//   MAIN (MB__ sections):
//     B-Core Main Beams              → depth key: CoreMain
//     B-Peripheral Dead Main Beams   → depth key: PeripheralDeadMain
//     B-Peripheral Portal Main Beams → depth key: PeripheralPortalMain
//     B-Internal Main Beams          → depth key: InternalMain
//   Width = per-type override if >0, else GPL wall thickness
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
    public class BeamImporterEnhanced
    {
        private readonly cSapModel sapModel;
        private readonly DxfDocument dxfDoc;
        private readonly string seismicZone;
        private readonly int totalTypicalFloors;
        private readonly Dictionary<string, int> beamDepths;
        private readonly Dictionary<string, int> beamWidthOverrides;   // [FIX-1]
        private readonly GradeScheduleManager gradeSchedule;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        // ====================================================================
        // SECTION CACHE  (static — loaded once per process from ETABS template)
        // ====================================================================

        // Gravity beam sections: prefix "B"  e.g. B20X75M35
        private static Dictionary<string, GravityBeamInfo> gravityBeamSections =
            new Dictionary<string, GravityBeamInfo>();

        // Main beam sections: prefix "MB" e.g. MB25X75M35
        private static Dictionary<string, MainBeamInfo> mainBeamSections =
            new Dictionary<string, MainBeamInfo>();

        private class GravityBeamInfo
        {
            public string SectionName { get; set; }
            public int WidthMm { get; set; }
            public int DepthMm { get; set; }
            public string Grade { get; set; }
        }

        private class MainBeamInfo
        {
            public string SectionName { get; set; }
            public int WidthMm { get; set; }
            public int DepthMm { get; set; }
            public string Grade { get; set; }
        }

        // ====================================================================
        // CONSTRUCTOR  — [FIX-1] added beamWidthOverrides parameter
        // ====================================================================

        /// <param name="depths">
        ///   Beam depth dictionary from FloorTypeConfig.BeamDepths.
        ///   Keys: InternalGravity, CantileverGravity, NoLoadGravity, EdeckGravity,
        ///         PodiumGravity, GroundGravity, BasementGravity,
        ///         CoreMain, PeripheralDeadMain, PeripheralPortalMain, InternalMain
        /// </param>
        /// <param name="widthOverrides">
        ///   Beam width override dictionary from FloorTypeConfig.BeamWidthOverrides.
        ///   Keys: GravityWidth, CoreMainWidth, PeripheralDeadMainWidth,
        ///         PeripheralPortalMainWidth, InternalMainWidth
        ///   Value = 0 means "use auto rule" (zone default / GPL wall thickness).
        /// </param>
        public BeamImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            string zone,
            int typicalFloors,
            Dictionary<string, int> depths,
            GradeScheduleManager gradeManager = null,
            Dictionary<string, int> widthOverrides = null)   // [FIX-1]
        {
            sapModel = model;
            dxfDoc = doc;
            seismicZone = zone;
            totalTypicalFloors = typicalFloors;
            beamDepths = depths ?? new Dictionary<string, int>();
            gradeSchedule = gradeManager;
            // [FIX-1] store overrides; use empty dict if caller doesn't provide them
            beamWidthOverrides = widthOverrides ?? new Dictionary<string, int>();

            LoadBeamSections();
        }

        // ====================================================================
        // SECTION LOADING
        // ====================================================================

        private void LoadBeamSections()
        {
            if (gravityBeamSections.Count > 0 && mainBeamSections.Count > 0) return;

            gravityBeamSections.Clear();
            mainBeamSections.Clear();

            try
            {
                int n = 0; string[] names = null;
                int ret = sapModel.PropFrame.GetNameList(ref n, ref names);
                if (ret != 0 || names == null) return;

                // Main beams: MB25X75M35  (must be tested BEFORE gravity pattern
                //             because "^B" would also match "MB" after trimming)
                var mainPat = new Regex(@"^MB(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
                    RegexOptions.IgnoreCase);

                // Gravity beams: B20X75M35
                var gravPat = new Regex(@"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
                    RegexOptions.IgnoreCase);

                foreach (string name in names)
                {
                    var mg = mainPat.Match(name);
                    if (mg.Success)
                    {
                        mainBeamSections[name] = new MainBeamInfo
                        {
                            SectionName = name,
                            WidthMm = (int)Math.Round(double.Parse(mg.Groups[1].Value) * 10),
                            DepthMm = (int)Math.Round(double.Parse(mg.Groups[2].Value) * 10),
                            Grade = mg.Groups[3].Value
                        };
                        continue;   // don't also match as gravity
                    }

                    var gg = gravPat.Match(name);
                    if (gg.Success)
                    {
                        gravityBeamSections[name] = new GravityBeamInfo
                        {
                            SectionName = name,
                            WidthMm = (int)Math.Round(double.Parse(gg.Groups[1].Value) * 10),
                            DepthMm = (int)Math.Round(double.Parse(gg.Groups[2].Value) * 10),
                            Grade = gg.Groups[3].Value
                        };
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"✓ Beam sections loaded: {gravityBeamSections.Count} gravity (B__), " +
                    $"{mainBeamSections.Count} main (MB__)");

                if (gravityBeamSections.Count == 0 && mainBeamSections.Count == 0)
                    throw new InvalidOperationException(
                        "No beam sections found in ETABS template.\n" +
                        "Expected format: B20X75M35 (gravity) and MB25X75M35 (main).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadBeamSections: {ex.Message}");
                throw;
            }
        }

        // ====================================================================
        // WIDTH RESOLUTION HELPERS  — [FIX-2] [FIX-3]
        // ====================================================================

        /// <summary>
        /// Gravity beam width: user override first, then zone auto-rule.
        /// [FIX-2] previously always used zone auto-rule, ignoring UI input.
        /// </summary>
        private int GravityBeamWidth()
        {
            // [FIX-2] honour user override (GravityWidth > 0 = explicit override)
            int ovr = beamWidthOverrides.GetValueOrDefault("GravityWidth", 0);
            if (ovr > 0) return ovr;

            // zone auto-rule: Zone II/III → 200 mm, Zone IV/V → 240 mm
            return (seismicZone.Contains("II") || seismicZone.Contains("III")) ? 200 : 240;
        }

        /// <summary>
        /// Main beam width: user override first, then GPL wall thickness.
        /// [FIX-3] previously always used GPL calc, ignoring UI width overrides.
        /// </summary>
        /// <param name="wt">Wall type whose GPL thickness is the fallback.</param>
        /// <param name="widthOverrideKey">Key in beamWidthOverrides dict.</param>
        private int MainBeamWidth(WallThicknessCalculator.WallType wt, string widthOverrideKey)
        {
            // [FIX-3] honour user override
            int ovr = beamWidthOverrides.GetValueOrDefault(widthOverrideKey, 0);
            if (ovr > 0) return ovr;

            // GPL fallback: wall thickness from IS 1893-2025 table
            return WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, wt, seismicZone, 2.0, false);
        }

        // ====================================================================
        // CLOSEST-SECTION FINDERS
        // ====================================================================

        private string BestGravitySection(int reqWidth, int reqDepth, string grade)
        {
            string gradeNum = NormalizeGrade(grade);
            string best = null;
            int minDiff = int.MaxValue;

            // Grade-preferred pass
            if (!string.IsNullOrEmpty(gradeNum))
            {
                foreach (var kvp in gravityBeamSections)
                {
                    if (kvp.Value.Grade != gradeNum) continue;
                    int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                             + Math.Abs(kvp.Value.WidthMm - reqWidth);
                    if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
                }
                if (best != null) return best;
            }

            // Any-grade fallback
            minDiff = int.MaxValue;
            foreach (var kvp in gravityBeamSections)
            {
                int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                         + Math.Abs(kvp.Value.WidthMm - reqWidth);
                if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
            }

            return best ?? throw new InvalidOperationException(
                $"No gravity beam section (B__) for {reqWidth}×{reqDepth}mm.");
        }

        private string BestMainSection(int reqWidth, int reqDepth, string grade)
        {
            string gradeNum = NormalizeGrade(grade);
            string best = null;
            int minDiff = int.MaxValue;

            if (!string.IsNullOrEmpty(gradeNum))
            {
                foreach (var kvp in mainBeamSections)
                {
                    if (kvp.Value.Grade != gradeNum) continue;
                    int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                             + Math.Abs(kvp.Value.WidthMm - reqWidth);
                    if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
                }
                if (best != null) return best;
            }

            minDiff = int.MaxValue;
            foreach (var kvp in mainBeamSections)
            {
                int diff = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                         + Math.Abs(kvp.Value.WidthMm - reqWidth);
                if (diff < minDiff) { minDiff = diff; best = kvp.Key; }
            }

            if (best == null && gravityBeamSections.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "⚠ No MB sections in template — falling back to gravity sections for main beam");
                return BestGravitySection(reqWidth, reqDepth, grade);
            }

            return best ?? throw new InvalidOperationException(
                $"No main beam section (MB__) for {reqWidth}×{reqDepth}mm.");
        }

        /// <summary>Strip "M"/"m" prefix from grade string for numeric comparison.</summary>
        private static string NormalizeGrade(string grade)
            => grade?.Replace("M", "").Replace("m", "").Trim();

        // ====================================================================
        // LAYER → SECTION MAPPING  — [FIX-4] NoLoadGravity key corrected
        // ====================================================================

        private enum BeamCategory { Gravity, Main }

        private (string section, BeamCategory cat) DetermineBeamSection(
            string layerName, string grade)
        {
            string u = layerName.ToUpperInvariant();
            int gw = GravityBeamWidth();   // [FIX-2] now respects override

            // ── MAIN BEAMS (MB__ sections) — test before generic "INTERNAL" ──

            if (u.Contains("CORE") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.CoreWall,
                                      "CoreMainWidth");   // [FIX-3]
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("CoreMain", 600), grade),
                    BeamCategory.Main);
            }

            if (u.Contains("PERIPHERAL") && u.Contains("DEAD") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.PeripheralDeadWall,
                                      "PeripheralDeadMainWidth");   // [FIX-3]
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("PeripheralDeadMain", 600), grade),
                    BeamCategory.Main);
            }

            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.PeripheralPortalWall,
                                      "PeripheralPortalMainWidth");   // [FIX-3]
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("PeripheralPortalMain", 650), grade),
                    BeamCategory.Main);
            }

            if (u.Contains("INTERNAL") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.InternalWall,
                                      "InternalMainWidth");   // [FIX-3]
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("InternalMain", 550), grade),
                    BeamCategory.Main);
            }

            // ── GRAVITY BEAMS (B__ sections) ─────────────────────────────────

            if (u.Contains("CANTILEVER") && u.Contains("GRAVITY"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("CantileverGravity", 500), grade),
                    BeamCategory.Gravity);

            // [FIX-4] "NO LOAD" now correctly uses "NoLoadGravity" key
            //         (v3.0 used "InternalGravity" so user's NoLoad depth was ignored)
            if (u.Contains("NO LOAD") || u.Contains("NOLOAD") || u.Contains("NO-LOAD"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("NoLoadGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                    BeamCategory.Gravity);

            // Named floor-type gravity variants (each falls back to InternalGravity)
            if (u.Contains("EDECK") || u.Contains("E-DECK") || u.Contains("E DECK"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("EdeckGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                    BeamCategory.Gravity);

            if (u.Contains("PODIUM"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("PodiumGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                    BeamCategory.Gravity);

            if (u.Contains("GROUND"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("GroundGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                    BeamCategory.Gravity);

            if (u.Contains("BASEMENT"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("BasementGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                    BeamCategory.Gravity);

            // "INTERNAL GRAVITY" — must come AFTER "INTERNAL MAIN" check above
            if (u.Contains("INTERNAL") && u.Contains("GRAVITY"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
                    BeamCategory.Gravity);

            // Generic gravity fallback
            System.Diagnostics.Debug.WriteLine(
                $"⚠ Unknown beam layer '{layerName}' → internal gravity fallback");
            return (BestGravitySection(gw,
                beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
                BeamCategory.Gravity);
        }

        // ====================================================================
        // PUBLIC IMPORT METHOD
        // ====================================================================

        public void ImportBeams(Dictionary<string, string> layerMapping,
            double elevation, int story)
        {
            var beamLayers = layerMapping
                .Where(x => x.Value == "Beam")
                .Select(x => x.Key)
                .ToList();

            if (beamLayers.Count == 0) return;

            string beamGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

            System.Diagnostics.Debug.WriteLine(
                $"\n========== IMPORTING BEAMS - Story {story} ==========");
            System.Diagnostics.Debug.WriteLine(
                $"Zone: {seismicZone}  |  Gravity width: {GravityBeamWidth()}mm  |  " +
                $"Grade: {beamGrade ?? "template default"}  |  Elev: {elevation:F3}m");

            // Log active width overrides for traceability
            if (beamWidthOverrides.Any(kv => kv.Value > 0))
            {
                System.Diagnostics.Debug.WriteLine("  Width overrides active:");
                foreach (var kv in beamWidthOverrides.Where(kv => kv.Value > 0))
                    System.Diagnostics.Debug.WriteLine($"    {kv.Key} = {kv.Value}mm");
            }

            int total = 0;

            foreach (string layerName in beamLayers)
            {
                var (section, cat) = DetermineBeamSection(layerName, beamGrade);
                int cnt = 0;

                System.Diagnostics.Debug.WriteLine(
                    $"\nLayer: {layerName} [{cat}] → {section}");

                foreach (var line in dxfDoc.Entities.Lines
                    .Where(l => l.Layer.Name == layerName))
                {
                    CreateBeamFromLine(line, elevation, section, story);
                    cnt++;
                }

                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                    cnt += CreateBeamFromPolyline(poly, elevation, section, story);

                System.Diagnostics.Debug.WriteLine($"  ✓ {cnt} beam(s)");
                total += cnt;
            }

            System.Diagnostics.Debug.WriteLine($"\nTotal beams this story: {total}");
        }

        // ====================================================================
        // GEOMETRY CREATION
        // ====================================================================

        private void CreateBeamFromLine(netDxf.Entities.Line line,
            double elevation, string section, int story)
        {
            string name = "";
            sapModel.FrameObj.AddByCoord(
                MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation,
                MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation,
                ref name, section, GetStoryName(story));
        }

        private int CreateBeamFromPolyline(Polyline2D poly,
            double elevation, string section, int story)
        {
            string storyName = GetStoryName(story);
            var verts = poly.Vertexes;
            int cnt = 0;

            for (int i = 0; i < verts.Count - 1; i++)
            {
                string name = "";
                sapModel.FrameObj.AddByCoord(
                    MX(verts[i].Position.X), MY(verts[i].Position.Y), elevation,
                    MX(verts[i + 1].Position.X), MY(verts[i + 1].Position.Y), elevation,
                    ref name, section, storyName);
                cnt++;
            }

            if (poly.IsClosed && verts.Count > 2)
            {
                string name = "";
                sapModel.FrameObj.AddByCoord(
                    MX(verts[verts.Count - 1].Position.X),
                    MY(verts[verts.Count - 1].Position.Y), elevation,
                    MX(verts[0].Position.X), MY(verts[0].Position.Y), elevation,
                    ref name, section, storyName);
                cnt++;
            }

            return cnt;
        }

        private string GetStoryName(int story)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
                    names != null && story >= 0 && story < names.Length)
                    return names[story];
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }

    // ====================================================================
    // EXTENSION HELPER  (safe GetValueOrDefault for .NET < 6)
    // ====================================================================

    internal static class DictExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue = default)
            => dict.TryGetValue(key, out TValue val) ? val : defaultValue;
    }
}
