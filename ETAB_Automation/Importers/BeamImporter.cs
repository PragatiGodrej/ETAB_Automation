






//// ============================================================================
//// FILE: Importers/BeamImporterEnhanced.cs
//// ============================================================================
//// BEAMS  → Assign > Frame Loads > Distributed  → Load PATTERN
//// API    : sapModel.FrameObj.SetLoadDistributed(name, patternName, ...)
//// Cache  : sapModel.LoadPatterns.GetNameList()
////
//// Flow per beam layer:
////   1. DetermineBeamSection()  → picks section name + loadSetKey from layer name
////   2. ResolveBeamLoadSetName()→ reads UI value (beamWallLoadSets[key])
////   3. ResolveLoadPattern()    → fuzzy-matches that name against ETABS patterns
////   4. AssignBeamWallLoads()   → calls SetLoadDistributed if pattern found
//// ============================================================================

//using ETAB_Automation.Core;
//using ETAB_Automation.Models;
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
//        private readonly Dictionary<string, int> beamWidthOverrides;
//        private readonly Dictionary<string, string> beamWallLoadSets; // from UI
//        private readonly GradeScheduleManager gradeSchedule;

//        private const double X_TO_M = 0.001;
//        private const double Y_TO_M = 0.001;
//        private double MX(double x) => x * X_TO_M;
//        private double MY(double y) => y * Y_TO_M;

//        // ====================================================================
//        // SECTION CACHE  (static — populated once per process lifetime)
//        // ====================================================================

//        private static readonly Dictionary<string, GravityBeamInfo> gravityBeamSections
//            = new Dictionary<string, GravityBeamInfo>();

//        private static readonly Dictionary<string, MainBeamInfo> mainBeamSections
//            = new Dictionary<string, MainBeamInfo>();

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
//        // LOAD PATTERN CACHE  (instance — re-read for every new model/session)
//        // Key   = UPPER-CASE pattern name
//        // Value = exact name stored in ETABS  (case-sensitive)
//        // ====================================================================

//        private Dictionary<string, string> etabsLoadPatterns;

//        // Key   = same as beamWallLoadSets (e.g. "InternalGravity")
//        // Value = magnitude in N/m entered by user in UI (kN/m × 1000)
//        private Dictionary<string, double> uiLoadMagnitudes;

//        // ====================================================================
//        // CONSTRUCTOR
//        // ====================================================================

//        public BeamImporterEnhanced(
//            cSapModel model,
//            DxfDocument doc,
//            string zone,
//            int typicalFloors,
//            Dictionary<string, int> depths,
//            GradeScheduleManager gradeManager = null,
//            Dictionary<string, int> widthOverrides = null,
//            Dictionary<string, string> wallLoadSets = null,
//            Dictionary<string, double> wallLoadMagnitudes = null)
//        {
//            sapModel = model;
//            dxfDoc = doc;
//            seismicZone = zone;
//            totalTypicalFloors = typicalFloors;
//            beamDepths = depths ?? new Dictionary<string, int>();
//            gradeSchedule = gradeManager;
//            beamWidthOverrides = widthOverrides ?? new Dictionary<string, int>();
//            beamWallLoadSets = wallLoadSets ?? new Dictionary<string, string>();

//            // Convert kN/m → N/m for internal use
//            uiLoadMagnitudes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
//            if (wallLoadMagnitudes != null)
//                foreach (var kv in wallLoadMagnitudes)
//                    if (kv.Value > 0)
//                        uiLoadMagnitudes[kv.Key] = kv.Value * 1000.0;

//            LoadBeamSections();
//            LoadEtabsLoadPatterns();
//        }

//        // ====================================================================
//        // STEP 1 — Discover all ETABS Load Patterns
//        // Logged in full so any mismatch is immediately visible.
//        // ====================================================================

//        private void LoadEtabsLoadPatterns()
//        {
//            etabsLoadPatterns = new Dictionary<string, string>(
//                StringComparer.OrdinalIgnoreCase);

//            try
//            {
//                int n = 0;
//                string[] names = null;
//                int ret = sapModel.LoadPatterns.GetNameList(ref n, ref names);

//                if (ret != 0 || names == null || n == 0)
//                {
//                    System.Diagnostics.Debug.WriteLine(
//                        "⚠ BEAM: Could not read Load Patterns from ETABS model.");
//                    return;
//                }

//                System.Diagnostics.Debug.WriteLine(
//                    $"\n===== ETABS LOAD PATTERNS (for BEAM assignment) — {n} total =====");
//                foreach (string name in names)
//                {
//                    etabsLoadPatterns[name.ToUpperInvariant()] = name;
//                    System.Diagnostics.Debug.WriteLine($"  LP: '{name}'");
//                }
//                System.Diagnostics.Debug.WriteLine(
//                    "====================================================================\n");
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    $"⚠ BEAM LoadEtabsLoadPatterns: {ex.Message}");
//            }
//        }


//        // ====================================================================
//        // STEP 2 — Fuzzy-resolve user name → exact ETABS Load Pattern name
//        // 1. Exact match (case-insensitive)
//        // 2. Substring match (user in pattern OR pattern in user)
//        // Returns null if nothing matches → caller skips assignment.
//        // ====================================================================

//        private string ResolveLoadPattern(string userPatternName)
//        {
//            if (string.IsNullOrWhiteSpace(userPatternName)) return null;
//            if (etabsLoadPatterns == null || etabsLoadPatterns.Count == 0) return null;

//            string u = userPatternName.ToUpperInvariant().Trim();

//            // 1. Exact
//            if (etabsLoadPatterns.TryGetValue(u, out string exact)) return exact;

//            // 2. Partial
//            foreach (var kv in etabsLoadPatterns)
//                if (kv.Key.Contains(u) || u.Contains(kv.Key))
//                    return kv.Value;

//            System.Diagnostics.Debug.WriteLine(
//                $"  ⚠ BEAM: Load Pattern '{userPatternName}' not found in ETABS. " +
//                "Check Define > Load Patterns — names are listed above.");
//            return null;
//        }

