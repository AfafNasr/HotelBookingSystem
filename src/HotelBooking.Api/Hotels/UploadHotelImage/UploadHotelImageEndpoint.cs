using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.UploadHotelImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.UploadHotelImage;

[ApiController]
[Route("api/hotels/{hotelId:int}/images")]
public sealed class UploadHotelImageEndpoint : ControllerBase
{
    private readonly UploadHotelImageCommandHandler _handler;

    public UploadHotelImageEndpoint(
        UploadHotelImageCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = HotelPermissions.UploadImage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int hotelId,
        [FromForm] UploadHotelImageRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new UploadHotelImageCommand(
            hotelId,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new UploadHotelImageResponse(
     result.ImageId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}

public sealed record UploadHotelImageRequest(
    IFormFile File);

public sealed record UploadHotelImageResponse(
    int ImageId);