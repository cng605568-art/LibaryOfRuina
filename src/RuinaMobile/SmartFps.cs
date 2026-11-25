using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// Thermal-aware frame pacing. Runs at IdleFps while the screen is static (story text,
    /// deckbuilding, menus) and at CombatFps while something is happening. "Something" is detected
    /// without knowing the game's internals: a touch / key / wheel, the main camera moving or
    /// zooming (clash camera work), lots of live particles, or a scene name the user listed.
    /// </summary>
    internal sealed class SmartFps : MonoBehaviour
    {
        internal static string Status = "";

        private bool managing;
        private int savedVSync;
        private int savedTarget;

        private float lastInput = -100f;
        private float lastMotion = -100f;
        private float lastFx = -100f;
        private Vector3 lastMouse;
        private Camera lastCam;
        private Vector3 lastCamPos;
        private float lastCamZoom;
        private ParticleSystem[] effects = new ParticleSystem[0];
        private float nextFxScan;
        private float nextFxCheck;
        private float nextStatus;
        private string sceneName = "";

        private void Awake()
        {
            sceneName = SceneManager.GetActiveScene().name ?? "";
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            sceneName = to.name ?? "";
            nextFxScan = 0f;
        }

        private void Update()
        {
            var cfg = RuinaMobilePlugin.Cfg;
            float now = Time.unscaledTime;

            if (!cfg.SmartFps.Value)
            {
                if (managing)
                {
                    QualitySettings.vSyncCount = savedVSync;
                    Application.targetFrameRate = savedTarget;
                    managing = false;
                }

                Status = "Off: the game controls its own frame rate";
                return;
            }

            if (!managing)
            {
                managing = true;
                savedVSync = QualitySettings.vSyncCount;
                savedTarget = Application.targetFrameRate;
            }

            if (ReadInput()) lastInput = now;
            TrackCamera(now);
            TrackEffects(now, cfg.EffectsParticleThreshold.Value);

            float hold = Mathf.Max(0.2f, cfg.BoostHoldSeconds.Value);
            string reason = null;
            if (MatchesAny(sceneName, cfg.BattleSceneKeywords.Value)) reason = "battle scene";
            else if (now - lastInput < hold) reason = "touch";
            else if (now - lastMotion < hold) reason = "camera moving";
            else if (now - lastFx < hold) reason = "effects";

            int combat = cfg.CombatFps.Value > 0 ? cfg.CombatFps.Value : NativeRefresh();
            int idle = Mathf.Clamp(cfg.IdleFps.Value, 10, combat);
            int target = reason != null ? combat : idle;

            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != target) Application.targetFrameRate = target;

            if (now >= nextStatus)
            {
                nextStatus = now + 0.5f;
                Status = target + " FPS cap (" + (reason ?? "idle") + "), scene '" + sceneName + "'";
            }
        }

        private bool ReadInput()
        {
            bool previous = PointerGate.Bypass;
            PointerGate.Bypass = true;
            try
            {
                Vector3 m = Input.mousePosition;
                bool moved = (m - lastMouse).sqrMagnitude > 4f;
                lastMouse = m;
                return moved || Input.anyKey || Input.GetMouseButton(0) || Input.GetMouseButton(1)
                       || Input.mouseScrollDelta.sqrMagnitude > 0f;
            }
            finally
            {
                PointerGate.Bypass = previous;
            }
        }

        private void TrackCamera(float now)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                lastCam = null;
                return;
            }

            Vector3 p = cam.transform.position;
            float zoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
            if (cam == lastCam && ((p - lastCamPos).sqrMagnitude > 1e-5f || Mathf.Abs(zoom - lastCamZoom) > 1e-4f))
                lastMotion = now;

            lastCam = cam;
            lastCamPos = p;
            lastCamZoom = zoom;
        }

        private void TrackEffects(float now, int threshold)
        {
            if (threshold <= 0) return;
            if (now >= nextFxScan)
            {
                nextFxScan = now + 3f;
                effects = FindObjectsOfType<ParticleSystem>();
            }

            if (now < nextFxCheck) return;
            nextFxCheck = now + 0.25f;

            int live = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                var ps = effects[i];
                if (ps == null || !ps.isPlaying) continue;
                live += ps.particleCount;
                if (live >= threshold)
                {
                    lastFx = now;
                    return;
                }
            }
        }

        private static bool MatchesAny(string text, string csv)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(csv)) return false;
            foreach (var raw in csv.Split(','))
            {
                var k = raw.Trim();
                if (k.Length > 0 && text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        private static int NativeRefresh()
        {
            int r = Screen.currentResolution.refreshRate;
            return r >= 30 ? r : 60;
        }
    }
}
