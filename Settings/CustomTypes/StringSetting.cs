using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using YARG.Menu.Navigation;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Types;

namespace YARGSpy.Settings.CustomTypes;

public class StringSetting : AbstractSetting<string>
{
    public override string AddressableName => "Setting/String";


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