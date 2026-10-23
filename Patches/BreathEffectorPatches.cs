using BreathFX.Breathing;
using HarmonyLib;
using EFT.Animations;

namespace BreathFX.Patches
{
    [HarmonyPatch(typeof(BreathEffector), nameof(BreathEffector.Process))]
    internal static class BreathEffectorProcessPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(BreathEffector __instance, float deltaTime)
        {
            if (__instance.Physical is Physical physical
                && BreathingManager.TryGet(physical, out BreathingController controller)
                && controller.IsActive
                && CustomBreathProcessor.TryProcess(__instance, controller, deltaTime))
            {
                return false;
            }

            return true;
        }
    }
}

