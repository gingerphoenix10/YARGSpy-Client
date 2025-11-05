using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using YARG.Localization;

namespace YARGSpy.Settings.Patches;

[HarmonyPatch(typeof(LocalizationManager))]
internal static class LocalizationManagerPatch
{
    // Add localization strings
    [HarmonyPatch(nameof(LocalizationManager.TryParseAndLoadLanguage))]
    [HarmonyPostfix]
    internal static void TryParseAndLoadLanguagePostfix(string cultureCode)
    {
        // Currently not localized, however I'll change that eventually

        LocalizationManager._localizationMap.Add("Settings.Tab.YARGSpy", "YARGSpy");

        LocalizationManager._localizationMap.Add("Settings.Header.Login", "Login");

        LocalizationManager._localizationMap.Add("Settings.Setting.Username.Name", "Username");
        LocalizationManager._localizationMap.Add("Settings.Setting.Password.Name", "Password");
        LocalizationManager._localizationMap.Add("Settings.Button.LoginToYARGSpy", "Login to YARGSpy");

        LocalizationManager._localizationMap.Add("Settings.Button.LogoutYARGSpy", "Logout from YARGSpy");
        LocalizationManager._localizationMap.Add("Settings.Header.Options", "Options");
        LocalizationManager._localizationMap.Add("Settings.Setting.UploadScores.Name", "Automatically Upload Scores");
        LocalizationManager._localizationMap.Add("Settings.Setting.ShowBoard.Name", "Show leaderboard in songs");
    }
}