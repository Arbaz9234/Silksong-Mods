using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[BepInPlugin(
    "silksong.pollip.pouch.2x",
    "Pollip Pouch 2X Effect",
    "1.0.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class PollipPouch2XEffect : BaseUnityPlugin
{
    private static ConfigEntry<int> PoisonTickMultiplier;
    private static ConfigEntry<int> PoisonDamageMultiplier;
    private static ConfigEntry<bool> StackPoison;
    private static ManualLogSource logger;

    // Track poison stacks per enemy
    private static Dictionary<int, int> enemyPoisonStacks = new Dictionary<int, int>();

    // Track the last enemy hit (temporary storage during hit processing)
    private static int lastHitEnemyId = 0;

    // Prevent duplicate increments - track last frame we incremented each enemy
    private static Dictionary<int, int> lastIncrementFrame = new Dictionary<int, int>();

    // Track when poison was last applied to detect when it's finished
    private static Dictionary<int, float> lastPoisonTime = new Dictionary<int, float>();

    // Track remaining ticks for each enemy to know when poison expires
    private static Dictionary<int, int> remainingPoisonTicks = new Dictionary<int, int>();

    private void Awake()
    {
        logger = Logger;

        PoisonTickMultiplier = Config.Bind(
            "Pollip Pouch",
            "Poison Tick Multiplier",
            2,
            new ConfigDescription(
                "Multiply poison damage ticks (2 = 4 ticks, 3 = 6 ticks, etc.)",
                new AcceptableValueRange<int>(1, 10)
            )
        );
        PoisonDamageMultiplier = Config.Bind(
            "Pollip Pouch",
            "Poison Damage Multiplier",
            1,
            new ConfigDescription(
                "Multiply poison damage per tick (default = 1)",
                new AcceptableValueRange<int>(1, 10)
            )
        );
        StackPoison = Config.Bind(
            "Pollip Pouch",
            "Stack Poison",
            true,
            "If true, poison ticks stack instead of override"
        );

        logger.LogInfo("=== Pollip Pouch 2X Effect Loaded ===");
        logger.LogInfo($"Poison Tick Multiplier: {PoisonTickMultiplier.Value}");
        logger.LogInfo($"Poison Damage Multiplier: {PoisonDamageMultiplier.Value}");
        logger.LogInfo($"Stack Poison: {StackPoison.Value}");

        new Harmony("silksong.pollip.pouch.2x").PatchAll();
    }

    // ─────────────────────────────────────────────
    // CAPTURE ENEMY HIT - Track which enemy was hit
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(DamageEnemies), "OnTriggerEnter2D")]
    private static class CaptureEnemyHitPatch
    {
        private static void Prefix(DamageEnemies __instance, Collider2D collision)
        {
            if (!Gameplay.PoisonPouchTool.Status.IsEquipped || !StackPoison.Value)
                return;

            // Get the enemy's root GameObject
            GameObject hitObject = collision.gameObject;
            if (hitObject == null)
                return;

            Transform enemyRoot = hitObject.transform.root;
            if (enemyRoot == null)
                return;

            int enemyId = enemyRoot.GetInstanceID();
            int currentFrame = Time.frameCount;
            float currentTime = Time.time;

            // Check if poison has expired (more than 2 seconds since last poison damage)
            if (lastPoisonTime.TryGetValue(enemyId, out float lastTime))
            {
                if (currentTime - lastTime > 2.0f)
                {
                    enemyPoisonStacks[enemyId] = 0;
                    remainingPoisonTicks[enemyId] = 0;
                }
            }

            // Check if we already incremented this enemy this frame
            if (lastIncrementFrame.TryGetValue(enemyId, out int lastFrame))
            {
                if (lastFrame == currentFrame)
                {
                    lastHitEnemyId = enemyId; // Still update this for getter
                    return;
                }
            }

            // Update last increment frame
            lastIncrementFrame[enemyId] = currentFrame;
            lastPoisonTime[enemyId] = currentTime;

            // Store the enemy we just hit
            lastHitEnemyId = enemyId;

            // Increment stack count for this enemy
            if (!enemyPoisonStacks.ContainsKey(enemyId))
            {
                enemyPoisonStacks[enemyId] = 1;
            }
            else
            {
                enemyPoisonStacks[enemyId]++;
            }
        }
    }

    // ─────────────────────────────────────────────
    // BLOCK OVERRIDE - Prevent game from setting poison
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(DamageEnemies), "OverridePoisonDamage")]
    private static class BlockOverridePatch
    {
        private static bool Prefix(DamageEnemies __instance, int value)
        {
            if (!Gameplay.PoisonPouchTool.Status.IsEquipped || !StackPoison.Value)
                return true; // Let game handle it normally


            // Do nothing - completely block the game's poison setting
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // POISON MULTIPLIER - Inject our stacked poison ticks
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(DamageEnemies), "PoisonDamageTicks", MethodType.Getter)]
    private static class PoisonTicksPatch
    {
        private static void Postfix(DamageEnemies __instance, ref int __result)
        {
            // Pollip Pouch not equipped
            if (!Gameplay.PoisonPouchTool.Status.IsEquipped)
                return;

            int multiplier = PoisonTickMultiplier.Value;

            // If stacking is enabled and we have a recent hit
            if (StackPoison.Value && lastHitEnemyId != 0)
            {
                if (enemyPoisonStacks.TryGetValue(lastHitEnemyId, out int stackCount))
                {
                    // Base poison ticks (from game)
                    int baseTicks = 2; // Default base value

                    // Calculate total ticks: base * multiplier * stack count
                    int totalTicks = baseTicks * multiplier * stackCount;

                    // Track remaining ticks
                    remainingPoisonTicks[lastHitEnemyId] = totalTicks;

                    __result = totalTicks;

                    return;
                }
            }

            // Default behavior: just apply multiplier if no stacking
            if (__result > 0 && multiplier > 1)
            {
                int original = __result;
                __result *= multiplier;
            }
        }
    }
    // ─────────────────────────────────────────────
    // CLEANUP
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(DamageEnemies), "OnDestroy")]
    private static class CleanupPatch
    {
        private static void Prefix(DamageEnemies __instance)
        {
            GameObject obj = __instance.gameObject;
            if (obj != null && obj.transform.root != null)
            {
                int enemyId = obj.transform.root.GetInstanceID();
                if (enemyPoisonStacks.ContainsKey(enemyId))
                {
                    logger.LogInfo($"[Cleanup] Removed poison tracking for enemy {enemyId}");
                    enemyPoisonStacks.Remove(enemyId);
                    lastIncrementFrame.Remove(enemyId);
                    lastPoisonTime.Remove(enemyId);
                    remainingPoisonTicks.Remove(enemyId);
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // POISON DAMAGE MULTIPLIER 
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(DamageTag), "OnHit")]
    public static class PollipPoisonDamagePatch
    {
        private static Dictionary<DamageTag, int> originalDamages = new Dictionary<DamageTag, int>();

        [HarmonyPrefix]
        private static void Prefix(DamageTag __instance)
        {
            if (!Gameplay.PoisonPouchTool.Status.IsEquipped)
                return;

            int multiplier = PoisonDamageMultiplier.Value;
            if (multiplier <= 1)
                return;

            var traverse = Traverse.Create(__instance);

            if (!originalDamages.ContainsKey(__instance))
            {
                int damage = traverse.Field("damageAmount").GetValue<int>();
                originalDamages[__instance] = damage;
            }

            int scaledDamage = originalDamages[__instance] * multiplier;
            traverse.Field("damageAmount").SetValue(scaledDamage);
        }

        [HarmonyPostfix]
        private static void Postfix(DamageTag __instance)
        {
            if (originalDamages.TryGetValue(__instance, out int originalDamage))
            {
                Traverse.Create(__instance)
                    .Field("damageAmount")
                    .SetValue(originalDamage);
            }
        }
    }
}