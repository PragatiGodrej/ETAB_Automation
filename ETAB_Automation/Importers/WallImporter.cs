
//// ============================================================================
//// FILE: Importers/WallImporterEnhanced.cs (WITH GRADE SCHEDULE SUPPORT)
//// ============================================================================
//using ETAB_Automation.Core;

//using ETABSv1;
//using netDxf;
//using netDxf.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ETABS_CAD_Automation.Importers
//{
//    public class WallImporterEnhanced
//    {
//        private cSapModel sapModel;
//        private DxfDocument dxfDoc;
//        private double floorHeight;
//        private int totalTypicalFloors;
//        private string seismicZone;
//        private GradeScheduleManager gradeSchedule; // NEW: Grade schedule

//        // CRITICAL FIX: Separate conversion factors for X and Y
//        // Z elevations come from StoryManager and are ALREADY in meters - NO CONVERSION NEEDED
//        private const double X_TO_M = 0.001;  // Convert DXF X coordinates to meters
//        private const double Y_TO_M = 0.001;  // Convert DXF Y coordinates to meters

//        private double MX(double xValue) => xValue * X_TO_M;
//        private double MY(double yValue) => yValue * Y_TO_M;

//        private int wallsCreated = 0;
//        private int wallsFailed = 0;
//        private Dictionary<string, int> wallTypeCount = new Dictionary<string, int>();

//        public WallImporterEnhanced(
//            cSapModel model,
//            DxfDocument doc,
//            double height,
//            int typicalFloors,
//            string zone,
//            GradeScheduleManager gradeManager = null)  // NEW: Optional grade manager
//        {
//            sapModel = model;
//            dxfDoc = doc;
//            floorHeight = height;
//            totalTypicalFloors = typicalFloors;
//            seismicZone = zone;
//            gradeSchedule = gradeManager;  // NEW

//            // Diagnose coordinate system
//            DiagnoseCoordinateSystem();

//            // Load available wall sections from template
//            WallThicknessCalculator.LoadAvailableWallSections(sapModel);
//        }

//        /// <summary>
//        /// Diagnose what's happening with X and Y coordinates
//        /// </summary>
//        private void DiagnoseCoordinateSystem()
//        {
//            System.Diagnostics.Debug.WriteLine("\n========== COORDINATE SYSTEM DIAGNOSTICS ==========");

//            try
//            {
//                // Get first line from DXF
//                var testLine = dxfDoc.Entities.Lines.FirstOrDefault();
//                if (testLine != null)
//                {
//                    System.Diagnostics.Debug.WriteLine($"\nDXF Raw Coordinates:");
//                    System.Diagnostics.Debug.WriteLine($"  Start X: {testLine.StartPoint.X}, Y: {testLine.StartPoint.Y}");
//                    System.Diagnostics.Debug.WriteLine($"  End   X: {testLine.EndPoint.X}, Y: {testLine.EndPoint.Y}");

//                    double rawLengthX = Math.Abs(testLine.EndPoint.X - testLine.StartPoint.X);
//                    double rawLengthY = Math.Abs(testLine.EndPoint.Y - testLine.StartPoint.Y);

//                    System.Diagnostics.Debug.WriteLine($"\nRaw Lengths:");
//                    System.Diagnostics.Debug.WriteLine($"  X span: {rawLengthX}");
//                    System.Diagnostics.Debug.WriteLine($"  Y span: {rawLengthY}");

//                    System.Diagnostics.Debug.WriteLine($"\nAfter conversion with X_TO_M={X_TO_M}, Y_TO_M={Y_TO_M}:");
//                    System.Diagnostics.Debug.WriteLine($"  Start: ({MX(testLine.StartPoint.X):F3}m, {MY(testLine.StartPoint.Y):F3}m)");
//                    System.Diagnostics.Debug.WriteLine($"  End:   ({MX(testLine.EndPoint.X):F3}m, {MY(testLine.EndPoint.Y):F3}m)");
//                    System.Diagnostics.Debug.WriteLine($"  X span: {rawLengthX * X_TO_M:F3}m");
//                    System.Diagnostics.Debug.WriteLine($"  Y span: {rawLengthY * Y_TO_M:F3}m");

