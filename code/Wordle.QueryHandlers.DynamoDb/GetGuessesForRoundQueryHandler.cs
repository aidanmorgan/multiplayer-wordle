using Amazon.DynamoDBv2.DocumentModel;
using MediatR;
using Wordle.Common;
using Wordle.Model;
using Wordle.Persistence.Dynamo;
using Wordle.Queries;

namespace Wordle.QueryHandlers.Dynamo;

public class GetGuessesForRoundQueryHandler : IRequestHandler<GetGuessesForRoundQuery, List<Guess>>
{
    private readonly DynamoGameConfiguration _dynamoConfiguration;
    private readonly IDynamoMappers _dynamoMappers;


    public GetGuessesForRoundQueryHandler(DynamoGameConfiguration dynamoConfiguration, IDynamoMappers dynamoMappers)
    {
        _dynamoConfiguration = dynamoConfiguration;
        _dynamoMappers = dynamoMappers;
    }

    public async Task<List<Guess>> Handle(GetGuessesForRoundQuery request, CancellationToken cancellationToken)
    {
        var table = _dynamoConfiguration.GetTable();

        var res = await table.Query(
            new QueryOperationConfig()
            {
                KeyExpression = new Expression()
                {
                    ExpressionStatement = "pk = :p and begins_with(sk, :s)",
                    ExpressionAttributeValues =
                    {
                        {":p", await request.RoundId.AsRoundIdAsync()},
                        {":s", IIdConstants.GuessIdPrefix}
                    }
                },
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = _dynamoMappers.GuessMapping.ColumnNames
            }
        ).GetRemainingAsync(cancellationToken);
        
        List<Guess> guesses = new List<Guess>();
        
        var mapTasks = 
           await res.Select(async x => await _dynamoMappers.GuessMapping.FromDynamoAsync(x)).WaitAll();
        
        guesses.AddRange(mapTasks);

        return guesses.OrderBy(x => x.Timestamp).ToList();
    }
}