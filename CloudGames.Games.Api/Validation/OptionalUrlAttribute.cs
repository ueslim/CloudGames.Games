using System.ComponentModel.DataAnnotations;

namespace CloudGames.Games.Api.Validation;

public class OptionalUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        // Allow null or empty values
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return true;

        // If value is provided, validate it's a valid URL
        var stringValue = value.ToString();
        return Uri.TryCreate(stringValue, UriKind.Absolute, out Uri? result) && 
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be a valid URL when provided.";
    }
}
