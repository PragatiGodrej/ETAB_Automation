

// ============================================================================
// FILE: Importers/ColumnImporter.cs — VERSION 2.0
// ============================================================================
//
// PURPOSE:
//   Reads CLOSED polylines on C- layers from the centerline DXF plan.
//   For each polyline:
//     1. Computes the oriented bounding box → derives B (width) and D (depth) in mm
//     2. Computes the centroid of the polyline
//     3. Defines a rectangular section C{B}X{D}M{grade} in ETABS (if not cached)
//     4. Places a vertical frame object at the centroid with the correct section
//
// CHANGE vs v1.x:
//   • ImportColumns() no longer accepts colB_mm / colD_mm from the caller.
//   • Dimensions are READ from each polyline's own geometry via MeasurePolyline().
//   • Each column may therefore have a unique B×D section.
//
// HEIGHT:  Column height = storyHeight (same as walls).
// GRADE:   Same concrete grade as walls for this floor (GradeScheduleManager).
// SECTION: C{B}X{D}M{grade}  e.g. "C300X450M40"
//
// CALL SITE (CADImporterEnhanced, inside per-floor loop):
//
//   var columnLayers = floorConfig.LayerMapping
//       .Where(kv => kv.Value.Equals("Column", StringComparison.OrdinalIgnoreCase))
//       .Select(kv => kv.Key).ToList();
//
//   if (columnLayers.Count > 0)
//   {
//       double colBaseZ = (isBasement && foundationHeight > 0) ? foundationHeight : geomBase;
//       var colImporter = new ColumnImporter(sapModel, dxfDoc, gradeSchedule, currentStoryIndex);
//       colImporter.ImportColumns(columnLayers, colBaseZ, wallHt, storyName);
//   }
//
// ============================================================================

