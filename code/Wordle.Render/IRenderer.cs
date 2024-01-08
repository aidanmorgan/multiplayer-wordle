using Wordle.Model;

namespace Wordle.Render;

public class DisplayWord
{
    public string Word { get; set; }
    public List<LetterState> State { get; set; } = new List<LetterState>();

    public DisplayWord(string word, List<LetterState> state)
    {
        this.Word = word;
        this.State = state;
    }
}

public interface IRenderer
{
    void Render(List<DisplayWord> letters, RenderOptions? renderOptions, RenderOutput output, Stream stream);
}