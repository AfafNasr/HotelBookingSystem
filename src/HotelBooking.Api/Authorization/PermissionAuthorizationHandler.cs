using HotelBooking.Application.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotelBooking.Api.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(
            System.Security.Claims.ClaimTypes.Role);

        var hasPermission = roles.Any(role =>
            RolePermissions.HasPermission(
                role.Value,
                requirement.Permission));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}