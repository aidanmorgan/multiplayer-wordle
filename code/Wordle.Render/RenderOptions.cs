namespace Wordle.Render;

public enum RenderOutput
{
    Png,
    Svg
}

public class RenderOptions
{
    private const double SpacingRatio = 10.0 / 1265.0;
    private const double LineWidthRatio = 4.0 / 1265.0;
    private const double FontSizeToHeightRatio = 48.0 / 125.0;

    private const double WidthToHeightRatio = 1450.0 / 1265.0;
    private const double HeightToWidthRatio = 1265.0 / 1450.0;
    
    public float ImageHeight { get; private set; }
    public float ImageWidth { get; private set; }

    public Tuple<int, int, int> Green { get; private set; }
    public float GreenOpacity { get; private set; } = 1.0f;

    public Tuple<int, int, int> Orange { get; private set; }
    public float OrangeOpacity { get; private set; } = 1.0f;

    public Tuple<int, int, int> Grey { get; private set; }
    public float GreyOpacity { get; private set; } = 1.0f;
    
    public Tuple<int,int,int> BackgroundColour { get; private set; }
    public float BackgroundOpacity { get; private set; } = 1.0f;

    public Tuple<int, int, int> LineColour { get; private set; }
    public int LineWidth { get; private set; } = 1;

    public int FontSize { get; private set; } = 1;
    public Tuple<int, int, int> FontColour { get; private set; }

    public int BoxSpacing { get; private set; } = 1;
    public int BoxWidth { get; private set; } = 1;
    public int BoxHeight { get; private set; } = 1;

    public int WordLength { get; private set; } = 1;
    public int NumGuesses { get; private set; } = 1;
    public List<string> fonts = new List<string>() {"'Clear Sans'", "'Helvetica Neue'", "Arial", "sans-serif"};

    private static void ConfigureFillStyles(RenderOptions opts)
    {
        opts.LineColour = new Tuple<int, int, int>(211, 214, 218);

        opts.Grey = new Tuple<int, int, int>(120, 124, 126);
        opts.GreyOpacity = 1.0f;

        opts.Orange = new Tuple<int, int, int>(181, 159, 58);
        opts.OrangeOpacity = 1.0f;

        opts.Green = new Tuple<int, int, int>(83, 141, 78);
        opts.GreenOpacity = 1.0f;

        opts.BackgroundColour = new Tuple<int, int, int>(255, 255, 255);
        opts.BackgroundOpacity = 1.0f;

        opts.FontSize = (int) Math.Floor(FontSizeToHeightRatio * (double) opts.BoxHeight);
        opts.FontColour = new Tuple<int, int, int>(215, 218, 220);
    }

    public static RenderOptions CreateFromHeight(int height, int wordLength = 5, int numGuesses = 6)
    {
        var options = new RenderOptions()
        {
            WordLength = wordLength,
            NumGuesses = numGuesses,

            ImageHeight = height,
            LineWidth = (int) Math.Floor(LineWidthRatio * height),
            BoxSpacing = (int) Math.Floor(SpacingRatio * height)
        };

        int temp = height - ((numGuesses * 2 * options.LineWidth) + ((numGuesses - 1) * options.BoxSpacing));
        options.BoxHeight = (int)Math.Floor( (double)temp / (double)(numGuesses + 2));
        options.BoxWidth = options.BoxHeight;

        options.ImageWidth = (options.BoxWidth + (wordLength + 2)) + ((wordLength - 1) * options.BoxSpacing);

        ConfigureFillStyles(options);

        return options;
    }

    public static RenderOptions CreateFromWidth(int width, int wordLength = 5, int numGuesses = 6)
    {
        var options = new RenderOptions()
        {
            WordLength = wordLength,
            NumGuesses = numGuesses,

            ImageWidth = width,
            LineWidth = (int) Math.Floor(LineWidthRatio * width),
            BoxSpacing = (int) Math.Floor(SpacingRatio * width)
        };

        int temp = width - ((wordLength * 2 * options.LineWidth) + ((wordLength - 2) * options.BoxSpacing));
        options.BoxWidth = (int) Math.Floor((double) temp / (double) (wordLength + 2));
        options.BoxHeight = options.BoxWidth;

        options.ImageHeight = (options.BoxHeight * (numGuesses + 2)) + ((numGuesses - 1) * options.BoxSpacing);

        ConfigureFillStyles(options);
        return options;
    }

    // prevent direct instantiation of this class, use the static factory methods.
    private RenderOptions()
    {
        
    }
}