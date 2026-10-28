using System.Net;
using System.Net.Http.Headers;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Constants;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;

namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

internal sealed class RecognitionImageContent : HttpContent
{
    private const int CopyBufferSizeBytes = 8192;

    private readonly Stream _source;

    public RecognitionImageContent(Stream source, string contentType)
    {
        _source = source;
        Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return CopyImageAsync(stream, CancellationToken.None);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        return CopyImageAsync(stream, cancellationToken);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    private async Task CopyImageAsync(Stream target, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[CopyBufferSizeBytes];
        long totalBytes = 0;
        int count;

        while ((count = await _source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            totalBytes += count;
            if (totalBytes > AnimalRecognitionConstraints.MaximumImageLength)
            {
                throw new RequestValidationException("Recognition image exceeds the maximum size.");
            }

            await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }

        if (totalBytes == 0)
        {
            throw new RequestValidationException("Recognition image is empty.");
        }
    }
}
