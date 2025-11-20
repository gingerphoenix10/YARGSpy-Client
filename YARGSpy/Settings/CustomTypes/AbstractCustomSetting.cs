using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using YARG.Menu.Settings.Visuals;
using YARG.Settings.Types;

namespace YARGSpy.Settings.CustomTypes;

public abstract class AbstractCustomSetting<T> : AbstractSetting<T>
{
    public sealed override string AddressableName => "";
    protected AbstractCustomSetting(Action<T> onChange) : base(onChange) { }
    public abstract BaseSettingVisual CreateSettingObject(Transform container);
}