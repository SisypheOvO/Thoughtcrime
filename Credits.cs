using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System.Linq;

namespace StorybrewScripts
{
    public class Credits : StoryboardObjectGenerator
    {
        [Group("Settings")]
        [Description("Folder path containing images, e.g. sb/fonts/")]
        [Configurable] public string Folder = "sb/fonts/";
        [Configurable] public double Scale = 0.2;
        [Configurable] public double OffsetX = 85.6;
        [Configurable] public double YVisible = 400;
        [Configurable] public double YHidden = 420;
        [Configurable] public double MaxOpacity = 0.9;
        [Configurable] public int FadeDuration = 300;
        [Configurable] public int SlideDuration = 600;

        [Group("Names (one file name per bookmark segment)")]
        [Description("Enter file names only (e.g. MapperA.png); timing is assigned by bookmark segments automatically")]
        [Configurable] public string Name0 = "";
        [Configurable] public string Name1 = "";
        [Configurable] public string Name2 = "";
        [Configurable] public string Name3 = "";
        [Configurable] public string Name4 = "";
        [Configurable] public string Name5 = "";
        [Configurable] public string Name6 = "";
        [Configurable] public string Name7 = "";
        [Configurable] public string Name8 = "";
        [Configurable] public string Name9 = "";
        [Configurable] public string Name10 = "";
        [Configurable] public string Name11 = "";
        [Configurable] public string Name12 = "";
        [Configurable] public string Name13 = "";
        [Configurable] public string Name14 = "";
        [Configurable] public string Name15 = "";

        public override void Generate()
        {
            var names = new[] { Name0, Name1, Name2, Name3, Name4, Name5, Name6, Name7, Name8, Name9, Name10, Name11, Name12, Name13, Name14, Name15 }
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToArray();

            if (names.Length == 0) return;

            // Read bookmarks as segment boundaries
            var bookmarks = Beatmap.Bookmarks.OrderBy(b => b).ToArray();
            if (bookmarks.Length == 0)
            {
                Log("No bookmarks were found. Please set bookmarks in the osu editor as segment points.");
                return;
            }

            // Segment count = min(name count, bookmark count)
            // Each bookmark is the start of a segment; the next bookmark is its end
            var songEnd = (int)(Beatmap.HitObjects.LastOrDefault()?.EndTime ?? AudioDuration);
            var count = System.Math.Min(names.Length, bookmarks.Length);

            for (int i = 0; i < count; i++)
            {
                var startTime = (int)bookmarks[i];
                var endTime = (i + 1 < bookmarks.Length) ? (int)bookmarks[i + 1] : songEnd;
                var path = Folder + names[i] + ".png";

                var layer = GetLayer("Credits");
                var sprite = layer.CreateSprite(path, OsbOrigin.BottomLeft);

                sprite.Scale(startTime, Scale);
                sprite.MoveX(startTime, OffsetX);

                // Fade in + slide in
                sprite.Fade(OsbEasing.OutCubic, startTime, startTime + FadeDuration, 0, MaxOpacity);
                sprite.MoveY(OsbEasing.OutQuad, startTime, startTime + SlideDuration, YHidden, YVisible);

                // Fade out + slide out
                sprite.MoveY(OsbEasing.InQuad, endTime - SlideDuration, endTime, YVisible, YHidden);
                sprite.Fade(OsbEasing.InQuart, endTime - FadeDuration, endTime, MaxOpacity, 0);
            }
        }
    }
}
