
//// ============================================================================
//// FILE: Core/StoryManager.cs — VERSION 3.2
////
//// ELEVATION MODEL (definitive — no foundation zone concept):
////
////   SetStories_2 base = 0.0  → ETABS "Base" row = 0m  ✓
////
////   Stories stack from 0 using user-supplied heights exactly:
////     Basement1 : height=1.5  → base=0.0,  top=1.5
////     Podium1   : height=3.5  → base=1.5,  top=5.0
////     Ground    : height=4.0  → base=5.0,  top=9.0
////     ...
////
////   Walls:  placed at baseElevation, height = storyHeight  (always)
////   Slabs:  placed at baseElevation + 0.005                (always)
////
////   The "foundationHeight" parameter is accepted for backward compatibility
////   but is completely ignored — the caller must pass it as Basement1's
////   story height in storyHeights[0] instead.
////
//// CHANGES from v3.1:
////   - foundationHeight param accepted but ignored (no longer used anywhere).
////   - cumulativeHeight starts at 0.0 (unchanged from v3.1).
////   - SetStories_2 base = 0.0 (unchanged from v3.1).
////   - FoundationHeight public property removed.
////   - All foundation-zone special casing eliminated.
//// ============================================================================

//using ETABSv1;
//using System;
//using System.Collections.Generic;

//namespace ETAB_Automation.Core
//{
//    public class StoryManager
//    {
//        private readonly cSapModel sapModel;

//        private Dictionary<int, double> storyBaseElevations;
//        private Dictionary<int, double> storyTopElevations;
//        private Dictionary<string, int> storyNameToIndex;
//        private List<string> storyNames;
//        private List<double> storyUserHeights;

//        public StoryManager(cSapModel model)
//        {
//            sapModel = model;
//            storyBaseElevations = new Dictionary<int, double>();
//            storyTopElevations = new Dictionary<int, double>();
//            storyNameToIndex = new Dictionary<string, int>();
//            storyNames = new List<string>();
//            storyUserHeights = new List<double>();
//        }

//        // ====================================================================
//        // PRIMARY METHOD
//        // foundationHeight is accepted for API compatibility but ignored.
//        // The caller passes Basement1's actual height in storyHeights[0].
//        // ====================================================================
//        public void DefineStoriesWithCustomNames(
//            List<double> storyHeights,
//            List<string> storyNames,
//            double foundationHeight = 0.0)   // ignored — kept for API compat only
//        {
//            if (storyHeights == null || storyNames == null)
//                throw new ArgumentNullException("Story heights or names cannot be null");
//            if (storyHeights.Count != storyNames.Count)
//                throw new ArgumentException(
//                    $"Story heights ({storyHeights.Count}) and names ({storyNames.Count}) count mismatch");
//            if (storyHeights.Count == 0)
//                throw new ArgumentException("Cannot define zero stories");
//            for (int i = 0; i < storyHeights.Count; i++)
//                if (storyHeights[i] <= 0.0)
//                    throw new ArgumentException(
//                        $"Story '{storyNames[i]}' height must be > 0.");

//            sapModel.SetModelIsLocked(false);
//            System.Diagnostics.Debug.WriteLine("\n========== UNIT SYSTEM CHECK ==========");
//            sapModel.SetPresentUnits(eUnits.N_m_C);
//            System.Diagnostics.Debug.WriteLine($"Units: {sapModel.GetPresentUnits()}");
//            System.Diagnostics.Debug.WriteLine("=========================================\n");

//            int numStories = storyHeights.Count;
//            this.storyNames = new List<string>(storyNames);
//            this.storyUserHeights = new List<double>(storyHeights);
//            storyBaseElevations.Clear();
//            storyTopElevations.Clear();
//            storyNameToIndex.Clear();

//            string[] names = new string[numStories];
//            double[] elevs = new double[numStories];
//            bool[] master = new bool[numStories];
//            string[] similar = new string[numStories];
//            bool[] splice = new bool[numStories];
//            double[] spliceHt = new double[numStories];
//            int[] colors = new int[numStories];

