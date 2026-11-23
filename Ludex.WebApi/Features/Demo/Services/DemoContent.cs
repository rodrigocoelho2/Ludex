using Ludex.Library.Enums;

namespace Ludex.WebApi.Features.Demo.Services;

/// <summary>A made-up account of the demo reviews.</summary>
/// <param name="Name">The user name.</param>
/// <param name="Bio">The line on the profile.</param>
/// <param name="Location">Where they say they are.</param>
/// <param name="Accent">The profile's colour.</param>
/// <param name="JoinedDaysAgo">How long ago they "joined".</param>
internal sealed record DemoUser(string Name, string Bio, string Location, string Accent, int JoinedDaysAgo);

/// <summary>A game in a demo account's library.</summary>
/// <param name="User">The account.</param>
/// <param name="Game">The game's name, as the catalog calls it.</param>
/// <param name="Status">The status.</param>
/// <param name="Rating">The rating (1–10).</param>
/// <param name="Hours">The hours.</param>
/// <param name="Platform">The platform.</param>
/// <param name="Ending">How it ended.</param>
/// <param name="DaysAgo">How long ago it was last logged.</param>
internal sealed record DemoEntry(string User, string Game, GameStatus Status, int? Rating, decimal? Hours, string Platform, GameEnding? Ending, int DaysAgo);

/// <summary>A demo review (its rating, status and platform come from the library entry).</summary>
/// <param name="User">The author.</param>
/// <param name="Game">The game.</param>
/// <param name="DaysAgo">How long ago it was written.</param>
/// <param name="Text">The text (original, written for Ludex).</param>
/// <param name="Likes">Who of the other demo accounts liked it.</param>
/// <param name="Spoilers">Whether it gives things away.</param>
internal sealed record DemoReview(string User, string Game, int DaysAgo, string Text, string[] Likes, bool Spoilers = false);

/// <summary>A comment under a demo review.</summary>
/// <param name="User">The writer.</param>
/// <param name="Author">The review's author.</param>
/// <param name="Game">The review's game.</param>
/// <param name="DaysAgo">How long ago.</param>
/// <param name="Text">The text.</param>
internal sealed record DemoComment(string User, string Author, string Game, int DaysAgo, string Text);

/// <summary>A demo list.</summary>
/// <param name="User">The owner.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">What it is about.</param>
/// <param name="Ranked">Whether it is a ranking.</param>
/// <param name="DaysAgo">How long ago it last changed.</param>
/// <param name="Games">Its games, in order.</param>
/// <param name="Likes">Who of the other demo accounts liked it.</param>
internal sealed record DemoList(string User, string Name, string Description, bool Ranked, int DaysAgo, string[] Games, string[] Likes);

/// <summary>A session in a demo account's journal.</summary>
/// <param name="User">The account.</param>
/// <param name="Game">The game.</param>
/// <param name="DaysAgo">Which day.</param>
/// <param name="Minutes">How long.</param>
/// <param name="Note">A note.</param>
internal sealed record DemoSession(string User, string Game, int DaysAgo, int Minutes, string? Note = null);

/// <summary>
/// The demo data: six made-up people with their own libraries, ratings, reviews, likes, comments, lists and journal
/// sessions, so a fresh Ludex shows what the community pages look like. Every text is original. Dates are relative to
/// the day the demo is added, so "this week" always has something in it.
/// </summary>
internal static class DemoContent
{
    private const string Pixel = "pixelmonk";
    private const string Sofia = "savefile_sofia";
    private const string Nuno = "noclip_nuno";
    private const string Ana = "ctrlaltdel_ana";
    private const string Raposo = "retro_raposo";
    private const string Leo = "lowfps_leo";

    private const string ClairObscur = "Clair Obscur: Expedition 33";
    private const string Hades2 = "Hades II";
    private const string Kcd2 = "Kingdom Come: Deliverance II";
    private const string Silksong = "Hollow Knight: Silksong";
    private const string BluePrince = "Blue Prince";

