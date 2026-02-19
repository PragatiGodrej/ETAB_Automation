
//// ============================================================================
//// FILE: Importers/SlabImporterEnhanced.cs (WITH GRADE SCHEDULE SUPPORT)
//// ============================================================================
//using ETABSv1;
//using netDxf;
//using netDxf.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;
//using ETAB_Automation.Core;
//using static netDxf.Entities.HatchBoundaryPath;

//namespace ETABS_CAD_Automation.Importers
//{
//    public class SlabImporterEnhanced
//    {
//        private cSapModel sapModel;
//        private DxfDocument dxfDoc;
//        private Dictionary<string, int> slabConfig;
//        private GradeScheduleManager gradeSchedule;  // Grade schedule manager

//        // Convert DXF coordinates to meters
//        // Z elevations come from StoryManager and are ALREADY in meters - NO CONVERSION NEEDED
//        private const double MM_TO_M = 0.001;
//        private const double CLOSURE_TOLERANCE = 10000.0; // 10000mm (10m) tolerance - auto-close large gaps
//        private const double MIN_AREA = 0.0001; // 0.01 m² minimum area

//        private double M(double mm) => mm * MM_TO_M;

//        // Store available slab sections from template
//        private static Dictionary<string, SlabSectionInfo> availableSlabSections =
//            new Dictionary<string, SlabSectionInfo>();

//        private class SlabSectionInfo
//        {
//            public string SectionName { get; set; }
//            public int ThicknessMm { get; set; }
//            public string Grade { get; set; }
//        }

//        // Slab thickness rules based on area (from your configuration)
//        private static readonly Dictionary<int, double> SlabThicknessAreaRules = new Dictionary<int, double>
//        {
//            { 125, 14 },   // 125mm for area <= 14 m²
//            { 135, 17 },   // 135mm for area <= 17 m²
//            { 150, 22 },   // 150mm for area <= 22 m²
//            { 160, 25 },   // 160mm for area <= 25 m²
//            { 175, 32 },   // 175mm for area <= 32 m²
//            { 200, 42 },   // 200mm for area <= 42 m²
//            { 250, 70 }    // 250mm for area <= 70 m²
//        };

//        // Cantilever thickness rules based on span (from your configuration)
//        private static readonly Dictionary<int, double> CantileverThicknessRules = new Dictionary<int, double>
//        {
//            { 125, 1.0 },   // 125mm for span <= 1.0m
//            { 160, 1.5 },   // 160mm for span <= 1.5m
//            { 180, 1.8 },   // 180mm for span <= 1.8m
//            { 200, 5.0 }    // 200mm for span <= 5.0m
//        };

//        public SlabImporterEnhanced(
//            cSapModel model,
//            DxfDocument doc,
//            Dictionary<string, int> config = null,
//            GradeScheduleManager gradeManager = null)  // Grade manager parameter
//        {
//            sapModel = model;
//            dxfDoc = doc;
//            slabConfig = config ?? new Dictionary<string, int>
//            {
//                { "Lobby", 160 },
//                { "Stair", 175 }
//            };
//            gradeSchedule = gradeManager;  // ✅ ENABLED: Assign grade schedule manager

//            LoadAvailableSlabSections();
//        }

//        private void LoadAvailableSlabSections()
//        {
//            if (availableSlabSections.Count > 0) return;

//            try
//            {
//                availableSlabSections.Clear();

//                int numSections = 0;
//                string[] sectionNames = null;

//                int ret = sapModel.PropArea.GetNameList(ref numSections, ref sectionNames);

//                if (ret == 0 && sectionNames != null)
//                {
//                    // Pattern: S160SM45 = 160mm thickness, M45 grade
//                    Regex slabPattern = new Regex(@"^S(\d+)SM(\d+)", RegexOptions.IgnoreCase);

//                    foreach (string sectionName in sectionNames)
//                    {
//                        Match match = slabPattern.Match(sectionName);

//                        if (match.Success)
//                        {
//                            int thicknessMm = int.Parse(match.Groups[1].Value);
//                            string grade = match.Groups[2].Value;

//                            availableSlabSections[sectionName] = new SlabSectionInfo
//                            {
//                                SectionName = sectionName,
//                                ThicknessMm = thicknessMm,
//                                Grade = grade
//                            };

//                            System.Diagnostics.Debug.WriteLine(
//                                $"Loaded slab: {sectionName} = {thicknessMm}mm (M{grade})");
//                        }
//                    }
//                }

//                System.Diagnostics.Debug.WriteLine(
//                    $"\n✓ Loaded {availableSlabSections.Count} slab sections from template");

//                if (availableSlabSections.Count == 0)
//                {
//                    System.Diagnostics.Debug.WriteLine(
//                        "⚠ No template slab sections found. Using fallback definitions.");
//                    DefineFallbackSections();
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Error loading slab sections: {ex.Message}");
//                DefineFallbackSections();
//            }
//        }

//        private void DefineFallbackSections()
//        {
//            // Fallback: Define standard slab sections if template doesn't have them
//            try
//            {
//                var thicknesses = new[] { 100, 125, 135, 150, 160, 175, 180, 200, 225, 250 };

//                foreach (int thickness in thicknesses)
//                {
//                    string sectionName = $"SLAB{thickness}";

//                    sapModel.PropArea.SetSlab(
//                        sectionName,
//                        eSlabType.Slab,
//                        eShellType.ShellThin,
//                        "CONC",
//                        thickness * 0.001, // Convert mm to m
//                        12,
//                        "CONC",
//                        "CONC");

//                    availableSlabSections[sectionName] = new SlabSectionInfo
//                    {
//                        SectionName = sectionName,
//                        ThicknessMm = thickness,
//                        Grade = "Default"
//                    };
//                }

