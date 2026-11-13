namespace Ludex.WebApi.Features.Playnite.Services;

/// <summary>
/// The column names Library Exporter Advanced writes, in every language Playnite has (the exporter names its columns
/// with Playnite's own translations). Taken from Playnite's Localization folder.
/// </summary>
internal static class PlayniteHeaders
{
    /// <summary>The game's name (always the first column).</summary>
    public static readonly HashSet<string> Name = new(StringComparer.OrdinalIgnoreCase)
    {
        "Jméno", "Naam", "Nama", "Name", "Namn", "Navn",
        "Naziv", "Nazwa", "Nimi", "Nom", "Nombre", "Nome",
        "Nume", "Názov", "Név", "Pavadinimas", "Tên", "İsim",
        "Όνομα", "Име", "Назва", "Название", "Назив", "שם",
        "الاسم", "نام", "नाव", "タイトル", "名称", "名稱",
        "게임 이름",
    };

    /// <summary>Where Playnite got the game (Steam, Epic, GOG…).</summary>
    public static readonly HashSet<string> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        "Allikad", "Bronnen", "Fontes", "Fonts", "Források", "Fuentes",
        "Izvori", "Kaynaklar", "Kilder", "Källor", "Lähteet", "Nguồn",
        "Orixes", "Quellen", "Sorgenti", "Sources", "Sumber", "Surse",
        "Zdroje", "Šaltiniai", "Źródła", "Πηγές", "Джерела", "Извори",
        "Източници", "Источники", "מקורות", "المصادر", "منابع", "स्रोत",
        "ソース", "來源", "来源", "소스",
    };

    /// <summary>The platforms.</summary>
    public static readonly HashSet<string> Platforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alustat", "Các nền tảng", "Piattaforme", "Plataformas", "Plataformes", "Plateformes",
        "Platform", "Platforme", "Platformlar", "Platformok", "Platformos", "Platforms",
        "Platformy", "Plattformar", "Plattformen", "Plattformer", "Platvormid", "Πλατφόρμες",
        "Платформе", "Платформи", "Платформы", "פלטפורמות", "المنصات", "سکوها",
        "प्लॅटफॉर्म", "プラットフォーム", "平台", "플랫폼",
    };

    /// <summary>The time played, in seconds.</summary>
    public static readonly HashSet<string> TimePlayed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Czas gry", "Iš viso žaista", "Játékidő", "Mängitud aeg", "Odehraný Čas", "Odohraný čas",
        "Oynama Süresi", "Pelattu aika", "Speltid", "Spilletid", "Tempo Jogado", "Tempo de jogo",
        "Tempo di gioco", "Tempo xogado", "Temps de jeu", "Temps jugat", "Thời gian chơi", "Tid spillet",
        "Tiempo jugado", "Tijd Gespeeld", "Time Played", "Timpul Jucat", "Vreme igranja", "Waktu Dimainkan",
        "Zeit gespielt", "Χρόνος παιχνιδιού", "Време играња", "Времени в игре", "Изиграно време", "Часу зіграно",
        "משך המשחק", "زمان بازی‌شده", "وقت اللعب", "खेळल्याची कालावधी", "プレイ時間", "游玩时间",
        "遊玩時數", "플레이 시간",
    };

    /// <summary>Playnite's completion status.</summary>
    public static readonly HashSet<string> CompletionStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "Befejezettség", "Completion Status", "Edistyminen", "Estado de Conclusão", "Estado de conclusão", "Estado de finalización",
        "Estat de finalització", "Fullføringsstatus", "Lõpetamisolek", "Penyelesaian", "Progression", "Spielfortschritt",
        "Stare de Finalizare", "Stato di completamento", "Status för avklarande", "Status završenosti", "Stav Dokončení", "Stav dokončenia",
        "Stopień ukończenia", "Tamamlanma Durumu", "Trạng thái", "Užbaigimo būsena", "Voltooiingsstatus", "Κατάσταση ολοκλήρωσης",
        "Статус", "Статус завершення", "Статус завршености", "Статус на завършване", "סטטוס השלמה", "حالة الاكمال",
        "وضعیت تکمیلی", "पूर्ण केल्याची स्थिती", "プレイ状況", "完成状态", "完成狀態", "완료 상태",
    };

    /// <summary>When it was last played.</summary>
    public static readonly HashSet<string> LastPlayed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aktivitas terakhir", "Dernière activité", "Giocato l'ultima volta", "Jugado por última vez", "Laatst Gespeeld", "Last Played",
        "Lần chơi cuối", "Naposledy Hráno", "Naposledy hrané", "Ostatnio uruchomiono", "Paskutinį kartą žaista", "Posljednji put igrano",
        "Senast Aktivitet", "Sidst spillet", "Sist spilt", "Son Oynanma", "Ultima Dată Jucat", "Utoljára játszva",
        "Viimati mängitud", "Viimeksi pelattu", "Zuletzt gespielt", "Última partida", "Última sessão", "Última sessão de jogo",
        "Última vez xogada", "Παίχτηκαν τελευταία", "Востаннє зіграно", "Последний запуск", "Последно играно", "Последњи пут играно",
        "שוחק לאחרונה", "آخرین بازی شده", "اخر تشغيل", "याआधी खेळलेले", "上次游玩", "最後のプレイ",
        "最後遊玩", "최근 플레이",
    };

    /// <summary>Playnite's id of the game.</summary>
    public static readonly HashSet<string> GameId = new(StringComparer.OrdinalIgnoreCase)
    {
        "Game Id", "ID Game", "ID Hry", "ID Igara", "ID Trò chơi", "ID do Jogo",
        "ID gry", "ID hry", "ID Παιχνιδιού", "ID игре", "ID игры", "Id de joc",
        "Id del gioco", "Id del joc", "Id del juego", "Id do Xogo", "Id do jogo", "Id jeu",
        "Játék azonosító", "Mängu id", "Oyun Kimliği", "Pelin tunnus", "Spel Id", "Spel-id",
        "Spiel-ID", "Spil ID", "Spill-id", "Žaidimo Id", "Ігровий Id", "Идентификационен номер на играта",
        "מזהה המשחק", "شناسه‌ی بازی", "معرف اللعبة (ID)", "गेम आय.डी.", "ゲーム ID", "游戏 ID",
        "遊戲 Id", "게임 Id",
    };
}
