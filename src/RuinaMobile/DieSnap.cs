using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// "Magnetic" speed dice: gives every object whose name matches SpeedDieKeywords a bigger
    /// touch area, so dragging a page onto a 5v5 enemy die does not miss.
    ///  - uGUI elements get an invisible child Image that is larger than the element. Pointer
    ///    events on it bubble up to the die's own handler, so the game logic is unchanged.
    ///  - 2D / 3D colliders get their size multiplied (original sizes are restored when turned off).
    /// </summary>
    internal sealed class DieSnap : MonoBehaviour
    {
        internal static string Status = "Off";
        private const string PadName = "RuinaMobile_SnapPad";

        private sealed class ColliderState
        {
            public Component Collider;
            public Vector3 Size;
            public float Radius;
        }

        private readonly List<GameObject> pads = new List<GameObject>();
        private readonly Dictionary<int, ColliderState> colliders = new Dictionary<int, ColliderState>();
        private PropertyInfo raycastTargetProp;
        private PropertyInfo colorProp;
        private PropertyInfo ignoreLayoutProp;
        private float nextScan;
        private bool active;
        private string appliedKeywords = "";
        private int appliedPadding = -1;
        private float appliedColliderScale = -1f;
        private bool loggedPadError;

        private static Settings Cfg
        {
            get { return RuinaMobilePlugin.Cfg; }
        }

        private void Start()
        {
            if (UiReflection.GraphicType != null)
            {
                raycastTargetProp = UiReflection.GraphicType.GetProperty("raycastTarget");
                colorProp = UiReflection.GraphicType.GetProperty("color");
            }

            if (UiReflection.LayoutElementType != null)
                ignoreLayoutProp = UiReflection.LayoutElementType.GetProperty("ignoreLayout");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RemoveAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextScan = 0f;
        }

        private void Update()
        {
            var cfg = Cfg;
            if (!cfg.DieSnap.Value)
            {
                if (active)
                {
                    RemoveAll();
                    active = false;
                }

                Status = "Off";
                return;
            }

            active = true;
            if (cfg.DieSnapKeywords.Value != appliedKeywords || cfg.DieSnapPaddingPx.Value != appliedPadding
                || !Mathf.Approximately(cfg.DieSnapColliderScale.Value, appliedColliderScale))
            {
                RemoveAll();
                appliedKeywords = cfg.DieSnapKeywords.Value;
                appliedPadding = cfg.DieSnapPaddingPx.Value;
                appliedColliderScale = cfg.DieSnapColliderScale.Value;
                nextScan = 0f;
            }

            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 2f;
            Scan();
        }

        private string[] Keywords()
        {
            return (Cfg.DieSnapKeywords.Value ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .ToArray();
        }

        private static bool Matches(string name, string[] keywords)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < keywords.Length; i++)
                if (name.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void Scan()
        {
            var keywords = Keywords();
            if (keywords.Length == 0)
            {
                Status = "No keywords set";
                return;
            }

            pads.RemoveAll(p => p == null);

            if (UiReflection.GraphicType != null && UiReflection.ImageType != null && Cfg.DieSnapPaddingPx.Value > 0)
            {
                var done = new HashSet<int>();
                foreach (var obj in FindObjectsOfType(UiReflection.GraphicType))
                {
                    var graphic = obj as Component;
                    if (graphic == null) continue;
                    var go = graphic.gameObject;
                    if (go.name == PadName || !Matches(go.name, keywords)) continue;

                    // Pad the outermost matching ancestor once (e.g. "SpeedDiceSlot" rather than its child image).
                    Transform target = go.transform;
                    for (var t = target.parent; t != null && Matches(t.name, keywords); t = t.parent) target = t;
                    var rect = target as RectTransform;
                    if (rect == null || !done.Add(rect.GetInstanceID())) continue;
                    if (rect.Find(PadName) != null) continue;
                    CreatePad(rect);
                }
            }

            float factor = Cfg.DieSnapColliderScale.Value;
            if (factor > 1.001f)
            {
                foreach (var c in FindObjectsOfType<Collider2D>()) ConsiderCollider(c, keywords, factor);
                foreach (var c in FindObjectsOfType<Collider>()) ConsiderCollider(c, keywords, factor);
            }

            Status = pads.Count + " UI touch pad(s), " + colliders.Count + " collider(s) enlarged";
        }

        private void CreatePad(RectTransform target)
        {
            try
            {
                var canvas = target.GetComponentInParent<Canvas>();
                if (canvas == null) return;
                var root = canvas.rootCanvas;

                float padPx = Cfg.DieSnapPaddingPx.Value * (Screen.height / 1080f);
                float local;
                if (root.renderMode == RenderMode.WorldSpace)
                {
                    var r = target.rect;
                    local = Mathf.Max(r.width, r.height) * 0.25f * (Cfg.DieSnapPaddingPx.Value / 28f);
                }
                else
                {
                    float rootScale = root.transform.lossyScale.x;
                    float relative = Mathf.Abs(rootScale) > 1e-6f ? target.lossyScale.x / rootScale : 1f;
                    float pixelsPerUnit = root.scaleFactor * relative;
                    local = pixelsPerUnit > 1e-4f ? padPx / pixelsPerUnit : padPx;
                }

                var go = new GameObject(PadName, typeof(RectTransform));
                go.layer = target.gameObject.layer;
                var pad = (RectTransform)go.transform;
                pad.SetParent(target, false);
                pad.anchorMin = Vector2.zero;
                pad.anchorMax = Vector2.one;
                pad.pivot = new Vector2(0.5f, 0.5f);
                pad.offsetMin = new Vector2(-local, -local);
                pad.offsetMax = new Vector2(local, local);
                pad.SetAsFirstSibling();

                if (UiReflection.LayoutElementType != null && ignoreLayoutProp != null)
                {
                    var layout = go.AddComponent(UiReflection.LayoutElementType);
                    ignoreLayoutProp.SetValue(layout, true, null);
                }

                var image = go.AddComponent(UiReflection.ImageType);
                if (colorProp != null) colorProp.SetValue(image, new Color(0f, 0f, 0f, 0f), null);
                if (raycastTargetProp != null) raycastTargetProp.SetValue(image, true, null);
                pads.Add(go);
            }
            catch (Exception e)
            {
                if (!loggedPadError)
                {
                    loggedPadError = true;
                    RuinaMobilePlugin.Log.LogWarning("Speed-die snap could not add a touch pad: " + e.Message);
                }
            }
        }

        private void ConsiderCollider(Component c, string[] keywords, float factor)
        {
            int id = c.GetInstanceID();
            if (colliders.ContainsKey(id)) return;

            var t = c.transform;
            bool match = Matches(t.name, keywords)
                         || (t.parent != null && Matches(t.parent.name, keywords))
                         || (t.parent != null && t.parent.parent != null && Matches(t.parent.parent.name, keywords));
            if (!match) return;

            var state = new ColliderState { Collider = c };
            switch (c)
            {
                case BoxCollider2D box2D:
                    state.Size = box2D.size;
                    box2D.size = box2D.size * factor;
                    break;
                case CircleCollider2D circle:
                    state.Radius = circle.radius;
                    circle.radius = circle.radius * factor;
                    break;
                case CapsuleCollider2D capsule:
                    state.Size = capsule.size;
                    capsule.size = capsule.size * factor;
                    break;
                case BoxCollider box:
                    state.Size = box.size;
                    box.size = box.size * factor;
                    break;
                case SphereCollider sphere:
                    state.Radius = sphere.radius;
                    sphere.radius = sphere.radius * factor;
                    break;
                default:
                    return;
            }

            colliders[id] = state;
        }

        private void RemoveAll()
        {
            foreach (var pad in pads)
                if (pad != null) Destroy(pad);
            pads.Clear();

            foreach (var state in colliders.Values)
            {
                var c = state.Collider;
                if (c == null) continue;
                switch (c)
                {
                    case BoxCollider2D box2D: box2D.size = state.Size; break;
                    case CircleCollider2D circle: circle.radius = state.Radius; break;
                    case CapsuleCollider2D capsule: capsule.size = state.Size; break;
                    case BoxCollider box: box.size = state.Size; break;
                    case SphereCollider sphere: sphere.radius = state.Radius; break;
                }
            }

            colliders.Clear();
        }
    }
}
