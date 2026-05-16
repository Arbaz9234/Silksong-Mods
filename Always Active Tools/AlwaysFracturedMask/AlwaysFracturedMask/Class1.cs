using BepInEx;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;

[BepInPlugin(
    "com.arbaz9234.alwaysfracturedmask",
    "AlwaysFracturedMask",
    "1.0.0"
)]
public class AlwaysFracturedMask : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("AlwaysFracturedMask loaded");

        Harmony harmony = new Harmony("com.arbaz9234.alwaysfracturedmask");
        harmony.PatchAll();
    }
}

//
// PATCH ToolItem.IsEquipped (GLOBAL & RELIABLE)
//
[HarmonyPatch(typeof(ToolItem), "get_IsEquipped")]
public static class ToolItem_IsEquipped_FracturedMaskPatch
{
    [HarmonyPostfix]
    private static void Postfix(ToolItem __instance, ref bool __result)
    {
        if ((Object)__instance == (Object)Gameplay.FracturedMaskTool)
        {
            AlwaysFracturedMask.Log.LogInfo(
                "Forcing Fractured Mask IsEquipped = true"
            );
            __result = true;
        }
    }
}
