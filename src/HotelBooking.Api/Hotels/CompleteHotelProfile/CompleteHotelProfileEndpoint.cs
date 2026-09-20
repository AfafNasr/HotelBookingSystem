using HotelBooking.Api.Common;
using HotelBooking.Application.Authorization;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.CompleteHotelProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.CompleteHotelProfile;


    [ApiController]
    [Route("api/owner/hotels")]
    public sealed class CompleteHotelProfileEndpoint : ControllerBase
    {
        private readonly CompleteHotelProfileCommandHandler _handler;

        public CompleteHotelProfileEndpoint(
            CompleteHotelProfileCommandHandler handler)
        {
            _handler = handler;
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

            var result = await _handler.HandleAsync(
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

