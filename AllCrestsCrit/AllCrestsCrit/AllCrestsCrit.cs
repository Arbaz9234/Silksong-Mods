using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using GlobalSettings;
using UnityEngine;

[BepInPlugin(
    "arbaz9234.allcrests.crit",
    "All Crests Critical Hits",
    "1.1.0"
)]

public class AllCrestsCrit : BaseUnityPlugin
{
    internal static AllCrestsCrit Instance;

    internal ConfigEntry<bool> EnableMod;
    internal ConfigEntry<float> CritChanceAdd;

    private void Awake()
    {
        Instance = this;

        EnableMod = Config.Bind(
            "Critical Hits",
            "Enable",
            true,
            "Enable critical hits for all crests"
        );

        CritChanceAdd = Config.Bind(
            "Critical Hits",
            "Crit Chance Bonus",
            0.25f,
            new ConfigDescription(
                "Additional crit chance (0.25 = +25%)",
                new AcceptableValueRange<float>(0f, 1f)
            )
        );

        new Harmony("silksong.allcrests.crit").PatchAll();
    }
}

// ─────────────────────────────────────────────
// Allow ALL crests to crit
// HeroController.IsWandererLucky
// ─────────────────────────────────────────────
[HarmonyPatch(typeof(HeroController), "IsWandererLucky", MethodType.Getter)]
public static class Patch_IsWandererLucky
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        if (!AllCrestsCrit.Instance.EnableMod.Value)
            return;
        var pd = PlayerData.instance;
        if (pd == null)
            return;
        if (HeroController.instance.cState.isMaggoted)
        {
            __result = false;
            return;
        }
        if (pd.silk < 9)
        {
            __result = false;
            return;
        }
        // Force crit eligibility for all crests
        __result = true;
    }
}

// ─────────────────────────────────────────────
// Increase crit chance
// Gameplay.WandererCritChance
// ─────────────────────────────────────────────
[HarmonyPatch(typeof(Gameplay), "WandererCritChance", MethodType.Getter)]
public static class Patch_WandererCritChance
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        float add = AllCrestsCrit.Instance.CritChanceAdd.Value;
        if (add <= 0f)
            return;

        __result = Mathf.Clamp01(__result + add);
    }
}

//using BepInEx;
//using BepInEx.Logging;
//using HarmonyLib;
//using HutongGames.PlayMaker.Actions;

//[BepInPlugin(
//    "com.arbaz9234.allcrestscrit",
//    "All Crests Critical Hits",
//    "2.1.0"
//)]
//[BepInProcess("Hollow Knight Silksong.exe")]
//public class AllCrestsCritKnightDebug : BaseUnityPlugin
//{
//    internal static ManualLogSource Log;

//    // Toggle this to false before release
//    internal static bool DebugCrits = true;

//    private void Awake()
//    {
//        Log = Logger;
//        new Harmony("com.arbaz9234.allcrestscrit.knightdebug").PatchAll();
//        Log.LogInfo("All Crests Critical Hits (Knight Debug) loaded");
//    }

//    // ─────────────────────────────────────────────
//    // 1️⃣ DAMAGE SYSTEM LAYER (Hornet + Knight)
//    // Forces crit at final damage application
//    // ─────────────────────────────────────────────
//    [HarmonyPatch(typeof(DamageEnemies), "StartDamage")]
//    private static class Patch_DamageEnemies_ForceCrit
//    {
//        private static readonly AccessTools.FieldRef<DamageEnemies, bool> WasCritForced =
//            AccessTools.FieldRefAccess<DamageEnemies, bool>("wasCriticalHitForced");

//        private static readonly AccessTools.FieldRef<DamageEnemies, bool> DoesNotCrit =
//            AccessTools.FieldRefAccess<DamageEnemies, bool>("doesNotCriticalHit");

//        private static void Prefix(DamageEnemies __instance)
//        {
//            // 👉 Replace this with your silk/chance logic if needed
//            bool shouldCrit = true;

//            if (!shouldCrit)
//                return;

//            WasCritForced(__instance) = true;
//            DoesNotCrit(__instance) = false;

//            if (DebugCrits)
//            {
//                Log.LogInfo(
//                    "[CRIT DEBUG] Source=DamageEnemies | Forced=true"
//                );
//            }
//        }
//    }

//    // ─────────────────────────────────────────────
//    // 2️⃣ PLAYMAKER FSM LAYER
//    // Prevents FSM from locking crits
//    // ─────────────────────────────────────────────
//    [HarmonyPatch(typeof(HeroSetWandererCrestState), nameof(HeroSetWandererCrestState.OnEnter))]
//    private static class Patch_WandererCrestFSM
//    {
//        private static void Postfix()
//        {
//            var hc = HeroController.instance;
//            if (hc == null)
//                return;

//            var state = hc.WandererState;

//            if (DebugCrits &&
//                (state.CriticalHitsLocked || state.QueuedNextHitCritical))
//            {
//                Log.LogInfo(
//                    $"[CRIT DEBUG] Source=WandererFSM | Queued={state.QueuedNextHitCritical} | Locked={state.CriticalHitsLocked}"
//                );
//            }

//            // Ensure FSM never locks crits
//            state.CriticalHitsLocked = false;

//            // Do NOT force queued crit unless you want guaranteed crits
//            // state.QueuedNextHitCritical = true;

//            hc.WandererState = state;
//        }
//    }

//    // ─────────────────────────────────────────────
//    // 3️⃣ FINAL CONFIRMATION LAYER
//    // Logs when a crit actually happens
//    // ─────────────────────────────────────────────
//    [HarmonyPatch(typeof(HitInstance), nameof(HitInstance.CriticalHit), MethodType.Getter)]
//    private static class Patch_HitInstance_CriticalHit
//    {
//        private static void Postfix(bool __result)
//        {
//            if (!DebugCrits || !__result)
//                return;

//            Log.LogInfo(
//                "[CRIT DEBUG] FINAL RESULT → CriticalHit = TRUE"
//            );
//        }
//    }
//}