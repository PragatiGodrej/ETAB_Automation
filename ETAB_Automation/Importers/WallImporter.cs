
// ============================================================================
// FILE: Importers/WallImporterEnhanced.cs
// VERSION: 3.1 — Full fix set
// FIXES:
//   [FIX-1] GetStoryName: ETABS GetNameList returns stories top-down; index
//           was used as-is (bottom-up) causing wrong story assignment and
//           index-out-of-bounds on model save.  Now flipped: names[n-1-story].
//   [FIX-2] WallThicknessOverrides parameter added; per-floor GPL overrides
//           from the UI are now applied before the GPL table fallback.
//   [FIX-3] NtaWallThickness parameter properly plumbed through (was already
//           present but callers never passed floorConfig.NtaWallThickness).
//   [FIX-4] FindClosestWallSection helper added to support FIX-2.
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
    public class WallImporterEnhanced
    {
        private cSapModel sapModel;
        private DxfDocument dxfDoc;
        private double floorHeight;
        private int totalTypicalFloors;
        private string seismicZone;
        private GradeScheduleManager gradeSchedule;

        // [FIX-3] user-defined NTA thickness (mm)
        private int ntaWallThicknessMm;

        // [FIX-2] per-floor wall thickness overrides from UI (0 = use GPL table)
        private readonly Dictionary<string, int> wallThicknessOverrides;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        private int wallsCreated = 0;
        private int wallsFailed = 0;
        private Dictionary<string, int> wallTypeCount = new Dictionary<string, int>();

        // ====================================================================
        // CONSTRUCTOR
        // [FIX-2][FIX-3] wallOverrides and ntaThicknessMm now wired all the
        //                 way from FloorTypeConfig via CADImporterEnhanced.
        // ====================================================================

        public WallImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            double height,
            int typicalFloors,
            string zone,
            GradeScheduleManager gradeManager = null,
            int ntaThicknessMm = 200,
            Dictionary<string, int> wallOverrides = null)
        {
            sapModel = model;
            dxfDoc = doc;
            floorHeight = height;
            totalTypicalFloors = typicalFloors;
            seismicZone = zone;
            gradeSchedule = gradeManager;
            ntaWallThicknessMm = ntaThicknessMm;
            wallThicknessOverrides = wallOverrides ?? new Dictionary<string, int>();

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

            return WallCategory.Internal; // safe default
        }

        // ====================================================================
        // WALL SECTION RESOLUTION
        // [FIX-2] Check per-floor override dict BEFORE calling GPL table.
        // ====================================================================

        private string GetWallSection(WallCategory cat, double wallLengthM, string preferredGrade)
        {
            if (cat == WallCategory.NTA)
                return GetNTASection(preferredGrade);

            // [FIX-2] Map category → override key matching FloorTypeConfig.WallThicknessOverrides
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
                return FindClosestWallSection(ovr, preferredGrade);
            }

            // GPL table fallback (original behaviour)
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

        // ====================================================================
        // [FIX-4] FindClosestWallSection — shared by NTA and override paths
        // ====================================================================

        private string FindClosestWallSection(int targetMm, string preferredGrade)
        {
            try
            {
                int num = 0; string[] names = null;
                sapModel.PropArea.GetNameList(ref num, ref names);

                string gradeNum = preferredGrade?.Replace("M", "").Replace("m", "").Trim();
                var pattern = new Regex(@"^W(\d+)M(\d+)", RegexOptions.IgnoreCase);

                string best = null;
                int minDiff = int.MaxValue;

                // Grade-matched pass
                if (!string.IsNullOrEmpty(gradeNum) && names != null)
                {
                    foreach (string name in names)
                    {
                        var m = pattern.Match(name);
                        if (!m.Success || m.Groups[2].Value != gradeNum) continue;
                        int diff = Math.Abs(int.Parse(m.Groups[1].Value) - targetMm);
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
                        int diff = Math.Abs(int.Parse(m.Groups[1].Value) - targetMm);
                        if (diff < minDiff) { minDiff = diff; best = name; }
                    }
                }

                return best ?? $"W{targetMm}M30";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ FindClosestWallSection: {ex.Message}");
                return $"W{targetMm}M30";
            }
        }

        // ====================================================================
        // NTA SECTION (user-defined thickness, no GPL table)
        // ====================================================================

        private string GetNTASection(string preferredGrade)
            => FindClosestWallSection(ntaWallThicknessMm, preferredGrade);

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

            if (wallThicknessOverrides.Any(kv => kv.Value > 0))
            {
                System.Diagnostics.Debug.WriteLine("  Wall thickness overrides active:");
                foreach (var kv in wallThicknessOverrides.Where(kv => kv.Value > 0))
                    System.Diagnostics.Debug.WriteLine($"    {kv.Key} = {kv.Value}mm");
            }

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

        /// <summary>
        /// [FIX-1] ETABS GetNameList returns stories top-down (index 0 = highest
        /// story, Terrace).  Our story index is bottom-up (0 = lowest floor).
        /// Flip: names[n - 1 - story] maps correctly bottom→top.
        /// </summary>
        private string GetStoryName(int story)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
                    names != null && story >= 0 && story < n)
                    return names[n - 1 - story];   // [FIX-1] flip the index
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
