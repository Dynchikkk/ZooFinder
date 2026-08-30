using ZooFinder.Application.Features.Animals.Recognition.Contracts;

namespace ZooFinder.Application.Features.Animals.Recognition.Mappers;

public static class AnimalRecognitionMapper
{
    public static AnimalRecognitionResponse ToResponse(AnimalRecognitionProviderResult result)
    {
        return new AnimalRecognitionResponse(
            result.IsAnimal,
            NormalizeName(result.CommonName),
            NormalizeName(result.ScientificName),
            [.. result.Alternatives.Select(ToResponse)]);
    }

    private static AnimalRecognitionCandidateResponse ToResponse(
        AnimalRecognitionProviderCandidate result)
    {
        return new AnimalRecognitionCandidateResponse(
            NormalizeName(result.CommonName),
            NormalizeName(result.ScientificName));
    }

    private static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }
}
