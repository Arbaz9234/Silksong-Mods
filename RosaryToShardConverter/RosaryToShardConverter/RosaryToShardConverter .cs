using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using GlobalSettings;

namespace RosaryToShardConverter
{
    [BepInPlugin("arbaz.rosarytoshellshards", "Rosary To Shell Shards", "1.0.1")]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class RosaryToShardConverter : BaseUnityPlugin
    {
        internal static ConfigEntry<int> AmountToConvert;
        internal static ConfigEntry<bool> EnableAutoConvert;

        private void Awake()
        {
            AmountToConvert = Config.Bind(
                "Manual Conversion",
                "Amount To Convert",
                0,
                "Rosaries to convert when pressing CTRL + '"
            );

            EnableAutoConvert = Config.Bind(
                "Auto Conversion",
                "Enable Auto Convert",
                false,
                "Automatically convert picked-up rosaries into shell shards"
            );

            new Harmony("arbaz.rosarytoshellshards.harmony").PatchAll();
            Logger.LogInfo("Rosary To Shell Shards loaded.");
        }

        private void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                Input.GetKeyDown(KeyCode.Quote))
            {
                ConvertRosaries(AmountToConvert.Value);
            }
        }

        // --------------------------------------------------
        // CORE CONVERSION LOGIC
        // --------------------------------------------------
        private static void ConvertRosaries(int requested)
        {
            if (requested <= 0)
                return;

            PlayerData pd = PlayerData.instance;
            if (pd == null)
                return;

            int available = pd.geo;
            if (available <= 0)
                return;

            int convert = Mathf.Min(requested, available);

            // Remove rosaries
            pd.AddGeo(-convert);

            // Add shards (game clamps internally)
            pd.AddShards(convert);
        }

        // --------------------------------------------------
        // AUTO CONVERSION ON PICKUP
        // --------------------------------------------------
        [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.AddGeo))]
        private static class AutoConvertPatch
        {
            private static void Postfix(int amount)
            {
                if (!EnableAutoConvert.Value)
                    return;

                if (amount <= 0)
                    return;

                PlayerData pd = PlayerData.instance;
                if (pd == null)
                    return;

                // Remove rosaries that were just added
                pd.AddGeo(-amount);

                // Convert to shards (overflow-safe)
                pd.AddShards(amount);
            }
        }
    }
}
