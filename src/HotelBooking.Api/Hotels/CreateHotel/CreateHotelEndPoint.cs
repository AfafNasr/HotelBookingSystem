using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.CreateHotel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.CreateHotel;

[ApiController]
[Route("api/admin/hotels")]
public sealed class CreateHotelEndpoint : ControllerBase
{
    private readonly CreateHotelCommandHandler _handler;

    public CreateHotelEndpoint(CreateHotelCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = HotelPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateHotelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateHotelCommand(
            request.Name,
            request.CityId,
            request.OwnerId,
            request.StarRating,
            request.Category);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new CreateHotelResponse(
            result.HotelId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}