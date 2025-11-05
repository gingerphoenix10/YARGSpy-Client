
// Code borrowed from YARGSpy Replay Validator

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YARG.Core;
using YARG.Core.Engine;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Vocals;
using YARG.Core.Game;
using YARG.Core.Replays;

namespace YARGSpy.Helpers;

public class ValidityHelper
{
    public static bool ValidEngine(ReplayFrame frame, ReplayInfo replayInfo)
    {
        var starMultiplierThreshold = frame.EngineParameters.StarMultiplierThresholds;
        BaseEngineParameters defaultEngine = EnginePreset.Default.ProKeys.Create(starMultiplierThreshold);
        BaseEngineParameters casualEngine = EnginePreset.Casual.ProKeys.Create(starMultiplierThreshold);
        BaseEngineParameters precisionEngine = EnginePreset.Precision.ProKeys.Create(starMultiplierThreshold);
        if (frame.EngineParameters is GuitarEngineParameters)
        {
            Instrument[] bass = [Instrument.FiveFretBass, Instrument.SixFretBass, Instrument.ProBass_17Fret, Instrument.ProBass_22Fret];
            bool isBass = bass.Contains(frame.Profile.CurrentInstrument);
            defaultEngine = EnginePreset.Default.FiveFretGuitar.Create(starMultiplierThreshold, isBass);
            casualEngine = EnginePreset.Casual.FiveFretGuitar.Create(starMultiplierThreshold, isBass);
            precisionEngine = EnginePreset.Precision.FiveFretGuitar.Create(starMultiplierThreshold, isBass);
        }
        else if (frame.EngineParameters is DrumsEngineParameters drumsEngineParameters)
        {
            defaultEngine = EnginePreset.Default.Drums.Create(starMultiplierThreshold, drumsEngineParameters.Mode);
            casualEngine = EnginePreset.Casual.Drums.Create(starMultiplierThreshold, drumsEngineParameters.Mode);
            precisionEngine = EnginePreset.Precision.Drums.Create(starMultiplierThreshold, drumsEngineParameters.Mode);
        }
        else if (frame.EngineParameters is VocalsEngineParameters vocalsEngineParameters)
        {
            defaultEngine = EnginePreset.Default.Vocals.Create(starMultiplierThreshold, frame.Profile.CurrentDifficulty, (float)vocalsEngineParameters.ApproximateVocalFps, vocalsEngineParameters.SingToActivateStarPower);
            casualEngine = EnginePreset.Casual.Vocals.Create(starMultiplierThreshold, frame.Profile.CurrentDifficulty, (float)vocalsEngineParameters.ApproximateVocalFps, vocalsEngineParameters.SingToActivateStarPower);
            precisionEngine = EnginePreset.Precision.Vocals.Create(starMultiplierThreshold, frame.Profile.CurrentDifficulty, (float)vocalsEngineParameters.ApproximateVocalFps, vocalsEngineParameters.SingToActivateStarPower);
        }
        defaultEngine.SongSpeed = replayInfo.SongSpeed;
        casualEngine.SongSpeed = replayInfo.SongSpeed;
        precisionEngine.SongSpeed = replayInfo.SongSpeed;
        defaultEngine.HitWindow.Scale = replayInfo.SongSpeed;
        casualEngine.HitWindow.Scale = replayInfo.SongSpeed;
        precisionEngine.HitWindow.Scale = replayInfo.SongSpeed;
        if (EngineEqualityCheck(frame.EngineParameters, defaultEngine))
            return true;
        else if (EngineEqualityCheck(frame.EngineParameters, casualEngine))
            return true;
        else if (EngineEqualityCheck(frame.EngineParameters, precisionEngine))
            return true;
        return false;
    }

    public static bool EngineEqualityCheck(BaseEngineParameters a, BaseEngineParameters b)
    {
        return JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b); // quite a bodge but screw it
    }
}
