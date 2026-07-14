// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Interfaces;
using Catalog.Infrastructure.Persistence;

namespace Catalog.Infrastructure.UnitOfWork;
public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _db;

    public UnitOfWork(CatalogDbContext db) => _db = db;

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
