using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Security.Authorization;
using HotelBooking.Application.Common.Security.Authorization.Permissions;

namespace HotelBooking.UnitTests.Authorization;

public sealed class RolePermissionsTests
{
    [Fact]
    public void HasPermission_WhenAdminHasPermission_ReturnsTrue()
    {
        var result = RolePermissions.HasPermission(
            Roles.Admin,
            HotelPermissions.Delete);

        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WhenCustomerDoesNotHavePermission_ReturnsFalse()
    {
        var result = RolePermissions.HasPermission(
            Roles.Customer,
            HotelPermissions.Delete);

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WhenRoleDoesNotExist_ReturnsFalse()
    {
        var result = RolePermissions.HasPermission(
            "UnknownRole",
            HotelPermissions.Delete);

        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WhenPermissionDoesNotExist_ReturnsFalse()
    {
        var result = RolePermissions.HasPermission(
            Roles.Admin,
            "Hotels.Unknown");

        Assert.False(result);
    }
}
