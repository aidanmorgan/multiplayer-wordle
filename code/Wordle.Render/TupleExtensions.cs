using GrapeCity.Documents.Svg;

namespace Wordle.Render;

public static class TupleExtensions
{
    public static SvgPaint ToPaint(this Tuple<int, int, int> val)
    {
        return new SvgPaint(val.ToArgb());
    }
    public static SvgColor ToArgb(this Tuple<int, int, int> val)
    {
        int argb = (255 << 24) + (val.Item1 << 16) + (val.Item2 << 8) + val.Item3;
        return new SvgColor(argb);
    }

    public static int ToArgbInt(this Tuple<int, int, int> val)
    {
        return (255 << 24) + (val.Item1 << 16) + (val.Item2 << 8) + val.Item3;
    }
}