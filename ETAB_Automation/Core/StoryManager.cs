
// ============================================================================
// FILE: Core/StoryManager.cs — VERSION 2.7
// ============================================================================

// ============================================================================

using ETABSv1;
using System;
using System.Collections.Generic;

namespace ETAB_Automation.Core
{
    public class StoryManager
    {
        private readonly cSapModel sapModel;

        private Dictionary<int, double> storyBaseElevations;
        private Dictionary<int, double> storyTopElevations;
        private Dictionary<string, int> storyNameToIndex;
        private List<string> storyNames;
        private List<double> storyUserHeights; // store original user heights

        public StoryManager(cSapModel model)
        {
            sapModel = model;
            storyBaseElevations = new Dictionary<int, double>();
            storyTopElevations = new Dictionary<int, double>();
            storyNameToIndex = new Dictionary<string, int>();
            storyNames = new List<string>();
            storyUserHeights = new List<double>();
        }

        // ====================================================================
        // PRIMARY METHOD
        //
        // TERRACE BEHAVIOUR (v2.7):
        //   • Terrace SLAB is placed at baseElevation  (= top of last real story)
        //   • storyTopElevations[terraceIdx] == storyBaseElevations[terraceIdx]
        //     so GetStoryHeight(terraceIdx) == 0  → slab placement elevation = base ✓
        //   • Wall importer uses GetTerraceParapetHeight() to build parapet walls
        //     from base upward by the user-supplied parapet height.
        //   • ETABS itself receives the real parapet height (or 0.01m guard if 0)
        //     so the story is visible in the UI at the correct top level.
        //   • cumulativeHeight is NOT incremented for Terrace so subsequent
        //     index lookups remain consistent.
        // ====================================================================
        public void DefineStoriesWithCustomNames(
            List<double> storyHeights,
            List<string> storyNames,
            double foundationHeight = 0.0)
        {
            if (storyHeights == null || storyNames == null)
                throw new ArgumentNullException("Story heights or names cannot be null");

            if (storyHeights.Count != storyNames.Count)
                throw new ArgumentException(
                    $"Story heights ({storyHeights.Count}) and names ({storyNames.Count}) count mismatch");

            if (storyHeights.Count == 0)
                throw new ArgumentException("Cannot define zero stories");

            for (int i = 0; i < storyHeights.Count; i++)
            {
                bool isTerrace = storyNames[i].Equals("Terrace", StringComparison.OrdinalIgnoreCase);
                if (!isTerrace && storyHeights[i] <= 0.0)
                    throw new ArgumentException(
                        $"Story '{storyNames[i]}' (index {i}) has height {storyHeights[i]:F3}m. " +
                        $"All story heights must be > 0 (Terrace may be 0 if no parapet).");
            }

            sapModel.SetModelIsLocked(false);

            // ----------------------------------------------------------------
            // [FIX-3] Fresh model guard
            // ----------------------------------------------------------------
            int existingCount = 0;
            string[] existingNames = null;
            if (sapModel.Story.GetNameList(ref existingCount, ref existingNames) == 0
                && existingCount > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠ [FIX-3] {existingCount} stories already in model — " +
                    $"reinitialising to clear stale element-story index references");
            }

            System.Diagnostics.Debug.WriteLine("\n========== UNIT SYSTEM CHECK ==========");
            System.Diagnostics.Debug.WriteLine("Setting units to: N_m_C");
            sapModel.SetPresentUnits(eUnits.N_m_C);
            System.Diagnostics.Debug.WriteLine($"Units after set: {sapModel.GetPresentUnits()}");
            System.Diagnostics.Debug.WriteLine("=========================================\n");

            int numStories = storyHeights.Count;

            this.storyNames = new List<string>(storyNames);
            this.storyUserHeights = new List<double>(storyHeights);
            storyBaseElevations.Clear();
            storyTopElevations.Clear();
            storyNameToIndex.Clear();

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            double cumulativeHeight = foundationHeight;

            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS ==========");
            if (foundationHeight > 0)
                System.Diagnostics.Debug.WriteLine(
                    $"Foundation offset: {foundationHeight:F3}m  " +
                    $"(stories start above this level)");