    /// <summary>Gets the accounts.</summary>
    public static IReadOnlyList<DemoUser> Users { get; } =
    [
        new(Pixel, "Turn-based everything: JRPGs, tactics, the odd rhythm game. Plays with a notebook open.", "Porto", "violet", 420),
        new(Sofia, "Jogos curtos, bandas sonoras longas. Quase tudo na Switch, quase sempre no comboio.", "Lisboa", "rose", 380),
        new(Nuno, "Open worlds, immersive sims and falling through the floor of both.", "Coimbra", "emerald", 300),
        new(Ana, "Roguelikes and card games. One more run, then bed. Then one more run.", "Braga", "gold", 260),
        new(Raposo, "Final Fantasy since the SNES. Ranks everything, regrets some of it.", "Faro", "ember", 500),
        new(Leo, "Plays everything on a laptop that really shouldn't run it.", "Madrid", "blue", 200),
    ];

    /// <summary>Gets the libraries.</summary>
    public static IReadOnlyList<DemoEntry> Entries { get; } =
    [
        new(Pixel, ClairObscur, GameStatus.Finished, 10, 61m, "PC", GameEnding.Completed, 2),
        new(Pixel, "Metaphor: ReFantazio", GameStatus.Finished, 9, 80m, "PS5", GameEnding.Completed, 40),
        new(Pixel, "Persona 5 Royal", GameStatus.Finished, 10, 120m, "PS5", GameEnding.Mastered, 300),
        new(Pixel, "Sea of Stars", GameStatus.Finished, 7, 30m, "PC", GameEnding.Completed, 200),
        new(Pixel, "Baldur's Gate 3", GameStatus.Finished, 10, 140m, "PC", GameEnding.Completed, 380),
        new(Pixel, "Final Fantasy XVI", GameStatus.Finished, 7, 55m, "PS5", GameEnding.Completed, 500),
        new(Pixel, "Tactical Breach Wizards", GameStatus.Finished, 9, 18m, "PC", GameEnding.Completed, 25),
        new(Pixel, "Citizen Sleeper 2: Starward Vector", GameStatus.Finished, 9, 11m, "PC", GameEnding.Completed, 22),
        new(Pixel, BluePrince, GameStatus.Finished, 9, 40m, "PC", GameEnding.Completed, 6),
        new(Pixel, Silksong, GameStatus.Playing, null, 22m, "PC", null, 1),
        new(Pixel, Hades2, GameStatus.Backlog, null, null, "PC", null, 10),

        new(Sofia, Hades2, GameStatus.Finished, 9, 61m, "Switch 2", GameEnding.Mastered, 1),
        new(Sofia, "A Short Hike", GameStatus.Finished, 9, 3m, "Switch", GameEnding.Completed, 120),
        new(Sofia, "Unpacking", GameStatus.Finished, 8, 4m, "PC", GameEnding.Completed, 150),
        new(Sofia, "Cocoon", GameStatus.Finished, 9, 5m, "PC", GameEnding.Completed, 60),
        new(Sofia, "Mouthwashing", GameStatus.Finished, 8, 2.5m, "PC", GameEnding.Completed, 30),
        new(Sofia, "Celeste", GameStatus.Finished, 10, 20m, "Switch", GameEnding.Mastered, 400),
        new(Sofia, "Animal Well", GameStatus.Finished, 9, 12m, "Switch", GameEnding.Completed, 90),
        new(Sofia, "Astro Bot", GameStatus.Finished, 10, 15m, "PS5", GameEnding.Mastered, 70),
        new(Sofia, "Stardew Valley", GameStatus.Played, 9, 200m, "Switch", null, 500),
        new(Sofia, "Chants of Sennaar", GameStatus.Finished, 8, 9m, "Switch", GameEnding.Completed, 45),
        new(Sofia, ClairObscur, GameStatus.Playing, null, 18m, "PS5", null, 3),

        new(Nuno, Kcd2, GameStatus.Finished, 8, 95m, "PS5", GameEnding.Completed, 4),
        new(Nuno, "Elden Ring", GameStatus.Finished, 10, 130m, "PS5", GameEnding.Completed, 600),
        new(Nuno, "Red Dead Redemption 2", GameStatus.Finished, 10, 90m, "PS5", GameEnding.Completed, 900),
        new(Nuno, "Cyberpunk 2077", GameStatus.Finished, 8, 70m, "PC", GameEnding.Completed, 400),
        new(Nuno, "Death Stranding 2: On the Beach", GameStatus.Finished, 7, 55m, "PS5", GameEnding.Completed, 9),
        new(Nuno, "Ghost of Yōtei", GameStatus.Playing, null, 30m, "PS5", null, 0),
        new(Nuno, "Stardew Valley", GameStatus.Played, 8, 60m, "PC", null, 300),
        new(Nuno, "Dave the Diver", GameStatus.Finished, 8, 25m, "PC", GameEnding.Completed, 200),
        new(Nuno, "Chants of Sennaar", GameStatus.Finished, 9, 10m, "PC", GameEnding.Completed, 50),
        new(Nuno, "Pentiment", GameStatus.Finished, 9, 18m, "PC", GameEnding.Completed, 100),
        new(Nuno, "The Witcher 3: Wild Hunt", GameStatus.Finished, 10, 150m, "PC", GameEnding.Mastered, 1200),
        new(Nuno, "Monster Hunter Wilds", GameStatus.Dropped, 5, 20m, "PS5", GameEnding.Shelved, 180),

        new(Ana, "Balatro", GameStatus.Played, 7, 140m, "PC", null, 3),
        new(Ana, "Slay the Spire", GameStatus.Played, 10, 300m, "PC", null, 700),
        new(Ana, "Inscryption", GameStatus.Finished, 9, 14m, "PC", GameEnding.Completed, 500),
        new(Ana, "Vampire Survivors", GameStatus.Played, 7, 40m, "PC", null, 400),
        new(Ana, "Outer Wilds", GameStatus.Finished, 10, 22m, "PC", GameEnding.Completed, 650),
        new(Ana, "Disco Elysium", GameStatus.Finished, 10, 35m, "PC", GameEnding.Completed, 800),
        new(Ana, "Pentiment", GameStatus.Finished, 8, 17m, "PC", GameEnding.Completed, 120),
        new(Ana, "Mouthwashing", GameStatus.Finished, 9, 2m, "PC", GameEnding.Completed, 35),
        new(Ana, "Hades", GameStatus.Finished, 9, 70m, "PC", GameEnding.Mastered, 900),
        new(Ana, Hades2, GameStatus.Playing, null, 25m, "PC", null, 2),
        new(Ana, BluePrince, GameStatus.Dropped, 6, 12m, "PC", GameEnding.Shelved, 8),
        new(Ana, "UFO 50", GameStatus.Played, 9, 45m, "PC", null, 60),
        new(Ana, "Lorelei and the Laser Eyes", GameStatus.Finished, 9, 16m, "PC", GameEnding.Completed, 75),

        new(Raposo, "Final Fantasy IX", GameStatus.Finished, 10, 50m, "PC", GameEnding.Mastered, 400),
        new(Raposo, "Final Fantasy VI", GameStatus.Finished, 10, 35m, "PC", GameEnding.Completed, 300),
        new(Raposo, "Final Fantasy X", GameStatus.Finished, 9, 60m, "PS4", GameEnding.Completed, 700),
        new(Raposo, "Final Fantasy VII", GameStatus.Finished, 9, 45m, "PC", GameEnding.Completed, 800),
        new(Raposo, "Final Fantasy XIV Online", GameStatus.Played, 9, 900m, "PC", null, 30),
        new(Raposo, "Final Fantasy XVI", GameStatus.Dropped, 6, 40m, "PS5", GameEnding.Shelved, 15),
        new(Raposo, "Final Fantasy XII", GameStatus.Finished, 8, 70m, "PS4", GameEnding.Completed, 900),
        new(Raposo, "Final Fantasy IV", GameStatus.Finished, 8, 25m, "PC", GameEnding.Completed, 600),
        new(Raposo, "Final Fantasy V", GameStatus.Finished, 8, 30m, "PC", GameEnding.Completed, 550),
        new(Raposo, "Final Fantasy VIII", GameStatus.Finished, 7, 50m, "PC", GameEnding.Completed, 1000),
        new(Raposo, "Final Fantasy XV", GameStatus.Finished, 6, 45m, "PS4", GameEnding.Completed, 1100),
        new(Raposo, "Final Fantasy III", GameStatus.Finished, 7, 22m, "PC", GameEnding.Completed, 650),
        new(Raposo, "Final Fantasy XIII", GameStatus.Finished, 6, 50m, "PC", GameEnding.Completed, 1200),
        new(Raposo, "Final Fantasy XI Online", GameStatus.Played, 6, 120m, "PC", null, 1300),
        new(Raposo, "Final Fantasy", GameStatus.Finished, 6, 15m, "PC", GameEnding.Completed, 700),
        new(Raposo, "Final Fantasy II", GameStatus.Finished, 5, 18m, "PC", GameEnding.Completed, 690),
        new(Raposo, "Sea of Stars", GameStatus.Finished, 8, 32m, "PC", GameEnding.Completed, 180),
        new(Raposo, ClairObscur, GameStatus.Finished, 9, 64m, "PC", GameEnding.Completed, 14),

        new(Leo, Hades2, GameStatus.Finished, 10, 75m, "PC", GameEnding.Mastered, 4),
        new(Leo, "Hades", GameStatus.Finished, 9, 60m, "PC", GameEnding.Completed, 700),
        new(Leo, "Balatro", GameStatus.Played, 9, 200m, "PC", null, 100),
        new(Leo, "Slay the Spire", GameStatus.Played, 9, 150m, "PC", null, 600),
        new(Leo, "Vampire Survivors", GameStatus.Played, 8, 50m, "PC", null, 450),
        new(Leo, "UFO 50", GameStatus.Played, 9, 60m, "PC", null, 50),
        new(Leo, "Lorelei and the Laser Eyes", GameStatus.Finished, 8, 14m, "PC", GameEnding.Completed, 80),
        new(Leo, "Citizen Sleeper 2: Starward Vector", GameStatus.Finished, 9, 12m, "PC", GameEnding.Completed, 20),
        new(Leo, ClairObscur, GameStatus.Finished, 9, 58m, "PC", GameEnding.Completed, 12),
        new(Leo, Silksong, GameStatus.Playing, null, 30m, "PC", null, 1),
        new(Leo, "Tactical Breach Wizards", GameStatus.Finished, 8, 16m, "PC", GameEnding.Completed, 30),
        new(Leo, "Prince of Persia: The Lost Crown", GameStatus.Finished, 8, 25m, "PC", GameEnding.Completed, 200),
        new(Leo, "Tunic", GameStatus.Finished, 9, 20m, "PC", GameEnding.Completed, 350),
    ];

