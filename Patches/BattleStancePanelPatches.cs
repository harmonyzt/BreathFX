using BreathFX.Hud;
using HarmonyLib;
using EFT;
using EFT.UI;

namespace BreathFX.Patches
{
    [HarmonyPatch(typeof(BattleStancePanel), nameof(BattleStancePanel.Show))]
    internal static class BattleStancePanelShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BattleStancePanel __instance, Player player)
        {
            BreathingHudBar.Attach(__instance, player);
        }
    }
}