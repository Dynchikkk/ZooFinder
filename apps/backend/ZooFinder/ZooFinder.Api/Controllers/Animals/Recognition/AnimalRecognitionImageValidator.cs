using ZooFinder.Application.Modules.Animals.Modules.Recognition.Constants;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Api.Controllers.Animals.Recognition;

public static class AnimalRecognitionImageValidator
{
    private const int HeaderLength = 12;
    private const int WebpFormatOffset = 8;

    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static async Task<string> ValidateAsync(IFormFile image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateLength(image.Length);

        await using Stream stream = image.OpenReadStream();
        byte[] header = new byte[HeaderLength];
        int headerLength = await stream.ReadAtLeastAsync(header, header.Length, false, cancellationToken);
        return GetContentType(header.AsSpan(0, headerLength));
    }

    private static void ValidateLength(long length)
    {
        if (length <= 0)
        {
            throw new RequestValidationException("Image file is empty.");
        }

        if (length > AnimalRecognitionConstraints.MaximumImageLength)
        {
            throw new BadHttpRequestException("Image file is too large.", StatusCodes.Status413PayloadTooLarge);
        }
    }

    private static string GetContentType(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegSignature))
        {
            return "image/jpeg";
        }

        if (header.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (header.Length >= HeaderLength && header.StartsWith("RIFF"u8) &&
            header.Slice(WebpFormatOffset).StartsWith("WEBP"u8))
        {
            return "image/webp";
        }

        throw new BadHttpRequestException("Image format is not supported.", StatusCodes.Status415UnsupportedMediaType);
    }
}
