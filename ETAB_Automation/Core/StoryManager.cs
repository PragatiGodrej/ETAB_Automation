

//// ============================================================================
//// FILE: Core/StoryManager.cs — VERSION 6.2
////
//// ELEVATION MODEL (definitive):
////
////   Height shift is now handled in MainForm BEFORE calling this method.
////   storyHeights[] passed in are already the correct ETABS heights.
////
////   This method simply stacks stories from 0 using storyHeights[] as-is.
////   geometry = ETABS span exactly for every story.
////
////   ONLY EXCEPTION — Basement with foundationHeight > 0:
////     geomBase = 0.0  (walls/foundation start at absolute base)

// ============================================================================
// FILE: Core/StoryManager.cs — VERSION 6.5
//
// ELEVATION MODEL (definitive):
//
//   Height shift is handled in MainForm BEFORE calling this method.
//   storyHeights[] passed in are already the correct ETABS heights.
//
//   BASEMENT STACKING (multi-basement fix v6.5):
//     With N basements the sequence is bottom→top: BasementN, ..., Basement1
//     Only the DEEPEST basement (first in the list, idx=0) starts at geomBase=0.
//     Each subsequent basement stacks on top of the previous one.
//
//     Example: foundationHeight=1.5, 3 basements of 3.5m each
//       idx=0 Basement3: geomBase=0.0       geomTop=0.0 +1.5+3.5= 5.0m
//       idx=1 Basement2: geomBase=5.0       geomTop=5.0      +3.5= 8.5m
//       idx=2 Basement1: geomBase=8.5       geomTop=8.5      +3.5=12.0m
//       idx=3 Ground:    geomBase=12.0      geomTop=12.0     +4.0=16.0m
//
//     CADImporter two-part wall treatment applies ONLY to idx=0 (deepest basement).
//     Basement2, Basement1 → normal wall treatment (geomBase→geomTop, no foundation split).
//
// CHANGES from v6.4:
//   - Basement geomBase/geomTop now stack correctly for multi-basement buildings.
//   - Introduced basementStackTop to track running top of the basement column.
//   - isDeepestBasement flag (i==0 && isBasement) controls foundation-zone geometry.
//   - Normal basements (not deepest) use basementStackTop as geomBase.
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
        private Dictionary<int, double> etabsStoryHeights;
        private Dictionary<int, double> planViewElevations;   // ETABS Plan View Z for each story
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
            planViewElevations = new Dictionary<int, double>();
            storyNameToIndex = new Dictionary<string, int>();
            storyNames = new List<string>();
            storyUserHeights = new List<double>();
        }

        // ====================================================================
        // PRIMARY METHOD
        //
        // storyHeights[] = already-shifted ETABS heights from MainForm
        // foundationHeight = needed only for basement geomTop calculation
        // ====================================================================
        public void DefineStoriesWithCustomNames(
            List<double> storyHeights,
            List<double> rawHeights,
            List<string> storyNames,
            double foundationHeight = 0.0)
        {
            if (storyHeights == null || storyNames == null || rawHeights == null)
                throw new ArgumentNullException("storyHeights, rawHeights and storyNames cannot be null");
            if (storyHeights.Count != storyNames.Count || storyHeights.Count != rawHeights.Count)
                throw new ArgumentException(
                    $"Count mismatch: heights={storyHeights.Count} raw={rawHeights.Count} names={storyNames.Count}");
            if (storyHeights.Count == 0)
                throw new ArgumentException("Cannot define zero stories");
            for (int i = 0; i < storyHeights.Count; i++)
                if (storyHeights[i] <= 0.0)
                    throw new ArgumentException($"Story '{storyNames[i]}' height must be > 0");

            FoundationHeight = foundationHeight;

            sapModel.SetModelIsLocked(false);
            sapModel.SetPresentUnits(eUnits.N_m_C);

            int numStories = storyHeights.Count;
            this.storyNames = new List<string>(storyNames);
            this.storyUserHeights = new List<double>(storyHeights);
            storyBaseElevations.Clear();
            storyTopElevations.Clear();
            etabsStoryHeights.Clear();
            planViewElevations.Clear();
            storyNameToIndex.Clear();

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            double etabsCumulative = 0.0;
            double rawCumulative = 0.0;          // all floors including basements
            double nonBasementRawCumul = 0.0;    // only non-basement floors above basements

            // basementStackTop: running top of the stacked basement column (m).
            // After all basements are processed this equals the physical top of
            // the shallowest basement slab — normal stories stack from here.
            double basementStackTop = 0.0;

            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS (v6.5) ==========");
            System.Diagnostics.Debug.WriteLine("ETABS Base: 0.000m");

            for (int i = 0; i < numStories; i++)
            {
                bool isBasement = storyNames[i].StartsWith("Basement",
                    StringComparison.OrdinalIgnoreCase);
                bool isDeepestBasement = (i == 0 && isBasement);

                names[i] = storyNames[i];
                master[i] = true;
                similar[i] = null;
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);
                storyNameToIndex[storyNames[i]] = i;

                double etabsHeight = storyHeights[i];
                double geomBase, geomTop;

                bool isFirstStoryNoBasement = (i == 0 && !isBasement && foundationHeight > 0);

                if (foundationHeight > 0 && isDeepestBasement)
                {
                    // ── Deepest basement only ────────────────────────────────
                    // Foundation zone: Z=0 → foundationHeight (Step A in CADImporter)
                    // Basement walls:  Z=foundationHeight → geomTop (Step B)
                    geomBase = 0.0;
                    geomTop = foundationHeight + rawHeights[i];
                    basementStackTop = geomTop;
                }
                else if (foundationHeight > 0 && isBasement)
                {
                    // ── Shallower basements ──────────────────────────────────
                    // Stack directly on top of previous basement.
                    // CADImporter treats these as normal-height walls (no foundation split).
                    geomBase = basementStackTop;
                    geomTop = basementStackTop + rawHeights[i];
                    basementStackTop = geomTop;
                }
                else if (isFirstStoryNoBasement)
                {
                    // ── No basements, but foundation exists ──────────────────
                    // First story gets the foundation-zone split treatment.
                    geomBase = 0.0;
                    geomTop = foundationHeight + rawHeights[i];
                    basementStackTop = geomTop;
                }
                else
                {
                    // ── Normal stories (Ground, Podium, EDeck, Typical, Terrace) ──
                    // physicalBase = top of basement stack + sum of non-basement
                    // raw heights already processed.
                    double physicalBase = basementStackTop + nonBasementRawCumul;
                    geomBase = physicalBase;
                    geomTop = physicalBase + rawHeights[i];
                    nonBasementRawCumul += rawHeights[i];
                }

                elevs[i] = etabsHeight;
                etabsStoryHeights[i] = etabsHeight;
                storyBaseElevations[i] = geomBase;
                storyTopElevations[i] = geomTop;
                etabsCumulative += etabsHeight;
                rawCumulative += rawHeights[i];
                planViewElevations[i] = etabsCumulative;

                System.Diagnostics.Debug.WriteLine(
                    $"Story {i}: {storyNames[i].PadRight(14)} | " +
                    $"ETABS h={etabsHeight:F3}m | " +
                    $"Plan View Z={etabsCumulative:F3}m | " +
                    $"Geom: {geomBase:F3}→{geomTop:F3}m  (slab@{etabsCumulative:F3}m)");
            }

            System.Diagnostics.Debug.WriteLine($"Total ETABS height: {etabsCumulative:F3}m");
            System.Diagnostics.Debug.WriteLine("=============================================\n");

            int ret = sapModel.Story.SetStories_2(
                0.0, numStories,
                ref names, ref elevs,
                ref master, ref similar,
                ref splice, ref spliceHt, ref colors);

            if (ret != 0)
                throw new Exception($"ETABS SetStories_2 failed. Error: {ret}");

            System.Diagnostics.Debug.WriteLine("\n========== ETABS VERIFICATION ==========");
            for (int i = 0; i < numStories; i++)
            {
                double storedElev = 0;
                if (sapModel.Story.GetElevation(names[i], ref storedElev) == 0)
                {
                    double expectedTop = storyTopElevations[i];
                    bool isBasV = names[i].StartsWith("Basement", StringComparison.OrdinalIgnoreCase);
                    if (isBasV && foundationHeight > 0)
                        expectedTop = etabsStoryHeights[i]; // ETABS top = foundationHeight
                    string ok = Math.Abs(storedElev - expectedTop) < 0.001 ? "✓" : "⚠";
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

        public double GetStoryBaseElevation(int storyIndex)
        {
            if (!storyBaseElevations.ContainsKey(storyIndex))
                throw new ArgumentException($"Story index {storyIndex} not found");
            return storyBaseElevations[storyIndex];
        }

        public double GetStoryTopElevation(int storyIndex)
        {
            if (!storyTopElevations.ContainsKey(storyIndex))
                throw new ArgumentException($"Story index {storyIndex} not found");
            return storyTopElevations[storyIndex];
        }

        public double GetStoryHeight(int storyIndex)
            => GetStoryTopElevation(storyIndex) - GetStoryBaseElevation(storyIndex);

        // Plan View Z = ETABS cumulative elevation = where the slab/beam should be placed
        public double GetStoryPlanViewZ(int storyIndex)
        {
            if (planViewElevations.ContainsKey(storyIndex)) return planViewElevations[storyIndex];
            throw new ArgumentException($"Story index {storyIndex} out of range");
        }

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
            if (name == "EDeck") return 65535;
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
            // No shift — rawHeights = storyHeights
            DefineStoriesWithCustomNames(storyHeights, storyHeights, names);
        }

        public void DefineStories(int numStories, double storyHeight)
        {
            var h = new List<double>(); var n = new List<string>();
            for (int i = 0; i < numStories; i++) { h.Add(storyHeight); n.Add($"Story{i + 1}"); }
            // No shift — rawHeights = storyHeights
            DefineStoriesWithCustomNames(h, h, n);
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
