using System.Collections.Generic;
using EFT;

namespace BreathFX.Breathing
{
    internal static class BreathingManager
    {
        private static readonly Dictionary<Physical, BreathingController> Controllers =
            new Dictionary<Physical, BreathingController>();

        internal static void Register(Physical physical, BreathingController controller)
        {
            if (physical == null || controller == null)
            {
                return;
            }

            Controllers[physical] = controller;
        }

        internal static void Unregister(Physical physical, BreathingController controller)
        {
            if (physical == null)
            {
                return;
            }

            if (Controllers.TryGetValue(physical, out BreathingController existing) && existing == controller)
            {
                Controllers.Remove(physical);
            }
        }

        internal static bool TryGet(PhysicalBase physical, out BreathingController controller)
        {
            controller = null;
            return physical is Physical typed && Controllers.TryGetValue(typed, out controller);
        }

        internal static bool TryGet(Player player, out BreathingController controller)
        {
            controller = null;
            return player != null && TryGet(player.Physical, out controller);
        }
    }
}