using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace AlwaysHasWhiteFlower
{
    [BepInPlugin("arbaz.whiteflower.always", "Always Has White Flower", "1.0.0")]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class AlwaysHasWhiteFlower : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> EnableMod;

        private void Awake()
        {
            EnableMod = Config.Bind(
                "General",
                "Enable Always Has White Flower",
                true,
                "If enabled, the game will always treat the player as having the White Flower"
            );

            new Harmony("arbaz.whiteflower.always.harmony").PatchAll();
            Logger.LogInfo("Always Has White Flower mod loaded.");
        }

        // --------------------------------------------------
        // FORCE HasWhiteFlower = true
        // --------------------------------------------------
        [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.HasWhiteFlower), MethodType.Getter)]
        private static class HasWhiteFlowerPatch
        {
            private static bool Prefix(ref bool __result)
            {
                if (!EnableMod.Value)
                    return true; // use original logic

                __result = true;
                return false; // skip original getter
            }
        }
    }
}
