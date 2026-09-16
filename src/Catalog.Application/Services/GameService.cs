using System.Text.Json;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using FCG.Shared.Events;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Catalog.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserGameRepository _userGameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISqsPublisher _sqsPublisher;
    private readonly IDistributedCache _cache;
    private readonly string _orderPlacedQueueUrl;

    public GameService(
        IGameRepository gameRepository,
        IUserGameRepository userGameRepository,
        IUnitOfWork unitOfWork,
        ISqsPublisher sqsPublisher,
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _gameRepository = gameRepository;
        _userGameRepository = userGameRepository;
        _unitOfWork = unitOfWork;
        _sqsPublisher = sqsPublisher;
        _cache = cache;
        _orderPlacedQueueUrl = configuration["SQS:OrderPlacedQueueUrl"] ?? string.Empty;
    }

    public async Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = new Game(request.Title, request.Description, request.Price, request.Genre, request.Developer, request.ReleaseDate);
        await _gameRepository.AddAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        await _cache.RemoveAsync("games:all", cancellationToken);
        return MapToResponse(game);
    }

    public async Task<GameResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game == null)
        {
            throw new KeyNotFoundException("Game not found.");
        }
        return MapToResponse(game);
    }

    public async Task<IEnumerable<GameResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "games:all";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<IEnumerable<GameResponse>>(cached)!;
        }

        var games = await _gameRepository.GetAllAsync(cancellationToken);
        var result = games.Select(MapToResponse).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);

        return result;
    }

    public async Task<GameResponse> UpdateAsync(Guid id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game == null)
        {
            throw new KeyNotFoundException("Game not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title)) game.SetTitle(request.Title);
        if (!string.IsNullOrWhiteSpace(request.Description)) game.SetDescription(request.Description);
        if (request.Price.HasValue) game.SetPrice(request.Price.Value);
        if (request.Genre.HasValue) game.SetGenre(request.Genre.Value);
        if (!string.IsNullOrWhiteSpace(request.Developer)) game.SetDeveloper(request.Developer);

        await _gameRepository.UpdateAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        await _cache.RemoveAsync("games:all", cancellationToken);
        return MapToResponse(game);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(id, cancellationToken);
        if (game == null)
        {
            throw new KeyNotFoundException("Game not found.");
        }
        game.Deactivate();
        await _gameRepository.UpdateAsync(game, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<PurchaseOrderResponse> PurchaseAsync(Guid gameId, Guid userId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null)
        {
            throw new KeyNotFoundException("Game not found.");
        }

        if (!game.IsActive)
        {
            throw new InvalidOperationException("Game is not available for purchase.");
        }

        if (await _userGameRepository.ExistsAsync(userId, gameId, cancellationToken))
        {
            throw new InvalidOperationException("Game already in library.");
        }

        var orderId = Guid.NewGuid();

        if (!string.IsNullOrEmpty(_orderPlacedQueueUrl))
        {
            await _sqsPublisher.PublishAsync(_orderPlacedQueueUrl, new OrderPlacedEvent
            {
                OrderId = orderId,
                UserId = userId,
                GameId = gameId,
                Price = game.Price,
                PlacedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return new PurchaseOrderResponse(orderId, "Order placed. Payment is being processed.");
    }

    public async Task<IEnumerable<UserGameResponse>> GetUserLibraryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _userGameRepository.GetByUserIdAsync(userId, cancellationToken);
        return items.Select(ug => new UserGameResponse(
            ug.GameId,
            ug.Game?.Title ?? string.Empty,
            ug.Game?.Description ?? string.Empty,
            ug.Game?.Price ?? 0,
            ug.Game?.Genre ?? Catalog.Domain.Enums.GameGenre.Other,
            ug.Game?.Developer ?? string.Empty,
            ug.Game?.ReleaseDate ?? DateTime.MinValue,
            ug.AcquiredAt));
    }

    private static GameResponse MapToResponse(Game game) =>
        new(game.Id, game.Title, game.Description, game.Price, game.Genre, game.Developer, game.ReleaseDate, game.IsActive);
}
