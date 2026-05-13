using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;

[BepInPlugin("silksong.ability.toggle", "Silksong Ability Toggle", "1.2.2")]
[BepInProcess("Hollow Knight Silksong.exe")]
public class AbilityTogglePlugin : BaseUnityPlugin
{
    // ───────────── CONFIG ─────────────

    private ConfigEntry<bool> dash;
    private ConfigEntry<bool> wallJump;
    private ConfigEntry<bool> glide;
    private ConfigEntry<bool> needolin;
    private ConfigEntry<bool> chargeSlash;
    private ConfigEntry<bool> grapplingHook;
    private ConfigEntry<bool> doubleJump;
    private ConfigEntry<bool> sylphsong;
    private ConfigEntry<bool> beastlingCall;
    private ConfigEntry<bool> needolinMemory;
    private ConfigEntry<bool> superJump;


    // ───────────── RUNTIME ─────────────

    private object playerData;
    private object heroConfig;
    private Harmony harmony;

    // INTERNAL IMPLEMENTATION DETAIL
    private bool sylphsongBehavior => sylphsong.Value;

    void Awake()
    {
        harmony = new Harmony("silksong.ability.toggle.harmony");
        harmony.PatchAll();

        doubleJump = Bind("Double Jump");
        dash = Bind("Dash");
        wallJump = Bind("Wall Jump");
        glide = Bind("Glide / Brolly");
        chargeSlash = Bind("Charge Slash");
        beastlingCall = Config.Bind(
                                "Abilities",
                                "Beastling Call",
                                false,
                                "Unlock Beastling fast travel"
                            );
        needolin = Bind("Needolin");
        needolinMemory = Bind("Needolin Memory");
        grapplingHook = Bind("Grappling Hook");
        superJump = Bind("Super Jump");
        sylphsong = Config.Bind(
            "Abilities",
            "Sylphsong",
            false,
            "Regenerate silk over time while resting on a bench"
        );

        StartCoroutine(InitializeWhenReady());
    }

    // ───────────── INITIALIZATION ─────────────

    private IEnumerator InitializeWhenReady()
    {
        while ((playerData = GetPlayerData()) == null ||
               (heroConfig = GetHeroConfig()) == null)
        {
            yield return null;
        }

        doubleJump.Value = GetPD("hasDoubleJump");
        dash.Value = GetPD("hasDash");
        wallJump.Value = GetPD("hasWalljump");
        glide.Value = GetPD("hasBrolly");
        chargeSlash.Value = GetPD("hasChargeSlash");
        needolin.Value = GetPD("hasNeedolin");
        beastlingCall.Value = GetPD("UnlockedFastTravelTeleport");
        needolinMemory.Value = GetPD("hasNeedolinMemoryPowerup");
        grapplingHook.Value = GetPD("hasHarpoonDash");
        superJump.Value = GetPD("hasSuperJump");

        ApplyAll();

        doubleJump.SettingChanged += (_, __) => Apply("hasDoubleJump", doubleJump.Value);
        dash.SettingChanged += (_, __) => Apply("hasDash", dash.Value);
        wallJump.SettingChanged += (_, __) => Apply("hasWalljump", wallJump.Value);

        glide.SettingChanged += (_, __) => Apply("hasBrolly", glide.Value, "canBrolly");
        chargeSlash.SettingChanged += (_, __) => Apply("hasChargeSlash", chargeSlash.Value, "canNailCharge");
        needolin.SettingChanged += (_, __) => Apply("hasNeedolin", needolin.Value, "canPlayNeedolin");
        beastlingCall.SettingChanged += (_, __) => Apply("UnlockedFastTravelTeleport", beastlingCall.Value);
        needolinMemory.SettingChanged += (_, __) => Apply("hasNeedolinMemoryPowerup", needolinMemory.Value);
        grapplingHook.SettingChanged += (_, __) => Apply("hasHarpoonDash", grapplingHook.Value, "canHarpoonDash");
        superJump.SettingChanged += (_, __) => Apply("hasSuperJump", superJump.Value);

        Logger.LogInfo("Ability Toggle initialized (Sylphsong enabled)");
    }

