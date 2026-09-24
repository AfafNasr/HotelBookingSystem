using HotelBooking.Application.Bookings.GetBookingConfirmation;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelBooking.Infrastructure.Documents;

public sealed class BookingConfirmationPdfGenerator
    : IBookingConfirmationPdfGenerator
{
    public byte[] Generate(BookingConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Header()
                    .Element(header => ComposeHeader(header, confirmation));

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        column.Item()
                            .Element(container =>
                                ComposeBookingDetails(container, confirmation));

                        column.Item()
                            .Element(container =>
                                ComposeStayDetails(container, confirmation));

                        column.Item()
                            .Element(container =>
                                ComposeRooms(container, confirmation));

                        column.Item()
                            .Element(container =>
                                ComposePaymentSummary(container, confirmation));
                    });

                page.Footer()
                    .Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        BookingConfirmation confirmation)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item()
                .Text("Booking Confirmation")
                .FontSize(24)
                .Bold();

            column.Item()
                .Text("BOOKING CONFIRMED")
                .FontSize(11)
                .Bold();

            column.Item()
                .Text(text =>
                {
                    text.Span("Confirmation Number: ").SemiBold();
                    text.Span(confirmation.ConfirmationNumber ?? "-");
                });
        });
    }

    private static void ComposeBookingDetails(
        IContainer container,
        BookingConfirmation confirmation)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item()
                .Text("Booking Details")
                .FontSize(14)
                .Bold();

            column.Item().LineHorizontal(1);

            column.Item().Text(text =>
            {
                text.Span("Hotel: ").SemiBold();
                text.Span(confirmation.HotelName);
            });

            if (!string.IsNullOrWhiteSpace(confirmation.HotelAddress))
            {
                column.Item().Text(text =>
                {
                    text.Span("Address: ").SemiBold();
                    text.Span(confirmation.HotelAddress);
                });
            }

            column.Item().Text(text =>
            {
                text.Span("Guest: ").SemiBold();
                text.Span(confirmation.GuestFullName);
            });

            column.Item().Text(text =>
            {
                text.Span("Email: ").SemiBold();
                text.Span(confirmation.GuestEmail);
            });
        });
    }

    private static void ComposeStayDetails(
        IContainer container,
        BookingConfirmation confirmation)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item()
                .Text("Stay Details")
                .FontSize(14)
                .Bold();

            column.Item().LineHorizontal(1);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(item =>
                {
                    item.Item().Text("Check-in").SemiBold();
                    item.Item().Text(
                        confirmation.CheckInDate.ToString("MMM dd, yyyy"));
                });

                row.RelativeItem().Column(item =>
                {
                    item.Item().Text("Check-out").SemiBold();
                    item.Item().Text(
                        confirmation.CheckOutDate.ToString("MMM dd, yyyy"));
                });

                row.RelativeItem().Column(item =>
                {
                    item.Item().Text("Nights").SemiBold();
                    item.Item().Text(
                        confirmation.NumberOfNights.ToString());
                });
            });
        });
    }

    private static void ComposeRooms(
        IContainer container,
        BookingConfirmation confirmation)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item()
                .Text("Room Details")
                .FontSize(14)
                .Bold();

            column.Item().LineHorizontal(1);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Room Type").SemiBold();
                    header.Cell().AlignRight().Text("Price / Night").SemiBold();
                    header.Cell().AlignRight().Text("Nights").SemiBold();
                });

                foreach (var room in confirmation.Rooms)
                {
                    table.Cell()
                        .PaddingTop(6)
                        .Text(room.RoomType.ToString());

                    table.Cell()
                        .PaddingTop(6)
                        .AlignRight()
                        .Text(FormatMoney(
                            room.OriginalPricePerNight,
                            confirmation.Currency));

                    table.Cell()
                        .PaddingTop(6)
                        .AlignRight()
                        .Text(confirmation.NumberOfNights.ToString());
                }
            });
        });
    }

    private static void ComposePaymentSummary(
        IContainer container,
        BookingConfirmation confirmation)
    {
        container.Column(column =>
        {
            column.Spacing(7);

            column.Item()
                .Text("Payment Summary")
                .FontSize(14)
                .Bold();

            column.Item().LineHorizontal(1);

            AddSummaryRow(
                column,
                "Subtotal",
                FormatMoney(
                    confirmation.SubtotalAmount,
                    confirmation.Currency));

            if (confirmation.DiscountAmount > 0)
            {
                AddSummaryRow(
                    column,
                    "Discount",
                    $"-{FormatMoney(
                        confirmation.DiscountAmount,
                        confirmation.Currency)}");
            }

            column.Item().LineHorizontal(1);

            AddSummaryRow(
                column,
                "Total Paid",
                FormatMoney(
                    confirmation.TotalAmount,
                    confirmation.Currency),
                bold: true);

            AddSummaryRow(
                column,
                "Payment Status",
                confirmation.PaymentStatus?.ToString() ?? "-");

            AddSummaryRow(
                column,
                "Currency",
                confirmation.Currency?.ToUpperInvariant() ?? "-");
        });
    }

    private static void AddSummaryRow(
        ColumnDescriptor column,
        string label,
        string value,
        bool bold = false)
    {
        column.Item().Row(row =>
        {
            var labelText = row.RelativeItem().Text(label);
            var valueText = row.RelativeItem().AlignRight().Text(value);

            if (bold)
            {
                labelText.Bold();
                valueText.Bold();
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container
            .AlignCenter()
            .Column(column =>
            {
                column.Item()
                    .AlignCenter()
                    .Text("Thank you for your booking.")
                    .SemiBold();

                column.Item()
                    .AlignCenter()
                    .Text("Please keep this confirmation for your records.")
                    .FontSize(9);
            });
    }

    private static string FormatMoney(
        decimal amount,
        string? currency)
    {
        var currencyCode =
            currency?.ToUpperInvariant() ?? string.Empty;

        return $"{amount:F2} {currencyCode}".Trim();
    }
}