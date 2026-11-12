using System.Globalization;
using Ludex.Library.Enums;
using Ludex.Library.Validation;

namespace Ludex.WebApi.Extensions.Globalization;

/// <summary>
/// Translates message keys and field identifiers into user-facing text.
/// </summary>
internal interface IGlobalizationHelper
{
    /// <summary>
    /// Translates a key.
    /// </summary>
    /// <param name="key">A <see cref="TranslationKeys"/> name or a field identifier.</param>
    /// <param name="arguments">Values for the message placeholders.</param>
    /// <returns>The translated text, or the key itself when there is no translation.</returns>
    Task<string> TranslateAsync(string key, params object[] arguments);

    /// <summary>
    /// Builds a validation error "{field label} {message}".
    /// </summary>
    /// <param name="field">The field identifier.</param>
    /// <param name="messageKey">The message key.</param>
    /// <param name="arguments">Values for the message placeholders.</param>
    /// <returns>The validation error.</returns>
    Task<ApiValidationError> ValidationErrorAsync(string field, string messageKey, params object[] arguments);
}

/// <summary>
/// The user-facing messages (English). The app has one language, so the texts live in code rather than in a
/// translation table.
/// </summary>
internal sealed class GlobalizationHelper : IGlobalizationHelper
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-IE");

    private static readonly Dictionary<string, string> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(TranslationKeys.ValidationProblems)] = "Some of the data sent is not valid.",
        [nameof(TranslationKeys.RequiredField)] = "is required.",
        [nameof(TranslationKeys.AlreadyExists)] = "'{0}' is already taken.",
        [nameof(TranslationKeys.NoRecordsFound)] = "'{0}' does not exist.",
        [nameof(TranslationKeys.UserNameInvalid)] = "must be 3–32 letters, numbers, dots, dashes or underscores.",
        [nameof(TranslationKeys.PasswordTooShort)] = "must have at least {0} characters.",
        [nameof(TranslationKeys.PasswordTooLong)] = "is too long.",
        [nameof(TranslationKeys.WrongCredentials)] = "Wrong user name or password.",
        [nameof(TranslationKeys.WrongCurrentPassword)] = "is wrong.",
        [nameof(TranslationKeys.LastAdmin)] = "The last admin can't be deleted.",
        [nameof(TranslationKeys.OutOfRange)] = "must be between {0} and {1}.",
        [nameof(TranslationKeys.TooLong)] = "can have at most {0} characters.",
        [nameof(TranslationKeys.GameNotFound)] = "That game isn't in the catalog.",
        [nameof(TranslationKeys.CatalogUnreachable)] = "Can't reach IGDB right now ({0}). Try again in a minute.",
        [nameof(TranslationKeys.FriendSelf)] = "That's you. Ask someone else.",
        [nameof(TranslationKeys.FriendUnknown)] = "There's no account called '{0}' here.",
        [nameof(TranslationKeys.AvatarInvalid)] = "must be a JPEG, PNG or WebP image of at most {0} KB.",
        [nameof(TranslationKeys.FavoriteNotInLibrary)] = "must be a game in your library.",
        [nameof(TranslationKeys.StoreNotForPlatform)] = "is only for PC copies (Steam, Epic Games, GOG…).",
        [nameof(TranslationKeys.FormatNotForPlatform)] = "isn't possible on {0}.",
        [nameof(TranslationKeys.CollectionNameTaken)] = "You already have a collection called '{0}'.",
        [nameof(TranslationKeys.PriceTargetInvalid)] = "must be a price between 0 and 1,000.",
        [nameof(TranslationKeys.AccentUnknown)] = "isn't one of the colour themes.",
        [nameof(TranslationKeys.DiscordNotLinked)] = "No account here is linked to that Discord account. Sign in with your password and link it in Settings.",
        [nameof(TranslationKeys.DiscordTaken)] = "That Discord account is already linked to another account.",
        [nameof(TranslationKeys.DiscordInvalid)] = "isn't a Discord account.",
        [nameof(TranslationKeys.DiscordNeedsPassword)] = "Discord is the only way into this account. Set a password before unlinking it.",

        ["UserName"] = "User name",
        ["Password"] = "Password",
        ["CurrentPassword"] = "Current password",
        ["NewPassword"] = "New password",
        ["Identifier"] = "Account",
        ["DiscordId"] = "Discord account",
        ["Query"] = "Search",
        ["GameId"] = "Game",
        ["Status"] = "Status",
        ["Rating"] = "Rating",
        ["Hours"] = "Hours",
        ["Platform"] = "Platform",
        ["Notes"] = "Notes",
        ["Friend"] = "Friend",
        ["Bio"] = "Bio",
        ["Location"] = "Location",
        ["FavoriteGameId"] = "Favourite game",
        ["Avatar"] = "Photo",
        ["Store"] = "Store",
        ["Format"] = "That kind of copy",
        ["Name"] = "Name",
        ["Target"] = "Alert price",
    };

    /// <inheritdoc />
    public Task<string> TranslateAsync(string key, params object[] arguments)
    {
        var text = Translations.TryGetValue(key, out var value) ? value : key;
        return Task.FromResult(arguments.Length == 0 ? text : string.Format(English, text, arguments));
    }

    /// <inheritdoc />
    public async Task<ApiValidationError> ValidationErrorAsync(string field, string messageKey, params object[] arguments)
    {
        var label = await TranslateAsync(field);
        var message = await TranslateAsync(messageKey, arguments);
        return new ApiValidationError(field, $"{label} {message}");
    }
}
