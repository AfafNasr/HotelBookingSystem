using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.DeleteRoomImage;
using HotelBooking.Application.Rooms.UpdateRoomImage;
using HotelBooking.Application.Rooms.UploadRoomImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms;

[ApiController]
[Route("api/rooms/{roomId:int}/images")]
public sealed class RoomImagesController : ControllerBase
{
    private readonly UploadRoomImageCommandHandler _uploadHandler;
    private readonly UpdateRoomImageCommandHandler _updateHandler;
    private readonly DeleteRoomImageCommandHandler _deleteHandler;

    public RoomImagesController(
    UploadRoomImageCommandHandler uploadHandler,
    UpdateRoomImageCommandHandler updateHandler,
    DeleteRoomImageCommandHandler deleteHandler)
    {
        _uploadHandler = uploadHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
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

    [HttpPut("{imageId:int}")]
    [Authorize(Policy = RoomPermissions.UpdateImage)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
    int roomId,
    int imageId,
    [FromForm] UploadRoomImageRequest request,
    CancellationToken cancellationToken)
    {
        await using var stream =
            request.File.OpenReadStream();

        var command =
            new UpdateRoomImageCommand(
                roomId,
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
    [Authorize(Policy = RoomPermissions.DeleteImage)]
    public async Task<IActionResult> Delete(
    int roomId,
    int imageId,
    CancellationToken cancellationToken)
    {
        var result =
            await _deleteHandler.HandleAsync(
                new DeleteRoomImageCommand(
                    roomId,
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
