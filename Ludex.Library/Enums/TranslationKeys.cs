namespace Ludex.Library.Enums;

/// <summary>
/// Keys of the user-facing messages. Use with <c>nameof(TranslationKeys.X)</c>, never a magic string.
/// </summary>
public enum TranslationKeys
{
    /// <summary>Generic title of a validation failure.</summary>
    ValidationProblems,

    /// <summary>A required field is empty.</summary>
    RequiredField,

    /// <summary>Another record already uses the value.</summary>
    AlreadyExists,

    /// <summary>The record does not exist.</summary>
    NoRecordsFound,

    /// <summary>A user name with characters or a length that is not allowed.</summary>
    UserNameInvalid,

    /// <summary>A password shorter than the minimum.</summary>
    PasswordTooShort,

    /// <summary>A password longer than the maximum.</summary>
    PasswordTooLong,

    /// <summary>Wrong user name or password (never says which).</summary>
    WrongCredentials,

    /// <summary>The current password is wrong.</summary>
    WrongCurrentPassword,

    /// <summary>The last admin cannot be deleted.</summary>
    LastAdmin,

    /// <summary>A value outside its allowed range.</summary>
    OutOfRange,

    /// <summary>A text longer than allowed.</summary>
    TooLong,

    /// <summary>The game is not in the catalog.</summary>
    GameNotFound,

    /// <summary>The catalog (IGDB) could not be reached.</summary>
    CatalogUnreachable,

    /// <summary>A friend request to oneself.</summary>
    FriendSelf,

    /// <summary>No account with that user name.</summary>
    FriendUnknown,

    /// <summary>A profile photo that isn't a JPEG, PNG or WebP image, or is too big.</summary>
    AvatarInvalid,

    /// <summary>A favourite game that isn't in the library.</summary>
    FavoriteNotInLibrary,

    /// <summary>A store that isn't a PC store, or a store on a platform that isn't a PC.</summary>
    StoreNotForPlatform,

    /// <summary>A format the platform never had (a digital NES game, a physical iOS one).</summary>
    FormatNotForPlatform,

    /// <summary>A collection name already used by the account.</summary>
    CollectionNameTaken,

    /// <summary>A price target that isn't a price.</summary>
    PriceTargetInvalid,

    /// <summary>A colour theme that isn't one of the themes.</summary>
    AccentUnknown,

    /// <summary>No account is linked to that Discord account.</summary>
    DiscordNotLinked,

    /// <summary>That Discord account is linked to another account.</summary>
    DiscordTaken,

    /// <summary>A Discord id that isn't one.</summary>
    DiscordInvalid,
}
