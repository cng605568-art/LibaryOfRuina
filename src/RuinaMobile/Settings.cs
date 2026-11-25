using BepInEx.Configuration;
using UnityEngine;

namespace RuinaMobile
{
    public enum GraphicsPreset
    {
        Auto,
        Performance,
        Balanced,
        Quality,
        Custom
    }

    /// <summary>
    /// Every user setting, stored by BepInEx in BepInEx/config/com.cng605568.ruinamobile.cfg.
    /// The in-game settings panel edits these entries; the file can also be edited by hand.
    /// </summary>
    internal sealed class Settings
    {
        // Graphics
        public readonly ConfigEntry<GraphicsPreset> Preset;
        public readonly ConfigEntry<float> CustomStageScale;
        public readonly ConfigEntry<bool> CustomLightRender;
        public readonly ConfigEntry<bool> StageFlipY;

        // Frame pacing
        public readonly ConfigEntry<bool> SmartFps;
        public readonly ConfigEntry<int> IdleFps;
        public readonly ConfigEntry<int> CombatFps;
        public readonly ConfigEntry<float> BoostHoldSeconds;
        public readonly ConfigEntry<int> EffectsParticleThreshold;
        public readonly ConfigEntry<string> BattleSceneKeywords;

        // On-screen buttons
        public readonly ConfigEntry<bool> HudEnabled;
        public readonly ConfigEntry<float> HudScale;
        public readonly ConfigEntry<float> HudOpacity;
        public readonly ConfigEntry<KeyCode> Button1Key;
        public readonly ConfigEntry<KeyCode> Button2Key;
        public readonly ConfigEntry<bool> ZoomButtons;
        public readonly ConfigEntry<int> ZoomStep;
        public readonly ConfigEntry<KeyCode> ZoomResetKey;
        public readonly ConfigEntry<string> HudLayoutString;

        // Touch helpers
        public readonly ConfigEntry<bool> DieSnap;
        public readonly ConfigEntry<int> DieSnapPaddingPx;
        public readonly ConfigEntry<float> DieSnapColliderScale;
        public readonly ConfigEntry<string> DieSnapKeywords;
        public readonly ConfigEntry<bool> Inspector;

        // Mods
        public readonly ConfigEntry<string> ExtraModFolders;

