// ============================================================================
// FILE: Importers/CADLayerReader.cs
// ============================================================================
using netDxf;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ETAB_Automation.Importers
{
    public class CADLayerReader
    {
        public List<string> GetLayerNamesFromFile(string path)
        {
            List<string> layerNames = new List<string>();

            try
            {
                DxfDocument doc = DxfDocument.Load(path);

                if (doc == null)
                {
                    MessageBox.Show("Failed to load DWG/DXF file.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return layerNames;
                }

                foreach (var layer in doc.Layers)
                {
                    layerNames.Add(layer.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read layers:\n{ex.Message}", "CAD Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return layerNames;
        }
    }
}