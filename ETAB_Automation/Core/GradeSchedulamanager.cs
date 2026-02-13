// ============================================================================
// FILE: Core/GradeScheduleManager.cs
// ============================================================================
// PURPOSE: Manages concrete grade assignments by floor level
// AUTHOR: ETAB Automation Team
// VERSION: 2.0
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ETAB_Automation.Core
{
    /// <summary>
    /// Manages concrete grade scheduling for walls, beams, and slabs
    /// Wall grades are user-defined, beam/slab grades auto-calculated as 0.7× wall grade
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

        public class GradeRange
        {
            public int StartFloor { get; set; }
            public int EndFloor { get; set; }
            public string WallGrade { get; set; }
            public string BeamSlabGrade { get; set; }
        }

        // ====================================================================
        // FIELDS
        // ====================================================================

        private List<GradeSchedule> gradeSchedules = new List<GradeSchedule>();
        private int totalFloors;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public GradeScheduleManager(List<string> wallGrades, List<int> floorsPerGrade)
        {
            if (wallGrades == null || floorsPerGrade == null)
                throw new ArgumentNullException("Grade schedule parameters cannot be null");

            if (wallGrades.Count != floorsPerGrade.Count)
                throw new ArgumentException("Wall grades and floors per grade must have same count");

            totalFloors = floorsPerGrade.Sum();

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
        // METHODS
        // ====================================================================

        private string CalculateBeamSlabGrade(string wallGrade)
        {
            int wallGradeValue = ExtractGradeValue(wallGrade);
            double beamSlabValue = wallGradeValue * 0.7;
            int roundedValue = (int)(Math.Ceiling(beamSlabValue / 5.0) * 5);
            if (roundedValue < 30) roundedValue = 30;
            return $"M{roundedValue}";
        }

        private int ExtractGradeValue(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                throw new ArgumentException("Grade cannot be null or empty");

            string numericPart = grade.ToUpperInvariant().Replace("M", "").Trim();

            if (int.TryParse(numericPart, out int value))
                return value;

            throw new ArgumentException($"Invalid grade format: {grade}");
        }

        public string GetWallGradeForStory(int story)
        {
            if (story < 0 || story >= totalFloors)
                return gradeSchedules.Last().WallGrade;

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                    return schedule.WallGrade;
            }
            return gradeSchedules.Last().WallGrade;
        }

        public string GetBeamSlabGradeForStory(int story)
        {
            if (story < 0 || story >= totalFloors)
                return gradeSchedules.Last().BeamSlabGrade;

            int floorsFromBottom = 0;
            foreach (var schedule in gradeSchedules)
            {
                floorsFromBottom += schedule.FloorsFromBottom;
                if (story < floorsFromBottom)
                    return schedule.BeamSlabGrade;
            }
            return gradeSchedules.Last().BeamSlabGrade;
        }

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

        public bool ValidateTotalFloors(int expectedFloors)
        {
            return totalFloors == expectedFloors;
        }

        public List<GradeSchedule> GetAllSchedules()
        {
            return new List<GradeSchedule>(gradeSchedules);
        }

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
    }
}
