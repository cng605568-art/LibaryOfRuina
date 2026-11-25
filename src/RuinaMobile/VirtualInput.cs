using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// Key presses produced by the on-screen buttons. InputPatches feeds these into
    /// Input.GetKeyDown / GetKey / GetKeyUp, so the game sees them like real keys.
    /// </summary>
    internal static class VirtualKeys
    {
        private struct Press
        {
            public KeyCode Key;
            public string Name;
            public int Down;
            public int Up;
        }

        private static readonly List<Press> Presses = new List<Press>();
        private static int cleanedFrame = -1;

        internal static void Tap(KeyCode key, int holdFrames = 3)
        {
            if (key == KeyCode.None) return;
            int f = Time.frameCount + 1;
            Presses.Add(new Press { Key = key, Name = KeyNames.Of(key), Down = f, Up = f + Math.Max(1, holdFrames) });
        }

        private static bool AnyActive()
        {
            if (Presses.Count == 0) return false;
            int f = Time.frameCount;
            if (f != cleanedFrame)
            {
                cleanedFrame = f;
                Presses.RemoveAll(p => p.Up < f);
            }
            return Presses.Count > 0;
        }

        internal static bool IsDown(KeyCode k)
        {
            if (!AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (Presses[i].Key == k && Presses[i].Down == f) return true;
            return false;
        }

        internal static bool IsHeld(KeyCode k)
        {
            if (!AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (Presses[i].Key == k && f >= Presses[i].Down && f < Presses[i].Up) return true;
            return false;
        }

        internal static bool IsUp(KeyCode k)
        {
            if (!AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (Presses[i].Key == k && Presses[i].Up == f) return true;
            return false;
        }

        internal static bool IsDown(string name)
        {
            if (name == null || !AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (Presses[i].Down == f && string.Equals(Presses[i].Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static bool IsHeld(string name)
        {
            if (name == null || !AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (f >= Presses[i].Down && f < Presses[i].Up && string.Equals(Presses[i].Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static bool IsUp(string name)
        {
            if (name == null || !AnyActive()) return false;
            int f = Time.frameCount;
            for (int i = 0; i < Presses.Count; i++)
                if (Presses[i].Up == f && string.Equals(Presses[i].Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    /// <summary>Mouse-wheel notches produced by the + / - zoom buttons.</summary>
    internal static class VirtualScroll
    {
        private static int startFrame = -1;
        private static int frames;
        private static int direction;

        internal static void Add(int notches)
        {
            if (notches == 0) return;
            startFrame = Time.frameCount + 1;
            frames = Math.Abs(notches);
            direction = Math.Sign(notches);
        }

        /// <summary>+1 / -1 on the frames where a virtual wheel notch is active, otherwise 0.</summary>
        internal static float Notches
        {
            get
            {
                int f = Time.frameCount;
                return f >= startFrame && f < startFrame + frames ? direction : 0f;
            }
        }
    }

    /// <summary>
    /// Decides when a touch belongs to Ruina Mobile's own buttons or settings panel, so the game
    /// underneath does not also react to it. A press that starts on a button stays "captured"
    /// until the finger lifts; a press that starts on the game is never blocked.
    /// </summary>
    internal static class PointerGate
    {
        /// <summary>True while Ruina Mobile reads raw input itself, so its own patches stay transparent.</summary>
        internal static bool Bypass;

        /// <summary>Settings panel or button-layout editor is open: the game gets no pointer input.</summary>
        internal static bool ModalOpen;

        /// <summary>Screen position (Unity bottom-left origin) -> is it over one of our buttons?</summary>
        internal static Func<Vector2, bool> HitTest;

        private static int frame = -1;
        private static bool captured;

        internal static bool BlockGameButtons
        {
            get
            {
                Refresh();
                return ModalOpen || captured;
            }
        }

        internal static bool BlockRaycasts
        {
            get
            {
                Refresh();
                return ModalOpen || captured || OverHud();
            }
        }

        private static bool OverHud()
        {
            var test = HitTest;
            return test != null && test(Input.mousePosition);
        }

        private static void Refresh()
        {
            int f = Time.frameCount;
            if (f == frame) return;
            frame = f;

            bool previous = Bypass;
            Bypass = true;
            try
            {
                bool down = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
                bool held = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                bool up = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
                if (down) captured = OverHud();
                else if (!held && !up) captured = false;
            }
            finally
            {
                Bypass = previous;
            }
        }
    }

    internal static class KeyNames
    {
        /// <summary>The name Unity's Input.GetKey(string) uses for a key.</summary>
        internal static string Of(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.Space: return "space";
                case KeyCode.Escape: return "escape";
                case KeyCode.Return: return "return";
                case KeyCode.KeypadEnter: return "enter";
                case KeyCode.Tab: return "tab";
                case KeyCode.Backspace: return "backspace";
                case KeyCode.LeftShift: return "left shift";
                case KeyCode.RightShift: return "right shift";
                case KeyCode.LeftControl: return "left ctrl";
                case KeyCode.RightControl: return "right ctrl";
                case KeyCode.LeftAlt: return "left alt";
                case KeyCode.RightAlt: return "right alt";
                case KeyCode.UpArrow: return "up";
                case KeyCode.DownArrow: return "down";
                case KeyCode.LeftArrow: return "left";
                case KeyCode.RightArrow: return "right";
            }

            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
            return k.ToString().ToLowerInvariant();
        }

        /// <summary>Short label for a button face.</summary>
        internal static string Label(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.None: return "OFF";
                case KeyCode.Space: return "SPACE";
                case KeyCode.Escape: return "ESC";
                case KeyCode.Return: return "ENTER";
                case KeyCode.Tab: return "TAB";
            }

            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
            return k.ToString().ToUpperInvariant();
        }
    }
}
