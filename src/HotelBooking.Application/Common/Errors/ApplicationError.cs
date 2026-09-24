namespace HotelBooking.Application.Common.Errors;

public sealed record ApplicationError(
    string Code,
    string Description,
    ErrorType Type);