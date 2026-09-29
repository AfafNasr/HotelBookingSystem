using HotelBooking.Application.Authentication;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Users.ActivateUser;
using HotelBooking.Application.Users.DeactivateUser;
using HotelBooking.Application.Users.GetAllUsers;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using HotelBooking.Application.Users.UpdateUser;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


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
    string password,
    CancellationToken cancellationToken)
    {
        var executionStrategy =
            _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            var user = new IdentityUser
            {
                UserName = username,
                Email = email
            };

            var createResult =
                await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(createResult);
            }

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    Roles.Customer);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(roleResult);
            }

            await transaction.CommitAsync(cancellationToken);

            return new RegisterResult(
                true,
                user.Id,
                Array.Empty<ApplicationError>());
        });
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

    public async Task<IReadOnlyCollection<AdminUserItem>> GetAllUsersAsync(
    CancellationToken cancellationToken)
    {
        var users =
            await _dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderBy(user => user.UserName)
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.Email,

                    DeactivatedAt =
                        EF.Property<DateTime?>(
                            user,
                            "DeactivatedAt")
                })
                .ToListAsync(cancellationToken);

        var userRoles =
            await (
                from userRole in _dbContext.UserRoles
                join role in _dbContext.Roles
                    on userRole.RoleId equals role.Id
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        var rolesByUserId =
            userRoles
                .GroupBy(item => item.UserId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        (IReadOnlyCollection<string>)group
                            .Where(item =>
                                !string.IsNullOrWhiteSpace(
                                    item.RoleName))
                            .Select(item => item.RoleName!)
                            .ToArray());

        return users
            .Select(user =>
                new AdminUserItem(
                    user.Id,
                    user.UserName ?? string.Empty,
                    user.Email ?? string.Empty,
                    rolesByUserId.TryGetValue(
                        user.Id,
                        out var roles)
                        ? roles
                        : Array.Empty<string>(),
                    user.DeactivatedAt is null,
                    user.DeactivatedAt))
            .ToArray();
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
    string userId , CancellationToken cancellationToken)
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

    public async Task<UpdateUserResult> UpdateUserAsync(
    string userId,
    string userName,
    string email,
    CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return new UpdateUserResult(
                false,
                [
                    new ApplicationError(
                    "User.NotFound",
                    "The specified user was not found.",
                    ErrorType.NotFound)
                ]);
        }

        user.UserName =
            userName.Trim();

        user.Email =
            email.Trim();

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors =
                result.Errors
                    .Select(error =>
                        new ApplicationError(
                            error.Code,
                            error.Description,
                            GetErrorType(error.Code)))
                    .ToArray();

            return new UpdateUserResult(
                false,
                errors);
        }

        return new UpdateUserResult(
            true,
            []);
    }

    public async Task<DeactivateUserResult> DeactivateUserAsync(
     string userId,
     DateTime deactivatedAt,
     CancellationToken cancellationToken)
    {
        var user =
            await _dbContext.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    user => user.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return new DeactivateUserResult(
                false,
                [
                    new ApplicationError(
                    "User.NotFound",
                    "The specified user was not found.",
                    ErrorType.NotFound)
                ]);
        }

        var deactivatedAtProperty =
            _dbContext.Entry(user)
                .Property<DateTime?>(
                    "DeactivatedAt");

        if (deactivatedAtProperty.CurrentValue is not null)
        {
            // Already inactive - idempotent success.
            return new DeactivateUserResult(
                true,
                []);
        }

        deactivatedAtProperty.CurrentValue =
            deactivatedAt;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new DeactivateUserResult(
            true,
            []);
    }

    public async Task<ActivateUserResult> ActivateUserAsync(
    string userId,
    CancellationToken cancellationToken)
    {
        var user =
            await _dbContext.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    user => user.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return new ActivateUserResult(
                false,
                [
                    new ApplicationError(
                    "User.NotFound",
                    "The specified user was not found.",
                    ErrorType.NotFound)
                ]);
        }

        var deactivatedAtProperty =
            _dbContext.Entry(user)
                .Property<DateTime?>(
                    "DeactivatedAt");

        if (deactivatedAtProperty.CurrentValue is null)
        {
            // Already active - idempotent success.
            return new ActivateUserResult(
                true,
                []);
        }

        deactivatedAtProperty.CurrentValue =
            null;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new ActivateUserResult(
            true,
            []);
    }

}