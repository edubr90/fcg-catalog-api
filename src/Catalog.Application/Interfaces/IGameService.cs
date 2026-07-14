// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Application.DTOs;

namespace Catalog.Application.Interfaces;
public interface IGameService
{
    Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<GameResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GameResponse> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderResponse> PurchaseAsync(Guid gameId, Guid userId, CancellationToken cancellationToken= default);
    Task<IEnumerable<UserGameResponse>> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default);
}
