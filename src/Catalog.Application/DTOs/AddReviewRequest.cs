namespace Catalog.Application.DTOs;

public record AddReviewRequest(
    string UserName,
    int Rating,
    string Comment
);
