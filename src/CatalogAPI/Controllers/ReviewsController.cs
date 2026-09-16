using System.Security.Claims;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers;

[ApiController]
[Route("api/games/{gameId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IGameReviewRepository _reviewRepo;

    public ReviewsController(IGameReviewRepository reviewRepo)
    {
        _reviewRepo = reviewRepo;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview(Guid gameId, [FromBody] AddReviewRequest request, CancellationToken ct)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest("Rating must be between 1 and 5.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var review = new GameReview
        {
            GameId = gameId,
            UserId = userId,
            UserName = request.UserName,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddReviewAsync(review, ct);
        return StatusCode(201, review);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(Guid gameId, CancellationToken ct)
    {
        var reviews = await _reviewRepo.GetReviewsByGameAsync(gameId, ct);
        return Ok(reviews);
    }
}
