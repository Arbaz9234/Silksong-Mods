using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[BepInPlugin(
    "com.arbaz9234.noshamanrunes",
    "Disable Shaman Runes",
    "1.7.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class DisableShamanRunes : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> DisableRuneSymbol;
    internal static ConfigEntry<bool> DisableZapVisuals;

    private void Awake()
    {
        Log = Logger;

        DisableRuneSymbol = Config.Bind(
            "Visuals",
            "Disable Rune Symbol",
            true,
            "Disables the purple rune symbol that appears on attacks"
        );

        DisableZapVisuals = Config.Bind(
            "Visuals",
            "Disable Zap Visuals",
            false,
            "Disables zap color tint (removes purple color from attacks)"
        );

        new Harmony("com.arbaz9234.noshamanrunes").PatchAll();
        Log.LogInfo("Disable Shaman Runes v1.7.0 loaded");
    }

    [HarmonyPatch(typeof(HeroShamanRuneEffect), nameof(HeroShamanRuneEffect.Refresh))]
    private static class Patch_ShadowRuneRefresh
    {
        private static void Postfix(HeroShamanRuneEffect __instance)
        {
            if (__instance == null)
                return;

            try
            {
                // Get all necessary fields
                var runeField = AccessTools.Field(typeof(HeroShamanRuneEffect), "rune");
                var spriteField = AccessTools.Field(typeof(HeroShamanRuneEffect), "zapTintSprites");
                var particleField = AccessTools.Field(typeof(HeroShamanRuneEffect), "zapTintParticles");
                var disableIfZapField = AccessTools.Field(typeof(HeroShamanRuneEffect), "disableIfZap");
                var initialSpriteColoursField = AccessTools.Field(typeof(HeroShamanRuneEffect), "initialSpriteColours");
                var initialParticleColoursField = AccessTools.Field(typeof(HeroShamanRuneEffect), "initialParticleColours");
                var runeSpawnEffectField = AccessTools.Field(typeof(HeroShamanRuneEffect), "runeSpawnEffect");
                var spawnOffsetField = AccessTools.Field(typeof(HeroShamanRuneEffect), "spawnOffset");
                var spawnScaleField = AccessTools.Field(typeof(HeroShamanRuneEffect), "spawnScale");
                var spawnDelayField = AccessTools.Field(typeof(HeroShamanRuneEffect), "spawnDelay");
                var spawnMultField = AccessTools.Field(typeof(HeroShamanRuneEffect), "spawnMult");

                var rune = runeField?.GetValue(__instance) as GameObject;
                var sprites = spriteField?.GetValue(__instance) as List<SpriteRenderer>;
                var particles = particleField?.GetValue(__instance) as List<ParticleSystem>;
                var disableIfZap = disableIfZapField?.GetValue(__instance) as GameObject[];
                var initialSpriteColours = initialSpriteColoursField?.GetValue(__instance) as Dictionary<SpriteRenderer, Color>;
                var initialParticleColours = initialParticleColoursField?.GetValue(__instance) as Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient>;
                var runeSpawnEffect = runeSpawnEffectField?.GetValue(__instance) as GameObject;
                var spawnOffset = (Vector3)(spawnOffsetField?.GetValue(__instance) ?? Vector3.zero);
                var spawnScale = (Vector3)(spawnScaleField?.GetValue(__instance) ?? Vector3.one);
                var spawnDelay = (float)(spawnDelayField?.GetValue(__instance) ?? 0f);
                var spawnMult = (float)(spawnMultField?.GetValue(__instance) ?? 1f);

                // ───── Feature 1: Just hide the rune symbol ─────
                if (DisableRuneSymbol.Value)
                {
                    if (rune != null && rune.activeSelf)
                    {
                        rune.SetActive(false);
                        Log.LogInfo("Disabled rune symbol");
                    }
                }
                // ───── Feature 2: Disable zap visuals but keep normal particles ─────
                else if (DisableZapVisuals.Value)
                {
                    // First, hide the rune to remove zap effects
                    if (rune != null && rune.activeSelf)
                    {
                        rune.SetActive(false);
                        Log.LogInfo("Disabled rune for zap removal");
                    }

                    // Re-enable normal particles that were disabled by zap
                    if (disableIfZap != null && disableIfZap.Length > 0)
                    {
                        foreach (var obj in disableIfZap)
                        {
                            if (obj != null && !obj.activeSelf)
                            {
                                obj.SetActive(true);
                                Log.LogInfo($"Re-enabled normal particles: {obj.name}");
                            }
                        }
                    }

                    // Respawn particles without zap tint using runeSpawnEffect
                    if (runeSpawnEffect != null)
                    {
                        Transform targetTransform = rune != null ? rune.transform : __instance.transform;

                        // Spawn the effect
                        GameObject spawnedEffect = Object.Instantiate(
                            runeSpawnEffect,
                            targetTransform.TransformPoint(spawnOffset),
                            Quaternion.identity
                        );

                        spawnedEffect.transform.localScale = targetTransform.TransformVector(spawnScale);

                        // Set up following components if they exist
                        var followTransform = spawnedEffect.GetComponent<FollowTransform>();
                        if (followTransform != null)
                        {
                            followTransform.Target = targetTransform;
                        }

                        var followRotation = spawnedEffect.GetComponent<FollowRotation>();
                        if (followRotation != null)
                        {
                            followRotation.Target = targetTransform;
                        }

                        // Configure particle system
                        var spawnedPS = spawnedEffect.GetComponent<ParticleSystem>();
                        if (spawnedPS != null)
                        {
                            var originalPS = runeSpawnEffect.GetComponent<ParticleSystem>();
                            if (originalPS != null)
                            {
                                var main = spawnedPS.main;
                                main.startDelay = spawnDelay;

                                var emission = spawnedPS.emission;
                                emission.rateOverTimeMultiplier = spawnMult * originalPS.emission.rateOverTimeMultiplier;

                                // Remove zap tint from spawned particles
                                main.startColor = new ParticleSystem.MinMaxGradient(Color.white);

                                spawnedPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                spawnedPS.Play();
                            }
                        }

                        Log.LogInfo("Spawned non-zap particle effect");
                    }

                    // Remove color tint from any remaining sprites
                    if (sprites != null && initialSpriteColours != null)
                    {
                        foreach (var sr in sprites)
                        {
                            if (sr != null && initialSpriteColours.ContainsKey(sr))
                            {
                                sr.color = initialSpriteColours[sr];
                                Log.LogInfo($"Reset sprite color: {sr.name}");
                            }
                        }
                    }

                    // Remove color tint from any remaining particles
                    if (particles != null && initialParticleColours != null)
                    {
                        foreach (var ps in particles)
                        {
                            if (ps != null && initialParticleColours.ContainsKey(ps))
                            {
                                var main = ps.main;
                                main.startColor = initialParticleColours[ps];
                                Log.LogInfo($"Reset particle color: {ps.name}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.LogInfo($"Error in Postfix patch: {ex}");
            }
        }
    }
}