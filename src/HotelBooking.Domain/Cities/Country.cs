namespace HotelBooking.Domain.Cities;

public class Country
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    private Country()
    {
    }

    public Country(string code, string name)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}