//                    System.Diagnostics.Debug.WriteLine($"\nZ Coordinate Handling:");
//                    System.Diagnostics.Debug.WriteLine($"  Z elevations come from StoryManager - already in meters");
//                    System.Diagnostics.Debug.WriteLine($"  No conversion applied to Z values");
//                }
//                else
//                {
//                    System.Diagnostics.Debug.WriteLine("\n⚠️ No lines found in DXF for diagnostics");
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"Diagnostic error: {ex.Message}");
//            }

//            System.Diagnostics.Debug.WriteLine("===================================================\n");
//        }

//        public void DefineSections()
//        {
//            System.Diagnostics.Debug.WriteLine(
//                "✓ Using wall sections from template - no need to define new sections");
//        }

//        public void ImportWalls(Dictionary<string, string> layerMapping, double elevation, int story)
//        {
//            wallsCreated = 0;
//            wallsFailed = 0;
//            wallTypeCount.Clear();

//            var wallLayers = layerMapping.Where(x => x.Value == "Wall").Select(x => x.Key).ToList();
//            if (wallLayers.Count == 0) return;

//            // NEW: Get wall grade for this story
//            string wallGrade = gradeSchedule?.GetWallGradeForStory(story);

//            System.Diagnostics.Debug.WriteLine(
//                $"\n========== IMPORTING WALLS - Story {story} ==========");
//            System.Diagnostics.Debug.WriteLine(
//                $"Building Config: {totalTypicalFloors} typical floors, {seismicZone}");

//            if (!string.IsNullOrEmpty(wallGrade))
//            {
//                System.Diagnostics.Debug.WriteLine($"Wall Concrete Grade: {wallGrade}");
//            }

//            System.Diagnostics.Debug.WriteLine(
//                $"Base Elevation: {elevation:F3}m (already in meters - no conversion)");
//            System.Diagnostics.Debug.WriteLine(
//                $"Top Elevation: {elevation + floorHeight:F3}m");

//            foreach (string layerName in wallLayers)
//            {
//                var wallType = WallThicknessCalculator.ClassifyWallFromLayerName(layerName);
//                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName} → {wallType}");

//                // Process lines
//                var lines = dxfDoc.Entities.Lines.Where(l => l.Layer.Name == layerName).ToList();
//                foreach (netDxf.Entities.Line line in lines)
//                {
//                    double wallLengthM = CalculateWallLengthInMeters(
//                        line.StartPoint.X, line.StartPoint.Y,
//                        line.EndPoint.X, line.EndPoint.Y);

//                    if (CreateWallFromLineWithAutoThickness(line, elevation, story, wallType, wallLengthM, wallGrade))
//                        wallsCreated++;
//                    else
//                        wallsFailed++;
//                }

//                // Process polylines
//                var polylines = dxfDoc.Entities.Polylines2D.Where(p => p.Layer.Name == layerName).ToList();
//                foreach (Polyline2D poly in polylines)
//                {
//                    int count = CreateWallFromPolylineWithAutoThickness(poly, elevation, story, wallType, wallGrade);
//                    wallsCreated += count;
//                }
//            }

//            System.Diagnostics.Debug.WriteLine($"\n========== WALL IMPORT SUMMARY ==========");
//            System.Diagnostics.Debug.WriteLine($"✓ Created: {wallsCreated}");
//            System.Diagnostics.Debug.WriteLine($"❌ Failed: {wallsFailed}");
//            System.Diagnostics.Debug.WriteLine($"\nWall Sections Used:");
//            foreach (var kvp in wallTypeCount.OrderBy(x => x.Key))
//            {
//                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value} walls");
//            }
//            System.Diagnostics.Debug.WriteLine($"=========================================\n");
//        }

//        private bool CreateWallFromLineWithAutoThickness(
//            netDxf.Entities.Line line,
//            double elevation,
//            int story,
//            WallThicknessCalculator.WallType wallType,
//            double wallLengthM,
//            string preferredGrade)  // NEW: Pass grade
//        {
//            try
//            {
//                if (wallLengthM < 0.001) return false;

//                string section = WallThicknessCalculator.GetRecommendedWallSection(
//                    totalTypicalFloors,
//                    wallType,
//                    seismicZone,
//                    wallLengthM,
//                    false,
//                    WallThicknessCalculator.ConstructionType.TypeII,
//                    preferredGrade);  // NEW: Pass grade