    /// <summary>Gets the reviews.</summary>
    public static IReadOnlyList<DemoReview> Reviews { get; } =
    [
        new(Pixel, ClairObscur, 2,
            "I did not expect a turn-based game to have me parrying like it was Sekiro. Every enemy turn is a tiny rhythm game, and by the second act I was dodging on instinct, eyes half on the animation and half on the music.\n\nThe music alone earns four stars. The last act earns the fifth: it takes the story somewhere I did not see coming and then trusts you to sit with it.\n\nIf you bounced off JRPGs years ago because of slow menus and random battles, this is the one that brings you back.",
            [Sofia, Nuno, Leo, Raposo, Ana]),
        new(Pixel, BluePrince, 6,
            "Every room is a little puzzle box and the house remembers what you did. I kept a paper notebook for the first time in years. The last stretch leans hard on note-taking; if you love that, there is nothing else like it.",
            [Ana, Leo, Sofia]),
        new(Pixel, "Metaphor: ReFantazio", 40,
            "Atlus took the Persona calendar and gave it a fantasy election to care about. The archetypes let you rebuild the party every few hours and the chanting in the soundtrack never gets old. The first ten hours are slow; after that it doesn't let go.",
            [Raposo, Sofia]),
        new(Pixel, "Tactical Breach Wizards", 25,
            "Tactics with jokes that land. Every turn is a puzzle about throwing someone out of a window, and you can rewind freely, so you always try the silly plan first. Short, sharp, no filler.",
            [Leo, Ana, Nuno]),
        new(Sofia, Hades2, 1,
            "Mais do que uma sequela: é o primeiro jogo, mas com mais confiança. A Melinoë joga-se de forma completamente diferente do Zagreus e a magia dá outra camada a cada run.\n\nJoguei-o quase todo no comboio, em runs de vinte minutos, e funciona na perfeição. Só tirei meia estrela porque o final chegou mais depressa do que eu queria.",
            [Leo, Ana, Pixel, Nuno]),
        new(Sofia, "A Short Hike", 120,
            "Três horas, uma montanha e um telemóvel sem rede. Não há pressa nenhuma e é exatamente isso que o torna especial. Joguei numa tarde de chuva e saí com vontade de ir para a serra.",
            [Nuno, Pixel]),
        new(Sofia, "Astro Bot", 70,
            "Cada nível tem uma ideia nova e larga-a antes de ficar repetida. O DualSense finalmente faz sentido. Fiz os 100% e ainda queria mais.",
            [Pixel, Leo, Raposo]),
        new(Sofia, "Mouthwashing", 30,
            "Duas horas e meia que não me saem da cabeça. Não conto nada: joguem às escuras, de noite e com auscultadores.",
            [Ana, Nuno]),
        new(Nuno, Kcd2, 4,
            "Spent three hours failing to pick a lock and the next ninety loving every minute. Henry is still hopeless at the start, which is the point: by the end you've earned every skill.\n\nThe quests branch in ways I only found out about from friends. A point off for the bug that ate a whole side quest line.",
            [Raposo, Pixel, Ana]),
        new(Nuno, "Death Stranding 2: On the Beach", 9,
            "Gorgeous, stranger than the first and much friendlier: more weapons, more vehicles, fewer long walks. I missed the loneliness that made the original special. Still the only game where I plan a delivery route like a holiday.",
            [Leo, Sofia]),
        new(Nuno, "Chants of Sennaar", 50,
            "A whole game about learning languages by watching people point at things. The moment two peoples finally understand each other because of your notebook is one of my favourite moments in years.",
            [Sofia, Ana, Pixel]),
        new(Nuno, "Monster Hunter Wilds", 180,
            "The hunts are great when they work. On my PS5 it ran rough, the story kept stopping me to talk, and twenty hours in I hadn't had a fight I'll remember. Shelved until a few more patches.",
            [Leo]),
        new(Ana, "Balatro", 3,
            "\"One more run\" is a lie I tell myself every night. Brilliant design. I knocked a star and a half off because the last stakes come down to luck, and I hate how much I still love it.",
            [Leo, Pixel, Sofia, Nuno]),
        new(Ana, "Outer Wilds", 650,
            "Don't look anything up. That's the review.\n\nOkay, one more line: the best ending I've played, and you earn it with nothing but curiosity.",
            [Pixel, Sofia, Nuno, Leo, Raposo]),
        new(Ana, BluePrince, 8,
            "I loved the first twenty days in the house. Then the luck of the draw started to decide more than I did, and I put it down. I'll be back, probably with a spreadsheet.",
            [Nuno]),
        new(Ana, "Lorelei and the Laser Eyes", 75,
            "A puzzle hotel in black, white and red. Bring a notebook and a lot of patience: the 'aha' moments are the biggest I've had since Outer Wilds.",
            [Leo, Pixel]),
        new(Raposo, "Final Fantasy IX", 400,
            "My first Final Fantasy and still the best. Vivi's story, about being made for one thing and choosing your own reason to live, hit me at twelve and hits harder now. The card game is still terrible and I still played every match.",
            [Pixel, Sofia, Leo]),
        new(Raposo, "Final Fantasy XVI", 15,
            "I wanted to love it as much as the old ones. The Eikon fights are incredible, but the side quests are errands and I never settled into the combat. Forty hours in, I realised I was playing out of loyalty. Not a bad game, just not my Final Fantasy.",
            [Nuno, Ana]),
        new(Raposo, "Final Fantasy VI", 300,
            "Uma ópera a meio do jogo, um vilão que ganha mesmo e um mundo em ruínas que temos de reconstruir aos bocados. Para mim continua a ser o melhor argumento da série.",
            [Pixel, Sofia]),
        new(Leo, Hades2, 4,
            "The last night run reframes the whole family story, and the true ending had me sitting through the credits. Melinoë and Chronos's final scene is the best writing Supergiant has done.",
            [Sofia, Ana, Pixel], Spoilers: true),
        new(Leo, "UFO 50", 50,
            "Fifty fake 8-bit games and at least ten of them are better than real ones. Party House alone is worth it. Runs at 60 fps on my ten-year-old laptop, which is the highest praise I can give.",
            [Ana, Pixel, Raposo]),
        new(Leo, "Citizen Sleeper 2: Starward Vector", 20,
            "Dice, stress and a crew you start to care about more than yourself. Tighter than the first one and still quietly kind. Ran fine at 30 fps; nothing here needs more.",
            [Pixel, Nuno]),
        new(Leo, ClairObscur, 12,
            "Turned everything down to low and it still looks better than most games on high. The parry timing is brutal at first. The music deserves every award it gets.",
            [Pixel, Raposo]),
    ];

