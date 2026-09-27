using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.DeleteHotelImage;
using HotelBooking.Application.Hotels.UpdateHotelImage;
using HotelBooking.Application.Hotels.UploadHotelImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels;


[ApiController]
[Route("api/hotels/{hotelId:int}/images")]
public class HotelImagesController : ControllerBase
{

    private readonly UploadHotelImageCommandHandler _uploadHandler;
    private readonly UpdateHotelImageCommandHandler _updateHandler;
    private readonly DeleteHotelImageCommandHandler _deleteHandler;


    public HotelImagesController(
        UploadHotelImageCommandHandler uploadHandler,
        UpdateHotelImageCommandHandler updateHandler,
        DeleteHotelImageCommandHandler deleteHandler)
    {
        _uploadHandler = uploadHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
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

    [HttpPut("{imageId:int}")]
    [Authorize(Policy = HotelPermissions.UpdateImage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
    int hotelId,
    int imageId,
    [FromForm] UploadHotelImageRequest request,
    CancellationToken cancellationToken)
    {
        await using var stream =
            request.File.OpenReadStream();

        var command =
            new UpdateHotelImageCommand(
                hotelId,
                imageId,
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length);

        var result =
            await _updateHandler.HandleAsync(
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

    [HttpDelete("{imageId:int}")]
    [Authorize(Policy = HotelPermissions.DeleteImage)]
    public async Task<IActionResult> Delete(
    int hotelId,
    int imageId,
    CancellationToken cancellationToken)
    {
        var result =
            await _deleteHandler.HandleAsync(
                new DeleteHotelImageCommand(
                    hotelId,
                    imageId),
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