//        // ====================================================================
//        // SECTION LOADING
//        // Pattern: B20X75M35  (gravity) | MB25X75M35 (main)
//        // Groups : width-in-cm  × depth-in-cm  grade-number
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

//                var mainPat = new Regex(
//                    @"^MB(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
//                    RegexOptions.IgnoreCase);
//                var gravPat = new Regex(
//                    @"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
//                    RegexOptions.IgnoreCase);

//                foreach (string name in names)
//                {
//                    var mg = mainPat.Match(name);
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
//                    var gg = gravPat.Match(name);
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
//                    $"✓ Beam sections loaded: {gravityBeamSections.Count} gravity (B__), " +
//                    $"{mainBeamSections.Count} main (MB__)");

//                if (gravityBeamSections.Count == 0 && mainBeamSections.Count == 0)
//                    System.Diagnostics.Debug.WriteLine(
//                        "⚠ No beam sections found in ETABS template — " +
//                        "sections will be auto-defined as needed (B__/MB__ format).");
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"❌ LoadBeamSections: {ex.Message}");
//                throw;
//            }
//        }

//        // ====================================================================
//        // WIDTH HELPERS
//        // ====================================================================

//        private int ResolveGravityWidth(string variantKey)
//        {
//            // 1. Per-variant override from UI
//            if (beamWidthOverrides.TryGetValue(variantKey, out int v) && v > 0) return v;
//            // 2. Shared gravity override (legacy)
//            if (beamWidthOverrides.TryGetValue("GravityWidth", out int s) && s > 0) return s;
//            // 3. Zone rule
//            return (seismicZone.Contains("II") || seismicZone.Contains("III")) ? 200 : 240;
//        }

//        private int ResolveMainWidth(WallThicknessCalculator.WallType wt, string key)
//        {
//            if (beamWidthOverrides.TryGetValue(key, out int v) && v > 0) return v;
//            return WallThicknessCalculator.GetRecommendedThickness(
//                totalTypicalFloors, wt, seismicZone, 2.0, false);
//        }

//        // ====================================================================
//        // CLOSEST-SECTION FINDERS
//        // ====================================================================

//        // ====================================================================
//        // SECTION RESOLUTION — define-if-missing via SectionDefiner
//        // ====================================================================

//        /// <summary>
//        /// Returns a gravity beam section name B{wCm}X{dCm}M{grade}.
//        /// If the section does not exist in the template it is auto-defined.
//        /// Falls back to closest existing section if auto-define fails.
//        /// </summary>
//        private string BestGravitySection(int reqWidth, int reqDepth, string grade)
//        {
//            // 1. Try to define / verify the exact section we need
//            string exact = SectionDefiner.EnsureGravityBeamSection(sapModel, reqWidth, reqDepth, grade);
//            if (!string.IsNullOrEmpty(exact))
//                return exact;

//            // 2. Fallback: closest existing section (old behaviour)
//            string gn = NormalizeGrade(grade);
//            string best = null; int minDiff = int.MaxValue;
//            if (!string.IsNullOrEmpty(gn))
//            {
//                foreach (var kvp in gravityBeamSections)
//                {
//                    if (kvp.Value.Grade != gn) continue;
//                    int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                          + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                    if (d < minDiff) { minDiff = d; best = kvp.Key; }
//                }
//                if (best != null) return best;
//            }
//            minDiff = int.MaxValue;
//            foreach (var kvp in gravityBeamSections)
//            {
//                int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                      + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                if (d < minDiff) { minDiff = d; best = kvp.Key; }
//            }
//            return best ?? SectionDefiner.EnsureGravityBeamSection(sapModel, reqWidth, reqDepth, "M30");
//        }

//        /// <summary>
//        /// Returns a main beam section name MB{wCm}X{dCm}M{grade}.
//        /// If the section does not exist in the template it is auto-defined.
//        /// Falls back to closest existing section if auto-define fails.
//        /// </summary>
//        private string BestMainSection(int reqWidth, int reqDepth, string grade)
//        {
//            // 1. Try to define / verify the exact section we need
//            string exact = SectionDefiner.EnsureMainBeamSection(sapModel, reqWidth, reqDepth, grade);
//            if (!string.IsNullOrEmpty(exact))
//                return exact;

//            // 2. Fallback: closest existing section
//            string gn = NormalizeGrade(grade);
//            string best = null; int minDiff = int.MaxValue;
//            if (!string.IsNullOrEmpty(gn))
//            {
//                foreach (var kvp in mainBeamSections)
//                {
//                    if (kvp.Value.Grade != gn) continue;
//                    int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                          + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                    if (d < minDiff) { minDiff = d; best = kvp.Key; }
//                }
//                if (best != null) return best;
//            }
//            minDiff = int.MaxValue;
//            foreach (var kvp in mainBeamSections)
//            {
//                int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
//                      + Math.Abs(kvp.Value.WidthMm - reqWidth);
//                if (d < minDiff) { minDiff = d; best = kvp.Key; }
//            }
//            if (best == null && gravityBeamSections.Count > 0)
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    "⚠ No MB__ sections — falling back to gravity (B__) for main beam");
//                return BestGravitySection(reqWidth, reqDepth, grade);
//            }
//            return best ?? SectionDefiner.EnsureMainBeamSection(sapModel, reqWidth, reqDepth, "M30");
//        }

//        private static string NormalizeGrade(string grade)
//            => grade?.Replace("M", "").Replace("m", "").Trim();

//        // ====================================================================
//        // LAYER → SECTION + LOAD-SET KEY
//        // Returns (sectionName, loadSetKey, category)
//        // loadSetKey is used to look up the user-typed load pattern name.
//        // ====================================================================

//        private enum BeamCategory { Gravity, Main }

//        private (string section, string loadSetKey, BeamCategory cat)
//            DetermineBeamSection(string layerName, string grade)
//        {
//            string u = layerName.ToUpperInvariant();

