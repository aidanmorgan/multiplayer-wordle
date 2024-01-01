using Amazon.DynamoDBv2.DocumentModel;
using MediatR;
using Wordle.Common;
using Wordle.Model;
using Wordle.Persistence.Dynamo;
using Wordle.Queries;

namespace Wordle.QueryHandlers.Dynamo;

public class GetOptionsForTenantQueryHandler : IRequestHandler<GetOptionsForTenantQuery, Options?>
{
    private readonly DynamoGameConfiguration _dynamoConfiguration;
    private readonly IDynamoMappers _dynamoMappers;


    public GetOptionsForTenantQueryHandler(DynamoGameConfiguration dynamoConfiguration, IDynamoMappers dynamoMappers)
    {
        _dynamoConfiguration = dynamoConfiguration;
        _dynamoMappers = dynamoMappers;
    }

    public async Task<Options?> Handle(GetOptionsForTenantQuery request, CancellationToken cancellationToken)
    {
        var table = _dynamoConfiguration.GetTable();

        var query = await table.Query(new QueryOperationConfig()
        {
            KeyExpression = new Expression()
            {
                ExpressionStatement = "pk = :pk and begins_with(sk, :sk)",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>()
                {
                    { ":pk", Tenant.CreateTenantId(request.TenantType, request.TenantName)},
                    { ":sk", IIdConstants.OptionsIdPrefix}
                }
            },
            Select = SelectValues.SpecificAttributes,
            AttributesToGet = _dynamoMappers.OptionsMapping.ColumnNames
        }).GetNextSetAsync(cancellationToken);

        if (query.Count == 0)
        {
            return null;
        }

        return await _dynamoMappers.OptionsMapping.FromDynamoAsync(query[0]);
    }
}