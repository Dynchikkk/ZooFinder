using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using ZooFinder.Api.Configuration.Parks.Settings;
using ZooFinder.Application.Modules.Parks.Modules.Import.Constants;
using ZooFinder.Application.Modules.Parks.Modules.Import.Services.ParkAnimalImport;
using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Shared.Language.Constants;

namespace ZooFinder.Api.Controllers.Parks.Import;

public sealed class ParkAnimalCsvReader
{
    private const string Delimiter = ";";
    private const string CommonNameColumn = "commonName";
    private const string ScientificNameColumn = "scientificName";
    private const string InformationSourceColumn = "informationSource";
    private const string SourceItemIdColumn = "sourceItemId";
    private const string LanguageCodeColumn = "languageCode";
    private const string LocalDescriptionColumn = "localDescription";

    private static readonly string[] RequiredHeaders =
    [
        CommonNameColumn,
        ScientificNameColumn,
        InformationSourceColumn,
        SourceItemIdColumn,
        LanguageCodeColumn,
        LocalDescriptionColumn
    ];

    private readonly ParkImportSettings _settings;

    public ParkAnimalCsvReader(IOptions<ParkImportSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<ParkAnimalImportRow>> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        string text = await ReadTextAsync(file, cancellationToken);
        using var textReader = new StringReader(text);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = Delimiter,
            DetectColumnCountChanges = true,
            ExceptionMessagesContainRawData = false
        };
        using var csv = new CsvReader(textReader, configuration);

        try
        {
            return await ReadRowsAsync(csv, cancellationToken);
        }
        catch (CsvHelperException)
        {
            throw new RequestValidationException($"CSV record {csv.Parser.Row} has an invalid format.");
        }
    }

    private async Task<string> ReadTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new RequestValidationException("CSV file is empty.");
        }

        if (file.Length > _settings.MaximumFileBytes)
        {
            throw new BadHttpRequestException("CSV file is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        await using Stream stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, new UTF8Encoding(true, true), detectEncodingFromByteOrderMarks: false);

        try
        {
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            throw new RequestValidationException("CSV file must use UTF-8 encoding.");
        }
    }

    private static async Task<IReadOnlyList<ParkAnimalImportRow>> ReadRowsAsync(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!await csv.ReadAsync())
        {
            throw new RequestValidationException("CSV header is required.");
        }

        csv.ReadHeader();
        ValidateHeaders(csv.HeaderRecord);
        var rows = new List<ParkAnimalImportRow>();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count >= ParkImportConstraints.MaximumRows)
            {
                throw new RequestValidationException($"Import supports up to {ParkImportConstraints.MaximumRows} rows.");
            }

            rows.Add(ReadRow(csv));
        }

        return rows;
    }

    private static void ValidateHeaders(string[]? headers)
    {
        if (headers == null || headers.Length != RequiredHeaders.Length)
        {
            throw new RequestValidationException($"CSV header must contain: {string.Join(Delimiter, RequiredHeaders)}.");
        }

        var remainingHeaders = new HashSet<string>(RequiredHeaders, StringComparer.Ordinal);

        foreach (string header in headers)
        {
            if (!remainingHeaders.Remove(header))
            {
                throw new RequestValidationException("CSV contains an unknown or duplicate column.");
            }
        }
    }

    private static ParkAnimalImportRow ReadRow(CsvReader csv)
    {
        string? languageCode = csv.GetField(LanguageCodeColumn);

        return new ParkAnimalImportRow(
            CommonName: csv.GetField(CommonNameColumn),
            ScientificName: csv.GetField(ScientificNameColumn),
            InformationSource: csv.GetField(InformationSourceColumn),
            SourceItemId: csv.GetField(SourceItemIdColumn),
            LanguageCode: string.IsNullOrWhiteSpace(languageCode) ? LanguageCodes.English : languageCode,
            LocalDescription: csv.GetField(LocalDescriptionColumn));
    }
}