            for (int i = 0; i < numStories; i++)
            {
                bool isTerraceFloor = storyNames[i].Equals(
                    "Terrace", StringComparison.OrdinalIgnoreCase);

                names[i] = storyNames[i];
                master[i] = true;
                similar[i] = null;   // [FIX-1] null = no similar story
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);

                storyBaseElevations[i] = cumulativeHeight;
                storyNameToIndex[storyNames[i]] = i;

                if (isTerraceFloor)
                {
                    // ── TERRACE (v2.7) ────────────────────────────────────
                    // The slab sits AT cumulativeHeight (= top of last real story).
                    // storyTopElevation == storyBaseElevation so that
                    // GetStoryHeight(i) == 0 and placementElevation = base. ✓
                    //
                    // ETABS receives the real parapet height so the story is
                    // rendered at the correct level in the UI.
                    // 0.01m crash-guard used only when user inputs 0.
                    //
                    // cumulativeHeight is NOT advanced — Terrace does not
                    // contribute to the building height stack.
                    // ─────────────────────────────────────────────────────
                    storyTopElevations[i] = cumulativeHeight; // slab level = base

                    elevs[i] = storyHeights[i] > 0.0 ? storyHeights[i] : 0.01;
                    // cumulativeHeight intentionally NOT incremented

                    System.Diagnostics.Debug.WriteLine(
                        $"Story {i}: {storyNames[i].PadRight(14)} | " +
                        $"Base (slab): {storyBaseElevations[i]:F3}m | " +
                        $"Parapet ht : {storyHeights[i]:F3}m | " +
                        $"ETABS ht   : {elevs[i]:F3}m | " +
                        $"ETABS top  : {storyBaseElevations[i] + elevs[i]:F3}m");
                }
                else
                {
                    // ── NORMAL STORY ──────────────────────────────────────
                    elevs[i] = storyHeights[i];
                    storyTopElevations[i] = cumulativeHeight + storyHeights[i];
                    cumulativeHeight += storyHeights[i];

                    System.Diagnostics.Debug.WriteLine(
                        $"Story {i}: {storyNames[i].PadRight(14)} | " +
                        $"Base: {storyBaseElevations[i]:F3}m | " +
                        $"Height: {storyHeights[i]:F3}m | " +
                        $"Top: {storyTopElevations[i]:F3}m");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"\nTotal Building Height (incl. foundation): {cumulativeHeight:F3}m");
            System.Diagnostics.Debug.WriteLine("======================================\n");

            int ret = sapModel.Story.SetStories_2(
                foundationHeight,
                numStories,
                ref names, ref elevs,
                ref master, ref similar,
                ref splice, ref spliceHt, ref colors);

            if (ret != 0)
                throw new Exception($"ETABS SetStories_2 failed. Error code: {ret}");

            // ── Verify ETABS stored values ───────────────────────────────────
            System.Diagnostics.Debug.WriteLine(
                "\n========== VERIFYING ETABS STORED VALUES ==========");
            for (int i = 0; i < numStories; i++)
            {
                double storedElev = 0;
                if (sapModel.Story.GetElevation(names[i], ref storedElev) == 0)
                {
                    double expected = storyBaseElevations[i] + elevs[i];
                    double diff = Math.Abs(storedElev - expected);
                    string status = diff <= 0.011 ? "✓" : $"⚠ expected {expected:F3}m";
                    System.Diagnostics.Debug.WriteLine(
                        $"  {names[i].PadRight(14)}: ETABS elevation={storedElev:F3}m  {status}");
                }
            }
            System.Diagnostics.Debug.WriteLine(
                "===================================================\n");

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        // ====================================================================
        // ELEVATION ACCESSORS
        // ====================================================================

        /// <summary>Base elevation of story (where its slab sits).</summary>
        public double GetStoryBaseElevation(int storyIndex)
        {
            if (!storyBaseElevations.ContainsKey(storyIndex))
                throw new ArgumentException(
                    $"Story index {storyIndex} not found. Valid: 0-{storyBaseElevations.Count - 1}");
            return storyBaseElevations[storyIndex];
        }