//            double cumulativeHeight = 0.0;   // always starts at 0

//            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS ==========");
//            System.Diagnostics.Debug.WriteLine("ETABS Base: 0.000m");

//            for (int i = 0; i < numStories; i++)
//            {
//                names[i] = storyNames[i];
//                master[i] = true;
//                similar[i] = null;
//                splice[i] = false;
//                spliceHt[i] = 0.0;
//                colors[i] = AssignColorByStoryType(storyNames[i]);

//                storyBaseElevations[i] = cumulativeHeight;
//                storyNameToIndex[storyNames[i]] = i;
//                elevs[i] = storyHeights[i];
//                storyTopElevations[i] = cumulativeHeight + storyHeights[i];
//                cumulativeHeight += storyHeights[i];

//                System.Diagnostics.Debug.WriteLine(
//                    $"Story {i}: {storyNames[i].PadRight(14)} | " +
//                    $"Base={storyBaseElevations[i]:F3}m  " +
//                    $"Height={storyHeights[i]:F3}m  " +
//                    $"Top={storyTopElevations[i]:F3}m");
//            }

//            System.Diagnostics.Debug.WriteLine($"Top of building: {cumulativeHeight:F3}m");
//            System.Diagnostics.Debug.WriteLine("======================================\n");

//            int ret = sapModel.Story.SetStories_2(
//                0.0,            // Base always = 0 ✓
//                numStories,
//                ref names, ref elevs,
//                ref master, ref similar,
//                ref splice, ref spliceHt, ref colors);

//            if (ret != 0)
//                throw new Exception($"ETABS SetStories_2 failed. Error code: {ret}");

//            System.Diagnostics.Debug.WriteLine("\n========== VERIFYING ETABS ==========");
//            for (int i = 0; i < numStories; i++)
//            {
//                double storedElev = 0;
//                if (sapModel.Story.GetElevation(names[i], ref storedElev) == 0)
//                {
//                    string status = Math.Abs(storedElev - storyTopElevations[i]) < 0.001 ? "✓" : "⚠";
//                    System.Diagnostics.Debug.WriteLine(
//                        $"  {names[i].PadRight(14)}: top={storedElev:F3}m  {status}");
//                }
//            }
//            System.Diagnostics.Debug.WriteLine("=====================================\n");

//            VerifyStories();
//            sapModel.View.RefreshView(0, true);
//        }

//        // ====================================================================
//        // ACCESSORS
//        // ====================================================================

//        public double GetStoryBaseElevation(int storyIndex)
//        {
//            if (!storyBaseElevations.ContainsKey(storyIndex))
//                throw new ArgumentException($"Story index {storyIndex} not found.");
//            return storyBaseElevations[storyIndex];
//        }

//        public double GetStoryTopElevation(int storyIndex)
//        {
//            if (!storyTopElevations.ContainsKey(storyIndex))
//                throw new ArgumentException($"Story index {storyIndex} not found.");
//            return storyTopElevations[storyIndex];
//        }

//        public double GetStoryHeight(int storyIndex)
//            => GetStoryTopElevation(storyIndex) - GetStoryBaseElevation(storyIndex);

//        public string GetStoryNameByIndex(int storyIndex)
//        {
//            if (storyIndex >= 0 && storyIndex < storyNames.Count)
//                return storyNames[storyIndex];
//            throw new ArgumentException($"Story index {storyIndex} out of range");
//        }

//        public int GetStoryIndexByName(string storyName)
//        {
//            if (storyNameToIndex.ContainsKey(storyName))
//                return storyNameToIndex[storyName];
//            throw new ArgumentException($"Story name '{storyName}' not found");
//        }

//        public int GetStoryCount() => storyBaseElevations.Count;

//        public double GetTotalBuildingHeight()
//            => storyTopElevations.Count == 0 ? 0 : storyTopElevations[storyTopElevations.Count - 1];

