using HotelBooking.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Identity;

public sealed class IdentityInitializer
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly InitialAdminOptions _adminOptions;

    public IdentityInitializer(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IOptions<InitialAdminOptions> adminOptions)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminOptions = adminOptions.Value;
    }

    public async Task InitializeAsync()
    {
        await EnsureRoleExistsAsync(Roles.Customer);
        await EnsureRoleExistsAsync(Roles.Admin);
        await EnsureInitialAdminExistsAsync();
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
            throw CreateIdentityException(
                $"Failed to create role '{roleName}'.",
                result);
        }
    }

    private async Task EnsureInitialAdminExistsAsync()
    {
        var admin = await _userManager.FindByNameAsync(
            _adminOptions.Username);

        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = _adminOptions.Username,
                Email = _adminOptions.Email
            };

            var createResult = await _userManager.CreateAsync(
                admin,
                _adminOptions.Password);

            if (!createResult.Succeeded)
            {
                throw CreateIdentityException(
                    "Failed to create the initial admin user.",
                    createResult);
            }
        }

        // Prevent an existing account with the configured username
        // but a different email from being promoted to Admin.

        if (!string.Equals(
        admin.Email,
        _adminOptions.Email,
        StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The configured initial admin username already exists with a different email address.");
        }

        if (!await _userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            var roleResult = await _userManager.AddToRoleAsync(
                admin,
                Roles.Admin);

            if (!roleResult.Succeeded)
            {
                throw CreateIdentityException(
                    "Failed to assign the Admin role to the initial admin user.",
                    roleResult);
            }
        }
    }

    private static InvalidOperationException CreateIdentityException(
        string message,
        IdentityResult result)
    {
        var errors = string.Join(
            ", ",
            result.Errors.Select(error => error.Description));

        return new InvalidOperationException(
            $"{message} {errors}");
    }
}