using System.Globalization;
using System.Text;
using Ludex.Library.Enums;

namespace Ludex.WebApi.Features.Catalog;

/// <summary>
/// The catalog Ludex uses while no IGDB credentials are set: a short list of well-known games, without images (the cards
/// are drawn in colour with the name). Enough to try Ludex out and for the CI screenshots; set the Twitch Client ID and
/// Secret for the full catalog. Ids start at <see cref="FirstId"/>, far above IGDB's, so they never collide.
/// </summary>
internal sealed class OfflineCatalog : IGameCatalog
{
    /// <summary>The first id of the built-in list.</summary>
    public const int FirstId = 1_000_000_000;

    private static readonly List<CatalogGame> Games = Build();

    /// <inheritdoc />
    public CatalogSource Source => CatalogSource.Offline;

    /// <summary>Gets every game of the list (for tests and the cover wall).</summary>
    public static IReadOnlyList<CatalogGame> All => Games;

    /// <inheritdoc />
    public Task<List<CatalogGame>> SearchAsync(string query, int take, CancellationToken cancellationToken = default)
    {
        var needle = Fold(query);
        var words = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var found = Games
            .Select(game => (Game: game, Name: Fold(game.Name)))
            .Where(item => words.Length > 0 && words.All(word => item.Name.Contains(word, StringComparison.Ordinal)))
            .OrderBy(item => item.Name.StartsWith(needle, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(item => item.Name.IndexOf(words[0], StringComparison.Ordinal))
            .ThenBy(item => item.Game.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, take))
            .Select(item => item.Game)
            .ToList();
        return Task.FromResult(found);
    }

    /// <inheritdoc />
    public Task<CatalogGame?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Games.FirstOrDefault(game => game.Id == id));

    /// <inheritdoc />
    public Task<List<CatalogGame>> PopularAsync(int take, CancellationToken cancellationToken = default) =>
        Task.FromResult(Games.Take(Math.Max(1, take)).ToList());

