namespace Catalog.Domain.Entities;

public class GameReview
{
    public Guid GameId { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
