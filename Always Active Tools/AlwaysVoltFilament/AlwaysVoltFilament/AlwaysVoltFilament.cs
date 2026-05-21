using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlwaysVoltFilament;

[BepInPlugin("arbaz9234.alwaysvoltfilament", "Always Volt Filament", "1.0.0")]
public class AlwaysVoltFilamentMod : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony("arbaz9234.alwaysvoltfilament");
    internal static ManualLogSource ModLogger;
    public static AlwaysVoltFilamentMod Instance;
    public static List<ToolItem> ToolItems;

    private void Awake()
    {
        if (AlwaysVoltFilamentMod.Instance == null)
            AlwaysVoltFilamentMod.Instance = this;

        AlwaysVoltFilamentMod.ModLogger = this.Logger;
        AlwaysVoltFilamentMod.ModLogger.LogInfo((object)"=== Plugin Always Volt Filament is loaded! ===");

        try
        {
            this.harmony.PatchAll();
            AlwaysVoltFilamentMod.ModLogger.LogInfo((object)"Harmony patches applied successfully!");
        }
        catch (Exception ex)
        {
            AlwaysVoltFilamentMod.ModLogger.LogError((object)("Failed to apply Harmony patches: " + ex.ToString()));
        }
    }

    public static void PopulateToolList()
    {
        if (!Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysVoltFilamentMod.ToolItems))
        {
            return;
        }

        AlwaysVoltFilamentMod.ToolItems = ToolItemManager.GetAllTools().ToList<ToolItem>();
        AlwaysVoltFilamentMod.ToolItems.RemoveRange(0, 29); //First 29 are the red tools and needle skills
    }
    private const int VOLT_FILAMENT_INDEX = 16;
    public static bool IsVoltFilament(ToolItem tool)
    {
        if (Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysVoltFilamentMod.ToolItems))
        {
            return false;
        }

        int index = AlwaysVoltFilamentMod.ToolItems.IndexOf(tool);
        return index == VOLT_FILAMENT_INDEX;
    }

    [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
    public class ToolItem_IsEquipped_Patch
    {
        [HarmonyPrefix]
        public static bool ToolItem_IsEquipped_Prefix(ref ToolItem __instance, ref bool __result)
        {
            AlwaysVoltFilamentMod.PopulateToolList();

            if (!AlwaysVoltFilamentMod.IsVoltFilament(__instance))
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ToolItem), "IsEquippedHud", MethodType.Getter)]
    public class ToolItem_IsEquippedHud_Patch
    {
        [HarmonyPrefix]
        public static bool ToolItem_IsEquippedHud_Prefix(ref ToolItem __instance, ref bool __result)
        {
            AlwaysVoltFilamentMod.PopulateToolList();

            if (!AlwaysVoltFilamentMod.IsVoltFilament(__instance))
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ToolItemManager), "IsToolEquipped", new Type[] { typeof(ToolItem), typeof(ToolEquippedReadSource) })]
    public class ToolItemManager_IsToolEquipped_IsEquipped_Patch
    {
        [HarmonyPrefix]
        public static bool ToolItemManager_IsToolEquipped_Prefix(
          ToolItem tool,
          ToolEquippedReadSource readSource,
          ref bool __result)
        {
            AlwaysVoltFilamentMod.PopulateToolList();

            if (!AlwaysVoltFilamentMod.IsVoltFilament(tool))
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ToolItemManager), "GetCurrentEquippedTools")]
    public class ToolItemManager_GetCurrentEquippedTools_Patch
    {
        [HarmonyPostfix]
        public static List<ToolItem> ToolItemManager_GetCurrentEquippedTools_Postfix(
          List<ToolItem> __result)
        {
            AlwaysVoltFilamentMod.PopulateToolList();

            if (!Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysVoltFilamentMod.ToolItems) &&
                AlwaysVoltFilamentMod.ToolItems.Count > VOLT_FILAMENT_INDEX)
            {
                ToolItem voltFilament = AlwaysVoltFilamentMod.ToolItems[VOLT_FILAMENT_INDEX];
                if (!__result.Contains(voltFilament))
                {
                    __result.Add(voltFilament);
                }
            }
            else
            {
                ModLogger.LogError((object)"ToolItems not properly initialized");
            }
            return __result;
        }
    }
}