
// ============================================================================
// FILE: Importers/BeamImporterEnhanced.cs
// VERSION: 3.2 — GetStoryName index flip fix
// FIXES (on top of v3.1):
//   [FIX-1] GetStoryName: ETABS GetNameList returns stories top-down; index
//           was used as-is (bottom-up) causing wrong story assignment and
//           index-out-of-bounds crash on model save. Flipped to names[n-1-story].
//
// All v3.1 fixes retained:
//   [v3.1-FIX-1] beamWidthOverrides wired through constructor
//   [v3.1-FIX-2] GravityBeamWidth() respects GravityWidth override
//   [v3.1-FIX-3] Main beam width checks per-type override before GPL calc
//   [v3.1-FIX-4] B-No Load layer uses "NoLoadGravity" key (not InternalGravity)
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
        private readonly Dictionary<string, int> beamWidthOverrides;
        private readonly GradeScheduleManager gradeSchedule;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        // ====================================================================
        // SECTION CACHE
        // ====================================================================

        private static Dictionary<string, GravityBeamInfo> gravityBeamSections =
            new Dictionary<string, GravityBeamInfo>();
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
        // CONSTRUCTOR
        // ====================================================================

        public BeamImporterEnhanced(
            cSapModel model,
            DxfDocument doc,
            string zone,
            int typicalFloors,
            Dictionary<string, int> depths,
            GradeScheduleManager gradeManager = null,
            Dictionary<string, int> widthOverrides = null)
        {
            sapModel = model;
            dxfDoc = doc;
            seismicZone = zone;
            totalTypicalFloors = typicalFloors;
            beamDepths = depths ?? new Dictionary<string, int>();
            gradeSchedule = gradeManager;
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

                var mainPat = new Regex(@"^MB(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
                    RegexOptions.IgnoreCase);
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
                        continue;
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
        // WIDTH RESOLUTION HELPERS
        // ====================================================================

        private int GravityBeamWidth()
        {
            int ovr = beamWidthOverrides.GetValueOrDefault("GravityWidth", 0);
            if (ovr > 0) return ovr;
            return (seismicZone.Contains("II") || seismicZone.Contains("III")) ? 200 : 240;
        }

        private int MainBeamWidth(WallThicknessCalculator.WallType wt, string widthOverrideKey)
        {
            int ovr = beamWidthOverrides.GetValueOrDefault(widthOverrideKey, 0);
            if (ovr > 0) return ovr;
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
                    "⚠ No MB sections — falling back to gravity sections for main beam");
                return BestGravitySection(reqWidth, reqDepth, grade);
            }

            return best ?? throw new InvalidOperationException(
                $"No main beam section (MB__) for {reqWidth}×{reqDepth}mm.");
        }

        private static string NormalizeGrade(string grade)
            => grade?.Replace("M", "").Replace("m", "").Trim();

        // ====================================================================
        // LAYER → SECTION MAPPING
        // ====================================================================

        private enum BeamCategory { Gravity, Main }

        private (string section, BeamCategory cat) DetermineBeamSection(
            string layerName, string grade)
        {
            string u = layerName.ToUpperInvariant();
            int gw = GravityBeamWidth();

            // ── MAIN BEAMS ────────────────────────────────────────────────
            if (u.Contains("CORE") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.CoreWall, "CoreMainWidth");
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("CoreMain", 600), grade), BeamCategory.Main);
            }

            if (u.Contains("PERIPHERAL") && u.Contains("DEAD") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.PeripheralDeadWall,
                                      "PeripheralDeadMainWidth");
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("PeripheralDeadMain", 600), grade), BeamCategory.Main);
            }

            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.PeripheralPortalWall,
                                      "PeripheralPortalMainWidth");
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("PeripheralPortalMain", 650), grade), BeamCategory.Main);
            }

            if (u.Contains("INTERNAL") && u.Contains("MAIN"))
            {
                int w = MainBeamWidth(WallThicknessCalculator.WallType.InternalWall, "InternalMainWidth");
                return (BestMainSection(w,
                    beamDepths.GetValueOrDefault("InternalMain", 550), grade), BeamCategory.Main);
            }

            // ── GRAVITY BEAMS ─────────────────────────────────────────────
            if (u.Contains("CANTILEVER") && u.Contains("GRAVITY"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("CantileverGravity", 500), grade), BeamCategory.Gravity);

            if (u.Contains("NO LOAD") || u.Contains("NOLOAD") || u.Contains("NO-LOAD"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("NoLoadGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade), BeamCategory.Gravity);

            if (u.Contains("EDECK") || u.Contains("E-DECK") || u.Contains("E DECK"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("EdeckGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade), BeamCategory.Gravity);

            if (u.Contains("PODIUM"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("PodiumGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade), BeamCategory.Gravity);

            if (u.Contains("GROUND"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("GroundGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade), BeamCategory.Gravity);

            if (u.Contains("BASEMENT"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("BasementGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade), BeamCategory.Gravity);

            if (u.Contains("INTERNAL") && u.Contains("GRAVITY"))
                return (BestGravitySection(gw,
                    beamDepths.GetValueOrDefault("InternalGravity", 450), grade), BeamCategory.Gravity);

            System.Diagnostics.Debug.WriteLine(
                $"⚠ Unknown beam layer '{layerName}' → internal gravity fallback");
            return (BestGravitySection(gw,
                beamDepths.GetValueOrDefault("InternalGravity", 450), grade), BeamCategory.Gravity);
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

                System.Diagnostics.Debug.WriteLine($"\nLayer: {layerName} [{cat}] → {section}");

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

        // ====================================================================
        // [FIX-1] GetStoryName — flip ETABS top-down index to bottom-up
        // ====================================================================

        /// <summary>
        /// ETABS Story.GetNameList returns stories ordered TOP-DOWN
        /// (index 0 = topmost story, e.g. Terrace).
        /// Our <paramref name="story"/> parameter is BOTTOM-UP
        /// (0 = lowest floor, e.g. Basement1).
        /// Flip formula: names[n - 1 - story]
        /// </summary>
        private string GetStoryName(int story)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
                    names != null && story >= 0 && story < n)
                    return names[n - 1 - story];   // [FIX-1] correct flip
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }

    // ====================================================================
    // EXTENSION HELPER
    // ====================================================================

    internal static class DictExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue = default)
            => dict.TryGetValue(key, out TValue val) ? val : defaultValue;
    }
}
