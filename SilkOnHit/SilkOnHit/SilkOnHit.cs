using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SilkOnHitMod
{
    [BepInPlugin("arbaz.silkonhit", "Silk On Hit", "1.1.0")]
    public class SilkOnHit : BaseUnityPlugin
    {
        internal static SilkOnHit Instance;

        // Config entries
        internal static ConfigEntry<bool> EnableMod;
        internal static ConfigEntry<int> SilkAmountOnHit;

        private void Awake()
        {
            Instance = this;

            // Config
            EnableMod = Config.Bind(
                "General",
                "Enable Silk On Hit",
                true,
                "Enable or disable gaining silk when taking damage"
            );

            SilkAmountOnHit = Config.Bind(
                "General",
                "Silk Amount On Hit",
                1,
                "Amount of silk gained whenever the player takes damage"
            );

            // Harmony
            Harmony harmony = new Harmony("arbaz.silkonhit.harmony");
            harmony.PatchAll();

            Logger.LogInfo("Silk On Hit loaded.");
        }

        // --------------------------------------------------
        // Add silk whenever real damage is taken
        // --------------------------------------------------
        [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeHealth))]
        private static class SilkOnDamagePatch
        {
            private static void Postfix(int amount)
            {
                if (!EnableMod.Value)
                    return;

                // Only when actual health was reduced
                if (amount <= 0)
                    return;

                int silk = SilkAmountOnHit.Value;

                if (silk <= 0)
                    return;

                HeroController.instance.AddSilk(silk, false);
            }
        }
    }
}
