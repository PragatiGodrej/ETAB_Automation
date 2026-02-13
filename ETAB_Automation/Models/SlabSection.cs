// ============================================================================
// FILE: Models/SlabSection.cs
// ============================================================================
namespace ETAB_Automation.Models
{
    /// <summary>
    /// Represents a slab section with thickness and material grade
    /// </summary>
    public class SlabSection
    {
        public string SectionName { get; set; }      // e.g., "S160SM45"
        public int ThicknessMm { get; set; }         // Thickness in millimeters
        public string Grade { get; set; }            // Concrete grade (e.g., "M45")
        public double ThicknessMeters => ThicknessMm / 1000.0;

        public SlabSection()
        {
        }

        public SlabSection(string sectionName, int thicknessMm, string grade)
        {
            SectionName = sectionName;
            ThicknessMm = thicknessMm;
            Grade = grade;
        }

        public override string ToString()
        {
            return $"{SectionName} ({ThicknessMm}mm, {Grade})";
        }
    }
}