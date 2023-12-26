namespace Wordle.Dictionary;

public class DictionaryException : Exception
{
    public DictionaryException(string msg) : base(msg)  {}
}