// "// Copyright (c) FIAP Cloud Games. All rights reserved."

using Catalog.Domain.Enums;

namespace Catalog.Application.DTOs;

public record CreateGameRequest(
    string Title,
    string Description,
    decimal Price,
    GameGenre Genre,
    string Developer,
    DateTime ReleaseDate
    );

public record UpdateGameRequest(
    string? Title,
    string? Description,
    decimal? Price,
    GameGenre? Genre,
    string? Developer
    );

public record GameResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    GameGenre Genre,
    string Developer,
    DateTime ReleaseDate,
    bool IsActive
    );

public record UserGameResponse(
    Guid GameId,
    string Title,
    string Description,
    decimal Price,
    GameGenre Genre,
    string Developer,
    DateTime ReleaseDate,
    DateTime AcquireAt
    );

public record PurchaseOrderResponse(Guid OrderId, string Message);