//                System.Diagnostics.Debug.WriteLine("✓ Fallback slab sections defined successfully");
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Error defining fallback sections: {ex.Message}");
//                throw;
//            }
//        }

//        private string GetClosestSlabSection(int requiredThickness, string preferredGrade = null)
//        {
//            if (availableSlabSections.Count == 0)
//            {
//                throw new InvalidOperationException("No slab sections available.");
//            }

//            string bestMatch = null;
//            int minDifference = int.MaxValue;

//            // ⭐ CRITICAL FIX: Remove "M" prefix from preferredGrade for comparison
//            // GradeScheduleManager returns "M30", but section.Grade is stored as "30"
//            string gradeToMatch = preferredGrade?.Replace("M", "").Replace("m", "").Trim();

//            // First try to find exact match with preferred grade
//            if (!string.IsNullOrEmpty(gradeToMatch))
//            {
//                foreach (var kvp in availableSlabSections)
//                {
//                    var section = kvp.Value;
//                    if (section.ThicknessMm == requiredThickness && section.Grade == gradeToMatch)
//                    {
//                        System.Diagnostics.Debug.WriteLine(
//                            $"  Exact match: {kvp.Key} ({section.ThicknessMm}mm M{section.Grade})");
//                        return kvp.Key;
//                    }
//                }

//                // Try to find closest thickness with preferred grade
//                foreach (var kvp in availableSlabSections)
//                {
//                    var section = kvp.Value;
//                    if (section.Grade == gradeToMatch)
//                    {
//                        int difference = Math.Abs(section.ThicknessMm - requiredThickness);

//                        if (difference < minDifference)
//                        {
//                            minDifference = difference;
//                            bestMatch = kvp.Key;
//                        }
//                    }
//                }

//                if (bestMatch != null)
//                {
//                    var matched = availableSlabSections[bestMatch];
//                    System.Diagnostics.Debug.WriteLine(
//                        $"  Required: {requiredThickness}mm M{gradeToMatch} → Using: {bestMatch} ({matched.ThicknessMm}mm M{matched.Grade})");
//                    return bestMatch;
//                }
//            }

//            // Fallback: Find closest thickness without grade preference
//            minDifference = int.MaxValue;
//            foreach (var kvp in availableSlabSections)
//            {
//                var section = kvp.Value;
//                int difference = Math.Abs(section.ThicknessMm - requiredThickness);

//                if (difference < minDifference)
//                {
//                    minDifference = difference;
//                    bestMatch = kvp.Key;
//                }
//            }

//            if (bestMatch != null)
//            {
//                var matched = availableSlabSections[bestMatch];
//                System.Diagnostics.Debug.WriteLine(
//                    $"  Required: {requiredThickness}mm → Using: {bestMatch} ({matched.ThicknessMm}mm M{matched.Grade})");
//                return bestMatch;
//            }

//            throw new InvalidOperationException(
//                $"No suitable slab section found for {requiredThickness}mm.");
//        }

//        private int DetermineSlabThicknessFromArea(double areaM2)
//        {
//            foreach (var rule in SlabThicknessAreaRules.OrderBy(r => r.Key))
//            {
//                if (areaM2 <= rule.Value)
//                {
//                    return rule.Key;
//                }
//            }
//            return 250; // Default for very large areas
//        }

//        private int DetermineCantileverThickness(double spanM)
//        {
//            foreach (var rule in CantileverThicknessRules.OrderBy(r => r.Key))
//            {
//                if (spanM <= rule.Value)
//                {
//                    return rule.Key;
//                }
//            }
//            return 200; // Default for long cantilevers
//        }

//        /// <summary>
//        /// ✅ CORRECTED: Calculate cantilever span as perpendicular distance from support edge to free edge
//        /// </summary>

//        private double CalculateCantileverSpan(List<netDxf.Vector2> points)
//        {
//            if (points.Count < 3) return 0;

//            var edges = new List<double>();
//            for (int i = 0; i < points.Count; i++)
//            {
//                int j = (i + 1) % points.Count;
//                double dx = points[i].X - points[j].X;
//                double dy = points[i].Y - points[j].Y;
//                edges.Add(Math.Sqrt(dx * dx + dy * dy));

//            }

//            return edges.Min() * MM_TO_M;
//        }
//        /// <summary>
//        /// Calculate perpendicular distance from a point to a line segment
//        /// </summary>
//        private double PerpendicularDistanceToLineSegment(netDxf.Vector2 point, netDxf.Vector2 lineStart, netDxf.Vector2 lineEnd)
//        {
//            // Vector from lineStart to lineEnd
//            double dx = lineEnd.X - lineStart.X;
//            double dy = lineEnd.Y - lineStart.Y;

//            // Length squared of the line segment
//            double lengthSquared = dx * dx + dy * dy;

//            if (lengthSquared == 0)
//            {
//                // Line segment is actually a point
//                double dpx = point.X - lineStart.X;
//                double dpy = point.Y - lineStart.Y;
//                return Math.Sqrt(dpx * dpx + dpy * dpy);
//            }

//            // Project point onto the line (infinite)
//            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;

//            // Clamp to line segment (not infinite line)
//            t = Math.Max(0, Math.Min(1, t));

//            // Find closest point on line segment
//            double closestX = lineStart.X + t * dx;
//            double closestY = lineStart.Y + t * dy;

//            // Calculate distance
//            double distX = point.X - closestX;
//            double distY = point.Y - closestY;

//            return Math.Sqrt(distX * distX + distY * distY);
//        }

//        private string DetermineSlabSection(string layerName, List<netDxf.Vector2> points, string preferredGrade)
//        {
//            string upper = layerName.ToUpperInvariant();
//            int thickness;

