using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Amenities;

public static class AmenityErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Amenity.NotFound",
            "The amenity was not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError AlreadyExists =
        new(
            "Amenity.AlreadyExists",
            "An amenity with the specified name already exists.",
            ErrorType.Conflict);

    public static readonly ApplicationError InvalidId =
        new(
            "Amenity.InvalidId",
            "The amenity ID must be greater than zero.",
            ErrorType.Validation);
}