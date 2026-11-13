using Ludex.WebApi.Features.Games.Models.Entities;
using Ludex.WebApi.Features.Games.Repositories;

namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// Finds the game of names from another library (Playnite, PlayStation): first among the games already in the
/// person's library (an import done again costs no calls), then in the catalog, saving the games found to dbo.Game
/// with everything the game page shows.
/// </summary>
/// <param name="catalog">The catalog.</param>
/// <param name="gameRepository">Games (the matched ones are saved).</param>
/// <param name="logger">Logger (what wasn't matched, and the closest candidates, to see why).</param>
internal sealed class GameMatcher(IGameCatalog catalog, IGameRepository gameRepository, ILogger<GameMatcher> logger)
{
    /// <summary>
    /// The game of each name, when known with confidence.
    /// </summary>
    /// <param name="names">The names, each with the platform its copy is on (to break ties; may be null).</param>
    /// <param name="known">Games already in the library (matched first, by name).</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>The game of each name matched (names not matched are left out).</returns>
    /// <exception cref="CatalogUnavailableException">The catalog can't be reached.</exception>
    public async Task<Dictionary<string, GameRow>> MatchAsync(
        IReadOnlyCollection<(string Name, string? Platform)> names,
        IEnumerable<GameRow> known,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, GameRow>(StringComparer.Ordinal);
        var wanted = names.Where(static n => NameMatch.Key(n.Name).Length > 0)
            .GroupBy(static n => n.Name, StringComparer.Ordinal)
            .Select(static g => g.First())
            .ToList();

        // 1. The library: the same folded name, or the same once editions are put aside.
        var byKey = new Dictionary<string, GameRow>(StringComparer.Ordinal);
        var byBase = new Dictionary<string, GameRow>(StringComparer.Ordinal);
        foreach (var game in known)
        {
            var key = NameMatch.Key(game.Name);
            byKey.TryAdd(key, game);
            byBase.TryAdd(NameMatch.Base(key), game);
        }

        var rest = new List<(string Name, string? Platform)>();
        foreach (var item in wanted)
        {
            var key = NameMatch.Key(item.Name);
            if (byKey.TryGetValue(key, out var game) || byBase.TryGetValue(NameMatch.Base(key), out game))
            {
                result[item.Name] = game;
            }
            else
            {
                rest.Add(item);
            }
        }

        if (rest.Count == 0)
        {
            return result;
        }

        // 2. The catalog. A name with an edition at its end is searched without it (IGDB knows the main game).
        static string Query(string name)
        {
            var key = NameMatch.Key(name);
            var stem = NameMatch.Base(key);
            if (stem.Length < key.Length && stem.Length > 0)
            {
                return stem;   // an edition put aside
            }

            return NameMatch.WithoutYear(name);   // "Mass Effect 2 (2010)" → "Mass Effect 2"
        }

        var candidates = await catalog.SearchManyAsync([.. rest.Select(n => Query(n.Name)).Distinct(StringComparer.Ordinal)], 8, cancellationToken);
        var chosen = new Dictionary<string, int>(StringComparer.Ordinal);
        var missed = new List<string>();
        foreach (var (name, platform) in rest)
        {
            if (candidates.TryGetValue(Query(name), out var list) && NameMatch.Best(name, list, platform) is { } game)
            {
                chosen[name] = game.Id;
            }
            else if (missed.Count < 30)
            {
                missed.Add(list is { Count: > 0 } ? $"{name} → {string.Join(" | ", list.Take(3).Select(static g => g.Name))}" : $"{name} → nothing");
            }
        }

        logger.LogInformation(
            "Matching: {Names} names, {Library} from the library, {Catalog} from IGDB, {Missed} not matched",
            wanted.Count,
            wanted.Count - rest.Count,
            chosen.Count,
            rest.Count - chosen.Count);
        if (missed.Count > 0)
        {
            logger.LogInformation("Not matched (name → IGDB's closest): {Missed}", string.Join(" ;; ", missed));
        }

        // Fetch the games chosen with everything the game page shows, and keep them.
        var saved = new Dictionary<int, GameRow>();
        foreach (var game in await catalog.GetManyAsync([.. chosen.Values.Distinct()], cancellationToken))
        {
            saved[game.Id] = await gameRepository.UpsertAsync(GameRow.From(game));
        }

        foreach (var (name, id) in chosen)
        {
            if (saved.TryGetValue(id, out var game))
            {
                result[name] = game;
            }
        }

        return result;
    }
}
