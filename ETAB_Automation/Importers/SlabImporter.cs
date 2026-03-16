


// ============================================================================
// FILE: Importers/SlabImporterEnhanced.cs
// ============================================================================
// SLABS  → Assign > Shell Loads > Uniform Load Set
// API    : sapModel.AreaObj.SetLoadUniform(name, loadPatternName, value, dir)
// Cache  : sapModel.LoadSets.GetNameList()   ← UNIFORM LOAD SETS (not LoadPatterns)
//
// IMPORTANT — how "Uniform Load Sets" work in ETABS vs the API:
//   The ETABS UI dialog "Assign > Shell Loads > Uniform Load Set" shows named
//   load sets (LOBBY, RESIDENTIAL, BALCONY, etc.).  These ARE a separate object
//   from Load Patterns — they live under sapModel.LoadSets.
//
//   To READ  their names: sapModel.LoadSets.GetNameList(ref n, ref names)
//   To ASSIGN to a slab : sapModel.AreaObj.SetLoadUniform(
//                             areaName, loadSetName, value, dir, replace, csys)
//
//   The "value" passed is the actual pressure in model units (kN/m²).
//   Since we cannot read back the magnitude stored in the Load Set via API,
//   we pass 1.0 and let ETABS apply the Load Set's own stored magnitude.
//   (The Load Set already has kN/m² baked in from the template.)
//
// Dir = 6 → Global -Z (Gravity downward)
//
// Flow per slab layer:
//   1. ClassifyLayer()              → AreaBased / CantileverSpan / UserThickness
//   2. DetermineSlabSection()       → picks ETABS slab section by thickness + grade
//   3. GetSlabLoadSetName()         → reads UI value (slabLoadSets[key])
//   4. ResolveUniformLoadSet()      → fuzzy-matches against ETABS Uniform Load Sets
//   5. AssignSlabLoads()            → calls AreaObj.SetLoadUniform if found
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
using static netDxf.Entities.HatchBoundaryPath;

namespace ETABS_CAD_Automation.Importers
{
    public class SlabImporterEnhanced
    {
        private readonly cSapModel sapModel;
        private readonly DxfDocument dxfDoc;
        private readonly Dictionary<string, int> slabConfig;
        // Per-layer individual load magnitudes (kN/m²) from UI
        // Key = pascal slab key (e.g. "Residential"), Value = SlabLoads
        private readonly Dictionary<string, SlabLoads> slabLoadData;
        private readonly GradeScheduleManager gradeSchedule;

        private const double MM_TO_M = 0.001;
        private const double CLOSURE_TOLERANCE = 10000.0;
        private const double MIN_AREA = 0.0001;
        private double M(double mm) => mm * MM_TO_M;

        // ====================================================================
        // SECTION CACHE  (static — populated once per process)
        // ====================================================================

        private static readonly Dictionary<string, SlabSectionInfo> availableSlabSections
            = new Dictionary<string, SlabSectionInfo>();

        private class SlabSectionInfo
        {
            public string SectionName { get; set; }
            public int ThicknessMm { get; set; }
            public string Grade { get; set; }
        }

        // (Uniform Load Set cache removed — loads are now assigned per-pattern directly)

        // ====================================================================
        // THICKNESS RULES  (instance — editable via constructor, not hardcoded)
        // ====================================================================

        // WHITE layers — area-based (area in m²): (thicknessMm, maxAreaM2)
        private readonly List<(int thickness, double maxArea)> AreaRules;

        // CYAN layers — cantilever span-based (span in m): (thicknessMm, maxSpanM)
        private readonly List<(int thickness, double maxSpan)> CantileverRules;

        // ====================================================================
        // LAYER CLASSIFICATION
        // ====================================================================

        private enum SlabRule { AreaBased, CantileverSpan, UserThickness }

        // YELLOW layers — fixed user-input thickness
        private static readonly HashSet<string> UserThicknessLayers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "S-FIRE TENDER", "S-LOBBY", "S-OHT", "S-STAIRCASE",
                "S-TERRACE FIRE TANK", "S-UGT", "S-LANDSCAPE",
                "S-SWIMMING", "S-DG", "S-STP"
            };

