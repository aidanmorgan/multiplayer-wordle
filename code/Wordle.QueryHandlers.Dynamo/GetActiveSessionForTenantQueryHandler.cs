using Amazon.DynamoDBv2.DocumentModel;
using MediatR;
using Queries;
using Wordle.Common;
using Wordle.Model;
using Wordle.Persistence.Dynamo;

namespace Wordle.QueryHandlers.Dynamo;

public class GetActiveSessionForTenantQueryHandler : IRequestHandler<GetActiveSessionForTenantQuery, Guid?>
{
    private readonly DynamoGameConfiguration _dynamoConfiguration;

    public GetActiveSessionForTenantQueryHandler(DynamoGameConfiguration dynamoConfiguration)
    {
        _dynamoConfiguration = dynamoConfiguration;
    }

    public async Task<Guid?> Handle(GetActiveSessionForTenantQuery request, CancellationToken cancellationToken)
    {
        var table = _dynamoConfiguration.GetTable();

        // this chaining looks odd, but it is an attempt to firstly ensure that we are looking at the correct object
        // type in the database (if pk and sk are equal then we are looking at a Session object) and then checking
        // the tenant_id and session_state values for the values we actually want. Given we don't know the session id
        // we can't do the pk = or sk = as we are scanning to find the id.
        var result = await table.Query(new QueryOperationConfig()
            {
                KeyExpression = new Expression()
                {
                    ExpressionStatement = "tenant = :t and begins_with(pk, :s)",
                    ExpressionAttributeValues =
                    {
                        { ":t", Tenant.CreateTenantId(request.TenantType, request.TenantName) },
                        { ":s", await IIdConstants.SessionIdPrefix.MakeDynamoAsync() }
                    }
                },
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string>()
                    { "tenant", "pk", "state", "active_round_id", "active_round_end" },
                IndexName = "tenant-session-index"
            }
        ).GetRemainingAsync(cancellationToken);

        if (result.Count == 0)
        {
            return null;
        }

        var subset = result
            .Where(x => !x["active_round_end"].IsNull())
            .Where(x => x["state"] == SessionState.ACTIVE.ToString())
            .ToList();

        if (subset.Count > 0) 
        {
            // get the document, sort them by the newest round and then return the id
            return await subset
                .Select(x => new Tuple<DateTimeOffset, Document>(DateTimeOffset.Parse(x["active_round_end"]), x))
                .OrderByDescending(x => x.Item1)
                .First().Item2["pk"]
                .AsSessionIdAsync();
        }

        return null;
    }
}