        public Settings(ConfigFile cfg)
        {
            Preset = cfg.Bind("Graphics", "Preset", GraphicsPreset.Auto,
                "Auto: Adreno or unrecognised GPU = 100% stage + full render settings; Mali / Xclipse / PowerVR = 80% stage + light render settings. " +
                "Performance = 70% stage + light. Balanced = 85% stage + full. Quality = 100% + full. Custom = the two Custom* values below.");
            CustomStageScale = cfg.Bind("Graphics", "CustomStageScale", 0.9f, new ConfigDescription(
                "Battle-stage render scale used when Preset = Custom. Menus, cards and dice text always stay at native resolution.",
                new AcceptableValueRange<float>(0.6f, 1f)));
            CustomLightRender = cfg.Bind("Graphics", "CustomLightRender", false,
                "Used when Preset = Custom. Light render settings turn off MSAA, anisotropic filtering, soft particles and realtime reflection probes.");
            StageFlipY = cfg.Bind("Graphics", "StageFlipY", false,
                "Only turn this on if the battle stage appears upside down while the stage scale is below 100%.");

            SmartFps = cfg.Bind("FramePacing", "SmartFps", true,
                "Run menus, story and deckbuilding at IdleFps and boost to CombatFps while you touch the screen, the camera moves, or many effects are playing.");
            IdleFps = cfg.Bind("FramePacing", "IdleFps", 30, new ConfigDescription("Frame cap while nothing is happening.",
                new AcceptableValueRange<int>(10, 60)));
            CombatFps = cfg.Bind("FramePacing", "CombatFps", 60, new ConfigDescription(
                "Frame cap while boosted. 0 = the display refresh rate reported by the emulator.",
                new AcceptableValueRange<int>(0, 240)));
            BoostHoldSeconds = cfg.Bind("FramePacing", "BoostHoldSeconds", 1.5f, new ConfigDescription(
                "How long the boost lasts after the last touch / camera movement / effect.",
                new AcceptableValueRange<float>(0.2f, 10f)));
            EffectsParticleThreshold = cfg.Bind("FramePacing", "EffectsParticleThreshold", 60, new ConfigDescription(
                "Boost when at least this many particles are alive. 0 disables the effects check.",
                new AcceptableValueRange<int>(0, 100000)));
            BattleSceneKeywords = cfg.Bind("FramePacing", "BattleSceneKeywords", "",
                "Comma-separated words. If the active scene name contains one, CombatFps is used the whole time. " +
                "Scene names are written to BepInEx/LogOutput.log as 'Scene loaded: ...'.");

            HudEnabled = cfg.Bind("Hud", "Enabled", true, "Show the on-screen combat buttons. The SET button is always shown.");
            HudScale = cfg.Bind("Hud", "Scale", 1f, new ConfigDescription("Size of the on-screen buttons.",
                new AcceptableValueRange<float>(0.5f, 2f)));
            HudOpacity = cfg.Bind("Hud", "Opacity", 0.55f, new ConfigDescription("Background opacity of the on-screen buttons.",
                new AcceptableValueRange<float>(0.1f, 1f)));
            Button1Key = cfg.Bind("Hud", "BigButtonKey", KeyCode.Space, "Key sent by the big button. None hides it.");
            Button2Key = cfg.Bind("Hud", "SmallButtonKey", KeyCode.Escape, "Key sent by the small button. None hides it.");
            ZoomButtons = cfg.Bind("Hud", "ZoomButtons", true, "Show the + / - / FIT zoom buttons (they send mouse-wheel input).");
            ZoomStep = cfg.Bind("Hud", "ZoomStep", 2, new ConfigDescription("Mouse-wheel notches sent per tap of + or -.",
                new AcceptableValueRange<int>(1, 10)));
            ZoomResetKey = cfg.Bind("Hud", "FitButtonKey", KeyCode.None,
                "Optional key also sent by the FIT button, if the game has its own camera-reset key.");
            HudLayoutString = cfg.Bind("Hud", "Layout", HudLayout.Serialize(HudLayout.Defaults()),
                "Button centres as fractions of the screen (x from the left, y from the top). Edit in game with Controls > Move buttons.");

            DieSnap = cfg.Bind("Touch", "SpeedDieSnap", true,
                "Enlarge the touch area of objects whose name contains one of SpeedDieKeywords.");
            DieSnapPaddingPx = cfg.Bind("Touch", "SpeedDiePaddingPx", 28, new ConfigDescription(
                "Extra touch area around each matching UI element, in pixels at 1080p (scaled to your resolution).",
                new AcceptableValueRange<int>(0, 120)));
            DieSnapColliderScale = cfg.Bind("Touch", "SpeedDieColliderScale", 1.35f, new ConfigDescription(
                "Size multiplier for matching 2D/3D colliders. 1 = unchanged.",
                new AcceptableValueRange<float>(1f, 3f)));
            DieSnapKeywords = cfg.Bind("Touch", "SpeedDieKeywords", "SpeedDice",
                "Comma-separated, case-insensitive parts of GameObject names to enlarge. Use the touch inspector to find the right names.");
            Inspector = cfg.Bind("Touch", "Inspector", false,
                "Show the names of whatever you tap (also written to the BepInEx log). For tuning SpeedDieKeywords.");

            ExtraModFolders = cfg.Bind("Mods", "ExtraModFolders", "",
                "Extra folders to include in the mod check, separated by ';'. Windows-style paths as seen inside the emulator.");
        }

        public float EffectiveStageScale
        {
            get
            {
                switch (Preset.Value)
                {
                    case GraphicsPreset.Performance: return 0.7f;
                    case GraphicsPreset.Balanced: return 0.85f;
                    case GraphicsPreset.Quality: return 1f;
                    case GraphicsPreset.Custom: return Mathf.Clamp(CustomStageScale.Value, 0.6f, 1f);
                    default: return GpuProfile.IsWeakMobile ? 0.8f : 1f;
                }
            }
        }

        public bool EffectiveLightRender
        {
            get
            {
                switch (Preset.Value)
                {
                    case GraphicsPreset.Performance: return true;
                    case GraphicsPreset.Balanced: return false;
                    case GraphicsPreset.Quality: return false;
                    case GraphicsPreset.Custom: return CustomLightRender.Value;
                    default: return GpuProfile.IsWeakMobile;
                }
            }
        }
    }
}
