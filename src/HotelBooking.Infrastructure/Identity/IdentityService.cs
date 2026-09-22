using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Security;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using HotelBooking.Application.Authentication;

namespace HotelBooking.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<RegisterResult> CreateCustomerAsync(
     string username,
     string email,
     string password)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        var user = new IdentityUser
        {
            UserName = username,
            Email = email
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return Failure(createResult);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            Roles.Customer);

        if (!roleResult.Succeeded)
        {
            return Failure(roleResult);
        }

        await transaction.CommitAsync();

        return new RegisterResult(
            true,
            user.Id,
            Array.Empty<ApplicationError>());
    }

    private static RegisterResult Failure(IdentityResult result)
    {
        var errors = result.Errors
            .Select(error => new ApplicationError(
                error.Code,
                error.Description,
                GetErrorType(error.Code)))
            .ToArray();

        return new RegisterResult(
            false,
            null,
            errors);
    }
    private static ErrorType GetErrorType(string errorCode)
    {
        return errorCode switch
        {
            "DuplicateUserName" => ErrorType.Conflict,
            "DuplicateEmail" => ErrorType.Conflict,
            _ => ErrorType.Validation
        };
    }
    public async Task<AuthenticatedUser?> AuthenticateAsync(
    string username,
    string password)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user is null)
        {
            return null;
        }

        var isPasswordValid =
            await _userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthenticatedUser(
            user.Id,
            user.UserName!,
            roles.ToArray());
    }

    public async Task<PromoteToHotelOwnerResult> PromoteToHotelOwnerAsync(
    string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return new PromoteToHotelOwnerResult(
                false,
                [
                    new ApplicationError(
                    "User.NotFound",
                    "The specified user was not found.",
                    ErrorType.NotFound)
                ]);
        }

        var isCustomer = await _userManager.IsInRoleAsync(
            user,
            Roles.Customer);

        if (!isCustomer)
        {
            return new PromoteToHotelOwnerResult(
                false,
                [
                    new ApplicationError(
                    "User.NotCustomer",
                    "Only customer accounts can be promoted to hotel owner.",
                    ErrorType.Validation)
                ]);
        }

        var isAlreadyHotelOwner = await _userManager.IsInRoleAsync(
            user,
            Roles.HotelOwner);

        if (isAlreadyHotelOwner)
        {
            return new PromoteToHotelOwnerResult(
                false,
                [
                    new ApplicationError(
                    "User.AlreadyHotelOwner",
                    "The user is already a hotel owner.",
                    ErrorType.Conflict)
                ]);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            Roles.HotelOwner);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
             ", ",
        roleResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Failed to assign the HotelOwner role to user '{user.Id}'. {errors}");
        }

        return new PromoteToHotelOwnerResult(
            true,
            Array.Empty<ApplicationError>());
    }

    public async Task<bool> IsUserInRoleAsync(
    string userId,
    string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, role);
    }

}