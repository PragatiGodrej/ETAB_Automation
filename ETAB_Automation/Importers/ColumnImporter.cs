//// ============================================================================
//// FILE: Importers/ColumnImporter.cs — VERSION 1.1
//// ============================================================================
////
//// PURPOSE:
////   Reads polylines on C- layers from the centerline DXF plan, computes the
////   centroid of each polyline (column outline), defines a rectangular column
////   section C{B}X{D}M{grade} in ETABS and places a vertical frame object at
////   every centroid.
////
//// HEIGHT:  Column height = storyHeight (same as walls — no separate input).
//// GRADE:   Same concrete grade as walls for this floor (GradeScheduleManager).
//// SECTION: C{B}X{D}M{grade}  e.g. "C300X450M40"
////
//// CALL SITE (CADImporterEnhanced, inside per-floor loop):
////
////   var columnLayers = floorConfig.LayerMapping
////       .Where(kv => kv.Value.Equals("Column", StringComparison.OrdinalIgnoreCase))
////       .Select(kv => kv.Key).ToList();
////
////   if (columnLayers.Count > 0 && floorConfig.ColumnB > 0 && floorConfig.ColumnD > 0)
////   {
////       double colBaseZ = (isBasement && foundationHeight > 0) ? foundationHeight : geomBase;
////       var colImporter = new ColumnImporter(sapModel, dxfDoc, gradeSchedule, currentStoryIndex);
////       colImporter.ImportColumns(columnLayers, floorConfig.ColumnB, floorConfig.ColumnD,
////           colBaseZ, wallHt, storyName);
////   }
////
//// ============================================================================

//using ETAB_Automation.Core;
//using ETABSv1;
//using netDxf;
//using netDxf.Entities;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace ETABS_CAD_Automation.Importers
//{
//    public class ColumnImporter
//    {
//        // ====================================================================
//        // FIELDS
//        // ====================================================================

//        private readonly cSapModel sapModel;
//        private readonly DxfDocument dxfDoc;
//        private readonly GradeScheduleManager gradeSchedule;
//        private readonly int storyIndex;

//        private const double X_TO_M = 0.001;   // CAD mm → metres
//        private const double Y_TO_M = 0.001;
//        private double MX(double x) => x * X_TO_M;
//        private double MY(double y) => y * Y_TO_M;

//        private int columnsCreated = 0;
//        private int columnsFailed = 0;

//        // Static cache: avoid redefining the same section across floors
//        private static readonly HashSet<string> definedSections =
//            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//        // ====================================================================
//        // CONSTRUCTOR
//        // ====================================================================

//        public ColumnImporter(
//            cSapModel model,
//            DxfDocument doc,
//            GradeScheduleManager gradeManager,
//            int storyIdx)
//        {
//            sapModel = model;
//            dxfDoc = doc;
//            gradeSchedule = gradeManager;
//            storyIndex = storyIdx;
//        }

//        // ====================================================================
//        // PUBLIC ENTRY POINT
//        // ====================================================================

//        /// <summary>
//        /// Reads C- layer polylines, computes centroids, defines the column
//        /// section and places vertical frame objects in ETABS.
//        /// Column height = storyHeight (same as walls — no separate input).
//        /// </summary>
//        public bool ImportColumns(
//            List<string> columnLayers,
//            int colB_mm,
//            int colD_mm,
//            double storyBaseElevation,
//            double storyHeight,
//            string storyName)
//        {
//            columnsCreated = 0;
//            columnsFailed = 0;

//            Debug.WriteLine($"\n──── ColumnImporter: {storyName} ────");
//            Debug.WriteLine($"     B={colB_mm}mm  D={colD_mm}mm");
//            Debug.WriteLine($"     BaseZ={storyBaseElevation:F3}m  H={storyHeight:F3}m (= wall height)");

