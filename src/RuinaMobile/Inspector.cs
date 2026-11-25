using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// Touch inspector: when switched on in Settings > Controls, every tap lists the UI elements and
    /// colliders under the finger (name path + component types). Use it to find the right
    /// SpeedDieKeywords for your game version without decompiling anything.
    /// </summary>
    internal sealed class Inspector : MonoBehaviour
    {
        internal static Inspector Instance;
        private string report = "";
        private float showUntil;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!RuinaMobilePlugin.Cfg.Inspector.Value) return;

            bool down;
            bool previous = PointerGate.Bypass;
            PointerGate.Bypass = true;
            try
            {
                down = Input.GetMouseButtonDown(0);
            }
            finally
            {
                PointerGate.Bypass = previous;
            }

            if (!down) return;
            Vector2 pos = Input.mousePosition;
            if (Hud.Instance != null && Hud.Instance.IsOverHud(pos)) return;

            report = BuildReport(pos);
            showUntil = Time.unscaledTime + 12f;
            RuinaMobilePlugin.Log.LogInfo("[Inspector] " + report.Replace("\n", " | "));
        }

        private static string BuildReport(Vector2 pos)
        {
            var sb = new StringBuilder();
            sb.Append("Tap at ").Append(Mathf.RoundToInt(pos.x)).Append(", ").Append(Mathf.RoundToInt(pos.y))
              .Append("   scene '").Append(SceneManager.GetActiveScene().name).Append("'\n");
            int lines = 0;

            foreach (var go in UiReflection.RaycastUi(pos))
            {
                if (lines >= 6) break;
                sb.Append("UI  ").Append(Describe(go)).Append('\n');
                lines++;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(pos.x, pos.y, Mathf.Abs(cam.transform.position.z)));
                foreach (var c in Physics2D.OverlapPointAll(world))
                {
                    if (lines >= 10) break;
                    sb.Append("2D  ").Append(Describe(c.gameObject)).Append('\n');
                    lines++;
                }

                foreach (var hit in Physics.RaycastAll(cam.ScreenPointToRay(pos), 1000f))
                {
                    if (lines >= 12) break;
                    sb.Append("3D  ").Append(Describe(hit.collider.gameObject)).Append('\n');
                    lines++;
                }
            }

            if (lines == 0) sb.Append("(nothing that accepts touches is here)\n");
            return sb.ToString().TrimEnd('\n');
        }

        private static string Describe(GameObject go)
        {
            var parts = new List<string>();
            var t = go.transform;
            for (int i = 0; i < 4 && t != null; i++, t = t.parent) parts.Insert(0, t.name);

            var types = new List<string>();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "Transform" || n == "RectTransform" || n == "CanvasRenderer") continue;
                types.Add(n);
                if (types.Count >= 4) break;
            }

            var sb = new StringBuilder(string.Join("/", parts.ToArray()));
            if (types.Count > 0) sb.Append("  [").Append(string.Join(", ", types.ToArray())).Append(']');
            return sb.ToString();
        }

        internal void DrawOverlay(float unit)
        {
            if (!RuinaMobilePlugin.Cfg.Inspector.Value) return;
            string text = Time.unscaledTime < showUntil
                ? report
                : "Touch inspector is ON: tap anything to see its name. Turn it off in SET > Controls.";
            var rect = new Rect(unit * 2f, Screen.height - unit * 32f, Screen.width - unit * 4f, unit * 30f);
            GUI.Box(rect, text, Styles.Report);
        }
    }
}
