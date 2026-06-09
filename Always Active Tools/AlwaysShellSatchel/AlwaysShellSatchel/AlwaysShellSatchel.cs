using BepInEx;
using GlobalSettings;
using HarmonyLib;

[BepInPlugin("com.arbaz9234.alwaysshellsatchel", "Always Shell Satchel", "1.0.0")]
[BepInProcess("Hollow Knight Silksong.exe")]
public class AlwaysShellSatchel : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("com.arbaz9234.alwaysshellsatchel").PatchAll();
        Logger.LogInfo("Always Shell Satchel loaded");
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.IsEquipped), MethodType.Getter)]
    private static class IsEquipped_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem __instance, ref bool __result)
        {
            if (__instance == Gameplay.ShellSatchelTool)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ToolItem), nameof(ToolItem.IsEquippedHud), MethodType.Getter)]
    private static class IsEquippedHud_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem __instance, ref bool __result)
        {
            if (__instance == Gameplay.ShellSatchelTool)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ToolItemManager), nameof(ToolItemManager.IsToolEquipped))]
    private static class IsToolEquipped_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ToolItem tool, ref bool __result)
        {
            if (tool == Gameplay.ShellSatchelTool)
                __result = true;
        }
    }
}
