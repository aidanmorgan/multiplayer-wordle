namespace DictionaryLoader;

public abstract class Common
{
    protected readonly List<string> TYPES = new List<string>()
    {
        "dutch",
        "french",
        "italian",
        "scrabble",
        "syscalls",
        "wordle"
    };
    
    private readonly List<string> DisallowedCharacters = [",", " ", ".", "`", "'"];

    protected List<string> ReadDictionary(string dict)
    {
        var lines = File.ReadAllLines($"./dictionaries/{dict}.txt");

        return lines
            .Select(x => x.Trim().ToUpperInvariant())
            .Where(cleaned => cleaned.Length >= 3 && !DisallowedCharacters.Any(cleaned.Contains))
            .ToList();
    }
    
}