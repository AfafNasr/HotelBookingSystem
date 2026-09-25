namespace HotelBooking.Application.Hotels.GetAdminHotels;

public sealed record AdminHotel(
    int Id,
    string Name,
    int StarRating,
    string Owner,
    int NumberOfRooms,
    DateTime CreatedAt,
    DateTime? UpdatedAt);