using ETAB_Automation.Core;
using ETABSv1;
using netDxf;
using netDxf.Entities;
using netDxf.Objects;          // Layout lives here (netDxf 2023.11.10)
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

        private const double CAD_TO_M = 0.001;   // CAD mm → metres
        private double ToM(double v) => v * CAD_TO_M;

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
        /// Reads closed C- layer polylines, measures each polyline's own
        /// B × D dimensions, defines per-column sections and places vertical
        /// frame objects in ETABS.
        /// NOTE: colB_mm / colD_mm are NO LONGER required from the caller.
        /// </summary>
        public bool ImportColumns(
            List<string> columnLayers,
            double storyBaseElevation,
            double storyHeight,
            string storyName)
        {
            columnsCreated = 0;
            columnsFailed = 0;

            Debug.WriteLine($"\n──── ColumnImporter v2.0: {storyName} ────");
            Debug.WriteLine($"     BaseZ={storyBaseElevation:F3}m  H={storyHeight:F3}m");
            Debug.WriteLine("     B and D will be read from each polyline's geometry.");

            if (columnLayers == null || columnLayers.Count == 0)
            {
                Debug.WriteLine("  ⚠ No column layers provided — skipping.");
                return false;
            }

            string grade = ResolveGrade();
            string gradeNum = grade.Replace("M", "").Replace("m", "").Trim();

            var columns = ExtractColumnData(columnLayers);
            Debug.WriteLine($"  Found {columns.Count} closed column polylines on C- layers.");

            if (columns.Count == 0)
            {
                Debug.WriteLine("  ⚠ No column polylines found.");
                return false;
            }

            double topZ = storyBaseElevation + storyHeight;

            foreach (var col in columns)
            {
                // Round dimensions to nearest mm for consistent section naming
                int b_mm = (int)Math.Round(col.B_mm);
                int d_mm = (int)Math.Round(col.D_mm);

                if (b_mm < 10 || d_mm < 10)
                {
                    Debug.WriteLine($"  ⚠ Skipping degenerate polyline — B={b_mm}mm D={d_mm}mm");
                    columnsFailed++;
                    continue;
                }

                string sectionName = $"C{b_mm}X{d_mm}M{gradeNum}";

                if (!EnsureSectionDefined(sectionName, b_mm, d_mm, grade))
                {
                    Debug.WriteLine($"  ❌ Could not define section '{sectionName}'. Skipping column.");
                    columnsFailed++;
                    continue;
                }

                PlaceColumn(col.Cx, col.Cy, storyBaseElevation, topZ,
                            sectionName, col.AngleDeg, storyName);
            }

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
        /// t3 = D (depth along local-3), t2 = B (width along local-2).
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
                if (sapModel.PropMaterial.GetNameList(ref nMat, ref matNames) == 0
                    && matNames != null)
                {
                    string m = matNames.FirstOrDefault(n =>
                        string.Equals(n, grade, StringComparison.OrdinalIgnoreCase));
                    if (m != null) return m;

                    m = matNames.FirstOrDefault(n =>
                        n.IndexOf(grade, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (m != null) return m;

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

            Debug.WriteLine($"  ⚠ Material '{grade}' not matched — using grade string directly.");
            return grade;
        }

        // ====================================================================
        // COLUMN DATA EXTRACTION  (centroid + B×D from polyline geometry)
        // ====================================================================

        /// <summary>Holds all data needed to place and size one column.</summary>
        private struct ColumnData
        {
            public double Cx, Cy;      // Centroid in metres
            public double B_mm;        // Width  (local-2 direction) in mm
            public double D_mm;        // Depth  (local-3 direction) in mm
            public double AngleDeg;    // Orientation of local-2 from +X axis
        }

        /// <summary>
        /// Iterates over every closed polyline on the C- layers.
        /// For each polyline:
        ///   • Projects vertices onto the oriented frame (longest edge = local-2).
        ///   • The span in the local-2 direction → B_mm.
        ///   • The span in the local-3 direction → D_mm.
        ///   • Centroid is the simple vertex average.
        /// </summary>
        private List<ColumnData> ExtractColumnData(List<string> columnLayers)
        {
            var result = new List<ColumnData>();
            var layerSet = new HashSet<string>(
                columnLayers.Select(l => l.ToUpperInvariant()));

            // ---- Polyline2D (formerly LwPolyline) ----
            foreach (var poly in dxfDoc.Entities.Polylines2D)
            {
                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;
                var raw = poly.Vertexes
                    .Select(v => (x: v.Position.X, y: v.Position.Y))
                    .ToList();
                if (raw.Count < 2) continue;
                result.Add(BuildColumnData(raw));
            }

            // ---- Polyline3D (safety net / 3-D outlines — use only X,Y) ----
            foreach (var poly in dxfDoc.Entities.Polylines3D)
            {
                if (!layerSet.Contains(poly.Layer.Name.ToUpperInvariant())) continue;
                var raw = poly.Vertexes
                    .Select(v => (x: v.X, y: v.Y))
                    .ToList();
                if (raw.Count < 2) continue;
                result.Add(BuildColumnData(raw));
            }

            // ---- Fallback: iterate model-space block directly ----
            if (result.Count == 0)
            {
                Debug.WriteLine("  ⚠ Typed collections returned 0 — trying model-space block fallback.");
                try
                {
                    var modelBlock =
                        dxfDoc.Layouts[Layout.ModelSpaceName]?.AssociatedBlock;

                    if (modelBlock != null)
                    {
                        foreach (var entity in modelBlock.Entities)
                        {
                            if (!layerSet.Contains(entity.Layer.Name.ToUpperInvariant())) continue;

                            List<(double x, double y)> raw = null;

                            if (entity is Polyline2D p2d)
                                raw = p2d.Vertexes
                                        .Select(v => (x: v.Position.X, y: v.Position.Y))
                                        .ToList();
                            else if (entity is Polyline3D p3d)
                                raw = p3d.Vertexes
                                        .Select(v => (x: v.X, y: v.Y))
                                        .ToList();

                            if (raw == null || raw.Count < 2) continue;
                            result.Add(BuildColumnData(raw));
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
        // ORIENTED BOUNDING BOX  →  B, D, centroid, angle
        // ====================================================================

        /// <summary>
        /// Given a list of raw CAD vertices (in CAD units = mm):
        ///   1. Computes the centroid (vertex average).
        ///   2. Finds the orientation angle from the longest edge.
        ///   3. Projects all vertices onto the local (u, v) frame.
        ///   4. B = span in u-direction (local-2)   [CAD mm → kept as mm]
        ///      D = span in v-direction (local-3)   [CAD mm → kept as mm]
        ///   5. Converts centroid to metres (× 0.001).
        /// </summary>
        private static ColumnData BuildColumnData(List<(double x, double y)> verts)
        {
            // --- centroid (still in CAD units) ---
            double sx = 0, sy = 0;
            foreach (var (x, y) in verts) { sx += x; sy += y; }
            double cx_cad = sx / verts.Count;
            double cy_cad = sy / verts.Count;

            // --- orientation: longest edge ---
            double angle = LongestEdgeAngle(verts);   // degrees
            double rad = angle * Math.PI / 180.0;
            double cosA = Math.Cos(rad);
            double sinA = Math.Sin(rad);

            // --- project onto local frame, measure extents ---
            double uMin = double.MaxValue, uMax = double.MinValue;
            double vMin = double.MaxValue, vMax = double.MinValue;

            foreach (var (x, y) in verts)
            {
                double u = (x - cx_cad) * cosA + (y - cy_cad) * sinA;
                double v = -(x - cx_cad) * sinA + (y - cy_cad) * cosA;
                if (u < uMin) uMin = u;
                if (u > uMax) uMax = u;
                if (v < vMin) vMin = v;
                if (v > vMax) vMax = v;
            }

            double b_mm = Math.Abs(uMax - uMin);   // width  along local-2
            double d_mm = Math.Abs(vMax - vMin);   // depth  along local-3

            Debug.WriteLine(
                $"  [OBB] centroid=({cx_cad:F1},{cy_cad:F1}) " +
                $"B={b_mm:F1}mm D={d_mm:F1}mm angle={angle:F1}°");

            return new ColumnData
            {
                Cx = cx_cad * 0.001,   // → metres
                Cy = cy_cad * 0.001,
                B_mm = b_mm,
                D_mm = d_mm,
                AngleDeg = angle
            };
        }

        // ====================================================================
        // GEOMETRY HELPERS
        // ====================================================================

        /// <summary>
        /// Returns the angle (degrees from +X axis) of the longest edge.
        /// Used as the orientation of the column local-2 axis in plan.
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
                    storyName);   // must match exact ETABS story name for correct hierarchy

                if (ret != 0)
                {
                    Debug.WriteLine($"  ❌ AddByCoord failed (ret={ret}) @ ({cx:F3},{cy:F3})");
                    columnsFailed++;
                    return;
                }

                // Rotate local axis so local-2 aligns with the longest polyline edge
                if (Math.Abs(angleDeg) > 0.01)
                {
                    int retAng = sapModel.FrameObj.SetLocalAxes(frameName, angleDeg);
                    if (retAng != 0)
                        Debug.WriteLine($"  ⚠ SetLocalAxes failed (ret={retAng}) '{frameName}'");
                }

                Debug.WriteLine(
                    $"  + '{frameName}' [{sectionName}] " +
                    $"@ ({cx:F3},{cy:F3}) Z={baseZ:F3}→{topZ:F3} angle={angleDeg:F1}°");
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
