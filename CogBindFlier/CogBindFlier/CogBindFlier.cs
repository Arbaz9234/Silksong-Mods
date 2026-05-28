using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[BepInPlugin("org.arbaz.cogbindflier", "Bind Cogwork Flier", "1.2")]
public class BindCogworkFlier : BaseUnityPlugin
{
    public static ConfigEntry<bool> EnableMod;
    public static ConfigEntry<int> ThrowsPerBind;
    public static ConfigEntry<bool> InfiniteCogflyDurability;

    public static bool ForcedThrow = false;
    public static ToolItem ForcedTool = null;

    private static readonly AccessTools.FieldRef<HeroController, ToolItem> f_willThrowTool =
        AccessTools.FieldRefAccess<HeroController, ToolItem>("willThrowTool");

    private static readonly MethodInfo m_ThrowTool =
        AccessTools.Method(typeof(HeroController), "ThrowTool", new Type[] { typeof(bool) });

    private static readonly MethodInfo m_GetCurrentEquippedTools =
        AccessTools.Method(typeof(ToolItemManager), "GetCurrentEquippedTools");

    private void Awake()
    {
        EnableMod = Config.Bind(
            "General",
            "Enable Mod",
            true,
            "Enable or disable the entire mod."
        );

        ThrowsPerBind = Config.Bind(
            "General",
            "Cogflies Per Bind",
            1,
            new ConfigDescription(
                "Number of Cogwork Fliers released per Bind.",
                new AcceptableValueRange<int>(1, 8)
            )
        );

        InfiniteCogflyDurability = Config.Bind(
            "General",
            "Infinite Cogfly Durability",
            false,
            "Your cog buddies won't get destroyed after dealing hits."
        );

        new Harmony("org.arbaz.cogbindflier").PatchAll();
        Logger.LogInfo("Bind Cogwork Flier v1.1.0 loaded");
    }

    private void Update()
    {
        if (!EnableMod.Value) return;
        if (HeroController.instance == null) return;

        HandleResetHotkey();

        var toolData = PlayerData.instance.GetToolData("Cogwork Flier");
        int maxCapacity = GetMaxCapacity();

        bool isEquipped = IsCogFlierEquipped();

        // Only auto-refill if NOT explicitly equipped
        if (!isEquipped && toolData.AmountLeft < 1)
        {
            toolData.AmountLeft = maxCapacity;
            PlayerData.instance.SetToolData("Cogwork Flier", toolData);
        }
    }

