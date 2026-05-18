using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

[BepInPlugin(
    "com.arbaz9234.alwayshitkratt",
    "Always Hit Kratt",
    "1.3.0"
)]
[BepInProcess("Hollow Knight Silksong.exe")]
public class AlwaysHitKratt : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> ToggleKrattHit;
    private const string KrattSpaScene = "Room_Caravan_Spa";
    private bool hasCompletedSequence = false;

    private void Awake()
    {
        Log = Logger;
        ToggleKrattHit = Config.Bind(
            "Kratt",
            "Enable Always Hit Kratt",
            true,
            "Allows hitting Kratt repeatedly by resetting hit-state after completing the sequence"
        );

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Log.LogInfo("Always Hit Kratt loaded v1.3.0");
    }

    private void OnActiveSceneChanged(Scene from, Scene to)
    {
        if (!ToggleKrattHit.Value || PlayerData.instance == null)
            return;

        Log.LogInfo($"Scene changed: {from.name} -> {to.name}");
        Log.LogInfo($"CaravanLechSpaAttacked: {PlayerData.instance.CaravanLechSpaAttacked}");
        Log.LogInfo($"CaravanLechWoundedSpoken: {PlayerData.instance.CaravanLechWoundedSpoken}");

        // Check if both flags are true (sequence completed)
        if (PlayerData.instance.CaravanLechSpaAttacked &&
            PlayerData.instance.CaravanLechWoundedSpoken)
        {
            hasCompletedSequence = true;
        }

        // If sequence was completed and we're changing scenes, reset the flags
        if (hasCompletedSequence)
        {
            PlayerData.instance.CaravanLechSpaAttacked = false;
            PlayerData.instance.CaravanLechWoundedSpoken = false;
            hasCompletedSequence = false;
            Log.LogInfo("[Kratt] Hit state reset - ready to hit again!");
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }
}