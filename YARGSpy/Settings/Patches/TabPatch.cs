/*
 *  [Patches]
 *  
 *  SpawnSettingVisuual - Prefix
 *  - For custom setting types, addressable prefabs can't be created via BepInEx.
 *    Instead, if we're using a custom setting then give the custom setting control over what's
 *    being spawned. A bit messy, but works
 * 
 */

using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Metadata;
using YARG.Settings.Types;
using YARGSpy.Settings.CustomTypes;

namespace YARGSpy.Settings.Patches;

[HarmonyPatch(typeof(Tab))]
internal static class TabPatch
{
    [HarmonyPatch(nameof(Tab.SpawnSettingVisual))]
    [HarmonyPrefix]
    internal static bool SpawnSettingVisualPrefix(ISettingType setting, Transform container, ref BaseSettingVisual __result)
    {
        Type type = setting.GetType();
        string b = type.Name;
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AbstractCustomSetting<>))
            {
                var method = type.GetMethod("CreateSettingObject");
                if (method != null)
                {
                    __result = (BaseSettingVisual)method.Invoke(setting, new Transform[] { container });
                    return false;
                }
            }
            type = type.BaseType;
        }
        return true;
    }
}