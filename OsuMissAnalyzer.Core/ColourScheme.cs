using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Core
{
    public class ColourScheme
    {
        public enum Type {Light, Dark};
        public static readonly ColourScheme Default = new("Default", Type.Light)
        {
            BackgroundColour = Color.White,
            PlayfieldColour = Color.DarkGray,
            TextColour = Color.Black,
            LineColour = Color.Black,
            MidpointColour = Color.Red,
            Colour300 = Color.FromRgb(23, 175, 235),
            Colour100 = Color.FromRgb(0, 255, 60),
            Colour50 = Color.Purple,
            CircleStartColour = Color.FromRgb(100, 100, 100),
            CircleEndColour = Color.FromRgb(200, 200, 200),
            CircleSelectedColour = Color.FromRgb(150, 100, 100),
            SliderColour = Color.DarkGoldenrod,
        };
        public static readonly ColourScheme Dark = new("Dark", Type.Dark)
        {
            BackgroundColour = Color.FromRgb(16, 16, 16),
            PlayfieldColour = Color.DarkGray,
            TextColour = Color.LightGrey,
            LineColour = Color.LightGrey,
            MidpointColour = Color.Red,
            Colour300 = Color.SkyBlue,
            Colour100 = Color.SpringGreen,
            Colour50 = Color.Violet,
            CircleStartColour = Color.FromRgb(120, 120, 120),
            CircleEndColour = Color.FromRgb(20, 20, 20),
            CircleSelectedColour = Color.FromRgb(150, 100, 100),
            SliderColour = Color.Goldenrod,
        };
        public string Name { get; init; }
        public Type SchemeType;
        public ColourScheme(string name, Type type) { Name = name; SchemeType = type; }

        public Color BackgroundColour { get; init; }
        public Color PlayfieldColour { get; init; }
        public Color TextColour { get; init; }
        public Color LineColour { get; init; }
        public Color MidpointColour { get; init; }
        public Color Colour300 { get; init; }
        public Color Colour100 { get; init; }
        public Color Colour50 { get; init; }
        public Color CircleStartColour { get; init; }
        public Color CircleEndColour { get; init; }
        public Color CircleSelectedColour { get; init; }
        public Color SliderColour { get; init; }
        public Color GetCircleColour(float time)
        {
            if (time == 0) return CircleSelectedColour;
            var start = (Vector4)CircleStartColour;
            var end = (Vector4)CircleEndColour;
            return new Color(Vector4.Lerp(start, end, time));
        }

        public static ColourScheme Parse(string s)
        {
            return typeof(ColourScheme).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ColourScheme))
                .Select(f => (ColourScheme)f.GetValue(null))
                .FirstOrDefault(scheme => scheme != null && scheme.Name.Equals(s, StringComparison.OrdinalIgnoreCase));
        }
    }
}