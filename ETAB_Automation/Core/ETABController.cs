using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ETABSv1;

namespace ETAB_Automation.Core
{
    /// <summary>
    /// Handles connection and initialization of ETABS
    /// Compatible with .NET 6/7/8
    /// </summary>
    public class ETABSController
    {
        #region Windows API Imports

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(
            byte bVk,
            byte bScan,
            uint dwFlags,
            UIntPtr dwExtraInfo);

        private const int VK_RETURN = 0x0D;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        #region COM GetActiveObject Replacement

        private static class ComHelper
        {
            [DllImport("oleaut32.dll", PreserveSig = false)]
            private static extern void GetActiveObject(
                ref Guid rclsid,
                IntPtr reserved,
                [MarshalAs(UnmanagedType.Interface)] out object ppunk);

            public static object GetActiveObject(string progID)
            {
                Type type = Type.GetTypeFromProgID(progID);
                if (type == null)
                    throw new Exception($"ProgID not registered: {progID}");

                Guid clsid = type.GUID;
                GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
                return obj;
            }
        }

        #endregion

        public cOAPI EtabsObject { get; private set; }
        public cSapModel SapModel { get; private set; }

        /// <summary>
        /// Connect to ETABS or start a new instance
        /// </summary>
        public bool Connect()
        {
            try
            {
                if (!AttachToRunningETABS())
                {
                    if (!StartNewETABS())
                        return false;
                }

                InitializeModel();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ETABS Connection Error:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        /// <summary>
        /// Try to attach to already running ETABS
        /// </summary>
        private bool AttachToRunningETABS()
        {
            try
            {
                EtabsObject = (cOAPI)ComHelper.GetActiveObject("CSI.ETABS.API.ETABSObject");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Start new ETABS instance
        /// </summary>
        private bool StartNewETABS()
        {
            try
            {
                cHelper helper = new Helper();
                EtabsObject = helper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");

                if (EtabsObject == null)
                    throw new Exception("Failed to create ETABS object.");

                EtabsObject.ApplicationStart();

                // Wait for ETABS to start
                Thread.Sleep(5000);

                HandleETABSLogin();

                Thread.Sleep(5000);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start ETABS:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        /// <summary>
        /// Initialize new blank model with proper units
        /// </summary>
        private void InitializeModel()
        {
            if (EtabsObject == null)
                throw new Exception("ETABS object is null.");

            SapModel = EtabsObject.SapModel;

            SapModel.InitializeNewModel();

           

            SapModel.File.NewBlank();

            // Set units AFTER creating model
            SapModel.SetPresentUnits(eUnits.N_m_C);

            SapModel.View.RefreshView(0, false);
        }

        /// <summary>
        /// Handle ETABS login dialog by simulating Enter key press
        /// </summary>
        private void HandleETABSLogin()
        {
            IntPtr hWnd = IntPtr.Zero;
            int attempts = 0;
            const int maxAttempts = 20;

            while (hWnd == IntPtr.Zero && attempts < maxAttempts)
            {
                hWnd = FindWindow(null, "ETABS");
                Thread.Sleep(500);
                attempts++;
            }

            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
                Thread.Sleep(1000);

                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }




        /// <summary>
        /// Disconnect from ETABS
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (SapModel != null)
                {
                    Marshal.ReleaseComObject(SapModel);
                    SapModel = null;
                }

                if (EtabsObject != null)
                {
                    Marshal.ReleaseComObject(EtabsObject);
                    EtabsObject = null;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
