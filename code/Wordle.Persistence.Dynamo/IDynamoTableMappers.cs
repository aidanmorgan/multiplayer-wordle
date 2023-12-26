using Wordle.Model;
using Wordle.Persistence.Dynamo;

namespace Wordle.Persistence.Dynamo;

public interface IDynamoMappers
{
    public TableMapper<Session> SessionMapping { get;}
    public TableMapper<Options> OptionsMapping { get;}
    public TableMapper<Guess> GuessMapping { get;}
    public TableMapper<Round> RoundMapping { get;}

}