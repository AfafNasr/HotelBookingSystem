using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Hotels;


// Admin hotel management

public sealed record CreateHotelRequest(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category);

public sealed record CreateHotelResponse(
    int HotelId);

public sealed record UpdateHotelRequest(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);

// Public hotel search

public sealed record SearchHotelsRequest(
    string Destination,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? StarRating = null,
    HotelCategory? Category = null,
    IReadOnlyCollection<int>? AmenityIds = null,
    HotelSearchSort SortBy = HotelSearchSort.Name,
    int Page = 1,
    int PageSize = 20);

public sealed record SearchHotelsResponse(
    IReadOnlyCollection<SearchHotelResponse> Hotels,
    int Page,
    int PageSize,
    bool HasNextPage);

public sealed record SearchHotelResponse(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    string? Description,
    decimal StartingPricePerNight,
    string? ThumbnailStorageKey);

// Public hotel details

public sealed record GetHotelDetailsRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms);

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

public sealed record UploadHotelImageRequest(
    IFormFile File);

public sealed record UploadHotelImageResponse(
    int ImageId);

public sealed record CompleteHotelProfileRequest(
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);

public sealed record AddHotelAmenityRequest(
    int AmenityId);

public sealed record NearbyAttractionResponse(
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    int? DistanceMeters);