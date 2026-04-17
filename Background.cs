using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System.Linq;

namespace StorybrewScripts
{
    public class Background : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Sprite")]
        [Description("Leave empty to automatically use the map's background.")]
        [Configurable] public string SpritePath = "";

        [Description("Opacity when SegmentMask=1 (dim).")]
        [Configurable] public double Opacity = 0.9;

        [Group("Bookmark Segments")]
        [Description("Control background opacity by bookmark segments using a 0/1 string, e.g. 1010101. 1=dim, 0=normal.")]
        [Configurable] public string SegmentMask = "10110011011100";
        [Configurable] public double OpacityWhen0 = 1;
        [Configurable] public int SegmentFadeIn = 400;
        [Configurable] public int SegmentFadeOut = 75;

        public override void Generate()
        {
            if (SpritePath == "") SpritePath = Beatmap.BackgroundPath ?? string.Empty;
            if (StartTime == EndTime) EndTime = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);

            var bitmap = GetMapsetBitmap(SpritePath);
            var bg = GetLayer("").CreateSprite(SpritePath, OsbOrigin.Centre);
            bg.Scale(StartTime, 480.0f / bitmap.Height);

            var mask = new string([.. (SegmentMask ?? string.Empty).Where(c => c == '0' || c == '1')]);
            var bookmarks = Beatmap.Bookmarks.OrderBy(b => b).ToArray();

            // Base opacity track (Start/End fades still apply)
            if (bookmarks.Length == 0 || mask.Length == 0)
            {
                bg.Fade(StartTime - 500, StartTime, 0, Opacity);
                bg.Fade(EndTime, EndTime + 500, Opacity, 0);
                return;
            }

            var count = System.Math.Min(mask.Length, bookmarks.Length);

            // Map: 0 -> normal, 1 -> dim
            var opacityByBit = new[] { OpacityWhen0, Opacity };

            // Treat pre-first-bookmark as first segment and post-last-bookmark as last segment.
            var startOpacity = opacityByBit[mask[0] - '0'];
            var endOpacity = opacityByBit[mask[count - 1] - '0'];
            var trackEndOpacity = endOpacity;

            bg.Fade(StartTime - 500, StartTime, 0, startOpacity);

            var fadeIn = System.Math.Max(0, SegmentFadeIn);
            var fadeOut = System.Math.Max(0, SegmentFadeOut);

            // Apply transitions at each bookmark boundary (fade completes exactly at the boundary, mirroring Overlay behavior)
            for (int i = 0; i + 1 < count; i++)
            {
                var currentVal = mask[i];
                var nextVal = mask[i + 1];
                if (currentVal == nextVal) continue;

                var fromOpacity = opacityByBit[currentVal - '0'];
                var toOpacity = opacityByBit[nextVal - '0'];
                var boundary = (int)bookmarks[i + 1];
                var duration = (nextVal == '1') ? fadeIn : fadeOut;
                if (duration <= 0) bg.Fade(boundary, toOpacity);
                else bg.Fade(boundary - duration, boundary, fromOpacity, toOpacity);
            }

            // If mask ended before bookmarks, treat the next segment as '0' (normal) like Overlay does (no more dimming).
            if (count < bookmarks.Length)
            {
                var currentVal = mask[count - 1];
                var nextVal = '0';
                if (currentVal != nextVal)
                {
                    var boundary = (int)bookmarks[count];
                    var duration = fadeOut;
                    if (duration <= 0) bg.Fade(boundary, OpacityWhen0);
                    else bg.Fade(boundary - duration, boundary, Opacity, OpacityWhen0);
                }
            }

            // Force black at the last bookmark with a fixed 50ms fade.
            const int finalBlackDuration = 50;
            var lastBookmark = (int)bookmarks[^1];
            if (lastBookmark >= StartTime && lastBookmark <= EndTime)
            {
                var blackStart = System.Math.Max(StartTime, lastBookmark - finalBlackDuration);
                double preLastBookmarkOpacity;

                if (count == bookmarks.Length && count >= 2)
                    preLastBookmarkOpacity = opacityByBit[mask[count - 2] - '0'];
                else if (count == bookmarks.Length)
                    preLastBookmarkOpacity = opacityByBit[mask[0] - '0'];
                else preLastBookmarkOpacity = OpacityWhen0;

                if (blackStart == lastBookmark) bg.Fade(lastBookmark, 0);
                else bg.Fade(blackStart, lastBookmark, preLastBookmarkOpacity, 0);

                trackEndOpacity = 0;
            }

            bg.Fade(EndTime, EndTime + 500, trackEndOpacity, 0);
        }
    }
}
