namespace HotelBooking.Application.Common.Models;

public sealed record CreateCustomerResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyCollection<ApplicationError> Errors);