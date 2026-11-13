using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Psn.Services;

/// <summary>
/// Keeps Sony's refresh token encrypted in the database (AES-GCM, with a key derived from Ludex's API key, so no new
/// setting is needed and a copy of the database alone can't use the token). Changing LUDEX_API_KEY makes the saved
/// tokens unreadable: the PlayStation accounts are then connected again.
/// </summary>
/// <param name="options">The API key.</param>
internal sealed class PsnTokenProtector(IOptions<LudexOptions> options)
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// Encrypts a token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>"v1:" and the nonce, the encrypted token and the tag, in Base64.</returns>
    public string Protect(string token)
    {
        var plain = Encoding.UTF8.GetBytes(token);
        var sealedBytes = new byte[NonceSize + plain.Length + TagSize];
        var nonce = sealedBytes.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(Key(), TagSize);
        aes.Encrypt(nonce, plain, sealedBytes.AsSpan(NonceSize, plain.Length), sealedBytes.AsSpan(NonceSize + plain.Length, TagSize));
        return Prefix + Convert.ToBase64String(sealedBytes);
    }

    /// <summary>
    /// Decrypts a token.
    /// </summary>
    /// <param name="value">What <see cref="Protect"/> returned.</param>
    /// <returns>The token, or null when it can't be read (another key, or damaged).</returns>
    public string? Unprotect(string? value)
    {
        if (value is null || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var sealedBytes = Convert.FromBase64String(value[Prefix.Length..]);
            if (sealedBytes.Length <= NonceSize + TagSize)
            {
                return null;
            }

            var length = sealedBytes.Length - NonceSize - TagSize;
            var plain = new byte[length];
            using var aes = new AesGcm(Key(), TagSize);
            aes.Decrypt(sealedBytes.AsSpan(0, NonceSize), sealedBytes.AsSpan(NonceSize, length), sealedBytes.AsSpan(NonceSize + length, TagSize), plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return null;
        }
    }

    private byte[] Key() =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(options.Value.ApiKey), 32, info: Encoding.UTF8.GetBytes("ludex:psn-token"));
}
