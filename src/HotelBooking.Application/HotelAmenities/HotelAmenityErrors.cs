using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.HotelAmenities;

public static class HotelAmenityErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "HotelAmenity.NotFound",
            "The amenity is not assigned to this hotel.",
            ErrorType.NotFound);

    public static readonly ApplicationError AlreadyExists =
        new(
            "HotelAmenity.AlreadyExists",
            "The amenity is already assigned to this hotel.",
            ErrorType.Conflict);
}