//        // ====================================================================
//        // PRIVATE HELPERS
//        // ====================================================================

//        private int AssignColorByStoryType(string name)
//        {
//            if (name.StartsWith("Basement")) return 255;
//            if (name.StartsWith("Podium")) return 65280;
//            if (name == "Ground") return 16776960;
//            if (name == "EDeck") return 16776960;
//            if (name.StartsWith("Story")) return 16711680;
//            if (name.StartsWith("Refuge")) return 16744448;
//            if (name == "Terrace") return 16711935;
//            return -1;
//        }

//        private void VerifyStories()
//        {
//            int n = 0; string[] names = null;
//            sapModel.Story.GetNameList(ref n, ref names);
//            System.Diagnostics.Debug.WriteLine($"ETABS story count: {n}");
//            if (names != null)
//                foreach (string s in names)
//                {
//                    double elev = 0;
//                    sapModel.Story.GetElevation(s, ref elev);
//                    System.Diagnostics.Debug.WriteLine($"  {s}: top={elev:F3}m");
//                }
//        }

//        // ====================================================================
//        // LEGACY COMPAT
//        // ====================================================================

//        public void DefineStoriesWithVariableHeights(List<double> storyHeights)
//        {
//            var names = new List<string>();
//            for (int i = 0; i < storyHeights.Count; i++) names.Add($"Story{i + 1}");
//            DefineStoriesWithCustomNames(storyHeights, names);
//        }

//        public void DefineStories(int numStories, double storyHeight)
//        {
//            var heights = new List<double>();
//            var names = new List<string>();
//            for (int i = 0; i < numStories; i++) { heights.Add(storyHeight); names.Add($"Story{i + 1}"); }
//            DefineStoriesWithCustomNames(heights, names);
//        }

//        public string GetStoryName(int storyIndex)
//        {
//            if (storyIndex >= 0 && storyIndex < storyNames.Count) return storyNames[storyIndex];
//            return storyIndex == 0 ? "Base" : $"Story{storyIndex + 1}";
//        }

//        public double GetStoryElevation(int story, double storyHeight) => story * storyHeight;

//        public double GetStoryElevationVariable(List<double> storyHeights, int storyIndex)
//        {
//            if (storyBaseElevations.ContainsKey(storyIndex)) return GetStoryBaseElevation(storyIndex);
//            if (storyIndex == 0) return 0.0;
//            double elev = 0;
//            for (int i = 0; i < storyIndex && i < storyHeights.Count; i++) elev += storyHeights[i];
//            return elev;
//        }

//        public double GetETABSStoryElevation(string storyName)
//        {
//            double e = 0;
//            if (sapModel.Story.GetElevation(storyName, ref e) == 0) return e;
//            throw new Exception($"Could not retrieve ETABS elevation for '{storyName}'");
//        }
//    }
//}
// ============================================================================
// FILE: Core/StoryManager.cs — VERSION 5.0
//
// DEFINITIVE ELEVATION MODEL:
//
//   User inputs:
//     foundationHeight   = 1.5m  (base to slab level — not a story)
//     storyHeights[0]    = 3.5m  (Basement1 wall height above slab)
//     storyHeights[1]    = 4.5m  (Podium1)
//     ...
//
//   ETABS Story Table (SetStories_2 base = 0.0):
//     Base      = 0.0m
//     Basement1 = foundationHeight + storyHeights[0] = 1.5+3.5 = 5.0m  ← top
//     Podium1   = top = 5.0 + 4.5 = 9.5m
//     ...
//
//   Geometry:
//     Foundation walls:  Z=0.000 → 1.500  (height=foundationHeight, no story)
//                        Same CAD plan as Basement1, same wall sections
//     Basement1 walls:   Z=1.500 → 5.000  (height=storyHeights[0]=3.5)
//     Basement1 slab:    Z=1.505           (foundationHeight+0.005)
//     Podium1 walls:     Z=5.000 → 9.500
//     Podium1 slab:      Z=5.005
//
//   storyBaseElevations[0] = foundationHeight = 1.5  (geometry base for B1)
//   storyTopElevations[0]  = foundationHeight + storyHeights[0] = 5.0
//
//   ETABS story Basement1 spans 0 → 5.0 (height = 5.0 passed to SetStories_2)
//   Walls at 1.5→5.0 are inside the ETABS story span 0→5.0  ✓
//   Slab at 1.505 is inside the ETABS story span 0→5.0       ✓
//   Foundation walls at 0→1.5 are also inside span 0→5.0     ✓
//
//   SetStories_2 base = 0.0  → ETABS "Base" row = 0m          ✓
//
// CHANGES from v4.0:
//   - SetStories_2 base = 0.0 always  (Base row = 0)
//   - For Basement stories, elevs[i] = foundationHeight + storyHeights[i]
//     so ETABS story height covers full zone from 0 to top of basement walls
//   - storyBaseElevations[0] = foundationHeight (for geometry placement)
//   - cumulativeHeight starts at 0 for ETABS, but geometry offset tracked
//     separately per story
// ============================================================================

