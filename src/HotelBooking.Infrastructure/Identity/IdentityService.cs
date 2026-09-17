using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Security;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

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

    public async Task<CreateCustomerResult> CreateCustomerAsync(
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

        return new CreateCustomerResult(
            true,
            user.Id,
            Array.Empty<ApplicationError>());
    }

    private static CreateCustomerResult Failure(IdentityResult result)
    {
        var errors = result.Errors
            .Select(error => new ApplicationError(
                error.Code,
                error.Description,
                GetErrorType(error.Code)))
            .ToArray();

        return new CreateCustomerResult(
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
}