//            // Lobby slabs - use configured thickness
//            if (upper.Contains("LOBBY") || upper.Equals("S-LOBBY", StringComparison.OrdinalIgnoreCase))
//            {
//                thickness = slabConfig["Lobby"];
//                System.Diagnostics.Debug.WriteLine($"  Lobby slab: {thickness}mm");
//                return GetClosestSlabSection(thickness, preferredGrade);
//            }

//            // Stair slabs - use configured thickness
//            if (upper.Contains("STAIR") || upper.Equals("S-STAIR", StringComparison.OrdinalIgnoreCase))
//            {
//                thickness = slabConfig["Stair"];
//                System.Diagnostics.Debug.WriteLine($"  Stair slab: {thickness}mm");
//                return GetClosestSlabSection(thickness, preferredGrade);
//            }

//            // Cantilever slabs - based on span (✅ NOW USING CORRECTED CALCULATION)
//            if (upper.Contains("CANTILEVER") || upper.Contains("BALCONY") || upper.Contains("CHAJJA") ||
//                upper.Equals("S-BALCONY SLABS", StringComparison.OrdinalIgnoreCase) ||
//                upper.Equals("S-CANTILVER BALCONY", StringComparison.OrdinalIgnoreCase))
//            {
//                double spanM = CalculateCantileverSpan(points);  // ✅ Use corrected method
//                thickness = DetermineCantileverThickness(spanM);
//                System.Diagnostics.Debug.WriteLine($"  Cantilever slab: span={spanM:F2}m → {thickness}mm");
//                return GetClosestSlabSection(thickness, preferredGrade);
//            }

//            // Regular slabs - based on area
//            double areaM2 = Math.Abs(CalculatePolygonArea(points));
//            thickness = DetermineSlabThicknessFromArea(areaM2);

//            string slabType = "Regular";
//            if (upper.Contains("RESIDENTIAL") || upper.Equals("S-RESIDENTIAL", StringComparison.OrdinalIgnoreCase))
//                slabType = "Residential";
//            else if (upper.Contains("KITCHEN") || upper.Equals("S-KITCHEN", StringComparison.OrdinalIgnoreCase))
//                slabType = "Kitchen";
//            else if (upper.Contains("TOILET") || upper.Equals("S-TOILET", StringComparison.OrdinalIgnoreCase))
//                slabType = "Toilet";
//            else if (upper.Contains("UTILITY") || upper.Equals("S-UTILITY", StringComparison.OrdinalIgnoreCase))
//                slabType = "Utility";
//            else if (upper.Contains("SERVICE") || upper.Equals("S-SERVICE SLABS", StringComparison.OrdinalIgnoreCase))
//                slabType = "Service";

//            System.Diagnostics.Debug.WriteLine($"  {slabType} slab: area={areaM2:F2}m² → {thickness}mm");
//            return GetClosestSlabSection(thickness, preferredGrade);
//        }

//        public void ImportSlabs(Dictionary<string, string> layerMapping, double elevation, int story)
//        {
//            var slabLayers = layerMapping.Where(x => x.Value == "Slab").Select(x => x.Key).ToList();

//            if (slabLayers.Count == 0)
//            {
//                System.Diagnostics.Debug.WriteLine($"⚠ No slab layers found in mapping for story {story}");
//                return;
//            }

//            // Get slab grade for this story (0.7x wall grade)
//            string slabGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

//            System.Diagnostics.Debug.WriteLine($"\n========== IMPORTING SLABS - Story {story} ==========");
//            System.Diagnostics.Debug.WriteLine($"Slab Elevation: {elevation:F3}m (already in meters - no conversion)");

//            if (!string.IsNullOrEmpty(slabGrade))
//            {
//                System.Diagnostics.Debug.WriteLine($"Slab Concrete Grade: {slabGrade}");
//            }

//            int successCount = 0;
//            int failCount = 0;
//            int skippedCount = 0;

//            foreach (string layerName in slabLayers)
//            {
//                System.Diagnostics.Debug.WriteLine($"\n--- Layer: {layerName} ---");

//                // Process Polylines2D
//                var polylines = dxfDoc.Entities.Polylines2D
//                    .Where(p => p.Layer.Name == layerName).ToList();

//                System.Diagnostics.Debug.WriteLine($"Found {polylines.Count} polylines");

//                foreach (var poly in polylines)
//                {
//                    var result = CreateSlabFromPolyline(poly, elevation, layerName, story, slabGrade);
//                    if (result == SlabCreationResult.Success) successCount++;
//                    else if (result == SlabCreationResult.Failed) failCount++;
//                    else skippedCount++;
//                }

//                // Process Hatches
//                var hatches = dxfDoc.Entities.Hatches
//                    .Where(h => h.Layer.Name == layerName).ToList();

//                System.Diagnostics.Debug.WriteLine($"Found {hatches.Count} hatches");

//                foreach (var hatch in hatches)
//                {
//                    var result = CreateSlabFromHatch(hatch, elevation, layerName, story, slabGrade);
//                    if (result == SlabCreationResult.Success) successCount++;
//                    else if (result == SlabCreationResult.Failed) failCount++;
//                    else skippedCount++;
//                }
//            }

//            System.Diagnostics.Debug.WriteLine($"\n========== SLAB IMPORT SUMMARY ==========");
//            System.Diagnostics.Debug.WriteLine($"✓ Success: {successCount}");
//            System.Diagnostics.Debug.WriteLine($"❌ Failed: {failCount}");
//            System.Diagnostics.Debug.WriteLine($"⊘ Skipped: {skippedCount}");
//            System.Diagnostics.Debug.WriteLine($"=========================================\n");
//        }

//        private enum SlabCreationResult
//        {
//            Success,
//            Failed,
//            Skipped
//        }

