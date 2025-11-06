/*
 *  [Patches]
 *  
 *  LoadSettings - Postfix
 *  - When loading regular settings, also load SpySettings
 *  
 *  SaveSettings - Postfix
 *  - When saving regular settings, also save SpySettings
 *  
 *  GetSettingByName - Prefix
 *  - When attempting to find a setting by name, first search SpySettings before
 *    the regular settings search
 *  
 *  InvokeButton - Prefix
 *  - When invoking a method by name (for buttons), first search if the method
 *    is in SpySettings before regular settings
 *  
 */

using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Events;
using YARG.Core.Logging;
using YARG.Helpers;
using YARG.Settings;
using YARG.Settings.Types;
using YARGSpy.Settings.CustomTypes;

namespace YARGSpy.Settings.Patches;

[HarmonyPatch(typeof(SettingsManager))]
internal static class SettingsManagerPatch
{
    public static UnityEvent ready;
    private static string SettingsFile => Path.Combine(PathHelper.PersistentDataPath, "yargspy_settings.json");

    [HarmonyPatch(nameof(SettingsManager.LoadSettings))]
    [HarmonyPostfix]
    internal static void LoadSettingsPostfix()
    {
        // Create settings container
        try
        {
            string text = File.ReadAllText(SettingsFile);
            SpySettings.instance = JsonConvert.DeserializeObject<SpySettings>(text, SettingsManager.JsonSettings);
        }
        catch (Exception e)
        {
            YargLogger.LogException(e, "[Spy] Failed to load YARGSpy settings!");
        }

        // If null, recreate
        SpySettings.instance ??= new SpySettings();

        // Now that we're done loading, call all of the callbacks
        var fields = typeof(SpySettings).GetProperties();
        foreach (var field in fields)
        {
            var value = field.GetValue(SpySettings.instance);

            if (value is not ISettingType settingType)
            {
                continue;
            }

            settingType.ForceInvokeCallback();
        }
        if (ready == null)
            ready = new UnityEvent();
        ready.Invoke();
    }

    [HarmonyPatch(nameof(SettingsManager.SaveSettings))]
    [HarmonyPostfix]
    internal static void SaveSettingsPostfix()
    {
        if (SpySettings.instance is not null)
        {
            PasswordSettingVisual.value = "";
            var json = JsonConvert.SerializeObject(SpySettings.instance, SettingsManager.JsonSettings);
            File.WriteAllText(SettingsFile, json);
        }
    }

    [HarmonyPatch(nameof(SettingsManager.GetSettingByName))]
    [HarmonyPrefix]
    internal static bool GetSettingByNamePrefix(string name, ref ISettingType __result)
    {
        var field = typeof(SpySettings).GetProperty(name);
        if (field == null)
            return true;

        var value = field.GetValue(SpySettings.instance);

        if (value == null)
        {
            YargLogger.LogFormatWarning("[Spy] `{0}` has a value of null. This might create errors.", name);
        }

        __result = (ISettingType)value;
        return false;
    }

    [HarmonyPatch(nameof(SettingsManager.InvokeButton))]
    [HarmonyPrefix]
    internal static bool InvokeButtonPrefix(string name)
    {
        var method = typeof(SpySettings).GetMethod(name);

        if (method == null)
            return true;

        method.Invoke(SpySettings.instance, null);
        return false;
    }
}