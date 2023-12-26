namespace Wordle.Common;

public interface IEtaggable
{
    string FieldHash { get; }
}

public class EtagValue<T> where T : class, IEtaggable
{
    private readonly T? _originalValue;
    private T? _latestValue;

    public EtagValue(T? val)
    {
        this._originalValue = val;
        this._latestValue = val;
    }

    public void Set(T val)
    {
        this._latestValue = val;
    }

    public T? Get()
    {
        return this._latestValue;
    }
    
    public bool Changed {
        get
        {
            if (_originalValue == null && _latestValue == null)
            {
                return false;
            }

            if (_originalValue == null && _latestValue != null)
            {
                return true;
            }

            if (_originalValue != null && _latestValue == null)
            {
                return true;
            }

            return !string.Equals(_originalValue.FieldHash, _latestValue.FieldHash);
        }
    }

    public string ETag => _latestValue != null ? _latestValue.FieldHash : null;
    public bool HasValue => _latestValue != null;

    public string ToString()
    {
        return _latestValue?.ToString();
    }
}