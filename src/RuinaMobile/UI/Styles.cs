using System.Collections.Generic;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>IMGUI styles sized for fingers. Rebuilt only when the screen size, HUD size or opacity changes.</summary>
    internal static class Styles
    {
        internal static GUIStyle HudButton;
        internal static GUIStyle HudButtonActive;
        internal static GUIStyle GearButton;
        internal static GUIStyle Panel;
        internal static GUIStyle Title;
        internal static GUIStyle Section;
        internal static GUIStyle Label;
        internal static GUIStyle Small;
        internal static GUIStyle Value;
        internal static GUIStyle Button;
        internal static GUIStyle ButtonOn;
        internal static GUIStyle Field;
        internal static GUIStyle Banner;
        internal static GUIStyle Report;

        private static float builtHud = -1f;
        private static float builtPanel = -1f;
        private static float builtOpacity = -1f;
        private static readonly List<Texture2D> Owned = new List<Texture2D>();

        private static readonly Color Ink = new Color(1f, 0.92f, 0.76f, 1f);
        private static readonly Color Gold = new Color(0.86f, 0.66f, 0.28f, 1f);
        private static readonly Color Dark = new Color(0.08f, 0.065f, 0.05f, 1f);

        internal static void Ensure(float hudUnit, float panelUnit, float opacity)
        {
            if (HudButton != null && Mathf.Approximately(hudUnit, builtHud) && Mathf.Approximately(panelUnit, builtPanel)
                && Mathf.Approximately(opacity, builtOpacity)) return;

            builtHud = hudUnit;
            builtPanel = panelUnit;
            builtOpacity = opacity;
            foreach (var t in Owned)
                if (t != null) Object.Destroy(t);
            Owned.Clear();

            int hudFont = Mathf.Max(12, Mathf.RoundToInt(hudUnit * 2.8f));
            int font = Mathf.Max(12, Mathf.RoundToInt(panelUnit * 2.6f));
            int small = Mathf.Max(10, Mathf.RoundToInt(panelUnit * 2.05f));
            int pad = Mathf.Max(4, Mathf.RoundToInt(panelUnit * 1.2f));
            float textAlpha = Mathf.Clamp01(opacity + 0.4f);

            HudButton = Make(GUI.skin.button, hudFont, FontStyle.Bold, TextAnchor.MiddleCenter, false, pad);
            Fill(HudButton, WithAlpha(Dark, opacity), new Color(0.5f, 0.36f, 0.12f, Mathf.Clamp01(opacity + 0.3f)), WithAlpha(Ink, textAlpha));

            HudButtonActive = Make(GUI.skin.box, hudFont, FontStyle.Bold, TextAnchor.MiddleCenter, false, pad);
            Fill(HudButtonActive, new Color(0.5f, 0.36f, 0.12f, 0.85f), new Color(0.5f, 0.36f, 0.12f, 0.85f), Color.white);

            GearButton = Make(GUI.skin.button, Mathf.Max(11, Mathf.RoundToInt(hudUnit * 2.3f)), FontStyle.Bold, TextAnchor.MiddleCenter, false, pad);
            Fill(GearButton, WithAlpha(Dark, opacity), new Color(0.5f, 0.36f, 0.12f, 0.9f), WithAlpha(Gold, textAlpha));

            Panel = Make(GUI.skin.box, font, FontStyle.Normal, TextAnchor.UpperLeft, true, pad);
            Fill(Panel, new Color(0.06f, 0.05f, 0.04f, 0.96f), new Color(0.06f, 0.05f, 0.04f, 0.96f), Ink);

            Title = Make(GUI.skin.label, Mathf.RoundToInt(font * 1.25f), FontStyle.Bold, TextAnchor.MiddleLeft, false, pad);
            Title.normal.textColor = Gold;

            Section = Make(GUI.skin.label, font, FontStyle.Bold, TextAnchor.MiddleLeft, false, pad);
            Section.normal.textColor = Gold;
            Section.margin = new RectOffset(0, 0, pad * 2, pad / 2);

            Label = Make(GUI.skin.label, font, FontStyle.Normal, TextAnchor.MiddleLeft, true, pad / 2);
            Label.normal.textColor = Ink;

            Small = Make(GUI.skin.label, small, FontStyle.Normal, TextAnchor.UpperLeft, true, pad / 2);
            Small.normal.textColor = new Color(0.82f, 0.78f, 0.7f, 1f);

            Value = Make(GUI.skin.label, font, FontStyle.Bold, TextAnchor.MiddleCenter, false, pad / 2);
            Value.normal.textColor = Ink;

            Button = Make(GUI.skin.button, font, FontStyle.Bold, TextAnchor.MiddleCenter, false, pad);
            Fill(Button, new Color(0.2f, 0.16f, 0.12f, 1f), new Color(0.5f, 0.36f, 0.12f, 1f), Ink);

            ButtonOn = Make(GUI.skin.button, font, FontStyle.Bold, TextAnchor.MiddleCenter, false, pad);
            Fill(ButtonOn, new Color(0.72f, 0.53f, 0.2f, 1f), new Color(0.86f, 0.66f, 0.28f, 1f), Dark);

            Field = Make(GUI.skin.textField, font, FontStyle.Normal, TextAnchor.MiddleLeft, false, pad);
            Fill(Field, new Color(0.14f, 0.12f, 0.1f, 1f), new Color(0.2f, 0.17f, 0.13f, 1f), Ink);

            Banner = Make(GUI.skin.box, font, FontStyle.Bold, TextAnchor.MiddleCenter, true, pad);
            Fill(Banner, new Color(0.06f, 0.05f, 0.04f, 0.92f), new Color(0.06f, 0.05f, 0.04f, 0.92f), Ink);

            Report = Make(GUI.skin.box, small, FontStyle.Normal, TextAnchor.UpperLeft, true, pad);
            Fill(Report, new Color(0f, 0f, 0f, 0.82f), new Color(0f, 0f, 0f, 0.82f), new Color(0.75f, 1f, 0.75f, 1f));
        }

        private static GUIStyle Make(GUIStyle from, int size, FontStyle fontStyle, TextAnchor anchor, bool wrap, int padding)
        {
            var s = new GUIStyle(from)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                wordWrap = wrap,
                richText = false,
                padding = new RectOffset(padding, padding, padding, padding),
                border = new RectOffset(0, 0, 0, 0)
            };
            return s;
        }

        private static void Fill(GUIStyle s, Color normal, Color pressed, Color text)
        {
            var n = Tex(normal);
            var p = Tex(pressed);
            s.normal.background = n;
            s.hover.background = n;
            s.focused.background = n;
            s.active.background = p;
            s.onNormal.background = p;
            s.onHover.background = p;
            s.onActive.background = p;
            s.onFocused.background = p;
            s.normal.textColor = text;
            s.hover.textColor = text;
            s.focused.textColor = text;
            s.active.textColor = text;
            s.onNormal.textColor = text;
            s.onHover.textColor = text;
            s.onActive.textColor = text;
            s.onFocused.textColor = text;
        }

        private static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            Owned.Add(t);
            return t;
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
