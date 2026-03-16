




// ============================================================================
// FILE: Importers/WallImporterEnhanced.cs — VERSION 3.3
//
// CHANGES from v3.2:
//   - Added ISCodeVersion parameter to constructor (default = IS2025)
//   - GetWallSection passes isCode to WallThicknessCalculator
//   - FindClosestWallSection unchanged
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

        private int ntaWallThicknessMm;
        private Dictionary<string, int> wallThicknessOverrides;

        // ── IS code edition (IS2016 or IS2025) ───────────────────────────
        private readonly WallThicknessCalculator.ISCodeVersion isCode;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        private int wallsCreated = 0;
        private int wallsFailed = 0;
        private Dictionary<string, int> wallTypeCount = new Dictionary<string, int>();

        // ====================================================================
        // CONSTRUCTOR  (9 arguments — isCode added at end, default IS2025)
        // ====================================================================
        public WallImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            double height,
            int typicalFloors,
            string zone,
            GradeScheduleManager gradeManager = null,
            int ntaThicknessMm = 200,
            Dictionary<string, int> wallOverrides = null,
            WallThicknessCalculator.ISCodeVersion isCode = WallThicknessCalculator.ISCodeVersion.IS2025)
        {
            sapModel = model;
            dxfDoc = doc;
            floorHeight = height;
            totalTypicalFloors = typicalFloors;
            seismicZone = zone;
            gradeSchedule = gradeManager;
            ntaWallThicknessMm = ntaThicknessMm;
            wallThicknessOverrides = wallOverrides ?? new Dictionary<string, int>();
            this.isCode = isCode;

            DiagnoseCoordinateSystem();
            WallThicknessCalculator.LoadAvailableWallSections(sapModel);
        }

        // ====================================================================
        // WALL CLASSIFICATION
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
            return WallCategory.Internal;
        }

        // ====================================================================
        // WALL SECTION RESOLUTION  — now passes isCode
        // ====================================================================
        private string GetWallSection(WallCategory cat, double wallLengthM, string preferredGrade)
        {
            if (cat == WallCategory.NTA)
                return GetNTASection(preferredGrade);

            string overrideKey = cat switch
            {
                WallCategory.Core => "CoreWall",
                WallCategory.PeripheralDead => "PeriphDeadWall",
                WallCategory.PeripheralPortal => "PeriphPortalWall",
                _ => "InternalWall"
            };

            if (wallThicknessOverrides.TryGetValue(overrideKey, out int ovr) && ovr > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  [{cat}] Using UI override: {ovr}mm (key={overrideKey})");
                return EnsureWallSection(ovr, preferredGrade);
            }

            int thicknessMm = WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors,
                cat switch
                {
                    WallCategory.Core => WallThicknessCalculator.WallType.CoreWall,
                    WallCategory.PeripheralDead => WallThicknessCalculator.WallType.PeripheralDeadWall,
                    WallCategory.PeripheralPortal => WallThicknessCalculator.WallType.PeripheralPortalWall,
                    _ => WallThicknessCalculator.WallType.InternalWall
                },
                seismicZone, wallLengthM, false,
                WallThicknessCalculator.ConstructionType.TypeII,
                isCode);

            return EnsureWallSection(thicknessMm, preferredGrade);
        }

        /// <summary>
        /// Returns a wall section name for the given thickness and grade.
        /// If the exact section W{cm}M{grade} does not exist in the template,
        /// it is defined automatically via SectionDefiner.
        /// </summary>
        private string EnsureWallSection(int thicknessMm, string grade)
            => SectionDefiner.EnsureWallSection(sapModel, thicknessMm, grade);

        private string GetNTASection(string preferredGrade)
            => EnsureWallSection(ntaWallThicknessMm, preferredGrade);

        // ====================================================================
        // PUBLIC IMPORT  —  two-pass: create walls first, then assign piers
        // ====================================================================

        // Holds every wall created across ALL stories: area name + midpoint XY
        // Key = area object name, Value = (xMid, yMid, isHorizontal)
        private readonly List<(string area, double xMid, double yMid, bool isHoriz)> _allCreatedWalls
            = new List<(string, double, double, bool)>();

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
            string codeLabel = isCode == WallThicknessCalculator.ISCodeVersion.IS2016
                ? "IS2016" : "IS2025";

            System.Diagnostics.Debug.WriteLine(
                $"\n========== IMPORTING WALLS - Story {story} [{codeLabel}] ==========");
            System.Diagnostics.Debug.WriteLine(
                $"Base: {elevation:F3}m | Top: {elevation + floorHeight:F3}m | " +
                $"Grade: {wallGrade ?? "default"} | NTA: {ntaWallThicknessMm}mm");

            if (wallThicknessOverrides.Any(kv => kv.Value > 0))
            {
                System.Diagnostics.Debug.WriteLine("  Wall thickness overrides active:");
                foreach (var kv in wallThicknessOverrides.Where(kv => kv.Value > 0))
                    System.Diagnostics.Debug.WriteLine($"    {kv.Key} = {kv.Value}mm");
            }

            // ── PASS 1: create all wall geometry, NO pier assignment yet ──
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
                    if (CreateWallFromLine(line, elevation, story, section)) wallsCreated++;
                    else wallsFailed++;
                }

                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    wallsCreated += CreateWallFromPolyline(poly, elevation, story, cat, wallGrade);
                }
            }

            System.Diagnostics.Debug.WriteLine($"\n✓ {wallsCreated}  ❌ {wallsFailed}");
        }

        /// <summary>
        /// Call ONCE after ALL stories have been imported.
        /// Sorts every created wall (horizontal first left→right row by row,
        /// then vertical left→right top→bottom) and assigns P1, P2, P3...
        /// Walls on different stories with the same XY midpoint share a pier name
        /// automatically because they round to the same key.
        /// </summary>
        public void AssignAllPiers()
        {
            if (_allCreatedWalls.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("AssignAllPiers: no walls to process.");
                return;
            }

            const double ROWGAP = 1.0; // walls within 1 m Y = same row

            // Separate horizontal and vertical — use unique XY keys so same
            // position on different stories maps to one pier name.
            var seen = new HashSet<string>();
            var horizPts = new List<(double rx, double ry)>();
            var vertPts = new List<(double rx, double ry)>();

            foreach (var w in _allCreatedWalls)
            {
                string key = PierKey(w.xMid, w.yMid);
                if (!seen.Add(key)) continue; // already recorded this XY position
                if (w.isHoriz) horizPts.Add((w.xMid, w.yMid));
                else vertPts.Add((w.xMid, w.yMid));
            }

            // ── Sort horizontals: row by row top→bottom, left→right within row ──
            horizPts.Sort((a, b) => b.ry != a.ry
                ? b.ry.CompareTo(a.ry) : a.rx.CompareTo(b.rx));

            var hRows = new List<List<(double rx, double ry)>>();
            foreach (var pt in horizPts)
            {
                bool added = false;
                foreach (var row in hRows)
                    if (Math.Abs(row[0].ry - pt.ry) <= ROWGAP) { row.Add(pt); added = true; break; }
                if (!added) hRows.Add(new List<(double, double)> { pt });
            }
            hRows.Sort((a, b) => b[0].ry.CompareTo(a[0].ry));
            foreach (var row in hRows) row.Sort((a, b) => a.rx.CompareTo(b.rx));

            // ── Sort verticals: left→right, tie-break top→bottom ──
            vertPts.Sort((a, b) =>
                Math.Abs(a.rx - b.rx) > 0.25
                    ? a.rx.CompareTo(b.rx)
                    : b.ry.CompareTo(a.ry));

            // ── Build pier name map ──
            var pierMap = new Dictionary<string, string>();
            int counter = 1;
            foreach (var row in hRows)
                foreach (var pt in row)
                {
                    string k = PierKey(pt.rx, pt.ry);
                    if (!pierMap.ContainsKey(k)) pierMap[k] = $"P{counter++}";
                }
            foreach (var pt in vertPts)
            {
                string k = PierKey(pt.rx, pt.ry);
                if (!pierMap.ContainsKey(k)) pierMap[k] = $"P{counter++}";
            }

            System.Diagnostics.Debug.WriteLine(
                $"AssignAllPiers: {horizPts.Count} horizontal + {vertPts.Count} vertical " +
                $"= {pierMap.Count} unique piers → assigning to {_allCreatedWalls.Count} walls...");

            // ── PASS 2: assign pier labels to every wall area object ──
            foreach (var w in _allCreatedWalls)
            {
                string k = PierKey(w.xMid, w.yMid);
                if (!pierMap.TryGetValue(k, out string pierName))
                {
                    pierName = $"P{counter++}";
                    pierMap[k] = pierName;
                    System.Diagnostics.Debug.WriteLine($"  ⚠ Fallback pier {pierName} for key {k}");
                }

                sapModel.PierLabel.SetPier(pierName);
                int ret = sapModel.AreaObj.SetPier(w.area, pierName);
                System.Diagnostics.Debug.WriteLine(ret == 0
                    ? $"  ✓ {w.area} → {pierName}"
                    : $"  ⚠ SetPier failed (ret={ret}) area={w.area} pier={pierName}");
            }

            System.Diagnostics.Debug.WriteLine("AssignAllPiers: complete.");
        }

        // ====================================================================
        // GEOMETRY CREATION
        // ====================================================================
        // ── PierKey helper — shared by AssignAllPiers ──
        private static string PierKey(double x, double y)
        {
            const double GRID = 0.25;
            double rx = Math.Round(x / GRID) * GRID;
            double ry = Math.Round(y / GRID) * GRID;
            return $"{rx:F2}_{ry:F2}";
        }

        /// <summary>Reset wall list and pier map — call before a fresh import run.</summary>
        /// <summary>
        /// Updates DXF source, floor height, and wall overrides before each ImportWalls call.
        /// Allows a single shared instance to be reused across different floor types/stories.
        /// </summary>
        public void UpdateDxfAndHeight(DxfDocument doc, double height,
            int ntaThicknessMm, Dictionary<string, int> wallOverrides)
        {
            dxfDoc = doc;
            floorHeight = height;
            ntaWallThicknessMm = ntaThicknessMm;
            // Merge overrides (clear then re-add so removals are respected)
            wallThicknessOverrides.Clear();
            if (wallOverrides != null)
                foreach (var kv in wallOverrides)
                    wallThicknessOverrides[kv.Key] = kv.Value;
        }

        public void ResetPiers()
        {
            _allCreatedWalls.Clear();
        }

        private bool CreateWallFromLine(netDxf.Entities.Line line,
            double elevation, int story, string section)
        {
            try
            {
                if (!wallTypeCount.ContainsKey(section)) wallTypeCount[section] = 0;
                wallTypeCount[section]++;

                double x1 = MX(line.StartPoint.X), y1 = MY(line.StartPoint.Y);
                double x2 = MX(line.EndPoint.X), y2 = MY(line.EndPoint.Y);

                string[] pts = new string[4];
                sapModel.PointObj.AddCartesian(x1, y1, elevation, ref pts[0], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation, ref pts[1], "Global");
                sapModel.PointObj.AddCartesian(x2, y2, elevation + floorHeight, ref pts[2], "Global");
                sapModel.PointObj.AddCartesian(x1, y1, elevation + floorHeight, ref pts[3], "Global");

                string area = "";
                int ret = sapModel.AreaObj.AddByPoint(4, ref pts, ref area, section);
                if (ret == 0 && !string.IsNullOrEmpty(area))
                {
                    sapModel.AreaObj.SetGroupAssign(area, GetStoryName(story));

                    // Record for pier assignment in Pass 2
                    double xMid = (x1 + x2) / 2.0;
                    double yMid = (y1 + y2) / 2.0;
                    bool isHoriz = Math.Abs(x2 - x1) >= Math.Abs(y2 - y1);
                    _allCreatedWalls.Add((area, xMid, yMid, isHoriz));

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

                    // Record for pier assignment in Pass 2
                    double xMid = (x1 + x2) / 2.0;
                    double yMid = (y1 + y2) / 2.0;
                    bool isHoriz = Math.Abs(x2 - x1) >= Math.Abs(y2 - y1);
                    _allCreatedWalls.Add((area, xMid, yMid, isHoriz));

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CreateWallSegment: {ex.Message}");
                return false;
            }
        }
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
                    names != null && story >= 0 && story < n)
                    return names[story];   // FIX: direct index — ETABS returns names bottom-to-top
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }

        private void DiagnoseCoordinateSystem()
        {
            System.Diagnostics.Debug.WriteLine(
                $"WallImporter v3.3: X/Y→m via ×0.001, Z already in m | ISCode={isCode}");
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
