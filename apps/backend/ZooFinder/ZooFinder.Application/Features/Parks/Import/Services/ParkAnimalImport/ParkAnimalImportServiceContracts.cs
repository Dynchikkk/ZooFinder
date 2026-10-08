using ZooFinder.Application.Common.Language.Constants;

namespace ZooFinder.Application.Features.Parks.Import.Services.ParkAnimalImport;

public sealed record ApplyParkAnimalImportRequest(Guid ParkId, IReadOnlyList<ParkAnimalImportSelection> Rows);

public sealed record ParkAnimalImportSelection(string InformationSource, string SourceItemId,
    string LanguageCode, string? LocalDescription = null, bool IsPublished = true);

public sealed record ParkAnimalImportPreviewResponse(IReadOnlyList<ParkAnimalImportRowResponse> Rows)
{
    public bool CanApply => Rows.All(row => row.Error == null);
}

public sealed record ParkAnimalImportRowResponse(int RowIndex, ParkAnimalImportMatchResponse? Match, string? Error);

public sealed record ParkAnimalImportMatchResponse(string InformationSource, string SourceItemId,
    string LanguageCode, string Title, string? ScientificName, string? LocalDescription);

public sealed record ParkAnimalImportResponse(IReadOnlyList<Guid> ParkAnimalIds);

public sealed record PreviewParkAnimalImportRequest(Guid ParkId, IReadOnlyList<ParkAnimalImportRow> Rows);

public sealed record ParkAnimalImportRow(string? CommonName = null, string? ScientificName = null,
    string? InformationSource = null, string? SourceItemId = null,
    string LanguageCode = LanguageCodes.English, string? LocalDescription = null);
