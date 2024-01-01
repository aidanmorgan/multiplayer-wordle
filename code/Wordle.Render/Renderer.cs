using System.Drawing;
using System.Reflection;
using System.Text;
using GrapeCity.Documents.Imaging;
using GrapeCity.Documents.Svg;
using GrapeCity.Documents.Text;
using Wordle.Model;

namespace Wordle.Render;

public class Renderer : IRenderer
{
    private static readonly List<string> EmbeddedFonts = new List<string>()
    {
        "fonts.ClearSans-Regular.ttf",
        "fonts.HelveticaNeue.ttf"
    };
    
    static Renderer()
    {
        var assembly = Assembly.GetAssembly(typeof(Renderer));

        var fonts = EmbeddedFonts.Select(x =>
        {
            using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{x}");
            return Font.FromStream(stream);
        });
        
        FontCollection.SystemFonts.AppendFallbackFonts(fonts.ToArray());
        FontCollection.SystemFonts.DefaultFont = fonts.First();
    }
    public void Render(List<DisplayWord> words, RenderOptions? inoos, Stream resultStream)
    {
        var renderOptions = inoos ?? RenderOptions.CreateFromWidth(500);

        using var svg = new GcSvgDocument();
        svg.RootSvg.Height = new SvgLength(renderOptions.ImageHeight, SvgLengthUnits.Pixels);
        svg.RootSvg.Width = new SvgLength(renderOptions.ImageWidth, SvgLengthUnits.Pixels);
        svg.RootSvg.Fill = new SvgPaint(renderOptions.BackgroundColour.ToArgb());
        svg.RootSvg.FillOpacity = renderOptions.BackgroundOpacity;

        for (var y = 0; y < renderOptions.NumGuesses; y++)
        {
            for (var x = 0; x < renderOptions.WordLength; x++)
            {
                var x_px = renderOptions.BoxWidth + (x * renderOptions.BoxWidth) + (x * renderOptions.BoxSpacing);
                var y_px = renderOptions.BoxHeight + (y * renderOptions.BoxHeight) + (y * renderOptions.BoxSpacing);

                if (y < words.Count && !string.IsNullOrEmpty(words[y].Word))
                {
                    var letter = words[y].Word[x];
                    var state = words[y].State[x];

                    var text_x_px = (float)Math.Ceiling(x_px + ((double)renderOptions.BoxWidth / 2.0));
                    var text_y_px = (float)Math.Ceiling(y_px + ((double)renderOptions.BoxHeight / 2.0) + ((double)renderOptions.FontSize / 2.0));

                    SvgRectElement box = new SvgRectElement()
                    {
                        X = new SvgLength(x_px),
                        Y = new SvgLength(y_px),
                        Width = new SvgLength(renderOptions.BoxWidth, SvgLengthUnits.Pixels),
                        Height = new SvgLength(renderOptions.BoxHeight, SvgLengthUnits.Pixels),
                    };

                    if (state == LetterState.CORRECT_LETTER_INCORRECT_POSITION)
                    {
                        box.Fill = new SvgPaint(renderOptions.Orange.ToArgb());
                        box.FillOpacity = renderOptions.OrangeOpacity;
                    }
                    else if (state == LetterState.CORRECT_LETTER_CORRECT_POSITION)
                    {
                        box.Fill = new SvgPaint(renderOptions.Green.ToArgb());
                        box.FillOpacity = renderOptions.GreenOpacity;
                    }
                    else
                    {
                        box.Fill = new SvgPaint(renderOptions.Grey.ToArgb());
                        box.FillOpacity = renderOptions.GreyOpacity;
                    }

                    SvgGroupElement groupElement = new SvgGroupElement();
                    groupElement.Children.Add(box);
                    groupElement.Children.Add(new SvgTextElement()
                    {
                        X = new List<SvgLength>() { new SvgLength(text_x_px, SvgLengthUnits.Pixels)},
                        Y = new List<SvgLength>() { new SvgLength(text_y_px, SvgLengthUnits.Pixels)},
                        TextAnchor = SvgTextAnchor.Middle,
                        FontFamily = renderOptions.fonts.Select(x => new SvgFontFamily(x, true)).ToList(),
                        FontSize = new SvgLength(renderOptions.FontSize, SvgLengthUnits.Pixels),
                        CustomAttributes = new List<SvgCustomAttribute>()
                        {
                            new SvgCustomAttribute("dominant_baseline", "middle"),
                            new SvgCustomAttribute("alignment_baseline", "middle"),
                        },
                        Fill = new SvgPaint(renderOptions.FontColour.ToArgb()),
                        Children = { new SvgContentElement() { Content = $"{letter}".ToUpper() }}
                    });
                        
                    svg.RootSvg.Children.Add(groupElement);
                }
                else
                {
                    var border = new SvgRectElement()
                    {
                        Height = new SvgLength(renderOptions.BoxHeight, SvgLengthUnits.Pixels),
                        Width = new SvgLength(renderOptions.BoxWidth, SvgLengthUnits.Pixels),
                        X = new SvgLength(x_px),
                        Y = new SvgLength(y_px),
                        Stroke = new SvgPaint(renderOptions.LineColour.ToArgb()),
                        StrokeWidth = new SvgLength(renderOptions.LineWidth),
                    };
                    
                    svg.RootSvg.Children.Add(border);
                }
            }
        }

        switch (renderOptions.Output)
        {
            case RenderOutput.Svg:
            {
                svg.Save(resultStream);
                break;
            }

            case RenderOutput.Png:
            {
                
                using (var bmp = new GcBitmap((int)renderOptions.ImageWidth, (int)renderOptions.ImageHeight, false))
                using (var g = bmp.CreateGraphics(Color.FromArgb(renderOptions.BackgroundColour.ToArgbInt())))
                {
                    g.DrawSvg(svg, PointF.Empty);
                    bmp.SaveAsPng(resultStream);
                }

                break;
            }
        }
    }
}