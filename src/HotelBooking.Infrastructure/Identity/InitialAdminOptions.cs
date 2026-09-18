namespace HotelBooking.Infrastructure.Identity;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}