using ETABSv1;
using System;
using System.Collections.Generic;

namespace ETAB_Automation.Core
{
    public class StoryManager
    {
        private readonly cSapModel sapModel;

        private Dictionary<int, double> storyBaseElevations;  // geometry base (wall/slab Z)
        private Dictionary<int, double> storyTopElevations;   // geometry top
        private Dictionary<int, double> etabsStoryHeights;    // height passed to ETABS
        private Dictionary<string, int> storyNameToIndex;
        private List<string> storyNames;
        private List<double> storyUserHeights;

        public double FoundationHeight { get; private set; }

        public StoryManager(cSapModel model)
        {
            sapModel = model;
            storyBaseElevations = new Dictionary<int, double>();
            storyTopElevations = new Dictionary<int, double>();
            etabsStoryHeights = new Dictionary<int, double>();
            storyNameToIndex = new Dictionary<string, int>();
            storyNames = new List<string>();
            storyUserHeights = new List<double>();
        }

        // ====================================================================
        // PRIMARY METHOD
        //
        // storyHeights[0]  = Basement1 WALL height (above slab level) = 3.5m
        // foundationHeight = height from base to slab level = 1.5m
        // For Basement1: ETABS story height = foundationHeight + storyHeights[0]
        // For all other stories: ETABS story height = storyHeights[i]
        //
        // IsBasementIndex: detects basement stories by checking storyNames for
        // names starting with "Basement"
        // ====================================================================
        public void DefineStoriesWithCustomNames(
            List<double> storyHeights,
            List<string> storyNames,
            double foundationHeight = 0.0)
        {
            if (storyHeights == null || storyNames == null)
                throw new ArgumentNullException("storyHeights and storyNames cannot be null");
            if (storyHeights.Count != storyNames.Count)
                throw new ArgumentException(
                    $"Count mismatch: heights={storyHeights.Count} names={storyNames.Count}");
            if (storyHeights.Count == 0)
                throw new ArgumentException("Cannot define zero stories");
            for (int i = 0; i < storyHeights.Count; i++)
                if (storyHeights[i] <= 0.0)
                    throw new ArgumentException($"Story '{storyNames[i]}' height must be > 0");

            FoundationHeight = foundationHeight;

            sapModel.SetModelIsLocked(false);
            System.Diagnostics.Debug.WriteLine("\n========== UNIT CHECK ==========");
            sapModel.SetPresentUnits(eUnits.N_m_C);
            System.Diagnostics.Debug.WriteLine($"Units: {sapModel.GetPresentUnits()}");
            System.Diagnostics.Debug.WriteLine("================================\n");

            int numStories = storyHeights.Count;
            this.storyNames = new List<string>(storyNames);
            this.storyUserHeights = new List<double>(storyHeights);
            storyBaseElevations.Clear();
            storyTopElevations.Clear();
            etabsStoryHeights.Clear();
            storyNameToIndex.Clear();

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];   // heights sent to ETABS
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            // ETABS story tops stack from 0.
            // For geometry, basement base = foundationHeight.
            // For non-basement stories, base = previous story's ETABS top.
            double etabsCumulative = 0.0;

            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS ==========");
            System.Diagnostics.Debug.WriteLine($"ETABS Base       : 0.000m");
            if (foundationHeight > 0)
                System.Diagnostics.Debug.WriteLine(
                    $"Foundation zone  : 0.000 → {foundationHeight:F3}m  (walls only, no story)");

