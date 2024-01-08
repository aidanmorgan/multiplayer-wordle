using System.Drawing;
using Svg;

namespace Wordle.Render;

public static class SvgExtensions
{
    public static Color ToArgb(this Tuple<int, int, int> val)
    {
        int argb = (255 << 24) + (val.Item1 << 16) + (val.Item2 << 8) + val.Item3;
        return Color.FromArgb(argb);
    }

    public static SvgColourServer ToSvgColour(this Color c)
    {
        return new SvgColourServer(c);
    }

    public static SvgPaintServer ToSvgPaint(this Color c)
    {
        return new SvgColourServer(c);
    }
    
    public static SvgPaintServer ToPaint(this Tuple<int, int, int> val)
    {
        int argb = (255 << 24) + (val.Item1 << 16) + (val.Item2 << 8) + val.Item3;
        return new SvgColourServer(Color.FromArgb(argb));
    }


    public static SvgUnit ToPixelUnit(this float f)
    {
        return new SvgUnit(SvgUnitType.Pixel, f);
    }

    public static SvgUnit ToPixelUnit(this int f)
    {
        return new SvgUnit(SvgUnitType.Pixel, f);
    }
    
    
}