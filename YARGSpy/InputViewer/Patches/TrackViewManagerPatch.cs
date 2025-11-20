using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Gameplay.HUD;
using YARG.Gameplay.Player;
using YARG.Helpers.Extensions;
using YARG.Player;
using YARGSpy.InputViewer;

namespace YARGSpy.InputViewer.Patches;

[HarmonyPatch(typeof(TrackViewManager))]
internal static class TrackViewManagerPatch
{
    private static readonly float colorIndent = 4f;
    private static GameObject buttonBase;
    private static GameObject viewerBase;
    [HarmonyPatch(nameof(TrackViewManager.CreateTrackView))]
    [HarmonyPostfix]
    internal static void CreateTrackView(TrackPlayer trackPlayer, YargPlayer player, ref TrackView __result)
    {
        if (buttonBase == null)
            buttonBase = Plugin.bundle.LoadAsset<GameObject>("SpyInputViewerButton");
        if (viewerBase == null)
            viewerBase = Plugin.bundle.LoadAsset<GameObject>("SpyInputViewer");
        Transform parent = __result.transform.Find("Track Image").Find("Scale Container").Find("Top Elements");
        GameObject viewerObj;
        switch (player.Profile.CurrentInstrument)
        {
            case YARG.Core.Instrument.FiveFretGuitar:
            case YARG.Core.Instrument.FiveFretBass:
            case YARG.Core.Instrument.FiveFretCoopGuitar:
            case YARG.Core.Instrument.FiveFretRhythm:
                viewerObj = GameObject.Instantiate(viewerBase, parent);
                viewerObj.transform.localPosition += new Vector3(0, 100, 0);
                viewerObj.transform.localScale -= new Vector3(0.15f,0.15f,0.15f);
                SpyFiveFretInputViewer FiveFretViewer = viewerObj.AddComponent<SpyFiveFretInputViewer>();
                Transform mainRow = viewerObj.transform.Find("Row");

                mainRow.DestroyChildren();

                FiveFretViewer._buttons = new InputViewerButton[7];

                CreateButtons(FiveFretViewer, mainRow, 7);

                trackPlayer.InputViewer = FiveFretViewer;
                break;
            case YARG.Core.Instrument.FourLaneDrums:
                viewerObj = GameObject.Instantiate(viewerBase, parent);
                viewerObj.transform.localPosition += new Vector3(0, 100, 0);
                viewerObj.transform.localScale -= new Vector3(0.15f, 0.15f, 0.15f);
                SpyFourLaneInputViewer FourLaneViewer = viewerObj.AddComponent<SpyFourLaneInputViewer>();
                mainRow = viewerObj.transform.Find("Row");

                mainRow.DestroyChildren();

                FourLaneViewer._buttons = new InputViewerButton[5];

                CreateButtons(FourLaneViewer, mainRow, 5);

                trackPlayer.InputViewer = FourLaneViewer;
                break;
            case YARG.Core.Instrument.ProDrums:
                viewerObj = GameObject.Instantiate(viewerBase, parent);
                viewerObj.transform.localPosition += new Vector3(0, 100, 0);
                viewerObj.transform.localScale -= new Vector3(0.15f, 0.15f, 0.15f);
                SpyProDrumsInputViewer ProDrumsViewer = viewerObj.AddComponent<SpyProDrumsInputViewer>();
                mainRow = viewerObj.transform.Find("Row");

                mainRow.DestroyChildren();

                Transform bottomRow = GameObject.Instantiate(mainRow, mainRow.parent);
                bottomRow.name = "Bottom row";
                bottomRow.DestroyChildren();

                ProDrumsViewer._buttons = new InputViewerButton[8];

                new GameObject().AddComponent<RectTransform>().transform.parent = mainRow;
                CreateButtons(ProDrumsViewer, mainRow, 3);
                new GameObject().AddComponent<RectTransform>().transform.parent = mainRow;

                CreateButtons(ProDrumsViewer, bottomRow, 5, 3);

                trackPlayer.InputViewer = ProDrumsViewer;
                break;
            default:
                return;
        }
    }

    private static void CreateButtons(BaseInputViewer viewer, Transform row, int buttons, int start = 0)
    {
        for (int i = 0; i < buttons; i++)
        {
            GameObject newButton = GameObject.Instantiate(buttonBase, row);
            Transform imagesRoot = newButton.transform.Find("Images");

            RectTransform coverTransform = imagesRoot.Find("Color Image").GetComponent<RectTransform>();
            Vector2 size = coverTransform.sizeDelta;
            size.y -= colorIndent;
            coverTransform.sizeDelta = size;
            if (((viewer is SpyFiveFretInputViewer) || start != 0) && i == 6)
                coverTransform.localPosition += new Vector3(0, colorIndent, 0);

            SpyInputViewerButton buttonInstance = newButton.AddComponent<SpyInputViewerButton>();
            buttonInstance._inputTimeText = newButton.transform.Find("PressTime").GetComponent<TextMeshProUGUI>();
            buttonInstance._holdTimeText = imagesRoot.transform.Find("HoldTime").GetComponent<TextMeshProUGUI>();
            buttonInstance._pressCountText = imagesRoot.transform.Find("Press Count").GetComponent<TextMeshProUGUI>();
            buttonInstance._imageHighlight = imagesRoot.transform.Find("Color Image").GetComponent<Image>();
            buttonInstance._coverImage = imagesRoot.transform.Find("Backing Image").GetComponent<Image>();
            viewer._buttons[i+start] = buttonInstance;
            Plugin.Logger.LogInfo(i+start);
        }
    }
}