using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// When StageScaler renders battle cameras into a smaller texture, those cameras' pixel space
    /// shrinks. These patches translate between screen pixels and stage-texture pixels for the
    /// redirected cameras only, so taps, drag arrows and UI that follows characters stay aligned.
    /// Unity overloads chain into each other (e.g. ScreenPointToRay(pos) -> ScreenPointToRay(pos, eye)),
    /// so a depth counter makes sure each call is converted exactly once.
    /// </summary>
    internal static class CameraPatches
    {
        private static int depth;

        internal static void Apply()
        {
            var camera = typeof(Camera);
            foreach (var m in camera.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var ps = m.GetParameters();
                if (ps.Length == 0 || ps[0].ParameterType != typeof(Vector3)) continue;

                switch (m.Name)
                {
                    case "ScreenPointToRay":
                    case "ScreenToWorldPoint":
                    case "ScreenToViewportPoint":
                        PatchUtil.Patch(m, typeof(CameraPatches), prefix: nameof(ScreenInPrefix), finalizer: nameof(DepthFinalizer));
                        break;
                    case "WorldToScreenPoint":
                    case "ViewportToScreenPoint":
                        PatchUtil.Patch(m, typeof(CameraPatches), prefix: nameof(ScreenOutPrefix), postfix: nameof(ScreenOutPostfix),
                            finalizer: nameof(DepthFinalizer));
                        break;
                }
            }

            PatchUtil.Patch(AccessTools.PropertyGetter(camera, "pixelRect"), typeof(CameraPatches), postfix: nameof(PixelRectPostfix));
            PatchUtil.Patch(AccessTools.PropertyGetter(camera, "pixelWidth"), typeof(CameraPatches), postfix: nameof(WidthPostfix), quiet: true);
            PatchUtil.Patch(AccessTools.PropertyGetter(camera, "pixelHeight"), typeof(CameraPatches), postfix: nameof(HeightPostfix), quiet: true);
            PatchUtil.Patch(AccessTools.PropertyGetter(camera, "scaledPixelWidth"), typeof(CameraPatches), postfix: nameof(WidthPostfix), quiet: true);
            PatchUtil.Patch(AccessTools.PropertyGetter(camera, "scaledPixelHeight"), typeof(CameraPatches), postfix: nameof(HeightPostfix), quiet: true);

            foreach (var m in typeof(RectTransformUtility).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "RectangleContainsScreenPoint") continue;
                var ps = m.GetParameters();
                if (ps.Length >= 3 && ps[1].ParameterType == typeof(Vector2) && ps[2].ParameterType == typeof(Camera))
                    PatchUtil.Patch(m, typeof(CameraPatches), prefix: nameof(RectContainsPrefix));
            }
        }

        private static void ScreenInPrefix(Camera __instance, ref Vector3 __0)
        {
            depth++;
            if (depth != 1 || !StageScaler.IsScaled(__instance)) return;
            __0.x *= StageScaler.ScaleX;
            __0.y *= StageScaler.ScaleY;
        }

        private static void ScreenOutPrefix(Camera __instance, out bool __state)
        {
            depth++;
            __state = depth == 1 && StageScaler.IsScaled(__instance);
        }

        private static void ScreenOutPostfix(ref Vector3 __result, bool __state)
        {
            if (!__state) return;
            __result.x /= StageScaler.ScaleX;
            __result.y /= StageScaler.ScaleY;
        }

        private static void DepthFinalizer()
        {
            if (depth > 0) depth--;
        }

        private static void PixelRectPostfix(Camera __instance, ref Rect __result)
        {
            if (StageScaler.IsScaled(__instance)) __result = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        private static void WidthPostfix(Camera __instance, ref int __result)
        {
            if (StageScaler.IsScaled(__instance)) __result = Screen.width;
        }

        private static void HeightPostfix(Camera __instance, ref int __result)
        {
            if (StageScaler.IsScaled(__instance)) __result = Screen.height;
        }

        private static void RectContainsPrefix(ref Vector2 __1, Camera __2)
        {
            if (!StageScaler.IsScaled(__2)) return;
            __1.x *= StageScaler.ScaleX;
            __1.y *= StageScaler.ScaleY;
        }
    }
}
