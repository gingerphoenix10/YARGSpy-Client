using YARG.Core.Game;
using YARG.Core.Input;
using YARG.Gameplay.HUD;
using YARG.Helpers.Extensions;

namespace YARGSpy.InputViewer;

public class SpyFiveFretInputViewer : BaseInputViewer
{
    public override void OnInput(GameInput input)
    {
        int num;
        switch ((GuitarAction)(byte)input.Action)
        {
            default:
                return;
            case GuitarAction.Fret1:
            case GuitarAction.Fret2:
            case GuitarAction.Fret3:
            case GuitarAction.Fret4:
            case GuitarAction.Fret5:
                num = input.Action;
                break;
            case GuitarAction.StrumUp:
            case GuitarAction.StrumDown:
                num = input.Action - 1;
                break;
        }

        _buttons[num].UpdatePressState(input.Button, input.Time);
    }

    public override void SetColors(ColorProfile colorProfile)
    {
        _buttons[0].ButtonColor = ColorProfile.Default.FiveFretGuitar.GreenNote.ToUnityColor();
        _buttons[1].ButtonColor = ColorProfile.Default.FiveFretGuitar.RedNote.ToUnityColor();
        _buttons[2].ButtonColor = ColorProfile.Default.FiveFretGuitar.YellowNote.ToUnityColor();
        _buttons[3].ButtonColor = ColorProfile.Default.FiveFretGuitar.BlueNote.ToUnityColor();
        _buttons[4].ButtonColor = ColorProfile.Default.FiveFretGuitar.OrangeNote.ToUnityColor();
        _buttons[5].ButtonColor = ColorProfile.Default.FiveFretGuitar.OpenNote.ToUnityColor();
        _buttons[6].ButtonColor = ColorProfile.Default.FiveFretGuitar.OpenNote.ToUnityColor();
    }
}