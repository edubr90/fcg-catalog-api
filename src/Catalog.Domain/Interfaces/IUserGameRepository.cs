// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Entities;

namespace Catalog.Domain.Interfaces;
public interface IUserGameRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task AddAsync(UserGame userGame,  CancellationToken cancellationToken = default);
    Task<IEnumerable<UserGame>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
