using YARG.Core.Game;
using YARG.Core.Input;
using YARG.Gameplay.HUD;
using YARG.Helpers.Extensions;

namespace YARGSpy.InputViewer;

public class SpyProDrumsInputViewer : BaseInputViewer
{
    public override void OnInput(GameInput input)
    {
        int num;
        switch ((DrumsAction)(byte)input.Action)
        {
            case DrumsAction.YellowCymbal:
            case DrumsAction.BlueCymbal:
            case DrumsAction.GreenCymbal:
                num = input.Action - 4;
                break;

            case DrumsAction.RedDrum:
            case DrumsAction.YellowDrum:
            case DrumsAction.BlueDrum:
            case DrumsAction.GreenDrum:
                num = input.Action+3;
                break;
            case DrumsAction.Kick:
                num = input.Action;
                break;
            default:
                return;
        }

        _buttons[num].UpdatePressState(input.Axis > 0, input.Time);
    }

    public override void SetColors(ColorProfile colorProfile)
    {
        _buttons[0].ButtonColor = colorProfile.FourLaneDrums.YellowCymbal.ToUnityColor();
        _buttons[1].ButtonColor = colorProfile.FourLaneDrums.BlueCymbal.ToUnityColor();
        _buttons[2].ButtonColor = colorProfile.FourLaneDrums.GreenCymbal.ToUnityColor();
        _buttons[3].ButtonColor = colorProfile.FourLaneDrums.RedFret.ToUnityColor();
        _buttons[4].ButtonColor = colorProfile.FourLaneDrums.YellowFret.ToUnityColor();
        _buttons[5].ButtonColor = colorProfile.FourLaneDrums.BlueFret.ToUnityColor();
        _buttons[6].ButtonColor = colorProfile.FourLaneDrums.GreenFret.ToUnityColor();

        _buttons[7].ButtonColor = colorProfile.FourLaneDrums.KickFret.ToUnityColor();
    }
}