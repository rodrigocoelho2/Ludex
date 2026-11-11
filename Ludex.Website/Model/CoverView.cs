using Ludex.Sdk.Models;

namespace Ludex.Website.Model;

/// <summary>
/// What the _Cover partial draws: a game's cover with its blur-up, or its colour and name when it has no cover.
/// </summary>
/// <param name="Game">The game.</param>
/// <param name="Transition">The view-transition name ("cover-1942", or "none").</param>
/// <param name="Large">True for the big cover of the game page (sharper image, loaded at once).</param>
public sealed record CoverView(GameSummaryDTO Game, string Transition, bool Large = false);
