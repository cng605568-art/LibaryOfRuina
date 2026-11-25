using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// BepInEx entry point. Runs inside the real PC build of Library of Ruina
    /// (Wine + Box64 + DXVK on Android) before the first scene loads, applies the
    /// engine-level patches and creates one persistent object that hosts every helper.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RuinaMobilePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.cng605568.ruinamobile";
        public const string PluginName = "Ruina Mobile";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static Settings Cfg;
        internal static string ConfigFilePath = "";

        private void Awake()
        {
            Log = Logger;
            Cfg = new Settings(Config);
            ConfigFilePath = Config.ConfigFilePath;
            GpuProfile.Detect();

            Log.LogInfo(PluginName + " " + PluginVersion
                + " | Unity " + Application.unityVersion
                + " | GPU '" + GpuProfile.DeviceName + "' (" + GpuProfile.Vendor + ") -> " + GpuProfile.Family
                + " | API " + SystemInfo.graphicsDeviceType
                + " | screen " + Screen.width + "x" + Screen.height);

            PatchUtil.Init(new Harmony(PluginGuid));
            InputPatches.Apply();
            CameraPatches.Apply();
            EventSystemPatches.Apply();
            Log.LogInfo("Patches: " + PatchUtil.Applied + " applied, " + PatchUtil.Failed + " skipped.");

            var host = new GameObject("RuinaMobile");
            DontDestroyOnLoad(host);
            host.AddComponent<GraphicsController>();
            host.AddComponent<StageScaler>();
            host.AddComponent<SmartFps>();
            host.AddComponent<CameraZoom>();
            host.AddComponent<DieSnap>();
            host.AddComponent<Inspector>();
            host.AddComponent<Hud>();

            ModIndex.Rescan();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo(PluginName + " loaded. Tap the SET button (top-left by default) for settings.");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo("Scene loaded: '" + scene.name + "' (" + mode + ")");
        }
    }

    /// <summary>Applies Harmony patches one by one so a single unsupported method never stops the rest.</summary>
    internal static class PatchUtil
    {
        private static Harmony harmony;
        internal static int Applied;
        internal static int Failed;

        internal static void Init(Harmony instance)
        {
            harmony = instance;
        }

        internal static bool Patch(MethodBase original, Type patchType, string prefix = null, string postfix = null,
            string finalizer = null, bool quiet = false)
        {
            if (original == null)
            {
                Failed++;
                return false;
            }

            try
            {
                harmony.Patch(original,
                    prefix: Method(patchType, prefix),
                    postfix: Method(patchType, postfix),
                    finalizer: Method(patchType, finalizer));
                Applied++;
                return true;
            }
            catch (Exception e)
            {
                Failed++;
                string msg = "Skipped patch " + (original.DeclaringType != null ? original.DeclaringType.Name : "?")
                             + "." + original.Name + ": " + e.GetType().Name + " " + e.Message;
                if (quiet) RuinaMobilePlugin.Log.LogDebug(msg);
                else RuinaMobilePlugin.Log.LogWarning(msg);
                return false;
            }
        }

        private static HarmonyMethod Method(Type type, string name)
        {
            if (name == null) return null;
            var m = AccessTools.Method(type, name);
            if (m == null) throw new MissingMethodException(type.Name, name);
            return new HarmonyMethod(m);
        }
    }
}