//        private SlabCreationResult CreateSlabFromPolyline(
//            Polyline2D poly,
//            double elevation,
//            string layerName,
//            int story,
//            string preferredGrade)
//        {
//            try
//            {
//                var vertices = poly.Vertexes;
//                if (vertices == null || vertices.Count < 3)
//                {
//                    System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Only {vertices?.Count ?? 0} vertices");
//                    return SlabCreationResult.Skipped;
//                }

//                // Extract vertex positions
//                List<netDxf.Vector2> points = new List<netDxf.Vector2>();
//                foreach (var v in vertices)
//                {
//                    points.Add(v.Position);
//                }

//                // Check closure and auto-close if needed
//                if (!IsClosedOrAutoClose(ref points))
//                {
//                    System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Polyline not closed and cannot auto-close");
//                    return SlabCreationResult.Skipped;
//                }

//                // Determine section based on layer name and geometry
//                string section = DetermineSlabSection(layerName, points, preferredGrade);

//                return CreateSlabFromPoints(points, elevation, section, story);
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Exception in CreateSlabFromPolyline: {ex.Message}");
//                return SlabCreationResult.Failed;
//            }
//        }

//        private SlabCreationResult CreateSlabFromHatch(
//            Hatch hatch,
//            double elevation,
//            string layerName,
//            int story,
//            string preferredGrade)
//        {
//            try
//            {
//                SlabCreationResult overallResult = SlabCreationResult.Skipped;

//                foreach (var boundaryPath in hatch.BoundaryPaths)
//                {
//                    var edges = boundaryPath.Edges;
//                    if (edges.Count == 0)
//                        continue;

//                    List<netDxf.Vector2> vertices = ExtractHatchBoundaryVertices(edges);

//                    if (vertices.Count >= 3)
//                    {
//                        // Check closure
//                        if (!IsClosedOrAutoClose(ref vertices))
//                        {
//                            System.Diagnostics.Debug.WriteLine($"⊘ Skipped: Hatch boundary not closed");
//                            continue;
//                        }

//                        // Determine section based on layer name and geometry
//                        string section = DetermineSlabSection(layerName, vertices, preferredGrade);

//                        var result = CreateSlabFromPoints(vertices, elevation, section, story);
//                        if (result == SlabCreationResult.Success)
//                            overallResult = SlabCreationResult.Success;
//                        else if (result == SlabCreationResult.Failed && overallResult != SlabCreationResult.Success)
//                            overallResult = SlabCreationResult.Failed;
//                    }
//                }

//                return overallResult;
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Exception in CreateSlabFromHatch: {ex.Message}");
//                return SlabCreationResult.Failed;
//            }
//        }

//        private List<netDxf.Vector2> ExtractHatchBoundaryVertices(IReadOnlyList<HatchBoundaryPath.Edge> edges)
//        {
//            List<netDxf.Vector2> vertices = new List<netDxf.Vector2>();

//            foreach (var edge in edges)
//            {
//                if (edge is HatchBoundaryPath.Line lineEdge)
//                {
//                    vertices.Add(lineEdge.Start);
//                }
//                else if (edge is HatchBoundaryPath.Arc arcEdge)
//                {
//                    var arcPoints = TessellateArc(arcEdge);
//                    vertices.AddRange(arcPoints);
//                }
//                else if (edge is HatchBoundaryPath.Ellipse ellipseEdge)
//                {
//                    var ellipsePoints = TessellateEllipse(ellipseEdge);
//                    vertices.AddRange(ellipsePoints);
//                }
//                else if (edge is HatchBoundaryPath.Spline splineEdge)
//                {
//                    foreach (var cp in splineEdge.ControlPoints)
//                    {
//                        vertices.Add(new netDxf.Vector2(cp.X, cp.Y));
//                    }
//                }
//            }

//            return vertices;
//        }

//        private List<netDxf.Vector2> TessellateArc(HatchBoundaryPath.Arc arc)
//        {
//            List<netDxf.Vector2> points = new List<netDxf.Vector2>();
//            int segments = 16;

//            double startAngle = arc.StartAngle * Math.PI / 180.0;
//            double endAngle = arc.EndAngle * Math.PI / 180.0;

//            if (endAngle < startAngle)
//                endAngle += 2 * Math.PI;

//            double angleStep = (endAngle - startAngle) / segments;

//            for (int i = 0; i <= segments; i++)
//            {
//                double angle = startAngle + i * angleStep;
//                double x = arc.Center.X + arc.Radius * Math.Cos(angle);
//                double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
//                points.Add(new netDxf.Vector2(x, y));
//            }

//            return points;
//        }

//        private List<netDxf.Vector2> TessellateEllipse(HatchBoundaryPath.Ellipse ellipse)
//        {
//            List<netDxf.Vector2> points = new List<netDxf.Vector2>();
//            int segments = 24;

//            double startAngle = ellipse.StartAngle * Math.PI / 180.0;
//            double endAngle = ellipse.EndAngle * Math.PI / 180.0;

//            if (endAngle < startAngle)
//                endAngle += 2 * Math.PI;

//            double angleStep = (endAngle - startAngle) / segments;

//            double majorAxis = Math.Sqrt(ellipse.EndMajorAxis.X * ellipse.EndMajorAxis.X +
//                                         ellipse.EndMajorAxis.Y * ellipse.EndMajorAxis.Y);
//            double minorAxis = majorAxis * ellipse.MinorRatio;

//            for (int i = 0; i <= segments; i++)
//            {
//                double angle = startAngle + i * angleStep;
//                double x = ellipse.Center.X + majorAxis * Math.Cos(angle);
//                double y = ellipse.Center.Y + minorAxis * Math.Sin(angle);
//                points.Add(new netDxf.Vector2(x, y));
//            }

//            return points;
//        }

//        private bool IsClosedOrAutoClose(ref List<netDxf.Vector2> points)
//        {
//            if (points.Count < 3)
//                return false;

//            var first = points[0];
//            var last = points[points.Count - 1];

