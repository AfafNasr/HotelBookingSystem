namespace HotelBooking.Application.Common.Security.Authorization.Permissions;

public static class HotelPermissions
{
    public const string Create = "Hotels.Create";
    public const string Update = "Hotels.Update";
    public const string Delete = "Hotels.Delete";
    public const string CompleteProfile = "Hotels.CompleteProfile";
    public const string UploadImage = "Hotels.UploadImage";
    public const string ManageAmenities = "Hotels.ManageAmenities";
    public const string GetAdminHotels = "Hotels.GetAdminHotels";
    public const string GetAdminHotelById ="Hotels.GetAdminHotelById";
    public const string DeleteAmenities= "Hotels.DeleteAmenities";


    public static readonly IReadOnlyCollection<string> All =
    [
        Create,
        Update,
        Delete,
        CompleteProfile,
        UploadImage,
        ManageAmenities,
        GetAdminHotels,
        GetAdminHotelById,
        DeleteAmenities
    ];
}