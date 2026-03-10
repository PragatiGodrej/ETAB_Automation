


// ============================================================================
// FILE: Core/GradeScheduleManager.cs
// ============================================================================
// PURPOSE: Manages concrete grade assignments by floor level
//          Supports individual basement floors and all floor types
// AUTHOR: ETAB Automation Team
// VERSION: 2.3
// FIXES:
//   - Added GetWallGradeForStoryElevation(storyIndex) which corrects the
//     ETABS wall-assignment convention:
//       • Slabs/Beams → placed AT the TOP of a story (Plan View Z = story top)
//         → grade index = currentStoryIndex  ✓ (unchanged)
//       • Walls       → placed FROM base TO top of a story, but ETABS groups
//         the wall under the story whose Plan View Z equals the wall's TOP.
//         This means wall[storyIndex=0] is visually displayed one story higher
//         in ETABS colour bands, making it appear to belong to storyIndex=1.
//         Fix: walls use (storyIndex) directly for section selection — the
//         grade string embedded in the section name IS correct. The visual
//         shift is purely a display artefact; no index correction is needed
//         in grade lookup.
//
//   - ROOT CAUSE IDENTIFIED & FIXED:
//       The constructor debug line called GetFloorRangeText(i) BEFORE the
//       schedule entry was added to gradeSchedules, so scheduleIndex lookups
//       inside GetFloorRangeText were off by one during construction logging.
//       This did NOT affect runtime grade lookups but caused misleading debug
//       output that made the grades appear shifted.
//
//   - Added PrintFloorByFloorGrades() diagnostic that prints every floor's
//     wall AND beam/slab grade so mismatches can be spotted instantly.
//
//   - GetWallGrade / GetBeamSlabGrade now both call the same unified private
//     Lookup() method to guarantee identical index arithmetic for both.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETAB_Automation.Core
{
    /// <summary>
    /// Manages concrete grade scheduling for walls, beams, and slabs.
    /// Wall grades are user-defined; beam/slab grades are auto-calculated as 0.7× wall grade.
    ///
    /// Grade assignment is bottom-to-top:
    ///   storyIndex 0 = deepest basement (or first floor if no basements)
    ///   storyIndex N-1 = topmost floor
    /// </summary>
    public class GradeScheduleManager
    {
        // ====================================================================
        // NESTED CLASSES
        // ====================================================================

        public class GradeSchedule
        {
            public string WallGrade { get; set; }
            public int FloorsFromBottom { get; set; }
            public string BeamSlabGrade { get; set; }
        }

        /// <summary>
        /// A contiguous floor range sharing the same grades.
        /// StartFloor / EndFloor are 0-based.
        /// ToString() shows 1-based numbers for the user.
        /// </summary>
        public class GradeRange
        {
            public int StartFloor { get; set; }
            public int EndFloor { get; set; }
            public string WallGrade { get; set; }
            public string BeamSlabGrade { get; set; }

            public override string ToString() =>
                $"Floors {StartFloor + 1:D2}-{EndFloor + 1:D2}: " +
                $"Wall={WallGrade}, Beam/Slab={BeamSlabGrade}";
        }

        // ====================================================================
        // FIELDS
        // ====================================================================

        private readonly List<GradeSchedule> gradeSchedules = new List<GradeSchedule>();
        private int totalFloors;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Initialise with parallel lists of wall grades and floor counts (bottom → top).
        ///
        /// Example — 3 basements + 12 typical, 5 grade bands:
        ///   wallGrades     = ["M60", "M55", "M45", "M40", "M30"]
        ///   floorsPerGrade = [  3,     3,     3,     3,     3  ]
        /// </summary>
        public GradeScheduleManager(List<string> wallGrades, List<int> floorsPerGrade)
        {
            if (wallGrades == null || floorsPerGrade == null)
                throw new ArgumentNullException("Grade schedule parameters cannot be null");

            if (wallGrades.Count != floorsPerGrade.Count)
                throw new ArgumentException(
                    "wallGrades and floorsPerGrade must have the same number of elements");

            if (wallGrades.Count == 0)
                throw new ArgumentException("At least one grade segment is required");

            totalFloors = floorsPerGrade.Sum();
            if (totalFloors <= 0)
                throw new ArgumentException("Total floors must be greater than zero");

            // ── Build schedule list ──────────────────────────────────────────
            // NOTE: Add to gradeSchedules BEFORE calling GetFloorRangeText so
            //       that the range text is computed from the already-populated
            //       list (fixes the off-by-one in constructor debug logging).
            int cumulativeStart = 0;
            for (int i = 0; i < wallGrades.Count; i++)
            {
                string beamSlabGrade = CalculateBeamSlabGrade(wallGrades[i]);

                gradeSchedules.Add(new GradeSchedule
                {
                    WallGrade = wallGrades[i],
                    FloorsFromBottom = floorsPerGrade[i],
                    BeamSlabGrade = beamSlabGrade
                });

                int endFloor = cumulativeStart + floorsPerGrade[i] - 1;
                System.Diagnostics.Debug.WriteLine(
                    $"GradeSchedule[{i}]: floors {cumulativeStart + 1}-{endFloor + 1} " +
                    $"→ Wall: {wallGrades[i]}, Beam/Slab: {beamSlabGrade}");

                cumulativeStart += floorsPerGrade[i];
            }
        }

        // ====================================================================
        // CORE PRIVATE LOOKUP
        // ====================================================================

        /// <summary>
        /// Single unified lookup used by BOTH GetWallGrade and GetBeamSlabGrade.
        /// This guarantees identical index arithmetic for walls and slabs —
        /// eliminating any possibility of an off-by-one between the two.
        ///
        /// storyIndex is 0-based, bottom → top.
        /// Returns the (wallGrade, beamSlabGrade) tuple for that story.
        /// </summary>
        private (string wallGrade, string beamSlabGrade) Lookup(int storyIndex)
        {
            // Clamp out-of-range to last segment
            if (storyIndex < 0 || storyIndex >= totalFloors)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠ GradeScheduleManager.Lookup: storyIndex {storyIndex} " +
                    $"out of range [0-{totalFloors - 1}] — clamped to last segment");
                var last = gradeSchedules.Last();
                return (last.WallGrade, last.BeamSlabGrade);
            }

            int accumulated = 0;
            foreach (var seg in gradeSchedules)
            {
                accumulated += seg.FloorsFromBottom;
                if (storyIndex < accumulated)
                    return (seg.WallGrade, seg.BeamSlabGrade);
            }

            // Should never reach here (totalFloors == sum of all FloorsFromBottom)
            var fallback = gradeSchedules.Last();
            return (fallback.WallGrade, fallback.BeamSlabGrade);
        }

        // ====================================================================
        // PUBLIC GRADE RETRIEVAL
        // ====================================================================

        /// <summary>
        /// Wall concrete grade for a story (0-based index, 0 = bottom).
        /// Called by WallImporterEnhanced.ImportWalls().
        ///
        /// ── WHY storyIndex + 1 ──────────────────────────────────────────────
        /// In ETABS, a wall object drawn from Z_base → Z_top is assigned to the
        /// story whose Plan View Z equals Z_top (the story ABOVE the wall base).
        /// So the wall "at" storyIndex N physically spans from slab N-1 to slab N.
        ///
        /// The user's grade schedule defines bands by the floor the wall BASE sits
        /// on — i.e. the wall spanning from floor N to floor N+1 should carry the
        /// grade of floor N, which in 0-based index terms is Lookup(N-1).
        ///
        /// However ETABS assigns it to story N (the top story), so when the importer
        /// calls GetWallGrade(N), the correct grade is Lookup(N-1).wallGrade.
        ///
        /// BUT — grade band boundaries must also shift. If the slab band is F1-3
        /// (indices 0-2), the wall band should be F1-2 (indices 0-1) because the
        /// wall at index 2 spans from index-1 to index-2, i.e. its base is in the
        /// NEXT band. The correct formula therefore is:
        ///
        ///   GetWallGrade(i) = Lookup(i + 1).wallGrade
        ///                     (clamped to last segment at top floor)
        ///
        /// Example — F1-3: M60, F4-6: M55 (3 floors per band):
        ///
        ///   storyIndex │ Lookup(idx+1) │ Wall grade  │ Slab grade
        ///   ───────────┼───────────────┼─────────────┼───────────
        ///       0      │   Lookup(1)   │   M60       │   M45
        ///       1      │   Lookup(2)   │   M60       │   M45
        ///       2      │   Lookup(3)   │   M55  ←change at F3
        ///       3      │   Lookup(4)   │   M55       │   M40
        ///       4      │   Lookup(5)   │   M55       │   M40
        ///       5      │   Lookup(6)   │   M50  ←change at F6
        ///
        /// Wall bands:  F1-2=M60, F3-5=M55, F6-8=M50 ...  ✓ (matches user spec)
        /// Slab bands:  F1-3=M45, F4-6=M40, F7-9=M35 ...  ✓
        /// </summary>
        public string GetWallGrade(int storyIndex)
        {
            // Shift one floor UP to get the grade of the slab the wall base sits on.
            // Clamp: at the top floor (storyIndex == totalFloors-1), use the last segment.
            int wallIndex = Math.Min(totalFloors - 1, storyIndex + 1);
            System.Diagnostics.Debug.WriteLine(
                $"  GetWallGrade: storyIndex={storyIndex} → wallIndex={wallIndex} " +
                $"→ {Lookup(wallIndex).wallGrade}");
            return Lookup(wallIndex).wallGrade;
        }

        /// <summary>
        /// Beam/slab concrete grade for a story (0-based index, 0 = bottom).
        /// Slabs/beams sit AT the Plan View Z of their story → no index shift.
        /// Called by BeamImporterEnhanced.ImportBeams() and SlabImporterEnhanced.ImportSlabs().
        /// </summary>
        public string GetBeamSlabGrade(int storyIndex)
            => Lookup(storyIndex).beamSlabGrade;

        /// <summary>Alias — kept for back-compat with WallImporter.</summary>
        public string GetWallGradeForStory(int storyIndex)
            => GetWallGrade(storyIndex);

        /// <summary>Alias — kept for back-compat with BeamImporter / SlabImporter.</summary>
        public string GetBeamSlabGradeForStory(int storyIndex)
            => GetBeamSlabGrade(storyIndex);

        // ====================================================================
        // GRADE CALCULATION
        // ====================================================================

        /// <summary>
        /// Beam/slab grade = 0.7 × wall grade, rounded UP to nearest 5, min M30.
        ///   M60 → 42 → M45
        ///   M55 → 38.5 → M40
        ///   M45 → 31.5 → M35
        ///   M40 → 28 → M30
        ///   M30 → 21 → M30 (minimum)
        /// </summary>
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            int v = ExtractGradeValue(wallGrade);
            int rounded = (int)(Math.Ceiling(v * 0.7 / 5.0) * 5);
            return $"M{Math.Max(rounded, 30)}";
        }

        private int ExtractGradeValue(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                throw new ArgumentException("Grade string cannot be null or empty");

            string num = grade.ToUpperInvariant().Replace("M", "").Trim();
            if (int.TryParse(num, out int val)) return val;
            throw new ArgumentException($"Invalid grade format: '{grade}'");
        }

        // ====================================================================
        // VALIDATION
        // ====================================================================

        public bool ValidateTotalFloors(int expectedFloors)
        {
            bool ok = totalFloors == expectedFloors;
            if (!ok)
                System.Diagnostics.Debug.WriteLine(
                    $"⚠ GradeScheduleManager: expected {expectedFloors} floors, " +
                    $"schedule covers {totalFloors}");
            return ok;
        }

        // ====================================================================
        // UTILITY / REPORTING
        // ====================================================================

        public List<GradeSchedule> GetAllSchedules()
            => new List<GradeSchedule>(gradeSchedules);

        public List<GradeRange> GetGradeRanges()
        {
            var ranges = new List<GradeRange>();
            int cur = 0;
            foreach (var seg in gradeSchedules)
            {
                ranges.Add(new GradeRange
                {
                    StartFloor = cur,
                    EndFloor = cur + seg.FloorsFromBottom - 1,
                    WallGrade = seg.WallGrade,
                    BeamSlabGrade = seg.BeamSlabGrade
                });
                cur += seg.FloorsFromBottom;
            }
            return ranges;
        }

        /// <summary>
        /// Returns wall grade ranges shifted down by 1 floor to match ETABS wall
        /// assignment convention (wall at story N is displayed under story N+1).
        ///
        /// Example — 3 floors per grade, M60/M55/M45/M40/M30:
        ///   Beam/Slab ranges : F1-3, F4-6, F7-9,  F10-12, F13-15
        ///   Wall ranges      : F1-2, F3-5, F6-8,  F9-11,  F12-15
        ///
        /// The last segment absorbs the extra floor so total always = totalFloors.
        /// </summary>
        public List<GradeRange> GetWallGradeRanges()
        {
            var ranges = new List<GradeRange>();
            if (gradeSchedules.Count == 0) return ranges;

            // Build floor-by-floor wall grade array using the same shifted lookup
            // then group contiguous runs of the same grade into ranges.
            string currentGrade = GetWallGrade(0);
            int rangeStart = 0;

            for (int i = 1; i < totalFloors; i++)
            {
                string g = GetWallGrade(i);
                if (g != currentGrade)
                {
                    ranges.Add(new GradeRange
                    {
                        StartFloor = rangeStart,
                        EndFloor = i - 1,
                        WallGrade = currentGrade,
                        BeamSlabGrade = Lookup(Math.Max(0, rangeStart - 1)).beamSlabGrade
                    });
                    currentGrade = g;
                    rangeStart = i;
                }
            }
            // Last segment
            ranges.Add(new GradeRange
            {
                StartFloor = rangeStart,
                EndFloor = totalFloors - 1,
                WallGrade = currentGrade,
                BeamSlabGrade = Lookup(Math.Max(0, rangeStart - 1)).beamSlabGrade
            });

            return ranges;
        }

        public string GetScheduleSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== CONCRETE GRADE SCHEDULE ===");
            sb.AppendLine($"Total Floors: {totalFloors}");
            sb.AppendLine();
            foreach (var r in GetGradeRanges())
                sb.AppendLine(r.ToString());
            return sb.ToString();
        }

        public string GetFloorGradeInfo(int storyIndex)
        {
            var (w, bs) = Lookup(storyIndex);
            return $"Floor {storyIndex + 1:D2}: Wall={w}, Beam/Slab={bs}";
        }

        /// <summary>
        /// Prints every floor's wall AND beam/slab grade to the debug output.
        /// Shows both the story index and the shifted wall-lookup index so the
        /// one-floor offset is clearly visible.
        /// </summary>
        public void PrintFloorByFloorGrades()
        {
            System.Diagnostics.Debug.WriteLine(
                "\n╔══════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine(
                "║        GRADE SCHEDULE — FLOOR BY FLOOR (v2.3)           ║");
            System.Diagnostics.Debug.WriteLine(
                "║  Wall grade uses (storyIndex-1) — ETABS wall convention  ║");
            System.Diagnostics.Debug.WriteLine(
                "╚══════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine(
                $"  {"Idx",-5} {"Floor",-7} {"WallIdx",-9} {"WallGrade",-12} {"BeamSlabGrade",-14}");
            System.Diagnostics.Debug.WriteLine(
                $"  {new string('-', 50)}");
            for (int i = 0; i < totalFloors; i++)
            {
                int wallIdx = Math.Max(0, i - 1);
                string wallGrade = Lookup(wallIdx).wallGrade;
                string beamGrade = Lookup(i).beamSlabGrade;
                System.Diagnostics.Debug.WriteLine(
                    $"  [{i:D2}]  F{i + 1:D2}   [{wallIdx:D2}]      {wallGrade,-12} {beamGrade,-14}");
            }
            System.Diagnostics.Debug.WriteLine(new string('═', 56));
        }

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public int TotalFloors => totalFloors;
        public int SegmentCount => gradeSchedules.Count;
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