//            if (columnLayers == null || columnLayers.Count == 0)
//            {
//                Debug.WriteLine("  ⚠ No column layers provided — skipping.");
//                return false;
//            }

//            // Grade = same concrete grade as walls on this floor
//            string grade = ResolveGrade();
//            string gradeNum = grade.Replace("M", "").Replace("m", "").Trim();
//            string sectionName = $"C{colB_mm}X{colD_mm}M{gradeNum}";

//            if (!EnsureSectionDefined(sectionName, colB_mm, colD_mm, grade))
//            {
//                Debug.WriteLine($"  ❌ Could not define section '{sectionName}'. Skipping floor.");
//                return false;
//            }

//            var centroids = ExtractColumnCentroids(columnLayers);
//            Debug.WriteLine($"  Found {centroids.Count} column polylines on C- layers.");

//            if (centroids.Count == 0)
//            {
//                Debug.WriteLine("  ⚠ No column polylines found.");
//                return false;
//            }

//            double topZ = storyBaseElevation + storyHeight;
//            foreach (var (cx, cy, angle) in centroids)
//                PlaceColumn(cx, cy, storyBaseElevation, topZ, sectionName, angle, storyName);

//            Debug.WriteLine($"  ✅ Columns placed={columnsCreated}  failed={columnsFailed}");
//            return columnsCreated > 0;
//        }

//        // ====================================================================
//        // GRADE RESOLUTION
//        // ====================================================================

//        private string ResolveGrade()
//        {
//            try
//            {
//                string raw = gradeSchedule?.GetWallGrade(storyIndex) ?? "M30";
//                return raw.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? raw : $"M{raw}";
//            }
//            catch { return "M30"; }
//        }

//        // ====================================================================
//        // SECTION DEFINITION
//        // ====================================================================

//        /// <summary>
//        /// Defines a rectangular column section via PropFrame.SetRectangle.
//        /// t3 = D (depth), t2 = B (width).
//        /// </summary>
//        private bool EnsureSectionDefined(string sectionName, int b_mm, int d_mm, string grade)
//        {
//            if (definedSections.Contains(sectionName))
//            {
//                Debug.WriteLine($"  ✓ Section '{sectionName}' already defined (cached).");
//                return true;
//            }

//            string matProp = ResolveMaterialName(grade);
//            double b_m = b_mm / 1000.0;
//            double d_m = d_mm / 1000.0;

//            // t3 = section depth (D), t2 = section width (B)
//            int ret = sapModel.PropFrame.SetRectangle(sectionName, matProp, d_m, b_m);

//            if (ret == 0)
//            {
//                definedSections.Add(sectionName);
//                Debug.WriteLine($"  ✓ Defined '{sectionName}' ({b_mm}×{d_mm}mm, mat={matProp})");
//                return true;
//            }

//            Debug.WriteLine($"  ❌ PropFrame.SetRectangle failed (ret={ret}) for '{sectionName}'");
//            return false;
//        }

//        /// <summary>
//        /// Fuzzy-matches grade string against ETABS material property names.
//        /// Tries exact → contains → numeric substring. Falls back to grade as-is.
//        /// </summary>
//        private string ResolveMaterialName(string grade)
//        {
//            try
//            {
//                int nMat = 0;
//                string[] matNames = null;
//                if (sapModel.PropMaterial.GetNameList(ref nMat, ref matNames) == 0 && matNames != null)
//                {
//                    // Exact match (e.g. "M40")
//                    string m = matNames.FirstOrDefault(n =>
//                        string.Equals(n, grade, StringComparison.OrdinalIgnoreCase));
//                    if (m != null) return m;

//                    // Contains match (e.g. "Concrete M40")
//                    m = matNames.FirstOrDefault(n =>
//                        n.IndexOf(grade, StringComparison.OrdinalIgnoreCase) >= 0);
//                    if (m != null) return m;

