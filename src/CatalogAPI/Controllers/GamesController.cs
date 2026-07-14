// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using System.Security.Claims;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers;
public class GamesController : Controller
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService) => _gameService = gameService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GameResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var games = await _gameService.GetAllAsync(cancellationToken);
        return Ok(games);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id,  CancellationToken cancellationToken)
    {
        var game = await _gameService.GetByIdAsync(id, cancellationToken);
        return Ok(game);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        var game = await _gameService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = game.Id }, game);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGameRequest request, CancellationToken cancellationToken)
    {
        var game = await _gameService.UpdateAsync(id, request, cancellationToken);
        return Ok(game);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _gameService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/purchase")]
    [Authorize]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Purchase(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _gameService.PurchaseAsync(id, userId, cancellationToken);
        return Accepted(result);
    }

    [HttpGet("library/{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserGameResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLibrary(Guid userId, CancellationToken cancellationToken)
    {
        var library = await _gameService.GetUserLibraryAsync(userId, cancellationToken);
        return Ok(library);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (claim == null || !Guid.TryParse(claim.Value, out var id))
            throw new InvalidOperationException("User ID claim is missing or invalid.");

        return id;
    }
}
