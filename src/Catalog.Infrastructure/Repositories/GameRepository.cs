// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;
public class GameRepository : IGameRepository
{
    private readonly CatalogDbContext _db;

    public GameRepository(CatalogDbContext db) => _db = db;

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _db.Games.AddAsync(game, cancellationToken);
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<Game>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Games.Where(g => g.IsActive).ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        _db.Games.Update(game);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _db.Games.FindAsync(new object[] { id }, cancellationToken);
        if (game != null) _db.Games.Remove(game);
    }

}
