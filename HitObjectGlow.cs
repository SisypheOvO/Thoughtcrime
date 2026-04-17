using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewScripts
{
    /// <summary>
    /// Generate glow particle effects at each hit object position
    /// </summary>
    public class HitObjectGlow : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Glow Settings")]
        [Configurable] public string GlowPath = "sb/glow.png";
        [Configurable] public double StartScale = 0.6;
        [Configurable] public double EndScale = 0.16;
        [Configurable] public int FadeDuration = 800;
        [Configurable] public double StartOpacity = 0.7;
        [Description("Color intensity multiplier (0~1). Lower values reduce the chance of turning white")]
        [Configurable] public double ColorIntensity = 0.4;

        [Group("Bookmark Segments")]
        [Description("Control glow by bookmark segments using a 0/1 string, e.g. 1010101. 1=enable, 0=disable.")]
        [Configurable] public string SegmentMask = "01000100000011";

        public override void Generate()
        {
            if (StartTime == EndTime)
                EndTime = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);

            var songEnd = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);
            var mask = new string([.. (SegmentMask ?? string.Empty).Where(c => c == '0' || c == '1')]);
            var bookmarks = Beatmap.Bookmarks.OrderBy(b => b).ToArray();
            var enabledRanges = new List<Tuple<int, int>>();

            if (bookmarks.Length == 0 || mask.Length == 0)
            {
                Log("No bookmarks available or SegmentMask is empty; HitObjectGlow applies for the full duration.");
                enabledRanges.Add(Tuple.Create(StartTime, EndTime));
            }
            else
            {
                var count = System.Math.Min(mask.Length, bookmarks.Length);
                for (int i = 0; i < count; i++)
                {
                    if (mask[i] != '1') continue;

                    var runStartIndex = i;
                    while (i + 1 < count && mask[i + 1] == '1') i++;
                    var runEndIndex = i;

                    var segmentStart = (int)bookmarks[runStartIndex];
                    var segmentEnd = (runEndIndex + 1 < bookmarks.Length) ? (int)bookmarks[runEndIndex + 1] : songEnd;

                    var start = System.Math.Max(segmentStart, StartTime);
                    var end = System.Math.Min(segmentEnd, EndTime);
                    if (end <= start) continue;

                    enabledRanges.Add(Tuple.Create(start, end));
                }
            }

            var colors = new[]
            {
                new Color4(9, 109, 124, 255),
                new Color4(170, 117, 110, 255),
                new Color4(36, 204, 204, 255),
                new Color4(41, 165, 199, 255),
            };

            // Lower color intensity to avoid turning white after additive blending
            float ci = (float)Math.Max(0, Math.Min(1, ColorIntensity));

            int colorIndex = 0;

            foreach (var hitObject in Beatmap.HitObjects)
            {
                if (hitObject.StartTime < StartTime || hitObject.StartTime > EndTime)
                    continue;

                var inEnabledRange = enabledRanges.Any(r => hitObject.StartTime >= r.Item1 && hitObject.StartTime < r.Item2);
                if (!inEnabledRange)
                    continue;

                var c = colors[colorIndex % colors.Length];
                var color = new Color4((byte)(c.R * 255 * ci), (byte)(c.G * 255 * ci), (byte)(c.B * 255 * ci), 255);
                colorIndex++;

                var layer = GetLayer("HitGlow");

                if (hitObject is OsuSlider slider)
                {
                    // Slider: move along the slider path
                    var startTime = (int)slider.StartTime;
                    var endTime = (int)slider.EndTime;
                    var duration = endTime - startTime;

                    var sprite = layer.CreateSprite(GlowPath, OsbOrigin.Centre);
                    sprite.Color(startTime, color);
                    sprite.Fade(OsbEasing.Out, startTime, startTime + FadeDuration + duration, StartOpacity, 0);
                    sprite.Scale(OsbEasing.Out, startTime, startTime + FadeDuration + duration, StartScale, EndScale);
                    sprite.Additive(startTime, startTime + FadeDuration + duration);

                    // Move along the slider path
                    var stepCount = Math.Max(2, duration / 50);
                    for (int i = 0; i < stepCount; i++)
                    {
                        var t0 = (double)i / stepCount;
                        var t1 = (double)(i + 1) / stepCount;
                        var pos0 = slider.PositionAtTime(startTime + duration * t0);
                        var pos1 = slider.PositionAtTime(startTime + duration * t1);
                        var time0 = startTime + (int)(duration * t0);
                        var time1 = startTime + (int)(duration * t1);
                        sprite.Move(time0, time1, pos0.X, pos0.Y, pos1.X, pos1.Y);
                    }
                }
                else
                {
                    // Circle/Spinner: static glow sprite
                    var startTime = (int)hitObject.StartTime;
                    var pos = hitObject.Position;

                    var sprite = layer.CreateSprite(GlowPath, OsbOrigin.Centre, new Vector2(pos.X, pos.Y));
                    sprite.Color(startTime, color);
                    sprite.Fade(OsbEasing.Out, startTime, startTime + FadeDuration, StartOpacity, 0);
                    sprite.Scale(OsbEasing.Out, startTime, startTime + FadeDuration, StartScale, EndScale);
                    sprite.Additive(startTime, startTime + FadeDuration);
                }
            }
        }
    }
}
