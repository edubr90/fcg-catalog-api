// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using NUnit.Framework;

namespace Catalog.UnitTests;

[TestFixture]
public class GameEntityTests
{
    private Game CreateGame() =>
        new("Test Game", "A great game", 59.99m, GameGenre.Action, "Dev studio", DateTime.UtcNow);

    [Test]
    public void Constructor_ShouldInitilizeDefaultValues()
    {
        var game = CreateGame();

        Assert.That(game.Title, Is.EqualTo("Test Game"));
        Assert.That(game.Price, Is.EqualTo(59.99m));
        Assert.That(game.IsActive, Is.True);
        Assert.That(game.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void SetTitle_Empty_ShouldThrow()
    {
        var game = CreateGame();
        Assert.Throws<ArgumentException>(() => game.SetTitle(string.Empty));
    }

    [Test]
    public void SetPrice_Zero_ShouldBeValid()
    {
        var game = CreateGame();
        game.SetPrice(0m);
        Assert.That(game.Price, Is.EqualTo(0m));
    }

    [Test]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var game = CreateGame();
        game.Deactivate();
        Assert.That(game.IsActive, Is.False);
    }

    [Test]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var game = CreateGame();
        game.Deactivate();
        game.Activate();
        Assert.That(game.IsActive, Is.True);
    }
}
