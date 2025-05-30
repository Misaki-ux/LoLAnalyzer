using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Data;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service de gestion des profils utilisateur et des statistiques personnelles
    /// </summary>
    public class UserProfileService
    {
        private readonly DatabaseManager _databaseManager;
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private bool _isInitialized = false;

        /// <summary>
        /// Constructeur du service de profils utilisateur
        /// </summary>
        /// <param name="databaseManager">Gestionnaire de base de données</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        public UserProfileService(DatabaseManager databaseManager, Logger logger, RiotApiService riotApiService)
        {
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _riotApiService = riotApiService ?? throw new ArgumentNullException(nameof(riotApiService));
        }

        /// <summary>
        /// Initialise le service
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                _logger.Log(LogLevel.Info, "Initialisation du service de profils utilisateur...");

                // Vérification de la connexion à la base de données
                await _databaseManager.InitializeAsync();

                _isInitialized = true;
                _logger.Log(LogLevel.Info, "Service de profils utilisateur initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'initialisation du service de profils utilisateur: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère ou crée un profil utilisateur
        /// </summary>
        /// <param name="summonerName">Nom d'invocateur</param>
        /// <param name="region">Région du serveur</param>
        /// <returns>Profil utilisateur</returns>
        public async Task<UserProfile> GetOrCreateProfileAsync(string summonerName, string region)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (string.IsNullOrWhiteSpace(summonerName))
                throw new ArgumentException("Le nom d'invocateur ne peut pas être vide", nameof(summonerName));

            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("La région ne peut pas être vide", nameof(region));

            try
            {
                _logger.Log(LogLevel.Info, $"Récupération du profil pour {summonerName} ({region})...");

                // Recherche du profil dans la base de données
                var profile = await _databaseManager.GetUserProfileAsync(summonerName);

                if (profile == null)
                {
                    // Création d'un nouveau profil
                    _logger.Log(LogLevel.Info, $"Création d'un nouveau profil pour {summonerName}...");

                    // Récupération des informations de l'invocateur depuis l'API Riot
                    var summonerInfo = await _riotApiService.GetSummonerInfoAsync(summonerName, region);

                    if (summonerInfo == null)
                    {
                        throw new Exception($"Impossible de récupérer les informations pour l'invocateur {summonerName}");
                    }

                    // Création du profil
                    profile = new UserProfile
                    {
                        SummonerName = summonerInfo.Name,
                        Region = region,
                        MainRole = await DetermineMainRoleAsync(summonerInfo.Id, region),
                        PreferredChampions = await DeterminePreferredChampionsAsync(summonerInfo.Id, region),
                        LastUpdated = DateTime.Now
                    };

                    // Enregistrement du profil dans la base de données
                    await SaveProfileAsync(profile);

                    // Récupération et enregistrement des statistiques
                    await UpdateUserStatsAsync(profile);
                }
                else if ((DateTime.Now - profile.LastUpdated).TotalDays > 1)
                {
                    // Mise à jour du profil si les données datent de plus d'un jour
                    _logger.Log(LogLevel.Info, $"Mise à jour du profil pour {summonerName}...");

                    // Récupération des informations de l'invocateur depuis l'API Riot
                    var summonerInfo = await _riotApiService.GetSummonerInfoAsync(summonerName, region);

                    if (summonerInfo != null)
                    {
                        // Mise à jour du profil
                        profile.MainRole = await DetermineMainRoleAsync(summonerInfo.Id, region);
                        profile.PreferredChampions = await DeterminePreferredChampionsAsync(summonerInfo.Id, region);
                        profile.LastUpdated = DateTime.Now;

                        // Enregistrement du profil dans la base de données
                        await SaveProfileAsync(profile);

                        // Mise à jour des statistiques
                        await UpdateUserStatsAsync(profile);
                    }
                }

                _logger.Log(LogLevel.Info, $"Profil récupéré pour {summonerName}");
                return profile;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération du profil: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère les statistiques d'un utilisateur
        /// </summary>
        /// <param name="profileId">ID du profil utilisateur</param>
        /// <returns>Liste des statistiques utilisateur</returns>
        public async Task<List<UserStats>> GetUserStatsAsync(int profileId)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
            {
                _logger.Log(LogLevel.Info, $"Récupération des statistiques pour le profil {profileId}...");

                // Récupération des statistiques depuis la base de données
                var stats = await _databaseManager.GetUserStatsAsync(profileId);

                _logger.Log(LogLevel.Info, $"Statistiques récupérées: {stats.Count} entrées");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des statistiques: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Met à jour les statistiques d'un utilisateur
        /// </summary>
        /// <param name="profile">Profil utilisateur</param>
        /// <returns>Liste des statistiques mises à jour</returns>
        public async Task<List<UserStats>> UpdateUserStatsAsync(UserProfile profile)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                _logger.Log(LogLevel.Info, $"Mise à jour des statistiques pour {profile.SummonerName}...");

                // Récupération de l'historique des matchs depuis l'API Riot
                var matchHistory = await _riotApiService.GetMatchHistoryAsync(profile.SummonerName, profile.Region);

                if (matchHistory == null || matchHistory.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucun match trouvé pour {profile.SummonerName}");
                    return new List<UserStats>();
                }

                // Traitement des statistiques par champion
                var championStats = ProcessMatchHistory(matchHistory, profile.SummonerName);

                // Enregistrement des statistiques dans la base de données
                await SaveUserStatsAsync(profile.Id, championStats);

                // Récupération des statistiques mises à jour
                var updatedStats = await _databaseManager.GetUserStatsAsync(profile.Id);

                _logger.Log(LogLevel.Info, $"Statistiques mises à jour: {updatedStats.Count} entrées");
                return updatedStats;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la mise à jour des statistiques: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Génère des recommandations personnalisées pour un utilisateur
        /// </summary>
        /// <param name="profile">Profil utilisateur</param>
        /// <param name="role">Rôle cible (optionnel)</param>
        /// <returns>Recommandations personnalisées</returns>
        public async Task<PersonalizedRecommendations> GetPersonalizedRecommendationsAsync(UserProfile profile, string role = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                _logger.Log(LogLevel.Info, $"Génération de recommandations personnalisées pour {profile.SummonerName}...");

                // Récupération des statistiques de l'utilisateur
                var userStats = await _databaseManager.GetUserStatsAsync(profile.Id);

                // Si un rôle spécifique est demandé, l'utiliser, sinon utiliser le rôle principal
                string targetRole = role ?? profile.MainRole;

                // Récupération des champions les plus joués par l'utilisateur dans ce rôle
                var topChampions = await GetTopChampionsForRoleAsync(profile.SummonerName, profile.Region, targetRole);

                // Récupération des champions avec le meilleur taux de victoire pour l'utilisateur
                var bestWinrateChampions = userStats
                    .Where(s => s.GamesPlayed >= 5) // Au moins 5 parties jouées
                    .OrderByDescending(s => (double)s.Wins / s.GamesPlayed)
                    .Take(3)
                    .ToList();

                // Récupération des champions recommandés en fonction du style de jeu
                var recommendedChampions = await GetRecommendedChampionsAsync(profile, targetRole);

                // Création des recommandations personnalisées
                var recommendations = new PersonalizedRecommendations
                {
                    ProfileId = profile.Id,
                    SummonerName = profile.SummonerName,
                    Role = targetRole,
                    TopPlayedChampions = topChampions.Select(c => new ChampionRecommendation
                    {
                        ChampionId = c.ChampionId,
                        ChampionName = c.ChampionName,
                        ImageUrl = c.ImageUrl,
                        Reason = "Champion fréquemment joué",
                        ConfidenceScore = 0.9
                    }).ToList(),
                    BestWinrateChampions = bestWinrateChampions.Select(s => new ChampionRecommendation
                    {
                        ChampionId = s.ChampionId,
                        ChampionName = s.ChampionName,
                        ImageUrl = s.ImageUrl,
                        Reason = $"Bon taux de victoire ({Math.Round((double)s.Wins / s.GamesPlayed * 100, 1)}%)",
                        ConfidenceScore = 0.8
                    }).ToList(),
                    RecommendedChampions = recommendedChampions.Select(c => new ChampionRecommendation
                    {
                        ChampionId = c.ChampionId,
                        ChampionName = c.ChampionName,
                        ImageUrl = c.ImageUrl,
                        Reason = c.Reason,
                        ConfidenceScore = c.ConfidenceScore
                    }).ToList(),
                    LastUpdated = DateTime.Now
                };

                _logger.Log(LogLevel.Info, $"Recommandations personnalisées générées pour {profile.SummonerName}");
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la génération des recommandations personnalisées: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Détermine le rôle principal d'un joueur
        /// </summary>
        private async Task<string> DetermineMainRoleAsync(string summonerId, string region)
        {
            try
            {
                // Récupération des statistiques de rôle depuis l'API Riot
                var roleStats = await _riotApiService.GetRoleStatsAsync(summonerId, region);

                if (roleStats == null || roleStats.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune statistique de rôle trouvée pour {summonerId}");
                    return "MID"; // Rôle par défaut
                }

                // Détermination du rôle le plus joué
                var mainRole = roleStats
                    .OrderByDescending(rs => rs.GamesPlayed)
                    .First();

                return NormalizeRole(mainRole.Role);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la détermination du rôle principal: {ex.Message}");
                return "MID"; // Rôle par défaut en cas d'erreur
            }
        }

        /// <summary>
        /// Détermine les champions préférés d'un joueur
        /// </summary>
        private async Task<string[]> DeterminePreferredChampionsAsync(string summonerId, string region)
        {
            try
            {
                // Récupération des statistiques de champion depuis l'API Riot
                var championStats = await _riotApiService.GetChampionStatsAsync(summonerId, region);

                if (championStats == null || championStats.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune statistique de champion trouvée pour {summonerId}");
                    return new string[0];
                }

                // Sélection des champions les plus joués
                var preferredChampions = championStats
                    .OrderByDescending(cs => cs.GamesPlayed)
                    .Take(5)
                    .Select(cs => cs.ChampionName)
                    .ToArray();

                return preferredChampions;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la détermination des champions préférés: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Récupère les champions les plus joués pour un rôle donné
        /// </summary>
        private async Task<List<UserStats>> GetTopChampionsForRoleAsync(string summonerName, string region, string role)
        {
            try
            {
                // Récupération des statistiques de champion par rôle depuis l'API Riot
                var championRoleStats = await _riotApiService.GetChampionRoleStatsAsync(summonerName, region, role);

                if (championRoleStats == null || championRoleStats.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune statistique de champion pour le rôle {role} trouvée pour {summonerName}");
                    return new List<UserStats>();
                }

                // Sélection des champions les plus joués dans ce rôle
                var topChampions = championRoleStats
                    .OrderByDescending(cs => cs.GamesPlayed)
                    .Take(3)
                    .ToList();

                return topChampions;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des champions les plus joués: {ex.Message}");
                return new List<UserStats>();
            }
        }

        /// <summary>
        /// Récupère les champions recommandés en fonction du style de jeu
        /// </summary>
        private async Task<List<ChampionRecommendation>> GetRecommendedChampionsAsync(UserProfile profile, string role)
        {
            try
            {
                // Récupération des statistiques de l'utilisateur
                var userStats = await _databaseManager.GetUserStatsAsync(profile.Id);

                if (userStats == null || userStats.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune statistique trouvée pour {profile.SummonerName}");
                    return new List<ChampionRecommendation>();
                }

                // Analyse du style de jeu
                var playStyle = AnalyzePlayStyle(userStats);

                // Récupération des champions qui correspondent au style de jeu
                var champions = await _databaseManager.GetAllChampionsAsync();
                var championRoles = await _databaseManager.GetAllChampionRolesAsync();

                // Filtrage des champions viables dans ce rôle
                var viableChampions = championRoles
                    .Where(cr => cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase) && cr.Viability >= 0.6)
                    .Join(champions, cr => cr.ChampionId, c => c.Id, (cr, c) => new { ChampionRole = cr, Champion = c })
                    .ToList();

                // Sélection des champions qui correspondent au style de jeu
                var recommendedChampions = new List<ChampionRecommendation>();

                foreach (var champion in viableChampions)
                {
                    double matchScore = CalculateStyleMatchScore(champion.Champion, playStyle);

                    // Si le score de correspondance est suffisamment élevé
                    if (matchScore >= 0.7)
                    {
                        recommendedChampions.Add(new ChampionRecommendation
                        {
                            ChampionId = champion.Champion.Id,
                            ChampionName = champion.Champion.Name,
                            ImageUrl = champion.Champion.ImageUrl,
                            Reason = GenerateRecommendationReason(champion.Champion, playStyle),
                            ConfidenceScore = matchScore
                        });
                    }
                }

                // Tri par score de confiance décroissant
                recommendedChampions = recommendedChampions
                    .OrderByDescending(rc => rc.ConfidenceScore)
                    .Take(3)
                    .ToList();

                return recommendedChampions;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des champions recommandés: {ex.Message}");
                return new List<ChampionRecommendation>();
            }
        }

        /// <summary>
        /// Analyse le style de jeu d'un utilisateur
        /// </summary>
        private PlayStyle AnalyzePlayStyle(List<UserStats> userStats)
        {
            // Initialisation du style de jeu
            var playStyle = new PlayStyle
            {
                AggressionLevel = 5, // Valeur par défaut (échelle de 0 à 10)
                FarmingFocus = 5,    // Valeur par défaut (échelle de 0 à 10)
                TeamplayFocus = 5,   // Valeur par défaut (échelle de 0 à 10)
                PreferredDamageType = "Mixed", // Valeur par défaut
                PreferredGamePhase = "Mid"     // Valeur par défaut
            };

            // Si aucune statistique n'est disponible, retourner les valeurs par défaut
            if (userStats == null || userStats.Count == 0)
                return playStyle;

            // Analyse des statistiques pour déterminer le style de jeu
            double totalKDA = 0;
            double totalCS = 0;
            int physicalDamageChampions = 0;
            int magicalDamageChampions = 0;
            int earlyGameChampions = 0;
            int midGameChampions = 0;
            int lateGameChampions = 0;

            foreach (var stat in userStats)
            {
                // Analyse du KDA
                string[] kdaParts = stat.KDA.Split('/');
                if (kdaParts.Length == 3)
                {
                    if (int.TryParse(kdaParts[0], out int kills) &&
                        int.TryParse(kdaParts[1], out int deaths) &&
                        int.TryParse(kdaParts[2], out int assists))
                    {
                        // Un KDA élevé indique un style de jeu plus prudent
                        double kda = deaths > 0 ? (kills + assists) / (double)deaths : (kills + assists);
                        totalKDA += kda;

                        // Un ratio kills/assists élevé indique un style de jeu plus agressif
                        if (kills > assists)
                            playStyle.AggressionLevel += 0.5;
                        else
                            playStyle.TeamplayFocus += 0.5;
                    }
                }

                // Analyse du CS
                totalCS += stat.AverageCS;

                // Analyse du type de champion
                var champion = GetChampionById(stat.ChampionId);
                if (champion != null)
                {
                    // Type de dégâts préféré
                    if (champion.PhysicalDamage > champion.MagicalDamage)
                        physicalDamageChampions++;
                    else
                        magicalDamageChampions++;

                    // Phase de jeu préférée
                    if (champion.EarlyGame > champion.MidGame && champion.EarlyGame > champion.LateGame)
                        earlyGameChampions++;
                    else if (champion.MidGame > champion.EarlyGame && champion.MidGame > champion.LateGame)
                        midGameChampions++;
                    else
                        lateGameChampions++;
                }
            }

            // Calcul des moyennes
            if (userStats.Count > 0)
            {
                double avgKDA = totalKDA / userStats.Count;
                double avgCS = totalCS / userStats.Count;

                // Ajustement du niveau d'agression en fonction du KDA moyen
                if (avgKDA < 2.0)
                    playStyle.AggressionLevel += 2;
                else if (avgKDA < 3.0)
                    playStyle.AggressionLevel += 1;
                else if (avgKDA > 4.0)
                    playStyle.AggressionLevel -= 1;
                else if (avgKDA > 5.0)
                    playStyle.AggressionLevel -= 2;

                // Ajustement du focus sur le farming en fonction du CS moyen
                if (avgCS < 5.0)
                    playStyle.FarmingFocus -= 2;
                else if (avgCS < 6.0)
                    playStyle.FarmingFocus -= 1;
                else if (avgCS > 7.0)
                    playStyle.FarmingFocus += 1;
                else if (avgCS > 8.0)
                    playStyle.FarmingFocus += 2;

                // Détermination du type de dégâts préféré
                if (physicalDamageChampions > magicalDamageChampions * 2)
                    playStyle.PreferredDamageType = "Physical";
                else if (magicalDamageChampions > physicalDamageChampions * 2)
                    playStyle.PreferredDamageType = "Magical";
                else
                    playStyle.PreferredDamageType = "Mixed";

                // Détermination de la phase de jeu préférée
                if (earlyGameChampions > midGameChampions && earlyGameChampions > lateGameChampions)
                    playStyle.PreferredGamePhase = "Early";
                else if (midGameChampions > earlyGameChampions && midGameChampions > lateGameChampions)
                    playStyle.PreferredGamePhase = "Mid";
                else
                    playStyle.PreferredGamePhase = "Late";
            }

            // Limitation des valeurs entre 0 et 10
            playStyle.AggressionLevel = Math.Max(0, Math.Min(10, playStyle.AggressionLevel));
            playStyle.FarmingFocus = Math.Max(0, Math.Min(10, playStyle.FarmingFocus));
            playStyle.TeamplayFocus = Math.Max(0, Math.Min(10, playStyle.TeamplayFocus));

            return playStyle;
        }

        /// <summary>
        /// Calcule le score de correspondance entre un champion et un style de jeu
        /// </summary>
        private double CalculateStyleMatchScore(Champion champion, PlayStyle playStyle)
        {
            if (champion == null || playStyle == null)
                return 0;

            double score = 0;
            double totalWeight = 0;

            // Correspondance du niveau d'agression
            double aggressionWeight = 0.3;
            double aggressionScore = 0;
            
            if (playStyle.AggressionLevel >= 7) // Style agressif
            {
                // Champions avec beaucoup de dégâts et de mobilité
                aggressionScore = (champion.PhysicalDamage + champion.MagicalDamage + champion.Mobility) / 3.0;
            }
            else if (playStyle.AggressionLevel <= 3) // Style défensif
            {
                // Champions avec beaucoup de tankiness et de sustain
                aggressionScore = (champion.Tankiness + champion.Sustain + champion.CC) / 3.0;
            }
            else // Style équilibré
            {
                // Champions polyvalents
                aggressionScore = (champion.PhysicalDamage + champion.MagicalDamage + champion.Tankiness + champion.Utility) / 4.0;
            }
            
            score += aggressionScore * aggressionWeight;
            totalWeight += aggressionWeight;

            // Correspondance du focus sur le farming
            double farmingWeight = 0.2;
            double farmingScore = 0;
            
            if (playStyle.FarmingFocus >= 7) // Focus élevé sur le farming
            {
                // Champions qui scale bien
                farmingScore = champion.LateGame / 10.0;
            }
            else if (playStyle.FarmingFocus <= 3) // Focus faible sur le farming
            {
                // Champions forts en early game
                farmingScore = champion.EarlyGame / 10.0;
            }
            else // Focus moyen sur le farming
            {
                // Champions équilibrés
                farmingScore = champion.MidGame / 10.0;
            }
            
            score += farmingScore * farmingWeight;
            totalWeight += farmingWeight;

            // Correspondance du focus sur le teamplay
            double teamplayWeight = 0.2;
            double teamplayScore = 0;
            
            if (playStyle.TeamplayFocus >= 7) // Focus élevé sur le teamplay
            {
                // Champions avec beaucoup de CC et d'utilité
                teamplayScore = (champion.CC + champion.Utility) / 2.0 / 10.0;
            }
            else if (playStyle.TeamplayFocus <= 3) // Focus faible sur le teamplay
            {
                // Champions indépendants
                teamplayScore = (champion.Sustain + champion.Mobility) / 2.0 / 10.0;
            }
            else // Focus moyen sur le teamplay
            {
                // Champions équilibrés
                teamplayScore = 0.5;
            }
            
            score += teamplayScore * teamplayWeight;
            totalWeight += teamplayWeight;

            // Correspondance du type de dégâts préféré
            double damageTypeWeight = 0.15;
            double damageTypeScore = 0;
            
            if (playStyle.PreferredDamageType == "Physical")
            {
                damageTypeScore = champion.PhysicalDamage / 10.0;
            }
            else if (playStyle.PreferredDamageType == "Magical")
            {
                damageTypeScore = champion.MagicalDamage / 10.0;
            }
            else // Mixed
            {
                damageTypeScore = (champion.PhysicalDamage + champion.MagicalDamage) / 2.0 / 10.0;
            }
            
            score += damageTypeScore * damageTypeWeight;
            totalWeight += damageTypeWeight;

            // Correspondance de la phase de jeu préférée
            double gamePhaseWeight = 0.15;
            double gamePhaseScore = 0;
            
            if (playStyle.PreferredGamePhase == "Early")
            {
                gamePhaseScore = champion.EarlyGame / 10.0;
            }
            else if (playStyle.PreferredGamePhase == "Mid")
            {
                gamePhaseScore = champion.MidGame / 10.0;
            }
            else // Late
            {
                gamePhaseScore = champion.LateGame / 10.0;
            }
            
            score += gamePhaseScore * gamePhaseWeight;
            totalWeight += gamePhaseWeight;

            // Calcul du score final
            return totalWeight > 0 ? score / totalWeight : 0;
        }

        /// <summary>
        /// Génère une raison de recommandation pour un champion
        /// </summary>
        private string GenerateRecommendationReason(Champion champion, PlayStyle playStyle)
        {
            if (champion == null || playStyle == null)
                return "Recommandation basée sur votre style de jeu";

            List<string> reasons = new List<string>();

            // Raisons basées sur le niveau d'agression
            if (playStyle.AggressionLevel >= 7 && (champion.PhysicalDamage >= 7 || champion.MagicalDamage >= 7))
            {
                reasons.Add("correspond à votre style de jeu agressif");
            }
            else if (playStyle.AggressionLevel <= 3 && champion.Tankiness >= 7)
            {
                reasons.Add("correspond à votre style de jeu défensif");
            }

            // Raisons basées sur le focus sur le farming
            if (playStyle.FarmingFocus >= 7 && champion.LateGame >= 7)
            {
                reasons.Add("bon scaling pour votre focus sur le farming");
            }
            else if (playStyle.FarmingFocus <= 3 && champion.EarlyGame >= 7)
            {
                reasons.Add("fort en early game pour votre style de jeu");
            }

            // Raisons basées sur le focus sur le teamplay
            if (playStyle.TeamplayFocus >= 7 && (champion.CC >= 7 || champion.Utility >= 7))
            {
                reasons.Add("bon pour le teamplay que vous privilégiez");
            }
            else if (playStyle.TeamplayFocus <= 3 && (champion.Sustain >= 7 || champion.Mobility >= 7))
            {
                reasons.Add("indépendant pour votre style de jeu solo");
            }

            // Raisons basées sur le type de dégâts préféré
            if (playStyle.PreferredDamageType == "Physical" && champion.PhysicalDamage >= 7)
            {
                reasons.Add("dégâts physiques que vous préférez");
            }
            else if (playStyle.PreferredDamageType == "Magical" && champion.MagicalDamage >= 7)
            {
                reasons.Add("dégâts magiques que vous préférez");
            }

            // Raisons basées sur la phase de jeu préférée
            if (playStyle.PreferredGamePhase == "Early" && champion.EarlyGame >= 7)
            {
                reasons.Add("fort en early game comme vous le préférez");
            }
            else if (playStyle.PreferredGamePhase == "Mid" && champion.MidGame >= 7)
            {
                reasons.Add("fort en mid game comme vous le préférez");
            }
            else if (playStyle.PreferredGamePhase == "Late" && champion.LateGame >= 7)
            {
                reasons.Add("fort en late game comme vous le préférez");
            }

            // Si aucune raison spécifique n'a été trouvée
            if (reasons.Count == 0)
            {
                return "Recommandation basée sur votre style de jeu";
            }

            // Combinaison des raisons
            return string.Join(", ", reasons);
        }

        /// <summary>
        /// Traite l'historique des matchs pour générer des statistiques par champion
        /// </summary>
        private Dictionary<int, UserStats> ProcessMatchHistory(List<MatchData> matchHistory, string summonerName)
        {
            var championStats = new Dictionary<int, UserStats>();

            foreach (var match in matchHistory)
            {
                // Recherche du joueur dans le match
                var playerData = match.Players.FirstOrDefault(p => p.SummonerName.Equals(summonerName, StringComparison.OrdinalIgnoreCase));
                
                if (playerData == null)
                    continue;

                // Récupération ou création des statistiques pour ce champion
                if (!championStats.TryGetValue(playerData.ChampionId, out var stats))
                {
                    stats = new UserStats
                    {
                        ChampionId = playerData.ChampionId,
                        ChampionName = playerData.ChampionName,
                        ImageUrl = playerData.ChampionImageUrl,
                        GamesPlayed = 0,
                        Wins = 0,
                        Losses = 0,
                        KDA = "0/0/0",
                        AverageCS = 0,
                        LastUpdated = DateTime.Now
                    };
                    
                    championStats[playerData.ChampionId] = stats;
                }

                // Mise à jour des statistiques
                stats.GamesPlayed++;
                
                if (playerData.Win)
                    stats.Wins++;
                else
                    stats.Losses++;

                // Mise à jour du KDA
                string[] kdaParts = stats.KDA.Split('/');
                int totalKills = int.Parse(kdaParts[0]) + playerData.Kills;
                int totalDeaths = int.Parse(kdaParts[1]) + playerData.Deaths;
                int totalAssists = int.Parse(kdaParts[2]) + playerData.Assists;
                stats.KDA = $"{totalKills}/{totalDeaths}/{totalAssists}";

                // Mise à jour du CS moyen
                stats.AverageCS = ((stats.AverageCS * (stats.GamesPlayed - 1)) + playerData.CS) / stats.GamesPlayed;
            }

            return championStats;
        }

        /// <summary>
        /// Enregistre un profil utilisateur dans la base de données
        /// </summary>
        private async Task SaveProfileAsync(UserProfile profile)
        {
            try
            {
                // Construction de la requête SQL
                string sql;
                Dictionary<string, object> parameters = new Dictionary<string, object>
                {
                    { "@SummonerName", profile.SummonerName },
                    { "@Region", profile.Region },
                    { "@MainRole", profile.MainRole },
                    { "@PreferredChampions", string.Join(",", profile.PreferredChampions) },
                    { "@LastUpdated", profile.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                if (profile.Id == 0)
                {
                    // Insertion d'un nouveau profil
                    sql = @"
                        INSERT INTO UserProfiles (SummonerName, Region, MainRole, PreferredChampions, LastUpdated)
                        VALUES (@SummonerName, @Region, @MainRole, @PreferredChampions, @LastUpdated);
                        SELECT last_insert_rowid();";

                    // Exécution de la requête et récupération de l'ID généré
                    var result = await _databaseManager.ExecuteScalarAsync(sql, parameters);
                    profile.Id = Convert.ToInt32(result);
                }
                else
                {
                    // Mise à jour d'un profil existant
                    sql = @"
                        UPDATE UserProfiles
                        SET SummonerName = @SummonerName,
                            Region = @Region,
                            MainRole = @MainRole,
                            PreferredChampions = @PreferredChampions,
                            LastUpdated = @LastUpdated
                        WHERE Id = @Id";

                    parameters.Add("@Id", profile.Id);

                    // Exécution de la requête
                    await _databaseManager.ExecuteNonQueryAsync(sql, parameters);
                }

                _logger.Log(LogLevel.Info, $"Profil enregistré pour {profile.SummonerName} (ID: {profile.Id})");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'enregistrement du profil: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Enregistre les statistiques utilisateur dans la base de données
        /// </summary>
        private async Task SaveUserStatsAsync(int profileId, Dictionary<int, UserStats> championStats)
        {
            try
            {
                foreach (var stats in championStats.Values)
                {
                    // Construction de la requête SQL
                    string sql;
                    Dictionary<string, object> parameters = new Dictionary<string, object>
                    {
                        { "@ProfileId", profileId },
                        { "@ChampionId", stats.ChampionId },
                        { "@GamesPlayed", stats.GamesPlayed },
                        { "@Wins", stats.Wins },
                        { "@Losses", stats.Losses },
                        { "@KDA", stats.KDA },
                        { "@AverageCS", stats.AverageCS },
                        { "@LastUpdated", stats.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss") }
                    };

                    // Vérification si les statistiques existent déjà
                    var existingStats = await _databaseManager.ExecuteScalarAsync(
                        "SELECT Id FROM UserStats WHERE ProfileId = @ProfileId AND ChampionId = @ChampionId",
                        new Dictionary<string, object>
                        {
                            { "@ProfileId", profileId },
                            { "@ChampionId", stats.ChampionId }
                        });

                    if (existingStats != null)
                    {
                        // Mise à jour des statistiques existantes
                        sql = @"
                            UPDATE UserStats
                            SET GamesPlayed = @GamesPlayed,
                                Wins = @Wins,
                                Losses = @Losses,
                                KDA = @KDA,
                                AverageCS = @AverageCS,
                                LastUpdated = @LastUpdated
                            WHERE ProfileId = @ProfileId AND ChampionId = @ChampionId";
                    }
                    else
                    {
                        // Insertion de nouvelles statistiques
                        sql = @"
                            INSERT INTO UserStats (ProfileId, ChampionId, GamesPlayed, Wins, Losses, KDA, AverageCS, LastUpdated)
                            VALUES (@ProfileId, @ChampionId, @GamesPlayed, @Wins, @Losses, @KDA, @AverageCS, @LastUpdated)";
                    }

                    // Exécution de la requête
                    await _databaseManager.ExecuteNonQueryAsync(sql, parameters);
                }

                _logger.Log(LogLevel.Info, $"Statistiques enregistrées pour le profil {profileId}: {championStats.Count} champions");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'enregistrement des statistiques: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère un champion par son ID
        /// </summary>
        private Champion GetChampionById(int championId)
        {
            try
            {
                // Cette méthode devrait idéalement récupérer le champion depuis la base de données
                // Pour l'instant, on retourne null
                return null;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération du champion: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Normalise le format du rôle
        /// </summary>
        private string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "MID"; // Rôle par défaut

            role = role.Trim().ToUpperInvariant();

            switch (role)
            {
                case "TOP":
                case "TOPLANE":
                    return "TOP";
                case "JG":
                case "JUNGLE":
                case "JUNGLER":
                    return "JUNGLE";
                case "MID":
                case "MIDDLE":
                case "MIDLANE":
                    return "MID";
                case "ADC":
                case "BOT":
                case "BOTTOM":
                    return "ADC";
                case "SUP":
                case "SUPP":
                case "SUPPORT":
                    return "SUPPORT";
                default:
                    return "MID"; // Rôle par défaut
            }
        }
    }
}