//            // ── MAIN BEAMS ─────────────────────────────────────────────────
//            if (u.Contains("CORE") && u.Contains("MAIN"))
//            {
//                int w = ResolveMainWidth(WallThicknessCalculator.WallType.CoreWall, "CoreMainWidth");
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("CoreMain", 600), grade),
//                        "CoreMain", BeamCategory.Main);
//            }
//            if (u.Contains("PERIPHERAL") && u.Contains("DEAD") && u.Contains("MAIN"))
//            {
//                int w = ResolveMainWidth(WallThicknessCalculator.WallType.PeripheralDeadWall, "PeripheralDeadMainWidth");
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralDeadMain", 600), grade),
//                        "PeripheralDeadMain", BeamCategory.Main);
//            }
//            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL") && u.Contains("MAIN"))
//            {
//                int w = ResolveMainWidth(WallThicknessCalculator.WallType.PeripheralPortalWall, "PeripheralPortalMainWidth");
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralPortalMain", 650), grade),
//                        "PeripheralPortalMain", BeamCategory.Main);
//            }
//            if (u.Contains("INTERNAL") && u.Contains("MAIN"))
//            {
//                int w = ResolveMainWidth(WallThicknessCalculator.WallType.InternalWall, "InternalMainWidth");
//                return (BestMainSection(w, beamDepths.GetValueOrDefault("InternalMain", 550), grade),
//                        "InternalMain", BeamCategory.Main);
//            }

//            // ── GRAVITY BEAMS ──────────────────────────────────────────────

//            // NoLoad must be checked BEFORE generic gravity matches
//            if (u.Contains("NO LOAD") || u.Contains("NOLOAD") || u.Contains("NO-LOAD"))
//            {
//                int w = ResolveGravityWidth("NoLoadGravityWidth");
//                return (BestGravitySection(w,
//                        beamDepths.GetValueOrDefault("NoLoadGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                        "NoLoadGravity",     // → empty string in load set table
//                        BeamCategory.Gravity);
//            }
//            if (u.Contains("CANTILEVER") && u.Contains("GRAVITY"))
//            {
//                int w = ResolveGravityWidth("CantileverGravityWidth");
//                return (BestGravitySection(w, beamDepths.GetValueOrDefault("CantileverGravity", 500), grade),
//                        "CantileverGravity", BeamCategory.Gravity);
//            }
//            if (u.Contains("EDECK") || u.Contains("E-DECK") || u.Contains("E DECK"))
//            {
//                int w = ResolveGravityWidth("EdeckGravityWidth");
//                return (BestGravitySection(w,
//                        beamDepths.GetValueOrDefault("EdeckGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                        "EdeckGravity", BeamCategory.Gravity);
//            }
//            if (u.Contains("PODIUM"))
//            {
//                int w = ResolveGravityWidth("PodiumGravityWidth");
//                return (BestGravitySection(w,
//                        beamDepths.GetValueOrDefault("PodiumGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                        "PodiumGravity", BeamCategory.Gravity);
//            }
//            if (u.Contains("GROUND"))
//            {
//                int w = ResolveGravityWidth("GroundGravityWidth");
//                return (BestGravitySection(w,
//                        beamDepths.GetValueOrDefault("GroundGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                        "GroundGravity", BeamCategory.Gravity);
//            }
//            if (u.Contains("BASEMENT"))
//            {
//                int w = ResolveGravityWidth("BasementGravityWidth");
//                return (BestGravitySection(w,
//                        beamDepths.GetValueOrDefault("BasementGravity",
//                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
//                        "BasementGravity", BeamCategory.Gravity);
//            }
//            if (u.Contains("INTERNAL") && u.Contains("GRAVITY"))
//            {
//                int w = ResolveGravityWidth("InternalGravityWidth");
//                return (BestGravitySection(w, beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
//                        "InternalGravity", BeamCategory.Gravity);
//            }

//            // Unknown → InternalGravity fallback
//            System.Diagnostics.Debug.WriteLine(
//                $"  ⚠ Unknown beam layer '{layerName}' → InternalGravity fallback");
//            {
//                int w = ResolveGravityWidth("InternalGravityWidth");
//                return (BestGravitySection(w, beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
//                        "InternalGravity", BeamCategory.Gravity);
//            }
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
//                $"\n========== IMPORTING BEAMS — Story {story} | Elev {elevation:F3}m | Grade {beamGrade ?? "default"} ==========");

//            int total = 0;

//            foreach (string layerName in beamLayers)
//            {
//                var (section, loadSetKey, cat) = DetermineBeamSection(layerName, beamGrade);

//                var createdNames = new List<string>();
//                int cnt = 0;

//                System.Diagnostics.Debug.WriteLine(
//                    $"\n  Layer: '{layerName}' [{cat}] → section='{section}' loadKey='{loadSetKey}'");

//                // ── Geometry creation ──────────────────────────────────────
//                foreach (var line in dxfDoc.Entities.Lines
//                    .Where(l => l.Layer.Name == layerName))
//                {
//                    string nm = CreateBeamFromLine(line, elevation, section, story);
//                    if (!string.IsNullOrEmpty(nm)) { createdNames.Add(nm); cnt++; }
//                }

//                foreach (var poly in dxfDoc.Entities.Polylines2D
//                    .Where(p => p.Layer.Name == layerName))
//                {
//                    var nms = CreateBeamFromPolyline(poly, elevation, section, story);
//                    createdNames.AddRange(nms);
//                    cnt += nms.Count;
//                }

//                System.Diagnostics.Debug.WriteLine($"  Created: {cnt} beam(s)");

//                // ── Load assignment ────────────────────────────────────────
//                if (createdNames.Count > 0)
//                {
//                    // Get the user-typed Load Pattern name for this beam type
//                    string userPatternName = GetBeamLoadPatternName(loadSetKey);
//                    AssignBeamWallLoads(createdNames, userPatternName, loadSetKey);
//                }

//                total += cnt;
//            }

//            System.Diagnostics.Debug.WriteLine(
//                $"\n  Total beams story {story}: {total}\n");
//        }

//        // ====================================================================
//        // GET BEAM LOAD PATTERN NAME FROM UI
//        // Priority: (1) UI beamWallLoadSets dict  (2) FloorTypeConfig defaults
//        // NoLoadGravity always returns empty → no load assigned.
//        // ====================================================================

//        private string GetBeamLoadPatternName(string loadSetKey)
//        {
//            // NoLoadGravity: explicitly no wall load
//            if (loadSetKey == "NoLoadGravity") return string.Empty;

