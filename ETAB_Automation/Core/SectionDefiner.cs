// ============================================================================
// FILE: Core/SectionDefiner.cs — VERSION 1.0
//
// PURPOSE:
//   Centralised "define-if-missing" helper for all structural section types.
//   Each importer calls the appropriate method BEFORE assigning a section name
//   to an ETABS object.  If the exact required section already exists in the
//   model it is reused; if it is absent it is created via the ETABS API using
//   the correct material property and geometry.
//
// SECTION NAMING CONVENTIONS (must match importer regex patterns):
//   Walls  : W{thickness_cm}M{grade}     e.g. W30M60   (30 cm = 300 mm)
//   Slabs  : S{thickness_mm}SM{grade}    e.g. S150SM45
//   Beams  : B{w_cm}X{d_cm}M{grade}     e.g. B20X75M40   (gravity)
//            MB{w_cm}X{d_cm}M{grade}     e.g. MB30X75M40  (main)
//   Columns: C{B_mm}X{D_mm}M{grade}     e.g. C300X450M60
//
// ETABS API used:
//   Wall / Slab : sapModel.PropArea.SetShellLayer   (eWallPropertyType.Specified)
//   Beam        : sapModel.PropFrame.SetRectangle
//   Column      : sapModel.PropFrame.SetRectangle
//   Material    : sapModel.PropMaterial.GetNameList (read-only, no auto-create)
//
// MATERIAL RESOLUTION:
//   Searches ETABS PropMaterial list for a name containing the grade number
//   (e.g. "60" from "M60").  Falls back to grade string directly if not found.
//   ETABS accepts the material name as-is; if it does not exist the API call
//   will fail with a non-zero return — logged but not fatal.
//
// THREAD SAFETY:
//   definedSections is static (process-wide).  Fine for single-threaded import.
//   Call ResetCache() at the start of each import run to force re-validation.
// ============================================================================

