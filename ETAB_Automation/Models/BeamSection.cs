// ============================================================================
// FILE: Models/BeamSection.cs
// ============================================================================
namespace ETABS_Automation.Models
{
    /// <summary>
    /// Represents a beam section with dimensions and material grade
    /// </summary>
    public class BeamSection
    {
        public string SectionName { get; set; }      // e.g., "B20X75M35"
        public int WidthMm { get; set; }             // Width in millimeters
        public int DepthMm { get; set; }             // Depth in millimeters
        //public string Grade { get; set; }            // Concrete grade (e.g., "M35")
        public double WidthMeters => WidthMm / 1000.0;
        public double DepthMeters => DepthMm / 1000.0;

        public BeamSection()
        {
        }

        public BeamSection(string sectionName, int widthMm, int depthMm, string grade)
        {
            SectionName = sectionName;
            WidthMm = widthMm;
            DepthMm = depthMm;
            //Grade = grade;
        }

        public override string ToString()
        {
            return $"{SectionName} ({WidthMm}x{DepthMm}mm";
        }
    }
}