//            double gap = Math.Sqrt(
//                Math.Pow(last.X - first.X, 2) +
//                Math.Pow(last.Y - first.Y, 2));

//            if (gap < CLOSURE_TOLERANCE)
//            {
//                if (gap < 0.1)
//                {
//                    points.RemoveAt(points.Count - 1);
//                    System.Diagnostics.Debug.WriteLine($"  Removed duplicate last vertex (gap: {gap:F4}mm)");
//                }
//                else
//                {
//                    System.Diagnostics.Debug.WriteLine($"  Auto-closing polyline (gap: {gap:F2}mm)");
//                }
//                return true;
//            }

//            System.Diagnostics.Debug.WriteLine($"  Gap too large: {gap:F2}mm (max: {CLOSURE_TOLERANCE}mm)");
//            return false;
//        }

//        private SlabCreationResult CreateSlabFromPoints(List<netDxf.Vector2> points, double elevation,
//            string section, int story)
//        {
//            try
//            {
//                if (points == null || points.Count < 3)
//                {
//                    System.Diagnostics.Debug.WriteLine($"⊘ Insufficient vertices: {points?.Count ?? 0}");
//                    return SlabCreationResult.Skipped;
//                }

//                // Validate area
//                double polygonArea = CalculatePolygonArea(points);
//                if (Math.Abs(polygonArea) < MIN_AREA)
//                {
//                    System.Diagnostics.Debug.WriteLine($"⊘ Area too small: {Math.Abs(polygonArea):F4} m²");
//                    return SlabCreationResult.Skipped;
//                }

//                // Ensure counter-clockwise winding
//                if (polygonArea < 0)
//                {
//                    points.Reverse();
//                    System.Diagnostics.Debug.WriteLine($"  Reversed winding order");
//                }

//                // Clean points
//                var cleanedPoints = RemoveDuplicateAndCollinearPoints(points);

//                if (cleanedPoints.Count < 3)
//                {
//                    System.Diagnostics.Debug.WriteLine($"⊘ After cleaning, only {cleanedPoints.Count} vertices");
//                    return SlabCreationResult.Skipped;
//                }

//                int n = cleanedPoints.Count;
//                string[] pts = new string[n];
//                string storyName = GetStoryName(story);

//                // CORRECTED: elevation is ALREADY in meters - NO CONVERSION
//                for (int i = 0; i < n; i++)
//                {
//                    string pointName = "";
//                    sapModel.PointObj.AddCartesian(
//                        M(cleanedPoints[i].X),
//                        M(cleanedPoints[i].Y),
//                        elevation,  // ← No conversion
//                        ref pointName, "Global");
//                    pts[i] = pointName;
//                }

//                // Create area object
//                string areaName = "";
//                int ret = sapModel.AreaObj.AddByPoint(n, ref pts, ref areaName, section);

//                if (ret == 0 && !string.IsNullOrEmpty(areaName))
//                {
//                    sapModel.AreaObj.SetGroupAssign(areaName, storyName);
//                    System.Diagnostics.Debug.WriteLine(
//                        $"✓ SUCCESS: {areaName} | Section: {section} | Vertices: {n} | " +
//                        $"Area: {Math.Abs(CalculatePolygonArea(cleanedPoints)):F2} m² | " +
//                        $"Elevation: {elevation:F3}m");
//                    return SlabCreationResult.Success;
//                }
//                else
//                {
//                    System.Diagnostics.Debug.WriteLine($"❌ FAILED: AddByPoint returned {ret}");
//                    return SlabCreationResult.Failed;
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
//                return SlabCreationResult.Failed;
//            }
//        }

//        private List<netDxf.Vector2> RemoveDuplicateAndCollinearPoints(List<netDxf.Vector2> points)
//        {
//            if (points.Count < 3)
//                return points;

//            List<netDxf.Vector2> cleaned = new List<netDxf.Vector2>();
//            const double epsilon = 0.001;

//            for (int i = 0; i < points.Count; i++)
//            {
//                var current = points[i];
//                var next = points[(i + 1) % points.Count];

//                double dist = Math.Sqrt(
//                    Math.Pow(next.X - current.X, 2) +
//                    Math.Pow(next.Y - current.Y, 2));

//                if (dist > epsilon)
//                {
//                    cleaned.Add(current);
//                }
//            }

//            return cleaned;
//        }

//        private double CalculatePolygonArea(List<netDxf.Vector2> points)
//        {
//            if (points.Count < 3)
//                return 0;

//            double polygonArea = 0;
//            for (int i = 0; i < points.Count; i++)
//            {
//                int j = (i + 1) % points.Count;
//                polygonArea += points[i].X * points[j].Y;
//                polygonArea -= points[j].X * points[i].Y;
//            }

//            return (polygonArea / 2.0) * MM_TO_M * MM_TO_M;
//        }

//        private string GetStoryName(int story)
//        {
//            try
//            {
//                int numStories = 0;
//                string[] storyNames = null;
//                int ret = sapModel.Story.GetNameList(ref numStories, ref storyNames);

//                // ✅ FIXED: Use story index directly (0-based)
//                if (ret == 0 && storyNames != null && story >= 0 && story < storyNames.Length)
//                {
//                    return storyNames[story];  // No subtraction
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"Error getting story name: {ex.Message}");
//            }

//            // Fallback
//            return story == 0 ? "Base" : $"Story{story + 1}";
//        }
//    }
//}
// ============================================================================
// FILE: Importers/SlabImporterEnhanced.cs
// VERSION: 3.0 — Full slab layer catalogue per specification
// ============================================================================
// Slab thickness rules:
//   CYAN  layers  → Cantilever (user input based on span)
//   YELLOW layers → User input fixed thickness (passed via slabConfig)
//   WHITE  layers → User input area rule (auto from polygon area)
// ============================================================================