//                string storyName = GetStoryName(story);

//                if (!wallTypeCount.ContainsKey(section))
//                    wallTypeCount[section] = 0;
//                wallTypeCount[section]++;

//                // CORRECTED: elevation and floorHeight are ALREADY in meters - NO CONVERSION
//                string[] pts = new string[4];
//                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation, ref pts[0], "Global");
//                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation, ref pts[1], "Global");
//                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation + floorHeight, ref pts[2], "Global");
//                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation + floorHeight, ref pts[3], "Global");

//                string area = "";
//                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);

//                if (ret == 0 && !string.IsNullOrEmpty(area))
//                {
//                    sapModel.AreaObj.SetGroupAssign(area, storyName);
//                    System.Diagnostics.Debug.WriteLine(
//                        $"  ✓ Wall: {area} | {section} | Length: {wallLengthM:F2}m | Type: {wallType} | " +
//                        $"Base: {elevation:F3}m | Top: {elevation + floorHeight:F3}m");
//                    return true;
//                }

//                return false;
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"  ❌ Error: {ex.Message}");
//                return false;
//            }
//        }

//        private int CreateWallFromPolylineWithAutoThickness(
//            Polyline2D poly,
//            double elevation,
//            int story,
//            WallThicknessCalculator.WallType wallType,
//            string preferredGrade)  // NEW: Pass grade
//        {
//            try
//            {
//                var vertices = poly.Vertexes;
//                if (vertices == null || vertices.Count < 2) return 0;

//                string storyName = GetStoryName(story);
//                int count = 0;

//                for (int i = 0; i < vertices.Count - 1; i++)
//                {
//                    double wallLengthM = CalculateWallLengthInMeters(
//                        vertices[i].Position.X, vertices[i].Position.Y,
//                        vertices[i + 1].Position.X, vertices[i + 1].Position.Y);

//                    if (CreateWallSegmentWithAutoThickness(
//                        MX(vertices[i].Position.X), MY(vertices[i].Position.Y),
//                        MX(vertices[i + 1].Position.X), MY(vertices[i + 1].Position.Y),
//                        elevation, storyName, wallType, wallLengthM, preferredGrade))
//                    {
//                        count++;
//                    }
//                }

//                if (poly.IsClosed && vertices.Count > 2)
//                {
//                    double wallLengthM = CalculateWallLengthInMeters(
//                        vertices[vertices.Count - 1].Position.X, vertices[vertices.Count - 1].Position.Y,
//                        vertices[0].Position.X, vertices[0].Position.Y);

//                    if (CreateWallSegmentWithAutoThickness(
//                        MX(vertices[vertices.Count - 1].Position.X), MY(vertices[vertices.Count - 1].Position.Y),
//                        MX(vertices[0].Position.X), MY(vertices[0].Position.Y),
//                        elevation, storyName, wallType, wallLengthM, preferredGrade))
//                    {
//                        count++;
//                    }
//                }

