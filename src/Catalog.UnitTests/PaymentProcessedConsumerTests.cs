using Catalog.Application.Consumers;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using FCG.Shared.Events;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Catalog.UnitTests;

[TestFixture]
public class PaymentProcessedConsumerTests
{
    private Mock<IUserGameRepository> _userGameRepoMock = null!;
    private Mock<IUnitOfWork> _uowMock = null!;
    private Mock<ILogger<PaymentProcessedConsumer>> _loggerMock = null!;
    private PaymentProcessedConsumer _consumer = null!;

    [SetUp]
    public void Setup()
    {
        _userGameRepoMock = new Mock<IUserGameRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PaymentProcessedConsumer>>();
        _consumer = new PaymentProcessedConsumer(
            _userGameRepoMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ConsumeAsync_Approved_ShouldAddGameToLibrary()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved,
            ProcessedAt = DateTime.UtcNow
        };

        _userGameRepoMock.Setup(r => r.ExistsAsync(evt.UserId, evt.GameId, default)).ReturnsAsync(false);

        await _consumer.ConsumeAsync(evt);

        _userGameRepoMock.Verify(r => r.AddAsync(
            It.Is<UserGame>(ug => ug.UserId == evt.UserId && ug.GameId == evt.GameId), default), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Test]
    public async Task ConsumeAsync_Rejected_ShouldNotAddGameToLibrary()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Rejected,
            ProcessedAt = DateTime.UtcNow
        };

        await _consumer.ConsumeAsync(evt);

        _userGameRepoMock.Verify(r => r.AddAsync(It.IsAny<UserGame>(), default), Times.Never);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Never);
    }

    [Test]
    public async Task ConsumeAsync_Approved_AlreadyOwned_ShouldBeIdempotent()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved,
            ProcessedAt = DateTime.UtcNow
        };

        _userGameRepoMock.Setup(r => r.ExistsAsync(evt.UserId, evt.GameId, default)).ReturnsAsync(true);

        await _consumer.ConsumeAsync(evt);

        _userGameRepoMock.Verify(r => r.AddAsync(It.IsAny<UserGame>(), default), Times.Never);
    }
}
