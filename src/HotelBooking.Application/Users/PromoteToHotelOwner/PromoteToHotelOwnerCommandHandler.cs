namespace HotelBooking.Application.Users.PromoteToHotelOwner;

public sealed record PromoteToHotelOwnerCommandHandler(
    string UserId);