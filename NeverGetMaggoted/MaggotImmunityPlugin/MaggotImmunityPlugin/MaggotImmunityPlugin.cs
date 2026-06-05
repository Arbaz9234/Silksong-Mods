using BepInEx;
using HarmonyLib;

[BepInPlugin(
    "silksong.maggot.immunity",
    "Silksong Maggot Immunity",
    "3.0.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class MaggotImmunityPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("silksong.maggot.immunity.harmony").PatchAll();
        Logger.LogInfo("Maggot Immunity loaded (pool + enemies)");
    }

    // ─────────────────────────────────────────────
    // 1️⃣ BLOCK MAGGOT POOLS (WATER REGIONS)
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(MaggotRegion), "OnHeroEnteredWater")]
    private static class BlockMaggotPoolEntry
    {
        private static bool Prefix()
        {
            // skip maggot pool logic
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // 2️⃣ BLOCK MAGGOT STATUS FROM ENEMIES / SCRIPTS
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.SetIsMaggoted))]
    private static class BlockMaggotedPatch
    {
        private static bool Prefix(bool value)
        {
            // Block APPLY, allow CLEAR
            if (value)
                return false;

            return true;
        }
    }
}