//                    // Numeric part only (e.g. "40" from "M40")
//                    string num = Regex.Match(grade, @"\d+").Value;
//                    if (!string.IsNullOrEmpty(num))
//                    {
//                        m = matNames.FirstOrDefault(n =>
//                            n.IndexOf(num, StringComparison.OrdinalIgnoreCase) >= 0);
//                        if (m != null) return m;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"  ⚠ Material resolve error: {ex.Message}");
//            }

//            Debug.WriteLine($"  ⚠ Material '{grade}' not matched in ETABS — using grade string directly.");
//            return grade;
//        }

//        // ====================================================================
//        // CENTROID EXTRACTION
//        // ====================================================================

//        private List<(double cx, double cy, double angle)> ExtractColumnCentroids(
//            List<string> columnLayers)
//        {
//            var result = new List<(double, double, double)>();
//            var layerSet = new HashSet<string>(
//                columnLayers.Select(l => l.ToUpperInvariant()));

//            // LwPolyline (standard 2D plan entities)
//            foreach (var poly in dxfDoc.Entities.LwPolylines)
//            {
//                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;
//                var verts = poly.Vertexes.Select(v => (v.Position.X, v.Position.Y)).ToList();
//                if (verts.Count < 2) continue;
//                var (cx, cy) = Centroid(verts);
//                result.Add((MX(cx), MY(cy), LongestEdgeAngle(verts)));
//            }

//            // Polyline2D
//            foreach (var poly in dxfDoc.Entities.Polylines2D)
//            {
//                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;
//                var verts = poly.Vertexes.Select(v => (v.Position.X, v.Position.Y)).ToList();
//                if (verts.Count < 2) continue;
//                var (cx, cy) = Centroid(verts);
//                result.Add((MX(cx), MY(cy), LongestEdgeAngle(verts)));
//            }

//            return result;
//        }

//        private static (double cx, double cy) Centroid(List<(double x, double y)> v)
//        {
//            double sx = 0, sy = 0;
//            foreach (var (x, y) in v) { sx += x; sy += y; }
//            return (sx / v.Count, sy / v.Count);
//        }

//        /// <summary>
//        /// Angle (degrees from +X axis) of the longest edge.
//        /// Used to orient the column local axis in plan.
//        /// </summary>
//        private static double LongestEdgeAngle(List<(double x, double y)> v)
//        {
//            double maxLen = -1, angle = 0;
//            for (int i = 0; i < v.Count; i++)
//            {
//                int j = (i + 1) % v.Count;
//                double dx = v[j].x - v[i].x, dy = v[j].y - v[i].y;
//                double len = Math.Sqrt(dx * dx + dy * dy);
//                if (len > maxLen) { maxLen = len; angle = Math.Atan2(dy, dx) * 180.0 / Math.PI; }
//            }
//            return angle;
//        }

//        // ====================================================================
//        // COLUMN PLACEMENT
//        // ====================================================================

//        private void PlaceColumn(
//            double cx, double cy,
//            double baseZ, double topZ,
//            string sectionName,
//            double angleDeg,
//            string storyName)
//        {
//            try
//            {
//                string frameName = string.Empty;

//                int ret = sapModel.FrameObj.AddByCoord(
//                    cx, cy, baseZ,
//                    cx, cy, topZ,
//                    ref frameName,
//                    sectionName,
//                    storyName + "_COL");

//                if (ret != 0)
//                {
//                    Debug.WriteLine($"  ❌ AddByCoord failed (ret={ret}) @ ({cx:F3},{cy:F3})");
//                    columnsFailed++;
//                    return;
//                }

//                // Rotate local axis so local-2 aligns with longest polyline edge
//                if (Math.Abs(angleDeg) > 0.01)
//                {
//                    int retAng = sapModel.FrameObj.SetLocalAxes(frameName, angleDeg);
//                    if (retAng != 0)
//                        Debug.WriteLine($"  ⚠ SetLocalAxes failed (ret={retAng}) '{frameName}'");
//                }

