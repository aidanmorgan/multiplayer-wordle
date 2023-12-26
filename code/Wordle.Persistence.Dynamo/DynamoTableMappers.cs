using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Wordle.Common;
using Wordle.Model;

namespace Wordle.Persistence.Dynamo;

public class DynamoMappers : IDynamoMappers
{
    public  TableMapper<Session> SessionMapping { get; } = CreateSessionMapping();
    public  TableMapper<Options> OptionsMapping { get; }= CreateOptionsMapping();
    public  TableMapper<Guess> GuessMapping { get; }= CreateGuessMapping();
    public  TableMapper<Round> RoundMapping { get; }= CreateRoundMapping();


    private static TableMapper<Session> CreateSessionMapping()
    {
        IList<DFieldMapper<Session>> fields = new List<DFieldMapper<Session>>();
        fields.Add(new DFieldMapper<Session>("pk", 
            session => session.Id.AsSessionIdAsync(),
            async (value, session) =>  session.Id = await value.AsSessionIdAsync())
        ); 
        
        // this is not used when it is read back from dynamo, its just used to help with the single-table
        // process but given session is the "main" object in the table, the pk and sk are set to the same value
        fields.Add(new DFieldMapper<Session>("sk",
            session => session.Id.AsSessionIdAsync(),
            (value, session) => Task.CompletedTask)
        );
        
        fields.Add(new DFieldMapper<Session>("created_at",
            round => round.CreatedAt.MakeDynamoAsync(),
            async (entry, round) => round.CreatedAt = await entry.AsDateTimeOffset()));
        
        
        fields.Add(new DFieldMapper<Session>("state",
            session => session.State.MakeDynamoAsync(),
            async (entry, session) => session.State = Enum.Parse<SessionState>(await entry.AsStringAsync())));
        
        fields.Add(new DFieldMapper<Session>("tenant",
            session => session.Tenant.MakeDynamoAsync(),
            async (value, session) => session.Tenant = await value.AsStringAsync())
        );
        
        fields.Add(new DFieldMapper<Session>("word",
            session => session.Word.MakeDynamoAsync(),
            async (value, session) => session.Word = await value.AsStringAsync())
        );
        
        fields.Add(new DFieldMapper<Session>("used_letters",
            session => session.UsedLetters.MakeDynamoAsync(),
            async (value, session) => session.UsedLetters = await value.AsStringListAsync())
        );
        
        fields.Add(new DFieldMapper<Session>("active_round_id",
            session => session.ActiveRoundId.MakeDynamoAsync(),
            async (value, session) => session.ActiveRoundId = await value.AsNullableGuidAsync()
        ));
        
        fields.Add(new DFieldMapper<Session>("active_round_end",
            session => session.ActiveRoundEnd.MakeDynamoAsync(),
            async (value, session) => session.ActiveRoundEnd = await value.AsNullableDateTimeOffsetAsync())
        );

        return new TableMapper<Session>(fields);
    }