//            // UI value (from constructor param, read from UI TextBox)
//            if (beamWallLoadSets.TryGetValue(loadSetKey, out string uiVal)
//                && !string.IsNullOrWhiteSpace(uiVal))
//                return uiVal.Trim();

//            // Static default fallback
//            if (FloorTypeConfig.DefaultBeamWallLoadSets.TryGetValue(loadSetKey, out string def)
//                && !string.IsNullOrWhiteSpace(def))
//                return def.Trim();

//            return string.Empty;
//        }

//        // ====================================================================
//        // ASSIGN BEAM WALL LOADS via SetLoadDistributed (Load Pattern)
//        // ====================================================================

//        private void AssignBeamWallLoads(List<string> frameNames,
//            string userPatternName, string loadSetKey)
//        {
//            // Empty → intentional (NoLoad or user left it blank)
//            if (string.IsNullOrWhiteSpace(userPatternName))
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    $"  ⊘ No wall load for '{loadSetKey}' — skipping (intentional).");
//                return;
//            }

//            // Resolve → exact ETABS Load Pattern name
//            string patternName = ResolveLoadPattern(userPatternName);
//            if (patternName == null)
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    $"  ⚠ Load Pattern '{userPatternName}' (key='{loadSetKey}') not found in ETABS. " +
//                    "Skipping. Fix in Define > Load Patterns and re-run.");
//                return;
//            }

//            // ── Resolve magnitude from UI (Wall Load Patterns tab, kN/m → N/m in constructor) ──
//            if (!uiLoadMagnitudes.TryGetValue(loadSetKey, out double loadMagN) || loadMagN <= 0)
//            {
//                System.Diagnostics.Debug.WriteLine(
//                    $"  ⚠ BEAM: Magnitude for '{loadSetKey}' is 0 — enter a kN/m value in the " +
//                    "Wall Load Patterns tab. Skipping.");
//                return;
//            }

//            System.Diagnostics.Debug.WriteLine(
//                $"  → Assigning Load Pattern '{patternName}' " +
//                $"@ {loadMagN / 1000.0:F2} kN/m (from UI) to {frameNames.Count} beam(s)");

//            int ok = 0, fail = 0;
//            foreach (string name in frameNames)
//            {
//                try
//                {
//                    int ret = sapModel.FrameObj.SetLoadDistributed(
//                        name,
//                        patternName,  // exact ETABS Load Pattern
//                        1,            // MyType : 1 = Force per unit length
//                        10,           // Dir    : 10 = Gravity (downward) for ETABS 2026
//                        0.0,          // Dist1  : relative start (0.0 = End-I)
//                        1.0,          // Dist2  : relative end   (1.0 = End-J)
//                        loadMagN,     // Val1   : magnitude read from ETABS (N/m)
//                        loadMagN,     // Val2   : magnitude read from ETABS (N/m)
//                        "Global",
//                        true,         // RelDist : true = relative distances
//                        true);        // Replace : true = replace existing load

//                    if (ret == 0) ok++;
//                    else
//                    {
//                        fail++;
//                        System.Diagnostics.Debug.WriteLine(
//                            $"    ⚠ SetLoadDistributed ret={ret} for beam '{name}'");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    fail++;
//                    System.Diagnostics.Debug.WriteLine(
//                        $"    ⚠ SetLoadDistributed exception for '{name}': {ex.Message}");
//                }
//            }
//            System.Diagnostics.Debug.WriteLine(
//                $"  Load Pattern '{patternName}': ✓{ok} assigned  ✗{fail} failed");
//        }

//        // ====================================================================
//        // GEOMETRY CREATION
//        // ====================================================================

//        private string CreateBeamFromLine(netDxf.Entities.Line line,
//            double elevation, string section, int story)
//        {
//            string name = "";
//            int ret = sapModel.FrameObj.AddByCoord(
//                MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation,
//                MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation,
//                ref name, section, GetStoryName(story));
//            return (ret == 0 && !string.IsNullOrEmpty(name)) ? name : null;
//        }

//        private List<string> CreateBeamFromPolyline(Polyline2D poly,
//            double elevation, string section, int story)
//        {
//            string storyName = GetStoryName(story);
//            var verts = poly.Vertexes;
//            var names = new List<string>();

//            for (int i = 0; i < verts.Count - 1; i++)
//            {
//                string name = "";
//                int ret = sapModel.FrameObj.AddByCoord(
//                    MX(verts[i].Position.X), MY(verts[i].Position.Y), elevation,
//                    MX(verts[i + 1].Position.X), MY(verts[i + 1].Position.Y), elevation,
//                    ref name, section, storyName);
//                if (ret == 0 && !string.IsNullOrEmpty(name)) names.Add(name);
//            }

//            if (poly.IsClosed && verts.Count > 2)
//            {
//                string name = "";
//                int ret = sapModel.FrameObj.AddByCoord(
//                    MX(verts[verts.Count - 1].Position.X),
//                    MY(verts[verts.Count - 1].Position.Y), elevation,
//                    MX(verts[0].Position.X),
//                    MY(verts[0].Position.Y), elevation,
//                    ref name, section, storyName);
//                if (ret == 0 && !string.IsNullOrEmpty(name)) names.Add(name);
//            }

//            return names;
//        }

//        // ====================================================================
//        // STORY NAME — ETABS GetNameList is TOP-DOWN; our index is BOTTOM-UP
//        // ====================================================================

//        private string GetStoryName(int story)
//        {
//            try
//            {
//                int n = 0; string[] names = null;
//                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
//                    names != null && story >= 0 && story < n)
//                    return names[n - 1 - story];   // flip: 0=bottom → last entry
//            }
//            catch { }
//            return story == 0 ? "Base" : $"Story{story + 1}";
//        }
//    }

//    // ====================================================================
//    // DICT EXTENSION HELPER
//    // ====================================================================

//    internal static class DictExtensions
//    {
//        public static TValue GetValueOrDefault<TKey, TValue>(
//            this Dictionary<TKey, TValue> dict, TKey key,
//            TValue defaultValue = default)
//            => dict.TryGetValue(key, out TValue val) ? val : defaultValue;
//    }
//}







