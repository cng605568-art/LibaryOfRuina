using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// Feeds the on-screen buttons into Unity's legacy Input API and hides touches that land on
    /// Ruina Mobile's own buttons from the game. Unity implements some of these methods natively;
    /// any that cannot be patched are skipped and logged at debug level.
    /// </summary>
    internal static class InputPatches
    {
        internal static void Apply()
        {
            var input = typeof(Input);
            foreach (var m in input.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var ps = m.GetParameters();
                if (ps.Length != 1) continue;
                var pt = ps[0].ParameterType;

                if (m.ReturnType == typeof(bool))
                {
                    if (pt == typeof(KeyCode))
                    {
                        if (m.Name == "GetKeyDown") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyDownCode));
                        else if (m.Name == "GetKey") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyHeldCode));
                        else if (m.Name == "GetKeyUp") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyUpCode));
                    }
                    else if (pt == typeof(string))
                    {
                        if (m.Name == "GetKeyDown") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyDownName));
                        else if (m.Name == "GetKey") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyHeldName));
                        else if (m.Name == "GetKeyUp") PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(KeyUpName));
                    }
                    else if (pt == typeof(int) && (m.Name == "GetMouseButtonDown" || m.Name == "GetMouseButton" || m.Name == "GetMouseButtonUp"))
                    {
                        PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(MouseButtonGate), quiet: true);
                    }
                }
                else if (m.ReturnType == typeof(float) && pt == typeof(string) && (m.Name == "GetAxis" || m.Name == "GetAxisRaw"))
                {
                    PatchUtil.Patch(m, typeof(InputPatches), postfix: nameof(AxisScroll), quiet: true);
                }
            }

            PatchUtil.Patch(AccessTools.PropertyGetter(input, "mouseScrollDelta"), typeof(InputPatches), postfix: nameof(ScrollDelta));
        }

        private static void KeyDownCode(KeyCode __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsDown(__0)) __result = true;
        }

        private static void KeyHeldCode(KeyCode __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsHeld(__0)) __result = true;
        }

        private static void KeyUpCode(KeyCode __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsUp(__0)) __result = true;
        }

        private static void KeyDownName(string __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsDown(__0)) __result = true;
        }

        private static void KeyHeldName(string __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsHeld(__0)) __result = true;
        }

        private static void KeyUpName(string __0, ref bool __result)
        {
            if (!__result && VirtualKeys.IsUp(__0)) __result = true;
        }

        private static void MouseButtonGate(ref bool __result)
        {
            if (__result && !PointerGate.Bypass && PointerGate.BlockGameButtons) __result = false;
        }

        private static void AxisScroll(string __0, ref float __result)
        {
            if (__0 != "Mouse ScrollWheel") return;
            float n = VirtualScroll.Notches;
            if (n != 0f) __result += n * 0.1f;
        }

        private static void ScrollDelta(ref Vector2 __result)
        {
            float n = VirtualScroll.Notches;
            if (n != 0f) __result.y += n;
        }
    }

    /// <summary>
    /// Clears uGUI / physics raycast results while a touch belongs to Ruina Mobile, so buttons
    /// and battle objects under our HUD or settings panel do not react.
    /// </summary>
    internal static class EventSystemPatches
    {
        internal static void Apply()
        {
            var type = UiReflection.EventSystemType;
            if (type == null)
            {
                RuinaMobilePlugin.Log.LogWarning("UnityEngine.UI EventSystem not found; HUD touches may also reach the game.");
                return;
            }

            PatchUtil.Patch(AccessTools.Method(type, "RaycastAll"), typeof(EventSystemPatches), postfix: nameof(RaycastAllPostfix));
        }

        // __1 is List<RaycastResult>; typed as object because UnityEngine.UI is not referenced at compile time.
        private static void RaycastAllPostfix(object __1)
        {
            if (PointerGate.Bypass || !PointerGate.BlockRaycasts) return;
            var list = __1 as System.Collections.IList;
            if (list != null) list.Clear();
        }
    }
}
