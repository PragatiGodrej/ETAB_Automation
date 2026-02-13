
// ============================================================================
// FILE: Core/StoryManager.cs (CORRECTED - WITH EXPLICIT UNIT SYSTEM)
// ============================================================================
using ETABSv1;
using System;
using System.Collections.Generic;

namespace ETAB_Automation.Core
{
    public class StoryManager
    {
        private readonly cSapModel sapModel;

        // NEW: Store calculated elevations for each story
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

        public void DefineStoriesWithCustomNames(List<double> storyHeights, List<string> storyNames)
        {

            // Validate inputs first
            if (storyHeights == null || storyNames == null)
                throw new ArgumentNullException("Story heights or names cannot be null");

            if (storyHeights.Count != storyNames.Count)
                throw new ArgumentException($"Story heights ({storyHeights.Count}) and names ({storyNames.Count}) count mismatch");

            if (storyHeights.Count == 0)
                throw new ArgumentException("Cannot define zero stories");
            sapModel.SetModelIsLocked(false);

            // ============================================================
            // CRITICAL FIX: Set ETABS to use METERS explicitly
            // This ensures story heights are interpreted correctly
            // ============================================================
            eUnits currentUnits = sapModel.GetPresentUnits();

            System.Diagnostics.Debug.WriteLine("\n========== UNIT SYSTEM CHECK ==========");
            System.Diagnostics.Debug.WriteLine($"ETABS current units: {currentUnits}");
            System.Diagnostics.Debug.WriteLine("Setting units to: N_m_C (Newton, meter, Celsius)");

            // Force ETABS to use meters for all operations
            sapModel.SetPresentUnits(eUnits.N_m_C);

            // Verify the change
            currentUnits = sapModel.GetPresentUnits();
            System.Diagnostics.Debug.WriteLine($"ETABS units after setting: {currentUnits}");
            System.Diagnostics.Debug.WriteLine("=========================================\n");

            int numStories = storyHeights.Count;

            if (storyHeights.Count != storyNames.Count)
            {
                throw new ArgumentException("Story heights and names count mismatch");
            }

            // NEW: Clear and calculate elevations
            this.storyNames = new List<string>(storyNames);
            storyBaseElevations.Clear();
            storyTopElevations.Clear();
            storyNameToIndex.Clear();

            double baseElev = 0.0;

            string[] names = new string[numStories];
            double[] elevs = new double[numStories];
            bool[] master = new bool[numStories];
            string[] similar = new string[numStories];
            bool[] splice = new bool[numStories];
            double[] spliceHt = new double[numStories];
            int[] colors = new int[numStories];

            double cumulativeHeight = 0.0;

            System.Diagnostics.Debug.WriteLine("\n========== STORY ELEVATIONS (StoryManager) ==========");

            for (int i = 0; i < numStories; i++)
            {
                names[i] = storyNames[i];
                elevs[i] = storyHeights[i];  // Story HEIGHT in METERS (not elevation)
                master[i] = (i == 0);
                similar[i] = (i == 0) ? "" : storyNames[0];
                splice[i] = false;
                spliceHt[i] = 0.0;
                colors[i] = AssignColorByStoryType(storyNames[i]);

                // NEW: Store elevations (BASE and TOP)
                storyBaseElevations[i] = cumulativeHeight;
                storyTopElevations[i] = cumulativeHeight + storyHeights[i];
                storyNameToIndex[storyNames[i]] = i;

                System.Diagnostics.Debug.WriteLine(
                    $"Story {i}: {storyNames[i]} | " +
                    $"Base: {storyBaseElevations[i]:F3}m | " +
                    $"Height: {storyHeights[i]:F3}m | " +
                    $"Top: {storyTopElevations[i]:F3}m | " +
                    $"Passing to ETABS: {elevs[i]:F3}m");

                cumulativeHeight += storyHeights[i];
            }

            System.Diagnostics.Debug.WriteLine($"Total Building Height: {cumulativeHeight:F3}m");
            System.Diagnostics.Debug.WriteLine("====================================================\n");

            // Send story data to ETABS (now in meters because we set units above)
            int ret = sapModel.Story.SetStories_2(
                baseElev, numStories, ref names, ref elevs,
                ref master, ref similar, ref splice, ref spliceHt, ref colors
            );
            //// Ensure arrays are properly bounded
            //if (names.Length != numStories || elevs.Length != numStories)
            //{
            //    throw new Exception($"Array size mismatch: names={names.Length}, elevs={elevs.Length}, expected={numStories}");
            //}

            //// Send story data to ETABS
            //int ret = sapModel.Story.SetStories_2(
            //    baseElev, numStories, ref names, ref elevs,
            //    ref master, ref similar, ref splice, ref spliceHt, ref colors

            //);

            //System.Diagnostics.Debug.WriteLine("\n========== ARRAY SIZE CHECK ==========");
            //System.Diagnostics.Debug.WriteLine($"numStories: {numStories}");
            //System.Diagnostics.Debug.WriteLine($"names.Length: {names.Length}");
            //System.Diagnostics.Debug.WriteLine($"elevs.Length: {elevs.Length}");
            //System.Diagnostics.Debug.WriteLine($"master.Length: {master.Length}");
            //System.Diagnostics.Debug.WriteLine($"similar.Length: {similar.Length}");
            //System.Diagnostics.Debug.WriteLine($"splice.Length: {splice.Length}");
            //System.Diagnostics.Debug.WriteLine($"spliceHt.Length: {spliceHt.Length}");
            //System.Diagnostics.Debug.WriteLine($"colors.Length: {colors.Length}");
            //System.Diagnostics.Debug.WriteLine("======================================\n");
            //if (ret != 0)
            //{
            //    throw new Exception($"Failed to define stories. Error code: {ret}");
            //}

            // ============================================================
            // VERIFICATION: Read back what ETABS actually stored
            // ============================================================
            System.Diagnostics.Debug.WriteLine("\n========== VERIFYING ETABS STORED VALUES ==========");
            for (int i = 0; i < numStories; i++)
            {
                double storedElev = 0;
                int retGet = sapModel.Story.GetElevation(names[i], ref storedElev);
                if (retGet == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Story '{names[i]}': " +
                        $"Sent height={elevs[i]:F3}m, " +
                        $"ETABS stored elevation={storedElev:F3}m");

                    // Check for discrepancy
                    double expectedElevation = storyTopElevations[i];
                    double difference = Math.Abs(storedElev - expectedElevation);

                    if (difference > 0.001)  // 1mm tolerance
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"  ⚠️ WARNING: Expected elevation {expectedElevation:F3}m, " +
                            $"but ETABS has {storedElev:F3}m (difference: {difference:F3}m)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✓ Elevation verified correct");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("===================================================\n");

            VerifyStories();
            sapModel.View.RefreshView(0, true);
        }

