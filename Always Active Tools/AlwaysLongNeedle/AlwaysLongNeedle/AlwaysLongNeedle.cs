using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using GlobalSettings;
using System.Collections.Generic;

[BepInPlugin("com.arbaz9234.alwayslongneedle", "Always Long Needle", "1.1.0")]
[BepInProcess("Hollow Knight Silksong.exe")]
public class AlwaysLongNeedle : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    // UI string → float mapping
    internal static ConfigEntry<string> RangeMultiplierString;

    private static readonly Dictionary<GameObject, Vector3> originalScales =
        new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        Log = Logger;

        RangeMultiplierString = Config.Bind(
            "General",
            "Needle Range Multiplier",
            "Normal (1x)",
            new ConfigDescription(
                "Increase needle damage range",
                new AcceptableValueList<string>(
                    "Normal (1x)",
                    "1.5x",
                    "2x"
                )
            )
        );

        new Harmony("com.arbaz9234.alwayslongneedle").PatchAll();
        Log.LogInfo("Always Long Needle v1.1.0 loaded");
    }

    // Convert UI string → actual float
    internal static float GetRangeMultiplier()
    {
        string value = RangeMultiplierString.Value;

        switch (value)
        {
            case "1.5x":
                return 1.1f;
            case "2x":
                return 1.2f;
            default:
                return 1f;
        }
    }

    // ─────────────────────────────────────────────
    // Always treat Long Needle as equipped
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(ToolItem), "get_IsEquipped")]
    private static class ToolItem_IsEquipped_LongNeedlePatch
    {
        private static void Postfix(ToolItem __instance, ref bool __result)
        {
            if (__instance == Gameplay.LongNeedleTool)
                __result = true;
        }
    }

    // ─────────────────────────────────────────────
    // Correct damage range scaling hook
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(NailAttackBase), "OnSlashStarting")]
    private static class NailAttackBase_OnSlashStarting_Patch
    {
        private static void Postfix(NailAttackBase __instance)
        {
            float mult = GetRangeMultiplier();
            if (mult <= 1f)
                return;

            GameObject go = __instance.gameObject;
            if (go == null)
                return;

            if (!originalScales.ContainsKey(go))
                originalScales[go] = go.transform.localScale;

            Vector3 baseScale = originalScales[go];

            go.transform.localScale = baseScale * mult;
        }
    }

    // ─────────────────────────────────────────────
    // Restore scale when attack ends
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(NailAttackBase), "CancelAttack")]
    private static class NailAttackBase_CancelAttack_Patch
    {
        private static void Postfix(NailAttackBase __instance)
        {
            GameObject go = __instance.gameObject;
            if (go == null)
                return;

            if (originalScales.TryGetValue(go, out Vector3 original))
            {
                go.transform.localScale = original;
                originalScales.Remove(go);
            }
        }
    }
}
