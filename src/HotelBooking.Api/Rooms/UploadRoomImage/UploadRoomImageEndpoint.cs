using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.UploadRoomImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.UploadRoomImage;

[ApiController]
[Route("api/rooms/{roomId:int}/images")]
public sealed class UploadRoomImageEndpoint : ControllerBase
{
    private readonly UploadRoomImageCommandHandler _handler;

    public UploadRoomImageEndpoint(
        UploadRoomImageCommandHandler handler)
    {
        _handler = handler;
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

        var result = await _handler.HandleAsync(
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