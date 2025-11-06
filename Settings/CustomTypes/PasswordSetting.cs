using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YARG.Menu.Navigation;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Types;

namespace YARGSpy.Settings.CustomTypes;

public class PasswordSetting : AbstractCustomSetting<string>
{
    public override BaseSettingVisual CreateSettingObject(Transform container)
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

        return visual;
    }

    public PasswordSetting(string value, Action<string> onChange = null)
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

public class PasswordSettingVisual : BaseSettingVisual<PasswordSetting>
{
    public TMP_InputField _inputField;
    public static string value;

    public override void RefreshVisual()
    {
        _inputField.text = value;
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
            value = _inputField.text; // Don't actually save to BaseSettingVisual.Setting
        }
        catch
        {
            // Ignore error
        }

        RefreshVisual();
    }
}