    /// <summary>Gets the comments.</summary>
    public static IReadOnlyList<DemoComment> Comments { get; } =
    [
        new(Sofia, Pixel, ClairObscur, 2, "O momento em que a música do último ato entra... tive de pousar o comando um minuto."),
        new(Nuno, Pixel, ClairObscur, 2, "Did you go back for the optional bosses? The one in the flower field nearly broke me."),
        new(Pixel, Pixel, ClairObscur, 1, "@noclip_nuno only two of them. Saving the rest for a second run with a different party."),
        new(Leo, Pixel, ClairObscur, 1, "Agreed on the parrying. I widened the dodge window a notch and it finally clicked."),
        new(Leo, Sofia, Hades2, 1, "Same on the ending, though the night runs make up for it."),
        new(Ana, Sofia, Hades2, 0, "Which weapon did you beat Chronos with? I'm stuck on him with the staff."),
        new(Sofia, Sofia, Hades2, 0, "@ctrlaltdel_ana as lâminas duplas, com o aspecto do segundo martelo. Vale a pena!"),
        new(Leo, Ana, "Balatro", 2, "Gold stake or bust."),
        new(Pixel, Raposo, "Final Fantasy XVI", 14, "The Titan fight alone deserves half a star more!"),
        new(Raposo, Raposo, "Final Fantasy XVI", 13, "Fair. Half a star for Titan, then."),
        new(Ana, Nuno, Kcd2, 3, "Did you sleep in a real bed before the big battle? Not saving cost me two hours."),
        new(Nuno, Ana, "Outer Wilds", 600, "Came here to say the same thing. Don't look it up."),
    ];

