using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed record HotelDetails(
    int Id,
    string Name,
    string CityName,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? AverageGuestRating,
    int ReviewCount,
    IReadOnlyCollection<HotelDetailsReview> RecentReviews,
    IReadOnlyCollection<AvailableRoomSummary> AvailableRooms);

public sealed record HotelDetailsReview(
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public sealed record AvailableRoomSummary(
    RoomType RoomType,
    int AvailableCount);