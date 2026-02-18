// ============================================================================
// FILE: Core/StoryManager.cs — VERSION 2.4
// ============================================================================
// FIX: Accept foundationHeight as baseElev offset.
//      When foundationHeight > 0, all story base elevations are shifted up
//      by foundationHeight so that:
//        - Foundation walls span  0.0  → foundationHeight  (below stories)
//        - Basement1 walls span   foundationHeight → foundationHeight + B1height
//        - etc.
//      The ETABS SetStories_2 baseElev parameter is set to foundationHeight
//      so ETABS itself knows where the building starts.
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

        public StoryManager(cSapModel model)
        {
            sapModel = model;
            storyBaseElevations = new Dictionary<int, double>();
            storyTopElevations = new Dictionary<int, double>();
            storyNameToIndex = new Dictionary<string, int>();
            storyNames = new List<string>();
        }

        // ====================================================================
        // PRIMARY METHOD
        // foundationHeight → base offset for ALL stories.
        //   Pass 0.0 when there is no foundation (default behaviour unchanged).
        //   Pass e.g. 1.5 when there is a 1.5 m foundation raft/wall below
        //   the first defined story.
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
                        $"All story heights must be > 0 (Terrace is the only exception — it is always 0).");
            }

            sapModel.SetModelIsLocked(false);

            System.Diagnostics.Debug.WriteLine("\n========== UNIT SYSTEM CHECK ==========");
            System.Diagnostics.Debug.WriteLine("Setting units to: N_m_C");
            sapModel.SetPresentUnits(eUnits.N_m_C);
            System.Diagnostics.Debug.WriteLine($"Units after set: {sapModel.GetPresentUnits()}");
            System.Diagnostics.Debug.WriteLine("=========================================\n");

            int numStories = storyHeights.Count;

            this.storyNames = new List<string>(storyNames);
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

            // ----------------------------------------------------------------
            // KEY: cumulativeHeight starts at foundationHeight so that
            //      storyBaseElevations[0] == foundationHeight, not 0.
            //      Foundation walls (0 → foundationHeight) are handled
            //      separately in CADImporterEnhanced and do NOT occupy a story.
            // ----------------------------------------------------------------
            double cumulativeHeight = foundationHeight;

            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS ==========");
            if (foundationHeight > 0)
                System.Diagnostics.Debug.WriteLine(
                    $"Foundation offset: {foundationHeight:F3}m  " +
                    $"(stories start above this level)");

            for (int i = 0; i < numStories; i++)
            {
                bool isTerraceFloor = storyNames[i].Equals("Terrace", StringComparison.OrdinalIgnoreCase);

                names[i] = storyNames[i];
                // ETABS does not accept height = 0. For Terrace we pass a
                // nominal 0.001m so ETABS is satisfied, but our internal
                // base == top (height = 0) so no walls/beams/slabs are placed.
                elevs[i] = isTerraceFloor ? 0.001 : storyHeights[i];
                master[i] = true;
                similar[i] = "";
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);

                storyBaseElevations[i] = cumulativeHeight;
                storyTopElevations[i] = cumulativeHeight + storyHeights[i]; // 0 for Terrace → base == top
                storyNameToIndex[storyNames[i]] = i;

                System.Diagnostics.Debug.WriteLine(
                    $"Story {i}: {storyNames[i].PadRight(14)} | " +
                    $"Base: {storyBaseElevations[i]:F3}m | " +
                    $"Height: {storyHeights[i]:F3}m" +
                    (isTerraceFloor ? " (Terrace=0, ETABS gets 0.001m nominal)" : "") +
                    $" | Top: {storyTopElevations[i]:F3}m");

                cumulativeHeight += storyHeights[i]; // adds 0 for Terrace
            }

            System.Diagnostics.Debug.WriteLine(
                $"\nTotal Building Height (incl. foundation): {cumulativeHeight:F3}m");
            System.Diagnostics.Debug.WriteLine("======================================\n");

            // baseElev = foundationHeight tells ETABS where the bottom of the
            // first defined story sits.
            int ret = sapModel.Story.SetStories_2(
                foundationHeight,   // ← offset: foundation sits below this
                numStories,
                ref names, ref elevs,
                ref master, ref similar,
                ref splice, ref spliceHt, ref colors);

            if (ret != 0)
                throw new Exception($"ETABS SetStories_2 failed. Error code: {ret}");

            // Verify
            System.Diagnostics.Debug.WriteLine("\n========== VERIFYING ETABS STORED VALUES ==========");
            for (int i = 0; i < numStories; i++)
            {
                double storedElev = 0;
                if (sapModel.Story.GetElevation(names[i], ref storedElev) == 0)
                {
                    double expected = storyTopElevations[i];
                    double diff = Math.Abs(storedElev - expected);
                    string status = diff <= 0.001 ? "✓" : $"⚠ expected {expected:F3}m";
                    System.Diagnostics.Debug.WriteLine(
                        $"  {names[i].PadRight(14)}: ETABS elevation={storedElev:F3}m  {status}");
                }
            }
            System.Diagnostics.Debug.WriteLine("===================================================\n");

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        // ====================================================================
        // ELEVATION ACCESSORS
        // ====================================================================

        /// <summary>Base elevation of story (where its walls start).</summary>
        public double GetStoryBaseElevation(int storyIndex)
        {
            if (!storyBaseElevations.ContainsKey(storyIndex))
                throw new ArgumentException(
                    $"Story index {storyIndex} not found. Valid: 0-{storyBaseElevations.Count - 1}");
            return storyBaseElevations[storyIndex];
        }

        /// <summary>Top elevation of story (where next story begins).</summary>
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

        public double GetStoryElevation(int story, double storyHeight) => story * storyHeight;

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
    }
}
