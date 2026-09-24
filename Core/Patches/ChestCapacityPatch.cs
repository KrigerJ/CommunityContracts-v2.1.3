using HarmonyLib;
using StardewValley.Objects;

namespace CommunityContracts.Core.Patches
{
    [HarmonyPatch(typeof(Chest), nameof(Chest.GetActualCapacity))]
    public static class ChestCapacityPatch
    {
        public static void Postfix(Chest __instance, ref int __result)
        {
            if (__instance.modData.ContainsKey("cc.processingChest"))
            {
                __result = 70;
            }
        }
    }
}
