using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// UnityEngine.UI ships inside the game's Managed folder, not as a NuGet reference package,
    /// so the plugin reaches uGUI types by name at runtime instead of compiling against them.
    /// </summary>
    internal static class UiReflection
    {
        internal static readonly Type EventSystemType = Find("UnityEngine.EventSystems.EventSystem");
        internal static readonly Type PointerEventDataType = Find("UnityEngine.EventSystems.PointerEventData");
        internal static readonly Type RaycastResultType = Find("UnityEngine.EventSystems.RaycastResult");
        internal static readonly Type GraphicType = Find("UnityEngine.UI.Graphic");
        internal static readonly Type ImageType = Find("UnityEngine.UI.Image");
        internal static readonly Type LayoutElementType = Find("UnityEngine.UI.LayoutElement");

        private static Type Find(string fullName)
        {
            Type t = null;
            try
            {
                t = Type.GetType(fullName + ", UnityEngine.UI", false);
            }
            catch (Exception)
            {
                // fall through to a search of loaded assemblies
            }

            if (t == null)
            {
                try
                {
                    t = AccessTools.TypeByName(fullName);
                }
                catch (Exception)
                {
                    t = null;
                }
            }

            return t;
        }

        internal static void Set(object target, string property, object value)
        {
            if (target == null) return;
            var p = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) p.SetValue(target, value, null);
        }

        /// <summary>Everything uGUI's EventSystem would hit at a screen position (bottom-left origin).</summary>
        internal static List<GameObject> RaycastUi(Vector2 screenPosition)
        {
            var hits = new List<GameObject>();
            if (EventSystemType == null || PointerEventDataType == null || RaycastResultType == null) return hits;

            var currentProp = EventSystemType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            object current = currentProp != null ? currentProp.GetValue(null, null) : null;
            var currentObject = current as UnityEngine.Object;
            if (current == null || currentObject == null) return hits;

            var raycastAll = EventSystemType.GetMethod("RaycastAll", BindingFlags.Public | BindingFlags.Instance);
            if (raycastAll == null) return hits;

            var data = Activator.CreateInstance(PointerEventDataType, current);
            Set(data, "position", screenPosition);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RaycastResultType));

            bool previous = PointerGate.Bypass;
            PointerGate.Bypass = true;
            try
            {
                raycastAll.Invoke(current, new object[] { data, list });
            }
            finally
            {
                PointerGate.Bypass = previous;
            }

            var goProp = RaycastResultType.GetProperty("gameObject", BindingFlags.Public | BindingFlags.Instance);
            if (goProp == null) return hits;
            foreach (var r in list)
            {
                var go = goProp.GetValue(r, null) as GameObject;
                if (go != null) hits.Add(go);
            }

            return hits;
        }
    }
}
