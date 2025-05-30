using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace LoLAnalyzer.Core.Data
{
    /// <summary>
    /// Gestionnaire de base de données enrichie pour LoL Analyzer
    /// </summary>
    public class EnhancedDatabaseManager
    {
        private readonly string _dbPath;
        private readonly Core.Utils.Logger _logger;
        private SQLiteConnection _connection;

        /// <summary>
        /// Constructeur du gestionnaire de base de données enrichie
        /// </summary>
        /// <param name="dbPath">Chemin vers le fichier de base de données SQLite</param>
        /// <param name="logger">Logger pour tracer les opérations</param>
        public EnhancedDatabaseManager(string dbPath, Core.Utils.Logger logger)
        {
            _dbPath = dbPath;
            _logger = logger;
        }

        /// <summary>
        /// Initialise la connexion à la base de données
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.Info($"Initialisation de la base de données enrichie: {_dbPath}");

                // Vérifier si le fichier de base de données existe
                bool dbExists = File.Exists(_dbPath);

                // Créer le répertoire parent si nécessaire
                string directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Créer la chaîne de connexion
                string connectionString = $"Data Source={_dbPath};Version=3;";
                _connection = new SQLiteConnection(connectionString);
                await _connection.OpenAsync();

                // Si la base de données n'existait pas, créer les tables
                if (!dbExists)
                {
                    await CreateTablesAsync();
                    await PopulateInitialDataAsync();
                }

                _logger.Info("Base de données enrichie initialisée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(Core.Utils.LogLevel.Error, "Erreur lors de l'initialisation de la base de données enrichie", ex);
                throw;
            }
        }

        /// <summary>
        /// Crée les tables de la base de données
        /// </summary>
        private async Task CreateTablesAsync()
        {
            _logger.Info("Création des tables de la base de données enrichie");

            // Table des champions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE Champions (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    ImageUrl TEXT NOT NULL,
                    Tags TEXT NOT NULL,
                    Stats TEXT NOT NULL
                )
            ");

            // Table des rôles de champions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionRoles (
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    WinRate REAL NOT NULL,
                    PickRate REAL NOT NULL,
                    BanRate REAL NOT NULL,
                    TierRank TEXT NOT NULL,
                    PRIMARY KEY (ChampionId, Role),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )
            ");

            // Table des synergies entre champions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionSynergies (
                    Champion1Id INTEGER NOT NULL,
                    Champion2Id INTEGER NOT NULL,
                    SynergyScore REAL NOT NULL,
                    WinRateIncrease REAL NOT NULL,
                    Description TEXT NOT NULL,
                    PRIMARY KEY (Champion1Id, Champion2Id),
                    FOREIGN KEY (Champion1Id) REFERENCES Champions(Id),
                    FOREIGN KEY (Champion2Id) REFERENCES Champions(Id)
                )
            ");

            // Table des contre-picks
            await ExecuteNonQueryAsync(@"
                CREATE TABLE CounterPicks (
                    ChampionId INTEGER NOT NULL,
                    CounterChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    CounterScore REAL NOT NULL,
                    WinRate REAL NOT NULL,
                    Difficulty TEXT NOT NULL,
                    Explanation TEXT NOT NULL,
                    PRIMARY KEY (ChampionId, CounterChampionId, Role),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id),
                    FOREIGN KEY (CounterChampionId) REFERENCES Champions(Id)
                )
            ");

            // Table des compositions méta
            await ExecuteNonQueryAsync(@"
                CREATE TABLE MetaCompositions (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Patch TEXT NOT NULL,
                    Tier TEXT NOT NULL,
                    WinRate REAL NOT NULL,
                    PlayStyle TEXT NOT NULL,
                    Difficulty TEXT NOT NULL
                )
            ");

            // Table des champions dans les compositions méta
            await ExecuteNonQueryAsync(@"
                CREATE TABLE MetaCompositionChampions (
                    CompositionId INTEGER NOT NULL,
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    PRIMARY KEY (CompositionId, ChampionId),
                    FOREIGN KEY (CompositionId) REFERENCES MetaCompositions(Id),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )
            ");

            // Table des forces et faiblesses des compositions méta
            await ExecuteNonQueryAsync(@"
                CREATE TABLE MetaCompositionAttributes (
                    CompositionId INTEGER NOT NULL,
                    AttributeType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    PRIMARY KEY (CompositionId, AttributeType, Description),
                    FOREIGN KEY (CompositionId) REFERENCES MetaCompositions(Id)
                )
            ");

            // Table des picks problématiques
            await ExecuteNonQueryAsync(@"
                CREATE TABLE TrollPicks (
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    WinRate REAL NOT NULL,
                    Reason TEXT NOT NULL,
                    PRIMARY KEY (ChampionId, Role),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )
            ");

            // Table des alternatives aux picks problématiques
            await ExecuteNonQueryAsync(@"
                CREATE TABLE TrollPickAlternatives (
                    TrollChampionId INTEGER NOT NULL,
                    TrollRole TEXT NOT NULL,
                    AlternativeChampionId INTEGER NOT NULL,
                    RecommendationScore REAL NOT NULL,
                    PRIMARY KEY (TrollChampionId, TrollRole, AlternativeChampionId),
                    FOREIGN KEY (TrollChampionId) REFERENCES Champions(Id),
                    FOREIGN KEY (AlternativeChampionId) REFERENCES Champions(Id)
                )
            ");

            // Table des statistiques par phase de jeu
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionPhaseStats (
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Phase TEXT NOT NULL,
                    PerformanceScore REAL NOT NULL,
                    PRIMARY KEY (ChampionId, Role, Phase),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )
            ");

            _logger.Info("Tables de la base de données enrichie créées avec succès");
        }

        /// <summary>
        /// Remplit la base de données avec des données initiales
        /// </summary>
        private async Task PopulateInitialDataAsync()
        {
            _logger.Info("Remplissage de la base de données enrichie avec des données initiales");

            // Insérer quelques champions (données de base)
            await InsertChampionsAsync();

            // Insérer les rôles de champions
            await InsertChampionRolesAsync();

            // Insérer les synergies entre champions
            await InsertChampionSynergiesAsync();

            // Insérer les contre-picks
            await InsertCounterPicksAsync();

            // Insérer les compositions méta
            await InsertMetaCompositionsAsync();

            // Insérer les picks problématiques
            await InsertTrollPicksAsync();

            // Insérer les statistiques par phase de jeu
            await InsertPhaseStatsAsync();

            _logger.Info("Données initiales insérées avec succès dans la base de données enrichie");
        }

        /// <summary>
        /// Insère les données de champions dans la base de données
        /// </summary>
        private async Task InsertChampionsAsync()
        {
            // Liste des champions populaires avec leurs données
            var champions = new List<(int Id, string Name, string Title, string ImageUrl, string Tags, string Stats)>
            {
                (1, "Ahri", "The Nine-Tailed Fox", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ahri.png", "Mage,Assassin", "{\"hp\":526,\"hpperlevel\":92,\"mp\":418,\"mpperlevel\":25,\"movespeed\":330,\"armor\":20.88,\"armorperlevel\":3.5,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":550,\"hpregen\":5.5,\"hpregenperlevel\":0.6,\"mpregen\":8,\"mpregenperlevel\":0.8,\"crit\":0,\"critperlevel\":0,\"attackdamage\":53.04,\"attackdamageperlevel\":3,\"attackspeedperlevel\":2,\"attackspeed\":0.668}"),
                (2, "Amumu", "The Sad Mummy", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Amumu.png", "Tank,Mage", "{\"hp\":615,\"hpperlevel\":80,\"mp\":285,\"mpperlevel\":40,\"movespeed\":335,\"armor\":30,\"armorperlevel\":3.8,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":9,\"hpregenperlevel\":0.85,\"mpregen\":7.38,\"mpregenperlevel\":0.53,\"crit\":0,\"critperlevel\":0,\"attackdamage\":53.38,\"attackdamageperlevel\":3.8,\"attackspeedperlevel\":2.18,\"attackspeed\":0.736}"),
                (3, "Darius", "The Hand of Noxus", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Darius.png", "Fighter,Tank", "{\"hp\":582.24,\"hpperlevel\":100,\"mp\":263,\"mpperlevel\":37.5,\"movespeed\":340,\"armor\":39,\"armorperlevel\":4,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":175,\"hpregen\":10,\"hpregenperlevel\":0.95,\"mpregen\":6.6,\"mpregenperlevel\":0.35,\"crit\":0,\"critperlevel\":0,\"attackdamage\":64,\"attackdamageperlevel\":5,\"attackspeedperlevel\":1,\"attackspeed\":0.625}"),
                (4, "Ezreal", "The Prodigal Explorer", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ezreal.png", "Marksman,Mage", "{\"hp\":530,\"hpperlevel\":88,\"mp\":375,\"mpperlevel\":70,\"movespeed\":325,\"armor\":22,\"armorperlevel\":3.5,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":550,\"hpregen\":4,\"hpregenperlevel\":0.55,\"mpregen\":8.5,\"mpregenperlevel\":0.65,\"crit\":0,\"critperlevel\":0,\"attackdamage\":60,\"attackdamageperlevel\":2.5,\"attackspeedperlevel\":2.8,\"attackspeed\":0.625}"),
                (5, "Fiora", "The Grand Duelist", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Fiora.png", "Fighter,Assassin", "{\"hp\":550,\"hpperlevel\":85,\"mp\":300,\"mpperlevel\":40,\"movespeed\":345,\"armor\":33,\"armorperlevel\":3.5,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":150,\"hpregen\":8.5,\"hpregenperlevel\":0.55,\"mpregen\":8,\"mpregenperlevel\":0.7,\"crit\":0,\"critperlevel\":0,\"attackdamage\":68,\"attackdamageperlevel\":3.3,\"attackspeedperlevel\":3.2,\"attackspeed\":0.69}"),
                (6, "Garen", "The Might of Demacia", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Garen.png", "Fighter,Tank", "{\"hp\":620,\"hpperlevel\":84,\"mp\":0,\"mpperlevel\":0,\"movespeed\":340,\"armor\":36,\"armorperlevel\":3,\"spellblock\":32.1,\"spellblockperlevel\":0.75,\"attackrange\":175,\"hpregen\":8,\"hpregenperlevel\":0.5,\"mpregen\":0,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":66,\"attackdamageperlevel\":4.5,\"attackspeedperlevel\":3.65,\"attackspeed\":0.625}"),
                (7, "Jinx", "The Loose Cannon", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Jinx.png", "Marksman", "{\"hp\":610,\"hpperlevel\":86,\"mp\":245,\"mpperlevel\":45,\"movespeed\":325,\"armor\":28,\"armorperlevel\":3.5,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":525,\"hpregen\":3.75,\"hpregenperlevel\":0.5,\"mpregen\":6.7,\"mpregenperlevel\":1,\"crit\":0,\"critperlevel\":0,\"attackdamage\":57,\"attackdamageperlevel\":3.4,\"attackspeedperlevel\":1,\"attackspeed\":0.625}"),
                (8, "Lee Sin", "The Blind Monk", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/LeeSin.png", "Fighter,Assassin", "{\"hp\":575,\"hpperlevel\":85,\"mp\":200,\"mpperlevel\":0,\"movespeed\":345,\"armor\":33,\"armorperlevel\":3.7,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":7.5,\"hpregenperlevel\":0.7,\"mpregen\":50,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":68,\"attackdamageperlevel\":3.7,\"attackspeedperlevel\":3,\"attackspeed\":0.651}"),
                (9, "Lulu", "The Fae Sorceress", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Lulu.png", "Support,Mage", "{\"hp\":525,\"hpperlevel\":74,\"mp\":350,\"mpperlevel\":55,\"movespeed\":330,\"armor\":29,\"armorperlevel\":3.7,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":550,\"hpregen\":6,\"hpregenperlevel\":0.6,\"mpregen\":11,\"mpregenperlevel\":0.6,\"crit\":0,\"critperlevel\":0,\"attackdamage\":47,\"attackdamageperlevel\":2.6,\"attackspeedperlevel\":2.25,\"attackspeed\":0.625}"),
                (10, "Malphite", "Shard of the Monolith", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Malphite.png", "Tank,Fighter", "{\"hp\":574.2,\"hpperlevel\":90,\"mp\":282.2,\"mpperlevel\":40,\"movespeed\":335,\"armor\":37,\"armorperlevel\":3.75,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":7,\"hpregenperlevel\":0.55,\"mpregen\":7.32,\"mpregenperlevel\":0.55,\"crit\":0,\"critperlevel\":0,\"attackdamage\":61.97,\"attackdamageperlevel\":4,\"attackspeedperlevel\":3.4,\"attackspeed\":0.736}"),
                (11, "Shen", "The Eye of Twilight", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Shen.png", "Tank", "{\"hp\":540,\"hpperlevel\":85,\"mp\":400,\"mpperlevel\":0,\"movespeed\":340,\"armor\":34,\"armorperlevel\":3,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":8.5,\"hpregenperlevel\":0.75,\"mpregen\":50,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":60,\"attackdamageperlevel\":3,\"attackspeedperlevel\":3,\"attackspeed\":0.751}"),
                (12, "Teemo", "The Swift Scout", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Teemo.png", "Marksman,Assassin", "{\"hp\":528,\"hpperlevel\":90,\"mp\":334,\"mpperlevel\":20,\"movespeed\":330,\"armor\":24.3,\"armorperlevel\":3.75,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":500,\"hpregen\":5.5,\"hpregenperlevel\":0.65,\"mpregen\":9.6,\"mpregenperlevel\":0.45,\"crit\":0,\"critperlevel\":0,\"attackdamage\":54,\"attackdamageperlevel\":3,\"attackspeedperlevel\":3.38,\"attackspeed\":0.69}"),
                (13, "Thresh", "The Chain Warden", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Thresh.png", "Support,Fighter", "{\"hp\":560.52,\"hpperlevel\":93,\"mp\":273.92,\"mpperlevel\":44,\"movespeed\":335,\"armor\":28,\"armorperlevel\":0,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":450,\"hpregen\":7,\"hpregenperlevel\":0.55,\"mpregen\":6,\"mpregenperlevel\":0.8,\"crit\":0,\"critperlevel\":0,\"attackdamage\":56,\"attackdamageperlevel\":2.3,\"attackspeedperlevel\":3.5,\"attackspeed\":0.625}"),
                (14, "Yasuo", "The Unforgiven", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Yasuo.png", "Fighter,Assassin", "{\"hp\":490,\"hpperlevel\":87,\"mp\":100,\"mpperlevel\":0,\"movespeed\":345,\"armor\":30,\"armorperlevel\":3.4,\"spellblock\":32,\"spellblockperlevel\":1.25,\"attackrange\":175,\"hpregen\":6.5,\"hpregenperlevel\":0.9,\"mpregen\":0,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":60,\"attackdamageperlevel\":3.2,\"attackspeedperlevel\":2.5,\"attackspeed\":0.697}"),
                (15, "Yone", "The Unforgotten", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Yone.png", "Assassin,Fighter", "{\"hp\":550,\"hpperlevel\":85,\"mp\":500,\"mpperlevel\":0,\"movespeed\":345,\"armor\":28,\"armorperlevel\":3.4,\"spellblock\":32,\"spellblockperlevel\":1.25,\"attackrange\":175,\"hpregen\":7.5,\"hpregenperlevel\":0.75,\"mpregen\":0,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":60,\"attackdamageperlevel\":3,\"attackspeedperlevel\":2.5,\"attackspeed\":0.625}"),
                (16, "Yuumi", "The Magical Cat", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Yuumi.png", "Support,Mage", "{\"hp\":480,\"hpperlevel\":70,\"mp\":400,\"mpperlevel\":45,\"movespeed\":330,\"armor\":25,\"armorperlevel\":3,\"spellblock\":25,\"spellblockperlevel\":0.3,\"attackrange\":500,\"hpregen\":7,\"hpregenperlevel\":0.55,\"mpregen\":10,\"mpregenperlevel\":0.4,\"crit\":0,\"critperlevel\":0,\"attackdamage\":55,\"attackdamageperlevel\":3.1,\"attackspeedperlevel\":1,\"attackspeed\":0.625}"),
                (17, "Zed", "The Master of Shadows", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Zed.png", "Assassin", "{\"hp\":584,\"hpperlevel\":85,\"mp\":200,\"mpperlevel\":0,\"movespeed\":345,\"armor\":32,\"armorperlevel\":3.5,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":7,\"hpregenperlevel\":0.65,\"mpregen\":50,\"mpregenperlevel\":0,\"crit\":0,\"critperlevel\":0,\"attackdamage\":63,\"attackdamageperlevel\":3.4,\"attackspeedperlevel\":3.3,\"attackspeed\":0.651}"),
                (18, "Leona", "The Radiant Dawn", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Leona.png", "Tank,Support", "{\"hp\":576.16,\"hpperlevel\":87,\"mp\":302.2,\"mpperlevel\":40,\"movespeed\":335,\"armor\":47,\"armorperlevel\":3.6,\"spellblock\":32.1,\"spellblockperlevel\":1.25,\"attackrange\":125,\"hpregen\":8.5,\"hpregenperlevel\":0.85,\"mpregen\":6,\"mpregenperlevel\":0.8,\"crit\":0,\"critperlevel\":0,\"attackdamage\":60.04,\"attackdamageperlevel\":3,\"attackspeedperlevel\":2.9,\"attackspeed\":0.625}"),
                (19, "Lux", "The Lady of Luminosity", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Lux.png", "Mage,Support", "{\"hp\":490,\"hpperlevel\":85,\"mp\":480,\"mpperlevel\":23.5,\"movespeed\":330,\"armor\":18.72,\"armorperlevel\":4,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":550,\"hpregen\":5.5,\"hpregenperlevel\":0.55,\"mpregen\":8,\"mpregenperlevel\":0.8,\"crit\":0,\"critperlevel\":0,\"attackdamage\":53.54,\"attackdamageperlevel\":3.3,\"attackspeedperlevel\":1,\"attackspeed\":0.669}"),
                (20, "Jhin", "The Virtuoso", "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Jhin.png", "Marksman,Mage", "{\"hp\":585,\"hpperlevel\":93,\"mp\":300,\"mpperlevel\":50,\"movespeed\":330,\"armor\":24,\"armorperlevel\":3.5,\"spellblock\":30,\"spellblockperlevel\":0.5,\"attackrange\":550,\"hpregen\":3.75,\"hpregenperlevel\":0.55,\"mpregen\":6,\"mpregenperlevel\":0.8,\"crit\":0,\"critperlevel\":0,\"attackdamage\":59,\"attackdamageperlevel\":4.7,\"attackspeedperlevel\":0,\"attackspeed\":0.625}")
            };

            foreach (var champion in champions)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO Champions (Id, Name, Title, ImageUrl, Tags, Stats)
                    VALUES (@Id, @Name, @Title, @ImageUrl, @Tags, @Stats)
                ", new Dictionary<string, object>
                {
                    { "@Id", champion.Id },
                    { "@Name", champion.Name },
                    { "@Title", champion.Title },
                    { "@ImageUrl", champion.ImageUrl },
                    { "@Tags", champion.Tags },
                    { "@Stats", champion.Stats }
                });
            }
        }

        /// <summary>
        /// Insère les données de rôles de champions dans la base de données
        /// </summary>
        private async Task InsertChampionRolesAsync()
        {
            // Liste des rôles de champions avec leurs statistiques
            var championRoles = new List<(int ChampionId, string Role, double WinRate, double PickRate, double BanRate, string TierRank)>
            {
                (1, "MID", 0.51, 0.08, 0.02, "A"),
                (2, "JUNGLE", 0.53, 0.05, 0.01, "B"),
                (2, "SUPPORT", 0.49, 0.02, 0.01, "C"),
                (3, "TOP", 0.52, 0.07, 0.05, "A"),
                (4, "ADC", 0.50, 0.15, 0.02, "S"),
                (4, "MID", 0.48, 0.03, 0.01, "B"),
                (5, "TOP", 0.51, 0.06, 0.03, "A"),
                (6, "TOP", 0.51, 0.05, 0.01, "B"),
                (7, "ADC", 0.52, 0.12, 0.03, "S"),
                (8, "JUNGLE", 0.49, 0.10, 0.02, "S"),
                (9, "SUPPORT", 0.52, 0.08, 0.01, "A"),
                (10, "TOP", 0.53, 0.04, 0.01, "B"),
                (10, "SUPPORT", 0.50, 0.02, 0.01, "C"),
                (11, "TOP", 0.51, 0.04, 0.01, "B"),
                (12, "TOP", 0.49, 0.03, 0.01, "C"),
                (12, "JUNGLE", 0.42, 0.01, 0.01, "D"),
                (13, "SUPPORT", 0.51, 0.10, 0.02, "S"),
                (14, "MID", 0.49, 0.09, 0.07, "A"),
                (14, "TOP", 0.48, 0.04, 0.03, "B"),
                (15, "MID", 0.50, 0.08, 0.05, "A"),
                (15, "TOP", 0.49, 0.04, 0.02, "B"),
                (16, "SUPPORT", 0.48, 0.05, 0.03, "B"),
                (17, "MID", 0.50, 0.07, 0.04, "A"),
                (18, "SUPPORT", 0.53, 0.09, 0.02, "S"),
                (19, "MID", 0.51, 0.06, 0.01, "B"),
                (19, "SUPPORT", 0.50, 0.07, 0.01, "B"),
                (20, "ADC", 0.51, 0.11, 0.02, "S")
            };

            foreach (var role in championRoles)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO ChampionRoles (ChampionId, Role, WinRate, PickRate, BanRate, TierRank)
                    VALUES (@ChampionId, @Role, @WinRate, @PickRate, @BanRate, @TierRank)
                ", new Dictionary<string, object>
                {
                    { "@ChampionId", role.ChampionId },
                    { "@Role", role.Role },
                    { "@WinRate", role.WinRate },
                    { "@PickRate", role.PickRate },
                    { "@BanRate", role.BanRate },
                    { "@TierRank", role.TierRank }
                });
            }
        }

        /// <summary>
        /// Insère les données de synergies entre champions dans la base de données
        /// </summary>
        private async Task InsertChampionSynergiesAsync()
        {
            // Liste des synergies entre champions
            var synergies = new List<(int Champion1Id, int Champion2Id, double SynergyScore, double WinRateIncrease, string Description)>
            {
                (2, 14, 0.85, 0.04, "La combinaison de l'ultime d'Amumu avec celui de Yasuo crée une synergie dévastatrice en teamfight. L'ultime d'Amumu immobilise les ennemis, permettant à Yasuo d'utiliser son ultime sur plusieurs cibles."),
                (2, 1, 0.80, 0.03, "Le CC d'Amumu permet à Ahri de toucher facilement ses charmes et de maximiser ses dégâts."),
                (13, 7, 0.90, 0.05, "Les crochets et la lanterne de Thresh offrent une protection et une mobilité exceptionnelles à Jinx, compensant sa faible mobilité et maximisant ses dégâts."),
                (13, 4, 0.85, 0.04, "Thresh peut protéger Ezreal et lui offrir des opportunités d'engagement sécurisées, tandis qu'Ezreal peut suivre facilement les engagements de Thresh grâce à sa téléportation."),
                (18, 7, 0.88, 0.04, "Le CC de Leona permet à Jinx de poser facilement ses pièges et de maximiser ses dégâts pendant que les ennemis sont immobilisés."),
                (9, 14, 0.82, 0.03, "Les buffs et la protection de Lulu permettent à Yasuo de jouer plus agressivement et de survivre plus longtemps dans les teamfights."),
                (9, 15, 0.84, 0.04, "Lulu peut amplifier considérablement l'impact de Yone en teamfight avec ses buffs et sa protection."),
                (3, 18, 0.79, 0.03, "Le CC de Leona permet à Darius d'atteindre facilement ses cibles et d'appliquer ses stacks d'hémorragie."),
                (19, 7, 0.81, 0.03, "Le CC et les dégâts à distance de Lux se combinent parfaitement avec les capacités de Jinx pour créer une lane dominante."),
                (10, 14, 0.83, 0.04, "L'ultime de Malphite crée une opportunité parfaite pour Yasuo d'utiliser son ultime sur plusieurs cibles."),
                (10, 20, 0.80, 0.03, "L'initiation de Malphite permet à Jhin de poser facilement son W et son ultime pour maximiser ses dégâts."),
                (8, 5, 0.78, 0.03, "Lee Sin peut aider Fiora à atteindre ses cibles et à appliquer la pression nécessaire pour dominer la top lane."),
                (8, 17, 0.81, 0.03, "La mobilité et l'agressivité précoce de Lee Sin se combinent parfaitement avec le potentiel de burst de Zed."),
                (16, 4, 0.86, 0.04, "Yuumi peut s'attacher à Ezreal, lui offrant des soins et des dégâts supplémentaires tout en bénéficiant de sa mobilité."),
                (16, 14, 0.85, 0.04, "Yuumi peut transformer Yasuo en une menace inarrêtable en lui fournissant des soins constants et des dégâts supplémentaires.")
            };

            foreach (var synergy in synergies)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO ChampionSynergies (Champion1Id, Champion2Id, SynergyScore, WinRateIncrease, Description)
                    VALUES (@Champion1Id, @Champion2Id, @SynergyScore, @WinRateIncrease, @Description)
                ", new Dictionary<string, object>
                {
                    { "@Champion1Id", synergy.Champion1Id },
                    { "@Champion2Id", synergy.Champion2Id },
                    { "@SynergyScore", synergy.SynergyScore },
                    { "@WinRateIncrease", synergy.WinRateIncrease },
                    { "@Description", synergy.Description }
                });

                // Ajouter également la synergie inverse (si A a une synergie avec B, alors B a une synergie avec A)
                if (synergy.Champion1Id != synergy.Champion2Id)
                {
                    await ExecuteNonQueryAsync(@"
                        INSERT INTO ChampionSynergies (Champion1Id, Champion2Id, SynergyScore, WinRateIncrease, Description)
                        VALUES (@Champion1Id, @Champion2Id, @SynergyScore, @WinRateIncrease, @Description)
                    ", new Dictionary<string, object>
                    {
                        { "@Champion1Id", synergy.Champion2Id },
                        { "@Champion2Id", synergy.Champion1Id },
                        { "@SynergyScore", synergy.SynergyScore },
                        { "@WinRateIncrease", synergy.WinRateIncrease },
                        { "@Description", synergy.Description }
                    });
                }
            }
        }

        /// <summary>
        /// Insère les données de contre-picks dans la base de données
        /// </summary>
        private async Task InsertCounterPicksAsync()
        {
            // Liste des contre-picks
            var counterPicks = new List<(int ChampionId, int CounterChampionId, string Role, double CounterScore, double WinRate, string Difficulty, string Explanation)>
            {
                (5, 10, "TOP", 0.85, 0.58, "EASY", "Malphite counter Fiora grâce à sa capacité à ralentir son attaque speed et à son armure naturelle qui réduit les dégâts de Fiora. Son ultime est également difficile à esquiver pour Fiora."),
                (5, 6, "TOP", 0.75, 0.54, "MEDIUM", "Garen peut facilement trader avec Fiora en early game et son silence l'empêche d'utiliser ses compétences. Sa régénération passive lui permet de survivre aux pokes de Fiora."),
                (5, 11, "TOP", 0.80, 0.56, "MEDIUM", "Shen peut bloquer les attaques de Fiora avec son W et échanger favorablement avec son Q. Son ultime lui permet également d'aider son équipe tout en splitpushant contre Fiora."),
                (14, 10, "MID", 0.90, 0.60, "EASY", "Malphite est extrêmement efficace contre Yasuo grâce à son armure naturelle et son ralentissement d'attaque speed. Son ultime est également difficile à contrer pour Yasuo."),
                (14, 1, "MID", 0.78, 0.53, "MEDIUM", "Ahri peut facilement harceler Yasuo à distance et son charme peut interrompre son dash. Sa mobilité lui permet également d'éviter les engagements de Yasuo."),
                (14, 19, "MID", 0.82, 0.55, "MEDIUM", "Lux peut maintenir Yasuo à distance avec ses compétences et son snare. Son ultime peut également passer à travers le mur de vent de Yasuo."),
                (12, 8, "JUNGLE", 0.95, 0.65, "EASY", "Lee Sin counter complètement Teemo jungle avec sa mobilité et ses dégâts précoces. Il peut facilement envahir la jungle de Teemo et le tuer."),
                (12, 17, "TOP", 0.88, 0.58, "MEDIUM", "Zed peut facilement all-in Teemo et le tuer avant qu'il ne puisse riposter. Son ultime lui permet d'éviter l'aveuglement de Teemo."),
                (12, 3, "TOP", 0.85, 0.57, "MEDIUM", "Darius peut facilement all-in Teemo et le tuer s'il parvient à l'attraper. Son saignement passif est également efficace contre Teemo."),
                (4, 7, "ADC", 0.75, 0.52, "HARD", "Jinx peut outscale Ezreal en late game et a un meilleur push de lane. Ses pièges peuvent également limiter la mobilité d'Ezreal."),
                (4, 20, "ADC", 0.78, 0.53, "MEDIUM", "Jhin peut facilement trader avec Ezreal en early game et son W peut le toucher même à travers les minions."),
                (9, 13, "SUPPORT", 0.80, 0.54, "MEDIUM", "Thresh peut facilement engager sur Lulu et la rendre vulnérable. Son crochet et son ultime peuvent également interrompre les buffs de Lulu."),
                (9, 18, "SUPPORT", 0.85, 0.56, "MEDIUM", "Leona peut facilement engager sur Lulu et la CC-lock. Son W lui permet également de résister aux pokes de Lulu."),
                (16, 13, "SUPPORT", 0.90, 0.60, "EASY", "Thresh peut facilement attraper Yuumi lorsqu'elle tente de proc son passif et l'empêcher de se rattacher à son ADC."),
                (16, 18, "SUPPORT", 0.92, 0.62, "EASY", "Leona peut facilement engager et CC-lock Yuumi lorsqu'elle tente de proc son passif, l'empêchant de se rattacher à son ADC.")
            };

            foreach (var counterPick in counterPicks)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO CounterPicks (ChampionId, CounterChampionId, Role, CounterScore, WinRate, Difficulty, Explanation)
                    VALUES (@ChampionId, @CounterChampionId, @Role, @CounterScore, @WinRate, @Difficulty, @Explanation)
                ", new Dictionary<string, object>
                {
                    { "@ChampionId", counterPick.ChampionId },
                    { "@CounterChampionId", counterPick.CounterChampionId },
                    { "@Role", counterPick.Role },
                    { "@CounterScore", counterPick.CounterScore },
                    { "@WinRate", counterPick.WinRate },
                    { "@Difficulty", counterPick.Difficulty },
                    { "@Explanation", counterPick.Explanation }
                });
            }
        }

        /// <summary>
        /// Insère les données de compositions méta dans la base de données
        /// </summary>
        private async Task InsertMetaCompositionsAsync()
        {
            // Liste des compositions méta
            var metaCompositions = new List<(int Id, string Name, string Description, string Patch, string Tier, double WinRate, string PlayStyle, string Difficulty)>
            {
                (1, "Wombo Combo", "Composition axée sur les combos de CC et les ultimates AoE pour dominer les teamfights.", "13.10", "S", 0.54, "Teamfight", "MEDIUM"),
                (2, "Poke & Siege", "Composition axée sur le poke à distance et le siège de tours.", "13.10", "A", 0.52, "Poke", "EASY"),
                (3, "Pick Composition", "Composition axée sur l'élimination rapide de cibles isolées.", "13.10", "A", 0.51, "Pick", "MEDIUM"),
                (4, "Split Push", "Composition axée sur la pression de lanes et le split push.", "13.10", "B", 0.50, "Split", "HARD"),
                (5, "Protect the Carry", "Composition axée sur la protection d'un hypercarry.", "13.10", "S", 0.53, "Scaling", "MEDIUM")
            };

            foreach (var composition in metaCompositions)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO MetaCompositions (Id, Name, Description, Patch, Tier, WinRate, PlayStyle, Difficulty)
                    VALUES (@Id, @Name, @Description, @Patch, @Tier, @WinRate, @PlayStyle, @Difficulty)
                ", new Dictionary<string, object>
                {
                    { "@Id", composition.Id },
                    { "@Name", composition.Name },
                    { "@Description", composition.Description },
                    { "@Patch", composition.Patch },
                    { "@Tier", composition.Tier },
                    { "@WinRate", composition.WinRate },
                    { "@PlayStyle", composition.PlayStyle },
                    { "@Difficulty", composition.Difficulty }
                });
            }

            // Liste des champions dans les compositions méta
            var metaCompositionChampions = new List<(int CompositionId, int ChampionId, string Role)>
            {
                // Wombo Combo
                (1, 10, "TOP"),
                (1, 2, "JUNGLE"),
                (1, 14, "MID"),
                (1, 7, "ADC"),
                (1, 13, "SUPPORT"),

                // Poke & Siege
                (2, 12, "TOP"),
                (2, 8, "JUNGLE"),
                (2, 19, "MID"),
                (2, 4, "ADC"),
                (2, 9, "SUPPORT"),

                // Pick Composition
                (3, 3, "TOP"),
                (3, 8, "JUNGLE"),
                (3, 1, "MID"),
                (3, 20, "ADC"),
                (3, 13, "SUPPORT"),

                // Split Push
                (4, 5, "TOP"),
                (4, 8, "JUNGLE"),
                (4, 15, "MID"),
                (4, 4, "ADC"),
                (4, 16, "SUPPORT"),

                // Protect the Carry
                (5, 11, "TOP"),
                (5, 2, "JUNGLE"),
                (5, 19, "MID"),
                (5, 7, "ADC"),
                (5, 9, "SUPPORT")
            };

            foreach (var champion in metaCompositionChampions)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO MetaCompositionChampions (CompositionId, ChampionId, Role)
                    VALUES (@CompositionId, @ChampionId, @Role)
                ", new Dictionary<string, object>
                {
                    { "@CompositionId", champion.CompositionId },
                    { "@ChampionId", champion.ChampionId },
                    { "@Role", champion.Role }
                });
            }

            // Liste des forces et faiblesses des compositions méta
            var metaCompositionAttributes = new List<(int CompositionId, string AttributeType, string Description)>
            {
                // Wombo Combo
                (1, "STRENGTH", "Excellente synergie en teamfight"),
                (1, "STRENGTH", "Forte capacité d'engagement"),
                (1, "STRENGTH", "Bonne scaling en mid-late game"),
                (1, "WEAKNESS", "Vulnérable au split push"),
                (1, "WEAKNESS", "Dépendant des ultimates"),

                // Poke & Siege
                (2, "STRENGTH", "Excellent poke à distance"),
                (2, "STRENGTH", "Bonne prise de tours"),
                (2, "STRENGTH", "Contrôle d'objectifs"),
                (2, "WEAKNESS", "Vulnérable aux engagements forcés"),
                (2, "WEAKNESS", "Faible en all-in"),

                // Pick Composition
                (3, "STRENGTH", "Excellente capacité à éliminer des cibles isolées"),
                (3, "STRENGTH", "Bon contrôle de vision"),
                (3, "STRENGTH", "Mobilité élevée"),
                (3, "WEAKNESS", "Faible en teamfight 5v5"),
                (3, "WEAKNESS", "Dépendant de l'avantage précoce"),

                // Split Push
                (4, "STRENGTH", "Excellente pression de map"),
                (4, "STRENGTH", "Bons duels 1v1"),
                (4, "STRENGTH", "Flexibilité tactique"),
                (4, "WEAKNESS", "Vulnérable aux engagements forcés"),
                (4, "WEAKNESS", "Coordination difficile"),

                // Protect the Carry
                (5, "STRENGTH", "Scaling exceptionnel en late game"),
                (5, "STRENGTH", "Excellente protection du carry"),
                (5, "STRENGTH", "Bonne capacité défensive"),
                (5, "WEAKNESS", "Faible en early game"),
                (5, "WEAKNESS", "Dépendant de la performance du carry")
            };

            foreach (var attribute in metaCompositionAttributes)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO MetaCompositionAttributes (CompositionId, AttributeType, Description)
                    VALUES (@CompositionId, @AttributeType, @Description)
                ", new Dictionary<string, object>
                {
                    { "@CompositionId", attribute.CompositionId },
                    { "@AttributeType", attribute.AttributeType },
                    { "@Description", attribute.Description }
                });
            }
        }

        /// <summary>
        /// Insère les données de picks problématiques dans la base de données
        /// </summary>
        private async Task InsertTrollPicksAsync()
        {
            // Liste des picks problématiques
            var trollPicks = new List<(int ChampionId, string Role, double WinRate, string Reason)>
            {
                (12, "JUNGLE", 0.42, "Teemo jungle a une clairance de jungle lente et des ganks faibles avant le niveau 6. Il est très vulnérable aux contre-jungling et a du mal à sécuriser les objectifs."),
                (16, "JUNGLE", 0.38, "Yuumi jungle est extrêmement inefficace en raison de sa faible capacité à clear les camps et de son incapacité à ganker efficacement. Elle est également très vulnérable aux invasions."),
                (19, "JUNGLE", 0.41, "Lux jungle a une clairance lente et est très vulnérable aux invasions. Ses ganks sont prévisibles et faciles à éviter sans setup de lane."),
                (6, "ADC", 0.43, "Garen ADC manque de portée et de dégâts à distance, ce qui le rend inefficace dans ce rôle. Il est facilement harcelé en lane et a du mal à farm sous pression."),
                (10, "ADC", 0.40, "Malphite ADC manque de dégâts soutenus et de portée, ce qui le rend inefficace dans ce rôle. Il est également très dépendant de son ultime pour être utile."),
                (3, "SUPPORT", 0.44, "Darius support manque d'outils de soutien et de protection pour son ADC. Il est également très vulnérable au harcèlement à distance et a besoin de ressources pour être efficace.")
            };

            foreach (var trollPick in trollPicks)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO TrollPicks (ChampionId, Role, WinRate, Reason)
                    VALUES (@ChampionId, @Role, @WinRate, @Reason)
                ", new Dictionary<string, object>
                {
                    { "@ChampionId", trollPick.ChampionId },
                    { "@Role", trollPick.Role },
                    { "@WinRate", trollPick.WinRate },
                    { "@Reason", trollPick.Reason }
                });
            }

            // Liste des alternatives aux picks problématiques
            var trollPickAlternatives = new List<(int TrollChampionId, string TrollRole, int AlternativeChampionId, double RecommendationScore)>
            {
                (12, "JUNGLE", 8, 0.95),  // Teemo Jungle -> Lee Sin
                (12, "JUNGLE", 2, 0.90),  // Teemo Jungle -> Amumu
                (12, "JUNGLE", 17, 0.85), // Teemo Jungle -> Zed
                (16, "JUNGLE", 8, 0.95),  // Yuumi Jungle -> Lee Sin
                (16, "JUNGLE", 2, 0.90),  // Yuumi Jungle -> Amumu
                (16, "JUNGLE", 17, 0.85), // Yuumi Jungle -> Zed
                (19, "JUNGLE", 8, 0.90),  // Lux Jungle -> Lee Sin
                (19, "JUNGLE", 2, 0.95),  // Lux Jungle -> Amumu
                (19, "JUNGLE", 17, 0.85), // Lux Jungle -> Zed
                (6, "ADC", 7, 0.95),      // Garen ADC -> Jinx
                (6, "ADC", 4, 0.90),      // Garen ADC -> Ezreal
                (6, "ADC", 20, 0.85),     // Garen ADC -> Jhin
                (10, "ADC", 7, 0.95),     // Malphite ADC -> Jinx
                (10, "ADC", 4, 0.90),     // Malphite ADC -> Ezreal
                (10, "ADC", 20, 0.85),    // Malphite ADC -> Jhin
                (3, "SUPPORT", 13, 0.95), // Darius Support -> Thresh
                (3, "SUPPORT", 18, 0.90), // Darius Support -> Leona
                (3, "SUPPORT", 9, 0.85)   // Darius Support -> Lulu
            };

            foreach (var alternative in trollPickAlternatives)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO TrollPickAlternatives (TrollChampionId, TrollRole, AlternativeChampionId, RecommendationScore)
                    VALUES (@TrollChampionId, @TrollRole, @AlternativeChampionId, @RecommendationScore)
                ", new Dictionary<string, object>
                {
                    { "@TrollChampionId", alternative.TrollChampionId },
                    { "@TrollRole", alternative.TrollRole },
                    { "@AlternativeChampionId", alternative.AlternativeChampionId },
                    { "@RecommendationScore", alternative.RecommendationScore }
                });
            }
        }

        /// <summary>
        /// Insère les données de statistiques par phase de jeu dans la base de données
        /// </summary>
        private async Task InsertPhaseStatsAsync()
        {
            // Liste des statistiques par phase de jeu
            var phaseStats = new List<(int ChampionId, string Role, string Phase, double PerformanceScore)>
            {
                // Ahri
                (1, "MID", "EARLY", 0.65),
                (1, "MID", "MID", 0.75),
                (1, "MID", "LATE", 0.70),

                // Amumu
                (2, "JUNGLE", "EARLY", 0.55),
                (2, "JUNGLE", "MID", 0.70),
                (2, "JUNGLE", "LATE", 0.80),
                (2, "SUPPORT", "EARLY", 0.50),
                (2, "SUPPORT", "MID", 0.65),
                (2, "SUPPORT", "LATE", 0.75),

                // Darius
                (3, "TOP", "EARLY", 0.75),
                (3, "TOP", "MID", 0.80),
                (3, "TOP", "LATE", 0.65),

                // Ezreal
                (4, "ADC", "EARLY", 0.60),
                (4, "ADC", "MID", 0.75),
                (4, "ADC", "LATE", 0.85),
                (4, "MID", "EARLY", 0.55),
                (4, "MID", "MID", 0.70),
                (4, "MID", "LATE", 0.80),

                // Fiora
                (5, "TOP", "EARLY", 0.65),
                (5, "TOP", "MID", 0.75),
                (5, "TOP", "LATE", 0.85),

                // Garen
                (6, "TOP", "EARLY", 0.70),
                (6, "TOP", "MID", 0.75),
                (6, "TOP", "LATE", 0.65),

                // Jinx
                (7, "ADC", "EARLY", 0.55),
                (7, "ADC", "MID", 0.70),
                (7, "ADC", "LATE", 0.90),

                // Lee Sin
                (8, "JUNGLE", "EARLY", 0.85),
                (8, "JUNGLE", "MID", 0.75),
                (8, "JUNGLE", "LATE", 0.60),

                // Lulu
                (9, "SUPPORT", "EARLY", 0.70),
                (9, "SUPPORT", "MID", 0.75),
                (9, "SUPPORT", "LATE", 0.85),

                // Malphite
                (10, "TOP", "EARLY", 0.60),
                (10, "TOP", "MID", 0.70),
                (10, "TOP", "LATE", 0.85),
                (10, "SUPPORT", "EARLY", 0.55),
                (10, "SUPPORT", "MID", 0.65),
                (10, "SUPPORT", "LATE", 0.80),

                // Shen
                (11, "TOP", "EARLY", 0.65),
                (11, "TOP", "MID", 0.75),
                (11, "TOP", "LATE", 0.70),

                // Teemo
                (12, "TOP", "EARLY", 0.75),
                (12, "TOP", "MID", 0.70),
                (12, "TOP", "LATE", 0.60),
                (12, "JUNGLE", "EARLY", 0.40),
                (12, "JUNGLE", "MID", 0.45),
                (12, "JUNGLE", "LATE", 0.50),

                // Thresh
                (13, "SUPPORT", "EARLY", 0.75),
                (13, "SUPPORT", "MID", 0.80),
                (13, "SUPPORT", "LATE", 0.85),

                // Yasuo
                (14, "MID", "EARLY", 0.65),
                (14, "MID", "MID", 0.80),
                (14, "MID", "LATE", 0.85),
                (14, "TOP", "EARLY", 0.60),
                (14, "TOP", "MID", 0.75),
                (14, "TOP", "LATE", 0.80),

                // Yone
                (15, "MID", "EARLY", 0.60),
                (15, "MID", "MID", 0.75),
                (15, "MID", "LATE", 0.90),
                (15, "TOP", "EARLY", 0.55),
                (15, "TOP", "MID", 0.70),
                (15, "TOP", "LATE", 0.85),

                // Yuumi
                (16, "SUPPORT", "EARLY", 0.50),
                (16, "SUPPORT", "MID", 0.65),
                (16, "SUPPORT", "LATE", 0.90),

                // Zed
                (17, "MID", "EARLY", 0.75),
                (17, "MID", "MID", 0.85),
                (17, "MID", "LATE", 0.70),

                // Leona
                (18, "SUPPORT", "EARLY", 0.80),
                (18, "SUPPORT", "MID", 0.75),
                (18, "SUPPORT", "LATE", 0.70),

                // Lux
                (19, "MID", "EARLY", 0.70),
                (19, "MID", "MID", 0.75),
                (19, "MID", "LATE", 0.80),
                (19, "SUPPORT", "EARLY", 0.75),
                (19, "SUPPORT", "MID", 0.70),
                (19, "SUPPORT", "LATE", 0.65),

                // Jhin
                (20, "ADC", "EARLY", 0.70),
                (20, "ADC", "MID", 0.80),
                (20, "ADC", "LATE", 0.85)
            };

            foreach (var stat in phaseStats)
            {
                await ExecuteNonQueryAsync(@"
                    INSERT INTO ChampionPhaseStats (ChampionId, Role, Phase, PerformanceScore)
                    VALUES (@ChampionId, @Role, @Phase, @PerformanceScore)
                ", new Dictionary<string, object>
                {
                    { "@ChampionId", stat.ChampionId },
                    { "@Role", stat.Role },
                    { "@Phase", stat.Phase },
                    { "@PerformanceScore", stat.PerformanceScore }
                });
            }
        }

        /// <summary>
        /// Exécute une requête SQL sans retour de résultat
        /// </summary>
        /// <param name="sql">Requête SQL à exécuter</param>
        /// <param name="parameters">Paramètres de la requête (optionnel)</param>
        private async Task ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            using (var command = new SQLiteCommand(sql, _connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Ferme la connexion à la base de données
        /// </summary>
        public void Close()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        /// <summary>
        /// Récupère les données de champion par ID
        /// </summary>
        /// <param name="championId">ID du champion</param>
        public async Task<Dictionary<string, object>> GetChampionByIdAsync(int championId)
        {
            string sql = "SELECT * FROM Champions WHERE Id = @ChampionId";
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Dictionary<string, object>
                        {
                            { "Id", reader.GetInt32(0) },
                            { "Name", reader.GetString(1) },
                            { "Title", reader.GetString(2) },
                            { "ImageUrl", reader.GetString(3) },
                            { "Tags", reader.GetString(4) },
                            { "Stats", reader.GetString(5) }
                        };
                    }
                    
                    return null;
                }
            }
        }

        /// <summary>
        /// Récupère les données de rôle de champion
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        public async Task<Dictionary<string, object>> GetChampionRoleAsync(int championId, string role)
        {
            string sql = "SELECT * FROM ChampionRoles WHERE ChampionId = @ChampionId AND Role = @Role";
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                command.Parameters.AddWithValue("@Role", role);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Dictionary<string, object>
                        {
                            { "ChampionId", reader.GetInt32(0) },
                            { "Role", reader.GetString(1) },
                            { "WinRate", reader.GetDouble(2) },
                            { "PickRate", reader.GetDouble(3) },
                            { "BanRate", reader.GetDouble(4) },
                            { "TierRank", reader.GetString(5) }
                        };
                    }
                    
                    return null;
                }
            }
        }

        /// <summary>
        /// Récupère les synergies d'un champion
        /// </summary>
        /// <param name="championId">ID du champion</param>
        public async Task<List<Dictionary<string, object>>> GetChampionSynergiesAsync(int championId)
        {
            string sql = @"
                SELECT cs.*, c.Name, c.ImageUrl 
                FROM ChampionSynergies cs
                JOIN Champions c ON cs.Champion2Id = c.Id
                WHERE cs.Champion1Id = @ChampionId
                ORDER BY cs.SynergyScore DESC
            ";
            
            var synergies = new List<Dictionary<string, object>>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        synergies.Add(new Dictionary<string, object>
                        {
                            { "Champion1Id", reader.GetInt32(0) },
                            { "Champion2Id", reader.GetInt32(1) },
                            { "SynergyScore", reader.GetDouble(2) },
                            { "WinRateIncrease", reader.GetDouble(3) },
                            { "Description", reader.GetString(4) },
                            { "ChampionName", reader.GetString(5) },
                            { "ChampionImageUrl", reader.GetString(6) }
                        });
                    }
                }
            }
            
            return synergies;
        }

        /// <summary>
        /// Récupère les contre-picks d'un champion dans un rôle spécifique
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        public async Task<List<Dictionary<string, object>>> GetCounterPicksAsync(int championId, string role)
        {
            string sql = @"
                SELECT cp.*, c.Name, c.ImageUrl 
                FROM CounterPicks cp
                JOIN Champions c ON cp.CounterChampionId = c.Id
                WHERE cp.ChampionId = @ChampionId AND cp.Role = @Role
                ORDER BY cp.CounterScore DESC
            ";
            
            var counterPicks = new List<Dictionary<string, object>>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                command.Parameters.AddWithValue("@Role", role);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        counterPicks.Add(new Dictionary<string, object>
                        {
                            { "ChampionId", reader.GetInt32(0) },
                            { "CounterChampionId", reader.GetInt32(1) },
                            { "Role", reader.GetString(2) },
                            { "CounterScore", reader.GetDouble(3) },
                            { "WinRate", reader.GetDouble(4) },
                            { "Difficulty", reader.GetString(5) },
                            { "Explanation", reader.GetString(6) },
                            { "ChampionName", reader.GetString(7) },
                            { "ChampionImageUrl", reader.GetString(8) }
                        });
                    }
                }
            }
            
            return counterPicks;
        }

        /// <summary>
        /// Vérifie si un pick est problématique
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        public async Task<Dictionary<string, object>> GetTrollPickAsync(int championId, string role)
        {
            string sql = @"
                SELECT tp.*, c.Name, c.ImageUrl 
                FROM TrollPicks tp
                JOIN Champions c ON tp.ChampionId = c.Id
                WHERE tp.ChampionId = @ChampionId AND tp.Role = @Role
            ";
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                command.Parameters.AddWithValue("@Role", role);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Dictionary<string, object>
                        {
                            { "ChampionId", reader.GetInt32(0) },
                            { "Role", reader.GetString(1) },
                            { "WinRate", reader.GetDouble(2) },
                            { "Reason", reader.GetString(3) },
                            { "ChampionName", reader.GetString(4) },
                            { "ChampionImageUrl", reader.GetString(5) }
                        };
                    }
                    
                    return null;
                }
            }
        }

        /// <summary>
        /// Récupère les alternatives à un pick problématique
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        public async Task<List<Dictionary<string, object>>> GetTrollPickAlternativesAsync(int championId, string role)
        {
            string sql = @"
                SELECT tpa.*, c.Name, c.ImageUrl 
                FROM TrollPickAlternatives tpa
                JOIN Champions c ON tpa.AlternativeChampionId = c.Id
                WHERE tpa.TrollChampionId = @ChampionId AND tpa.TrollRole = @Role
                ORDER BY tpa.RecommendationScore DESC
            ";
            
            var alternatives = new List<Dictionary<string, object>>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                command.Parameters.AddWithValue("@Role", role);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        alternatives.Add(new Dictionary<string, object>
                        {
                            { "TrollChampionId", reader.GetInt32(0) },
                            { "TrollRole", reader.GetString(1) },
                            { "AlternativeChampionId", reader.GetInt32(2) },
                            { "RecommendationScore", reader.GetDouble(3) },
                            { "ChampionName", reader.GetString(4) },
                            { "ChampionImageUrl", reader.GetString(5) }
                        });
                    }
                }
            }
            
            return alternatives;
        }

        /// <summary>
        /// Récupère les compositions méta
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetMetaCompositionsAsync()
        {
            string sql = "SELECT * FROM MetaCompositions ORDER BY Tier, WinRate DESC";
            
            var compositions = new List<Dictionary<string, object>>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        compositions.Add(new Dictionary<string, object>
                        {
                            { "Id", reader.GetInt32(0) },
                            { "Name", reader.GetString(1) },
                            { "Description", reader.GetString(2) },
                            { "Patch", reader.GetString(3) },
                            { "Tier", reader.GetString(4) },
                            { "WinRate", reader.GetDouble(5) },
                            { "PlayStyle", reader.GetString(6) },
                            { "Difficulty", reader.GetString(7) }
                        });
                    }
                }
            }
            
            return compositions;
        }

        /// <summary>
        /// Récupère les champions d'une composition méta
        /// </summary>
        /// <param name="compositionId">ID de la composition</param>
        public async Task<List<Dictionary<string, object>>> GetMetaCompositionChampionsAsync(int compositionId)
        {
            string sql = @"
                SELECT mcc.*, c.Name, c.ImageUrl 
                FROM MetaCompositionChampions mcc
                JOIN Champions c ON mcc.ChampionId = c.Id
                WHERE mcc.CompositionId = @CompositionId
                ORDER BY CASE mcc.Role 
                    WHEN 'TOP' THEN 1 
                    WHEN 'JUNGLE' THEN 2 
                    WHEN 'MID' THEN 3 
                    WHEN 'ADC' THEN 4 
                    WHEN 'SUPPORT' THEN 5 
                    ELSE 6 
                END
            ";
            
            var champions = new List<Dictionary<string, object>>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@CompositionId", compositionId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        champions.Add(new Dictionary<string, object>
                        {
                            { "CompositionId", reader.GetInt32(0) },
                            { "ChampionId", reader.GetInt32(1) },
                            { "Role", reader.GetString(2) },
                            { "ChampionName", reader.GetString(3) },
                            { "ChampionImageUrl", reader.GetString(4) }
                        });
                    }
                }
            }
            
            return champions;
        }

        /// <summary>
        /// Récupère les forces et faiblesses d'une composition méta
        /// </summary>
        /// <param name="compositionId">ID de la composition</param>
        public async Task<Dictionary<string, List<string>>> GetMetaCompositionAttributesAsync(int compositionId)
        {
            string sql = @"
                SELECT AttributeType, Description 
                FROM MetaCompositionAttributes
                WHERE CompositionId = @CompositionId
                ORDER BY AttributeType
            ";
            
            var attributes = new Dictionary<string, List<string>>
            {
                { "STRENGTH", new List<string>() },
                { "WEAKNESS", new List<string>() }
            };
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@CompositionId", compositionId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string attributeType = reader.GetString(0);
                        string description = reader.GetString(1);
                        
                        if (attributes.ContainsKey(attributeType))
                        {
                            attributes[attributeType].Add(description);
                        }
                    }
                }
            }
            
            return attributes;
        }

        /// <summary>
        /// Récupère les statistiques par phase de jeu d'un champion dans un rôle spécifique
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        public async Task<Dictionary<string, double>> GetChampionPhaseStatsAsync(int championId, string role)
        {
            string sql = @"
                SELECT Phase, PerformanceScore 
                FROM ChampionPhaseStats
                WHERE ChampionId = @ChampionId AND Role = @Role
            ";
            
            var phaseStats = new Dictionary<string, double>();
            
            using (var command = new SQLiteCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@ChampionId", championId);
                command.Parameters.AddWithValue("@Role", role);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string phase = reader.GetString(0);
                        double performanceScore = reader.GetDouble(1);
                        
                        phaseStats[phase] = performanceScore;
                    }
                }
            }
            
            return phaseStats;
        }
    }
}
