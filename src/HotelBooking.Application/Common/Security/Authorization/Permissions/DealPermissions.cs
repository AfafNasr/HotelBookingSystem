namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class DealPermissions
{
    public const string Create = "Deals.Create";

    public static readonly IReadOnlyCollection<string> All =
   [
       Create
        
   ];
}
