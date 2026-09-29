namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class DealPermissions
{
    public const string Create = "Deals.Create";
    public const string Update = "Deals.Update";
    public const string Delete = "Deals.Delete";


    public static readonly IReadOnlyCollection<string> All =
   [
       Create,
         Update,
            Delete

   ];
}
