// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using FCG.Shared.Events;
using MassTransit;

namespace Catalog.Application.Services;
public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserGameRepository _userGameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public GameService(
        IGameRepository gameRepository,
        IUserGameRepository userGameRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _gameRepository = gameRepository;
        _userGameRepository = userGameRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }
    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = new Game(request.Title, request.Description, request.Price, request.Genre, request.Developer, request.ReleaseDate);
        await _gameRepository.AddAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToResponse(game);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Game not found");

        game.Deactivate();
        await _gameRepository.UpdateAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<IEnumerable<GameResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetAllAsync(cancellationToken);
        return games.Select(MapToResponse);
    }

    public async Task<GameResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Game not found");
        return MapToResponse(game);
    }

    public async Task<IEnumerable<UserGameResponse>> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _userGameRepository.GetByUserIdAsync(userId, cancellationToken);
        return items.Select(ug => new UserGameResponse(
            ug.GameId,
            ug.Game?.Title ?? string.Empty,
            ug.Game?.Description ?? string.Empty,
            ug.Game?.Price ?? 0,
            ug.Game?.Genre ?? Domain.Enums.GameGenre.Other,
            ug.Game?.Developer ?? string.Empty,
            ug.Game?.ReleaseDate ?? DateTime.MinValue,
            ug.AcquireAt));
    }

    public async Task<PurchaseOrderResponse> PurchaseAsync(Guid gameId, Guid userId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken)
            ?? throw new KeyNotFoundException("Game not found");

        if (!game.IsActive)
            throw new InvalidOperationException("Game is not available for purchase");

        if (await _userGameRepository.ExistsAsync(userId, gameId, cancellationToken))
            throw new InvalidOperationException("Game already in library");

        var orderId = Guid.NewGuid();

        await _publishEndpoint.Publish(new OrderPlacedEvent
        {
            OrderId = orderId,
            UserId = userId,
            GameId = gameId,
            Price = game.Price,
            PlacedAt = DateTime.UtcNow
        }, cancellationToken);

        return new PurchaseOrderResponse(orderId, "Order placed. Payment is being processed.");
    }

    public async Task<GameResponse> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Game not found");

        if (!string.IsNullOrWhiteSpace(request.Title))
            game.SetTitle(request.Title);

        if (!string.IsNullOrWhiteSpace(request.Description))
            game.SetDescription(request.Description);

        if (request.Price.HasValue)
            game.SetPrice(request.Price.Value);

        if (request.Genre.HasValue)
            game.SetGenre(request.Genre.Value);

        if (!string.IsNullOrWhiteSpace(request.Developer))
            game.SetDeveloper(request.Developer);

        await _gameRepository.UpdateAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return MapToResponse(game);

    }

    private static GameResponse MapToResponse(Game g) =>
        new(g.Id, g.Title, g.Description, g.Price, g.Genre, g.Developer, g.ReleaseDate, g.IsActive);
}
