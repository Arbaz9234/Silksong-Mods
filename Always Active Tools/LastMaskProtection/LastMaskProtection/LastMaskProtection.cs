using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrueAlwaysFracturedMask
{
    [BepInPlugin(
        "arbaz.truealwaysfracturedmask",
        "True Always Fractured Mask",
        "2.0.0"
    )]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class TrueAlwaysFracturedMask : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("arbaz.truealwaysfracturedmask.harmony");
        internal static ManualLogSource ModLogger;
        public static List<ToolItem> ToolItems;

        // CONFIG
        internal static ConfigEntry<bool> EnableLastMaskProtection;

        // Fractured Mask index
        private const int FRACTURED_MASK_INDEX = 5;

        private void Awake()
        {
            ModLogger = Logger;

            EnableLastMaskProtection = Config.Bind(
                "General",
                "Enable Last Mask Protection",
                true,
                "If enabled, health will never drop below 1 mask. If disabled, only Fractured Mask is forced."
            );

            try
            {
                harmony.PatchAll();
                ModLogger.LogInfo("True Always Fractured Mask loaded successfully");
            }
            catch (Exception ex)
            {
                ModLogger.LogError("Failed to apply patches:\n" + ex);
            }
        }

        // ─────────────────────────────────────────────
        // TOOL HANDLING
        // ─────────────────────────────────────────────
        public static void PopulateToolList()
        {
            if (!Extensions.IsNullOrEmpty(ToolItems))
                return;

            ToolItems = ToolItemManager.GetAllTools().ToList();
            ToolItems.RemoveRange(0, 29);
        }

        public static bool IsFracturedMask(ToolItem tool)
        {
            if (Extensions.IsNullOrEmpty(ToolItems))
                return false;

            return ToolItems.IndexOf(tool) == FRACTURED_MASK_INDEX;
        }

        // Force equipped
        [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
        private class ToolItem_IsEquipped_Patch
        {
            private static bool Prefix(ToolItem __instance, ref bool __result)
            {
                PopulateToolList();

                if (!IsFracturedMask(__instance))
                    return true;

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItem), "IsEquippedHud", MethodType.Getter)]
        private class ToolItem_IsEquippedHud_Patch
        {
            private static bool Prefix(ToolItem __instance, ref bool __result)
            {
                PopulateToolList();

                if (!IsFracturedMask(__instance))
                    return true;

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(
            typeof(ToolItemManager),
            "IsToolEquipped",
            new Type[] { typeof(ToolItem), typeof(ToolEquippedReadSource) }
        )]
        private class ToolItemManager_IsToolEquipped_Patch
        {
            private static bool Prefix(
                ToolItem tool,
                ToolEquippedReadSource readSource,
                ref bool __result)
            {
                PopulateToolList();

                if (!IsFracturedMask(tool))
                    return true;

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItemManager), "GetCurrentEquippedTools")]
        private class ToolItemManager_GetCurrentEquippedTools_Patch
        {
            private static void Postfix(List<ToolItem> __result)
            {
                PopulateToolList();

                if (ToolItems == null || ToolItems.Count <= FRACTURED_MASK_INDEX)
                    return;

                ToolItem fracturedMask = ToolItems[FRACTURED_MASK_INDEX];

                if (!__result.Contains(fracturedMask))
                    __result.Add(fracturedMask);
            }
        }

        // ─────────────────────────────────────────────
        // LAST MASK PROTECTION (OPTIONAL)
        // ─────────────────────────────────────────────
        [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeHealth))]
        private static class TakeHealthPatch
        {
            private static void Prefix(PlayerData __instance, ref int amount)
            {
                if (!EnableLastMaskProtection.Value)
                    return;

                if (amount <= 0)
                    return;

                int currentHealth = __instance.health;

                // Already at last mask → block all damage
                if (currentHealth <= 1)
                {
                    amount = 0;
                    return;
                }

                // Clamp damage so health stops at 1
                if (currentHealth - amount < 1)
                {
                    amount = currentHealth - 1;
                }
            }
        }
    }
}