using ETABSv1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ETAB_Automation.Core
{
    public static class SectionDefiner
    {
        // ── Process-wide cache: sections we have already verified / created ──
        private static readonly HashSet<string> definedSections =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Process-wide cache: material names loaded from ETABS ─────────────
        private static string[] cachedMaterialNames = null;

        /// <summary>
        /// Call once at the beginning of each import run to flush the cache.
        /// Ensures previously-defined sections in a different model/template
        /// are not falsely reported as present.
        /// </summary>
        public static void ResetCache()
        {
            definedSections.Clear();
            cachedMaterialNames = null;
            Debug.WriteLine("SectionDefiner: cache reset.");
        }

        // ====================================================================
        // MATERIAL RESOLUTION
        // ====================================================================

        /// <summary>
        /// Finds the ETABS material property whose name best matches the
        /// requested concrete grade (e.g. "M60", "M45").
        /// Priority:
        ///   1. Exact match           (e.g. "M60")
        ///   2. Contains match        (e.g. "Concrete M60")
        ///   3. Numeric part only     (e.g. "60" within "C60/75")
        ///   4. Grade string as-is    (last resort — ETABS may reject but we log it)
        /// </summary>
        public static string ResolveMaterial(cSapModel sapModel, string grade)
        {
            if (cachedMaterialNames == null)
            {
                int n = 0;
                string[] names = null;
                if (sapModel.PropMaterial.GetNameList(ref n, ref names) == 0 && names != null)
                    cachedMaterialNames = names;
                else
                    cachedMaterialNames = new string[0];

                Debug.WriteLine($"SectionDefiner: loaded {cachedMaterialNames.Length} materials " +
                    $"[{string.Join(", ", cachedMaterialNames.Take(8))}...]");
            }

            if (cachedMaterialNames.Length == 0)
            {
                Debug.WriteLine($"  ⚠ No materials in model — using grade '{grade}' directly.");
                return grade;
            }

            // 1. Exact
            string m = cachedMaterialNames.FirstOrDefault(
                n => string.Equals(n, grade, StringComparison.OrdinalIgnoreCase));
            if (m != null) return m;

            // 2. Contains (e.g. "Concrete M60")
            m = cachedMaterialNames.FirstOrDefault(
                n => n.IndexOf(grade, StringComparison.OrdinalIgnoreCase) >= 0);
            if (m != null) return m;

            // 3. Numeric part only (e.g. "60" from "M60")
            string num = Regex.Match(grade ?? "", @"\d+").Value;
            if (!string.IsNullOrEmpty(num))
            {
                m = cachedMaterialNames.FirstOrDefault(
                    n => n.IndexOf(num, StringComparison.OrdinalIgnoreCase) >= 0);
                if (m != null) return m;
            }

            Debug.WriteLine($"  ⚠ Material for grade '{grade}' not found — using grade string directly.");
            return grade;
        }

        // ====================================================================
        // WALL SECTIONS   (W{cm}M{grade}  e.g. W30M60)
        //
        // ETABS API: PropArea.SetWall
        //   (sectionName, eWallPropType.Specified, eShellType.ShellThin,
        //    matProp, thickness_m, color, notes, guid)
        //
        // Mirrors the signature of PropArea.GetWall used in WallThicknessCalculator.
        // ====================================================================

        /// <summary>
        /// Ensures a wall section named W{thicknessCm}M{grade} exists in the
        /// ETABS model.  Creates it if absent.  Returns the section name.
        /// </summary>
        public static string EnsureWallSection(cSapModel sapModel,
            int thicknessMm, string grade)
        {
            int thicknessCm = thicknessMm / 10;
            string gradeNum = (grade ?? "M30").Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"W{thicknessCm}M{gradeNum}";

            if (definedSections.Contains(sectionName))
                return sectionName;

            if (SectionExistsInArea(sapModel, sectionName))
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✓ Wall section '{sectionName}' found in template.");
                return sectionName;
            }

            string matProp = ResolveMaterial(sapModel, $"M{gradeNum}");
            double thicknessM = thicknessMm / 1000.0;

            int ret = sapModel.PropArea.SetWall(
                sectionName,
                eWallPropType.Specified,
                eShellType.ShellThin,
                matProp,
                thicknessM,
                -1,     // color: -1 = default
                "",     // notes
                "");    // GUID (auto-assign)

            if (ret == 0)
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✅ Defined wall section '{sectionName}' " +
                    $"({thicknessMm}mm, mat={matProp})");
            }
            else
            {
                Debug.WriteLine($"  ❌ PropArea.SetWall failed (ret={ret}) " +
                    $"for '{sectionName}' — check material '{matProp}' exists.");
            }

            return sectionName;
        }

        // ====================================================================
        // SLAB SECTIONS   (S{mm}SM{grade}  e.g. S150SM45)
        //
        // ETABS API: PropArea.SetSlab
        //   (sectionName, eSlabType.Slab, eShellType.ShellThin,
        //    matProp, thickness_m, color, notes, guid)
        //
        // Matches the SetSlab call used in SlabImporter.DefineFallbackSections.
        // ====================================================================

        /// <summary>
        /// Ensures a slab section named S{thicknessMm}SM{grade} exists.
        /// Creates it if absent.  Returns the section name.
        /// </summary>
        public static string EnsureSlabSection(cSapModel sapModel,
            int thicknessMm, string grade)
        {
            string gradeNum = (grade ?? "M30").Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"S{thicknessMm}SM{gradeNum}";

            if (definedSections.Contains(sectionName))
                return sectionName;

            if (SectionExistsInArea(sapModel, sectionName))
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✓ Slab section '{sectionName}' found in template.");
                return sectionName;
            }

            string matProp = ResolveMaterial(sapModel, $"M{gradeNum}");
            double thicknessM = thicknessMm / 1000.0;

            // Signature: SetSlab(name, slabType, shellType, matProp, thickness,
            //                    color, notes, GUID)  — matches SlabImporter usage
            int ret = sapModel.PropArea.SetSlab(
                sectionName,
                eSlabType.Slab,
                eShellType.ShellThin,
                matProp,
                thicknessM,
                -1,     // color: -1 = default
                "",     // notes
                "");    // GUID (auto-assign)

            if (ret == 0)
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✅ Defined slab section '{sectionName}' " +
                    $"({thicknessMm}mm, mat={matProp})");
            }
            else
            {
                Debug.WriteLine($"  ❌ PropArea.SetSlab failed (ret={ret}) " +
                    $"for '{sectionName}' — check material '{matProp}' exists.");
            }

            return sectionName;
        }

        // ====================================================================
        // BEAM SECTIONS   (B{w_cm}X{d_cm}M{grade}  /  MB{w_cm}X{d_cm}M{grade})
        //
        // ETABS API: PropFrame.SetRectangle
        //   sectionName, matProp, t3 (depth), t2 (width)
        //   t3 = D (depth in direction 3 = major axis)
        //   t2 = B (width in direction 2 = minor axis)
        // ====================================================================

        /// <summary>
        /// Ensures a gravity beam section named B{wCm}X{dCm}M{grade} exists.
        /// Creates it if absent.  Returns the section name.
        /// </summary>
        public static string EnsureGravityBeamSection(cSapModel sapModel,
            int widthMm, int depthMm, string grade)
        {
            int wCm = widthMm / 10;
            int dCm = depthMm / 10;
            string gradeNum = (grade ?? "M30").Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"B{wCm}X{dCm}M{gradeNum}";
            return EnsureFrameSection(sapModel, sectionName, widthMm, depthMm, $"M{gradeNum}");
        }

        /// <summary>
        /// Ensures a main beam section named MB{wCm}X{dCm}M{grade} exists.
        /// Creates it if absent.  Returns the section name.
        /// </summary>
        public static string EnsureMainBeamSection(cSapModel sapModel,
            int widthMm, int depthMm, string grade)
        {
            int wCm = widthMm / 10;
            int dCm = depthMm / 10;
            string gradeNum = (grade ?? "M30").Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"MB{wCm}X{dCm}M{gradeNum}";
            return EnsureFrameSection(sapModel, sectionName, widthMm, depthMm, $"M{gradeNum}");
        }

        // ====================================================================
        // COLUMN SECTIONS   (C{B_mm}X{D_mm}M{grade}  e.g. C300X450M60)
        // ====================================================================

        /// <summary>
        /// Ensures a column section named C{B_mm}X{D_mm}M{grade} exists.
        /// Creates it if absent.  Returns the section name.
        /// </summary>
        public static string EnsureColumnSection(cSapModel sapModel,
            int widthMm, int depthMm, string grade)
        {
            string gradeNum = (grade ?? "M30").Replace("M", "").Replace("m", "").Trim();
            string sectionName = $"C{widthMm}X{depthMm}M{gradeNum}";
            return EnsureFrameSection(sapModel, sectionName, widthMm, depthMm, $"M{gradeNum}");
        }

        // ====================================================================
        // PRIVATE HELPERS
        // ====================================================================

        private static string EnsureFrameSection(cSapModel sapModel,
            string sectionName, int widthMm, int depthMm, string grade)
        {
            if (definedSections.Contains(sectionName))
                return sectionName;

            if (SectionExistsInFrame(sapModel, sectionName))
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✓ Frame section '{sectionName}' found in template.");
                return sectionName;
            }

            string matProp = ResolveMaterial(sapModel, grade);
            double widthM = widthMm / 1000.0;
            double depthM = depthMm / 1000.0;

            // t3 = depth (D, major axis), t2 = width (B, minor axis)
            int ret = sapModel.PropFrame.SetRectangle(
                sectionName, matProp, depthM, widthM);

            if (ret == 0)
            {
                definedSections.Add(sectionName);
                Debug.WriteLine($"  ✅ Defined frame section '{sectionName}' " +
                    $"({widthMm}×{depthMm}mm, mat={matProp})");
            }
            else
            {
                Debug.WriteLine($"  ❌ PropFrame.SetRectangle failed (ret={ret}) " +
                    $"for '{sectionName}' — check material '{matProp}' exists.");
            }

            return sectionName;
        }

        private static bool SectionExistsInArea(cSapModel sapModel, string name)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.PropArea.GetNameList(ref n, ref names) == 0 && names != null)
                    return names.Any(nm =>
                        string.Equals(nm, name, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
            return false;
        }

        private static bool SectionExistsInFrame(cSapModel sapModel, string name)
        {
            try
            {
                int n = 0; string[] names = null;
                if (sapModel.PropFrame.GetNameList(ref n, ref names) == 0 && names != null)
                    return names.Any(nm =>
                        string.Equals(nm, name, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
            return false;
        }
    }
}
