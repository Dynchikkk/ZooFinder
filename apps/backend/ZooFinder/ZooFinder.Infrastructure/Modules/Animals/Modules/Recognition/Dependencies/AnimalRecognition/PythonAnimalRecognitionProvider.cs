using ZooFinder.Application.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;
using ZooFinder.Application.Modules.Animals.Modules.Recognition.Validators;

namespace ZooFinder.Infrastructure.Modules.Animals.Modules.Recognition.Dependencies.AnimalRecognition;

public sealed class PythonAnimalRecognitionProvider : IAnimalRecognitionProvider
{
    private const string RecognizedStatus = "recognized";
    private const string UncertainStatus = "uncertain";
    private const string NoAnimalStatus = "noAnimal";

    private readonly PythonAnimalRecognitionApiClient _client;
    private readonly PythonAnimalRecognitionSettings _settings;

    public PythonAnimalRecognitionProvider(HttpClient httpClient, PythonAnimalRecognitionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        PythonAnimalRecognitionValidator.ValidateSettings(settings);

        _client = new PythonAnimalRecognitionApiClient(httpClient, settings);
        _settings = settings;
    }

    public async Task<AnimalRecognitionProviderResult> RecognizeAsync(
        AnimalRecognitionProviderRequest request,
        CancellationToken cancellationToken)
    {
        PythonAnimalRecognitionValidator.ValidateRequest(request, _settings.SupportedImageTypes);
        cancellationToken.ThrowIfCancellationRequested();

        PythonRecognitionResponse response = await _client.RecognizeAsync(request, cancellationToken);
        PythonAnimalRecognitionValidator.ValidateResponse(response);

        AnimalRecognitionProviderResult result = CreateResult(response);
        AnimalRecognitionValidator.ValidateResult(result, request.Candidates);
        return result;
    }

    private static AnimalRecognitionProviderResult CreateResult(PythonRecognitionResponse response)
    {
        var alternatives = response.Alternatives!
            .Select(candidate => new AnimalRecognitionProviderCandidate(
                candidate.CommonName, candidate.ScientificName, candidate.AnimalId))
            .ToArray();

        return new AnimalRecognitionProviderResult(
            ParseStatus(response.Status), response.CommonName, response.ScientificName, alternatives,
            response.AnimalId, CreateExecution(response.Execution));
    }

    private static AnimalRecognitionStatus ParseStatus(string? status)
    {
        return status switch
        {
            RecognizedStatus => AnimalRecognitionStatus.Recognized,
            UncertainStatus => AnimalRecognitionStatus.Uncertain,
            NoAnimalStatus => AnimalRecognitionStatus.NoAnimal,
            _ => throw new InvalidOperationException("Recognition service returned an unknown status.")
        };
    }

    private static AnimalRecognitionExecutionResult? CreateExecution(PythonRecognitionExecution? execution)
    {
        if (execution == null)
        {
            return null;
        }

        return new AnimalRecognitionExecutionResult(
            execution.ModelId!, execution.Revision, execution.InferenceMilliseconds!.Value);
    }
}
