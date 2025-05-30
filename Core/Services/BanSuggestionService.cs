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
    /// Service de suggestion de bans stratégiques
    /// </summary>
    public class BanSuggestionService
    {
        private readonly DatabaseManager _databaseManager;
        private readonly Logger _logger;
        private List<Champion> _champions;
        private List<ChampionRole> _championRoles;
        private List<BanSuggestion> _banSuggestions;
        private bool _isInitialized = false;

        /// <summary>
        /// Constructeur du service de suggestion de bans
        /// </summary>
        /// <param name="databaseManager">Gestionnaire de base de données</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        public BanSuggestionService(DatabaseManager databaseManager, Logger logger)
        {
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initialise le service avec les données nécessaires
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                _logger.Log(LogLevel.Info, "Initialisation du service de suggestion de bans...");

                // Chargement des données depuis la base de données
                _champions = await _databaseManager.GetAllChampionsAsync();
                _championRoles = await _databaseManager.GetAllChampionRolesAsync();
                
                // Les suggestions de bans sont chargées à la demande en fonction du tier

                _isInitialized = true;
                _logger.Log(LogLevel.Info, "Service de suggestion de bans initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'initialisation du service de suggestion de bans: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Suggère des bans stratégiques en fonction du niveau de jeu et des préférences de l'équipe
        /// </summary>
        /// <param name="tier">Niveau de jeu (Bronze, Silver, Gold, etc.)</param>
        /// <param name="teamPreferences">Préférences de l'équipe (champions préférés par rôle)</param>
        /// <param name="enemyPreferences">Préférences connues de l'équipe ennemie (optionnel)</param>
        /// <returns>Liste des suggestions de bans</returns>
        public async Task<List<BanSuggestion>> SuggestBansAsync(string tier, Dictionary<string, List<string>> teamPreferences = null, Dictionary<string, List<string>> enemyPreferences = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
            {
                _logger.Log(LogLevel.Info, $"Génération de suggestions de bans pour le tier {tier}...");

                // Normalisation du tier
                tier = NormalizeTier(tier);

                // Chargement des suggestions de bans pour ce tier
                _banSuggestions = await _databaseManager.GetBanSuggestionsAsync(tier);

                // Liste des suggestions finales
                var finalSuggestions = new List<BanSuggestion>();

                // 1. Suggestions basées sur les champions OP du meta actuel
                var metaSuggestions = _banSuggestions.Take(3).ToList();
                finalSuggestions.AddRange(metaSuggestions);

                // 2. Suggestions basées sur les préférences de l'équipe ennemie (si connues)
                if (enemyPreferences != null && enemyPreferences.Count > 0)
                {
                    var enemyPreferencesSuggestions = GetEnemyPreferencesBans(enemyPreferences, tier);
                    finalSuggestions.AddRange(enemyPreferencesSuggestions.Take(2));
                }

                // 3. Suggestions basées sur les counters des champions préférés de notre équipe
                if (teamPreferences != null && teamPreferences.Count > 0)
                {
                    var teamProtectionSuggestions = GetTeamProtectionBans(teamPreferences, tier);
                    finalSuggestions.AddRange(teamProtectionSuggestions.Take(2));
                }

                // Si on n'a pas assez de suggestions, compléter avec les champions à fort taux de bannissement
                if (finalSuggestions.Count < 5)
                {
                    var additionalSuggestions = _banSuggestions
                        .Where(bs => !finalSuggestions.Any(fs => fs.ChampionId == bs.ChampionId))
                        .Take(5 - finalSuggestions.Count);
                    
                    finalSuggestions.AddRange(additionalSuggestions);
                }

                // Limiter à 5 suggestions
                finalSuggestions = finalSuggestions.Take(5).ToList();

                // Ajouter des explications détaillées pour chaque suggestion
                foreach (var suggestion in finalSuggestions)
                {
                    EnrichBanSuggestion(suggestion, teamPreferences, enemyPreferences, tier);
                }

                _logger.Log(LogLevel.Info, $"Suggestions de bans générées: {finalSuggestions.Count} suggestions");
                return finalSuggestions;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la génération des suggestions de bans: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Génère des suggestions de bans basées sur les préférences connues de l'équipe ennemie
        /// </summary>
        private List<BanSuggestion> GetEnemyPreferencesBans(Dictionary<string, List<string>> enemyPreferences, string tier)
        {
            var suggestions = new List<BanSuggestion>();

            foreach (var rolePreference in enemyPreferences)
            {
                string role = rolePreference.Key;
                List<string> preferredChampions = rolePreference.Value;

                foreach (var championName in preferredChampions)
                {
                    var champion = _champions.FirstOrDefault(c => c.Name.Equals(championName, StringComparison.OrdinalIgnoreCase));
                    
                    if (champion != null)
                    {
                        // Vérifier si le champion est viable dans ce rôle
                        var championRole = _championRoles.FirstOrDefault(cr => 
                            cr.ChampionId == champion.Id && 
                            cr.Role.Equals(NormalizeRole(role), StringComparison.OrdinalIgnoreCase));
                        
                        if (championRole != null && championRole.WinRate > 0.5)
                        {
                            // Calculer un score de ban basé sur le taux de victoire et le taux de sélection
                            double banScore = (championRole.WinRate * 0.7 + championRole.PickRate * 0.3) * 10;
                            
                            suggestions.Add(new BanSuggestion
                            {
                                ChampionId = champion.Id,
                                ChampionName = champion.Name,
                                ImageUrl = champion.ImageUrl,
                                Tier = tier,
                                Role = NormalizeRole(role),
                                BanScore = Math.Round(banScore, 1),
                                Reason = $"Champion préféré de l'adversaire en {NormalizeRole(role)}",
                                LastUpdated = DateTime.Now
                            });
                        }
                    }
                }
            }

            // Trier par score de ban décroissant
            return suggestions.OrderByDescending(s => s.BanScore).ToList();
        }

        /// <summary>
        /// Génère des suggestions de bans pour protéger les champions préférés de notre équipe
        /// </summary>
        private List<BanSuggestion> GetTeamProtectionBans(Dictionary<string, List<string>> teamPreferences, string tier)
        {
            var suggestions = new List<BanSuggestion>();

            foreach (var rolePreference in teamPreferences)
            {
                string role = rolePreference.Key;
                List<string> preferredChampions = rolePreference.Value;

                foreach (var championName in preferredChampions)
                {
                    var champion = _champions.FirstOrDefault(c => c.Name.Equals(championName, StringComparison.OrdinalIgnoreCase));
                    
                    if (champion != null)
                    {
                        // Trouver les counters de ce champion dans ce rôle
                        var counters = _championRoles
                            .Where(cr => cr.Role.Equals(NormalizeRole(role), StringComparison.OrdinalIgnoreCase))
                            .Join(_champions, cr => cr.ChampionId, c => c.Id, (cr, c) => new { ChampionRole = cr, Champion = c })
                            .Where(x => IsCounterTo(x.Champion.Id, champion.Id, NormalizeRole(role)))
                            .OrderByDescending(x => GetCounterEffectiveness(x.Champion.Id, champion.Id, NormalizeRole(role)))
                            .Take(2);
                        
                        foreach (var counter in counters)
                        {
                            double effectiveness = GetCounterEffectiveness(counter.Champion.Id, champion.Id, NormalizeRole(role));
                            
                            suggestions.Add(new BanSuggestion
                            {
                                ChampionId = counter.Champion.Id,
                                ChampionName = counter.Champion.Name,
                                ImageUrl = counter.Champion.ImageUrl,
                                Tier = tier,
                                Role = NormalizeRole(role),
                                BanScore = Math.Round(effectiveness * 10, 1),
                                Reason = $"Counter fort contre {champion.Name} en {NormalizeRole(role)}",
                                LastUpdated = DateTime.Now
                            });
                        }
                    }
                }
            }

            // Trier par score de ban décroissant
            return suggestions.OrderByDescending(s => s.BanScore).ToList();
        }

        /// <summary>
        /// Vérifie si un champion est un counter d'un autre champion dans un rôle donné
        /// </summary>
        private bool IsCounterTo(int championId, int targetId, string role)
        {
            // Cette méthode devrait idéalement utiliser une table de counters dans la base de données
            // Pour l'instant, on utilise une heuristique simple basée sur les attributs des champions
            
            var champion = _champions.FirstOrDefault(c => c.Id == championId);
            var target = _champions.FirstOrDefault(c => c.Id == targetId);
            
            if (champion == null || target == null)
                return false;
            
            // Exemple d'heuristique: un champion avec beaucoup de mobilité counter un champion immobile
            if (champion.Mobility > 7 && target.Mobility < 4)
                return true;
            
            // Un champion avec beaucoup de CC counter un champion sans échappatoire
            if (champion.CC > 7 && target.Mobility < 4)
                return true;
            
            // Un champion avec beaucoup de dégâts vrais counter un tank
            if (champion.TrueDamage > 5 && target.Tankiness > 7)
                return true;
            
            // Un champion avec beaucoup de sustain counter un champion avec des dégâts progressifs
            if (champion.Sustain > 7 && target.PhysicalDamage < 7 && target.MagicalDamage < 7)
                return true;
            
            return false;
        }

        /// <summary>
        /// Récupère l'efficacité d'un counter contre un champion cible dans un rôle donné
        /// </summary>
        private double GetCounterEffectiveness(int championId, int targetId, string role)
        {
            // Cette méthode devrait idéalement utiliser une table de counters dans la base de données
            // Pour l'instant, on utilise une heuristique simple
            
            var champion = _champions.FirstOrDefault(c => c.Id == championId);
            var target = _champions.FirstOrDefault(c => c.Id == targetId);
            
            if (champion == null || target == null)
                return 0.5;
            
            double effectiveness = 0.5; // Valeur par défaut
            
            // Mobilité vs immobilité
            if (champion.Mobility > 7 && target.Mobility < 4)
                effectiveness += 0.2;
            
            // CC vs pas d'échappatoire
            if (champion.CC > 7 && target.Mobility < 4)
                effectiveness += 0.2;
            
            // Dégâts vrais vs tank
            if (champion.TrueDamage > 5 && target.Tankiness > 7)
                effectiveness += 0.2;
            
            // Sustain vs dégâts progressifs
            if (champion.Sustain > 7 && target.PhysicalDamage < 7 && target.MagicalDamage < 7)
                effectiveness += 0.1;
            
            // Limiter l'efficacité entre 0.5 et 1.0
            return Math.Min(1.0, Math.Max(0.5, effectiveness));
        }

        /// <summary>
        /// Enrichit une suggestion de ban avec des explications détaillées
        /// </summary>
        private void EnrichBanSuggestion(BanSuggestion suggestion, Dictionary<string, List<string>> teamPreferences, Dictionary<string, List<string>> enemyPreferences, string tier)
        {
            var champion = _champions.FirstOrDefault(c => c.Id == suggestion.ChampionId);
            
            if (champion == null)
                return;
            
            var championRole = _championRoles.FirstOrDefault(cr => 
                cr.ChampionId == champion.Id && 
                cr.Role.Equals(suggestion.Role, StringComparison.OrdinalIgnoreCase));
            
            if (championRole == null)
                return;
            
            // Enrichir la raison avec des statistiques
            string enrichedReason = suggestion.Reason;
            
            // Ajouter le taux de victoire et de sélection
            enrichedReason += $" (WR: {Math.Round(championRole.WinRate * 100, 1)}%, PR: {Math.Round(championRole.PickRate * 100, 1)}%)";
            
            // Ajouter des informations sur les forces du champion
            List<string> strengths = new List<string>();
            
            if (champion.PhysicalDamage > 7)
                strengths.Add("dégâts physiques élevés");
            
            if (champion.MagicalDamage > 7)
                strengths.Add("dégâts magiques élevés");
            
            if (champion.TrueDamage > 5)
                strengths.Add("dégâts vrais");
            
            if (champion.Tankiness > 7)
                strengths.Add("très résistant");
            
            if (champion.Mobility > 7)
                strengths.Add("très mobile");
            
            if (champion.CC > 7)
                strengths.Add("beaucoup de CC");
            
            if (champion.Sustain > 7)
                strengths.Add("sustain important");
            
            if (strengths.Count > 0)
            {
                enrichedReason += $" - {string.Join(", ", strengths)}";
            }
            
            // Ajouter des informations sur les phases de jeu
            if (champion.EarlyGame > 7)
                enrichedReason += " - Très fort en early game";
            else if (champion.MidGame > 7)
                enrichedReason += " - Très fort en mid game";
            else if (champion.LateGame > 7)
                enrichedReason += " - Très fort en late game";
            
            suggestion.Reason = enrichedReason;
        }

        /// <summary>
        /// Normalise le format du tier
        /// </summary>
        private string NormalizeTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return "GOLD"; // Valeur par défaut

            tier = tier.Trim().ToUpperInvariant();

            switch (tier)
            {
                case "IRON":
                case "BRONZE":
                case "SILVER":
                case "GOLD":
                case "PLATINUM":
                case "DIAMOND":
                case "MASTER":
                case "GRANDMASTER":
                case "CHALLENGER":
                    return tier;
                default:
                    return "GOLD"; // Valeur par défaut
            }
        }

        /// <summary>
        /// Normalise le format du rôle
        /// </summary>
        private string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

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
                    return role;
            }
        }
    }
}