            for (int i = 0; i < numStories; i++)
            {
                bool isBasement = storyNames[i].StartsWith("Basement",
                    StringComparison.OrdinalIgnoreCase);

                names[i] = storyNames[i];
                master[i] = true;
                similar[i] = null;
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);
                storyNameToIndex[storyNames[i]] = i;

                if (isBasement && foundationHeight > 0)
                {
                    // ETABS story height = foundationHeight + wall height
                    // So ETABS story spans 0 → (foundationHeight + wallHeight)
                    // Geometry base = foundationHeight (walls start above foundation)
                    double etabsHeight = foundationHeight + storyHeights[i];
                    elevs[i] = etabsHeight;
                    etabsStoryHeights[i] = etabsHeight;
                    storyBaseElevations[i] = etabsCumulative + foundationHeight;
                    storyTopElevations[i] = etabsCumulative + etabsHeight;
                    etabsCumulative += etabsHeight;
                }
                else
                {
                    // Normal story: ETABS height = user height
                    // Geometry base = etabsCumulative (story base in ETABS = geometry base)
                    elevs[i] = storyHeights[i];
                    etabsStoryHeights[i] = storyHeights[i];
                    storyBaseElevations[i] = etabsCumulative;
                    storyTopElevations[i] = etabsCumulative + storyHeights[i];
                    etabsCumulative += storyHeights[i];
                }

                System.Diagnostics.Debug.WriteLine(
                    $"Story {i}: {storyNames[i].PadRight(14)} | " +
                    $"ETABS height={elevs[i]:F3}m | " +
                    $"Geom base={storyBaseElevations[i]:F3}m | " +
                    $"Geom top={storyTopElevations[i]:F3}m" +
                    (isBasement && foundationHeight > 0
                        ? $"  [basement: fdn={foundationHeight:F2}+walls={storyHeights[i]:F2}]"
                        : ""));
            }

            System.Diagnostics.Debug.WriteLine($"\nTotal ETABS height: {etabsCumulative:F3}m");
            System.Diagnostics.Debug.WriteLine("======================================\n");

            // SetStories_2 base = 0.0 → ETABS Base row = 0m ✓
            int ret = sapModel.Story.SetStories_2(
                0.0,
                numStories,
                ref names, ref elevs,
                ref master, ref similar,
                ref splice, ref spliceHt, ref colors);

            if (ret != 0)
                throw new Exception($"ETABS SetStories_2 failed. Error code: {ret}");

            System.Diagnostics.Debug.WriteLine("\n========== ETABS VERIFICATION ==========");
            for (int i = 0; i < numStories; i++)
            {
                double storedElev = 0;
                if (sapModel.Story.GetElevation(names[i], ref storedElev) == 0)
                {
                    string ok = Math.Abs(storedElev - storyTopElevations[i]) < 0.001 ? "✓" : "⚠";
                    System.Diagnostics.Debug.WriteLine(
                        $"  {names[i].PadRight(14)}: ETABS top={storedElev:F3}m  {ok}");
                }
            }
            System.Diagnostics.Debug.WriteLine("=========================================\n");

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        // ====================================================================
        // ACCESSORS
        // ====================================================================

        /// <summary>Absolute Z where this story's walls and slab begin.</summary>
        public double GetStoryBaseElevation(int storyIndex)
        {
            if (!storyBaseElevations.ContainsKey(storyIndex))
                throw new ArgumentException($"Story index {storyIndex} not found");
            return storyBaseElevations[storyIndex];
        }

