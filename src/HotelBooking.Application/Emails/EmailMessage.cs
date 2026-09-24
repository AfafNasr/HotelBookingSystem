namespace HotelBooking.Application.Emails;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody);