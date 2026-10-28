namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

internal sealed record PythonRecognitionMetadata(
    string LanguageCode,
    IReadOnlyList<PythonRecognitionContextCandidate> Candidates);

internal sealed record PythonRecognitionContextCandidate(
    Guid AnimalId,
    string CommonName,
    string? ScientificName);

internal sealed record PythonRecognitionResponse(
    string? Status,
    string? CommonName,
    string? ScientificName,
    Guid? AnimalId,
    IReadOnlyList<PythonRecognitionAlternative>? Alternatives,
    PythonRecognitionExecution? Execution);

internal sealed record PythonRecognitionAlternative(
    string? CommonName,
    string? ScientificName,
    Guid? AnimalId);

internal sealed record PythonRecognitionExecution(
    string? ModelId,
    string? Revision,
    double? InferenceMilliseconds);

internal sealed record PythonRecognitionError(string? Code);
