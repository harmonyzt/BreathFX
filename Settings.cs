using BepInEx.Configuration;

namespace BreathFX
{
    internal static class Settings
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> MaxHoldSeconds;
        internal static ConfigEntry<float> RecoverySeconds;
        internal static ConfigEntry<float> DepletedLockSeconds;
        internal static ConfigEntry<float> GaspSeconds;
        internal static ConfigEntry<bool> ReleaseOnSprint;
        internal static ConfigEntry<bool> AutoHoldOnScope;
        internal static ConfigEntry<bool> AutoHoldAssaultRifles;
        internal static ConfigEntry<bool> AutoHoldDmrs;
        internal static ConfigEntry<bool> AutoHoldBoltActions;
        internal static ConfigEntry<bool> ReplaceWeaponSway;
        internal static ConfigEntry<bool> ShowBreathingBar;
        internal static ConfigEntry<bool> ShowBreathVignette;
        internal static ConfigEntry<float> VignetteStrength;
        internal static ConfigEntry<float> VignetteRange;
        internal static ConfigEntry<bool> EnableHeartbeat;
        internal static ConfigEntry<bool> EnableBreathSounds;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the custom breathing system. When disabled, vanilla EFT breathing is restored.");

            MaxHoldSeconds = config.Bind("Breathing", "Hold Breath Duration", 15f,
                new ConfigDescription("How long a full breath can be held before the character runs out of air.",
                    new AcceptableValueRange<float>(1f, 60f)));

            RecoverySeconds = config.Bind("Breathing", "Recovery Duration", 20f,
                new ConfigDescription("How long it takes to recover from empty to full breath while resting.",
                    new AcceptableValueRange<float>(1f, 60f)));

            DepletedLockSeconds = config.Bind("Breathing", "Delay After Running Out", 5.5f,
                new ConfigDescription("How long before the player can hold breath again after running out.",
                    new AcceptableValueRange<float>(0f, 10f)));

            GaspSeconds = config.Bind("Breathing", "Gasp Duration", 2f,
                new ConfigDescription("How long the forced gasp and heavy sway last after running out of air.",
                    new AcceptableValueRange<float>(0f, 10f)));

            ReleaseOnSprint = config.Bind("Breathing", "Release While Sprinting", true,
                "Automatically stop holding breath while sprinting. Not recommended to turn off, due this being more of a bug fix, rather the feature.");

            AutoHoldOnScope = config.Bind("Breathing", "Auto Hold On Scope", true,
                "Automatically hold breath while aiming down sights. The weapon-type options below choose which weapons trigger it.");

            AutoHoldAssaultRifles = config.Bind("Breathing", "Auto Hold Assault Rifles", false,
                "Auto-hold breath when scoping with assault rifles and assault carbines.");

            AutoHoldDmrs = config.Bind("Breathing", "Auto Hold DMRs", true,
                "Auto-hold breath when scoping with marksman rifles.");

            AutoHoldBoltActions = config.Bind("Breathing", "Auto Hold Bolt-Action / Sniper Rifles", true,
                "Auto-hold breath when scoping with bolt-action and other sniper rifles.");

            ReplaceWeaponSway = config.Bind("Sway", "Custom Weapon Sway", false,
                "Replace EFT's weapon sway with sway driven by this mod.");

            ShowBreathingBar = config.Bind("HUD", "Show Breath Bar", true,
                "Show the custom breathing bar next to the stance stamina bars.");

            ShowBreathVignette = config.Bind("HUD", "Show Breath Vignette", true,
                "Darken the screen edges as held breath runs low.");

            VignetteStrength = config.Bind("HUD", "Vignette Strength", 1.0f,
                new ConfigDescription("Maximum darkness of the vignette at empty breath.",
                    new AcceptableValueRange<float>(0f, 1f)));

            VignetteRange = config.Bind("HUD", "Vignette Range", 0.85f,
                new ConfigDescription("How far the vignette reaches toward the center of the screen.",
                    new AcceptableValueRange<float>(0f, 1f)));

            EnableHeartbeat = config.Bind("Audio", "Heartbeat Sound", true,
                "Play a heartbeat that gets louder as held breath runs low.");

            EnableBreathSounds = config.Bind("Audio", "Inhale / Exhale Sounds", true,
                "Play an inhale when you start holding breath and exhale when you release it.");
        }
    }
}