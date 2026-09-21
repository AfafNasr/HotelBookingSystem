namespace HotelBooking.Application.Hotels.UploadHotelImage;

internal static class ImageFileValidator
{
    public static bool HasValidSignature(
        Stream stream,
        string contentType)
    {
        if (!stream.CanRead)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[12];

        var originalPosition =
            stream.CanSeek ? stream.Position : 0;

        var bytesRead = stream.Read(header);

        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        if (bytesRead < 12)
        {
            return false;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" =>
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,

            "image/png" =>
                header[..8].SequenceEqual(
                    new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    }),

            "image/webp" =>
                header[..4].SequenceEqual("RIFF"u8) &&
                header[8..12].SequenceEqual("WEBP"u8),

            _ => false
        };
    }
}