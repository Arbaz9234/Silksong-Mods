using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

[BepInPlugin("silksong.act3.start.early", "Act 3 Start Early", "1.0")]
[BepInProcess("Hollow Knight Silksong.exe")]
public class Act3StartEarly : BaseUnityPlugin
{
    private ConfigEntry<bool> enableAct3;

    private void Awake()
    {
        enableAct3 = Config.Bind(
            "Gameplay",
            "Enable Act 3 Early",
            false,
            "Toggle soulSnareReady (allows Act 3 to start early)"
        );

        SceneManager.sceneLoaded += OnSceneLoaded;
        Logger.LogInfo("Act 3 Start Early (toggle) loaded");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //On Entering the Cradle Scene
        if (scene.name != "Cradle_03")
            return;

        Apply();
    }

    private void Apply()
    {
        if (PlayerData.instance == null)
            return;

        PlayerData.instance.soulSnareReady = enableAct3.Value;

        Logger.LogInfo(
            enableAct3.Value
                ? "Soul Snare set to READY (Act 3 enabled)"
                : "Soul Snare set to NOT READY (Act 3 disabled)"
        );
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
