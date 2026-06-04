using BepInEx;
using HarmonyLib;

[BepInPlugin(
    "silksong.never.maggoted",
    "Silksong Never Maggoted",
    "1.0.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class NeverMaggotedPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("silksong.never.maggoted.harmony").PatchAll();
        Logger.LogInfo("Never Maggoted mod loaded");
    }

    // ─────────────────────────────────────────────
    // BLOCK MAGGOT STATE AT SOURCE
    // ─────────────────────────────────────────────
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.SetIsMaggoted))]
    private static class BlockMaggotedPatch
    {
        private static bool Prefix(bool value)
        {
            // If game tries to APPLY maggoted → block it
            if (value)
                return false;

            // Allow clearing maggoted (just in case)
            return true;
        }
    }
}