// ============================================================================
// FILE: Importers/BeamImporterEnhanced.cs
// ============================================================================
// BEAMS  → Assign > Frame Loads > Distributed  → Load PATTERN
// API    : sapModel.FrameObj.SetLoadDistributed(name, patternName, ...)
// Cache  : sapModel.LoadPatterns.GetNameList()
//
// Flow per beam layer:
//   1. DetermineBeamSection()  → picks section name + loadSetKey from layer name
//   2. ResolveBeamLoadSetName()→ reads UI value (beamWallLoadSets[key])
//   3. ResolveLoadPattern()    → fuzzy-matches that name against ETABS patterns
//   4. AssignBeamWallLoads()   → calls SetLoadDistributed if pattern found
// ============================================================================

using ETAB_Automation.Core;
using ETAB_Automation.Models;
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
        private readonly Dictionary<string, string> beamWallLoadSets; // from UI
        private readonly GradeScheduleManager gradeSchedule;

        private const double X_TO_M = 0.001;
        private const double Y_TO_M = 0.001;
        private double MX(double x) => x * X_TO_M;
        private double MY(double y) => y * Y_TO_M;

        // ====================================================================
        // SECTION CACHE  (static — populated once per process lifetime)
        // ====================================================================

        private static readonly Dictionary<string, GravityBeamInfo> gravityBeamSections
            = new Dictionary<string, GravityBeamInfo>();

        private static readonly Dictionary<string, MainBeamInfo> mainBeamSections
            = new Dictionary<string, MainBeamInfo>();

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
        // LOAD PATTERN CACHE  (instance — re-read for every new model/session)
        // Key   = UPPER-CASE pattern name
        // Value = exact name stored in ETABS  (case-sensitive)
        // ====================================================================

        private Dictionary<string, string> etabsLoadPatterns;

        // Key   = same as beamWallLoadSets (e.g. "InternalGravity")
        // Value = magnitude in N/m entered by user in UI (kN/m × 1000)
        private Dictionary<string, double> uiLoadMagnitudes;

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
            Dictionary<string, int> widthOverrides = null,
            Dictionary<string, string> wallLoadSets = null,
            Dictionary<string, double> wallLoadMagnitudes = null)
        {
            sapModel = model;
            dxfDoc = doc;
            seismicZone = zone;
            totalTypicalFloors = typicalFloors;
            beamDepths = depths ?? new Dictionary<string, int>();
            gradeSchedule = gradeManager;
            beamWidthOverrides = widthOverrides ?? new Dictionary<string, int>();
            beamWallLoadSets = wallLoadSets ?? new Dictionary<string, string>();

            // Convert kN/m → N/m for internal use
            uiLoadMagnitudes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (wallLoadMagnitudes != null)
                foreach (var kv in wallLoadMagnitudes)
                    if (kv.Value > 0)
                        uiLoadMagnitudes[kv.Key] = kv.Value * 1000.0;

            LoadBeamSections();
            LoadEtabsLoadPatterns();
        }

        // ====================================================================
        // STEP 1 — Discover all ETABS Load Patterns
        // Logged in full so any mismatch is immediately visible.
        // ====================================================================

        private void LoadEtabsLoadPatterns()
        {
            etabsLoadPatterns = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                int n = 0;
                string[] names = null;
                int ret = sapModel.LoadPatterns.GetNameList(ref n, ref names);

                if (ret != 0 || names == null || n == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "⚠ BEAM: Could not read Load Patterns from ETABS model.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"\n===== ETABS LOAD PATTERNS (for BEAM assignment) — {n} total =====");
                foreach (string name in names)
                {
                    etabsLoadPatterns[name.ToUpperInvariant()] = name;
                    System.Diagnostics.Debug.WriteLine($"  LP: '{name}'");
                }
                System.Diagnostics.Debug.WriteLine(
                    "====================================================================\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠ BEAM LoadEtabsLoadPatterns: {ex.Message}");
            }
        }


        // ====================================================================
        // STEP 2 — Fuzzy-resolve user name → exact ETABS Load Pattern name
        // 1. Exact match (case-insensitive)
        // 2. Substring match (user in pattern OR pattern in user)
        // Returns null if nothing matches → caller skips assignment.
        // ====================================================================

        private string ResolveLoadPattern(string userPatternName)
        {
            if (string.IsNullOrWhiteSpace(userPatternName)) return null;
            if (etabsLoadPatterns == null || etabsLoadPatterns.Count == 0) return null;

            string u = userPatternName.ToUpperInvariant().Trim();

            // 1. Exact
            if (etabsLoadPatterns.TryGetValue(u, out string exact)) return exact;

            // 2. Partial
            foreach (var kv in etabsLoadPatterns)
                if (kv.Key.Contains(u) || u.Contains(kv.Key))
                    return kv.Value;

            System.Diagnostics.Debug.WriteLine(
                $"  ⚠ BEAM: Load Pattern '{userPatternName}' not found in ETABS. " +
                "Check Define > Load Patterns — names are listed above.");
            return null;
        }

        // ====================================================================
        // SECTION LOADING
        // Pattern: B20X75M35  (gravity) | MB25X75M35 (main)
        // Groups : width-in-cm  × depth-in-cm  grade-number
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

                var mainPat = new Regex(
                    @"^MB(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
                    RegexOptions.IgnoreCase);
                var gravPat = new Regex(
                    @"^B(\d+(?:\.\d+)?)X(\d+(?:\.\d+)?)M(\d+)",
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
                    System.Diagnostics.Debug.WriteLine(
                        "⚠ No beam sections found in ETABS template — " +
                        "sections will be auto-defined as needed (B__/MB__ format).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadBeamSections: {ex.Message}");
                throw;
            }
        }

        // ====================================================================
        // WIDTH HELPERS
        // ====================================================================

        private int ResolveGravityWidth(string variantKey)
        {
            // 1. Per-variant override from UI
            if (beamWidthOverrides.TryGetValue(variantKey, out int v) && v > 0) return v;
            // 2. Shared gravity override (legacy)
            if (beamWidthOverrides.TryGetValue("GravityWidth", out int s) && s > 0) return s;
            // 3. Zone rule
            return (seismicZone.Contains("II") || seismicZone.Contains("III")) ? 200 : 240;
        }

        private int ResolveMainWidth(WallThicknessCalculator.WallType wt, string key)
        {
            if (beamWidthOverrides.TryGetValue(key, out int v) && v > 0) return v;
            return WallThicknessCalculator.GetRecommendedThickness(
                totalTypicalFloors, wt, seismicZone, 2.0, false);
        }

        // ====================================================================
        // CLOSEST-SECTION FINDERS
        // ====================================================================

        // ====================================================================
        // SECTION RESOLUTION — define-if-missing via SectionDefiner
        // ====================================================================

        /// <summary>
        /// Returns a gravity beam section name B{wCm}X{dCm}M{grade}.
        /// If the section does not exist in the template it is auto-defined.
        /// Falls back to closest existing section if auto-define fails.
        /// </summary>
        private string BestGravitySection(int reqWidth, int reqDepth, string grade)
        {
            // 1. Try to define / verify the exact section we need
            string exact = SectionDefiner.EnsureGravityBeamSection(sapModel, reqWidth, reqDepth, grade);
            if (!string.IsNullOrEmpty(exact))
                return exact;

            // 2. Fallback: closest existing section (old behaviour)
            string gn = NormalizeGrade(grade);
            string best = null; int minDiff = int.MaxValue;
            if (!string.IsNullOrEmpty(gn))
            {
                foreach (var kvp in gravityBeamSections)
                {
                    if (kvp.Value.Grade != gn) continue;
                    int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                          + Math.Abs(kvp.Value.WidthMm - reqWidth);
                    if (d < minDiff) { minDiff = d; best = kvp.Key; }
                }
                if (best != null) return best;
            }
            minDiff = int.MaxValue;
            foreach (var kvp in gravityBeamSections)
            {
                int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                      + Math.Abs(kvp.Value.WidthMm - reqWidth);
                if (d < minDiff) { minDiff = d; best = kvp.Key; }
            }
            return best ?? SectionDefiner.EnsureGravityBeamSection(sapModel, reqWidth, reqDepth, "M30");
        }

        /// <summary>
        /// Returns a main beam section name MB{wCm}X{dCm}M{grade}.
        /// If the section does not exist in the template it is auto-defined.
        /// Falls back to closest existing section if auto-define fails.
        /// </summary>
        private string BestMainSection(int reqWidth, int reqDepth, string grade)
        {
            // 1. Try to define / verify the exact section we need
            string exact = SectionDefiner.EnsureMainBeamSection(sapModel, reqWidth, reqDepth, grade);
            if (!string.IsNullOrEmpty(exact))
                return exact;

            // 2. Fallback: closest existing section
            string gn = NormalizeGrade(grade);
            string best = null; int minDiff = int.MaxValue;
            if (!string.IsNullOrEmpty(gn))
            {
                foreach (var kvp in mainBeamSections)
                {
                    if (kvp.Value.Grade != gn) continue;
                    int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                          + Math.Abs(kvp.Value.WidthMm - reqWidth);
                    if (d < minDiff) { minDiff = d; best = kvp.Key; }
                }
                if (best != null) return best;
            }
            minDiff = int.MaxValue;
            foreach (var kvp in mainBeamSections)
            {
                int d = Math.Abs(kvp.Value.DepthMm - reqDepth) * 2
                      + Math.Abs(kvp.Value.WidthMm - reqWidth);
                if (d < minDiff) { minDiff = d; best = kvp.Key; }
            }
            if (best == null && gravityBeamSections.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "⚠ No MB__ sections — falling back to gravity (B__) for main beam");
                return BestGravitySection(reqWidth, reqDepth, grade);
            }
            return best ?? SectionDefiner.EnsureMainBeamSection(sapModel, reqWidth, reqDepth, "M30");
        }

        private static string NormalizeGrade(string grade)
            => grade?.Replace("M", "").Replace("m", "").Trim();

        // ====================================================================
        // LAYER → SECTION + LOAD-SET KEY
        // Returns (sectionName, loadSetKey, category)
        // loadSetKey is used to look up the user-typed load pattern name.
        // ====================================================================

        private enum BeamCategory { Gravity, Main }

        private (string section, string loadSetKey, BeamCategory cat)
            DetermineBeamSection(string layerName, string grade)
        {
            string u = layerName.ToUpperInvariant();

            // ── MAIN BEAMS ─────────────────────────────────────────────────
            if (u.Contains("CORE") && u.Contains("MAIN"))
            {
                int w = ResolveMainWidth(WallThicknessCalculator.WallType.CoreWall, "CoreMainWidth");
                return (BestMainSection(w, beamDepths.GetValueOrDefault("CoreMain", 600), grade),
                        "CoreMain", BeamCategory.Main);
            }
            if (u.Contains("PERIPHERAL") && u.Contains("DEAD") && u.Contains("MAIN"))
            {
                int w = ResolveMainWidth(WallThicknessCalculator.WallType.PeripheralDeadWall, "PeripheralDeadMainWidth");
                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralDeadMain", 600), grade),
                        "PeripheralDeadMain", BeamCategory.Main);
            }
            if (u.Contains("PERIPHERAL") && u.Contains("PORTAL") && u.Contains("MAIN"))
            {
                int w = ResolveMainWidth(WallThicknessCalculator.WallType.PeripheralPortalWall, "PeripheralPortalMainWidth");
                return (BestMainSection(w, beamDepths.GetValueOrDefault("PeripheralPortalMain", 650), grade),
                        "PeripheralPortalMain", BeamCategory.Main);
            }
            if (u.Contains("INTERNAL") && u.Contains("MAIN"))
            {
                int w = ResolveMainWidth(WallThicknessCalculator.WallType.InternalWall, "InternalMainWidth");
                return (BestMainSection(w, beamDepths.GetValueOrDefault("InternalMain", 550), grade),
                        "InternalMain", BeamCategory.Main);
            }

            // ── GRAVITY BEAMS ──────────────────────────────────────────────

            // NoLoad must be checked BEFORE generic gravity matches
            if (u.Contains("NO LOAD") || u.Contains("NOLOAD") || u.Contains("NO-LOAD"))
            {
                int w = ResolveGravityWidth("NoLoadGravityWidth");
                return (BestGravitySection(w,
                        beamDepths.GetValueOrDefault("NoLoadGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                        "NoLoadGravity",     // → empty string in load set table
                        BeamCategory.Gravity);
            }
            if (u.Contains("CANTILEVER") && u.Contains("GRAVITY"))
            {
                int w = ResolveGravityWidth("CantileverGravityWidth");
                return (BestGravitySection(w, beamDepths.GetValueOrDefault("CantileverGravity", 500), grade),
                        "CantileverGravity", BeamCategory.Gravity);
            }
            if (u.Contains("EDECK") || u.Contains("E-DECK") || u.Contains("E DECK"))
            {
                int w = ResolveGravityWidth("EdeckGravityWidth");
                return (BestGravitySection(w,
                        beamDepths.GetValueOrDefault("EdeckGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                        "EdeckGravity", BeamCategory.Gravity);
            }
            if (u.Contains("PODIUM"))
            {
                int w = ResolveGravityWidth("PodiumGravityWidth");
                return (BestGravitySection(w,
                        beamDepths.GetValueOrDefault("PodiumGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                        "PodiumGravity", BeamCategory.Gravity);
            }
            if (u.Contains("GROUND"))
            {
                int w = ResolveGravityWidth("GroundGravityWidth");
                return (BestGravitySection(w,
                        beamDepths.GetValueOrDefault("GroundGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                        "GroundGravity", BeamCategory.Gravity);
            }
            if (u.Contains("BASEMENT"))
            {
                int w = ResolveGravityWidth("BasementGravityWidth");
                return (BestGravitySection(w,
                        beamDepths.GetValueOrDefault("BasementGravity",
                        beamDepths.GetValueOrDefault("InternalGravity", 450)), grade),
                        "BasementGravity", BeamCategory.Gravity);
            }
            if (u.Contains("INTERNAL") && u.Contains("GRAVITY"))
            {
                int w = ResolveGravityWidth("InternalGravityWidth");
                return (BestGravitySection(w, beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
                        "InternalGravity", BeamCategory.Gravity);
            }

            // Unknown → InternalGravity fallback
            System.Diagnostics.Debug.WriteLine(
                $"  ⚠ Unknown beam layer '{layerName}' → InternalGravity fallback");
            {
                int w = ResolveGravityWidth("InternalGravityWidth");
                return (BestGravitySection(w, beamDepths.GetValueOrDefault("InternalGravity", 450), grade),
                        "InternalGravity", BeamCategory.Gravity);
            }
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
                $"\n========== IMPORTING BEAMS — Story {story} | Elev {elevation:F3}m | Grade {beamGrade ?? "default"} ==========");

            int total = 0;

            foreach (string layerName in beamLayers)
            {
                var (section, loadSetKey, cat) = DetermineBeamSection(layerName, beamGrade);

                var createdNames = new List<string>();
                int cnt = 0;

                System.Diagnostics.Debug.WriteLine(
                    $"\n  Layer: '{layerName}' [{cat}] → section='{section}' loadKey='{loadSetKey}'");

                // ── Geometry creation ──────────────────────────────────────
                foreach (var line in dxfDoc.Entities.Lines
                    .Where(l => l.Layer.Name == layerName))
                {
                    string nm = CreateBeamFromLine(line, elevation, section, story);
                    if (!string.IsNullOrEmpty(nm)) { createdNames.Add(nm); cnt++; }
                }

                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    var nms = CreateBeamFromPolyline(poly, elevation, section, story);
                    createdNames.AddRange(nms);
                    cnt += nms.Count;
                }

                System.Diagnostics.Debug.WriteLine($"  Created: {cnt} beam(s)");

                // ── Moment releases (gravity beams only) ──────────────────
                if (createdNames.Count > 0 && _releaseLoadKeys.Contains(loadSetKey))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"  Assigning moment releases to {createdNames.Count} '{loadSetKey}' beam(s)...");
                    AssignMomentRelease(createdNames);
                }

                // ── Load assignment ────────────────────────────────────────
                if (createdNames.Count > 0)
                {
                    // Get the user-typed Load Pattern name for this beam type
                    string userPatternName = GetBeamLoadPatternName(loadSetKey);
                    AssignBeamWallLoads(createdNames, userPatternName, loadSetKey);
                }

                total += cnt;
            }

            System.Diagnostics.Debug.WriteLine(
                $"\n  Total beams story {story}: {total}\n");
        }

        // ====================================================================
        // GET BEAM LOAD PATTERN NAME FROM UI
        // Priority: (1) UI beamWallLoadSets dict  (2) FloorTypeConfig defaults
        // NoLoadGravity always returns empty → no load assigned.
        // ====================================================================

        private string GetBeamLoadPatternName(string loadSetKey)
        {
            // NoLoadGravity: explicitly no wall load
            if (loadSetKey == "NoLoadGravity") return string.Empty;

            // UI value (from constructor param, read from UI TextBox)
            if (beamWallLoadSets.TryGetValue(loadSetKey, out string uiVal)
                && !string.IsNullOrWhiteSpace(uiVal))
                return uiVal.Trim();

            // Static default fallback
            if (FloorTypeConfig.DefaultBeamWallLoadSets.TryGetValue(loadSetKey, out string def)
                && !string.IsNullOrWhiteSpace(def))
                return def.Trim();

            return string.Empty;
        }

        // ====================================================================
        // ASSIGN BEAM WALL LOADS via SetLoadDistributed (Load Pattern)
        // ====================================================================

        private void AssignBeamWallLoads(List<string> frameNames,
            string userPatternName, string loadSetKey)
        {
            // Empty → intentional (NoLoad or user left it blank)
            if (string.IsNullOrWhiteSpace(userPatternName))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⊘ No wall load for '{loadSetKey}' — skipping (intentional).");
                return;
            }

            // Resolve → exact ETABS Load Pattern name
            string patternName = ResolveLoadPattern(userPatternName);
            if (patternName == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⚠ Load Pattern '{userPatternName}' (key='{loadSetKey}') not found in ETABS. " +
                    "Skipping. Fix in Define > Load Patterns and re-run.");
                return;
            }

            // ── Resolve magnitude from UI (Wall Load Patterns tab, kN/m → N/m in constructor) ──
            if (!uiLoadMagnitudes.TryGetValue(loadSetKey, out double loadMagN) || loadMagN <= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⚠ BEAM: Magnitude for '{loadSetKey}' is 0 — enter a kN/m value in the " +
                    "Wall Load Patterns tab. Skipping.");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"  → Assigning Load Pattern '{patternName}' " +
                $"@ {loadMagN / 1000.0:F2} kN/m (from UI) to {frameNames.Count} beam(s)");

            int ok = 0, fail = 0;
            foreach (string name in frameNames)
            {
                try
                {
                    int ret = sapModel.FrameObj.SetLoadDistributed(
                        name,
                        patternName,  // exact ETABS Load Pattern
                        1,            // MyType : 1 = Force per unit length
                        10,           // Dir    : 10 = Gravity (downward) for ETABS 2026
                        0.0,          // Dist1  : relative start (0.0 = End-I)
                        1.0,          // Dist2  : relative end   (1.0 = End-J)
                        loadMagN,     // Val1   : magnitude read from ETABS (N/m)
                        loadMagN,     // Val2   : magnitude read from ETABS (N/m)
                        "Global",
                        true,         // RelDist : true = relative distances
                        true);        // Replace : true = replace existing load

                    if (ret == 0) ok++;
                    else
                    {
                        fail++;
                        System.Diagnostics.Debug.WriteLine(
                            $"    ⚠ SetLoadDistributed ret={ret} for beam '{name}'");
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    System.Diagnostics.Debug.WriteLine(
                        $"    ⚠ SetLoadDistributed exception for '{name}': {ex.Message}");
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"  Load Pattern '{patternName}': ✓{ok} assigned  ✗{fail} failed");
        }

        // ====================================================================
        // MOMENT RELEASE ASSIGNMENT
        // Releases Moment 22 (Minor) and Moment 33 (Major) at both ends.
        // Applied to gravity beams that should behave as simply supported.
        // ETABS API: FrameObj.SetReleases(name, iI[], iJ[], startV[], endV[])
        //   Index mapping (0-based):
        //     0 = Axial, 1 = Shear2, 2 = Shear3, 3 = Torsion, 4 = M22, 5 = M33
        // ====================================================================

        private static readonly HashSet<string> _releaseLoadKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "InternalGravity",
                "BasementGravity",
                "PodiumGravity",
                "GroundGravity",
                "EdeckGravity"
            };

        private void AssignMomentRelease(List<string> frameNames)
        {
            // ii = releases at start (I-end), ij = releases at end (J-end)
            // true  = released (pinned), false = fixed
            bool[] ii = { false, false, false, false, true, true }; // M22+M33 released at start
            bool[] ij = { false, false, false, false, true, true }; // M22+M33 released at end
            double[] startV = { 0, 0, 0, 0, 0, 0 }; // spring stiffness = 0 (pure release)
            double[] endV = { 0, 0, 0, 0, 0, 0 };

            int ok = 0, fail = 0;
            foreach (string name in frameNames)
            {
                try
                {
                    int ret = sapModel.FrameObj.SetReleases(name, ref ii, ref ij, ref startV, ref endV);
                    if (ret == 0) ok++;
                    else
                    {
                        fail++;
                        System.Diagnostics.Debug.WriteLine(
                            $"    ⚠ SetReleases ret={ret} for beam '{name}'");
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    System.Diagnostics.Debug.WriteLine(
                        $"    ⚠ SetReleases exception for '{name}': {ex.Message}");
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"  Moment releases (M22+M33 both ends): ✓{ok} assigned  ✗{fail} failed");
        }

        // ====================================================================
        // GEOMETRY CREATION
        // ====================================================================

        private string CreateBeamFromLine(netDxf.Entities.Line line,
            double elevation, string section, int story)
        {
            string name = "";
            int ret = sapModel.FrameObj.AddByCoord(
                MX(line.StartPoint.X), MY(line.StartPoint.Y), elevation,
                MX(line.EndPoint.X), MY(line.EndPoint.Y), elevation,
                ref name, section, GetStoryName(story));
            return (ret == 0 && !string.IsNullOrEmpty(name)) ? name : null;
        }

        private List<string> CreateBeamFromPolyline(Polyline2D poly,
            double elevation, string section, int story)
        {
            string storyName = GetStoryName(story);
            var verts = poly.Vertexes;
            var names = new List<string>();

            for (int i = 0; i < verts.Count - 1; i++)
            {
                string name = "";
                int ret = sapModel.FrameObj.AddByCoord(
                    MX(verts[i].Position.X), MY(verts[i].Position.Y), elevation,
                    MX(verts[i + 1].Position.X), MY(verts[i + 1].Position.Y), elevation,
                    ref name, section, storyName);
                if (ret == 0 && !string.IsNullOrEmpty(name)) names.Add(name);
            }

            if (poly.IsClosed && verts.Count > 2)
            {
                string name = "";
                int ret = sapModel.FrameObj.AddByCoord(
                    MX(verts[verts.Count - 1].Position.X),
                    MY(verts[verts.Count - 1].Position.Y), elevation,
                    MX(verts[0].Position.X),
                    MY(verts[0].Position.Y), elevation,
                    ref name, section, storyName);
                if (ret == 0 && !string.IsNullOrEmpty(name)) names.Add(name);
            }

            return names;
        }

        // ====================================================================
        // STORY NAME — ETABS GetNameList is TOP-DOWN; our index is BOTTOM-UP
        // ====================================================================

        private string GetStoryName(int story)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.Story.GetNameList(ref n, ref names) == 0 &&
                    names != null && story >= 0 && story < n)
                    return names[n - 1 - story];   // flip: 0=bottom → last entry
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }

    // ====================================================================
    // DICT EXTENSION HELPER
    // ====================================================================

    internal static class DictExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue = default)
            => dict.TryGetValue(key, out TValue val) ? val : defaultValue;
    }
}
