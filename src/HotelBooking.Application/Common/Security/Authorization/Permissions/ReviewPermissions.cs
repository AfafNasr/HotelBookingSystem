using HotelBooking.Application.Cities.UpdateCity;
using System.ComponentModel.Design;

namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class ReviewPermissions
{
    public const string Create = "Reviews.Create";
    public const string View = "Reviews.View";
    public const string Update = "Reviews.Update";
    public const string Delete = "Reviews.Delete";

    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        View,
        Update,
        Delete
    ];
}