    private void HandleResetHotkey()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.R))
        {
            GameObject[] activeFlies = GameObject.FindGameObjectsWithTag("Knight Hatchling");

            foreach (var fly in activeFlies)
                GameObject.Destroy(fly);

            var toolData = PlayerData.instance.GetToolData("Cogwork Flier");
            toolData.AmountLeft = GetMaxCapacity();
            PlayerData.instance.SetToolData("Cogwork Flier", toolData);

            ToolItemManager.SendEquippedChangedEvent(true);

            Logger.LogInfo("[BindCogworkFlier] Manual reset triggered.");
        }
    }

    private static bool IsCogFlierEquipped()
    {
        var equipped = m_GetCurrentEquippedTools?.Invoke(null, null) as List<ToolItem>;
        if (equipped == null) return false;

        return equipped.Any(t => t != null && t.name == "Cogwork Flier");
    }

    private static int GetMaxCapacity()
    {
        int pouch = PlayerData.instance.ToolPouchUpgrades;

        return pouch switch
        {
            1 => 5,
            2 => 6,
            3 => 7,
            4 => 8,
            _ => 4
        };
    }

    // ─────────────────────────────────────────────
    // THROW AFTER BIND
    // ─────────────────────────────────────────────

    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    private class Patch_BindCompleted
    {
        private static void Postfix(HeroController __instance)
        {
            if (!EnableMod.Value) return;
            if (__instance == null) return;

            ToolItem cogFlier = ToolItemManager.GetToolByName("Cogwork Flier");
            if (!cogFlier) return;
            int maxCapacity = 8;
            int throwsRequested = Mathf.Clamp(ThrowsPerBind.Value, 1, 8);

            // 1️⃣ Spawn all requested first
            ForcedTool = cogFlier;
            ForcedThrow = true;

            for (int i = 0; i < throwsRequested; i++)
            {
                f_willThrowTool(__instance) = cogFlier;
                m_ThrowTool.Invoke(__instance, new object[] { false });
            }

            ForcedThrow = false;
            ForcedTool = null;

            // 2️⃣ Now check total and trim excess
            GameObject[] activeFlies = GameObject.FindGameObjectsWithTag("Knight Hatchling");

            int overflow = activeFlies.Length - maxCapacity;

            if (overflow > 0)
            {
                for (int i = 0; i < overflow; i++)
                {
                    GameObject.Destroy(activeFlies[i]);
                }
            }
            bool isEquipped = IsCogFlierEquipped();

            if (isEquipped)
                ToolItemManager.SendEquippedChangedEvent(true);
        }
    }

    // ─────────────────────────────────────────────
    // BYPASS EQUIP REQUIREMENT
    // ─────────────────────────────────────────────

    [HarmonyPatch(typeof(ToolItemManager), "GetAttackToolBinding")]
    private class Patch_GetAttackToolBinding
    {
        private static bool Prefix(ToolItem tool, ref AttackToolBinding? __result)
        {
            if (!EnableMod.Value)
                return true;

            if (!ForcedThrow || ForcedTool == null)
                return true;

            if (tool.name != ForcedTool.name)
                return true;

            __result = (AttackToolBinding)0;
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // PREVENT BREAK
    // ─────────────────────────────────────────────

    [HarmonyPatch(typeof(ToolItemLimiter), "Break")]
    private class Patch_ToolItemLimiter
    {
        private static bool Prefix()
        {
            if (!EnableMod.Value)
                return true;

            return false;
        }
    }
    [HarmonyPatch(typeof(ClockworkHatchling), "HitLanded")]
    private class Patch_InfiniteCogflyHits
    {
        private static void Postfix(object __instance)
        {
            if (!EnableMod.Value) return;
            if (!InfiniteCogflyDurability.Value) return;

            var traverse = Traverse.Create(__instance);

            int currentHp = traverse.Field("hpCurrent").GetValue<int>();

            // Boost HP massively so it never dies from hits
            traverse.Field("hpCurrent").SetValue(currentHp + 99999);
        }
    }
}
//using BepInEx;
//using BepInEx.Configuration;
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Reflection;

//[BepInPlugin("org.arbaz.cogbindflier", "Bind Cogwork Flier", "1.1.0")]
//public class BindCogworkFlier : BaseUnityPlugin
//{
//    public static ConfigEntry<bool> EnableMod;

//    public static bool ForcedThrow = false;
//    public static ToolItem ForcedTool = null;

//    private static readonly AccessTools.FieldRef<HeroController, ToolItem> f_willThrowTool =
//        AccessTools.FieldRefAccess<HeroController, ToolItem>("willThrowTool");

//    private static readonly MethodInfo m_ThrowTool =
//        AccessTools.Method(typeof(HeroController), "ThrowTool", new Type[] { typeof(bool) });

//    private static readonly MethodInfo m_GetCurrentEquippedTools =
//        AccessTools.Method(typeof(ToolItemManager), "GetCurrentEquippedTools");
//    private void Awake()
//    {
//        EnableMod = Config.Bind(
//            "General",
//            "Enable Mod",
//            true,
//            "Enable or disable the entire mod."
//        );

//        new Harmony("org.arbaz.cogbindflier").PatchAll();
//        Logger.LogInfo("Bind Cogwork Flier v1.0.0 loaded");
//    }

//    private void Update()
//    {
//        if (!EnableMod.Value) return;
//        if (HeroController.instance == null) return;

//        // Maintain stock based on pouch capacity
//        var toolData = PlayerData.instance.GetToolData("Cogwork Flier");

//        int pouchCapacity = PlayerData.instance.ToolPouchUpgrades;

//        int maxAmount =
//            pouchCapacity == 1 ? 5 :
//            pouchCapacity == 2 ? 6 :
//            pouchCapacity == 3 ? 7 :
//            pouchCapacity == 4 ? 8 : 4;

//        if (toolData.AmountLeft < 1)
//        {
//            toolData.AmountLeft = maxAmount;
//            PlayerData.instance.SetToolData("Cogwork Flier", toolData);

//            bool isEquipped = (m_GetCurrentEquippedTools?.Invoke(null, null) as List<ToolItem>)?.Exists(t => t?.name == "Cogwork Flier") == true;
//            if (isEquipped)
//                ToolItemManager.SendEquippedChangedEvent(true);
//        }
//    }

//    // ─────────────────────────────────────────────
//    // THROW AFTER BIND
//    // ─────────────────────────────────────────────

//    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
//    private class Patch_BindCompleted
//    {
//        private static void Postfix(HeroController __instance)
//        {
//            if (!EnableMod.Value) return;
//            if (__instance == null) return;

//            ToolItem cogFlier = ToolItemManager.GetToolByName("Cogwork Flier");
//            if (!cogFlier) return;

//            ForcedTool = cogFlier;
//            ForcedThrow = true;

//            f_willThrowTool(__instance) = cogFlier;
//            m_ThrowTool.Invoke(__instance, new object[] { false });

//            ForcedThrow = false;
//            ForcedTool = null;

//            bool isEquipped = (m_GetCurrentEquippedTools?.Invoke(null, null) as List<ToolItem>)?.Exists(t => t?.name == "Cogwork Flier") == true;
//            if (isEquipped)
//            ToolItemManager.SendEquippedChangedEvent(true);
//        }
//    }

//    // ─────────────────────────────────────────────
//    // BYPASS EQUIP REQUIREMENT
//    // ─────────────────────────────────────────────

//    [HarmonyPatch(typeof(ToolItemManager), "GetAttackToolBinding")]
//    private class Patch_GetAttackToolBinding
//    {
//        private static bool Prefix(ToolItem tool, ref AttackToolBinding? __result)
//        {
//            if (!EnableMod.Value)
//                return true;

//            if (!ForcedThrow || ForcedTool == null)
//                return true;

//            if (tool.name != ForcedTool.name)
//                return true;

//            __result = (AttackToolBinding)0;
//            return false;
//        }
//    }

//    // ─────────────────────────────────────────────
//    // PREVENT BREAK
//    // ─────────────────────────────────────────────

//    [HarmonyPatch(typeof(ToolItemLimiter), "Break")]
//    private class Patch_ToolItemLimiter
//    {
//        private static bool Prefix()
//        {
//            if (!EnableMod.Value)
//                return true;

//            return false;
//        }
//    }
//}