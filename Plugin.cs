using BepInEx;
using BepInEx.Logging;
using BreathFX.Audio;
using HarmonyLib;

namespace BreathFX
{
    [BepInPlugin(Guid, "BreathFX", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.harmonyzt.BreathFX";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Settings.Bind(Config);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Logger.LogInfo("BreathFX loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            BreathingAudioLoader.Dispose();
        }
    }
}