    // ───────────── APPLY ─────────────

    private void ApplyAll()
    {
        Apply("hasDoubleJump", doubleJump.Value);
        Apply("hasDash", dash.Value);
        Apply("hasWalljump", wallJump.Value);
        Apply("hasBrolly", glide.Value, "canBrolly");
        Apply("hasChargeSlash", chargeSlash.Value, "canNailCharge");
        Apply("hasNeedolin", needolin.Value, "canPlayNeedolin");
        Apply("UnlockedFastTravelTeleport", beastlingCall.Value);
        Apply("hasNeedolinMemoryPowerup", needolinMemory.Value);
        Apply("hasHarpoonDash", grapplingHook.Value, "canHarpoonDash");
        Apply("hasSuperJump", superJump.Value);
    }

    private void Apply(string pdField, bool value, string hcField = null)
    {
        SetPD(pdField, value);
        if (!string.IsNullOrEmpty(hcField))
            SetHC(hcField, value);
    }

    private ConfigEntry<bool> Bind(string name)
    {
        return Config.Bind("Abilities", name, false, $"Enable or disable {name}");
    }

    // ───────────── PLAYER DATA ─────────────

    private object GetPlayerData()
    {
        try
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            var type = asm?.GetTypes().FirstOrDefault(t => t.Name == "PlayerData");
            return type?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
        }
        catch { return null; }
    }

    private bool GetPD(string field)
    {
        try
        {
            var f = playerData.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            return f != null && (bool)f.GetValue(playerData);
        }
        catch { return false; }
    }

    private void SetPD(string field, bool value)
    {
        try
        {
            var f = playerData.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            f?.SetValue(playerData, value);
        }
        catch { }
    }

    // ───────────── HERO CONFIG ─────────────

    private object GetHeroConfig()
    {
        try
        {
            var hero = UnityEngine.Object.FindFirstObjectByType<HeroController>();
            return hero?.GetType().GetProperty("Config")?.GetValue(hero);
        }
        catch { return null; }
    }

    private void SetHC(string field, bool value)
    {
        try
        {
            var f = heroConfig.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            f?.SetValue(heroConfig, value);
        }
        catch { }
    }

    // ───────────── SYLPHSONG (INTERNAL BEHAVIOR) ─────────────

    [HarmonyPatch(typeof(RestBenchHelper), nameof(RestBenchHelper.SetOnBench))]
    private static class SylphsongBehaviorPatch
    {
        private static Coroutine silkCoroutine;

        private static void Postfix(RestBenchHelper __instance, bool onBench)
        {
            if (__instance == null)
                return;

            var plugin = FindAnyObjectByType<AbilityTogglePlugin>();
            if (plugin == null || !plugin.sylphsongBehavior)
            {
                Stop(__instance);
                return;
            }

            HeroController hc = HeroController.instance;
            if (hc == null)
                return;

            if (onBench)
                Start(__instance, hc);
            else
                Stop(__instance);
        }

        private static void Start(RestBenchHelper bench, HeroController hc)
        {
            if (silkCoroutine != null)
                return;

            silkCoroutine = bench.StartCoroutine(RestoreSilk(hc));
        }

        private static void Stop(RestBenchHelper bench)
        {
            if (silkCoroutine == null)
                return;

            bench.StopCoroutine(silkCoroutine);
            silkCoroutine = null;
        }

        private static IEnumerator RestoreSilk(HeroController hc)
        {
            const float interval = 0.25f;

            while (hc != null)
            {
                var plugin = FindAnyObjectByType<AbilityTogglePlugin>();
                if (plugin == null || !plugin.sylphsongBehavior)
                    yield break;

                hc.AddSilk(1, false);
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
