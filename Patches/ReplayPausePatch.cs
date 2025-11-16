using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using YARG.Gameplay.HUD;
using YARG.Player;

namespace YARGSpy.Patches;

[HarmonyPatch(typeof(ReplayPause))]
internal static class ReplayPausePatch
{
    [HarmonyPatch(nameof(ReplayPause.SaveColorProfile))]
    [HarmonyPostfix]
    internal static void SaveColorPostfix(ReplayPause __instance)
    {
        PlayerContainer.AddProfile(__instance._thisPlayer.Player.Profile);
    }
}