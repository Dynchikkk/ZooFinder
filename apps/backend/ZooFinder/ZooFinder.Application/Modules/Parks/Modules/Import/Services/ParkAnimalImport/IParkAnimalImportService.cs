namespace ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;

public interface IParkAnimalImportService
{
    // Rows are decoded by the transport/file boundary, not by the business layer.
    Task<ParkAnimalImportPreviewResponse> PreviewAsync(
        PreviewParkAnimalImportRequest request,
        CancellationToken cancellationToken);
    Task<ParkAnimalImportResponse> ApplyAsync(
        ApplyParkAnimalImportRequest request,
        CancellationToken cancellationToken);
}