        // CYAN layers — cantilever span
        private static readonly HashSet<string> CantileverLayers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "S-CANTILEVER BALCONY", "S-CANTILVER BALCONY",
                "S-CANTILEVER CHAJJA",  "S-CANTILEVER CHAJJA+ODU",
                "S-BALCONY SLABS"
            };

        private SlabRule ClassifyLayer(string layerName)
        {
            string u = layerName.ToUpperInvariant().Trim();

            if (CantileverLayers.Contains(u) ||
                u.Contains("CANTILEVER") ||
                u.Contains("CANTILVER") ||
                u.Contains("CHAJJA") ||
                u.Contains("BALCONY"))
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
            GradeScheduleManager gradeManager = null,
            Dictionary<string, SlabLoads> loadData = null,
            List<(int thickness, double maxArea)> areaRules = null,
            List<(int thickness, double maxSpan)> cantileverRules = null)
        {
            sapModel = model;
            dxfDoc = doc;
            gradeSchedule = gradeManager;
            slabLoadData = loadData ?? new Dictionary<string, SlabLoads>();

            // Use user-supplied rules if provided and non-empty; else fall back to defaults
            AreaRules = (areaRules != null && areaRules.Count > 0)
                ? areaRules
                : FloorTypeConfig.DefaultSlabAreaRules;

            CantileverRules = (cantileverRules != null && cantileverRules.Count > 0)
                ? cantileverRules
                : FloorTypeConfig.DefaultSlabCantileverRules;

            System.Diagnostics.Debug.WriteLine(
                $"SlabImporter: AreaRules={AreaRules.Count} rows, " +
                $"CantileverRules={CantileverRules.Count} rows");

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
        // LOAD PATTERN MAP
        // Maps SlabLoads field name → ETABS load pattern name
        // These load patterns must exist in the ETABS model.
        // ====================================================================

        private static readonly Dictionary<string, string> LoadPatternNames
            = new Dictionary<string, string>
            {
                ["FF"] = "FLOOR FINISH",
                ["Filling"] = "FILLING",
                ["ASDL"] = "ASDL",
                ["LL"] = "LL",
                ["LL3"] = "LL>3",
                ["FireTender"] = "FIRE TENDER",
                ["TreeLoad"] = "TREE LOAD",
                ["MachineRoom"] = "MACHINE ROOM",
                ["WaterTank"] = "WATER TANK",
            };

        // ====================================================================
        // SECTION LOADING
        // Pattern: S125SM30  → thickness=125 mm, grade=30
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
                    $"✓ Slab sections loaded: {availableSlabSections.Count}");

                if (availableSlabSections.Count == 0)
                    DefineFallbackSections();
            }
            catch
            {
                DefineFallbackSections();
            }
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

        private string GetClosestSlabSection(int requiredThickness,
            string preferredGrade = null)
        {
            if (availableSlabSections.Count == 0)
                throw new InvalidOperationException("No slab sections available.");

            string gradeToMatch = preferredGrade?
                .Replace("M", "").Replace("m", "").Trim();

            string bestMatch = null;
            int minDiff = int.MaxValue;

            if (!string.IsNullOrEmpty(gradeToMatch))
            {
                // Exact thickness + grade
                foreach (var kvp in availableSlabSections)
                    if (kvp.Value.ThicknessMm == requiredThickness &&
                        kvp.Value.Grade == gradeToMatch)
                        return kvp.Key;

                // Closest thickness within same grade
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
                throw new InvalidOperationException(
                    $"No slab section found for {requiredThickness}mm.");
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

            // Build edge list: (length, normalised angle 0–180°)
            var edges = new List<(double len, double angle)>();
            for (int i = 0; i < pts.Count; i++)
            {
                int j = (i + 1) % pts.Count;
                double dx = pts[j].X - pts[i].X;
                double dy = pts[j].Y - pts[i].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0) angle += 180.0;
                edges.Add((len, angle));
            }

            const double ANGLE_TOL = 10.0;

            // Find the shortest-average parallel pair
            double bestSpan = double.MaxValue;
            bool foundPair = false;

            for (int a = 0; a < edges.Count - 1; a++)
            {
                for (int b = a + 1; b < edges.Count; b++)
                {
                    double diff = Math.Abs(edges[a].angle - edges[b].angle);
                    if (diff > 90) diff = 180.0 - diff;

                    if (diff <= ANGLE_TOL)
                    {
                        double avgLen = (edges[a].len + edges[b].len) / 2.0;
                        if (avgLen < bestSpan)
                        {
                            bestSpan = avgLen;
                            foundPair = true;
                        }
                    }
                }
            }

            // Fallback to shortest single edge if no parallel pair found
            double spanMm = foundPair ? bestSpan : edges.Min(e => e.len);
            return spanMm * MM_TO_M;
        }
        private int GetUserThickness(string layerUpper)
        {
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
                slabConfig.TryGetValue(key, out int t)) return t;

            foreach (var kvp in map)
                if (layerUpper.Contains(kvp.Key) || kvp.Key.Contains(layerUpper))
                    if (slabConfig.TryGetValue(kvp.Value, out int t2)) return t2;

            return 150;
        }

        // ====================================================================
        // SECTION DETERMINATION
        // ====================================================================

        private string DetermineSlabSection(string layerName,
            List<netDxf.Vector2> pts, string preferredGrade)
        {
            var rule = ClassifyLayer(layerName);
            switch (rule)
            {
                case SlabRule.CantileverSpan:
                    {
                        double span = CalculateCantileverSpan(pts);
                        int t = ThicknessFromSpan(span);
                        System.Diagnostics.Debug.WriteLine(
                            $"  CYAN [{layerName}]: span={span:F2}m → {t}mm");
                        // Try to create exact section first, fall back to closest
                        string exact = SectionDefiner.EnsureSlabSection(sapModel, t, preferredGrade);
                        if (!string.IsNullOrEmpty(exact)) return exact;
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                case SlabRule.UserThickness:
                    {
                        int t = GetUserThickness(layerName.ToUpperInvariant().Trim());
                        System.Diagnostics.Debug.WriteLine(
                            $"  YELLOW [{layerName}]: {t}mm (user input)");
                        string exact = SectionDefiner.EnsureSlabSection(sapModel, t, preferredGrade);
                        if (!string.IsNullOrEmpty(exact)) return exact;
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                default: // AreaBased (WHITE)
                    {
                        double area = Math.Abs(CalculatePolygonArea(pts));
                        int t = ThicknessFromArea(area);
                        System.Diagnostics.Debug.WriteLine(
                            $"  WHITE [{layerName}]: area={area:F2}m² → {t}mm");
                        string exact = SectionDefiner.EnsureSlabSection(sapModel, t, preferredGrade);
                        if (!string.IsNullOrEmpty(exact)) return exact;
                        return GetClosestSlabSection(t, preferredGrade);
                    }
            }
        }

        // ====================================================================
        // GET SLAB LOADS FOR LAYER
        // Maps CAD layer name → pascal key → SlabLoads from UI or defaults.
        // ====================================================================

        private SlabLoads GetSlabLoadsForLayer(string layerName)
        {
            string key = NormaliseToPascalKey(layerName);

            // 1. Check UI-supplied loads (from sharedSlabLoadControls via constructor)
            if (slabLoadData.TryGetValue(key, out SlabLoads ui) && ui != null)
                return ui;

            // 2. Fall back to defaults
            if (FloorTypeConfig.DefaultSlabLoads.TryGetValue(key, out SlabLoads def) && def != null)
                return def.Clone();

            System.Diagnostics.Debug.WriteLine(
                $"  ⚠ SLAB: No load data found for layer '{layerName}' (key='{key}'). " +
                "Using ASDL=1.0 minimum default.");
            return new SlabLoads(0, 0, 1, 0);
        }

        // ====================================================================
        // NORMALISE CAD LAYER FRAGMENT → PASCAL KEY
        // ====================================================================

        private static string NormaliseToPascalKey(string layerName)
        {
            // Strip "S-" prefix and any cantilever prefix
            string s = layerName.Trim();
            if (s.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(2).Trim();

            // Strip cantilever prefix if present
            if (s.StartsWith("CANTILEVER ", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(11).Trim();
            else if (s.StartsWith("CANTILVER ", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(10).Trim();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BALCONY"] = "Balcony",
                ["BALCONY SLABS"] = "Balcony",
                ["CHAJJA"] = "Chajja",
                ["CHAJJA+ODU"] = "ChajjaODU",
                ["FIRE TENDER"] = "FireTender",
                ["FIRE WATER TANK"] = "FireWaterTank",
                ["GARBAGE ROOM"] = "GarbageRoom",
                ["GARDEN/DINING AREA"] = "GardenDining",
                ["GARDEN DINING"] = "GardenDining",
                ["GYMNASIUM"] = "Gymnasium",
                ["INDOOR SPORTS"] = "IndoorSports",
                ["KITCHEN SUNK"] = "KitchenSunk",
                ["KITCHEN"] = "KitchenSunk",
                ["KITCHEN SINK"] = "KitchenSink",
                ["LMR TOP"] = "LMRTop",
                ["LMR"] = "LMR",
                ["LOBBY"] = "Lobby",
                ["METER ROOM"] = "MeterRoom",
                ["MULTIPURPOSE HALL"] = "MultipurposeHall",
                ["OHT TOP"] = "OHTTop",
                ["OHT"] = "OHT",
                ["PARKING TOILET"] = "ParkingToilet",
                ["PARKING"] = "Parking",
                ["PUMP ROOM"] = "PumpRoom",
                ["REFUGE"] = "Refuge",
                ["RESIDENTIAL"] = "Residential",
                ["RETAIL MAZZANINE"] = "RetailMazzanine",
                ["RETAIL TOILET"] = "RetailToilet",
                ["RETAIL"] = "Retail",
                ["SERVICE SLAB"] = "ServiceSlab",
                ["SOCIETY ROOM"] = "SocietyRoom",
                ["STACK PARKING"] = "StackParking",
                ["STAIRCASE"] = "Staircase",
                ["TERRACE FIRE TANK"] = "TerraceFire",
                ["TERRACE PUMP ROOM"] = "TerracePumpRoom",
                ["TERRACE"] = "Terrace",
                ["TOILET"] = "Toilet",
                ["UTILITY"] = "Utility",
                ["AMENITIES"] = "Amenities",
                ["DRIVEWAY"] = "Driveway",
                ["UGT"] = "UGT",
                ["LANDSCAPE"] = "Landscape",
                ["SWIMMING"] = "Swimming",
                ["DG"] = "DG",
                ["STP"] = "STP",
            };

            string u = s.ToUpperInvariant().Trim();
            if (map.TryGetValue(u, out string mapped)) return mapped;

            // Auto-pascal fallback
            var parts = s.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Select(p =>
                p.Length > 0
                    ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()
                    : ""));
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
                $"\n========== IMPORTING SLABS — Story {story} | Elev {elevation:F3}m | Grade {slabGrade ?? "default"} ==========");

            int ok = 0, fail = 0, skip = 0;

            foreach (string layerName in slabLayers)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"\n  Layer: '{layerName}' [{ClassifyLayer(layerName)}]");

                var createdNames = new List<string>();

                // ── Geometry creation ──────────────────────────────────────
                foreach (var poly in dxfDoc.Entities.Polylines2D
                    .Where(p => p.Layer.Name == layerName))
                {
                    var r = CreateSlabFromPolyline(poly, elevation, layerName,
                        slabGrade, createdNames);
                    if (r == Result.Success) ok++;
                    else if (r == Result.Failed) fail++;
                    else skip++;
                }

                foreach (var hatch in dxfDoc.Entities.Hatches
                    .Where(h => h.Layer.Name == layerName))
                {
                    var r = CreateSlabFromHatch(hatch, elevation, layerName,
                        slabGrade, createdNames);
                    if (r == Result.Success) ok++;
                    else if (r == Result.Failed) fail++;
                    else skip++;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"  Created: {createdNames.Count} slab(s)");

                // ── Individual load pattern assignment ─────────────────────
                if (createdNames.Count > 0)
                {
                    SlabLoads loads = GetSlabLoadsForLayer(layerName);
                    AssignIndividualLoads(createdNames, loads, layerName);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"\n  Slabs story {story}: ✓{ok}  ❌{fail}  ⊘{skip}\n");
        }

        // ====================================================================
        // ASSIGN INDIVIDUAL LOAD PATTERNS
        // Each non-zero field in SlabLoads is assigned to its own ETABS
        // load pattern via AreaObj.SetLoadUniform().
        //
        // Dir = 6 → Global -Z (Gravity downward, global coordinate system)
        // CSys = "Global"
        // Replace = true (first call); subsequent calls within same layer
        //           use Replace = false so prior patterns are not wiped.
        // ====================================================================

        private void AssignIndividualLoads(List<string> areaNames,
            SlabLoads loads, string layerName)
        {
            if (loads == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⊘ No load data for layer '{layerName}' — skipping.");
                return;
            }

            // Build list of (patternName, value) pairs — skip zero values
            var assignments = new List<(string pattern, double value)>
            {
                ("FLOOR FINISH", loads.FF),
                ("FILLING",      loads.Filling),
                ("ASDL",         loads.ASDL),
                ("LL",           loads.LL),
                ("LL>3",         loads.LL3),
                ("FIRE TENDER",  loads.FireTender),
                ("TREE LOAD",    loads.TreeLoad),
                ("MACHINE ROOM", loads.MachineRoom),
                ("WATER TANK",   loads.WaterTank),
            };

            System.Diagnostics.Debug.WriteLine(
                $"  → Assigning individual loads to {areaNames.Count} slab(s) on layer '{layerName}':");

            // FIX: Replace flag tracked per-slab (not per-pattern).
            // Each slab gets Replace=true on its very first load assignment,
            // then Replace=false for all subsequent patterns on that same slab.
            var firstCallPerSlab = new HashSet<string>(areaNames);

            foreach (var (pattern, value) in assignments)
            {
                if (value <= 0) continue;

                System.Diagnostics.Debug.WriteLine(
                    $"    [{pattern}] = {value:F2} kN/m²");

                int assigned = 0, failed = 0;

                foreach (string areaName in areaNames)
                {
                    try
                    {
                        bool replace = firstCallPerSlab.Contains(areaName);

                        // ETABS 2026 API direction codes for AreaObj.SetLoadUniform:
                        //   10 = Gravity (downward) ← correct for ETABS 2026
                        //    6 = Global Z (upward)  ← wrong
                        //    4 = Global X           ← old bug
                        int ret = sapModel.AreaObj.SetLoadUniform(
                            areaName,   // area object name
                            pattern,    // ETABS load pattern name
                            value * 1000,      // kN/m² magnitude
                            10,         // Dir=10 → Gravity (downward) for ETABS 2026
                            replace,    // Replace=true only on first pattern per slab
                            "Global");  // coordinate system

                        if (ret == 0)
                        {
                            assigned++;
                            firstCallPerSlab.Remove(areaName);
                        }
                        else
                        {
                            failed++;
                            System.Diagnostics.Debug.WriteLine(
                                $"      ⚠ SetLoadUniform ret={ret} for slab '{areaName}' " +
                                $"(pattern: '{pattern}', value: {value:F2})");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        System.Diagnostics.Debug.WriteLine(
                            $"      ⚠ SetLoadUniform exception for '{areaName}' " +
                            $"[{pattern}]: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"      ✓{assigned} assigned  ✗{failed} failed");
            }
        }

        // ====================================================================
        // GEOMETRY CREATION
        // ====================================================================

        private enum Result { Success, Failed, Skipped }

        private Result CreateSlabFromPolyline(Polyline2D poly, double elevation,
            string layerName, string grade, List<string> createdNames)
        {
            try
            {
                var verts = poly.Vertexes;
                if (verts == null || verts.Count < 3) return Result.Skipped;

                var pts = verts.Select(v => v.Position).ToList();
                if (!IsClosedOrAutoClose(ref pts)) return Result.Skipped;

                string section = DetermineSlabSection(layerName, pts, grade);
                return CreateSlabFromPoints(pts, elevation, section, createdNames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ❌ Polyline [{layerName}]: {ex.Message}");
                return Result.Failed;
            }
        }

        private Result CreateSlabFromHatch(Hatch hatch, double elevation,
            string layerName, string grade, List<string> createdNames)
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
                    var r = CreateSlabFromPoints(verts, elevation, section, createdNames);
                    if (r == Result.Success) overall = Result.Success;
                    else if (r == Result.Failed && overall != Result.Success)
                        overall = Result.Failed;
                }
                return overall;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ❌ Hatch [{layerName}]: {ex.Message}");
                return Result.Failed;
            }
        }

        private List<netDxf.Vector2> ExtractHatchBoundary(
            IReadOnlyList<Edge> edges)
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
            const int segs = 16;
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

        private Result CreateSlabFromPoints(List<netDxf.Vector2> pts,
            double elevation, string section, List<string> createdNames)
        {
            try
            {
                double area = CalculatePolygonArea(pts);
                if (Math.Abs(area) < MIN_AREA) return Result.Skipped;
                if (area < 0) pts.Reverse();  // ensure CCW winding

                var clean = RemoveDuplicates(pts);
                if (clean.Count < 3) return Result.Skipped;

                int n = clean.Count;
                string[] ptNames = new string[n];

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
                    createdNames?.Add(areaName);
                    System.Diagnostics.Debug.WriteLine(
                        $"  ✓ slab '{areaName}' | {section} | {n}pts | {Math.Abs(area):F2}m²");
                    return Result.Success;
                }
                return Result.Failed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ❌ CreateSlabFromPoints: {ex.Message}");
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
                    return names[n - 1 - story];
            }
            catch { }
            return story == 0 ? "Base" : $"Story{story + 1}";
        }
    }
}