        /// <summary>Absolute Z of the top of this story's walls.</summary>
        public double GetStoryTopElevation(int storyIndex)
        {
            if (!storyTopElevations.ContainsKey(storyIndex))
                throw new ArgumentException($"Story index {storyIndex} not found");
            return storyTopElevations[storyIndex];
        }

        /// <summary>User-supplied wall height for this story (NOT the ETABS story height for basements).</summary>
        public double GetStoryHeight(int storyIndex)
            => GetStoryTopElevation(storyIndex) - GetStoryBaseElevation(storyIndex);

        public string GetStoryNameByIndex(int storyIndex)
        {
            if (storyIndex >= 0 && storyIndex < storyNames.Count)
                return storyNames[storyIndex];
            throw new ArgumentException($"Story index {storyIndex} out of range");
        }

        public int GetStoryIndexByName(string storyName)
        {
            if (storyNameToIndex.ContainsKey(storyName))
                return storyNameToIndex[storyName];
            throw new ArgumentException($"Story '{storyName}' not found");
        }

        public int GetStoryCount() => storyBaseElevations.Count;

        public double GetTotalBuildingHeight()
            => storyTopElevations.Count == 0 ? 0 : storyTopElevations[storyTopElevations.Count - 1];

        // ====================================================================
        // PRIVATE HELPERS
        // ====================================================================

        private int AssignColorByStoryType(string name)
        {
            if (name.StartsWith("Basement")) return 255;
            if (name.StartsWith("Podium")) return 65280;
            if (name == "Ground") return 16776960;
            if (name == "EDeck") return 16776960;
            if (name.StartsWith("Story")) return 16711680;
            if (name.StartsWith("Refuge")) return 16744448;
            if (name == "Terrace") return 16711935;
            return -1;
        }

        private void VerifyStories()
        {
            int n = 0; string[] names = null;
            sapModel.Story.GetNameList(ref n, ref names);
            System.Diagnostics.Debug.WriteLine($"ETABS story count: {n}");
            if (names != null)
                foreach (string s in names)
                {
                    double elev = 0;
                    sapModel.Story.GetElevation(s, ref elev);
                    System.Diagnostics.Debug.WriteLine($"  {s}: ETABS top={elev:F3}m");
                }
        }

        // ====================================================================
        // LEGACY COMPAT
        // ====================================================================

        public void DefineStoriesWithVariableHeights(List<double> storyHeights)
        {
            var names = new List<string>();
            for (int i = 0; i < storyHeights.Count; i++) names.Add($"Story{i + 1}");
            DefineStoriesWithCustomNames(storyHeights, names);
        }

        public void DefineStories(int numStories, double storyHeight)
        {
            var h = new List<double>(); var n = new List<string>();
            for (int i = 0; i < numStories; i++) { h.Add(storyHeight); n.Add($"Story{i + 1}"); }
            DefineStoriesWithCustomNames(h, n);
        }

        public string GetStoryName(int idx)
        {
            if (idx >= 0 && idx < storyNames.Count) return storyNames[idx];
            return idx == 0 ? "Base" : $"Story{idx + 1}";
        }

        public double GetStoryElevation(int story, double storyHeight) => story * storyHeight;

        public double GetStoryElevationVariable(List<double> storyHeights, int idx)
        {
            if (storyBaseElevations.ContainsKey(idx)) return GetStoryBaseElevation(idx);
            if (idx == 0) return 0.0;
            double e = 0;
            for (int i = 0; i < idx && i < storyHeights.Count; i++) e += storyHeights[i];
            return e;
        }

        public double GetETABSStoryElevation(string storyName)
        {
            double e = 0;
            if (sapModel.Story.GetElevation(storyName, ref e) == 0) return e;
            throw new Exception($"Could not get ETABS elevation for '{storyName}'");
        }
    }
}
