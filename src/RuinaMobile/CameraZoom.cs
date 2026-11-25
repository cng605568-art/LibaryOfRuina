using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinaMobile
{
    /// <summary>
    /// FIT button: remembers where the main camera sat shortly after it appeared and puts it back.
    /// If the game's own camera controller overrides that every frame, set Hud.FitButtonKey to the
    /// game's camera-reset key instead (the button sends both).
    /// </summary>
    internal sealed class CameraZoom : MonoBehaviour
    {
        internal static CameraZoom Instance;

        private Camera tracked;
        private bool hasSnapshot;
        private float snapshotAt;
        private Vector3 position;
        private float orthoSize;
        private float fieldOfView;

        private void Awake()
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            tracked = null;
            hasSnapshot = false;
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != tracked)
            {
                tracked = cam;
                hasSnapshot = false;
                snapshotAt = Time.unscaledTime + 1.5f;
            }

            if (!hasSnapshot && cam != null && Time.unscaledTime >= snapshotAt)
            {
                position = cam.transform.localPosition;
                orthoSize = cam.orthographicSize;
                fieldOfView = cam.fieldOfView;
                hasSnapshot = true;
            }
        }

        internal void ResetZoom()
        {
            VirtualKeys.Tap(RuinaMobilePlugin.Cfg.ZoomResetKey.Value);
            if (!hasSnapshot || tracked == null)
            {
                if (Hud.Instance != null) Hud.Instance.Toast("No camera to reset yet");
                return;
            }

            tracked.transform.localPosition = position;
            if (tracked.orthographic) tracked.orthographicSize = orthoSize;
            else tracked.fieldOfView = fieldOfView;
        }
    }
}
