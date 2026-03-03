

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
        private readonly Dictionary<string, string> slabLoadSets; // from UI
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

        // ====================================================================
        // UNIFORM LOAD SET CACHE  (instance — re-read for every new session)
        //
        // These are the sets visible under:
        //   Assign > Shell Loads > Uniform Load Set
        // (e.g. BALCONY, LOBBY, RESIDENTIAL, CHAJJA …)
        //
        // Retrieved via: sapModel.LoadSets.GetNameList(ref n, ref names)
        // Assigned via : sapModel.AreaObj.SetLoadUniform(areaName, setName, value, dir, ...)
        //
        // Key   = UPPERCASE set name
        // Value = exact name as stored in ETABS
        // ====================================================================

        private Dictionary<string, string> etabsUniformLoadSets;

        // ====================================================================
        // THICKNESS RULES
        // ====================================================================

        // WHITE layers — area-based (area in m²)
        private static readonly List<(int thickness, double maxArea)> AreaRules =
            new List<(int, double)>
            {
                (125, 14), (135, 17), (150, 22), (160, 25),
                (175, 32), (200, 42), (250, 70)
            };

        // CYAN layers — cantilever span-based (span in m)
        private static readonly List<(int thickness, double maxSpan)> CantileverRules =
            new List<(int, double)>
            {
                (125, 1.0), (160, 1.5), (180, 1.8), (200, 5.0)
            };

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
            Dictionary<string, string> loadSets = null)
        {
            sapModel = model;
            dxfDoc = doc;
            gradeSchedule = gradeManager;
            slabLoadSets = loadSets ?? new Dictionary<string, string>();

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
            LoadEtabsUniformLoadSets();   // ← replaces LoadEtabsLoadPatterns()
        }

        // ====================================================================
        // STEP 1 — Build Uniform Load Set registry
        //
        // ETABS API LIMITATION: There is NO API method to retrieve the list of
        // Uniform Load Sets (shown in Assign > Shell Loads > Uniform Load Set).
        // sapModel.LoadSets does NOT exist in ETABSv1.
        //
        // Solution: We seed from the known set names visible in your ETABS model
        // template (from the UI screenshot), then attempt runtime discovery by
        // reading back any already-assigned loads from existing area objects.
        //
        // The assignment API (AreaObj.SetLoadUniform) accepts the Uniform Load
        // Set name exactly as it appears in the ETABS dialog — so as long as the
        // name matches, the call succeeds even without pre-validation.
        //
        // Strategy:
        //   1. Seed with all known Uniform Load Set names from your template.
        //   2. Accept any user-supplied name from UI (slabLoadSets dict) as-is.
        //   3. If SetLoadUniform returns non-zero, log a warning.
        // ====================================================================

        private void LoadEtabsUniformLoadSets()
        {
            etabsUniformLoadSets = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            // ── Seed with known Uniform Load Set names from ETABS template ──
            // These match exactly what is shown in:
            //   Assign > Shell Loads > Uniform Load Set dialog
            // Add or remove names here to match your specific model template.
            var knownSets = new[]
            {
                "FACADE",
                "BALCONY",
                "CHAJJA",
                "SOCIETY ROOM",
                "DRIVEWAY",
                "GARDEN/DINING AREA",
                "GYMNASIUM",
                "INDOOR SPORTS",
                "KITCHEN SUNK",
                "LMR",
                "LOBBY",
                "METER ROOM",
                "MULTIPURPOSE HALL",
                "OHT",
                "PARKING",
                "REFUGE",
                "RESIDENTIAL",
                "RETAIL",
                "SERVICE SLAB",
                "STACK PARKING",
                "STAIRCASE",
                "TERRACE",
                "TOILET",
                "UTILITY",
                "AMENITIES",
                "UGT",
                "LANDSCAPE",
                "SWIMMING",
                "DG",
                "STP",
                "FIRE TENDER",
                "WATER TANK",
                "PUMP ROOM",
                "LMR TOP",
                "OHT TOP",
                "TERRACE FIRE TANK",
                "TERRACE PUMP ROOM",
                "PARKING TOILET",
                "RETAIL TOILET",
                "RETAIL MAZZANINE",
                "GARBAGE ROOM",
                "GARDEN DINING",
                "INDOOR SPORTS",
                "KITCHEN SINK",
            };

            System.Diagnostics.Debug.WriteLine(
                $"\n===== UNIFORM LOAD SETS (seeded from template) — {knownSets.Length} entries =====");

            foreach (var name in knownSets)
            {
                etabsUniformLoadSets[name.ToUpperInvariant()] = name;
                System.Diagnostics.Debug.WriteLine($"  ULS: '{name}'");
            }

            System.Diagnostics.Debug.WriteLine(
                "=======================================================================\n");

            // ── Also add any names supplied from the UI dict directly ───────
            // This ensures user-typed custom names always pass through.
            foreach (var kv in slabLoadSets)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                {
                    string v = kv.Value.Trim();
                    etabsUniformLoadSets[v.ToUpperInvariant()] = v;
                }
            }
        }

        // ====================================================================
        // STEP 2 — Fuzzy-resolve user name → exact ETABS Uniform Load Set name
        // 1. Exact match (case-insensitive)
        // 2. Substring match (either direction)
        // Returns null if nothing found → caller skips assignment.
        // ====================================================================

        private string ResolveUniformLoadSet(string userSetName)
        {
            if (string.IsNullOrWhiteSpace(userSetName)) return null;
            if (etabsUniformLoadSets == null || etabsUniformLoadSets.Count == 0) return null;

            string u = userSetName.ToUpperInvariant().Trim();

            // 1. Exact
            if (etabsUniformLoadSets.TryGetValue(u, out string exact)) return exact;

            // 2. Partial
            foreach (var kv in etabsUniformLoadSets)
                if (kv.Key.Contains(u) || u.Contains(kv.Key))
                    return kv.Value;

            System.Diagnostics.Debug.WriteLine(
                $"  ⚠ SLAB: Uniform Load Set '{userSetName}' not found in ETABS. " +
                "Check Define > Load Sets > Uniform Load Sets — all set names are listed above.");
            return null;
        }

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
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                case SlabRule.UserThickness:
                    {
                        int t = GetUserThickness(layerName.ToUpperInvariant().Trim());
                        System.Diagnostics.Debug.WriteLine(
                            $"  YELLOW [{layerName}]: {t}mm (user input)");
                        return GetClosestSlabSection(t, preferredGrade);
                    }
                default: // AreaBased (WHITE)
                    {
                        double area = Math.Abs(CalculatePolygonArea(pts));
                        int t = ThicknessFromArea(area);
                        System.Diagnostics.Debug.WriteLine(
                            $"  WHITE [{layerName}]: area={area:F2}m² → {t}mm");
                        return GetClosestSlabSection(t, preferredGrade);
                    }
            }
        }

        // ====================================================================
        // GET SLAB UNIFORM LOAD SET NAME FROM UI
        // Priority: (1) UI slabLoadSets dict  (2) FloorTypeConfig defaults
        // Maps raw CAD layer name → short key → Uniform Load Set name.
        // ====================================================================

        private string GetSlabLoadSetName(string layerName)
        {
            // Strip "S-" prefix
            string stripped = layerName.Trim();
            if (stripped.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
                stripped = stripped.Substring(2).Trim();

            // 1. Exact match in UI dict (case-insensitive key)
            foreach (var kv in slabLoadSets)
                if (string.Equals(kv.Key, stripped, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(kv.Value))
                    return kv.Value.Trim();

            // 2. PascalCase normalisation → FloorTypeConfig default table
            string pascalKey = NormaliseToPascalKey(stripped);
            if (FloorTypeConfig.DefaultSlabLoadSets.TryGetValue(pascalKey, out string def)
                && !string.IsNullOrWhiteSpace(def))
                return def.Trim();

            // 3. Layer name itself uppercased (last resort)
            return stripped.ToUpperInvariant();
        }

        // ====================================================================
        // NORMALISE CAD LAYER FRAGMENT → PASCAL KEY
        // Matches keys in FloorTypeConfig.DefaultSlabLoadSets.
        // ====================================================================

        private static string NormaliseToPascalKey(string s)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CANTILEVER BALCONY"] = "Balcony",
                ["CANTILVER BALCONY"] = "Balcony",
                ["BALCONY SLABS"] = "Balcony",
                ["CANTILEVER CHAJJA"] = "Chajja",
                ["CANTILEVER CHAJJA+ODU"] = "ChajjaODU",
                ["FIRE TENDER"] = "FireTender",
                ["FIRE WATER TANK"] = "FireWaterTank",
                ["GARBAGE ROOM"] = "GarbageRoom",
                ["GARDEN/DINING AREA"] = "GardenDining",
                ["GARDEN DINING"] = "GardenDining",
                ["INDOOR SPORTS"] = "IndoorSports",
                ["KITCHEN SUNK"] = "KitchenSink",
                ["KITCHEN SINK"] = "KitchenSink",
                ["LMR TOP"] = "LMRTop",
                ["LMRTOP"] = "LMRTop",
                ["METER ROOM"] = "MeterRoom",
                ["MULTIPURPOSE HALL"] = "MultipurposeHall",
                ["OHT TOP"] = "OHTTop",
                ["PARKING TOILET"] = "ParkingToilet",
                ["PUMP ROOM"] = "PumpRoom",
                ["RETAIL MAZZANINE"] = "RetailMazzanine",
                ["RETAIL TOILET"] = "RetailToilet",
                ["SERVICE SLAB"] = "ServiceSlab",
                ["SOCIETY ROOM"] = "SocietyRoom",
                ["STACK PARKING"] = "StackParking",
                ["STAIRCASE"] = "Staircase",
                ["TERRACE FIRE TANK"] = "TerraceFire",
                ["TERRACE PUMP ROOM"] = "TerracePumpRoom",
                ["AMENITIES"] = "Amenities",
                ["DRIVEWAY"] = "Driveway",
                ["GYMNASIUM"] = "Gymnasium",
                ["LMR"] = "LMR",
                ["LOBBY"] = "Lobby",
                ["OHT"] = "OHT",
                ["PARKING"] = "Parking",
                ["REFUGE"] = "Refuge",
                ["RESIDENTIAL"] = "Residential",
                ["RETAIL"] = "Retail",
                ["TERRACE"] = "Terrace",
                ["TOILET"] = "Toilet",
                ["UTILITY"] = "Utility",
                ["UGT"] = "UGT",
                ["LANDSCAPE"] = "Landscape",
                ["SWIMMING"] = "Swimming",
                ["DG"] = "DG",
                ["STP"] = "STP",
            };

            string u = s.ToUpperInvariant().Trim();
            if (map.TryGetValue(u, out string mapped)) return mapped;

            // Auto-pascal fallback
            var parts = s.Split(new[] { ' ', '-', '_' },
                StringSplitOptions.RemoveEmptyEntries);
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

                // ── Uniform Load Set assignment (only after creation confirmed) ──
                if (createdNames.Count > 0)
                {
                    string userSetName = GetSlabLoadSetName(layerName);
                    AssignSlabUniformLoadSets(createdNames, userSetName, layerName);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"\n  Slabs story {story}: ✓{ok}  ❌{fail}  ⊘{skip}\n");
        }

        //, string CSys,
        //                      eItemType ItemType)
        //
        // CRITICAL DISTINCTION:
        //   • Beams   → use LoadPatterns (DL, LL, WALL LOAD …)
        //   • Slabs   → use Uniform Load Sets (BALCONY, LOBBY, RESIDENTIAL …)
        //               retrieved via sapModel.LoadSets.GetNameList()
        //               assigned via the same SetLoadUniform call but with
        //               the Uniform Load Set name as "LoadPat" argument.
        //
        // Dir = 6 → Global -Z (Gravity downward)
        //           (Dir 4 = Global X, 5 = Global Y, 6 = Global Z / gravity)
        //
        // Value = 1.0 because the Uniform Load Set already stores its own
        //         kN/m² magnitude defined in Define > Load Sets > Uniform Load Sets.
        // ====================================================================

        private void AssignSlabUniformLoadSets(List<string> areaNames,
            string userSetName, string layerName)
        {
            if (string.IsNullOrWhiteSpace(userSetName))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⊘ No Uniform Load Set name for layer '{layerName}' — skipping.");
                return;
            }

            // Resolve user-typed name → exact ETABS Uniform Load Set name
            string resolvedSetName = ResolveUniformLoadSet(userSetName);
            if (resolvedSetName == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"  ⚠ Uniform Load Set '{userSetName}' not found in ETABS " +
                    "(see Define > Load Sets > Uniform Load Sets). Skipping.");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"  → Assigning Uniform Load Set '{resolvedSetName}' to {areaNames.Count} slab(s)");

            int assigned = 0, failed = 0;
            foreach (string name in areaNames)
            {
                try
                {
                    // SetLoadUniform assigns a uniform pressure load to a shell
                    // using a Uniform Load Set name.
                    // Dir 2 = Local -Z = perpendicular to shell surface downward.
                    // CSys "Local" — matches official ETABS API documentation example.
                    // Value = 1.0 because the Uniform Load Set already stores its
                    // own kN/m² magnitude (set via Define > Load Sets > Uniform Load Sets).
                    int ret = sapModel.AreaObj.SetLoadUniform(
                        name,               // area object name
                        resolvedSetName,    // Uniform Load Set name (exact from ETABS dialog)
                        1.0,                // multiplier — Load Set stores actual kN/m²
                        2,                  // Dir: 2 = Local -Z (gravity, perpendicular to shell)
                        true,               // Replace existing loads
                        "Local");           // coordinate system — per official API docs

                    if (ret == 0)
                    {
                        assigned++;
                        System.Diagnostics.Debug.WriteLine(
                            $"    ✓ '{resolvedSetName}' → slab '{name}'");
                    }
                    else
                    {
                        failed++;
                        System.Diagnostics.Debug.WriteLine(
                            $"    ⚠ SetLoadUniform ret={ret} for slab '{name}' " +
                            $"(Load Set: '{resolvedSetName}')");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    System.Diagnostics.Debug.WriteLine(
                        $"    ⚠ SetLoadUniform exception for '{name}': {ex.Message}");
                }
            }
            System.Diagnostics.Debug.WriteLine(
                $"  Uniform Load Set '{resolvedSetName}': ✓{assigned} assigned  ✗{failed} failed");
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
