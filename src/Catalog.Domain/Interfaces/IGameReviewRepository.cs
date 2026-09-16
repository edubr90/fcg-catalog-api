using Catalog.Domain.Entities;

namespace Catalog.Domain.Interfaces;

public interface IGameReviewRepository
{
    Task AddReviewAsync(GameReview review, CancellationToken ct = default);
    Task<IEnumerable<GameReview>> GetReviewsByGameAsync(Guid gameId, CancellationToken ct = default);
}
