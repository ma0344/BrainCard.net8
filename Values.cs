using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BrainCard
{
    public static class Values
    {
        public static readonly double cardWidth = 537;
        public static readonly double cardHeight = 380;
        public static readonly double thumbWidth = cardWidth + 2;
        public static readonly double thumbHeight = cardHeight + 2;
        public static readonly double AspectRatio = cardWidth / cardHeight;
        public static readonly double SubWindowShadowThickness = 20;
        public static readonly Effect DefaultCardShadow = new DropShadowEffect
        {
            BlurRadius = 16.0f,
            ShadowDepth = 3,
            Direction = 315,
            Color = Colors.Black,
            Opacity = 0.2f,
        };

    }
}