using ETABSv1;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ETAB_Automation.Core;
using static netDxf.Entities.HatchBoundaryPath;

namespace ETABS_CAD_Automation.Importers
{
    public class SlabImporterEnhanced
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private Dictionary<string, int> slabConfig;
        private GradeScheduleManager gradeSchedule;

        private const double MM_TO_M = 0.001;
        private const double CLOSURE_TOLERANCE = 10000.0;
        private const double MIN_AREA = 0.0001;

        private double M(double mm) => mm * MM_TO_M;

        private static Dictionary<string, SlabSectionInfo> availableSlabSections =
            new Dictionary<string, SlabSectionInfo>();

        private class SlabSectionInfo
        {
            public string SectionName { get; set; }
            public int ThicknessMm { get; set; }
            public string Grade { get; set; }
        }

        // ====================================================================
        // SLAB THICKNESS RULES
        // ====================================================================

        // Area-based rules (white layers)
        private static readonly List<(int thickness, double maxArea)> AreaRules =
            new List<(int, double)>
            {
                (125, 14), (135, 17), (150, 22), (160, 25),
                (175, 32), (200, 42), (250, 70)
            };

        // Cantilever span-based rules (cyan layers)
        private static readonly List<(int thickness, double maxSpan)> CantileverRules =
            new List<(int, double)>
            {
                (125, 1.0), (160, 1.5), (180, 1.8), (200, 5.0)
            };

        // ====================================================================
        // LAYER CLASSIFICATION
        // ====================================================================

        private enum SlabRule { AreaBased, CantileverSpan, UserThickness }

