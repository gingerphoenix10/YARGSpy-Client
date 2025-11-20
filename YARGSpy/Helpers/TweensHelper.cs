using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace YARGSpy.Helpers;

public static class TweensHelper
{
    public static class EasingStyles
    {
        public static float BelowSix(float a, float b, float t)
        {
            // Custom function. Prob not made the best, all I did was just screw around in desmos till it worked lol
            float y;

            if (t < 0.6)
                y = (float)(1 + Math.Cos(5 * Math.PI * t)) / 2;
            else
                y = (float)(1 + Math.Sin((5 * Math.PI / 2) * t)) / 2;
            return a + y * (b - a);
        }
        public static float easeOutQuint(float a, float b, float t)
        {
            return (float)(a + (1 - Math.Pow(1 - t, 5)) * (b - a));
        }
    }
    public class TweenInfo : IDisposable
    {
        public float _startValue;
        public float _endValue;
        public float _length;
        public float _start;
        public Func<float, float, float, float> _lerpStyle;
        public Action<float> _update;

        public TweenInfo(float startValue, float endValue, float length, Func<float, float, float, float> lerpStyle, Action<float> update)
        {
            _startValue = startValue;
            _endValue = endValue;
            _length = length;
            _start = Time.time;
            _lerpStyle = lerpStyle;
            _update = update;
        }

        public void Dispose()
        {
            _update(_endValue);
        }
    }
    private static List<TweenInfo> activeTweens = new();
    public static void UpdateTweens()
    {
        List<TweenInfo> finished = new();
        foreach (TweenInfo tween in activeTweens)
        {
            float elapsed = Time.time - tween._start;
            if (elapsed / tween._length >= 1)
            {
                tween.Dispose();
                finished.Add(tween);
            }
            tween._update(tween._lerpStyle(tween._startValue, tween._endValue, elapsed / tween._length));
        }
        foreach (TweenInfo tween in finished)
        {
            activeTweens.Remove(tween);
        }
    }

    public static void SetTweens(List<TweenInfo> tweens)
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            activeTweens[i].Dispose();
            activeTweens.RemoveAt(i);
        }
        activeTweens = tweens;
    }
}