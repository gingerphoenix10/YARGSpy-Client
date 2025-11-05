using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
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
    // Replace the addressable prefab instantiation with whatever we want
    [HarmonyPatch(nameof(Tab.SpawnSettingVisual))]
    [HarmonyPrefix]
    internal static bool SpawnSettingVisualPrefix(ISettingType setting, Transform container, ref BaseSettingVisual __result)
    {
        if (setting is StringSetting)
        {
            var settingPrefab = Addressables.LoadAssetAsync<GameObject>("Setting/Int")
                .WaitForCompletion();
            var go = GameObject.Instantiate(settingPrefab, container);

            // Set the setting, and cache the object
            IntSettingVisual original = go.GetComponent<IntSettingVisual>();
            StringSettingVisual visual = go.AddComponent<StringSettingVisual>();

            // Idk if these are actually set right now, but I'll sync them anyways
            visual._inputField = original._inputField;
            visual._settingLabel = original._settingLabel;
            visual.IsPresetSetting = original.IsPresetSetting;
            visual.HasDescription = original.HasDescription;
            visual.UnlocalizedName = original.UnlocalizedName;

            visual._inputField.contentType = TMP_InputField.ContentType.Standard;
            ((TextMeshProUGUI)visual._inputField.placeholder).text = "Enter text...";

            visual._inputField.onValueChanged.AddListener((_) => visual.OnTextFieldChange());

            GameObject.Destroy(original);

            __result = visual;
            return false;
        }
        if (setting is PasswordSetting)
        {
            var settingPrefab = Addressables.LoadAssetAsync<GameObject>("Setting/Int")
                .WaitForCompletion();
            var go = GameObject.Instantiate(settingPrefab, container);

            // Set the setting, and cache the object
            IntSettingVisual original = go.GetComponent<IntSettingVisual>();
            PasswordSettingVisual visual = go.AddComponent<PasswordSettingVisual>();

            // Idk if these are actually set right now, but I'll sync them anyways
            visual._inputField = original._inputField;
            visual._settingLabel = original._settingLabel;
            visual.IsPresetSetting = original.IsPresetSetting;
            visual.HasDescription = original.HasDescription;
            visual.UnlocalizedName = original.UnlocalizedName;

            visual._inputField.contentType = TMP_InputField.ContentType.Password;
            ((TextMeshProUGUI)visual._inputField.placeholder).text = "Enter password...";

            visual._inputField.onValueChanged.AddListener((_) => visual.OnTextFieldChange());

            GameObject.Destroy(original);

            __result = visual;
            return false;
        }
        return true;
    }
}