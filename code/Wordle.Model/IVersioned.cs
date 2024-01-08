namespace Wordle.Model;

public interface IVersioned
{
    long Version { get; set; }
}