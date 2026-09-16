using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;

namespace Catalog.Infrastructure.Repositories;

public class DynamoDbGameReviewRepository : IGameReviewRepository
{
    private readonly IAmazonDynamoDB _dynamo;
    private const string TableName = "fcg-game-reviews";

    public DynamoDbGameReviewRepository(IAmazonDynamoDB dynamo)
    {
        _dynamo = dynamo;
    }

    public async Task AddReviewAsync(GameReview review, CancellationToken ct = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["GameId"] = new AttributeValue { S = $"game#{review.GameId}" },
            ["SortKey"] = new AttributeValue { S = $"review#{review.UserId}#{review.CreatedAt:O}" },
            ["UserId"] = new AttributeValue { S = review.UserId.ToString() },
            ["UserName"] = new AttributeValue { S = review.UserName },
            ["Rating"] = new AttributeValue { N = review.Rating.ToString() },
            ["Comment"] = new AttributeValue { S = review.Comment },
            ["CreatedAt"] = new AttributeValue { S = review.CreatedAt.ToString("O") }
        };

        await _dynamo.PutItemAsync(TableName, item, ct);
    }

    public async Task<IEnumerable<GameReview>> GetReviewsByGameAsync(Guid gameId, CancellationToken ct = default)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "GameId = :gid",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gid"] = new AttributeValue { S = $"game#{gameId}" }
            }
        };

        var response = await _dynamo.QueryAsync(request, ct);
        return response.Items.Select(MapToReview);
    }

    private static GameReview MapToReview(Dictionary<string, AttributeValue> item) => new()
    {
        GameId = Guid.Parse(item["GameId"].S.Replace("game#", "")),
        UserId = Guid.Parse(item.TryGetValue("UserId", out var uid) ? uid.S : item["SortKey"].S.Split('#')[1]),
        UserName = item.TryGetValue("UserName", out var un) ? un.S : string.Empty,
        Rating = item.TryGetValue("Rating", out var r) ? int.Parse(r.N) : 0,
        Comment = item.TryGetValue("Comment", out var c) ? c.S : string.Empty,
        CreatedAt = item.TryGetValue("CreatedAt", out var ca)
            ? DateTime.Parse(ca.S, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow
    };
}