    private static TableMapper<Options> CreateOptionsMapping()
    {
        IList<DFieldMapper<Options>> field = new List<DFieldMapper<Options>>();
        
        field.Add(new DFieldMapper<Options>("pk",  
            options =>
            {
                if (options.IsSession())
                {
                    return options.SessionId!.MakeDynamoAsync();
                }
                else if (options.IsTenant())
                {
                    return options.TenantId!.MakeDynamoAsync();
                }
                else
                {
                    throw new ArgumentException();
                }
            },
            async (value, options) =>
            {
                var str = await value.AsStringAsync();

                if (str.IsSessionId())
                {
                    options.SessionId = await value.AsStringAsync();                    
                }
                else if (str.IsTenantId())
                {
                    options.TenantId = await value.AsStringAsync();
                }
                else
                {
                    throw new ArgumentException();
                }
                
            })
        );
        
        field.Add(new DFieldMapper<Options>("sk", 
            options => options.Id.AsOptionsIdAsync(),
            async (value, options) => options.Id = await value.AsOptionsIdAsync())
        );
        
        field.Add(new DFieldMapper<Options>("created_at",
            round => round.CreatedAt.MakeDynamoAsync(),
            async (entry, round) => round.CreatedAt = await entry.AsDateTimeOffset()));
        
        
        field.Add(new DFieldMapper<Options>("dictionary_name", 
            options => options.DictionaryName.MakeDynamoAsync(),
            async (value, options) => options.DictionaryName = await value.AsStringAsync())
        );
        
        field.Add(new DFieldMapper<Options>("tiebreaker_strategy",
            options => options.TiebreakerStrategy.MakeDynamoAsync(),
            async (value, options) => { options.TiebreakerStrategy = await value.AsEnumAsync<TiebreakerStrategy>(); })
        );
        
        field.Add(new DFieldMapper<Options>("initial_round_length",
            options => options.InitialRoundLength.MakeDynamoAsync(),
            async (value, options) => options.InitialRoundLength = await value.AsIntAsync())
        );
        
        field.Add(new DFieldMapper<Options>("maximum_round_extensions",
            options => options.MaximumRoundExtensions.MakeDynamoAsync(),
            async (value, options) => options.MaximumRoundExtensions = await value.AsIntAsync())
        );
        
        field.Add(new DFieldMapper<Options>("word_length", 
            options => options.WordLength.MakeDynamoAsync(), 
            async (entry, options) => options.WordLength = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("max_round_extensions", 
            options => options.MaximumRoundExtensions.MakeDynamoAsync(), 
            async (entry, options) => options.MaximumRoundExtensions = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("min_answers_required", 
            options => options.MinimumAnswersRequired.MakeDynamoAsync(), 
            async (entry, options) => options.MinimumAnswersRequired = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("num_rounds", 
            options => options.NumberOfRounds.MakeDynamoAsync(), 
            async (entry, options) => options.NumberOfRounds = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("round_extension_length", 
            options => options.RoundExtensionLength.MakeDynamoAsync(), 
            async (entry, options) => options.RoundExtensionLength = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("round_extension_window", 
            options => options.RoundExtensionWindow.MakeDynamoAsync(), 
            async (entry, options) => options.RoundExtensionWindow = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("votes_per_user", 
            options => options.RoundVotesPerUser.MakeDynamoAsync(), 
            async (entry, options) => options.RoundVotesPerUser = await entry.AsIntAsync()));

        field.Add(new DFieldMapper<Options>("allow_guess_after_round_end", 
            options => options.AllowGuessesAfterRoundEnd.MakeDynamoAsync(), 
            async (entry, options) => options.AllowGuessesAfterRoundEnd = await entry.AsBooleanAsync()));
        
        
        return new TableMapper<Options>(field);
    }
    
    private static TableMapper<Round> CreateRoundMapping()
    {
        IList<DFieldMapper<Round>> fields = new List<DFieldMapper<Round>>();
        fields.Add(new DFieldMapper<Round>("pk", 
            session => session.SessionId.AsSessionIdAsync(),
            async (value, session) =>  session.SessionId = await value.AsSessionIdAsync())
        ); 
        
        fields.Add(new DFieldMapper<Round>("sk",
            round => round.Id.AsRoundIdAsync(),
            async (value, round) => round.Id = await value.AsRoundIdAsync())
        );
        
        fields.Add(new DFieldMapper<Round>("created_at",
            round => round.CreatedAt.MakeDynamoAsync(),
            async (entry, round) => round.CreatedAt = await entry.AsDateTimeOffset()));
        
        
        fields.Add(new DFieldMapper<Round>("word", 
            round => round.Guess.MakeDynamoAsync(),
            async (entry, round) => round.Guess = await entry.AsStringAsync()));
        
        fields.Add(new DFieldMapper<Round>("result",
            round => round.Result.MakeDynamoAsync(),
            async (entry, round) => round.Result = await entry.AsEnumListAsync<LetterState>()));
        
        fields.Add(new DFieldMapper<Round>("state",
            round =>  round.State.MakeDynamoAsync(),
            async (entry, round) => round.State = await entry.AsEnumAsync<RoundState>()));
        
        return new TableMapper<Round>(fields);
    }
    

    private static TableMapper<Guess> CreateGuessMapping()
    {
        IList<DFieldMapper<Guess>> fields = new List<DFieldMapper<Guess>>();
        fields.Add(new DFieldMapper<Guess>("pk", 
            guess => guess.RoundId.AsRoundIdAsync(),
            async (value, guess) => guess.RoundId = await value.AsRoundIdAsync()
        ));
        
        fields.Add(new DFieldMapper<Guess>("sk",
            guess => guess.Id.AsGuessIdAsync(),
            async (value, guess) => guess.Id = await value.AsGuessIdAsync()
        ));


        
        fields.Add(new DFieldMapper<Guess>("session_id",
            guess => guess.SessionId.MakeDynamoAsync(),
            async (v,g) => g.SessionId = await v.AsGuidAsync()
        ));
        
        fields.Add(new DFieldMapper<Guess>("created_at", 
            guess => guess.Timestamp.MakeDynamoAsync(),
            async (entry, guess) => guess.Timestamp = await entry.AsDateTimeOffset()
        ));
        
        fields.Add(new DFieldMapper<Guess>("word",
            guess => guess.Word.MakeDynamoAsync(),
            async (entry, guess) => guess.Word = await entry.AsStringAsync()));
        
        fields.Add(new DFieldMapper<Guess>("user",
            guess => guess.User.MakeDynamoAsync(),
            async (e, g) => g.User = await e.AsStringAsync()
        ));
        
        

        return new TableMapper<Guess>(fields);
    }
}