        // Yellow layers — fixed user-input thickness (key comes from slabConfig)
        private static readonly HashSet<string> UserThicknessLayers = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "S-FIRE TENDER",
            "S-LOBBY",
            "S-OHT",
            "S-STAIRCASE",
            "S-TERRACE FIRE TANK",
            "S-UGT",
            "S-LANDSCAPE",
            "S-SWIMMING",
            "S-DG",
            "S-STP"
        };

        // Cyan layers — cantilever span logic
        private static readonly HashSet<string> CantileverLayers = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "S-CANTILEVER BALCONY",
            "S-CANTILVER BALCONY",        // common typo variant
            "S-CANTILEVER CHAJJA",
            "S-CANTILEVER CHAJJA+ODU",
            "S-BALCONY SLABS"
        };

        // ====================================================================
        // slabConfig KEYS expected from UI
        // ====================================================================
        // "Lobby"          → S-LOBBY
        // "Stair"          → S-STAIRCASE
        // "FireTender"     → S-FIRE TENDER
        // "OHT"            → S-OHT
        // "TerraceFire"    → S-TERRACE FIRE TANK
        // "UGT"            → S-UGT
        // "Landscape"      → S-LANDSCAPE
        // "Swimming"       → S-SWIMMING
        // "DG"             → S-DG
        // "STP"            → S-STP

        private int GetUserThickness(string layerUpper)
        {
            // Map layer name → slabConfig key
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["S-FIRE TENDER"] = "FireTender",
                ["S-LOBBY"] = "Lobby",
                ["S-OHT"] = "OHT",
                ["S-STAIRCASE"] = "Stair",
                ["S-TERRACE FIRE TANK"] = "TerraceFire",
                ["S-UGT"] = "UGT",
                ["S-LANDSCAPE"] = "Landscape",
                ["S-SWIMMING"] = "Swimming",
                ["S-DG"] = "DG",
                ["S-STP"] = "STP"
            };

            if (map.TryGetValue(layerUpper, out string key) &&
                slabConfig.TryGetValue(key, out int t))
                return t;

            // Fallback: try partial match
            foreach (var kvp in map)
                if (layerUpper.Contains(kvp.Key) || kvp.Key.Contains(layerUpper))
                    if (slabConfig.TryGetValue(kvp.Value, out int t2)) return t2;

            return 150; // safe default
        }

        private SlabRule ClassifyLayer(string layerName)
        {
            string u = layerName.ToUpperInvariant().Trim();

            if (CantileverLayers.Contains(u) ||
                u.Contains("CANTILEVER") || u.Contains("CANTILVER") ||
                u.Contains("CHAJJA") || u.Contains("BALCONY"))
                return SlabRule.CantileverSpan;

            if (UserThicknessLayers.Contains(u))
                return SlabRule.UserThickness;

            return SlabRule.AreaBased;
        }

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public SlabImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            Dictionary<string, int> config = null,
            GradeScheduleManager gradeManager = null)
        {
            sapModel = model;
            dxfDoc = doc;
            gradeSchedule = gradeManager;

            // Default config covers all user-thickness layers
            slabConfig = config ?? new Dictionary<string, int>
            {
                ["Lobby"] = 160,
                ["Stair"] = 175,
                ["FireTender"] = 200,
                ["OHT"] = 200,
                ["TerraceFire"] = 200,
                ["UGT"] = 250,
                ["Landscape"] = 175,
                ["Swimming"] = 250,
                ["DG"] = 200,
                ["STP"] = 200
            };

            LoadAvailableSlabSections();
        }

        // ====================================================================
        // SECTION LOADING
        // ====================================================================

        private void LoadAvailableSlabSections()
        {
            if (availableSlabSections.Count > 0) return;
            try
            {
                availableSlabSections.Clear();
                int num = 0; string[] names = null;
                int ret = sapModel.PropArea.GetNameList(ref num, ref names);

                if (ret == 0 && names != null)
                {
                    var pattern = new Regex(@"^S(\d+)SM(\d+)", RegexOptions.IgnoreCase);
                    foreach (string name in names)
                    {
                        var m = pattern.Match(name);
                        if (m.Success)
                        {
                            availableSlabSections[name] = new SlabSectionInfo
                            {
                                SectionName = name,
                                ThicknessMm = int.Parse(m.Groups[1].Value),
                                Grade = m.Groups[2].Value
                            };
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"✓ Loaded {availableSlabSections.Count} slab sections");

                if (availableSlabSections.Count == 0)
                    DefineFallbackSections();
            }
            catch { DefineFallbackSections(); }
        }

        private void DefineFallbackSections()
        {
            int[] thicknesses = { 100, 125, 135, 150, 160, 175, 180, 200, 225, 250 };
            foreach (int t in thicknesses)
            {
                string name = $"SLAB{t}";
                sapModel.PropArea.SetSlab(name, eSlabType.Slab, eShellType.ShellThin,
                    "CONC", t * 0.001, 12, "CONC", "CONC");
                availableSlabSections[name] = new SlabSectionInfo
                {
                    SectionName = name,
                    ThicknessMm = t,
                    Grade = "Default"
                };
            }
        }

        private string GetClosestSlabSection(int requiredThickness, string preferredGrade = null)
        {
            if (availableSlabSections.Count == 0)
                throw new InvalidOperationException("No slab sections available.");

            string gradeToMatch = preferredGrade?.Replace("M", "").Replace("m", "").Trim();
            string bestMatch = null;
            int minDiff = int.MaxValue;

            if (!string.IsNullOrEmpty(gradeToMatch))
            {
                // Exact thickness + grade
                foreach (var kvp in availableSlabSections)
                    if (kvp.Value.ThicknessMm == requiredThickness && kvp.Value.Grade == gradeToMatch)
                        return kvp.Key;

                // Closest thickness with grade
                foreach (var kvp in availableSlabSections)
                {
                    if (kvp.Value.Grade != gradeToMatch) continue;
                    int diff = Math.Abs(kvp.Value.ThicknessMm - requiredThickness);
                    if (diff < minDiff) { minDiff = diff; bestMatch = kvp.Key; }
                }
                if (bestMatch != null) return bestMatch;
            }

            // Grade-agnostic fallback
            minDiff = int.MaxValue;
            foreach (var kvp in availableSlabSections)
            {
                int diff = Math.Abs(kvp.Value.ThicknessMm - requiredThickness);
                if (diff < minDiff) { minDiff = diff; bestMatch = kvp.Key; }
            }
            return bestMatch ??
                throw new InvalidOperationException($"No slab section for {requiredThickness}mm");
        }

        // ====================================================================
        // THICKNESS CALCULATORS
        // ====================================================================

        private int ThicknessFromArea(double areaM2)
        {
            foreach (var rule in AreaRules)
                if (areaM2 <= rule.maxArea) return rule.thickness;
            return 250;
        }

        private int ThicknessFromSpan(double spanM)
        {
            foreach (var rule in CantileverRules)
                if (spanM <= rule.maxSpan) return rule.thickness;
            return 200;
        }

        private double CalculateCantileverSpan(List<netDxf.Vector2> pts)
        {
            if (pts.Count < 3) return 0;
            double minEdge = double.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                int j = (i + 1) % pts.Count;
                double dx = pts[i].X - pts[j].X, dy = pts[i].Y - pts[j].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < minEdge) minEdge = len;
            }
            return minEdge * MM_TO_M;
        }

        // ====================================================================
        // SECTION DETERMINATION
        // ====================================================================

        private string DetermineSlabSection(string layerName, List<netDxf.Vector2> pts,
            string preferredGrade)
        {
            var rule = ClassifyLayer(layerName);

            switch (rule)
            {
                case SlabRule.CantileverSpan:
                    {
                        double span = CalculateCantileverSpan(pts);
                        int t = ThicknessFromSpan(span);
                        System.Diagnostics.Debug.WriteLine(
                            $"  Cantilever [{layerName}]: span={span:F2}m → {t}mm");
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                case SlabRule.UserThickness:
                    {
                        int t = GetUserThickness(layerName.ToUpperInvariant().Trim());
                        System.Diagnostics.Debug.WriteLine(
                            $"  UserThickness [{layerName}]: {t}mm");
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                default: // AreaBased
                    {
                        double area = Math.Abs(CalculatePolygonArea(pts));
                        int t = ThicknessFromArea(area);
                        System.Diagnostics.Debug.WriteLine(
                            $"  AreaBased [{layerName}]: area={area:F2}m² → {t}mm");
                        return GetClosestSlabSection(t, preferredGrade);
                    }
            }
        }

        // ====================================================================
        // PUBLIC IMPORT METHOD
        // ====================================================================

        public void ImportSlabs(Dictionary<string, string> layerMapping,
            double elevation, int story)
        {
            var slabLayers = layerMapping
                .Where(x => x.Value == "Slab")
                .Select(x => x.Key)
                .ToList();

            if (slabLayers.Count == 0) return;

            string slabGrade = gradeSchedule?.GetBeamSlabGradeForStory(story);

            System.Diagnostics.Debug.WriteLine(
                $"\n========== IMPORTING SLABS - Story {story} ==========");
            System.Diagnostics.Debug.WriteLine(
                $"Elevation: {elevation:F3}m | Grade: {slabGrade ?? "default"}");

            int ok = 0, fail = 0, skip = 0;

            foreach (string layerName in slabLayers)
            {
                System.Diagnostics.Debug.WriteLine($"\n--- Layer: {layerName} " +
                    $"[{ClassifyLayer(layerName)}] ---");

                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    var r = CreateSlabFromPolyline(poly, elevation, layerName,
                        story, slabGrade);
                    if (r == Result.Success) ok++;
                    else if (r == Result.Failed) fail++;
                    else skip++;
                }

                foreach (var hatch in dxfDoc.Entities.Hatches
                    .Where(h => h.Layer.Name == layerName))
                {
                    var r = CreateSlabFromHatch(hatch, elevation, layerName,
                        story, slabGrade);
                    if (r == Result.Success) ok++;
                    else if (r == Result.Failed) fail++;
                    else skip++;
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"\n✓ {ok}  ❌ {fail}  ⊘ {skip}");
        }

        // ====================================================================
        // CREATION HELPERS
        // ====================================================================

        private enum Result { Success, Failed, Skipped }

        private Result CreateSlabFromPolyline(Polyline2D poly, double elevation,
            string layerName, int story, string grade)
        {
            try
            {
                var verts = poly.Vertexes;
                if (verts == null || verts.Count < 3) return Result.Skipped;

                var pts = verts.Select(v => v.Position).ToList();
                if (!IsClosedOrAutoClose(ref pts)) return Result.Skipped;

                string section = DetermineSlabSection(layerName, pts, grade);
                return CreateSlabFromPoints(pts, elevation, section, story);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {ex.Message}");
                return Result.Failed;
            }
        }

        private Result CreateSlabFromHatch(Hatch hatch, double elevation,
            string layerName, int story, string grade)
        {
            try
            {
                Result overall = Result.Skipped;
                foreach (var bp in hatch.BoundaryPaths)
                {
                    var verts = ExtractHatchBoundary(bp.Edges);
                    if (verts.Count < 3) continue;
                    if (!IsClosedOrAutoClose(ref verts)) continue;
                    string section = DetermineSlabSection(layerName, verts, grade);
                    var r = CreateSlabFromPoints(verts, elevation, section, story);
                    if (r == Result.Success) overall = Result.Success;
                    else if (r == Result.Failed && overall != Result.Success)
                        overall = Result.Failed;
                }
                return overall;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {ex.Message}");
                return Result.Failed;
            }
        }

        private List<netDxf.Vector2> ExtractHatchBoundary(
            IReadOnlyList<HatchBoundaryPath.Edge> edges)
        {
            var pts = new List<netDxf.Vector2>();
            foreach (var edge in edges)
            {
                if (edge is HatchBoundaryPath.Line le)
                    pts.Add(le.Start);
                else if (edge is HatchBoundaryPath.Arc ae)
                    pts.AddRange(TessellateArc(ae));
                else if (edge is HatchBoundaryPath.Spline se)
                    pts.AddRange(se.ControlPoints.Select(cp =>
                        new netDxf.Vector2(cp.X, cp.Y)));
            }
            return pts;
        }

        private List<netDxf.Vector2> TessellateArc(HatchBoundaryPath.Arc arc)
        {
            var pts = new List<netDxf.Vector2>();
            int segs = 16;
            double start = arc.StartAngle * Math.PI / 180;
            double end = arc.EndAngle * Math.PI / 180;
            if (end < start) end += 2 * Math.PI;
            double step = (end - start) / segs;
            for (int i = 0; i <= segs; i++)
            {
                double a = start + i * step;
                pts.Add(new netDxf.Vector2(
                    arc.Center.X + arc.Radius * Math.Cos(a),
                    arc.Center.Y + arc.Radius * Math.Sin(a)));
            }
            return pts;
        }

        private bool IsClosedOrAutoClose(ref List<netDxf.Vector2> pts)
        {
            if (pts.Count < 3) return false;
            var f = pts[0]; var l = pts[pts.Count - 1];
            double gap = Math.Sqrt(Math.Pow(l.X - f.X, 2) + Math.Pow(l.Y - f.Y, 2));
            if (gap < CLOSURE_TOLERANCE)
            {
                if (gap < 0.1) pts.RemoveAt(pts.Count - 1);
                return true;
            }
            return false;
        }

        private Result CreateSlabFromPoints(List<netDxf.Vector2> pts, double elevation,
            string section, int story)
        {
            try
            {
                double area = CalculatePolygonArea(pts);
                if (Math.Abs(area) < MIN_AREA) return Result.Skipped;
                if (area < 0) pts.Reverse();

                var clean = RemoveDuplicates(pts);
                if (clean.Count < 3) return Result.Skipped;

                int n = clean.Count;
                string[] ptNames = new string[n];
                string storyName = GetStoryName(story);

                for (int i = 0; i < n; i++)
                {
                    string pn = "";
                    sapModel.PointObj.AddCartesian(
                        M(clean[i].X), M(clean[i].Y), elevation, ref pn, "Global");
                    ptNames[i] = pn;
                }

                string areaName = "";
                int ret = sapModel.AreaObj.AddByPoint(n, ref ptNames, ref areaName, section);
                if (ret == 0 && !string.IsNullOrEmpty(areaName))
                {
                    sapModel.AreaObj.SetGroupAssign(areaName, storyName);
                    System.Diagnostics.Debug.WriteLine(
                        $"✓ {areaName} | {section} | {n}pts | {Math.Abs(area):F2}m²");
                    return Result.Success;
                }
                return Result.Failed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {ex.Message}");
                return Result.Failed;
            }
        }

        private List<netDxf.Vector2> RemoveDuplicates(List<netDxf.Vector2> pts)
        {
            var clean = new List<netDxf.Vector2>();
            const double eps = 0.001;
            for (int i = 0; i < pts.Count; i++)
            {
                var cur = pts[i];
                var next = pts[(i + 1) % pts.Count];
                double dx = next.X - cur.X, dy = next.Y - cur.Y;
                if (Math.Sqrt(dx * dx + dy * dy) > eps) clean.Add(cur);
            }
            return clean;
        }

        private double CalculatePolygonArea(List<netDxf.Vector2> pts)
        {
            if (pts.Count < 3) return 0;
            double a = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                int j = (i + 1) % pts.Count;
                a += pts[i].X * pts[j].Y - pts[j].X * pts[i].Y;
            }
            return (a / 2.0) * MM_TO_M * MM_TO_M;
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
}
