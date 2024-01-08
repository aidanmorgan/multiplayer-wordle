using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Xml.Schema;
using Svg;
using Wordle.Model;

namespace Wordle.Render;

public class Renderer : IRenderer
{
    private static readonly List<string> EmbeddedFonts = new List<string>()
    {
        "fonts.ClearSans-Regular.ttf",
        "fonts.HelveticaNeue.ttf"
    };
    
    public void Render(List<DisplayWord> words, RenderOptions? inoos, RenderOutput output, Stream resultStream)
    {
        var renderOptions = inoos ?? RenderOptions.CreateFromWidth(500);
        
        var svgDocument = new SvgDocument
        {
            Width = renderOptions.ImageWidth.ToPixelUnit(),
            Height = renderOptions.ImageHeight.ToPixelUnit(),
            Fill = renderOptions.BackgroundColour.ToPaint(),
            FillOpacity = renderOptions.BackgroundOpacity
        };

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

                    var box = new SvgRectangle()
                    {
                        X = x_px.ToPixelUnit(),
                        Y = y_px.ToPixelUnit(),
                        Width = renderOptions.BoxWidth.ToPixelUnit(),
                        Height = renderOptions.BoxHeight.ToPixelUnit(),
                    };

                    if (state == LetterState.CORRECT_LETTER_INCORRECT_POSITION)
                    {
                        box.Fill = new SvgColourServer(renderOptions.Orange.ToArgb());
                        box.FillOpacity = renderOptions.OrangeOpacity;
                    }
                    else if (state == LetterState.CORRECT_LETTER_CORRECT_POSITION)
                    {
                        box.Fill = new SvgColourServer(renderOptions.Green.ToArgb());
                        box.FillOpacity = renderOptions.GreenOpacity;
                    }
                    else
                    {
                        box.Fill = new SvgColourServer(renderOptions.Grey.ToArgb());
                        box.FillOpacity = renderOptions.GreyOpacity;
                    }

                    var groupElement = new SvgGroup();
                    groupElement.Children.Add(box);
                    groupElement.Children.Add(new SvgText()
                    {
                        X = [ text_x_px.ToPixelUnit() ],
                        Y = [ text_y_px.ToPixelUnit() ], 
                        TextAnchor = SvgTextAnchor.Middle,
                        FontFamily = string.Join(", ", renderOptions.Fonts),
                        FontSize = renderOptions.FontSize.ToPixelUnit(),
                        Fill = renderOptions.FontColour.ToPaint(),
                        Text = $"{letter}".ToUpper(),
                        CustomAttributes =
                        {
                            {"dominant_baseline", "middle" },
                            { "alignment_baseline", "middle" }
                        }
                    });

                    svgDocument.Children.Add(groupElement);
                }
                else
                {
                    var border = new SvgRectangle()
                    {
                        Height = renderOptions.BoxHeight.ToPixelUnit(),
                        Width = renderOptions.BoxWidth.ToPixelUnit(),
                        X = x_px.ToPixelUnit(),
                        Y = y_px.ToPixelUnit(),
                        Stroke = renderOptions.LineColour.ToPaint(),
                        StrokeWidth = renderOptions.LineWidth.ToPixelUnit(),
                    };
                    
                    svgDocument.Children.Add(border);
                }
            }
        }

        switch (output)
        {
            case RenderOutput.Svg:
            {
                svgDocument.Write(resultStream, false);
                break;
            }

            case RenderOutput.Png:
            {
                throw new NotImplementedException();
            }
        }
    }
}