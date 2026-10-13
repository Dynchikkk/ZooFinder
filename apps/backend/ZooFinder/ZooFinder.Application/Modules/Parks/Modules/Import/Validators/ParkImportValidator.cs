using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Modules.Parks.Modules.Import.Constants;

namespace ZooFinder.Application.Modules.Parks.Modules.Import.Validators;

public static class ParkImportValidator
{
    public static void ValidateRows<T>(IReadOnlyList<T>? rows)
    {
        if (rows == null || rows.Count > ParkImportConstraints.MaximumRows || rows.Any(row => row == null))
        {
            throw new RequestValidationException("Import rows are missing or exceed the limit.");
        }
    }

    public static void ValidateName(string name)
    {
        if (name.Length > ParkImportConstraints.MaximumNameLength)
        {
            throw new RequestValidationException("Import animal name is too long.");
        }
    }
}
