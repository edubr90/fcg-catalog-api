// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using FCG.Shared.Events;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace Catalog.UnitTests;
public class GameServiceTests
{
    private Mock<IGameRepository> _gameRepoMock = null;
    private Mock<IUserGameRepository> _userGameRepoMock = null;
    private Mock<IUnitOfWork> _uowMock = null;
    private Mock<IPublishEndpoint> _publishMock = null;
    private GameService _service = null;

    [SetUp]
    public void SetUp()
    {
        _gameRepoMock = new Mock<IGameRepository>();
        _userGameRepoMock = new Mock<IUserGameRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _publishMock = new Mock<IPublishEndpoint>();
        _service = new GameService(_gameRepoMock.Object, _userGameRepoMock.Object, _uowMock.Object, _publishMock.Object);
    }

    [Test]
    public async Task CreateAsync_ShouldAddAndReturnGame()
    {
        var request = new CreateGameRequest("New game", "Desc", 49.99m, Domain.Enums.GameGenre.RPG, "Studio", DateTime.UtcNow);

        var result = await _service.CreateAsync(request);

        _gameRepoMock.Verify(r => r.AddAsync(It.IsAny<Game>(), default), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Once);
        Assert.That(result.Title, Is.EqualTo("New game"));
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
        var game = new Game("New game", "Desc", 49.99m, Domain.Enums.GameGenre.RPG, "Studio", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);
        _userGameRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), game.Id, default)).ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.PurchaseAsync(game.Id, Guid.NewGuid()));
    }

    [Test]
    public async Task PurchaseAsync_Valid_ShouldPublishOrderPlacedEvent()
    {
        var game = new Game("Game", "Desc", 99m, Domain.Enums.GameGenre.Action, "Dev", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);
        _userGameRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), game.Id, default)).ReturnsAsync(false);
        _publishMock.Setup(p => p.Publish(It.IsAny<OrderPlacedEvent>(), default)).Returns(Task.CompletedTask);

        var userId = Guid.NewGuid();
        var result = await _service.PurchaseAsync(game.Id, userId);

        _publishMock.Verify(p =>
            p.Publish(It.Is<OrderPlacedEvent>(e =>
                e.GameId == game.Id && e.UserId == userId && e.Price == 99m), default),
                Times.Once);

        Assert.That(result.OrderId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task DeleteAsync_ShouldDeactivateGame()
    {
        var game = new Game("Game", "Desc", 10m, Domain.Enums.GameGenre.Action, "Dev", DateTime.UtcNow);
        _gameRepoMock.Setup(r => r.GetByIdAsync(game.Id, default)).ReturnsAsync(game);

        await _service.DeleteAsync(game.Id);

        Assert.That(game.IsActive, Is.False);
        _uowMock.Verify(u => u.CommitAsync(default), Times.Once);
    }
}
