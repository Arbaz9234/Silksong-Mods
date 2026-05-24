using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using GlobalSettings;


[BepInPlugin(
    "com.arbaz9234.doublecresteffects",
    "Double Beast & Reaper Crest Effects",
    "1.2.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class DoubleBeastAndReaperCrestEffects : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private const float DurationMultiplier = 2f;
    internal static bool DamageResetInProgress;

    // ───────────── CONFIG ─────────────
    internal static ConfigEntry<bool> EnableReaper;
    internal static ConfigEntry<bool> EnableBeastDuration;
    internal static ConfigEntry<int> ReaperSilkMultiplier;
    internal static ConfigEntry<int> BeastImmediateHealCount;

    private void Awake()
    {
        Log = Logger;

        EnableReaper = Config.Bind(
            "Reaper Crest",
            "Enable Reaper Crest 2x Duration",
            true,
            "Doubles the duration of Reaper Crest Bind effect"
        );

        EnableBeastDuration = Config.Bind(
            "Beast Crest",
            "Enable Beast Crest 2x Duration",
            true,
            "Doubles the duration of Beast Crest Fury and heal cap"
        );

        ReaperSilkMultiplier = Config.Bind(
            "Reaper Crest",
            "Silk Ball Multiplier",
            2,
            new ConfigDescription(
                "Multiplier for silk balls spawned during Reaper mode (1 = vanilla)",
                new AcceptableValueRange<int>(1, 5)
            )
        );

        BeastImmediateHealCount = Config.Bind(
            "Beast Crest",
            "Immediate Heal Count",
            1,
            new ConfigDescription(
                "Select count for immediate heal with beast crest bind",
                new AcceptableValueRange<int>(0, 2)
            )
        );

        new Harmony("com.arbaz9234.doublecresteffects").PatchAll();
        Log.LogInfo("Double Beast & Reaper Crest Effects loaded");
    }

    // ─────────────────────────────────────────────
    // 1️⃣ Reaper Crest — 2× duration
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    private static class Patch_ReaperCrest_Duration
    {
        private static void Postfix(HeroController __instance)
        {

            if (!EnableReaper.Value)
                return;

            var reaperRef =
                AccessTools.FieldRefAccess<
                    HeroController,
                    HeroController.ReaperCrestStateInfo
                >("reaperState");

            var reaperState = reaperRef(__instance);

            if (!reaperState.IsInReaperMode || reaperState.ReaperModeDurationLeft <= 0f)
                return;

            reaperState.ReaperModeDurationLeft *= DurationMultiplier;
            reaperRef(__instance) = reaperState;
        }
    }

    // ─────────────────────────────────────────────
    // 2️⃣ Beast Crest — 2× duration
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    private static class Patch_BeastCrest_Duration
    {
        private static void Postfix(HeroController __instance)
        {
            if (!EnableBeastDuration.Value)
                return;

            var warriorRef =
                AccessTools.FieldRefAccess<
                    HeroController,
                    HeroController.WarriorCrestStateInfo
                >("warriorState");

            var state = warriorRef(__instance);

            if (!state.IsInRageMode || state.RageTimeLeft <= 0f)
                return;

            state.RageTimeLeft *= DurationMultiplier;
            warriorRef(__instance) = state;
        }
    }

    // ─────────────────────────────────────────────
    // 3️⃣ Beast Crest — 2× heal cap (HUD + logic)
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.GetRageModeHealCap))]
    private static class Patch_BeastCrest_HealCap
    {
        private static void Postfix(HeroController __instance, ref int __result)
        {
            if (!EnableBeastDuration.Value)
                return;
            var warriorRef =
                AccessTools.FieldRefAccess<
                    HeroController,
                    HeroController.WarriorCrestStateInfo
                >("warriorState");

            var state = warriorRef(__instance);

            if (!state.IsInRageMode)
                return;

            __result *= 2;
        }
    }
    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    public static class Patch_BindCompleted_ApplyHealing
    {
        [HarmonyPostfix]
        private static void AddHealthPostFix(HeroController __instance)
        {
            if (!EnableBeastDuration.Value || BeastImmediateHealCount.Value == 0)
                return;
            if (!((ToolBase)Gameplay.WarriorCrest).IsEquipped)
                return;
            __instance.AddHealth(BeastImmediateHealCount.Value);
        }
    }

    // ─────────────────────────────────────────────
    // 4️⃣ Prevent Reaper reset on damage
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), "TakeDamage")]
    private static class Patch_ReaperCrest_DamageDetected
    {
        private static void Prefix() => DamageResetInProgress = true;
        private static void Postfix() => DamageResetInProgress = false;
    }

    [HarmonyPatch(typeof(HeroController), "ResetReaperCrestState")]
    private static class Patch_ReaperCrest_BlockResetOnDamage
    {
        private static bool Prefix(HeroController __instance)
        {
            if (!EnableReaper.Value)
                return true;

            var reaperRef =
                AccessTools.FieldRefAccess<
                    HeroController,
                    HeroController.ReaperCrestStateInfo
                >("reaperState");

            var state = reaperRef(__instance);

            // Block reset ONLY if caused by damage
            if (state.IsInReaperMode && DamageResetInProgress)
                return false;

            return true;
        }
    }

    // ─────────────────────────────────────────────
    // 5️⃣ Reaper Crest — Extra silk balls (1×–5×)
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
    private static class Patch_ReaperCrest_ExtraSilkBalls
    {
        private static void Postfix(
            HealthManager __instance,
            HitInstance hitInstance
        )
        {
            if (!EnableReaper.Value)
                return;

            var hc = HeroController.instance;
            if (hc == null || !hc.ReaperState.IsInReaperMode)
                return;

            if (__instance.DoNotGiveSilk)
                return;

            if (hitInstance.SilkGeneration == HitSilkGeneration.None)
                return;

            int mult = ReaperSilkMultiplier.Value;
            if (mult <= 1)
                return;

            int extra = mult - 1;

            for (int i = 0; i < extra; i++)
            {
                FlingUtils.SpawnAndFling(
                    new FlingUtils.Config
                    {
                        Prefab = Gameplay.ReaperBundlePrefab,
                        AmountMin = 1,
                        AmountMax = 1,
                        SpeedMin = 25f,
                        SpeedMax = 50f,
                        AngleMin = 0f,
                        AngleMax = 360f
                    },
                    __instance.transform,
                    __instance.EffectOrigin,
                    null,
                    -1f
                );
            }
        }
    }
}
