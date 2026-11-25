using UnityEngine;

namespace RuinaMobile
{
    /// <summary>
    /// "Light render settings" for weaker GPUs: switches off the quality features that cost the most
    /// under DXVK / WineD3D on phones, and puts the game's own values back when turned off again.
    /// </summary>
    internal sealed class GraphicsController : MonoBehaviour
    {
        private bool lightApplied;
        private int savedAntiAliasing;
        private AnisotropicFiltering savedAniso;
        private bool savedSoftParticles;
        private bool savedReflectionProbes;

        private void Update()
        {
            bool want = RuinaMobilePlugin.Cfg.EffectiveLightRender;
            if (want == lightApplied) return;

            if (want)
            {
                savedAntiAliasing = QualitySettings.antiAliasing;
                savedAniso = QualitySettings.anisotropicFiltering;
                savedSoftParticles = QualitySettings.softParticles;
                savedReflectionProbes = QualitySettings.realtimeReflectionProbes;

                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                RuinaMobilePlugin.Log.LogInfo("Light render settings on (MSAA, anisotropic filtering, soft particles, reflection probes off).");
            }
            else
            {
                QualitySettings.antiAliasing = savedAntiAliasing;
                QualitySettings.anisotropicFiltering = savedAniso;
                QualitySettings.softParticles = savedSoftParticles;
                QualitySettings.realtimeReflectionProbes = savedReflectionProbes;
                RuinaMobilePlugin.Log.LogInfo("Light render settings off (game values restored).");
            }

            lightApplied = want;
        }
    }
}
