namespace Starfall.Content;

internal static class ContentIdentityRules
{
    public static void Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value[0] is < 'a' or > 'z' || value.Any(static character =>
                character is not (>= 'a' and <= 'z') &&
                character is not (>= '0' and <= '9') &&
                character != '_'))
        {
            throw new ArgumentException(
                "Content identities must use lowercase ASCII letters, digits and underscores and begin with a letter.",
                parameterName);
        }
    }
}
