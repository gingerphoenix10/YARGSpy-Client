using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YARG.Menu.Navigation;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Types;

namespace YARGSpy.Settings.CustomTypes;

public class StringSetting : AbstractCustomSetting<string>
{
    public override BaseSettingVisual CreateSettingObject(Transform container)
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

        return visual;
    }

    public StringSetting(string value, Action<string> onChange = null)
        : base(onChange)
    {
        _value = value;
    }

    public override void SetValue(string value)
    {
        _value = value;
    }

    public override bool ValueEquals(string value)
    {
        return value == base.Value;
    }
}

public class StringSettingVisual : BaseSettingVisual<StringSetting>
{
    public TMP_InputField _inputField;

    public override void RefreshVisual()
    {
        _inputField.text = Setting.Value;
    }

    public override NavigationScheme GetNavigationScheme()
    {
        return new NavigationScheme(new()
            {
                NavigateFinish
            }, true);
    }

    public void OnTextFieldChange()
    {
        try
        {
            Setting.Value = _inputField.text;
        }
        catch
        {
            // Ignore error
        }

        RefreshVisual();
    }
}