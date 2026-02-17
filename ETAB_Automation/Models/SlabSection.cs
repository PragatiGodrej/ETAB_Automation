// ============================================================================
// FILE: Models/SlabSection.cs
// ============================================================================
// PURPOSE: Data model for slab sections with thickness and material properties
// AUTHOR: ETAB Automation Team
// VERSION: 2.1
// ============================================================================

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Represents a slab section with thickness and material grade
    /// Used to define slab cross-sections in ETABS
    /// </summary>
    public class SlabSection
    {
        /// <summary>
        /// Section name in ETABS format (e.g., "S160SM45", "S200SM40")
        /// Format: S{thickness}SM{grade}
        /// </summary>
        public string SectionName { get; set; }

        /// <summary>
        /// Slab thickness in millimeters
        /// </summary>
        public int ThicknessMm { get; set; }

        /// <summary>
        /// Concrete grade (e.g., "M45", "M40", "M35")
        /// </summary>
        public string Grade { get; set; }

        /// <summary>
        /// Slab thickness in meters (calculated property)
        /// </summary>
        public double ThicknessMeters => ThicknessMm / 1000.0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SlabSection()
        {
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="sectionName">Section name (e.g., "S160SM45")</param>
        /// <param name="thicknessMm">Thickness in millimeters</param>
        /// <param name="grade">Concrete grade (e.g., "M45")</param>
        public SlabSection(string sectionName, int thicknessMm, string grade)
        {
            SectionName = sectionName;
            ThicknessMm = thicknessMm;
            Grade = grade;
        }

        /// <summary>
        /// String representation of the slab section
        /// </summary>
        public override string ToString()
        {
            return $"{SectionName} ({ThicknessMm}mm, {Grade})";
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
