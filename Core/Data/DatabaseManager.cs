using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Data
{
    /// <summary>
    /// Gestionnaire de base de données SQLite pour l'application LoL Analyzer
    /// </summary>
    public class DatabaseManager : IDisposable
    {
        private SQLiteConnection _connection;
        private readonly string _dbPath;
        private readonly Logger _logger;
        private bool _isInitialized = false;

        /// <summary>
        /// Constructeur du gestionnaire de base de données
        /// </summary>
        /// <param name="dbPath">Chemin vers le fichier de base de données SQLite</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        public DatabaseManager(string dbPath, Logger logger)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initialise la connexion à la base de données et crée les tables si nécessaire
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                bool createTables = !File.Exists(_dbPath);
                
                // Création du répertoire parent si nécessaire
                string directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Création de la connexion SQLite
                string connectionString = $"Data Source={_dbPath};Version=3;";
                _connection = new SQLiteConnection(connectionString);
                await _connection.OpenAsync();
                
                _logger.Log(LogLevel.Info, $"Connexion à la base de données établie: {_dbPath}");

                // Création des tables si nécessaire
                if (createTables)
                {
                    await CreateTablesAsync();
                    _logger.Log(LogLevel.Info, "Tables de base de données créées avec succès");
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'initialisation de la base de données: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crée les tables de la base de données
        /// </summary>
        private async Task CreateTablesAsync()
        {
            // Table Champions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE Champions (
                    Id INTEGER PRIMARY KEY,
                    RiotId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    ImageUrl TEXT NOT NULL,
                    PhysicalDamage REAL NOT NULL,
                    MagicalDamage REAL NOT NULL,
                    TrueDamage REAL NOT NULL,
                    Tankiness REAL NOT NULL,
                    Mobility REAL NOT NULL,
                    CC REAL NOT NULL,
                    Sustain REAL NOT NULL,
                    Utility REAL NOT NULL,
                    EarlyGame REAL NOT NULL,
                    MidGame REAL NOT NULL,
                    LateGame REAL NOT NULL,
                    Tags TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL
                )");

            // Table ChampionRoles
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionRoles (
                    Id INTEGER PRIMARY KEY,
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Viability REAL NOT NULL,
                    WinRate REAL NOT NULL,
                    PickRate REAL NOT NULL,
                    BanRate REAL NOT NULL,
                    IsMeta BOOLEAN NOT NULL,
                    IsTroll BOOLEAN NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )");

            // Table CounterPicks
            await ExecuteNonQueryAsync(@"
                CREATE TABLE CounterPicks (
                    Id INTEGER PRIMARY KEY,
                    ChampionId INTEGER NOT NULL,
                    CounterId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Effectiveness REAL NOT NULL,
                    WinRate REAL NOT NULL,
                    SampleSize INTEGER NOT NULL,
                    Reason TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id),
                    FOREIGN KEY (CounterId) REFERENCES Champions(Id)
                )");

            // Table Synergies
            await ExecuteNonQueryAsync(@"
                CREATE TABLE Synergies (
                    Id INTEGER PRIMARY KEY,
                    Champion1Id INTEGER NOT NULL,
                    Champion2Id INTEGER NOT NULL,
                    Strength REAL NOT NULL,
                    WinRate REAL NOT NULL,
                    SampleSize INTEGER NOT NULL,
                    Description TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (Champion1Id) REFERENCES Champions(Id),
                    FOREIGN KEY (Champion2Id) REFERENCES Champions(Id)
                )");

            // Table MetaData
            await ExecuteNonQueryAsync(@"
                CREATE TABLE MetaData (
                    Id INTEGER PRIMARY KEY,
                    PatchVersion TEXT NOT NULL,
                    DataSource TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL
                )");
                
            // Table Runes
            await ExecuteNonQueryAsync(@"
                CREATE TABLE Runes (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Path TEXT NOT NULL,
                    Slot INTEGER NOT NULL,
                    ImageUrl TEXT NOT NULL
                )");
                
            // Table ChampionRunes
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionRunes (
                    Id INTEGER PRIMARY KEY,
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    PrimaryPathId INTEGER NOT NULL,
                    SecondaryPathId INTEGER NOT NULL,
                    Rune1Id INTEGER NOT NULL,
                    Rune2Id INTEGER NOT NULL,
                    Rune3Id INTEGER NOT NULL,
                    Rune4Id INTEGER NOT NULL,
                    Rune5Id INTEGER NOT NULL,
                    Rune6Id INTEGER NOT NULL,
                    StatMod1 TEXT NOT NULL,
                    StatMod2 TEXT NOT NULL,
                    StatMod3 TEXT NOT NULL,
                    WinRate REAL NOT NULL,
                    PickRate REAL NOT NULL,
                    SampleSize INTEGER NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )");
                
            // Table SummonerSpells
            await ExecuteNonQueryAsync(@"
                CREATE TABLE SummonerSpells (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    ImageUrl TEXT NOT NULL
                )");
                
            // Table ChampionSummonerSpells
            await ExecuteNonQueryAsync(@"
                CREATE TABLE ChampionSummonerSpells (
                    Id INTEGER PRIMARY KEY,
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Spell1Id INTEGER NOT NULL,
                    Spell2Id INTEGER NOT NULL,
                    WinRate REAL NOT NULL,
                    PickRate REAL NOT NULL,
                    SampleSize INTEGER NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id),
                    FOREIGN KEY (Spell1Id) REFERENCES SummonerSpells(Id),
                    FOREIGN KEY (Spell2Id) REFERENCES SummonerSpells(Id)
                )");
                
            // Table UserProfiles
            await ExecuteNonQueryAsync(@"
                CREATE TABLE UserProfiles (
                    Id INTEGER PRIMARY KEY,
                    SummonerName TEXT NOT NULL,
                    Region TEXT NOT NULL,
                    MainRole TEXT NOT NULL,
                    PreferredChampions TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL
                )");
                
            // Table UserStats
            await ExecuteNonQueryAsync(@"
                CREATE TABLE UserStats (
                    Id INTEGER PRIMARY KEY,
                    ProfileId INTEGER NOT NULL,
                    ChampionId INTEGER NOT NULL,
                    GamesPlayed INTEGER NOT NULL,
                    Wins INTEGER NOT NULL,
                    Losses INTEGER NOT NULL,
                    KDA TEXT NOT NULL,
                    AverageCS REAL NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ProfileId) REFERENCES UserProfiles(Id),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )");
                
            // Table PatchHistory
            await ExecuteNonQueryAsync(@"
                CREATE TABLE PatchHistory (
                    Id INTEGER PRIMARY KEY,
                    PatchVersion TEXT NOT NULL,
                    ReleaseDate TEXT NOT NULL,
                    Notes TEXT NOT NULL
                )");
                
            // Table BanSuggestions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE BanSuggestions (
                    Id INTEGER PRIMARY KEY,
                    ChampionId INTEGER NOT NULL,
                    Tier TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    BanScore REAL NOT NULL,
                    Reason TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )");
                
            // Table TeamCompositions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE TeamCompositions (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Style TEXT NOT NULL,
                    Difficulty INTEGER NOT NULL,
                    WinCondition TEXT NOT NULL
                )");
                
            // Table TeamCompositionChampions
            await ExecuteNonQueryAsync(@"
                CREATE TABLE TeamCompositionChampions (
                    Id INTEGER PRIMARY KEY,
                    CompositionId INTEGER NOT NULL,
                    ChampionId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Importance INTEGER NOT NULL,
                    FOREIGN KEY (CompositionId) REFERENCES TeamCompositions(Id),
                    FOREIGN KEY (ChampionId) REFERENCES Champions(Id)
                )");
        }

        /// <summary>
        /// Exécute une requête SQL sans retour de résultat
        /// </summary>
        /// <param name="sql">Requête SQL à exécuter</param>
        /// <param name="parameters">Paramètres de la requête</param>
        public async Task ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
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
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'exécution de la requête: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Exécute une requête SQL et retourne un DataTable
        /// </summary>
        /// <param name="sql">Requête SQL à exécuter</param>
        /// <param name="parameters">Paramètres de la requête</param>
        /// <returns>DataTable contenant les résultats</returns>
        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
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

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'exécution de la requête: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Exécute une requête SQL et retourne un scalaire
        /// </summary>
        /// <param name="sql">Requête SQL à exécuter</param>
        /// <param name="parameters">Paramètres de la requête</param>
        /// <returns>Résultat scalaire</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
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

                    return await command.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'exécution de la requête: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère tous les champions de la base de données
        /// </summary>
        /// <returns>Liste des champions</returns>
        public async Task<List<Champion>> GetAllChampionsAsync()
        {
            var champions = new List<Champion>();
            
            var dataTable = await ExecuteQueryAsync("SELECT * FROM Champions");
            
            foreach (DataRow row in dataTable.Rows)
            {
                var champion = new Champion
                {
                    Id = Convert.ToInt32(row["Id"]),
                    RiotId = row["RiotId"].ToString(),
                    Name = row["Name"].ToString(),
                    Title = row["Title"].ToString(),
                    ImageUrl = row["ImageUrl"].ToString(),
                    PhysicalDamage = Convert.ToDouble(row["PhysicalDamage"]),
                    MagicalDamage = Convert.ToDouble(row["MagicalDamage"]),
                    TrueDamage = Convert.ToDouble(row["TrueDamage"]),
                    Tankiness = Convert.ToDouble(row["Tankiness"]),
                    Mobility = Convert.ToDouble(row["Mobility"]),
                    CC = Convert.ToDouble(row["CC"]),
                    Sustain = Convert.ToDouble(row["Sustain"]),
                    Utility = Convert.ToDouble(row["Utility"]),
                    EarlyGame = Convert.ToDouble(row["EarlyGame"]),
                    MidGame = Convert.ToDouble(row["MidGame"]),
                    LateGame = Convert.ToDouble(row["LateGame"]),
                    Tags = row["Tags"].ToString().Split(','),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                champions.Add(champion);
            }
            
            return champions;
        }

        /// <summary>
        /// Récupère tous les rôles de champions de la base de données
        /// </summary>
        /// <returns>Liste des rôles de champions</returns>
        public async Task<List<ChampionRole>> GetAllChampionRolesAsync()
        {
            var championRoles = new List<ChampionRole>();
            
            var dataTable = await ExecuteQueryAsync("SELECT * FROM ChampionRoles");
            
            foreach (DataRow row in dataTable.Rows)
            {
                var championRole = new ChampionRole
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    Role = row["Role"].ToString(),
                    Viability = Convert.ToDouble(row["Viability"]),
                    WinRate = Convert.ToDouble(row["WinRate"]),
                    PickRate = Convert.ToDouble(row["PickRate"]),
                    BanRate = Convert.ToDouble(row["BanRate"]),
                    IsMeta = Convert.ToBoolean(row["IsMeta"]),
                    IsTroll = Convert.ToBoolean(row["IsTroll"]),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                championRoles.Add(championRole);
            }
            
            return championRoles;
        }

        /// <summary>
        /// Récupère tous les contre-picks de la base de données
        /// </summary>
        /// <returns>Liste des contre-picks</returns>
        public async Task<List<CounterPick>> GetAllCounterPicksAsync()
        {
            var counterPicks = new List<CounterPick>();
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT cp.*, c1.Name as ChampionName, c2.Name as CounterName 
                FROM CounterPicks cp
                JOIN Champions c1 ON cp.ChampionId = c1.Id
                JOIN Champions c2 ON cp.CounterId = c2.Id");
            
            foreach (DataRow row in dataTable.Rows)
            {
                var counterPick = new CounterPick
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    CounterId = Convert.ToInt32(row["CounterId"]),
                    Role = row["Role"].ToString(),
                    Effectiveness = Convert.ToDouble(row["Effectiveness"]),
                    WinRate = Convert.ToDouble(row["WinRate"]),
                    SampleSize = Convert.ToInt32(row["SampleSize"]),
                    Reason = row["Reason"].ToString(),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString()),
                    ChampionName = row["ChampionName"].ToString(),
                    CounterName = row["CounterName"].ToString()
                };
                
                counterPicks.Add(counterPick);
            }
            
            return counterPicks;
        }

        /// <summary>
        /// Récupère toutes les synergies de la base de données
        /// </summary>
        /// <returns>Liste des synergies</returns>
        public async Task<List<Synergy>> GetAllSynergiesAsync()
        {
            var synergies = new List<Synergy>();
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT s.*, c1.Name as Champion1Name, c2.Name as Champion2Name 
                FROM Synergies s
                JOIN Champions c1 ON s.Champion1Id = c1.Id
                JOIN Champions c2 ON s.Champion2Id = c2.Id");
            
            foreach (DataRow row in dataTable.Rows)
            {
                var synergy = new Synergy
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Champion1Id = Convert.ToInt32(row["Champion1Id"]),
                    Champion2Id = Convert.ToInt32(row["Champion2Id"]),
                    Strength = Convert.ToDouble(row["Strength"]),
                    WinRate = Convert.ToDouble(row["WinRate"]),
                    SampleSize = Convert.ToInt32(row["SampleSize"]),
                    Description = row["Description"].ToString(),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString()),
                    Champion1Name = row["Champion1Name"].ToString(),
                    Champion2Name = row["Champion2Name"].ToString()
                };
                
                synergies.Add(synergy);
            }
            
            return synergies;
        }

        /// <summary>
        /// Récupère les suggestions de runes pour un champion et un rôle donnés
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        /// <returns>Liste des suggestions de runes</returns>
        public async Task<List<ChampionRunes>> GetChampionRunesAsync(int championId, string role)
        {
            var championRunes = new List<ChampionRunes>();
            
            var parameters = new Dictionary<string, object>
            {
                { "@ChampionId", championId },
                { "@Role", role }
            };
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT cr.*, c.Name as ChampionName
                FROM ChampionRunes cr
                JOIN Champions c ON cr.ChampionId = c.Id
                WHERE cr.ChampionId = @ChampionId AND cr.Role = @Role
                ORDER BY cr.WinRate DESC", parameters);
            
            foreach (DataRow row in dataTable.Rows)
            {
                var runes = new ChampionRunes
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    ChampionName = row["ChampionName"].ToString(),
                    Role = row["Role"].ToString(),
                    PrimaryPathId = Convert.ToInt32(row["PrimaryPathId"]),
                    SecondaryPathId = Convert.ToInt32(row["SecondaryPathId"]),
                    Rune1Id = Convert.ToInt32(row["Rune1Id"]),
                    Rune2Id = Convert.ToInt32(row["Rune2Id"]),
                    Rune3Id = Convert.ToInt32(row["Rune3Id"]),
                    Rune4Id = Convert.ToInt32(row["Rune4Id"]),
                    Rune5Id = Convert.ToInt32(row["Rune5Id"]),
                    Rune6Id = Convert.ToInt32(row["Rune6Id"]),
                    StatMod1 = row["StatMod1"].ToString(),
                    StatMod2 = row["StatMod2"].ToString(),
                    StatMod3 = row["StatMod3"].ToString(),
                    WinRate = Convert.ToDouble(row["WinRate"]),
                    PickRate = Convert.ToDouble(row["PickRate"]),
                    SampleSize = Convert.ToInt32(row["SampleSize"]),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                championRunes.Add(runes);
            }
            
            return championRunes;
        }

        /// <summary>
        /// Récupère les suggestions de sorts d'invocateur pour un champion et un rôle donnés
        /// </summary>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle du champion</param>
        /// <returns>Liste des suggestions de sorts d'invocateur</returns>
        public async Task<List<ChampionSummonerSpells>> GetChampionSummonerSpellsAsync(int championId, string role)
        {
            var championSpells = new List<ChampionSummonerSpells>();
            
            var parameters = new Dictionary<string, object>
            {
                { "@ChampionId", championId },
                { "@Role", role }
            };
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT css.*, c.Name as ChampionName, s1.Name as Spell1Name, s2.Name as Spell2Name
                FROM ChampionSummonerSpells css
                JOIN Champions c ON css.ChampionId = c.Id
                JOIN SummonerSpells s1 ON css.Spell1Id = s1.Id
                JOIN SummonerSpells s2 ON css.Spell2Id = s2.Id
                WHERE css.ChampionId = @ChampionId AND css.Role = @Role
                ORDER BY css.WinRate DESC", parameters);
            
            foreach (DataRow row in dataTable.Rows)
            {
                var spells = new ChampionSummonerSpells
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    ChampionName = row["ChampionName"].ToString(),
                    Role = row["Role"].ToString(),
                    Spell1Id = Convert.ToInt32(row["Spell1Id"]),
                    Spell2Id = Convert.ToInt32(row["Spell2Id"]),
                    Spell1Name = row["Spell1Name"].ToString(),
                    Spell2Name = row["Spell2Name"].ToString(),
                    WinRate = Convert.ToDouble(row["WinRate"]),
                    PickRate = Convert.ToDouble(row["PickRate"]),
                    SampleSize = Convert.ToInt32(row["SampleSize"]),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                championSpells.Add(spells);
            }
            
            return championSpells;
        }

        /// <summary>
        /// Récupère les suggestions de bans pour un niveau de jeu donné
        /// </summary>
        /// <param name="tier">Niveau de jeu (Bronze, Silver, Gold, etc.)</param>
        /// <returns>Liste des suggestions de bans</returns>
        public async Task<List<BanSuggestion>> GetBanSuggestionsAsync(string tier)
        {
            var banSuggestions = new List<BanSuggestion>();
            
            var parameters = new Dictionary<string, object>
            {
                { "@Tier", tier }
            };
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT bs.*, c.Name as ChampionName, c.ImageUrl
                FROM BanSuggestions bs
                JOIN Champions c ON bs.ChampionId = c.Id
                WHERE bs.Tier = @Tier
                ORDER BY bs.BanScore DESC
                LIMIT 10", parameters);
            
            foreach (DataRow row in dataTable.Rows)
            {
                var banSuggestion = new BanSuggestion
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    ChampionName = row["ChampionName"].ToString(),
                    ImageUrl = row["ImageUrl"].ToString(),
                    Tier = row["Tier"].ToString(),
                    Role = row["Role"].ToString(),
                    BanScore = Convert.ToDouble(row["BanScore"]),
                    Reason = row["Reason"].ToString(),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                banSuggestions.Add(banSuggestion);
            }
            
            return banSuggestions;
        }

        /// <summary>
        /// Récupère le profil utilisateur par nom d'invocateur
        /// </summary>
        /// <param name="summonerName">Nom d'invocateur</param>
        /// <returns>Profil utilisateur</returns>
        public async Task<UserProfile> GetUserProfileAsync(string summonerName)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@SummonerName", summonerName }
            };
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT * FROM UserProfiles
                WHERE SummonerName = @SummonerName", parameters);
            
            if (dataTable.Rows.Count == 0)
                return null;
            
            var row = dataTable.Rows[0];
            
            return new UserProfile
            {
                Id = Convert.ToInt32(row["Id"]),
                SummonerName = row["SummonerName"].ToString(),
                Region = row["Region"].ToString(),
                MainRole = row["MainRole"].ToString(),
                PreferredChampions = row["PreferredChampions"].ToString().Split(','),
                LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
            };
        }

        /// <summary>
        /// Récupère les statistiques utilisateur pour un profil donné
        /// </summary>
        /// <param name="profileId">ID du profil utilisateur</param>
        /// <returns>Liste des statistiques utilisateur</returns>
        public async Task<List<UserStats>> GetUserStatsAsync(int profileId)
        {
            var userStats = new List<UserStats>();
            
            var parameters = new Dictionary<string, object>
            {
                { "@ProfileId", profileId }
            };
            
            var dataTable = await ExecuteQueryAsync(@"
                SELECT us.*, c.Name as ChampionName, c.ImageUrl
                FROM UserStats us
                JOIN Champions c ON us.ChampionId = c.Id
                WHERE us.ProfileId = @ProfileId
                ORDER BY us.GamesPlayed DESC", parameters);
            
            foreach (DataRow row in dataTable.Rows)
            {
                var stats = new UserStats
                {
                    Id = Convert.ToInt32(row["Id"]),
                    ProfileId = Convert.ToInt32(row["ProfileId"]),
                    ChampionId = Convert.ToInt32(row["ChampionId"]),
                    ChampionName = row["ChampionName"].ToString(),
                    ImageUrl = row["ImageUrl"].ToString(),
                    GamesPlayed = Convert.ToInt32(row["GamesPlayed"]),
                    Wins = Convert.ToInt32(row["Wins"]),
                    Losses = Convert.ToInt32(row["Losses"]),
                    KDA = row["KDA"].ToString(),
                    AverageCS = Convert.ToDouble(row["AverageCS"]),
                    LastUpdated = DateTime.Parse(row["LastUpdated"].ToString())
                };
                
                userStats.Add(stats);
            }
            
            return userStats;
        }

        /// <summary>
        /// Récupère la version actuelle du patch
        /// </summary>
        /// <returns>Version du patch</returns>
        public async Task<string> GetCurrentPatchVersionAsync()
        {
            var result = await ExecuteScalarAsync(@"
                SELECT PatchVersion FROM MetaData
                ORDER BY LastUpdated DESC
                LIMIT 1");
            
            return result?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Vérifie si la base de données doit être mise à jour
        /// </summary>
        /// <param name="currentPatchVersion">Version actuelle du patch</param>
        /// <returns>True si une mise à jour est nécessaire, sinon False</returns>
        public async Task<bool> ShouldUpdateDatabaseAsync(string currentPatchVersion)
        {
            var storedPatchVersion = await GetCurrentPatchVersionAsync();
            
            if (storedPatchVersion == "Unknown")
                return true;
            
            if (storedPatchVersion != currentPatchVersion)
                return true;
            
            // Vérifier la date de dernière mise à jour
            var result = await ExecuteScalarAsync(@"
                SELECT LastUpdated FROM MetaData
                ORDER BY LastUpdated DESC
                LIMIT 1");
            
            if (result == null)
                return true;
            
            var lastUpdated = DateTime.Parse(result.ToString());
            var daysSinceUpdate = (DateTime.Now - lastUpdated).TotalDays;
            
            // Mettre à jour si la dernière mise à jour date de plus de 3 jours
            return daysSinceUpdate > 3;
        }

        /// <summary>
        /// Libère les ressources utilisées par le gestionnaire de base de données
        /// </summary>
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