//                Debug.WriteLine(
//                    $"  + '{frameName}' @ ({cx:F3},{cy:F3}) Z={baseZ:F3}→{topZ:F3} angle={angleDeg:F1}°");
//                columnsCreated++;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"  ❌ Exception @ ({cx:F3},{cy:F3}): {ex.Message}");
//                columnsFailed++;
//            }
//        }

//        // ====================================================================
//        // DIAGNOSTICS / UTILITIES
//        // ====================================================================

//        public int ColumnsCreated => columnsCreated;
//        public int ColumnsFailed => columnsFailed;

//        /// <summary>
//        /// Clears the static section cache.
//        /// Call once at the start of ImportMultiFloorTypeCAD (new model).
//        /// </summary>
//        public static void ClearSectionCache() => definedSections.Clear();
//    }
//}
//// ============================================================================
//// END OF FILE
// ============================================================================
// FILE: Importers/ColumnImporter.cs — VERSION 1.3
// ============================================================================
//
// PURPOSE:
//   Reads polylines on C- layers from the centerline DXF plan, computes the
//   centroid of each polyline (column outline), defines a rectangular column
//   section C{B}X{D}M{grade} in ETABS and places a vertical frame object at
//   every centroid.
//
// HEIGHT:  Column height = storyHeight (same as walls — no separate input).
// GRADE:   Same concrete grade as walls for this floor (GradeScheduleManager).
// SECTION: C{B}X{D}M{grade}  e.g. "C300X450M40"
//
// ============================================================================
// FIXES v1.3 — all four compiler errors resolved for netDxf 2023.11.10
// ============================================================================
//
//  ERROR 1: 'LwPolyline' not found
//    → Renamed to Polyline2D (see netDxf changelog).
//      No reference to LwPolyline remains in this file.
//
//  ERROR 2 & 3: 'Vector3' does not contain a definition for 'Position'
//    → Polyline2D.Vertexes is List<Polyline2DVertex>.
//      Polyline2DVertex.Position is a Vector2  →  access as v.Position.X / v.Position.Y
//      Polyline3D.Vertexes is List<Vector3>    →  access as v.X / v.Y  (no .Position wrapper)
//
//  ERROR 4: 'Layout' does not exist in namespace 'netDxf.Tables'
//    → Layout moved to netDxf.Objects.  Layout.ModelSpaceName is the correct
//      static string.  Access: dxfDoc.Layouts[Layout.ModelSpaceName].AssociatedBlock
//
// ============================================================================

