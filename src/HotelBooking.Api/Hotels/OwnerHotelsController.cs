using HotelBooking.Api.Common;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.CompleteHotelProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels;

[ApiController]
[Route("api/owner/hotels")]
public sealed class OwnerHotelsController : ControllerBase
{
    private readonly CompleteHotelProfileCommandHandler _completeProfileHandler;
    private readonly AddHotelAmenityCommandHandler _addAmenityHandler;

    public OwnerHotelsController(
    CompleteHotelProfileCommandHandler completeProfileHandler,
    AddHotelAmenityCommandHandler addAmenityHandler)
    {
        _completeProfileHandler = completeProfileHandler;
        _addAmenityHandler = addAmenityHandler;
    }

    [HttpPut("{hotelId:int}/profile")]
    [Authorize(Policy = HotelPermissions.CompleteProfile)]
    public async Task<IActionResult> CompleteProfile(
        int hotelId,
        CompleteHotelProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteHotelProfileCommand(
            hotelId,
            request.Description,
            request.Address,
            request.Latitude,
            request.Longitude);

        var result = await _completeProfileHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{hotelId:int}/amenities")]
    [Authorize(Policy = HotelPermissions.ManageAmenities)]
    public async Task<IActionResult> AddAmenity(
    int hotelId,
    AddHotelAmenityRequest request,
    CancellationToken cancellationToken)
    {
        var command = new AddHotelAmenityCommand(
            hotelId,
            request.AmenityId);

        var result = await _addAmenityHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }


}