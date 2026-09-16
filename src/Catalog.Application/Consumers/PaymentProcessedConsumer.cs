using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using FCG.Shared.Events;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Consumers;

public class PaymentProcessedConsumer
{
    private readonly IUserGameRepository _userGameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(
        IUserGameRepository userGameRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentProcessedConsumer> logger)
    {
        _userGameRepository = userGameRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ConsumeAsync(PaymentProcessedEvent evt, CancellationToken ct = default)
    {
        if (evt.Status != PaymentStatus.Approved)
        {
            _logger.LogInformation(
                "[CATALOG] Payment rejected for OrderId: {OrderId} - game NOT added to library.",
                evt.OrderId);
            return;
        }

        if (await _userGameRepository.ExistsAsync(evt.UserId, evt.GameId, ct))
        {
            _logger.LogWarning(
                "[CATALOG] Idempotency check - game {GameId} already in library for UserId: {UserId}. Skipping.",
                evt.GameId, evt.UserId);
            return;
        }

        var userGame = new UserGame(evt.UserId, evt.GameId);
        await _userGameRepository.AddAsync(userGame, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation(
            "[CATALOG] Game {GameId} added to library for UserId: {UserId} (OrderId: {OrderId})",
            evt.GameId, evt.UserId, evt.OrderId);
    }
}
