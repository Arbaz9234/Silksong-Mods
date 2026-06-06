using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using System.Collections;
using UnityEngine;

[BepInPlugin(
    "silksong.parry.refund.onfail.v2",
    "Refund Silk On Failed Cross Stitch",
    "2.0.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class RefundSilkOnFailedCrossStitch : BaseUnityPlugin
{
    // ───────── CONFIG ─────────
    private static ConfigEntry<bool> EnableMod;

    // ───────── STATE ─────────
    private static bool parryActive;
    private static Coroutine refundRoutine;

    private static ManualLogSource Log;

    private void Awake()
    {
        EnableMod = Config.Bind(
            "General",
            "Enable Mod",
            true,
            "Enable or disable silk refund on failed Cross Stitch parry"
        );

        Log = Logger;
        new Harmony("silksong.parry.refund.onfail.correct.harmony").PatchAll();

        Log.LogInfo("Refund Silk On Failed Cross Stitch loaded");
    }

    // ─────────────────────────────────────────────
    // 1️⃣ PARRY ATTEMPT START (FSM)
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(PlayMakerFSM), nameof(PlayMakerFSM.SendEvent))]
    private static class ParryStartPatch
    {
        private static void Prefix(string eventName)
        {
            if (!EnableMod.Value)
                return;

            if (eventName != "PARRY")
                return;
            Log.LogInfo("INVUL_TIME_CROSS_STITCH" + HeroController.instance.INVUL_TIME_CROSS_STITCH);
            parryActive = true;

            if (refundRoutine != null)
            {
                HeroController.instance.StopCoroutine(refundRoutine);
                refundRoutine = null;
            }

            refundRoutine = HeroController.instance.StartCoroutine(RefundIfFailed());

            Log.LogInfo("[Parry] Attempt started");
        }
    }

    // ─────────────────────────────────────────────
    // 2️⃣ SUCCESS SIGNAL
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.CrossStitchInvuln))]
    private static class ParrySuccessPatch
    {
        private static void Prefix()
        {
            if (!EnableMod.Value)
                return;

            if (!parryActive)
                return;

            parryActive = false;

            if (refundRoutine != null)
            {
                HeroController.instance.StopCoroutine(refundRoutine);
                refundRoutine = null;
            }

            Log.LogInfo("[Parry] Success detected — no refund");
        }
    }

    // ─────────────────────────────────────────────
    // 3️⃣ FAILURE → REFUND
    // ─────────────────────────────────────────────
    private static IEnumerator RefundIfFailed()
    {
        yield return new WaitForSeconds(1f);

        if (parryActive && EnableMod.Value)
        {
            int refundAmount = PlayerData.instance.SilkSkillCost;
            HeroController.instance.AddSilk(refundAmount, false);

            Log.LogInfo($"[Parry] Failed — refunded {refundAmount} silk");
        }

        parryActive = false;
        refundRoutine = null;
    }
}
