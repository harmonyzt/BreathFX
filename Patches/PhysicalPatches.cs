using BreathFX.Breathing;
using HarmonyLib;
using EFT;

namespace BreathFX.Patches
{
    [HarmonyPatch(typeof(Physical), nameof(Physical.Init))]
    internal static class PhysicalInitPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Physical __instance)
        {
            Player player = __instance._player;
            if (player == null || !player.IsYourPlayer || player.IsAI)
            {
                return;
            }

            BreathingController controller = player.gameObject.GetComponent<BreathingController>();
            if (controller == null)
            {
                controller = player.gameObject.AddComponent<BreathingController>();
            }

            controller.Initialize(__instance, player);
            BreathingManager.Register(__instance, controller);
        }
    }

    [HarmonyPatch(typeof(Physical), nameof(Physical.HoldBreath))]
    internal static class PhysicalHoldBreathPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Physical __instance, bool enable)
        {
            if (BreathingManager.TryGet(__instance, out BreathingController controller) && controller.IsActive)
            {
                controller.SetHoldRequested(enable);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Physical), "get_HoldingBreath")]
    internal static class PhysicalHoldingBreathPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Physical __instance, ref bool __result)
        {
            if (BreathingManager.TryGet(__instance, out BreathingController controller) && controller.IsActive)
            {
                __result = controller.IsHolding;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PhysicalBase), "get_BreathIsAudible")]
    internal static class PhysicalBreathIsAudiblePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PhysicalBase __instance, ref bool __result)
        {
            if (__instance is Physical physical
                && BreathingManager.TryGet(physical, out BreathingController controller)
                && controller.IsActive)
            {
                __result = controller.IsAudible;
                return false;
            }

            return true;
        }
    }
}