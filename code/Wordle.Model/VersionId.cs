namespace Wordle.Model;

public class VersionId<T>
{
    private string VersionType => typeof(T).Name;
    public Guid Id { get; init; }
    public long Version { get; init; }

    public VersionId()
    {
        
    }

    public VersionId(Guid id, long version)
    {
        Id = id;
        Version = version;
    }

    public override string ToString()
    {
        return $"{VersionType}#{Id}#{Version}";
    }
}