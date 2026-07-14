// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;
public class UserGameRepository : IUserGameRepository
{
    private readonly CatalogDbContext _db;

    public UserGameRepository(CatalogDbContext db) => _db = db;
    public async Task AddAsync(UserGame userGame, CancellationToken cancellationToken = default)
    {
        await _db.UserGames.AddAsync(userGame, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _db.UserGames.AnyAsync(ug => ug.UserId == userId && ug.GameId == gameId, cancellationToken);
    }

    public async Task<IEnumerable<UserGame>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserGames
                        .Include(ug => ug.Game)
                        .Where(ug => ug.UserId == userId)
                        .ToListAsync();
    }
}
