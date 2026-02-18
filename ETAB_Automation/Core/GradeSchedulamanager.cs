
// ============================================================================
// FILE: Core/GradeScheduleManager.cs (UPDATED)
// ============================================================================
// PURPOSE: Manages concrete grade assignments by floor level
//          Supports individual basement floors and all floor types
// AUTHOR: ETAB Automation Team
// VERSION: 2.2 (Individual Basement Floor Support)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETAB_Automation.Core
{
    /// <summary>
    /// Manages concrete grade scheduling for walls, beams, and slabs
    /// Wall grades are user-defined, beam/slab grades auto-calculated as 0.7× wall grade
    /// 
    /// Supports all floor types:
    /// - Individual basement floors (Basement1-5)
    /// - Podium floors
    /// - Ground floor
    /// - E-Deck floor
    /// - Typical floors
    /// - Terrace floor
    /// </summary>
    public class GradeScheduleManager
    {
        // ====================================================================
        // NESTED CLASSES
        // ====================================================================

        /// <summary>
        /// Represents a single grade schedule segment
        /// </summary>
        public class GradeSchedule
        {
            /// <summary>Wall concrete grade (e.g., M50, M45, M40)</summary>
            public string WallGrade { get; set; }

            /// <summary>Number of floors from bottom using this grade</summary>
            public int FloorsFromBottom { get; set; }

            /// <summary>Auto-calculated beam/slab grade (0.7× wall grade)</summary>
            public string BeamSlabGrade { get; set; }
        }

        /// <summary>
        /// Represents a floor range with assigned grades
        /// Floor indices are 0-based:
        ///   Index 0 = First basement (Basement1) or first floor if no basements
        ///   Index 1 = Second basement (Basement2) or second floor
        ///   etc.
        /// </summary>
        public class GradeRange
        {
            /// <summary>Starting floor number (0-based)</summary>
            public int StartFloor { get; set; }

            /// <summary>Ending floor number (0-based, inclusive)</summary>
            public int EndFloor { get; set; }

            /// <summary>Wall concrete grade for this range</summary>
            public string WallGrade { get; set; }

            /// <summary>Beam/slab concrete grade for this range</summary>
            public string BeamSlabGrade { get; set; }

            /// <summary>
            /// Get a human-readable description of this range
            /// Display uses 1-based floor numbers for user clarity
            /// </summary>
            public override string ToString()
            {
                return $"Floors {StartFloor + 1:D2}-{EndFloor + 1:D2}: Wall={WallGrade}, Beam/Slab={BeamSlabGrade}";
            }
        }

        // ====================================================================
        // FIELDS
        // ====================================================================

        private List<GradeSchedule> gradeSchedules = new List<GradeSchedule>();
        private int totalFloors;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Initialize grade schedule manager with wall grades and floor counts
        /// Grades are assigned from bottom to top of building
        /// 
        /// Example for 5-basement + 10-typical building:
        ///   wallGrades = ["M50", "M45", "M40"]
        ///   floorsPerGrade = [5, 5, 5]
        ///   Result: Basements 1-5 = M50, Typical 1-5 = M45, Typical 6-10 = M40
        /// </summary>
        /// <param name="wallGrades">List of wall concrete grades (e.g., ["M50", "M45", "M40"])</param>
        /// <param name="floorsPerGrade">Number of floors for each grade segment (e.g., [5, 5, 5])</param>
        public GradeScheduleManager(List<string> wallGrades, List<int> floorsPerGrade)
        {
            if (wallGrades == null || floorsPerGrade == null)
                throw new ArgumentNullException("Grade schedule parameters cannot be null");

            if (wallGrades.Count != floorsPerGrade.Count)
                throw new ArgumentException("Wall grades and floors per grade must have same count");

            if (wallGrades.Count == 0)
                throw new ArgumentException("At least one grade segment is required");

            totalFloors = floorsPerGrade.Sum();

            if (totalFloors <= 0)
                throw new ArgumentException("Total floors must be greater than zero");

            // Build grade schedule
            for (int i = 0; i < wallGrades.Count; i++)
            {
                string wallGrade = wallGrades[i];
                int floors = floorsPerGrade[i];
                string beamSlabGrade = CalculateBeamSlabGrade(wallGrade);

                gradeSchedules.Add(new GradeSchedule
                {
                    WallGrade = wallGrade,
                    FloorsFromBottom = floors,
                    BeamSlabGrade = beamSlabGrade
                });

                System.Diagnostics.Debug.WriteLine(
                    $"Grade Schedule: Floors {GetFloorRangeText(i)} → Wall: {wallGrade}, Beam/Slab: {beamSlabGrade}");
            }
        }

        // ====================================================================
        // GRADE CALCULATION
        // ====================================================================

        /// <summary>
        /// Calculate beam/slab grade from wall grade using 0.7× formula
        /// Result is rounded up to nearest 5, with minimum of M30
        /// 
        /// Examples:
        ///   M50 → 50 × 0.7 = 35 → M35
        ///   M45 → 45 × 0.7 = 31.5 → round to 35 → M35
        ///   M40 → 40 × 0.7 = 28 → round to 30 → M30 (minimum)
        /// </summary>
        /// <param name="wallGrade">Wall grade (e.g., "M50")</param>
        /// <returns>Calculated beam/slab grade (e.g., "M35")</returns>
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            int wallGradeValue = ExtractGradeValue(wallGrade);
            double beamSlabValue = wallGradeValue * 0.7;
            int roundedValue = (int)(Math.Ceiling(beamSlabValue / 5.0) * 5);

            // Minimum grade M30
            if (roundedValue < 30)
                roundedValue = 30;

            return $"M{roundedValue}";
        }

        /// <summary>
        /// Extract numeric value from grade string (e.g., "M50" → 50)
        /// </summary>
        /// <param name="grade">Grade string (e.g., "M50", "m45")</param>
        /// <returns>Numeric grade value</returns>
        private int ExtractGradeValue(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                throw new ArgumentException("Grade cannot be null or empty");

            string numericPart = grade.ToUpperInvariant().Replace("M", "").Trim();

            if (int.TryParse(numericPart, out int value))
                return value;

            throw new ArgumentException($"Invalid grade format: {grade}");
        }

        // ====================================================================
        // GRADE RETRIEVAL BY STORY INDEX
        // ====================================================================

        /// <summary>
        /// Get wall grade for a specific story (0-based index)
        /// This is the method called by CADImporterEnhanced
        /// 
        /// Story index mapping:
        ///   0 = First basement (Basement1) OR first floor if no basements
        ///   1 = Second basement (Basement2) OR second floor
        ///   2 = Third basement (Basement3) OR third floor
        ///   etc.
        /// </summary>
        /// <param name="storyIndex">Story index (0-based)</param>
        /// <returns>Wall concrete grade (e.g., "M50")</returns>
        public string GetWallGrade(int storyIndex)
        {
            return GetWallGradeForStory(storyIndex);
        }

        /// <summary>
        /// Get beam/slab grade for a specific story (0-based index)
        /// This is the method called by CADImporterEnhanced
        /// </summary>
        /// <param name="storyIndex">Story index (0-based)</param>
        /// <returns>Beam/slab concrete grade (e.g., "M35")</returns>
        public string GetBeamSlabGrade(int storyIndex)
        {
            return GetBeamSlabGradeForStory(storyIndex);
        }

        /// <summary>
        /// Get wall grade for a specific story (0-based index)
        /// Internal implementation method
        /// </summary>
        /// <param name="story">Story index (0-based, 0 = bottom floor)</param>
        /// <returns>Wall concrete grade</returns>
        public string GetWallGradeForStory(int story)
        {
            // Handle out-of-range: use last grade as default
            if (story < 0 || story >= totalFloors)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️  Story index {story} out of range [0-{totalFloors - 1}], using last grade");
                return gradeSchedules.Last().WallGrade;
            }

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                    return schedule.WallGrade;
            }

            // Fallback to last grade
            return gradeSchedules.Last().WallGrade;
        }

        /// <summary>
        /// Get beam/slab grade for a specific story (0-based index)
        /// Internal implementation method
        /// </summary>
        /// <param name="story">Story index (0-based, 0 = bottom floor)</param>
        /// <returns>Beam/slab concrete grade</returns>
        public string GetBeamSlabGradeForStory(int story)
        {
            // Handle out-of-range: use last grade as default
            if (story < 0 || story >= totalFloors)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️  Story index {story} out of range [0-{totalFloors - 1}], using last grade");
                return gradeSchedules.Last().BeamSlabGrade;
            }

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                    return schedule.BeamSlabGrade;
            }

            // Fallback to last grade
            return gradeSchedules.Last().BeamSlabGrade;
        }

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        /// <summary>
        /// Get floor range text for a schedule index (for display purposes)
        /// Uses 1-based floor numbers for user clarity
        /// </summary>
        /// <param name="scheduleIndex">Index in gradeSchedules list</param>
        /// <returns>Floor range text (e.g., "1-11")</returns>
        private string GetFloorRangeText(int scheduleIndex)
        {
            if (scheduleIndex < 0 || scheduleIndex >= gradeSchedules.Count)
                return "Unknown";

            int startFloor = 0;
            for (int i = 0; i < scheduleIndex; i++)
                startFloor += gradeSchedules[i].FloorsFromBottom;

            int endFloor = startFloor + gradeSchedules[scheduleIndex].FloorsFromBottom - 1;
            return $"{startFloor + 1}-{endFloor + 1}";
        }

        /// <summary>
        /// Get a formatted summary of the entire grade schedule
        /// </summary>
        /// <returns>Multi-line summary string</returns>
        public string GetScheduleSummary()
        {
            string summary = "=== CONCRETE GRADE SCHEDULE ===\n\n";
            summary += $"Total Floors: {totalFloors}\n\n";

            for (int i = 0; i < gradeSchedules.Count; i++)
            {
                var schedule = gradeSchedules[i];
                string floorRange = GetFloorRangeText(i);
                summary += $"Floors {floorRange} ({schedule.FloorsFromBottom} floors):\n";
                summary += $"  Wall Grade: {schedule.WallGrade}\n";
                summary += $"  Beam/Slab Grade: {schedule.BeamSlabGrade}\n\n";
            }
            return summary;
        }

        /// <summary>
        /// Validate that the total floors in the schedule matches expected count
        /// </summary>
        /// <param name="expectedFloors">Expected number of floors</param>
        /// <returns>True if total matches expected</returns>
        public bool ValidateTotalFloors(int expectedFloors)
        {
            bool isValid = totalFloors == expectedFloors;

            if (!isValid)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️  Grade schedule validation failed: Expected {expectedFloors} floors, got {totalFloors}");
            }

            return isValid;
        }

        /// <summary>
        /// Get a copy of all grade schedules
        /// </summary>
        /// <returns>List of grade schedules</returns>
        public List<GradeSchedule> GetAllSchedules()
        {
            return new List<GradeSchedule>(gradeSchedules);
        }

        /// <summary>
        /// Get grade ranges with floor numbers (useful for display/reporting)
        /// Returns 0-based floor indices internally, but ToString() shows 1-based
        /// </summary>
        /// <returns>List of grade ranges</returns>
        public List<GradeRange> GetGradeRanges()
        {
            List<GradeRange> ranges = new List<GradeRange>();
            int currentFloor = 0;

            for (int i = 0; i < gradeSchedules.Count; i++)
            {
                var schedule = gradeSchedules[i];
                ranges.Add(new GradeRange
                {
                    StartFloor = currentFloor,
                    EndFloor = currentFloor + schedule.FloorsFromBottom - 1,
                    WallGrade = schedule.WallGrade,
                    BeamSlabGrade = schedule.BeamSlabGrade
                });
                currentFloor += schedule.FloorsFromBottom;
            }
            return ranges;
        }

        // ====================================================================
        // DIAGNOSTIC METHODS
        // ====================================================================

        /// <summary>
        /// Print detailed grade schedule to debug output
        /// Shows how grades map to individual basement floors and other floors
        /// </summary>
        public void PrintDetailedSchedule()
        {
            System.Diagnostics.Debug.WriteLine("\n╔════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║         CONCRETE GRADE SCHEDULE DETAIL             ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════╝\n");

            System.Diagnostics.Debug.WriteLine($"Total Floors: {totalFloors}");
            System.Diagnostics.Debug.WriteLine("(Includes individual basements, ground, podium, etc.)\n");

            var ranges = GetGradeRanges();
            foreach (var range in ranges)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Floors {range.StartFloor + 1:D2}-{range.EndFloor + 1:D2}: " +
                    $"Wall={range.WallGrade}, Beam/Slab={range.BeamSlabGrade}");
            }

            System.Diagnostics.Debug.WriteLine("\n" + new string('═', 56));
        }

        /// <summary>
        /// Get grade information for a specific floor (for debugging)
        /// </summary>
        /// <param name="floorIndex">Floor index (0-based)</param>
        /// <returns>Formatted string with grade info</returns>
        public string GetFloorGradeInfo(int floorIndex)
        {
            string wallGrade = GetWallGrade(floorIndex);
            string beamSlabGrade = GetBeamSlabGrade(floorIndex);
            return $"Floor {floorIndex + 1:D2}: Wall={wallGrade}, Beam/Slab={beamSlabGrade}";
        }

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        /// <summary>
        /// Get total number of floors covered by this schedule
        /// Includes all floor types: basements, podium, ground, e-deck, typical, terrace
        /// </summary>
        public int TotalFloors => totalFloors;

        /// <summary>
        /// Get number of grade segments in the schedule
        /// </summary>
        public int SegmentCount => gradeSchedules.Count;
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
