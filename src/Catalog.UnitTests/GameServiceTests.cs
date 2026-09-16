using Catalog.Application.Interfaces;
using Catalog.Application.Services;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Catalog.UnitTests;

[TestFixture]
public class GameServiceTests
{
    private Mock<IGameRepository> _gameRepoMock = null!;
    private Mock<IUserGameRepository> _userGameRepoMock = null!;
    private Mock<IUnitOfWork> _uowMock = null!;
    private Mock<ISqsPublisher> _sqsMock = null!;
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private GameService _service = null!;

    [SetUp]
    public void Setup()
    {
        _gameRepoMock = new Mock<IGameRepository>();
        _userGameRepoMock = new Mock<IUserGameRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _sqsMock = new Mock<ISqsPublisher>();
        _cacheMock = new Mock<IDistributedCache>();
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["SQS:OrderPlacedQueueUrl"]).Returns("https://sqs.sa-east-1.amazonaws.com/123/fcg-order-pla");
        _service = new GameService(
            _gameRepoMock.Object, _userGameRepoMock.Object, _uowMock.Object,
            _sqsMock.Object, _cacheMock.Object, _configMock.Object);
    }

    [Test]
    public async Task CreateAsync_ShouldAddAndReturnGame()
    {
        var request = new CreateGameRequest("New Game", "Desc", 49.99m, GameGenre.RPG, "Studio", DateTime.UtcNow);

        var result = await _service.CreateAsync(request);

        _gameRepoMock.Verify(r => r.AddAsync(It.IsAny<Game>(), default), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Once);
        Assert.That(result.Title, Is.EqualTo("New Game"));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ShouldThrow()
    {
        _gameRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Game?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task PurchaseAsync_GameNotFound_ShouldThrow()
    {
        _gameRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Game?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.PurchaseAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task PurchaseAsync_GameAlreadyOwned_ShouldThrow()
    {
        var game = new Game("Game", "Desc", 10m, GameGenre.Action, "Dev", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);
        _userGameRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), game.Id, default)).ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.PurchaseAsync(game.Id, Guid.NewGuid()));
    }

    [Test]
    public async Task PurchaseAsync_Valid_ShouldPublishOrderPlacedEvent()
    {
        var game = new Game("Game", "Desc", 99m, GameGenre.Action, "Dev", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);
        _userGameRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), game.Id, default)).ReturnsAsync(false);
        _sqsMock.Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), default)).Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var result = await _service.PurchaseAsync(game.Id, userId);

        _sqsMock.Verify(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), default), Times.Once);
        Assert.That(result.OrderId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task DeleteAsync_ShouldDeactivateGame()
    {
        var game = new Game("Game", "Desc", 10m, GameGenre.Action, "Dev", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);

        await _service.DeleteAsync(game.Id);

        Assert.That(game.IsActive, Is.False);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Once);
    }
}
