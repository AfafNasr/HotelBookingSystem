using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Users.ActivateUser;
using HotelBooking.Application.Users.CreateUser;
using HotelBooking.Application.Users.DeactivateUser;
using HotelBooking.Application.Users.GetAllUsers;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using HotelBooking.Application.Users.UpdateUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Users;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly PromoteToHotelOwnerCommandHandler _promoteHandler;
    private readonly GetAllUsersQueryHandler _getAllHandler;
    private readonly CreateUserCommandHandler _createHandler;
    private readonly UpdateUserCommandHandler _updateHandler;
    private readonly DeactivateUserCommandHandler _deactivateHandler;
    private readonly ActivateUserCommandHandler _activateHandler;

    public AdminUsersController(
          PromoteToHotelOwnerCommandHandler promoteHandler,
          GetAllUsersQueryHandler getAllHandler,
          CreateUserCommandHandler createHandler,
          UpdateUserCommandHandler updateHandler,
          DeactivateUserCommandHandler deactivateHandler,
          ActivateUserCommandHandler activateHandler)
    {
        _promoteHandler = promoteHandler;
        _getAllHandler = getAllHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
        _activateHandler = activateHandler;
    }

    [HttpPost("{userId}/hotel-owner-role")]
    [Authorize(Policy = UserPermissions.PromoteToHotelOwner)]
    public async Task<IActionResult> PromoteToHotelOwner(
        string userId , CancellationToken cancellationToken)
    {
        var command = new PromoteToHotelOwnerCommand(userId);

        var result =
            await _promoteHandler.HandleAsync(command, cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = UserPermissions.View)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var users =
            await _getAllHandler.HandleAsync(
                cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    [Authorize(Policy = UserPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _createHandler.HandleAsync(
                new CreateUserCommand(
                    request.UserName,
                    request.Email,
                    request.Password),
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateUserResponse(
                result.UserId!));
    }

    [HttpPut("{userId}")]
    [Authorize(Policy = UserPermissions.Update)]
    public async Task<IActionResult> Update(
    string userId,
    UpdateUserRequest request,
    CancellationToken cancellationToken)
    {
        var result =
            await _updateHandler.HandleAsync(
                new UpdateUserCommand(
                    userId,
                    request.UserName,
                    request.Email),
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{userId}/deactivate")]
    [Authorize(Policy = UserPermissions.Deactivate)]
    public async Task<IActionResult> Deactivate(
    string userId,
    CancellationToken cancellationToken)
    {
        var result =
            await _deactivateHandler.HandleAsync(
                new DeactivateUserCommand(userId),
                cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{userId}/activate")]
    [Authorize(Policy = UserPermissions.Activate)]
    public async Task<IActionResult> Activate(
        string userId,
        CancellationToken cancellationToken)
    {
        var result =
            await _activateHandler.HandleAsync(
                new ActivateUserCommand(userId),
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