using ETAB_Automation.Core;
using ETABSv1;
using netDxf;
using netDxf.Entities;
using netDxf.Objects;          // <-- Layout lives here, NOT in netDxf.Tables
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ETABS_CAD_Automation.Importers
{
    public class ColumnImporter
    {
        // ====================================================================
        // FIELDS
        // ====================================================================

        private readonly cSapModel sapModel;
        private readonly DxfDocument dxfDoc;
        private readonly GradeScheduleManager gradeSchedule;
        private readonly int storyIndex;

        private const double X_TO_M = 0.001;   // CAD mm → metres
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        private int columnsCreated = 0;
        private int columnsFailed = 0;

        // Static cache: avoid redefining the same section across floors
        private static readonly HashSet<string> definedSections =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public ColumnImporter(
            cSapModel model,
            DxfDocument doc,
            GradeScheduleManager gradeManager,
            int storyIdx)
        {
            sapModel = model;
            dxfDoc = doc;
            gradeSchedule = gradeManager;
            storyIndex = storyIdx;
        }

        // ====================================================================
        // PUBLIC ENTRY POINT
        // ====================================================================

        /// <summary>
        /// Reads C- layer polylines, computes centroids, defines the column
        /// section and places vertical frame objects in ETABS.
        /// Column height = storyHeight (same as walls — no separate input).
        /// </summary>
        public bool ImportColumns(
            List<string> columnLayers,
            int colB_mm,
            int colD_mm,
            double storyBaseElevation,
            double storyHeight,
            string storyName)
        {
            columnsCreated = 0;
            columnsFailed = 0;

            Debug.WriteLine($"\n──── ColumnImporter: {storyName} ────");
            Debug.WriteLine($"     B={colB_mm}mm  D={colD_mm}mm");
            Debug.WriteLine($"     BaseZ={storyBaseElevation:F3}m  H={storyHeight:F3}m (= wall height)");

            if (columnLayers == null || columnLayers.Count == 0)
            {
                Debug.WriteLine("  ⚠ No column layers provided — skipping.");
                return false;
            }

            // Grade = same concrete grade as walls on this floor
            string grade = ResolveGrade();
            string gradeNum = grade.Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"C{colB_mm}X{colD_mm}M{gradeNum}";

            if (!EnsureSectionDefined(sectionName, colB_mm, colD_mm, grade))
            {
                Debug.WriteLine($"  ❌ Could not define section '{sectionName}'. Skipping floor.");
                return false;
            }

            var centroids = ExtractColumnCentroids(columnLayers);
            Debug.WriteLine($"  Found {centroids.Count} column polylines on C- layers.");

            if (centroids.Count == 0)
            {
                Debug.WriteLine("  ⚠ No column polylines found.");
                return false;
            }

            double topZ = storyBaseElevation + storyHeight;
            foreach (var (cx, cy, angle) in centroids)
                PlaceColumn(cx, cy, storyBaseElevation, topZ, sectionName, angle, storyName);

            Debug.WriteLine($"  ✅ Columns placed={columnsCreated}  failed={columnsFailed}");
            return columnsCreated > 0;
        }

        // ====================================================================
        // GRADE RESOLUTION
        // ====================================================================

        private string ResolveGrade()
        {
            try
            {
                string raw = gradeSchedule?.GetWallGrade(storyIndex) ?? "M30";
                return raw.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? raw : $"M{raw}";
            }
            catch { return "M30"; }
        }

        // ====================================================================
        // SECTION DEFINITION
        // ====================================================================

        /// <summary>
        /// Defines a rectangular column section via PropFrame.SetRectangle.
        /// t3 = D (depth), t2 = B (width).
        /// </summary>
        private bool EnsureSectionDefined(string sectionName, int b_mm, int d_mm, string grade)
        {
            if (definedSections.Contains(sectionName))
            {
                Debug.WriteLine($"  ✓ Section '{sectionName}' already defined (cached).");
                return true;
            }

            string matProp = ResolveMaterialName(grade);
            double b_m = b_mm / 1000.0;
            double d_m = d_mm / 1000.0;

            // t3 = section depth (D), t2 = section width (B)
            int ret = sapModel.PropFrame.SetRectangle(sectionName, matProp, d_m, b_m);

            if (ret == 0)
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✓ Defined '{sectionName}' ({b_mm}×{d_mm}mm, mat={matProp})");
                return true;
            }

            Debug.WriteLine($"  ❌ PropFrame.SetRectangle failed (ret={ret}) for '{sectionName}'");
            return false;
        }

        /// <summary>
        /// Fuzzy-matches grade string against ETABS material property names.
        /// Tries exact → contains → numeric substring. Falls back to grade as-is.
        /// </summary>
        private string ResolveMaterialName(string grade)
        {
            try
            {
                int nMat = 0;
                string[] matNames = null;
                if (sapModel.PropMaterial.GetNameList(ref nMat, ref matNames) == 0 && matNames != null)
                {
                    // Exact match (e.g. "M40")
                    string m = matNames.FirstOrDefault(n =>
                        string.Equals(n, grade, StringComparison.OrdinalIgnoreCase));
                    if (m != null) return m;

                    // Contains match (e.g. "Concrete M40")
                    m = matNames.FirstOrDefault(n =>
                        n.IndexOf(grade, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (m != null) return m;

                    // Numeric part only (e.g. "40" from "M40")
                    string num = Regex.Match(grade, @"\d+").Value;
                    if (!string.IsNullOrEmpty(num))
                    {
                        m = matNames.FirstOrDefault(n =>
                            n.IndexOf(num, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (m != null) return m;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ⚠ Material resolve error: {ex.Message}");
            }

            Debug.WriteLine($"  ⚠ Material '{grade}' not matched in ETABS — using grade string directly.");
            return grade;
        }

        // ====================================================================
        // CENTROID EXTRACTION
        // ====================================================================

        /// <summary>
        /// Extracts centroids and orientation angles from polylines on the
        /// specified C- layers.
        ///
        /// netDxf 2023.11.10 API facts:
        ///
        ///   Polyline2D (formerly LwPolyline)
        ///     • dxfDoc.Entities.Polylines2D
        ///     • Polyline2D.Vertexes → List&lt;Polyline2DVertex&gt;
        ///     • Polyline2DVertex.Position → Vector2  (.X and .Y)
        ///
        ///   Polyline3D (formerly Polyline)
        ///     • dxfDoc.Entities.Polylines3D
        ///     • Polyline3D.Vertexes → List&lt;Vector3&gt;
        ///     • Access coords directly: v.X  and  v.Y  (no .Position wrapper)
        ///
        ///   Layout is in netDxf.Objects, not netDxf.Tables.
        ///   Model space: dxfDoc.Layouts[Layout.ModelSpaceName].AssociatedBlock
        /// </summary>
        private List<(double cx, double cy, double angle)> ExtractColumnCentroids(
            List<string> columnLayers)
        {
            var result = new List<(double, double, double)>();
            var layerSet = new HashSet<string>(
                columnLayers.Select(l => l.ToUpperInvariant()));

            // ----------------------------------------------------------------
            // Polyline2D  (formerly LwPolyline)
            // Vertexes → List<Polyline2DVertex>
            // Polyline2DVertex.Position → Vector2  (.X / .Y)
            // ----------------------------------------------------------------
            foreach (var poly in dxfDoc.Entities.Polylines2D)
            {
                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;

                var verts = poly.Vertexes
                    .Select(v => (x: v.Position.X, y: v.Position.Y))
                    .ToList();

                if (verts.Count < 2) continue;
                var (cx, cy) = Centroid(verts);
                result.Add((MX(cx), MY(cy), LongestEdgeAngle(verts)));
            }

            // ----------------------------------------------------------------
            // Polyline3D  (formerly Polyline — safety net / 3-D outlines)
            // Vertexes → List<Vector3>
            // Vector3 has .X and .Y directly — there is NO .Position property
            // ----------------------------------------------------------------
            foreach (var poly in dxfDoc.Entities.Polylines3D)
            {
                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;

                var verts = poly.Vertexes
                    .Select(v => (x: v.X, y: v.Y))      // Vector3: access .X/.Y directly
                    .ToList();

                if (verts.Count < 2) continue;
                var (cx, cy) = Centroid(verts);
                result.Add((MX(cx), MY(cy), LongestEdgeAngle(verts)));
            }

            // ----------------------------------------------------------------
            // FALLBACK — iterate model-space block directly.
            // Layout is in netDxf.Objects; Layout.ModelSpaceName is the key.
            // ----------------------------------------------------------------
            if (result.Count == 0)
            {
                Debug.WriteLine("  ⚠ Typed collections returned 0 — trying model-space block fallback.");
                try
                {
                    // dxfDoc.Layouts is indexed by layout name.
                    // Layout.ModelSpaceName == "Model"  (from netDxf.Objects.Layout)
                    var modelBlock = dxfDoc.Layouts[Layout.ModelSpaceName]?.AssociatedBlock;

                    if (modelBlock != null)
                    {
                        foreach (var entity in modelBlock.Entities)
                        {
                            if (!layerSet.Contains(entity.Layer.Name.ToUpperInvariant())) continue;

                            List<(double x, double y)> verts = null;

                            if (entity is Polyline2D p2d)
                            {
                                // Polyline2DVertex.Position → Vector2
                                verts = p2d.Vertexes
                                    .Select(v => (x: v.Position.X, y: v.Position.Y))
                                    .ToList();
                            }
                            else if (entity is Polyline3D p3d)
                            {
                                // Vertexes are Vector3 — no .Position wrapper
                                verts = p3d.Vertexes
                                    .Select(v => (x: v.X, y: v.Y))
                                    .ToList();
                            }

                            if (verts == null || verts.Count < 2) continue;
                            var (cx, cy) = Centroid(verts);
                            result.Add((MX(cx), MY(cy), LongestEdgeAngle(verts)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  ⚠ Model-space fallback error: {ex.Message}");
                }
            }

            return result;
        }

        // ====================================================================
        // GEOMETRY HELPERS
        // ====================================================================

        private static (double cx, double cy) Centroid(List<(double x, double y)> v)
        {
            double sx = 0, sy = 0;
            foreach (var (x, y) in v) { sx += x; sy += y; }
            return (sx / v.Count, sy / v.Count);
        }

        /// <summary>
        /// Returns the angle (degrees from +X axis) of the longest edge.
        /// Used to orient the column local-2 axis in plan.
        /// </summary>
        private static double LongestEdgeAngle(List<(double x, double y)> v)
        {
            double maxLen = -1, angle = 0;
            for (int i = 0; i < v.Count; i++)
            {
                int j = (i + 1) % v.Count;
                double dx = v[j].x - v[i].x;
                double dy = v[j].y - v[i].y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > maxLen) { maxLen = len; angle = Math.Atan2(dy, dx) * 180.0 / Math.PI; }
            }
            return angle;
        }

        // ====================================================================
        // COLUMN PLACEMENT
        // ====================================================================

        private void PlaceColumn(
            double cx, double cy,
            double baseZ, double topZ,
            string sectionName,
            double angleDeg,
            string storyName)
        {
            try
            {
                string frameName = string.Empty;

                int ret = sapModel.FrameObj.AddByCoord(
                    cx, cy, baseZ,
                    cx, cy, topZ,
                    ref frameName,
                    sectionName,
                    storyName + "_COL");

                if (ret != 0)
                {
                    Debug.WriteLine($"  ❌ AddByCoord failed (ret={ret}) @ ({cx:F3},{cy:F3})");
                    columnsFailed++;
                    return;
                }

                // Rotate local axis so local-2 aligns with longest polyline edge
                if (Math.Abs(angleDeg) > 0.01)
                {
                    int retAng = sapModel.FrameObj.SetLocalAxes(frameName, angleDeg);
                    if (retAng != 0)
                        Debug.WriteLine($"  ⚠ SetLocalAxes failed (ret={retAng}) '{frameName}'");
                }

                Debug.WriteLine(
                    $"  + '{frameName}' @ ({cx:F3},{cy:F3}) " +
                    $"Z={baseZ:F3}→{topZ:F3} angle={angleDeg:F1}°");
                columnsCreated++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  ❌ Exception @ ({cx:F3},{cy:F3}): {ex.Message}");
                columnsFailed++;
            }
        }

        // ====================================================================
        // DIAGNOSTICS / UTILITIES
        // ====================================================================

        public int ColumnsCreated => columnsCreated;
        public int ColumnsFailed => columnsFailed;

        /// <summary>
        /// Clears the static section cache.
        /// Call once at the start of ImportMultiFloorTypeCAD (new model).
        /// </summary>
        public static void ClearSectionCache() => definedSections.Clear();
    }
}
// ============================================================================
// END OF FILE
// ============================================================================