    /// <summary>Gets the lists.</summary>
    public static IReadOnlyList<DemoList> Lists { get; } =
    [
        new(Raposo, "Every Final Fantasy, ranked",
            "The numbered games only, from the one I'd replay tomorrow to the one I finished out of stubbornness. The MMOs count. Remakes count as their original.",
            true, 15,
            ["Final Fantasy IX", "Final Fantasy VI", "Final Fantasy X", "Final Fantasy VII", "Final Fantasy XIV Online", "Final Fantasy XII", "Final Fantasy IV", "Final Fantasy V",
             "Final Fantasy VIII", "Final Fantasy XVI", "Final Fantasy III", "Final Fantasy XV", "Final Fantasy XIII", "Final Fantasy XI Online", "Final Fantasy", "Final Fantasy II"],
            [Pixel, Sofia, Nuno, Leo]),
        new(Sofia, "Curtos mas bons (menos de 6 horas)",
            "Para quem tem pouco tempo e quer acabar alguma coisa esta semana.",
            false, 20,
            ["A Short Hike", "Unpacking", "Cocoon", "Mouthwashing", "Celeste"],
            [Ana, Nuno, Pixel]),
        new(Nuno, "Cosy winter",
            "Blanket, tea, rain on the window. Nothing here will stress you out.",
            false, 40,
            ["Stardew Valley", "A Short Hike", "Dave the Diver", "Unpacking", "Astro Bot", "Sea of Stars"],
            [Sofia]),
        new(Leo, "Roguelikes worth 100 hours",
            "Every one of these ran on my laptop, and every one of them ate a month.",
            true, 6,
            ["Slay the Spire", "Hades", Hades2, "Balatro", "Vampire Survivors", "UFO 50"],
            [Ana, Pixel]),
        new(Ana, "Games that made me put the controller down",
            "Not because they were bad. Because I needed a minute.",
            false, 35,
            ["Outer Wilds", "Disco Elysium", "Mouthwashing", "Pentiment", "Final Fantasy IX"],
            [Sofia, Raposo, Leo]),
        new(Pixel, "Best of 2025 (so far)",
            "Updated as I go. Ranked by how much I think about them on the bus.",
            true, 2,
            [ClairObscur, BluePrince, "Citizen Sleeper 2: Starward Vector", Kcd2, Silksong, "Death Stranding 2: On the Beach"],
            [Leo, Nuno, Sofia]),
    ];

