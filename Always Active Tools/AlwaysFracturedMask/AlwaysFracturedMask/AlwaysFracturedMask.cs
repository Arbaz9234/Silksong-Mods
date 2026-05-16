using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
namespace AlwaysFracturedMask;

[BepInPlugin("arbaz9234.alwaysfracturedmask", "Always Fractured Mask", "1.0.0")]
public class AlwaysFracturedMask : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony("arbaz9234.alwaysfracturedmask");
    internal static ManualLogSource ModLogger;
    public static AlwaysFracturedMask Instance;
    public static List<ToolItem> ToolItems;

    private void Awake()
    {
        if (AlwaysFracturedMask.Instance == null)
            AlwaysFracturedMask.Instance = this;

        AlwaysFracturedMask.ModLogger = this.Logger;
        AlwaysFracturedMask.ModLogger.LogInfo((object)"=== Plugin Always Fractured Mask is loaded! ===");

        try
        {
            this.harmony.PatchAll();
            AlwaysFracturedMask.ModLogger.LogInfo((object)"Harmony patches applied successfully!");
        }
        catch (Exception ex)
        {
            AlwaysFracturedMask.ModLogger.LogError((object)("Failed to apply Harmony patches: " + ex.ToString()));
        }
    }

    public static void PopulateToolList()
    {
        if (!Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysFracturedMask.ToolItems))
        {
            return;
        }
        AlwaysFracturedMask.ToolItems = ToolItemManager.GetAllTools().ToList<ToolItem>();
        AlwaysFracturedMask.ToolItems.RemoveRange(0, 29);
    }
    private const int FRACTURED_MASK_INDEX = 5;
    public static bool IsFracturedMask(ToolItem tool)
    {
        if (Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysFracturedMask.ToolItems))
        {
            return false;
        }

        int index = AlwaysFracturedMask.ToolItems.IndexOf(tool);
        return index == FRACTURED_MASK_INDEX;
    }

    [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
    public class ToolItem_IsEquipped_Patch
    {
        [HarmonyPrefix]
        public static bool ToolItem_IsEquipped_Prefix(ref ToolItem __instance, ref bool __result)
        {
            AlwaysFracturedMask.PopulateToolList();

            if (!AlwaysFracturedMask.IsFracturedMask(__instance))
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
            AlwaysFracturedMask.PopulateToolList();

            if (!AlwaysFracturedMask.IsFracturedMask(__instance))
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
            AlwaysFracturedMask.PopulateToolList();

            if (!AlwaysFracturedMask.IsFracturedMask(tool))
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
            AlwaysFracturedMask.PopulateToolList();

            if (!Extensions.IsNullOrEmpty<ToolItem>((ICollection<ToolItem>)AlwaysFracturedMask.ToolItems) &&
                AlwaysFracturedMask.ToolItems.Count > FRACTURED_MASK_INDEX)
            {
                ToolItem fracturedMask = AlwaysFracturedMask.ToolItems[FRACTURED_MASK_INDEX];
                if (!__result.Contains(fracturedMask))
                {
                    __result.Add(fracturedMask);
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