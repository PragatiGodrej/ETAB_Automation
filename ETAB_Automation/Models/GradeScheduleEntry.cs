// ============================================================================
// FILE: Models/GradeSchedule.cs
// ============================================================================
using System.Collections.Generic;
using System.Linq;

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Represents a single grade schedule entry
    /// </summary>
    public class GradeScheduleEntry
    {
        public string WallGrade { get; set; }           // e.g., "M50"
        public int FloorsFromBottom { get; set; }       // Number of floors for this grade
        public string BeamSlabGrade { get; set; }       // Auto-calculated: 0.7x wall grade
        public int StartFloor { get; set; }             // Starting floor number (0-based)
        public int EndFloor { get; set; }               // Ending floor number (0-based)

        public GradeScheduleEntry()
        {
        }

        public GradeScheduleEntry(string wallGrade, int floorsFromBottom)
        {
            WallGrade = wallGrade;
            FloorsFromBottom = floorsFromBottom;
            BeamSlabGrade = CalculateBeamSlabGrade(wallGrade);
        }

        /// <summary>
        /// Calculate beam/slab grade from wall grade (0.7x, rounded up to nearest 5)
        /// </summary>
        private string CalculateBeamSlabGrade(string wallGrade)
        {
            try
            {
                int wallValue = int.Parse(wallGrade.Replace("M", "").Replace("m", "").Trim());
                double beamSlabValue = wallValue * 0.7;
                int roundedValue = (int)(System.Math.Ceiling(beamSlabValue / 5.0) * 5);

                if (roundedValue < 30)
                    roundedValue = 30;

                return $"M{roundedValue}";
            }
            catch
            {
                return "M30";
            }
        }

        public override string ToString()
        {
            return $"Floors {StartFloor + 1}-{EndFloor + 1}: Wall {WallGrade}, Beam/Slab {BeamSlabGrade}";
        }
    }

    /// <summary>
    /// Complete grade schedule for the building
    /// </summary>
    public class GradeSchedule
    {
        public List<GradeScheduleEntry> Entries { get; set; }
        public int TotalFloors { get; private set; }

        public GradeSchedule()
        {
            Entries = new List<GradeScheduleEntry>();
        }

        public GradeSchedule(List<string> wallGrades, List<int> floorsPerGrade)
        {
            Entries = new List<GradeScheduleEntry>();
            Initialize(wallGrades, floorsPerGrade);
        }

        /// <summary>
        /// Initialize grade schedule from wall grades and floor counts
        /// </summary>
        public void Initialize(List<string> wallGrades, List<int> floorsPerGrade)
        {
            if (wallGrades == null || floorsPerGrade == null)
                throw new System.ArgumentNullException("Grade schedule parameters cannot be null");

            if (wallGrades.Count != floorsPerGrade.Count)
                throw new System.ArgumentException("Wall grades and floors per grade must have same count");

            Entries.Clear();
            TotalFloors = floorsPerGrade.Sum();
            int currentFloor = 0;

            for (int i = 0; i < wallGrades.Count; i++)
            {
                var entry = new GradeScheduleEntry(wallGrades[i], floorsPerGrade[i])
                {
                    StartFloor = currentFloor,
                    EndFloor = currentFloor + floorsPerGrade[i] - 1
                };

                Entries.Add(entry);
                currentFloor += floorsPerGrade[i];
            }
        }

        /// <summary>
        /// Add a grade schedule entry
        /// </summary>
        public void AddEntry(string wallGrade, int floors)
        {
            var entry = new GradeScheduleEntry(wallGrade, floors);

            if (Entries.Count > 0)
            {
                var lastEntry = Entries[Entries.Count - 1];
                entry.StartFloor = lastEntry.EndFloor + 1;
                entry.EndFloor = entry.StartFloor + floors - 1;
            }
            else
            {
                entry.StartFloor = 0;
                entry.EndFloor = floors - 1;
            }

            Entries.Add(entry);
            TotalFloors += floors;
        }

        /// <summary>
        /// Get wall grade for a specific story (0-based index)
        /// </summary>
        public string GetWallGradeForStory(int story)
        {
            if (story < 0 || story >= TotalFloors)
            {
                return Entries.Last().WallGrade;
            }

            foreach (var entry in Entries)
            {
                if (story >= entry.StartFloor && story <= entry.EndFloor)
                {
                    return entry.WallGrade;
                }
            }

            return Entries.Last().WallGrade;
        }

        /// <summary>
        /// Get beam/slab grade for a specific story (0-based index)
        /// </summary>
        public string GetBeamSlabGradeForStory(int story)
        {
            if (story < 0 || story >= TotalFloors)
            {
                return Entries.Last().BeamSlabGrade;
            }

            foreach (var entry in Entries)
            {
                if (story >= entry.StartFloor && story <= entry.EndFloor)
                {
                    return entry.BeamSlabGrade;
                }
            }

            return Entries.Last().BeamSlabGrade;
        }

        /// <summary>
        /// Validate that total floors match expected count
        /// </summary>
        public bool ValidateTotalFloors(int expectedFloors)
        {
            return TotalFloors == expectedFloors;
        }

        /// <summary>
        /// Get summary of grade schedule
        /// </summary>
        public string GetSummary()
        {
            string summary = "=== CONCRETE GRADE SCHEDULE ===\n\n";
            summary += $"Total Floors: {TotalFloors}\n\n";

            foreach (var entry in Entries)
            {
                summary += $"{entry}\n";
            }

            return summary;
        }

        public override string ToString()
        {
            return $"GradeSchedule: {Entries.Count} entries, {TotalFloors} total floors";
        }
    }
}