        // NEW: Get BASE elevation of a story (where walls start)
        // Returns elevation in METERS - independent of coordinate conversion
        public double GetStoryBaseElevation(int storyIndex)
        {
            if (!storyBaseElevations.ContainsKey(storyIndex))
            {
                throw new ArgumentException(
                    $"Story index {storyIndex} not found. Valid range: 0-{storyBaseElevations.Count - 1}");
            }
            return storyBaseElevations[storyIndex];
        }

        // NEW: Get TOP elevation of a story (where beams/slabs are)
        // Returns elevation in METERS - independent of coordinate conversion
        public double GetStoryTopElevation(int storyIndex)
        {
            if (!storyTopElevations.ContainsKey(storyIndex))
            {
                throw new ArgumentException(
                    $"Story index {storyIndex} not found. Valid range: 0-{storyTopElevations.Count - 1}");
            }
            return storyTopElevations[storyIndex];
        }

        // NEW: Get story name by index
        public string GetStoryNameByIndex(int storyIndex)
        {
            if (storyIndex >= 0 && storyIndex < storyNames.Count)
            {
                return storyNames[storyIndex];
            }
            throw new ArgumentException($"Story index {storyIndex} out of range");
        }

        // NEW: Get story index by name
        public int GetStoryIndexByName(string storyName)
        {
            if (storyNameToIndex.ContainsKey(storyName))
            {
                return storyNameToIndex[storyName];
            }
            throw new ArgumentException($"Story name '{storyName}' not found");
        }

        // NEW: Get total number of stories
        public int GetStoryCount()
        {
            return storyBaseElevations.Count;
        }

        // NEW: Get total building height in METERS
        public double GetTotalBuildingHeight()
        {
            if (storyTopElevations.Count == 0)
                return 0;

            int lastStoryIndex = storyTopElevations.Count - 1;
            return storyTopElevations[lastStoryIndex];
        }

        private int AssignColorByStoryType(string storyName)
        {
            if (storyName.StartsWith("Basement"))
                return 255;
            else if (storyName.StartsWith("Podium"))
                return 65280;
            else if (storyName == "EDeck")
                return 16776960;
            else if (storyName.StartsWith("Story"))
                return 16711680;
            else
                return -1;
        }

        public void DefineStoriesWithVariableHeights(List<double> storyHeights)
        {
            sapModel.SetModelIsLocked(false);

            // Generate default story names
            List<string> defaultNames = new List<string>();
            for (int i = 0; i < storyHeights.Count; i++)
            {
                defaultNames.Add($"Story{i + 1}");
            }

            // Use the main method with elevation tracking
            DefineStoriesWithCustomNames(storyHeights, defaultNames);
        }

        public void DefineStories(int numStories, double storyHeight)
        {
            sapModel.SetModelIsLocked(false);

            // Generate heights and names
            List<double> heights = new List<double>();
            List<string> names = new List<string>();

            for (int i = 0; i < numStories; i++)
            {
                heights.Add(storyHeight);
                names.Add($"Story{i + 1}");
            }

            // Use the main method with elevation tracking
            DefineStoriesWithCustomNames(heights, names);
        }

        private void VerifyStories()
        {
            int numExisting = 0;
            string[] existingStories = null;
            sapModel.Story.GetNameList(ref numExisting, ref existingStories);

            System.Diagnostics.Debug.WriteLine($"Total stories created: {numExisting}");

            if (existingStories != null)
            {
                foreach (string story in existingStories)
                {
                    double elev = 0;
                    sapModel.Story.GetElevation(story, ref elev);
                    System.Diagnostics.Debug.WriteLine($"  - {story} at elevation {elev:F3}m");
                }
            }
        }

        // LEGACY: Keep for backward compatibility
        public string GetStoryName(int story)
        {
            return story == 0 ? "Base" : $"Story{story}";
        }

        // LEGACY: Keep for backward compatibility
        public double GetStoryElevation(int story, double storyHeight)
        {
            return story * storyHeight;
        }

        // LEGACY: Keep for backward compatibility
        public double GetStoryElevationVariable(List<double> storyHeights, int storyIndex)
        {
            // Use new method if available
            if (storyBaseElevations.ContainsKey(storyIndex))
            {
                return GetStoryBaseElevation(storyIndex);
            }

            // Fallback to old calculation
            if (storyIndex == 0) return 0.0;

            double elevation = 0.0;
            for (int i = 0; i < storyIndex && i < storyHeights.Count; i++)
            {
                elevation += storyHeights[i];
            }

            return elevation;
        }
    }
}