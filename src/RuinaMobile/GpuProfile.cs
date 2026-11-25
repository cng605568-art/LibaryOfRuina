using UnityEngine;

namespace RuinaMobile
{
    internal enum GpuFamily
    {
        Unknown,
        Adreno,
        Mali,
        Xclipse,
        PowerVR,
        Desktop
    }

    /// <summary>
    /// Reads the GPU name the game sees. Under Winlator / GameHub this is whatever DXVK or WineD3D
    /// reports: usually the real phone GPU (e.g. "Turnip Adreno (TM) 740"), but some emulator
    /// profiles spoof a desktop card, in which case Auto falls back to the full-quality defaults.
    /// </summary>
    internal static class GpuProfile
    {
        internal static string DeviceName = "";
        internal static string Vendor = "";
        internal static GpuFamily Family = GpuFamily.Unknown;

        internal static bool IsWeakMobile
        {
            get { return Family == GpuFamily.Mali || Family == GpuFamily.Xclipse || Family == GpuFamily.PowerVR; }
        }

        internal static void Detect()
        {
            DeviceName = SystemInfo.graphicsDeviceName ?? "";
            Vendor = SystemInfo.graphicsDeviceVendor ?? "";
            string s = (DeviceName + " " + Vendor).ToLowerInvariant();

            if (s.Contains("adreno") || s.Contains("turnip") || s.Contains("qualcomm")) Family = GpuFamily.Adreno;
            else if (s.Contains("mali") || s.Contains("immortalis")) Family = GpuFamily.Mali;
            else if (s.Contains("xclipse")) Family = GpuFamily.Xclipse;
            else if (s.Contains("powervr") || s.Contains("imagination")) Family = GpuFamily.PowerVR;
            else if (s.Contains("nvidia") || s.Contains("geforce") || s.Contains("radeon") || s.Contains("amd")
                     || s.Contains("intel")) Family = GpuFamily.Desktop;
            else Family = GpuFamily.Unknown;
        }

        internal static string Describe()
        {
            switch (Family)
            {
                case GpuFamily.Adreno: return "Adreno (Snapdragon)";
                case GpuFamily.Mali: return "Mali (MediaTek / Tensor / Exynos)";
                case GpuFamily.Xclipse: return "Xclipse (Exynos)";
                case GpuFamily.PowerVR: return "PowerVR";
                case GpuFamily.Desktop: return "desktop GPU name (probably spoofed by the emulator)";
                default: return "not recognised";
            }
        }
    }
}
