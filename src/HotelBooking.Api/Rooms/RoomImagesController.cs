using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.UploadRoomImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms;

[ApiController]
[Route("api/rooms/{roomId:int}/images")]
public sealed class RoomImagesController : ControllerBase
{
    private readonly UploadRoomImageCommandHandler _uploadHandler;

    public RoomImagesController(
        UploadRoomImageCommandHandler uploadHandler)
    {
        _uploadHandler = uploadHandler;
    }

    [HttpPost]
    [Authorize(Policy = RoomPermissions.UploadImage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int roomId,
        [FromForm] UploadRoomImageRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new UploadRoomImageCommand(
            roomId,
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

        var response = new UploadRoomImageResponse(
    result.ImageId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}
