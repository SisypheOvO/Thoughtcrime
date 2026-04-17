using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewScripts
{
    /// <summary>
    /// Full-song floating particle effect
    /// </summary>
    public class FloatingParticles : StoryboardObjectGenerator
    {
        [Group("Timing")]
        [Configurable] public int StartTime = 0;
        [Configurable] public int EndTime = 0;

        [Group("Particle Settings")]
        [Configurable] public string ParticlePath = "sb/circle.png";
        [Configurable] public int ParticleCount = 10;
        [Configurable] public double Scale = 0.1;
        [Configurable] public double Opacity = 0.1;
        [Configurable] public int LoopCount = 80;
        [Configurable] public int LoopDuration = 5050;

        public override void Generate()
        {
            if (StartTime == EndTime)
                EndTime = (int)AudioDuration;

            if (EndTime <= StartTime || LoopDuration <= 0 || LoopCount <= 0)
                return;

            var random = new System.Random();
            var layer = GetLayer("Particles");

            var colors = new[]
            {
                new Color4(252, 254, 254, 255),
                new Color4(100, 225, 249, 255),
                new Color4(155, 234, 250, 255),
                new Color4(192, 236, 245, 255),
                new Color4(81, 120, 127, 255),
                new Color4(41, 98, 110, 255),
            };

            for (int i = 0; i < ParticleCount; i++)
            {
                // Random start position
                var startX = random.Next(-107, 747);
                var startY = random.Next(-20, 500);
                // Random end position (drift direction)
                var endX = startX + random.Next(-750, 750);
                var endY = startY + random.Next(-200, 200);

                var rotation = 3.05 + random.NextDouble() * 0.2;
                var color = colors[random.Next(colors.Length)];

                // Keep particle start inside the valid window so at least one full loop can fit.
                var maxStagger = Math.Max(0, Math.Min(3000, EndTime - StartTime - LoopDuration));
                var particleStart = StartTime + random.Next(0, maxStagger + 1);

                // Clamp loop count so particle timeline never goes past EndTime.
                var maxLoopsByTime = (EndTime - particleStart) / LoopDuration;
                var effectiveLoopCount = Math.Min(LoopCount, maxLoopsByTime);
                if (effectiveLoopCount <= 0)
                    continue;

                var sprite = layer.CreateSprite(ParticlePath, OsbOrigin.BottomCentre);
                sprite.Scale(particleStart, Scale);
                sprite.Rotate(particleStart, rotation);
                sprite.Color(particleStart, color);

                sprite.StartLoopGroup(particleStart, effectiveLoopCount);
                sprite.Fade(OsbEasing.InSine, 0, LoopDuration / 5, 0, Opacity);
                sprite.Move(0, LoopDuration, startX, startY, endX, endY);
                sprite.Fade(OsbEasing.Out, LoopDuration * 4 / 5, LoopDuration, Opacity, 0);
                sprite.EndGroup();
            }
        }
    }
}
