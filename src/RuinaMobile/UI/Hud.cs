using System.Collections.Generic;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// Floating translucent buttons drawn with IMGUI on top of the game:
    ///  SET   - opens the settings panel (always visible)
    ///  SPACE - big button, sends Button1Key (Space by default)
    ///  ESC   - small button, sends Button2Key (Escape by default)
    ///  + / - - mouse-wheel zoom, FIT - camera reset
    /// Positions are editable in game (SET > Controls > Move buttons) and saved to the config file.
    /// </summary>
    internal sealed class Hud : MonoBehaviour
    {
        internal static Hud Instance;

        private Dictionary<string, Vector2> layout;
        private SettingsPanel panel;
        private bool panelOpen;
        private bool editMode;
        private string dragging;
        private Vector2 dragOffset;
        private string toast = "";
        private float toastUntil;
        private readonly List<string> drawIds = new List<string>(8);
        private readonly List<string> hitIds = new List<string>(8);

        private static Settings Cfg
        {
            get { return RuinaMobilePlugin.Cfg; }
        }

        /// <summary>HUD size unit: 1% of screen height times the user's button size.</summary>
        internal float Unit
        {
            get { return Screen.height / 100f * Mathf.Clamp(Cfg.HudScale.Value, 0.5f, 2f); }
        }

        /// <summary>Panel size unit: 1% of screen height.</summary>
        internal float PanelUnit
        {
            get { return Mathf.Max(6f, Screen.height / 100f); }
        }

        private void Awake()
        {
            Instance = this;
            layout = HudLayout.Parse(Cfg.HudLayoutString.Value);
            panel = new SettingsPanel(this);
            PointerGate.HitTest = IsOverHud;
        }

        private void Update()
        {
            PointerGate.ModalOpen = panelOpen || editMode;
        }

        internal void OpenPanel()
        {
            panelOpen = true;
            PointerGate.ModalOpen = true;
        }

        internal void ClosePanel()
        {
            panelOpen = false;
            PointerGate.ModalOpen = editMode;
        }

        internal void StartEditLayout()
        {
            panelOpen = false;
            editMode = true;
            PointerGate.ModalOpen = true;
        }

        internal void ResetLayout()
        {
            layout = HudLayout.Defaults();
            SaveLayout();
            Toast("Button positions reset");
        }

        internal void Toast(string message)
        {
            toast = message;
            toastUntil = Time.unscaledTime + 2.5f;
        }

        private void SaveLayout()
        {
            Cfg.HudLayoutString.Value = HudLayout.Serialize(layout);
        }

        /// <summary>Is this screen position (Unity bottom-left origin) on a Ruina Mobile button or panel?</summary>
        internal bool IsOverHud(Vector2 screenPosition)
        {
            if (panelOpen || editMode) return true;
            var p = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            CollectVisible(hitIds);
            for (int i = 0; i < hitIds.Count; i++)
                if (RectFor(hitIds[i]).Contains(p)) return true;
            return false;
        }

        private void CollectVisible(List<string> into)
        {
            into.Clear();
            into.Add("gear");
            var c = Cfg;
            if (!c.HudEnabled.Value) return;
            if (c.Button1Key.Value != KeyCode.None) into.Add("key1");
            if (c.Button2Key.Value != KeyCode.None) into.Add("key2");
            if (c.ZoomButtons.Value)
            {
                into.Add("zin");
                into.Add("zout");
                into.Add("zreset");
            }
        }

        private static float SizeOf(string id)
        {
            switch (id)
            {
                case "key1": return 15f;
                case "key2": return 10f;
                case "gear": return 8f;
                default: return 8.5f;
            }
        }

        private Rect RectFor(string id)
        {
            Vector2 c;
            if (!layout.TryGetValue(id, out c)) c = new Vector2(0.5f, 0.5f);
            float s = SizeOf(id) * Unit;
            float x = Mathf.Clamp(c.x * Screen.width - s * 0.5f, 0f, Mathf.Max(0f, Screen.width - s));
            float y = Mathf.Clamp(c.y * Screen.height - s * 0.5f, 0f, Mathf.Max(0f, Screen.height - s));
            return new Rect(x, y, s, s);
        }

        private string LabelOf(string id)
        {
            switch (id)
            {
                case "gear": return "SET";
                case "key1": return KeyNames.Label(Cfg.Button1Key.Value);
                case "key2": return KeyNames.Label(Cfg.Button2Key.Value);
                case "zin": return "+";
                case "zout": return "-";
                case "zreset": return "FIT";
                default: return id;
            }
        }

        private void OnGUI()
        {
            GUI.depth = -10000;
            Styles.Ensure(Unit, PanelUnit, Cfg.HudOpacity.Value);
            try
            {
                if (Inspector.Instance != null) Inspector.Instance.DrawOverlay(PanelUnit);

                if (panelOpen)
                {
                    panel.Draw(PanelUnit);
                }
                else if (editMode)
                {
                    DrawEditMode();
                }
                else
                {
                    DrawButtons();
                }

                if (Time.unscaledTime < toastUntil)
                {
                    float u = PanelUnit;
                    GUI.Box(new Rect(Screen.width * 0.5f - u * 30f, Screen.height - u * 12f, u * 60f, u * 8f), toast, Styles.Banner);
                }
            }
            finally
            {
                panel.FlushDeferred();
            }
        }

        private void DrawButtons()
        {
            CollectVisible(drawIds);
            for (int i = 0; i < drawIds.Count; i++)
            {
                string id = drawIds[i];
                if (GUI.Button(RectFor(id), LabelOf(id), id == "gear" ? Styles.GearButton : Styles.HudButton)) Press(id);
            }
        }

        private void Press(string id)
        {
            var c = Cfg;
            switch (id)
            {
                case "gear":
                    OpenPanel();
                    break;
                case "key1":
                    VirtualKeys.Tap(c.Button1Key.Value);
                    break;
                case "key2":
                    VirtualKeys.Tap(c.Button2Key.Value);
                    break;
                case "zin":
                    VirtualScroll.Add(c.ZoomStep.Value);
                    break;
                case "zout":
                    VirtualScroll.Add(-c.ZoomStep.Value);
                    break;
                case "zreset":
                    if (CameraZoom.Instance != null) CameraZoom.Instance.ResetZoom();
                    break;
            }
        }

        private void DrawEditMode()
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                for (int i = HudLayout.Ids.Length - 1; i >= 0; i--)
                {
                    var r = RectFor(HudLayout.Ids[i]);
                    if (!r.Contains(e.mousePosition)) continue;
                    dragging = HudLayout.Ids[i];
                    dragOffset = r.center - e.mousePosition;
                    e.Use();
                    break;
                }
            }
            else if (e.type == EventType.MouseDrag && dragging != null)
            {
                var centre = e.mousePosition + dragOffset;
                layout[dragging] = new Vector2(Mathf.Clamp01(centre.x / Screen.width), Mathf.Clamp01(centre.y / Screen.height));
                e.Use();
            }
            else if (e.type == EventType.MouseUp && dragging != null)
            {
                dragging = null;
                SaveLayout();
                e.Use();
            }

            foreach (var id in HudLayout.Ids)
                GUI.Box(RectFor(id), LabelOf(id), id == dragging ? Styles.HudButtonActive : Styles.HudButton);

            float u = PanelUnit;
            var banner = new Rect(Screen.width * 0.5f - u * 38f, Screen.height * 0.5f - u * 12f, u * 76f, u * 10f);
            GUI.Box(banner, "Drag the buttons where you want them, then tap DONE.", Styles.Banner);
            if (GUI.Button(new Rect(banner.center.x - u * 12f, banner.yMax + u * 2f, u * 24f, u * 9f), "DONE", Styles.ButtonOn))
            {
                editMode = false;
                dragging = null;
                PointerGate.ModalOpen = panelOpen;
                SaveLayout();
                Toast("Button positions saved");
            }
        }
    }
}
