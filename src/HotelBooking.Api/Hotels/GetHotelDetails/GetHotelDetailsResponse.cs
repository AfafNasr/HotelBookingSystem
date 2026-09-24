using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Hotels.GetHotelDetails;

public sealed record GetHotelDetailsResponse(
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
    IReadOnlyCollection<HotelReviewResponse> RecentReviews,
    IReadOnlyCollection<AvailableRoomResponse> AvailableRooms);

public sealed record HotelReviewResponse(
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public sealed record AvailableRoomResponse(
    RoomType RoomType,
    int AvailableCount);