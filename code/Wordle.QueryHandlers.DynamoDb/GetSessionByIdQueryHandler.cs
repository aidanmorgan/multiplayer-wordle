using Amazon.DynamoDBv2.DocumentModel;
using MediatR;
using Wordle.Common;
using Wordle.Model;
using Wordle.Persistence.Dynamo;
using Wordle.Queries;

namespace Wordle.QueryHandlers.Dynamo;

public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, SessionQueryResult?>
{
    private readonly DynamoGameConfiguration _dynamoConfiguration;
    private readonly IDynamoMappers _dynamoMappers;

    public GetSessionByIdQueryHandler(DynamoGameConfiguration dynamoConfiguration, IDynamoMappers dynamoMappers)
    {
        _dynamoConfiguration = dynamoConfiguration;
        _dynamoMappers = dynamoMappers;
    }

    public async Task<SessionQueryResult?> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var table = _dynamoConfiguration.GetTable();

        var result = await table.Query(new QueryOperationConfig()
        {
            CollectResults = true,
            KeyExpression = new Expression()
            {
                ExpressionStatement = "pk = :p",
                ExpressionAttributeValues =
                {
                    {":p", request.Id.ToSessionId()}
                }
            },
            Select = SelectValues.SpecificAttributes,
            AttributesToGet = GetAttributesToRetrieve(request)
        }).GetNextSetAsync(cancellationToken);
        
        var entry = result.FirstOrDefault(x => x.IsSession());
        if (entry == null)
        {
            return null;
        }

        SessionQueryResult ret = new SessionQueryResult
        {
            Session = await _dynamoMappers.SessionMapping.FromDynamoAsync(entry)
        };

        if (!request.IncludeWord)
        {
            ret.Session.Word = string.Empty;
        }

        if (request.IncludeOptions)
        {
            var opts = result.FirstOrDefault(x => x.IsOptions());
            ret.Options = opts == null
                ? new Options()
                : await _dynamoMappers.OptionsMapping.FromDynamoAsync(opts); 
        }

        if (request.IncludeRounds)
        {
            ret.Rounds = (await result
                .Where(x => x.IsRound())
                .Select(x => _dynamoMappers.RoundMapping.FromDynamoAsync(x))
                .WaitAll<Round>())
                .OrderBy(x => x.CreatedAt)
                .ToList();
        }
        
        return ret;
    }

    private List<string> GetAttributesToRetrieve(GetSessionByIdQuery request)
    {
        var attributes = new List<string>(_dynamoMappers.SessionMapping.ColumnNames);
        
        if (request.IncludeOptions)
        {
            attributes.AddRange(_dynamoMappers.OptionsMapping.ColumnNames);
        }

        if (request.IncludeRounds)
        {
            attributes.AddRange(_dynamoMappers.RoundMapping.ColumnNames);
        }

        return attributes.Distinct().ToList();
    }
}