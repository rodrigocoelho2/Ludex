using Ludex.Library.Enums;
using Ludex.WebApi.Features.Catalog;
using Ludex.WebApi.Features.Demo.Models.DTOs;
using Ludex.WebApi.Features.Demo.Repositories;
using Ludex.WebApi.Features.Library.Repositories;
using Ludex.WebApi.Features.Lists.Repositories;
using Ludex.WebApi.Features.Reviews.Repositories;
using Microsoft.Extensions.Options;

namespace Ludex.WebApi.Features.Demo.Services;

/// <summary>
/// Adds the demo accounts (<see cref="DemoContent"/>) or takes them out again. They are marked IsDemo, can't sign in,
/// and only come in once a real account exists (so the first real account is still the admin).
/// </summary>
/// <param name="demoRepository">Demo accounts.</param>
/// <param name="libraryRepository">Their libraries.</param>
/// <param name="reviewRepository">Their reviews and comments.</param>
/// <param name="listRepository">Their lists.</param>
/// <param name="gameMatcher">Finds the games by name in the catalog (and saves them).</param>
/// <param name="options">Whether Seed:DemoReviews is on.</param>
/// <param name="timeProvider">The clock (dates are relative to today).</param>
/// <param name="logger">Logger.</param>
internal sealed class DemoSeeder(
    IDemoRepository demoRepository,
    ILibraryRepository libraryRepository,
    IReviewRepository reviewRepository,
    IListRepository listRepository,
    GameMatcher gameMatcher,
    IOptions<SeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DemoSeeder> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// How many demo accounts and reviews there are.
    /// </summary>
    /// <returns>The status.</returns>
    public async Task<DemoStatusDTO> StatusAsync(string? message = null)
    {
        var row = await demoRepository.StatusAsync();
        return new DemoStatusDTO
        {
            Accounts = row.Accounts,
            Reviews = row.Reviews,
            SeedOnStart = options.Value.DemoReviews,
            CanSeed = row.RealAccounts > 0 && row.Accounts == 0,
            Message = message,
        };
    }

    /// <summary>
    /// Adds the demo accounts, when there are none yet and a real account exists.
    /// </summary>
    /// <param name="cancellationToken">Stops it.</param>
    /// <returns>The status, with what happened.</returns>
    public async Task<DemoStatusDTO> SeedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var status = await demoRepository.StatusAsync();
            if (status.RealAccounts == 0)
            {
                return await StatusAsync("Create your own account first: the demo accounts come in after it.");
            }

            if (status.Accounts > 0)
            {
                return await StatusAsync("The demo accounts are already here.");
            }

            Dictionary<string, Games.Models.Entities.GameRow> games;
            try
            {
                games = await gameMatcher.MatchAsync([.. DemoContent.GameNames().Select(static n => (n, (string?)null))], [], cancellationToken);
            }
            catch (CatalogUnavailableException exception)
            {
                return await StatusAsync($"Can't reach the catalog right now ({exception.Message}). Try again in a minute.");
            }

            if (games.Count == 0)
            {
                return await StatusAsync("None of the demo games are in the catalog (set IGDB_CLIENT_ID and IGDB_CLIENT_SECRET for IGDB).");
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            DateTime Ago(double days) => now.AddDays(-days);
            var today = DateOnly.FromDateTime(now);

            // The accounts
            var users = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var user in DemoContent.Users)
            {
                if (await demoRepository.CreateUserAsync(user.Name, user.Bio, user.Location, user.Accent, Ago(user.JoinedDaysAgo)) is { } id)
                {
                    users[user.Name] = id;
                }
            }

            // Their libraries, with how each game ended and the feed lines at the right time
            var entries = new Dictionary<(string User, string Game), DemoEntry>();
            foreach (var entry in DemoContent.Entries)
            {
                if (!users.TryGetValue(entry.User, out var user) || !games.TryGetValue(entry.Game, out var game))
                {
                    continue;
                }

                var entryId = await libraryRepository.SaveAsync(user, game.GameID, (byte)entry.Status, (byte?)entry.Rating, entry.Hours, entry.Platform, null, null, null);
                var finished = entry.Status is GameStatus.Finished ? today.AddDays(-entry.DaysAgo) : (DateOnly?)null;
                var started = entry.Hours is { } hours && entry.Status is not GameStatus.Backlog
                    ? today.AddDays(-entry.DaysAgo - Math.Max(3, (int)Math.Ceiling(hours / 2m)))
                    : (DateOnly?)null;
                await libraryRepository.SaveLogAsync(user, entryId, (byte?)entry.Ending, started, finished);

                var kind = entry.Status switch
                {
                    GameStatus.Finished => ActivityKind.Finished,
                    GameStatus.Played => ActivityKind.Played,
                    GameStatus.Dropped => ActivityKind.Dropped,
                    GameStatus.Playing => ActivityKind.Started,
                    GameStatus.Wishlist => ActivityKind.Wishlisted,
                    _ => ActivityKind.Added,
                };
                await demoRepository.ActivityAsync(user, game.GameID, (byte)kind, null, Ago(entry.DaysAgo + 0.2));
                if (entry.Rating is { } rating)
                {
                    await demoRepository.ActivityAsync(user, game.GameID, (byte)ActivityKind.Rated, (byte)rating, Ago(entry.DaysAgo + 0.1));
                }

                entries[(entry.User, entry.Game)] = entry;
            }

            // Reviews, their likes, and the comments under them
            var reviews = new Dictionary<(string User, string Game), long>();
            foreach (var review in DemoContent.Reviews)
            {
                if (!users.TryGetValue(review.User, out var user) || !games.TryGetValue(review.Game, out var game) || !entries.TryGetValue((review.User, review.Game), out var entry))
                {
                    continue;
                }

                var reviewId = await reviewRepository.SaveAsync(
                    user, game.GameID, review.Text, review.Spoilers, (byte?)entry.Rating, (byte)entry.Status, (byte?)entry.Ending, entry.Platform, Ago(review.DaysAgo));
                reviews[(review.User, review.Game)] = reviewId;
                for (var i = 0; i < review.Likes.Length; i++)
                {
                    if (users.TryGetValue(review.Likes[i], out var liker))
                    {
                        await demoRepository.LikeAsync(liker, reviewId, null, Ago(Math.Max(0.05, review.DaysAgo - (0.3 * (i + 1)))));
                    }
                }
            }

            foreach (var comment in DemoContent.Comments)
            {
                if (users.TryGetValue(comment.User, out var user) && reviews.TryGetValue((comment.Author, comment.Game), out var reviewId))
                {
                    await reviewRepository.CommentAsync(user, reviewId, comment.Text, Ago(comment.DaysAgo + 0.01));
                }
            }

            // Lists
            foreach (var list in DemoContent.Lists)
            {
                if (!users.TryGetValue(list.User, out var user))
                {
                    continue;
                }

                var listId = await listRepository.SaveAsync(user, null, list.Name, list.Description, list.Ranked, (byte)ListVisibility.Public, Ago(list.DaysAgo));
                foreach (var name in list.Games)
                {
                    if (games.TryGetValue(name, out var game))
                    {
                        await listRepository.SetItemAsync(user, listId, game.GameID, true, null);
                    }
                }

                for (var i = 0; i < list.Likes.Length; i++)
                {
                    if (users.TryGetValue(list.Likes[i], out var liker))
                    {
                        await demoRepository.LikeAsync(liker, null, listId, Ago(Math.Max(0.05, list.DaysAgo - (0.5 * (i + 1)))));
                    }
                }
            }

            // The journal
            foreach (var session in DemoContent.Sessions)
            {
                if (users.TryGetValue(session.User, out var user) && games.TryGetValue(session.Game, out var game))
                {
                    var platform = entries.TryGetValue((session.User, session.Game), out var entry) ? entry.Platform : null;
                    await demoRepository.SessionAsync(user, game.GameID, today.AddDays(-session.DaysAgo), session.Minutes, platform, session.Note);
                }
            }

            logger.LogInformation("Demo reviews: {Users} accounts, {Games} games, {Reviews} reviews added", users.Count, games.Count, reviews.Count);
            var missing = DemoContent.GameNames().Count - games.Count;
            return await StatusAsync($"Added {users.Count} demo accounts with {reviews.Count} reviews"
                                     + (missing > 0 ? $" ({missing} of their games weren't found in the catalog and were left out)." : "."));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Deletes every demo account and everything it wrote.
    /// </summary>
    /// <returns>The status, with what happened.</returns>
    public async Task<DemoStatusDTO> RemoveAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var removed = await demoRepository.RemoveAsync();
            logger.LogInformation("Demo reviews: {Removed} demo accounts removed", removed);
            var note = options.Value.DemoReviews ? " Seed__DemoReviews is still true: they come back at the next restart unless you set it to false." : string.Empty;
            return await StatusAsync($"Removed {removed} demo accounts and everything they wrote.{note}");
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>
/// Options of the "Seed" configuration section (in Docker: Seed__DemoReviews).
/// </summary>
public sealed class SeedOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Seed";

    /// <summary>
    /// Gets or sets a value indicating whether the demo accounts with their reviews are added at start (once a real
    /// account exists, and only when there are none yet).
    /// </summary>
    public bool DemoReviews { get; set; }
}

/// <summary>
/// Adds the demo accounts at start when Seed:DemoReviews is on: a minute after the WebApi starts, then every ten
/// minutes until a real account exists and they're in.
/// </summary>
/// <param name="seeder">The seeder.</param>
/// <param name="options">Whether it is on.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">Logger.</param>
internal sealed class DemoSeedJob(DemoSeeder seeder, IOptions<SeedOptions> options, TimeProvider timeProvider, ILogger<DemoSeedJob> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.DemoReviews)
        {
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), timeProvider, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var status = await seeder.StatusAsync();
                    if (status.Accounts > 0)
                    {
                        return;
                    }

                    if (status.CanSeed)
                    {
                        var result = await seeder.SeedAsync(stoppingToken);
                        logger.LogInformation("Seed:DemoReviews: {Message}", result.Message);
                        if (result.Accounts > 0)
                        {
                            return;
                        }
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Seed:DemoReviews: couldn't add the demo accounts yet");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
    }
}
