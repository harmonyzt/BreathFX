using BreathFX.Audio;
using EFT;
using HarmonyLib;

namespace BreathFX.Patches
{
    [HarmonyPatch(typeof(GameWorld), nameof(GameWorld.OnGameStarted))]
    internal static class GameWorldRaidAudioPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            BreathingAudioLoader.RecoverForRaid();
        }
    }
}