    /// <summary>
    /// Lower case without accents or punctuation, for matching ("Pokémon: Z-A" → "pokemon z a").
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The folded text.</returns>
    internal static string Fold(string? text)
    {
        var builder = new StringBuilder();
        foreach (var c in (text ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<CatalogGame> Build()
    {
        var list = new List<CatalogGame>();
        void Add(string name, int year, string developer, string publisher, string genres, string platforms, string modes, string summary) =>
            list.Add(new CatalogGame
            {
                Id = FirstId + list.Count + 1,
                Name = name,
                Slug = Fold(name).Replace(' ', '-'),
                Summary = summary,
                ReleaseDate = new DateOnly(year, 1, 1),
                Developer = developer,
                Publisher = publisher,
                Genres = [.. genres.Split(';')],
                Platforms = [.. platforms.Split(';')],
                Modes = [.. modes.Split(';')],
            });

        Add("Elden Ring", 2022, "FromSoftware", "Bandai Namco", "Role-playing (RPG);Adventure", "PC;PS5;PS4;Xbox Series;Xbox One", "Single player;Multiplayer;Co-operative",
            "An open-world action RPG across the Lands Between: ride, explore ruins and dungeons in any order and take on huge bosses with demanding, Souls-style combat.");
        Add("Hollow Knight: Silksong", 2025, "Team Cherry", "Team Cherry", "Platform;Adventure;Indie", "PC;Switch;Switch 2;PS5;PS4;Xbox Series;Xbox One", "Single player",
            "Hornet climbs a kingdom of silk and song in a hand-drawn action platformer, with fast acrobatic combat, crafted tools and a world full of secrets.");
        Add("Hollow Knight", 2017, "Team Cherry", "Team Cherry", "Platform;Adventure;Indie", "PC;Switch;PS4;Xbox One", "Single player",
            "A lone knight descends into the ruined bug kingdom of Hallownest in a hand-drawn metroidvania with tight combat and a vast interconnected map.");
        Add("Hades II", 2025, "Supergiant Games", "Supergiant Games", "Role-playing (RPG);Hack and slash/Beat 'em up;Indie", "PC;Switch;Switch 2", "Single player",
            "Melinoë, princess of the Underworld, fights her way towards the Titan of Time in a roguelike where every run brings new boons, weapons and story.");
        Add("Hades", 2020, "Supergiant Games", "Supergiant Games", "Role-playing (RPG);Hack and slash/Beat 'em up;Indie", "PC;Switch;PS5;PS4;Xbox Series;Xbox One", "Single player",
            "Zagreus tries to escape the Underworld again and again in a fast roguelike dungeon crawler where the gods of Olympus lend their powers.");
        Add("Balatro", 2024, "LocalThunk", "Playstack", "Card & Board Game;Strategy;Indie", "PC;Switch;PS5;PS4;Xbox Series;iOS;Android", "Single player",
            "A poker-inspired roguelike deckbuilder: play hands, collect jokers that bend the rules and chase ever bigger scores.");
        Add("Baldur's Gate 3", 2023, "Larian Studios", "Larian Studios", "Role-playing (RPG);Strategy;Turn-based strategy (TBS)", "PC;PS5;Xbox Series;Mac", "Single player;Multiplayer;Co-operative",
            "A party-based RPG set in the Forgotten Realms, with turn-based combat built on Dungeons & Dragons rules and choices that reshape the story.");
        Add("Clair Obscur: Expedition 33", 2025, "Sandfall Interactive", "Kepler Interactive", "Role-playing (RPG);Turn-based strategy (TBS)", "PC;PS5;Xbox Series", "Single player",
            "A Belle Époque–inspired RPG where an expedition sets out to stop the Paintress, mixing turn-based battles with real-time dodges and parries.");
        Add("Pokémon Legends: Z-A", 2025, "Game Freak", "Nintendo", "Role-playing (RPG);Adventure", "Switch;Switch 2", "Single player;Multiplayer",
            "A Pokémon adventure set in Lumiose City during a redevelopment plan, with real-time battles and Mega Evolution.");
        Add("The Legend of Zelda: Tears of the Kingdom", 2023, "Nintendo EPD", "Nintendo", "Adventure;Puzzle", "Switch;Switch 2", "Single player",
            "Link explores Hyrule's land, skies and depths, building vehicles and contraptions with new powers in a sprawling open-world adventure.");
        Add("The Legend of Zelda: Breath of the Wild", 2017, "Nintendo EPD", "Nintendo", "Adventure;Puzzle", "Switch;Wii U", "Single player",
            "An open-air Hyrule to climb, glide and cook your way across, where any mountain on the horizon can be reached.");
        Add("The Legend of Zelda: Echoes of Wisdom", 2024, "Grezzo", "Nintendo", "Adventure;Puzzle", "Switch", "Single player",
            "Princess Zelda copies objects and monsters as echoes to solve puzzles and fight in a top-down Zelda adventure.");
        Add("The Legend of Zelda: Ocarina of Time", 1998, "Nintendo EAD", "Nintendo", "Adventure;Puzzle", "N64;3DS", "Single player",
            "Link travels through time to stop Ganondorf in the 3D Zelda that set the template for action-adventure games.");
        Add("Cyberpunk 2077", 2020, "CD Projekt Red", "CD Projekt", "Role-playing (RPG);Shooter;Adventure", "PC;PS5;Xbox Series;Switch 2", "Single player",
            "An open-world RPG in Night City: build your mercenary, take on gigs and carry a rogue AI in your head.");
        Add("Red Dead Redemption 2", 2018, "Rockstar Games", "Rockstar Games", "Adventure;Shooter", "PC;PS4;Xbox One", "Single player;Multiplayer",
            "Arthur Morgan rides with the Van der Linde gang as the Wild West comes to an end, in a vast and detailed open world.");
        Add("The Witcher 3: Wild Hunt", 2015, "CD Projekt Red", "CD Projekt", "Role-playing (RPG);Adventure", "PC;PS5;PS4;Xbox Series;Xbox One;Switch", "Single player",
            "Geralt of Rivia searches for his adopted daughter across a war-torn continent, taking monster contracts along the way.");
        Add("Celeste", 2018, "Maddy Makes Games", "Maddy Makes Games", "Platform;Indie", "PC;Switch;PS4;Xbox One", "Single player",
            "Madeline climbs Celeste Mountain in a precise, challenging platformer about anxiety, with hundreds of hand-crafted screens.");
        Add("Stardew Valley", 2016, "ConcernedApe", "ConcernedApe", "Simulator;Role-playing (RPG);Indie", "PC;Switch;PS4;Xbox One;iOS;Android", "Single player;Multiplayer;Co-operative",
            "Take over your grandfather's old farm: grow crops, raise animals, fish, mine and get to know the people of Pelican Town.");
        Add("Persona 5 Royal", 2019, "Atlus", "Sega", "Role-playing (RPG);Turn-based strategy (TBS)", "PC;PS5;PS4;Switch;Xbox Series;Xbox One", "Single player",
            "High-school students lead double lives as the Phantom Thieves, stealing the corrupt desires of adults in a stylish turn-based RPG.");
        Add("Outer Wilds", 2019, "Mobius Digital", "Annapurna Interactive", "Adventure;Puzzle;Indie", "PC;PS5;PS4;Xbox Series;Xbox One;Switch", "Single player",
            "Explore a tiny solar system caught in a 22-minute time loop and piece together the mystery of an ancient civilization.");
        Add("Disco Elysium", 2019, "ZA/UM", "ZA/UM", "Role-playing (RPG);Adventure", "PC;PS5;PS4;Xbox Series;Xbox One;Switch", "Single player",
            "An amnesiac detective solves a murder in a decaying city, where skills are voices in your head and every conversation is a choice.");
        Add("Portal 2", 2011, "Valve", "Valve", "Puzzle;Platform", "PC;PS3;Xbox 360;Switch", "Single player;Co-operative",
            "Solve physics puzzles with the portal gun in the depths of Aperture Science, alone or with a friend in co-op.");
        Add("Sekiro: Shadows Die Twice", 2019, "FromSoftware", "Activision", "Adventure;Hack and slash/Beat 'em up", "PC;PS4;Xbox One", "Single player",
            "A shinobi seeks revenge in a reimagined Sengoku Japan, where combat is a duel of posture, timing and deflection.");
        Add("Hi-Fi Rush", 2023, "Tango Gameworks", "Bethesda Softworks", "Hack and slash/Beat 'em up;Music", "PC;PS5;Xbox Series", "Single player",
            "A would-be rock star fights a megacorp in a colourful action game where everything moves, and every hit lands, to the beat.");
        Add("Monster Hunter Wilds", 2025, "Capcom", "Capcom", "Role-playing (RPG);Hack and slash/Beat 'em up", "PC;PS5;Xbox Series", "Single player;Multiplayer;Co-operative",
            "Track and hunt huge monsters across living ecosystems, then craft gear from what you carve, alone or with up to three friends.");
        Add("Split Fiction", 2025, "Hazelight Studios", "Electronic Arts", "Platform;Adventure", "PC;PS5;Xbox Series;Switch 2", "Co-operative;Split screen",
            "Two writers trapped in their own sci-fi and fantasy stories must work together in a co-op-only adventure that changes its rules every level.");
        Add("Minecraft", 2011, "Mojang Studios", "Mojang Studios", "Simulator;Adventure", "PC;Switch;PS4;Xbox One;iOS;Android", "Single player;Multiplayer;Co-operative",
            "Gather, craft and build in endless blocky worlds, survive the nights or create freely in creative mode.");
        Add("Dune: Imperium", 2024, "Dire Wolf", "Dire Wolf", "Card & Board Game;Strategy", "PC;iOS;Android", "Single player;Multiplayer",
            "The digital edition of the board game that mixes deck-building and worker placement in the struggle for Arrakis.");
        Add("Catan", 1995, "Catan GmbH", "Catan GmbH", "Card & Board Game;Strategy", "PC;iOS;Android;Switch", "Multiplayer",
            "Settle an island by trading wood, brick, wheat, sheep and ore with the other players, and be first to ten victory points.");
        Add("Mario Kart World", 2025, "Nintendo EPD", "Nintendo", "Racing;Arcade", "Switch 2", "Single player;Multiplayer;Split screen",
            "Race through a connected world with 24 drivers on the track, or roam it freely between the Grand Prix.");
        Add("Super Mario Odyssey", 2017, "Nintendo EPD", "Nintendo", "Platform;Adventure", "Switch", "Single player;Co-operative",
            "Mario and his hat companion Cappy travel the world in a 3D platformer where throwing the cap lets Mario take over enemies and objects.");
        Add("God of War Ragnarök", 2022, "Santa Monica Studio", "Sony Interactive Entertainment", "Adventure;Hack and slash/Beat 'em up", "PS5;PS4;PC", "Single player",
            "Kratos and Atreus journey across the Nine Realms as Fimbulwinter heralds the end of the world.");
        Add("Final Fantasy VII Rebirth", 2024, "Square Enix", "Square Enix", "Role-playing (RPG);Adventure", "PS5;PC", "Single player",
            "Cloud and his friends leave Midgar behind and chase Sephiroth across an open world in the second part of the remake.");
        Add("Astro Bot", 2024, "Team Asobi", "Sony Interactive Entertainment", "Platform;Adventure", "PS5", "Single player",
            "Astro rescues his crew across dozens of inventive planets in a joyful 3D platformer built around the DualSense controller.");
        Add("Pokémon Scarlet", 2022, "Game Freak", "Nintendo", "Role-playing (RPG);Adventure", "Switch", "Single player;Multiplayer",
            "Explore the open Paldea region, catch and train Pokémon and take on gyms, Titans and Team Star in any order.");

        return list;
    }
}
