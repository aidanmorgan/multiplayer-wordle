using Wordle.Model;

namespace Wordle.Dictionary;

public class DictionaryDetail
{
    public string DictionaryName { get; set; }
    public int RowCount { get; set; }
    public int WordLength { get; set; }

    public string Key => $"{DictionaryName}#{WordLength}";
    
    public string AllowedLetters { get; set; }
}

public interface IWordleDictionaryService
{
    Task<string> RandomWord(Options opts);
    Task<string> RandomWord(string dictionary, int numLetters);

    Task<List<DictionaryDetail>> GetDictionaries();

    Task<DictionaryDetail?> GetDictionary(string dictionary, int numLetters);
}