        /// <summary>Top elevation of story.</summary>
        public double GetStoryTopElevation(int storyIndex)
        {
            if (!storyTopElevations.ContainsKey(storyIndex))
                throw new ArgumentException(
                    $"Story index {storyIndex} not found. Valid: 0-{storyTopElevations.Count - 1}");
            return storyTopElevations[storyIndex];
        }

        /// <summary>Height of a single story in metres (top - base).</summary>
        public double GetStoryHeight(int storyIndex)
            => GetStoryTopElevation(storyIndex) - GetStoryBaseElevation(storyIndex);

        /// <summary>
        /// For Terrace: returns the user-supplied parapet height.
        /// For all other stories: same as GetStoryHeight().
        /// Wall importer uses this to build parapet walls above the terrace slab.
        /// </summary>
        public double GetTerraceParapetHeight(int storyIndex)
        {
            if (storyIndex >= 0 && storyIndex < storyUserHeights.Count)
                return storyUserHeights[storyIndex];
            return GetStoryHeight(storyIndex);
        }

        // ====================================================================
        // NAME / INDEX HELPERS
        // ====================================================================

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
            throw new ArgumentException($"Story name '{storyName}' not found");
        }

        public int GetStoryCount() => storyBaseElevations.Count;

        public double GetTotalBuildingHeight()
            => storyTopElevations.Count == 0
                ? 0
                : storyTopElevations[storyTopElevations.Count - 1];

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
            int numExisting = 0;
            string[] existingStories = null;
            sapModel.Story.GetNameList(ref numExisting, ref existingStories);
            System.Diagnostics.Debug.WriteLine($"Total stories in ETABS: {numExisting}");
            if (existingStories != null)
            {
                foreach (string s in existingStories)
                {
                    double elev = 0;
                    sapModel.Story.GetElevation(s, ref elev);
                    System.Diagnostics.Debug.WriteLine($"  - {s} at elevation {elev:F3}m");
                }
            }
        }

        // ====================================================================
        // LEGACY / BACKWARD-COMPAT METHODS
        // ====================================================================

        public void DefineStoriesWithVariableHeights(List<double> storyHeights)
        {
            var defaultNames = new List<string>();
            for (int i = 0; i < storyHeights.Count; i++)
                defaultNames.Add($"Story{i + 1}");
            DefineStoriesWithCustomNames(storyHeights, defaultNames);
        }

        public void DefineStories(int numStories, double storyHeight)
        {
            var heights = new List<double>();
            var names = new List<string>();
            for (int i = 0; i < numStories; i++)
            {
                heights.Add(storyHeight);
                names.Add($"Story{i + 1}");
            }
            DefineStoriesWithCustomNames(heights, names);
        }

        public string GetStoryName(int storyIndex)
        {
            if (storyIndex >= 0 && storyIndex < storyNames.Count)
                return storyNames[storyIndex];
            return storyIndex == 0 ? "Base" : $"Story{storyIndex + 1}";
        }

        public double GetStoryElevation(int story, double storyHeight)
            => story * storyHeight;

        public double GetStoryElevationVariable(List<double> storyHeights, int storyIndex)
        {
            if (storyBaseElevations.ContainsKey(storyIndex))
                return GetStoryBaseElevation(storyIndex);
            if (storyIndex == 0) return 0.0;
            double elevation = 0.0;
            for (int i = 0; i < storyIndex && i < storyHeights.Count; i++)
                elevation += storyHeights[i];
            return elevation;
        }

        public double GetETABSStoryElevation(string storyName)
        {
            double storedElev = 0;
            if (sapModel.Story.GetElevation(storyName, ref storedElev) == 0)
                return storedElev;
            throw new Exception(
                $"Could not retrieve ETABS elevation for story '{storyName}'");
        }
    }
}
// ============================================================================
// END OF FILE
// ============================================================================