    /// <summary>Gets the journal sessions.</summary>
    public static IReadOnlyList<DemoSession> Sessions { get; } =
    [
        new(Pixel, Silksong, 1, 95, "Second act. The bell town, at last."),
        new(Pixel, Silksong, 3, 120, "Stuck on the first big boss, then suddenly not."),
        new(Pixel, Silksong, 5, 60),
        new(Pixel, ClairObscur, 13, 180, "The last act."),
        new(Pixel, ClairObscur, 14, 150),
        new(Pixel, BluePrince, 7, 110, "Room 46?"),
        new(Sofia, Hades2, 1, 100, "Créditos! 100% na última run."),
        new(Sofia, Hades2, 4, 55, "Duas runs curtas no comboio."),
        new(Sofia, Hades2, 9, 70),
        new(Sofia, ClairObscur, 3, 90, "Lumière!"),
        new(Sofia, ClairObscur, 6, 60),
        new(Nuno, "Ghost of Yōtei", 0, 135, "Took the long way round the lake."),
        new(Nuno, "Ghost of Yōtei", 2, 120),
        new(Nuno, "Ghost of Yōtei", 5, 210, "Long Saturday session."),
        new(Nuno, Kcd2, 4, 150, "Finally the ending."),
        new(Nuno, Kcd2, 7, 180),
        new(Ana, Hades2, 2, 75),
        new(Ana, Hades2, 6, 40, "Night run with the staff."),
        new(Ana, "Balatro", 3, 55, "Gold stake, finally."),
        new(Ana, "Balatro", 8, 40),
        new(Leo, Silksong, 1, 65),
        new(Leo, Silksong, 4, 80, "Hornet moves so fast my laptop fans gave up."),
        new(Leo, Hades2, 4, 120, "True ending."),
        new(Leo, Hades2, 10, 90),
        new(Raposo, "Final Fantasy XIV Online", 2, 140, "Raid night."),
        new(Raposo, "Final Fantasy XIV Online", 6, 95),
    ];

    /// <summary>Every game the demo uses, by name.</summary>
    /// <returns>The names, each once.</returns>
    public static List<string> GameNames() =>
        [.. Entries.Select(static e => e.Game).Concat(Lists.SelectMany(static l => l.Games)).Distinct(StringComparer.Ordinal)];
}
