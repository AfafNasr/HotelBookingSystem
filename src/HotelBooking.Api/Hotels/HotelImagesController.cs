using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.UploadHotelImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels;


[ApiController]
[Route("api/hotels/{hotelId:int}/images")]
public class HotelImagesController : ControllerBase
{

    private readonly UploadHotelImageCommandHandler _uploadHandler;

    public HotelImagesController(
        UploadHotelImageCommandHandler uploadHandler)
    {
        _uploadHandler = uploadHandler;
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

        var result = await _uploadHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new UploadHotelImageResponse(
                result.ImageId!.Value));
    }
}

