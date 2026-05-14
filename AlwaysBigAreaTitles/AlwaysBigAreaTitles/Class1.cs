using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;


[BepInPlugin(
    "silksong.arbaz9234.alwaysbigareatitles",
    "Always Big Area Titles",
    "1.0.0"
)]
public class AlwaysBigAreaTitles : BaseUnityPlugin
{
    internal static ConfigEntry<bool> EnableBigTitle;
    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        EnableBigTitle = Config.Bind(
            "UI",
            "EnableBigTitle",
            true,
            "Always shows the area title card in big size"
        );
        Harmony harmony = new Harmony("silksong.arbaz9234.alwaysbigareatitles");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.LogInfo("Always Big Area Titles loaded");
    }
}


[HarmonyPatch(typeof(AreaTitleController), "Finish")]
public static class AreaTitleController_Finish_Patch
{
    static bool Prefix(AreaTitleController __instance)
    {
        if (!AlwaysBigAreaTitles.EnableBigTitle.Value)
            return true;
        var currentAreaDataField =
            AccessTools.Field(typeof(AreaTitleController), "currentAreaData");

        object currentAreaData = currentAreaDataField.GetValue(__instance);
        if (currentAreaData == null)
            return true; // allow vanilla

        string identifier =
            (string)currentAreaData.GetType().GetField("Identifier").GetValue(currentAreaData);

        string visitedBool =
            (string)currentAreaData.GetType().GetField("VisitedBool").GetValue(currentAreaData);

        if (string.IsNullOrEmpty(identifier))
            return true; // allow vanilla

        GameManager gm = GameManager.instance;
        PlayerData playerData = gm.playerData;

        // 🔑 1. If area has NEVER been visited, let vanilla decide WHEN to show title
        if (!string.IsNullOrEmpty(visitedBool) && !playerData.GetBool(visitedBool))
        {
            return true; // DO NOT interfere
        }

        // 🔑 2. If same area, this is a room change → skip
        if (playerData.currentArea == identifier)
        {
            __instance.gameObject.SetActive(false);
            return false;
        }

        // 🔑 3. Area was visited before AND area actually changed → force BIG title
        playerData.currentArea = identifier;

        __instance.StartCoroutine(ForceBigTitle(__instance, currentAreaData));

        return false; // skip vanilla
    }

    private static IEnumerator ForceBigTitle(
    AreaTitleController controller,
    object currentAreaData
)
    {
        yield return new WaitForSeconds(2f);

        GameObject areaTitle =
            (GameObject)AccessTools.Field(
                typeof(AreaTitleController),
                "areaTitle"
            ).GetValue(controller);

        if (!areaTitle)
            yield break;

        InteractManager.IsDisabled = true;

        // Reset & let FSM control lifecycle
        areaTitle.SetActive(false);
        areaTitle.SetActive(true);

        PlayMakerFSM fsm = FSMUtility.GetFSM(areaTitle);
        if (fsm)
        {
            string identifier =
                (string)currentAreaData.GetType().GetField("Identifier").GetValue(currentAreaData);

            bool displayRight =
                (bool)AccessTools.Field(
                    typeof(AreaTitleController),
                    "displayRight"
                ).GetValue(controller);

            FSMUtility.SetBool(fsm, "Visited", false); // BIG title
            FSMUtility.SetBool(fsm, "NPC Title", false);
            FSMUtility.SetBool(fsm, "City Title", IsCityTitle());
            FSMUtility.SetBool(fsm, "Display Right", displayRight);
            FSMUtility.SetString(fsm, "Area Event", identifier);
        }

        // 🔑 WAIT for FSM to finish fading naturally
        float timeout = 5f;
        while (areaTitle.activeInHierarchy && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Do NOT disable areaTitle here
        InteractManager.IsDisabled = false;
    }


    private static bool IsCityTitle()
    {
        MapZone zone = GameManager.instance.GetCurrentMapZoneEnum();
        return zone == MapZone.CITY_OF_SONG
            || (uint)(zone - 22) <= 1
            || (uint)(zone - 26) <= 1;
    }
}
