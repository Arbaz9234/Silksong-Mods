using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;

[BepInPlugin(
    "silksong.maggot.bind.healfix",
    "Allow Bind Heal While Maggoted",
    "1.0.0"
)]
public class MaggotBindHealFix : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private static bool wasMaggotedAtBindStart;
    private bool lastIsBinding;
    private Coroutine bindingCheckCoroutine;

    private void Awake()
    {
        Log = Logger;
        new Harmony("silksong.maggot.bind.healfix.harmony").PatchAll();
        Log.LogInfo("=== Bind Heal While Maggoted Loaded ===");

        StartBindingTracking();
    }

    // ─────────────────────────────────────────────
    // BIND STATE TRACKER (POLLING)
    // ─────────────────────────────────────────────
    private void StartBindingTracking()
    {
        if (bindingCheckCoroutine != null)
            return;

        bindingCheckCoroutine = StartCoroutine(BindingCheckCoroutine());
        Log.LogInfo("[BindingTracker] Started");
    }

    private IEnumerator BindingCheckCoroutine()
    {
        var wait = new WaitForSeconds(0.5f);

        while (true)
        {
            HeroController hero = HeroController.instance;

            if (hero == null || hero.cState == null)
            {
                lastIsBinding = false;
                yield return wait;
                continue;
            }

            bool isBindingNow = hero.cState.isBinding;

            // ───── Bind START edge ─────
            if (isBindingNow && !lastIsBinding)
            {
                OnBindStart(hero);
            }

            lastIsBinding = isBindingNow;
            yield return wait;
        }
    }

    // ─────────────────────────────────────────────
    // BIND START LOGIC
    // ─────────────────────────────────────────────
    private void OnBindStart(HeroController hero)
    {
        wasMaggotedAtBindStart = hero.cState.isMaggoted;

        Log.LogDebug(
            $"[Bind Start] wasMaggoted={wasMaggotedAtBindStart}, insideMaggotRegion={MaggotRegion.IsInsideAny}"
        );

        // Key behavior: clear maggot BEFORE bind finishes so healing is allowed
        if (hero.cState.isMaggoted && !MaggotRegion.IsInsideAny)
        {
            hero.SetIsMaggoted(false);
            Log.LogInfo("[Bind Start] Cleared maggot early to allow bind healing");
        }
    }
}
