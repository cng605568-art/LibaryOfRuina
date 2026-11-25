using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// Touch-friendly settings panel (IMGUI). Big buttons and +/- steppers instead of tiny sliders,
    /// drag anywhere to scroll. Every change is written to the BepInEx config file immediately.
    /// State changes are deferred to the end of OnGUI so IMGUI's layout passes stay consistent.
    /// </summary>
    internal sealed class SettingsPanel
    {
        private enum Tab
        {
            Graphics,
            Controls,
            Mods,
            Info
        }

        private static readonly KeyCode[] KeyChoices =
        {
            KeyCode.Space, KeyCode.Escape, KeyCode.Return, KeyCode.Tab,
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
            KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4,
            KeyCode.None
        };

        private static readonly int[] CombatChoices = { 30, 45, 60, 0 };

        private readonly Hud hud;
        private readonly List<Action> deferred = new List<Action>();
        private Tab tab = Tab.Graphics;
        private Vector2 scroll;
        private float unit = 8f;
        private bool touchScrolling;
        private float touchTravel;

        internal SettingsPanel(Hud hud)
        {
            this.hud = hud;
        }

        private static Settings Cfg
        {
            get { return RuinaMobilePlugin.Cfg; }
        }

        private void Defer(Action action)
        {
            if (action != null) deferred.Add(action);
        }

        internal void FlushDeferred()
        {
            if (deferred.Count == 0) return;
            var pending = deferred.ToArray();
            deferred.Clear();
            foreach (var action in pending)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    RuinaMobilePlugin.Log.LogWarning("Settings action failed: " + e.Message);
                }
            }
        }

        private GUILayoutOption W(float units)
        {
            return GUILayout.Width(units * unit);
        }

        private GUILayoutOption H(float units)
        {
            return GUILayout.Height(units * unit);
        }

        private GUILayoutOption RowHeight()
        {
            return GUILayout.MinHeight(8f * unit);
        }

        internal void Draw(float u)
        {
            unit = u;
            float width = Mathf.Min(Screen.width - u * 4f, u * 170f);
            float height = Screen.height - u * 6f;
            var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            HandleTouchScroll(area);
            GUI.Box(area, GUIContent.none, Styles.Panel);
            GUILayout.BeginArea(new Rect(area.x + u * 2f, area.y + u * 1.5f, area.width - u * 4f, area.height - u * 3f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ruina Mobile " + RuinaMobilePlugin.PluginVersion, Styles.Title, H(8));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("CLOSE", Styles.ButtonOn, W(22), H(8))) Defer(hud.ClosePanel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            TabButton(Tab.Graphics, "Graphics");
            TabButton(Tab.Controls, "Controls");
            TabButton(Tab.Mods, "Mods");
            TabButton(Tab.Info, "Info");
            GUILayout.EndHorizontal();
            GUILayout.Space(u);

            scroll = GUILayout.BeginScrollView(scroll, false, true);
            switch (tab)
            {
                case Tab.Graphics:
                    DrawGraphics();
                    break;
                case Tab.Controls:
                    DrawControls();
                    break;
                case Tab.Mods:
                    DrawMods();
                    break;
                default:
                    DrawInfo();
                    break;
            }

            GUILayout.Space(u * 4f);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Drag-to-scroll for touch screens. A drag never also counts as a button tap.</summary>
        private void HandleTouchScroll(Rect area)
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    touchScrolling = false;
                    touchTravel = 0f;
                    break;
                case EventType.MouseDrag:
                    if (!area.Contains(e.mousePosition)) break;
                    touchTravel += Mathf.Abs(e.delta.y);
                    if (touchTravel > unit * 1.5f) touchScrolling = true;
                    if (touchScrolling)
                    {
                        scroll.y = Mathf.Max(0f, scroll.y - e.delta.y);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (touchScrolling)
                    {
                        touchScrolling = false;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }

                    break;
            }
        }

        private void TabButton(Tab t, string label)
        {
            if (GUILayout.Button(label, tab == t ? Styles.ButtonOn : Styles.Button, H(8)))
            {
                Defer(() =>
                {
                    tab = t;
                    scroll = Vector2.zero;
                });
            }
        }

        // ---------------------------------------------------------------- tabs

        private void DrawGraphics()
        {
            var c = Cfg;

            SectionLabel("Your GPU");
            Note((GpuProfile.DeviceName.Length > 0 ? GpuProfile.DeviceName : "(no name reported)") + "  ->  "
                 + GpuProfile.Describe() + "   |   API: " + SystemInfo.graphicsDeviceType);
            if (GpuProfile.Family == GpuFamily.Desktop)
                Note("That looks like a desktop GPU name, so the emulator is probably hiding your phone's GPU. "
                     + "Auto treats it as a strong GPU. Pick Balanced or Performance yourself if battles stutter.");

            SectionLabel("Preset");
            ChoiceRow((GraphicsPreset[])Enum.GetValues(typeof(GraphicsPreset)), c.Preset.Value, p => p.ToString(),
                p => c.Preset.Value = p);
            Note("Now using: stage " + Pct(c.EffectiveStageScale) + ", " + (c.EffectiveLightRender ? "light" : "full")
                 + " render settings. Changing either value below switches the preset to Custom.");

            StepperRow("Stage resolution", Pct(c.EffectiveStageScale),
                () => SetCustom(c.EffectiveStageScale - 0.05f, c.EffectiveLightRender),
                () => SetCustom(c.EffectiveStageScale + 0.05f, c.EffectiveLightRender));
            Note("Only the battle stage is scaled; cards, dice numbers and menus stay at full resolution. Status: "
                 + StageScaler.Status + ".");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Light render settings", Styles.Label, GUILayout.ExpandWidth(true), RowHeight());
            bool light = c.EffectiveLightRender;
            if (GUILayout.Button(light ? "ON" : "OFF", light ? Styles.ButtonOn : Styles.Button, W(18), H(8)))
                Defer(() => SetCustom(c.EffectiveStageScale, !light));
            GUILayout.EndHorizontal();
            Note("Turns off MSAA, anisotropic filtering, soft particles and realtime reflection probes.");

            ToggleRow("Flip stage image", c.StageFlipY);
            Note("Only needed if the battle looks upside down when the stage resolution is below 100%.");

            SectionLabel("Frame rate");
            ToggleRow("Smart FPS", c.SmartFps);
            Note("Idle FPS in menus, story and deckbuilding; combat FPS while you touch the screen, the camera moves or effects play.");
            StepperRow("Idle FPS", c.IdleFps.Value.ToString(),
                () => c.IdleFps.Value = Mathf.Max(15, c.IdleFps.Value - 5),
                () => c.IdleFps.Value = Mathf.Min(60, c.IdleFps.Value + 5));
            GUILayout.Label("Combat FPS", Styles.Label, RowHeight());
            ChoiceRow(CombatChoices, c.CombatFps.Value, v => v == 0 ? "Native" : v.ToString(), v => c.CombatFps.Value = v);
            Note(SmartFps.Status);
        }

        private void DrawControls()
        {
            var c = Cfg;

            SectionLabel("On-screen buttons");
            ToggleRow("Show combat buttons", c.HudEnabled);
            StepperRow("Button size", c.HudScale.Value.ToString("0.0"),
                () => c.HudScale.Value = Mathf.Max(0.5f, Round(c.HudScale.Value - 0.1f, 10f)),
                () => c.HudScale.Value = Mathf.Min(2f, Round(c.HudScale.Value + 0.1f, 10f)));
            StepperRow("Opacity", Pct(c.HudOpacity.Value),
                () => c.HudOpacity.Value = Mathf.Max(0.1f, Round(c.HudOpacity.Value - 0.05f, 20f)),
                () => c.HudOpacity.Value = Mathf.Min(1f, Round(c.HudOpacity.Value + 0.05f, 20f)));
            StepperRow("Big button sends", KeyNames.Label(c.Button1Key.Value),
                () => c.Button1Key.Value = Cycle(c.Button1Key.Value, -1),
                () => c.Button1Key.Value = Cycle(c.Button1Key.Value, 1), "<", ">");
            StepperRow("Small button sends", KeyNames.Label(c.Button2Key.Value),
                () => c.Button2Key.Value = Cycle(c.Button2Key.Value, -1),
                () => c.Button2Key.Value = Cycle(c.Button2Key.Value, 1), "<", ">");
            ToggleRow("Zoom buttons (+ / - / FIT)", c.ZoomButtons);
            StepperRow("Zoom per tap", c.ZoomStep.Value + " notch",
                () => c.ZoomStep.Value = Mathf.Max(1, c.ZoomStep.Value - 1),
                () => c.ZoomStep.Value = Mathf.Min(6, c.ZoomStep.Value + 1));
            StepperRow("FIT also sends", KeyNames.Label(c.ZoomResetKey.Value),
                () => c.ZoomResetKey.Value = Cycle(c.ZoomResetKey.Value, -1),
                () => c.ZoomResetKey.Value = Cycle(c.ZoomResetKey.Value, 1), "<", ">");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move buttons", Styles.Button, H(8))) Defer(hud.StartEditLayout);
            if (GUILayout.Button("Reset positions", Styles.Button, H(8))) Defer(hud.ResetLayout);
            GUILayout.EndHorizontal();
            Note("Pinch and two-finger gestures come from the emulator's own touch settings. The + / - buttons always work "
                 + "because they send mouse-wheel input to the game.");

            SectionLabel("Speed-die snap");
            ToggleRow("Bigger touch area on speed dice", c.DieSnap);
            StepperRow("Extra touch area", c.DieSnapPaddingPx.Value + " px",
                () => c.DieSnapPaddingPx.Value = Mathf.Max(0, c.DieSnapPaddingPx.Value - 4),
                () => c.DieSnapPaddingPx.Value = Mathf.Min(120, c.DieSnapPaddingPx.Value + 4));
            StepperRow("Collider size", "x" + c.DieSnapColliderScale.Value.ToString("0.00"),
                () => c.DieSnapColliderScale.Value = Mathf.Max(1f, Round(c.DieSnapColliderScale.Value - 0.05f, 20f)),
                () => c.DieSnapColliderScale.Value = Mathf.Min(3f, Round(c.DieSnapColliderScale.Value + 0.05f, 20f)));
            GUILayout.Label("Name keywords (comma separated)", Styles.Label, RowHeight());
            string current = c.DieSnapKeywords.Value ?? "";
            string edited = GUILayout.TextField(current, 200, Styles.Field, H(8));
            if (edited != current) Defer(() => c.DieSnapKeywords.Value = edited);
            Note(DieSnap.Status + ". 'SpeedDice' is a starting guess at the game's object names; "
                 + "confirm it with the touch inspector below.");
            ToggleRow("Touch inspector", c.Inspector);
            Note("Shows the names of whatever you tap at the bottom of the screen (also saved to BepInEx/LogOutput.log).");
        }

        private void DrawMods()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", Styles.Button, W(26), H(8))) Defer(ModIndex.Rescan);
            GUILayout.Label("   Last check: " + ModIndex.LastScan + "   |   " + ModIndex.Mods.Count + " folder(s)", Styles.Label, RowHeight());
            GUILayout.EndHorizontal();
            Note("Library of Ruina loads mods itself: switch them on/off and set their order in the game's Mods menu on the "
                 + "title screen. This list checks that the files are where the game looks for them.");

            SectionLabel("Folders checked");
            foreach (var f in ModIndex.Folders)
                Note((f.Exists ? "[found]   " : "[missing] ") + f.Label + "   " + f.FullPath);

            SectionLabel("Mods");
            if (ModIndex.Mods.Count == 0)
                Note("No mod folders found. Copy each mod's folder into LibraryOfRuina_Data\\Mods, then tap Rescan.");

            foreach (var m in ModIndex.Mods)
            {
                bool ok = string.IsNullOrEmpty(m.problem);
                string name = string.IsNullOrEmpty(m.name) ? m.folder : m.name;
                GUILayout.Label((ok ? "OK    " : "!!    ") + name, Styles.Label);

                var details = new StringBuilder();
                details.Append(m.source).Append("  |  folder '").Append(m.folder).Append('\'');
                if (!string.IsNullOrEmpty(m.id)) details.Append("  |  id ").Append(m.id);
                if (!string.IsNullOrEmpty(m.version)) details.Append("  |  v").Append(m.version);
                details.Append("  |  ").Append(m.assemblies != null ? m.assemblies.Length : 0).Append(" DLL(s)");
                if (!ok) details.Append('\n').Append(m.problem);
                Note(details.ToString());
            }
        }

        private void DrawInfo()
        {
            SectionLabel("Folders");
            Note("Game folder: " + Paths.GameRootPath);
            Note("Game data: " + Application.dataPath);
            Note("Save files: " + Application.persistentDataPath);
            Note("Settings file: " + RuinaMobilePlugin.ConfigFilePath);
            Note("Log file: " + Path.Combine(Paths.BepInExRootPath, "LogOutput.log"));
            Note("Mod check cache: " + ModIndex.CachePath);

            SectionLabel("System");
            Note("Unity " + Application.unityVersion + "  |  " + SystemInfo.graphicsDeviceType + "  |  " + SystemInfo.graphicsDeviceName);
            Note("Screen " + Screen.width + "x" + Screen.height + " @ " + Screen.currentResolution.refreshRate + " Hz  |  "
                 + SystemInfo.processorCount + " CPU threads  |  " + SystemInfo.systemMemorySize + " MB RAM");
            Note("Engine patches: " + PatchUtil.Applied + " applied, " + PatchUtil.Failed
                 + " skipped (skipped ones are named in the log).");

            SectionLabel("About");
            Note("Ruina Mobile runs inside the PC version of Library of Ruina under Wine / Box64 and contains no game files. "
                 + "Library of Ruina belongs to Project Moon.");
        }

        // ---------------------------------------------------------------- rows

        private void SectionLabel(string text)
        {
            GUILayout.Label(text, Styles.Section);
        }

        private void Note(string text)
        {
            GUILayout.Label(text, Styles.Small);
        }

        private void ToggleRow(string label, ConfigEntry<bool> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Styles.Label, GUILayout.ExpandWidth(true), RowHeight());
            bool v = entry.Value;
            if (GUILayout.Button(v ? "ON" : "OFF", v ? Styles.ButtonOn : Styles.Button, W(18), H(8)))
                Defer(() => entry.Value = !v);
            GUILayout.EndHorizontal();
        }

        private void StepperRow(string label, string value, Action minus, Action plus, string minusText = "-", string plusText = "+")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Styles.Label, GUILayout.ExpandWidth(true), RowHeight());
            if (GUILayout.Button(minusText, Styles.Button, W(10), H(8))) Defer(minus);
            GUILayout.Label(value, Styles.Value, W(26), H(8));
            if (GUILayout.Button(plusText, Styles.Button, W(10), H(8))) Defer(plus);
            GUILayout.EndHorizontal();
        }

        private void ChoiceRow<T>(IList<T> options, T current, Func<T, string> name, Action<T> select)
        {
            GUILayout.BeginHorizontal();
            foreach (var option in options)
            {
                var chosen = option;
                bool on = EqualityComparer<T>.Default.Equals(option, current);
                if (GUILayout.Button(name(option), on ? Styles.ButtonOn : Styles.Button, H(8)))
                    Defer(() => select(chosen));
            }

            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------- helpers

        private static void SetCustom(float scale, bool light)
        {
            var c = Cfg;
            c.CustomStageScale.Value = Mathf.Clamp(Mathf.Round(scale * 20f) / 20f, 0.6f, 1f);
            c.CustomLightRender.Value = light;
            c.Preset.Value = GraphicsPreset.Custom;
        }

        private static KeyCode Cycle(KeyCode current, int direction)
        {
            int i = Array.IndexOf(KeyChoices, current);
            i = i < 0 ? 0 : (i + direction + KeyChoices.Length) % KeyChoices.Length;
            return KeyChoices[i];
        }

        private static float Round(float value, float steps)
        {
            return Mathf.Round(value * steps) / steps;
        }

        private static string Pct(float value)
        {
            return Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
