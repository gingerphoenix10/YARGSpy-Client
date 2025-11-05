using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using YARG.Menu.Navigation;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Types;

namespace YARGSpy.Settings.CustomTypes;

public class PasswordSetting : AbstractSetting<string>
{
    public override string AddressableName => "Setting/String";


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
