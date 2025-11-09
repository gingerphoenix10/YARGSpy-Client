/*
 * [Patches]
 * 
 * UpdateVisual - Prefix
 * - Make sure that YARGSpy input viewer buttons use custom transparency
 * 
 */

using Cysharp.Text;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using YARG.Gameplay;
using YARG.Gameplay.HUD;

namespace YARGSpy.InputViewer.Patches;

[HarmonyPatch(typeof(InputViewerButton))]
internal static class InputViewerButtonPatch
{
    [HarmonyPatch(nameof(InputViewerButton.UpdatePressState))]
    [HarmonyPrefix]
    internal static bool UpdatePressStatePrefix(InputViewerButton __instance, bool pressed, double time)
    {
        if (!(__instance is SpyInputViewerButton button))
            return true;
        time += 2.0;
        button._holdStartTime = button._gameManager.InputTime + 2.0;
        button._isPressed = pressed;
        if (pressed)
        {
            button._inputTime = time;
            button._pressCount++;
        }

        __instance.UpdateVisual();
        return false;
    }


    [HarmonyPatch(nameof(InputViewerButton.UpdateVisual))]
    [HarmonyPrefix]
    internal static bool UpdateVisualPrefix(InputViewerButton __instance)
    {
        if (!(__instance is SpyInputViewerButton button))
            return true;

        button._inputTimeText.SetText(button._inputTime.ToString("F3"));
        button._pressCountText.SetText(button._pressCount);
        Color buttonColor = new Color(0.1f, 0.1f, 0.1f);
        buttonColor.a = (button._isPressed ? 0.75f : 1f);
        button._imageHighlight.color = buttonColor;
        button._coverImage.color = button.ButtonColor;
        return false;
    }

    [HarmonyPatch(nameof(InputViewerButton.Update))]
    [HarmonyPrefix]
    internal static bool UpdatePrefix(InputViewerButton __instance)
    {
        if (!(__instance is SpyInputViewerButton button))
            return true;
        if (button._isPressed)
        {
            button._holdTime = button._gameManager.InputTime - button._holdStartTime + 2.0;
            button._holdTimeText.SetText(button._holdTime.ToString("F3"));
        }
        return false;
    }
}