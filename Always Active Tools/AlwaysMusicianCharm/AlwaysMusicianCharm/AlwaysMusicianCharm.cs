using BepInEx;
using GlobalSettings;
using HarmonyLib;

[BepInPlugin("com.arbaz9234.alwaysmusiciancharm", "Always Musician Charm", "1.0.0")]
[BepInProcess("Hollow Knight Silksong.exe")]
public class AlwaysMusicianCharm : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("com.arbaz9234.alwaysmusiciancharm").PatchAll();
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.IsEquipped), MethodType.Getter)]
    private static class IsEquipped_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem __instance, ref bool __result)
        {
            if (__instance == Gameplay.MusicianCharmTool)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.IsEquippedHud), MethodType.Getter)]
    private static class IsEquippedHud_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem __instance, ref bool __result)
        {
            if (__instance == Gameplay.MusicianCharmTool)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ToolItemManager), nameof(ToolItemManager.IsToolEquipped))]
    private static class IsToolEquipped_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem tool, ref bool __result)
        {
            if (tool == Gameplay.MusicianCharmTool)
                __result = true;
        }
    }
}