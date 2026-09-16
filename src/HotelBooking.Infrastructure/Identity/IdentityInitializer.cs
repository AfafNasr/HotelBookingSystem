using HotelBooking.Application.Common.Security;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Identity;

public sealed class IdentityInitializer
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentityInitializer(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task InitializeRolesAsync()
    {
        await EnsureRoleExistsAsync(Roles.Customer);
        await EnsureRoleExistsAsync(Roles.Admin);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(
            new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Failed to create role '{roleName}': {errors}");
        }
    }
}