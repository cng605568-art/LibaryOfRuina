using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// Split-resolution rendering. Cameras that draw the world (battle stage, characters, effects)
    /// are redirected into one RenderTexture at the chosen scale; a presenter camera stretches it
    /// to the screen with bilinear filtering; screen-space UI then draws on top at native resolution.
    ///
    /// Safety rules: a camera that also renders a Screen Space - Camera canvas is never scaled
    /// (its UI would go blurry), and if any other screen camera draws before the stage, scaling is
    /// skipped for that scene (the stretched stage would cover it). The reason shows in Settings.
    /// </summary>
    internal sealed class StageScaler : MonoBehaviour
    {
        private static readonly HashSet<int> ScaledIds = new HashSet<int>();
        internal static float ScaleX = 1f;
        internal static float ScaleY = 1f;
        internal static string Status = "Off (100%)";

        internal static bool IsScaled(Camera cam)
        {
            if (ScaledIds.Count == 0 || ReferenceEquals(cam, null)) return false;
            return ScaledIds.Contains(cam.GetInstanceID());
        }

        private const int UiLayerMask = 1 << 5;
        private static readonly Rect FullViewport = new Rect(0f, 0f, 1f, 1f);

        private readonly Dictionary<int, Camera> redirected = new Dictionary<int, Camera>();
        private readonly HashSet<Camera> canvasCameras = new HashSet<Camera>();
        private readonly List<Camera> candidates = new List<Camera>();
        private readonly List<int> drop = new List<int>();
        private Camera presenter;
        private RenderTexture stageTexture;
        private float appliedScale = 1f;
        private int appliedWidth;
        private int appliedHeight;
        private float nextScan;
        private float nextCanvasScan;

        internal RenderTexture StageTexture
        {
            get { return stageTexture; }
        }

        private void Awake()
        {
            var go = new GameObject("RuinaMobile_StagePresenter");
            go.transform.SetParent(transform, false);
            presenter = go.AddComponent<Camera>();
            presenter.cullingMask = 0;
            presenter.clearFlags = CameraClearFlags.SolidColor;
            presenter.backgroundColor = Color.black;
            presenter.orthographic = true;
            presenter.useOcclusionCulling = false;
            presenter.allowHDR = false;
            presenter.allowMSAA = false;
            presenter.enabled = false;
            go.AddComponent<StagePresenter>().Owner = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreAll("Stopped");
            ReleaseTexture();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextScan = 0f;
            nextCanvasScan = 0f;
        }

        private void LateUpdate()
        {
            float scale = RuinaMobilePlugin.Cfg.EffectiveStageScale;
            if (scale >= 0.995f)
            {
                if (redirected.Count > 0 || stageTexture != null)
                {
                    RestoreAll("Off (100%)");
                    ReleaseTexture();
                }
                else
                {
                    Status = "Off (100%)";
                }

                appliedScale = 1f;
                return;
            }

            if (stageTexture == null || !Mathf.Approximately(scale, appliedScale)
                || Screen.width != appliedWidth || Screen.height != appliedHeight)
            {
                appliedScale = scale;
                appliedWidth = Screen.width;
                appliedHeight = Screen.height;
                RebuildTexture(scale);
                nextScan = 0f;
            }

            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.25f;
                Scan();
            }
        }

        private void Scan()
        {
            float now = Time.unscaledTime;
            if (now >= nextCanvasScan)
            {
                nextCanvasScan = now + 2f;
                canvasCameras.Clear();
                foreach (var canvas in FindObjectsOfType<Canvas>())
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
                        canvasCameras.Add(canvas.worldCamera);
                }
            }

            // Let go of cameras that were destroyed, disabled, re-targeted by the game, or now carry UI.
            drop.Clear();
            foreach (var kv in redirected)
            {
                var cam = kv.Value;
                if (cam == null || !cam.isActiveAndEnabled || cam.targetTexture != stageTexture || canvasCameras.Contains(cam))
                    drop.Add(kv.Key);
            }

            foreach (int id in drop)
            {
                var cam = redirected[id];
                if (cam != null && cam.targetTexture == stageTexture) cam.targetTexture = null;
                redirected.Remove(id);
                ScaledIds.Remove(id);
            }

            candidates.Clear();
            float maxStageDepth = float.MinValue;
            var cams = Camera.allCameras;
            foreach (var cam in cams)
            {
                if (cam == presenter) continue;
                if (!redirected.ContainsKey(cam.GetInstanceID()))
                {
                    if (cam.targetTexture != null || cam.targetDisplay != 0 || cam.rect != FullViewport) continue;
                    if (canvasCameras.Contains(cam)) continue;
                    if ((cam.cullingMask & ~UiLayerMask) == 0) continue;
                }

                candidates.Add(cam);
                if (cam.depth > maxStageDepth) maxStageDepth = cam.depth;
            }

            if (candidates.Count == 0)
            {
                presenter.enabled = false;
                Status = "Waiting: no stage camera in this scene";
                return;
            }

            float minOtherDepth = float.MaxValue;
            foreach (var cam in cams)
            {
                if (cam == presenter || candidates.Contains(cam)) continue;
                if (cam.targetTexture != null || cam.targetDisplay != 0) continue;
                if (cam.depth <= maxStageDepth)
                {
                    RestoreAll("Skipped here: camera '" + cam.name + "' draws before the stage, scaling would hide it");
                    return;
                }

                if (cam.depth < minOtherDepth) minOtherDepth = cam.depth;
            }

            foreach (var cam in candidates)
            {
                int id = cam.GetInstanceID();
                if (redirected.ContainsKey(id)) continue;
                redirected[id] = cam;
                ScaledIds.Add(id);
                cam.targetTexture = stageTexture;
                RuinaMobilePlugin.Log.LogInfo("Stage scale: rendering camera '" + cam.name + "' at "
                                              + stageTexture.width + "x" + stageTexture.height);
            }

            float gap = minOtherDepth == float.MaxValue ? 1f : Mathf.Min(1f, (minOtherDepth - maxStageDepth) * 0.5f);
            presenter.depth = maxStageDepth + gap;
            presenter.enabled = true;
            Status = "Active on " + redirected.Count + " camera(s), " + stageTexture.width + "x" + stageTexture.height;
        }

        private void RebuildTexture(float scale)
        {
            int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * scale));
            int h = Mathf.Max(64, Mathf.RoundToInt(Screen.height * scale));
            var old = stageTexture;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
            rt.name = "RuinaMobile_Stage";
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            rt.Create();

            stageTexture = rt;
            ScaleX = w / (float)Screen.width;
            ScaleY = h / (float)Screen.height;
            foreach (var cam in redirected.Values)
                if (cam != null) cam.targetTexture = rt;

            if (old != null)
            {
                old.Release();
                Destroy(old);
            }
        }

        private void RestoreAll(string status)
        {
            foreach (var cam in redirected.Values)
                if (cam != null && cam.targetTexture == stageTexture) cam.targetTexture = null;
            redirected.Clear();
            ScaledIds.Clear();
            if (presenter != null) presenter.enabled = false;
            Status = status;
        }

        private void ReleaseTexture()
        {
            if (stageTexture != null)
            {
                stageTexture.Release();
                Destroy(stageTexture);
                stageTexture = null;
            }

            ScaleX = 1f;
            ScaleY = 1f;
        }
    }

    /// <summary>Draws the scaled stage texture onto the screen, before any UI camera or overlay canvas.</summary>
    internal sealed class StagePresenter : MonoBehaviour
    {
        internal StageScaler Owner;
        private Material copyMaterial;
        private bool lookedUpShader;

        private void OnPostRender()
        {
            var tex = Owner != null ? Owner.StageTexture : null;
            if (tex == null) return;

            if (!lookedUpShader)
            {
                lookedUpShader = true;
                // Opaque copy so the stage's alpha channel cannot darken the image.
                var shader = Shader.Find("Hidden/BlitCopy");
                if (shader != null) copyMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                else RuinaMobilePlugin.Log.LogWarning("Hidden/BlitCopy not found; presenting the stage with the GUI shader.");
            }

            var source = RuinaMobilePlugin.Cfg.StageFlipY.Value ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            Graphics.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), tex, source, 0, 0, 0, 0, copyMaterial, -1);
            GL.PopMatrix();
        }
    }
}
