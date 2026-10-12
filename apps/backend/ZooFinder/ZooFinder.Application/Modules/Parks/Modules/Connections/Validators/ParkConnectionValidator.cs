using ZooFinder.Application.Shared.ErrorHandling.Exceptions;
using ZooFinder.Application.Modules.Parks.Modules.Connections.Constants;

namespace ZooFinder.Application.Modules.Parks.Modules.Connections.Validators;

public static class ParkConnectionValidator
{
    public static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException("Connection request ID must not be empty.");
        }
    }

    public static string? NormalizeText(string? value, int maximumLength)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (text?.Length > maximumLength)
        {
            throw new RequestValidationException("Connection request text is too long.");
        }

        return text;
    }

    public static void ValidatePrice(decimal amount, string currency)
    {
        if (amount < 0 || amount > ParkConnectionConstraints.MaximumAmount || decimal.Round(amount, 2) != amount)
        {
            throw new RequestValidationException("Amount must be non-negative and have at most two decimal places.");
        }

        if (currency.Length != 3 || currency.Any(character => character < 'A' || character > 'Z'))
        {
            throw new RequestValidationException("Currency must be a three-letter code.");
        }
    }
}
