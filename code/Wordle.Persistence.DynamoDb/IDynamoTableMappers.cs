using Wordle.Model;

namespace Wordle.Persistence.DynamoDb;

public interface IDynamoMappers
{
    public TableMapper<Session> SessionMapping { get;}
    public TableMapper<Options> OptionsMapping { get;}
    public TableMapper<Guess> GuessMapping { get;}
    public TableMapper<Round> RoundMapping { get;}

}