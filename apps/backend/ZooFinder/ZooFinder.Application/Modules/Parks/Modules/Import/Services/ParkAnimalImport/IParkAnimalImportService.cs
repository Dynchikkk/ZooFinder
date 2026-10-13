namespace ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;

public interface IParkAnimalImportService
{
    Task<ParkAnimalImportPreviewResponse> PreviewAsync(
        PreviewParkAnimalImportRequest request,
        CancellationToken cancellationToken);

    Task<ParkAnimalImportResponse> ApplyAsync(
        ApplyParkAnimalImportRequest request,
        CancellationToken cancellationToken);
}
