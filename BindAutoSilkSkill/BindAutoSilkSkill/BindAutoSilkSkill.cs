using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

[BepInPlugin(
    "silksong.bind.autoskill.selectable",
    "Bind Auto Silk Skill",
    "1.1.0"
)]
public class BindAutoSilkSkill : BaseUnityPlugin
{
    // ───────── CONFIG ─────────
    private static ConfigEntry<bool> Enabled;
    private static ConfigEntry<bool> RotateSkills;
    private static ConfigEntry<SilkSkill> SelectedSkill;

    // NEW HOTKEY CONFIG
    private static ConfigEntry<KeyboardShortcut> ToggleHotkey;

    private static ManualLogSource Log;

    // Bind tracking for Multibinder
    private static int consecutiveBinds = 0;
    private static float lastBindTime = 0f;

    // Rotation state
    private static int rotationIndex = 0;

    // Reference to plugin instance for coroutines
    private static BindAutoSilkSkill instance;

    private enum SilkSkill
    {
        Silkspear,
        ThreadStorm,
        SharpDart,
        CrossStitch,
        RuneRage,
        PaleNails
    }

    private static readonly SilkSkill[] RotationOrder =
    {
        SilkSkill.Silkspear,
        SilkSkill.ThreadStorm,
        SilkSkill.SharpDart,
        SilkSkill.CrossStitch,
        SilkSkill.RuneRage,
        SilkSkill.PaleNails
    };

    // ───────── UNITY ─────────
    private void Awake()
    {
        instance = this;

        Enabled = Config.Bind(
            "General",
            "Enable",
            true,
            "Automatically cast a silk skill after bind"
        );

        RotateSkills = Config.Bind(
            "General",
            "Rotate Skills",
            false,
            "Rotate between silk skills after each bind"
        );

        SelectedSkill = Config.Bind(
            "General",
            "Auto Silk Skill",
            SilkSkill.PaleNails,
            "Silk skill to auto-cast when rotation is disabled"
        );

        // ───────── HOTKEY ─────────
        ToggleHotkey = Config.Bind(
            "Hotkeys",
            "Toggle Mod",
            new KeyboardShortcut(KeyCode.F5),
            "Toggle the auto silk skill mod ON/OFF"
        );

        Log = Logger;

        new Harmony("silksong.bind.autoskill.selectable.harmony").PatchAll();

        Log.LogInfo("[BindAutoSkill] Plugin loaded!");
    }

    // ───────── HOTKEY UPDATE ─────────
    private void Update()
    {
        if (ToggleHotkey.Value.IsDown())
        {
            Enabled.Value = !Enabled.Value;

            Log.LogInfo(
                $"[BindAutoSkill] Auto Skill " +
                $"{(Enabled.Value ? "Enabled" : "Disabled")}"
            );
        }
    }

    // ─────────────────────────────────────────────
    // BIND INTERRUPTED HOOK - Reset counter
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), "BindInterrupted")]
    private static class Patch_BindInterrupted
    {
        private static void Postfix()
        {
            if (!Enabled.Value)
                return;

            consecutiveBinds = 0;
            lastBindTime = 0f;
        }
    }

    // ─────────────────────────────────────────────
    // BIND COMPLETION HOOK
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    private static class Patch_BindCompleted
    {
        private static void Postfix()
        {
            if (!Enabled.Value)
                return;

            HeroController hc = HeroController.instance;
            PlayerData pd = PlayerData.instance;

            if (!hc)
                return;

            float currentTime = Time.time;

            if (currentTime - lastBindTime > 5f)
                consecutiveBinds = 0;

            lastBindTime = currentTime;
            consecutiveBinds++;

            bool hasMultibinder = Gameplay.MultibindTool.Status.IsEquipped;

            bool isSingleBindCrest =
                pd.CurrentCrestID == "Warrior" ||
                pd.CurrentCrestID == "Witch";

            int requiredBinds =
                !hasMultibinder ? 1 :
                isSingleBindCrest ? 1 :
                2;

            if (consecutiveBinds < requiredBinds)
                return;

            // Pick skill
            SilkSkill skill = RotateSkills.Value
                ? RotationOrder[
                    rotationIndex = (rotationIndex + 1) % RotationOrder.Length
                  ]
                : SelectedSkill.Value;

            // Trigger skill
            if (
                skill == SilkSkill.SharpDart ||
                skill == SilkSkill.CrossStitch
            )
            {
                if (
                    pd.CurrentCrestID == "Warrior" &&
                    hasMultibinder
                )
                {
                    instance.StartCoroutine(
                        DelayedTrigger(hc, 0.5f)
                    );
                }
                else
                {
                    instance.StartCoroutine(
                        DelayedTrigger(hc, 0.34f)
                    );
                }
            }
            else
            {
                if (
                    skill == SilkSkill.Silkspear &&
                    pd.CurrentCrestID == "Witch"
                )
                {
                    instance.StartCoroutine(
                        DelayedTrigger(hc, 0.2f)
                    );
                }

                TriggerSilkSkill(hc);
            }

            consecutiveBinds = 0;
        }

        private static IEnumerator DelayedTrigger(
            HeroController hc,
            float delay
        )
        {
            yield return new WaitForSeconds(delay);

            TriggerSilkSkill(hc);
        }
    }

    // ─────────────────────────────────────────────
    // CORE SKILL LOGIC
    // ─────────────────────────────────────────────
    private static void TriggerSilkSkill(HeroController hc)
    {
        SilkSkill skill =
            RotateSkills.Value
                ? RotationOrder[rotationIndex % RotationOrder.Length]
                : SelectedSkill.Value;

        PlayMakerFSM fsm =
            typeof(HeroController)
            .GetField(
                "skillEventTarget",
                BindingFlags.Instance | BindingFlags.NonPublic
            )
            ?.GetValue(hc) as PlayMakerFSM;

        if (!fsm)
            return;

        string evt = skill switch
        {
            SilkSkill.Silkspear => "NEEDLE THROW",
            SilkSkill.ThreadStorm => "THREAD SPHERE",
            SilkSkill.SharpDart => "SILK CHARGE",
            SilkSkill.CrossStitch => "PARRY",
            SilkSkill.RuneRage => "SILK BOMB",
            SilkSkill.PaleNails => "BOSS NEEDLE",
            _ => null
        };

        if (evt == null)
            return;

        // Grant temporary silk
        hc.AddSilk(4, false);

        // Trigger skill
        fsm.SendEvent(evt);

        Log.LogInfo($"[BindAutoSkill] Cast → {skill}");
    }
}