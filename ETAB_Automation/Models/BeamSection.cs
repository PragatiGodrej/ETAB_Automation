
// ============================================================================
// FILE: Models/BeamSection.cs
// ============================================================================
// PURPOSE: Data model for beam cross-sections with dimensions and properties
// AUTHOR: ETAB Automation Team
// VERSION: 2.1
// ============================================================================

namespace ETAB_Automation.Models
{
    /// <summary>
    /// Represents a beam section with dimensions and material grade
    /// Used to define beam cross-sections in ETABS
    /// </summary>
    public class BeamSection
    {
        /// <summary>
        /// Section name in ETABS format (e.g., "B20X75M35", "B24X600M40")
        /// Format: B{width}X{depth}M{grade}
        /// </summary>
        public string SectionName { get; set; }

        /// <summary>
        /// Beam width in millimeters
        /// </summary>
        public int WidthMm { get; set; }

        /// <summary>
        /// Beam depth in millimeters
        /// </summary>
        public int DepthMm { get; set; }

        /// <summary>
        /// Beam width in meters (calculated property)
        /// </summary>
        public double WidthMeters => WidthMm / 1000.0;

        /// <summary>
        /// Beam depth in meters (calculated property)
        /// </summary>
        public double DepthMeters => DepthMm / 1000.0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BeamSection()
        {
        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="sectionName">Section name (e.g., "B24X600M40")</param>
        /// <param name="widthMm">Width in millimeters</param>
        /// <param name="depthMm">Depth in millimeters</param>
        /// <param name="grade">Concrete grade (e.g., "M40")</param>
        public BeamSection(string sectionName, int widthMm, int depthMm, string grade)
        {
            SectionName = sectionName;
            WidthMm = widthMm;
            DepthMm = depthMm;
        }

        /// <summary>
        /// String representation of the beam section
        /// </summary>
        public override string ToString()
        {
            return $"{SectionName} ({WidthMm}x{DepthMm}mm)";
        }
    }
}

// ============================================================================
// END OF FILE
// ============================================================================
