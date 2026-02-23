
// ============================================================================
// FILE: Importers/SlabImporterEnhanced.cs

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
                    return names[ n - 1 - story];
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }
}