//                return count;
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        private bool CreateWallSegmentWithAutoThickness(
//            double x1M, double y1M,
//            double x2M, double y2M,
//            double elevation,
//            string storyName,
//            WallThicknessCalculator.WallType wallType,
//            double wallLengthM,
//            string preferredGrade)  // NEW: Pass grade
//        {
//            try
//            {
//                if (wallLengthM < 0.001) return false;

//                string section = WallThicknessCalculator.GetRecommendedWallSection(
//                    totalTypicalFloors,
//                    wallType,
//                    seismicZone,
//                    wallLengthM,
//                    false,
//                    WallThicknessCalculator.ConstructionType.TypeII,
//                    preferredGrade);  // NEW: Pass grade

//                if (!wallTypeCount.ContainsKey(section))
//                    wallTypeCount[section] = 0;
//                wallTypeCount[section]++;

//                // CORRECTED: elevation and floorHeight are ALREADY in meters - NO CONVERSION
//                string[] pts = new string[4];
//                sapModel.PointObj.AddCartesian(x1M, y1M, elevation, ref pts[0], "Global");
//                sapModel.PointObj.AddCartesian(x2M, y2M, elevation, ref pts[1], "Global");
//                sapModel.PointObj.AddCartesian(x2M, y2M, elevation + floorHeight, ref pts[2], "Global");
//                sapModel.PointObj.AddCartesian(x1M, y1M, elevation + floorHeight, ref pts[3], "Global");

//                string area = "";
//                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);

//                if (ret == 0 && !string.IsNullOrEmpty(area))
//                {
//                    sapModel.AreaObj.SetGroupAssign(area, storyName);
//                    return true;
//                }

//                return false;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private double CalculateWallLengthInMeters(double x1, double y1, double x2, double y2)
//        {
//            // Use separate conversions for X and Y
//            double x1M = MX(x1);
//            double y1M = MY(y1);
//            double x2M = MX(x2);
//            double y2M = MY(y2);

//            double dx = x2M - x1M;
//            double dy = y2M - y1M;
//            return Math.Sqrt(dx * dx + dy * dy);
//        }

//        private string GetStoryName(int story)
//        {
//            try
//            {
//                int numStories = 0;
//                string[] storyNames = null;
//                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

//                if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
//                {
//                    return storyNames[story];
//                }
//            }
//            catch { }

//            return story == 0 ? "Base" : $"Story{story + 1}";
//        }

//        public string GetImportStatistics()
//        {
//            string stats = $"Walls Created: {wallsCreated}, Failed: {wallsFailed}\n";
//            stats += "\nWall Sections Used:\n";
//            foreach (var kvp in wallTypeCount.OrderBy(x => x.Key))
//            {
//                stats += $"  {kvp.Key}: {kvp.Value} walls\n";
//            }
//            return stats;
//        }

//        public void ResetStatistics()
//        {
//            wallsCreated = 0;
//            wallsFailed = 0;
//            wallTypeCount.Clear();
//        }
//    }
//}


// ============================================================================
// FILE: Importers/WallImporterEnhanced.cs
// VERSION: 3.0 — Added W-NTA wall (user-defined thickness)
// ============================================================================
// Wall layers:
//   W-Core wall              → GPL TABLE (IS 1893-2025)
//   W-Peripheral dead wall   → GPL TABLE
//   W-Peripheral Portal wall → GPL TABLE
//   W-Internal wall          → GPL TABLE
//   W-NTA wall               → User input wall thickness (from slabConfig["NTA"])
//
// Wall numbering: Pier label P1, P2, P3...
// ============================================================================

using ETAB_Automation.Core;
using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETABS_CAD_Automation.Importers
{
    public class WallImporterEnhanced
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private double floorHeight;
        private int totalTypicalFloors;
        private string seismicZone;
        private GradeScheduleManager gradeSchedule;

        // User-defined thickness for W-NTA walls (mm), default 200
        private int ntaWallThicknessMm;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        private int wallsCreated = 0;
        private int wallsFailed = 0;
        private Dictionary<string, int> wallTypeCount = new Dictionary<string, int>();

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public WallImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            double height,
            int typicalFloors,
            string zone,
            GradeScheduleManager gradeManager = null,
            int ntaThicknessMm = 200)      // ← NEW parameter for W-NTA
        {
            sapModel = model;
            dxfDoc = doc;
            floorHeight = height;
            totalTypicalFloors = typicalFloors;
            seismicZone = zone;
            gradeSchedule = gradeManager;
            ntaWallThicknessMm = ntaThicknessMm;

            DiagnoseCoordinateSystem();
            WallThicknessCalculator.LoadAvailableWallSections(sapModel);
        }

        // ====================================================================
        // WALL CLASSIFICATION  (extended for W-NTA)
        // ====================================================================

        private enum WallCategory
        {
            Core, PeripheralDead, PeripheralPortal, Internal, NTA
        }

        private WallCategory ClassifyWall(string layerName)
        {
            string u = layerName.ToUpperInvariant();

            if (u.Contains("NTA")) return WallCategory.NTA;
            if (u.Contains("CORE")) return WallCategory.Core;
            if (u.Contains("PERIPHERAL") && u.Contains("DEAD")) return WallCategory.PeripheralDead;
            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL")) return WallCategory.PeripheralPortal;
            if (u.Contains("PERIPHERAL")) return WallCategory.PeripheralDead;
            if (u.Contains("INTERNAL")) return WallCategory.Internal;

            return WallCategory.Internal; // safe default
        }

        private string GetWallSection(WallCategory cat, double wallLengthM,
            string preferredGrade)
        {
            if (cat == WallCategory.NTA)
            {
                // Direct thickness lookup — no GPL table
                return GetNTASection(preferredGrade);
            }

            // GPL table via WallThicknessCalculator
            var wtType = cat switch
            {
                WallCategory.Core => WallThicknessCalculator.WallType.CoreWall,
                WallCategory.PeripheralDead => WallThicknessCalculator.WallType.PeripheralDeadWall,
                WallCategory.PeripheralPortal => WallThicknessCalculator.WallType.PeripheralPortalWall,
                _ => WallThicknessCalculator.WallType.InternalWall
            };

            return WallThicknessCalculator.GetRecommendedWallSection(
                totalTypicalFloors, wtType, seismicZone,
                wallLengthM, false,
                WallThicknessCalculator.ConstructionType.TypeII,
                preferredGrade);
        }

        private string GetNTASection(string preferredGrade)
        {
            // Find the closest wall section to ntaWallThicknessMm
            // WallThicknessCalculator exposes its section dict via GetClosestSection helper
            // We call it via the existing public method by temporarily passing a fixed thickness
            // Since WallThicknessCalculator doesn't expose GetClosest directly, we use a workaround:
            // create a dummy wall type match at the right thickness.
            // We reuse the same private mechanism via reflection isn't clean, so
            // instead we query PropArea directly.
            try
            {
                int num = 0; string[] names = null;
                sapModel.PropArea.GetNameList(ref num, ref names);

                string gradeNum = preferredGrade?.Replace("M", "").Replace("m", "").Trim();
                string best = null; int minDiff = int.MaxValue;

                var pattern = new System.Text.RegularExpressions.Regex(
                    @"^W(\d+)M(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Grade-matched first
                if (!string.IsNullOrEmpty(gradeNum) && names != null)
                {
                    foreach (string name in names)
                    {
                        var m = pattern.Match(name);
                        if (!m.Success) continue;
                        if (m.Groups[2].Value != gradeNum) continue;
                        int t = int.Parse(m.Groups[1].Value);
                        int diff = Math.Abs(t - ntaWallThicknessMm);
                        if (diff < minDiff) { minDiff = diff; best = name; }
                    }
                    if (best != null) return best;
                }

                // Grade-agnostic fallback
                minDiff = int.MaxValue;
                if (names != null)
                {
                    foreach (string name in names)
                    {
                        var m = pattern.Match(name);
                        if (!m.Success) continue;
                        int t = int.Parse(m.Groups[1].Value);
                        int diff = Math.Abs(t - ntaWallThicknessMm);
                        if (diff < minDiff) { minDiff = diff; best = name; }
                    }
                }

                if (best != null) return best;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ GetNTASection: {ex.Message}");
            }

            // Last resort — any section
            return $"W{ntaWallThicknessMm}M30";
        }

        // ====================================================================
        // PUBLIC IMPORT
        // ====================================================================

        public void ImportWalls(Dictionary<string, string> layerMapping,
            double elevation, int story)
        {
            wallsCreated = 0; wallsFailed = 0; wallTypeCount.Clear();

            var wallLayers = layerMapping
                .Where(x => x.Value == "Wall")
                .Select(x => x.Key)
                .ToList();
            if (wallLayers.Count == 0) return;

            string wallGrade = gradeSchedule?.GetWallGradeForStory(story);

            System.Diagnostics.Debug.WriteLine(
                $"\n========== IMPORTING WALLS - Story {story} ==========");
            System.Diagnostics.Debug.WriteLine(
                $"Base: {elevation:F3}m | Top: {elevation + floorHeight:F3}m | " +
                $"Grade: {wallGrade ?? "default"} | NTA: {ntaWallThicknessMm}mm");

            foreach (string layerName in wallLayers)
            {
                var cat = ClassifyWall(layerName);
                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName} [{cat}]");

                foreach (var line in dxfDoc.Entities.Lines
                    .Where(l => l.Layer.Name == layerName))
                {
                    double len = WallLength(line.StartPoint.X, line.StartPoint.Y,
                                           line.EndPoint.X, line.EndPoint.Y);
                    string section = GetWallSection(cat, len, wallGrade);
                    if (CreateWallFromLine(line, elevation, story, section))
                        wallsCreated++;
                    else
                        wallsFailed++;
                }

                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    wallsCreated += CreateWallFromPolyline(poly, elevation, story, cat, wallGrade);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"\n✓ {wallsCreated}  ❌ {wallsFailed}");
        }

        // ====================================================================
        // GEOMETRY CREATION
        // ====================================================================

        private bool CreateWallFromLine(netDxf.Entities.Line line,
            double elevation, int story, string section)
        {
            try
            {
                if (!wallTypeCount.ContainsKey(section)) wallTypeCount[section] = 0;
                wallTypeCount[section]++;

                string[] pts = new string[4];
                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y),
                    elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y),
                    elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(MX(line.EndPoint.X), MY(line.EndPoint.Y),
                    elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(MX(line.StartPoint.X), MY(line.StartPoint.Y),
                    elevation + floorHeight, ref pts[3], "Global");

                string area = "";
                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);
                if (ret == 0 && !string.IsNullOrEmpty(area))
                {
                    sapModel.AreaObj.SetGroupAssign(area, GetStoryName(story));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {ex.Message}");
                return false;
            }
        }

        private int CreateWallFromPolyline(Polyline2D poly, double elevation,
            int story, WallCategory cat, string grade)
        {
            try
            {
                var verts = poly.Vertexes;
                if (verts == null || verts.Count < 2) return 0;

                string storyName = GetStoryName(story);
                int cnt = 0;

                for (int i = 0; i < verts.Count - 1; i++)
                {
                    double len = WallLength(verts[i].Position.X, verts[i].Position.Y,
                                           verts[i + 1].Position.X, verts[i + 1].Position.Y);
                    string section = GetWallSection(cat, len, grade);
                    if (CreateWallSegment(
                        MX(verts[i].Position.X), MY(verts[i].Position.Y),
                        MX(verts[i + 1].Position.X), MY(verts[i + 1].Position.Y),
                        elevation, storyName, section)) cnt++;
                }

                if (poly.IsClosed && verts.Count > 2)
                {
                    int last = verts.Count - 1;
                    double len = WallLength(verts[last].Position.X, verts[last].Position.Y,
                                           verts[0].Position.X, verts[0].Position.Y);
                    string section = GetWallSection(cat, len, grade);
                    if (CreateWallSegment(
                        MX(verts[last].Position.X), MY(verts[last].Position.Y),
                        MX(verts[0].Position.X), MY(verts[0].Position.Y),
                        elevation, storyName, section)) cnt++;
                }
                return cnt;
            }
            catch { return 0; }
        }

        private bool CreateWallSegment(double x1, double y1, double x2, double y2,
            double elevation, string storyName, string section)
        {
            try
            {
                if (!wallTypeCount.ContainsKey(section)) wallTypeCount[section] = 0;
                wallTypeCount[section]++;

                string[] pts = new string[4];
                sapModel.PointObj.AddCartesian(x1, y1, elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(x1, y1, elevation + floorHeight, ref pts[3], "Global");

                string area = "";
                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);
                if (ret == 0 && !string.IsNullOrEmpty(area))
                {
                    sapModel.AreaObj.SetGroupAssign(area, storyName);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private double WallLength(double x1, double y1, double x2, double y2)
        {
            double dx = (x2 - x1) * X_TO_M, dy = (y2 - y1) * Y_TO_M;
            return Math.Sqrt(dx * dx + dy * dy);
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

        private void DiagnoseCoordinateSystem()
        {
            System.Diagnostics.Debug.WriteLine("WallImporter: X/Y→m via ×0.001, Z already in m");
        }

        public void DefineSections() { }

        public string GetImportStatistics()
        {
            string s = $"Walls Created: {wallsCreated}, Failed: {wallsFailed}\n";
            foreach (var kvp in wallTypeCount.OrderBy(x => x.Key))
                s += $"  {kvp.Key}: {kvp.Value}\n";
            return s;
        }

        public void ResetStatistics()
        {
            wallsCreated = 0; wallsFailed = 0; wallTypeCount.Clear();
        }
    }
}
