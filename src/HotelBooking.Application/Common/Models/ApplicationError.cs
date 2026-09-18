namespace HotelBooking.Application.Common.Models;

public sealed record ApplicationError(
    string Code,
    string Description,
    ErrorType Type);