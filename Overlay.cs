using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System.Linq;

namespace StorybrewScripts
{
    /// <summary>
    /// Vignette + interlude black-screen transition effect
    /// </summary>
    public class Overlay : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Vignette")]
        [Configurable] public string VignettePath = "sb/Vignette.png";
        [Configurable] public double VignetteOpacity = 1;

        [Group("Black Transition")]
        [Description("Start and end time (ms) for the interlude black-screen transition. Set to 0 to disable.")]
        [Configurable] public int BlackStart = 0;
        [Configurable] public int BlackEnd = 0;
        [Configurable] public string PixelPath = "sb/Pixel.jpg";
        [Configurable] public int BlackFadeIn = 1000;
        [Configurable] public int BlackFadeOut = 1000;

        [Group("Bookmark Segments")]
        [Description("Control vignette visibility by bookmark segments using a 0/1 string, e.g. 1010101. 1=show, 0=hide.")]
        [Configurable] public string SegmentMask = "10111011111100";
        [Configurable] public int SegmentFadeIn = 400;
        [Configurable] public int SegmentFadeOut = 75;

        public override void Generate()
        {
            if (StartTime == EndTime)
                EndTime = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);

            var vignetteLayer = GetLayer("Vignette");
            var bitmap = GetMapsetBitmap(VignettePath);
            var vignetteScale = 480.0f / bitmap.Height;

            var mask = new string([.. (SegmentMask ?? string.Empty).Where(c => c == '0' || c == '1')]);
            var bookmarks = Beatmap.Bookmarks.OrderBy(b => b).ToArray();

            if (bookmarks.Length == 0 || mask.Length == 0)
            {
                Log("No bookmarks available or SegmentMask is empty; applying vignette for the full duration.");
                var vignette = vignetteLayer.CreateSprite(VignettePath, OsbOrigin.Centre);
                vignette.Scale(StartTime, vignetteScale);
                vignette.Fade(StartTime - 500, StartTime, 0, VignetteOpacity);
                vignette.Fade(EndTime, EndTime + 500, VignetteOpacity, 0);
            }
            else
            {
                var count = System.Math.Min(mask.Length, bookmarks.Length);
                var fadeIn = System.Math.Max(0, SegmentFadeIn);
                var fadeOut = System.Math.Max(0, SegmentFadeOut);
                var songEnd = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);

                for (int i = 0; i < count; i++)
                {
                    if (mask[i] != '1') continue;

                    var runStartIndex = i;
                    while (i + 1 < count && mask[i + 1] == '1') i++; // Treat consecutive '1's as one segment, until '0' or end
                    var runEndIndex = i;

                    var segmentStart = (int)bookmarks[runStartIndex];
                    var segmentEnd = (runEndIndex + 1 < bookmarks.Length) ? (int)bookmarks[runEndIndex + 1] : songEnd;

                    var start = System.Math.Max(segmentStart, StartTime);
                    var end = System.Math.Min(segmentEnd, EndTime);
                    if (end <= start) continue;

                    var vignette = vignetteLayer.CreateSprite(VignettePath, OsbOrigin.Centre);
                    vignette.Scale(start, vignetteScale);
                    vignette.Fade(start - fadeIn, start, 0, VignetteOpacity);
                    vignette.Fade(end - fadeOut, end, VignetteOpacity, 0);
                }
            }

            // Interlude black-screen transition
            if (BlackStart > 0 && BlackEnd > 0 && BlackEnd > BlackStart)
            {
                var pixel = GetLayer("BlackTransition").CreateSprite(PixelPath, OsbOrigin.Centre);
                pixel.Color(BlackStart, 0, 0, 0);
                pixel.ScaleVec(BlackStart, 854, 480);
                pixel.Fade(OsbEasing.InQuart, BlackStart, BlackStart + BlackFadeIn, 0, 1);
                pixel.Fade(OsbEasing.InCubic, BlackEnd - BlackFadeOut, BlackEnd, 1, 0);
            }
        }
    }
}
