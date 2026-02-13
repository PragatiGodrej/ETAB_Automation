// ============================================================================
// FILE: Models/LayerMapping.cs
// ============================================================================
using System.Collections.Generic;

namespace ETABS_CAD_Automation.Models
{
    /// <summary>
    /// Represents mapping between CAD layers and structural element types
    /// </summary>
    public class LayerMapping
    {
        public Dictionary<string, string> Mappings { get; set; }

        public LayerMapping()
        {
            Mappings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Add a layer mapping
        /// </summary>
        public void AddMapping(string layerName, string elementType)
        {
            if (!Mappings.ContainsKey(layerName))
            {
                Mappings[layerName] = elementType;
            }
        }

        /// <summary>
        /// Remove a layer mapping
        /// </summary>
        public void RemoveMapping(string layerName)
        {
            if (Mappings.ContainsKey(layerName))
            {
                Mappings.Remove(layerName);
            }
        }

        /// <summary>
        /// Get element type for a layer
        /// </summary>
        public string GetElementType(string layerName)
        {
            return Mappings.ContainsKey(layerName) ? Mappings[layerName] : null;
        }

        /// <summary>
        /// Get all layers mapped to a specific element type
        /// </summary>
        public List<string> GetLayersByElementType(string elementType)
        {
            List<string> layers = new List<string>();
            foreach (var kvp in Mappings)
            {
                if (kvp.Value == elementType)
                {
                    layers.Add(kvp.Key);
                }
            }
            return layers;
        }

        /// <summary>
        /// Clear all mappings
        /// </summary>
        public void Clear()
        {
            Mappings.Clear();
        }

        /// <summary>
        /// Get total number of mappings
        /// </summary>
        public int Count => Mappings.Count;

        public override string ToString()
        {
            return $"LayerMapping: {Mappings.Count} layers mapped";
        }
    }
}