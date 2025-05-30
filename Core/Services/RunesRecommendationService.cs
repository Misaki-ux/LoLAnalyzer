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
    /// Service de recommandation de runes et sorts d'invocateur
    /// </summary>
    public class RunesRecommendationService
    {
        private readonly DatabaseManager _databaseManager;
        private readonly Logger _logger;
        private List<Champion> _champions;
        private List<ChampionRole> _championRoles;
        private bool _isInitialized = false;

        /// <summary>
        /// Constructeur du service de recommandation de runes
        /// </summary>
        /// <param name="databaseManager">Gestionnaire de base de données</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        public RunesRecommendationService(DatabaseManager databaseManager, Logger logger)
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
                _logger.Log(LogLevel.Info, "Initialisation du service de recommandation de runes...");

                // Chargement des données depuis la base de données
                _champions = await _databaseManager.GetAllChampionsAsync();
                _championRoles = await _databaseManager.GetAllChampionRolesAsync();

                _isInitialized = true;
                _logger.Log(LogLevel.Info, "Service de recommandation de runes initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'initialisation du service de recommandation de runes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Recommande des runes pour un champion et un rôle donnés
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="role">Rôle du champion</param>
        /// <param name="enemyTeam">Composition de l'équipe ennemie (optionnel)</param>
        /// <returns>Liste des recommandations de runes</returns>
        public async Task<List<RuneRecommendation>> RecommendRunesAsync(string championName, string role, List<TeamMember> enemyTeam = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (string.IsNullOrWhiteSpace(championName))
                throw new ArgumentException("Le nom du champion ne peut pas être vide", nameof(championName));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Le rôle ne peut pas être vide", nameof(role));

            try
            {
                _logger.Log(LogLevel.Info, $"Génération de recommandations de runes pour {championName} en {role}...");

                // Normalisation du rôle
                role = NormalizeRole(role);

                // Recherche du champion
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(championName, StringComparison.OrdinalIgnoreCase));
                
                if (champion == null)
                    throw new ArgumentException($"Champion non trouvé: {championName}", nameof(championName));

                // Vérification si le champion est viable dans ce rôle
                var championRole = _championRoles.FirstOrDefault(cr => 
                    cr.ChampionId == champion.Id && 
                    cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
                
                if (championRole == null)
                {
                    _logger.Log(LogLevel.Warning, $"{championName} n'est pas couramment joué en {role}, utilisation des données génériques");
                    
                    // Utiliser le rôle le plus courant pour ce champion
                    championRole = _championRoles
                        .Where(cr => cr.ChampionId == champion.Id)
                        .OrderByDescending(cr => cr.PickRate)
                        .FirstOrDefault();
                    
                    if (championRole == null)
                        throw new ArgumentException($"Aucun rôle trouvé pour {championName}", nameof(championName));
                    
                    role = championRole.Role;
                }

                // Récupération des recommandations de runes depuis la base de données
                var championRunes = await _databaseManager.GetChampionRunesAsync(champion.Id, role);
                
                // Si aucune recommandation n'est trouvée, générer des recommandations génériques
                if (championRunes == null || championRunes.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune recommandation de runes trouvée pour {championName} en {role}, génération de recommandations génériques");
                    return GenerateGenericRuneRecommendations(champion, role, enemyTeam);
                }

                // Conversion en modèle de présentation
                var recommendations = new List<RuneRecommendation>();
                
                foreach (var runes in championRunes)
                {
                    var recommendation = new RuneRecommendation
                    {
                        ChampionId = champion.Id,
                        ChampionName = champion.Name,
                        Role = role,
                        PrimaryPathId = runes.PrimaryPathId,
                        SecondaryPathId = runes.SecondaryPathId,
                        PrimaryPathName = GetRunePathName(runes.PrimaryPathId),
                        SecondaryPathName = GetRunePathName(runes.SecondaryPathId),
                        RuneIds = new int[] { runes.Rune1Id, runes.Rune2Id, runes.Rune3Id, runes.Rune4Id, runes.Rune5Id, runes.Rune6Id },
                        RuneNames = new string[] { 
                            GetRuneName(runes.Rune1Id), 
                            GetRuneName(runes.Rune2Id), 
                            GetRuneName(runes.Rune3Id), 
                            GetRuneName(runes.Rune4Id), 
                            GetRuneName(runes.Rune5Id), 
                            GetRuneName(runes.Rune6Id) 
                        },
                        StatMods = new string[] { runes.StatMod1, runes.StatMod2, runes.StatMod3 },
                        WinRate = runes.WinRate * 100,
                        PickRate = runes.PickRate * 100,
                        SampleSize = runes.SampleSize,
                        Explanation = GenerateRuneExplanation(champion, role, runes, enemyTeam),
                        LastUpdated = runes.LastUpdated
                    };
                    
                    recommendations.Add(recommendation);
                }

                // Tri par taux de victoire décroissant
                recommendations = recommendations.OrderByDescending(r => r.WinRate).ToList();

                // Limiter à 3 recommandations
                recommendations = recommendations.Take(3).ToList();

                _logger.Log(LogLevel.Info, $"Recommandations de runes générées: {recommendations.Count} recommandations");
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la génération des recommandations de runes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Recommande des sorts d'invocateur pour un champion et un rôle donnés
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="role">Rôle du champion</param>
        /// <param name="enemyTeam">Composition de l'équipe ennemie (optionnel)</param>
        /// <returns>Liste des recommandations de sorts d'invocateur</returns>
        public async Task<List<SummonerSpellRecommendation>> RecommendSummonerSpellsAsync(string championName, string role, List<TeamMember> enemyTeam = null)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (string.IsNullOrWhiteSpace(championName))
                throw new ArgumentException("Le nom du champion ne peut pas être vide", nameof(championName));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Le rôle ne peut pas être vide", nameof(role));

            try
            {
                _logger.Log(LogLevel.Info, $"Génération de recommandations de sorts d'invocateur pour {championName} en {role}...");

                // Normalisation du rôle
                role = NormalizeRole(role);

                // Recherche du champion
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(championName, StringComparison.OrdinalIgnoreCase));
                
                if (champion == null)
                    throw new ArgumentException($"Champion non trouvé: {championName}", nameof(championName));

                // Vérification si le champion est viable dans ce rôle
                var championRole = _championRoles.FirstOrDefault(cr => 
                    cr.ChampionId == champion.Id && 
                    cr.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
                
                if (championRole == null)
                {
                    _logger.Log(LogLevel.Warning, $"{championName} n'est pas couramment joué en {role}, utilisation des données génériques");
                    
                    // Utiliser le rôle le plus courant pour ce champion
                    championRole = _championRoles
                        .Where(cr => cr.ChampionId == champion.Id)
                        .OrderByDescending(cr => cr.PickRate)
                        .FirstOrDefault();
                    
                    if (championRole == null)
                        throw new ArgumentException($"Aucun rôle trouvé pour {championName}", nameof(championName));
                    
                    role = championRole.Role;
                }

                // Récupération des recommandations de sorts d'invocateur depuis la base de données
                var championSpells = await _databaseManager.GetChampionSummonerSpellsAsync(champion.Id, role);
                
                // Si aucune recommandation n'est trouvée, générer des recommandations génériques
                if (championSpells == null || championSpells.Count == 0)
                {
                    _logger.Log(LogLevel.Warning, $"Aucune recommandation de sorts d'invocateur trouvée pour {championName} en {role}, génération de recommandations génériques");
                    return GenerateGenericSummonerSpellRecommendations(champion, role, enemyTeam);
                }

                // Conversion en modèle de présentation
                var recommendations = new List<SummonerSpellRecommendation>();
                
                foreach (var spells in championSpells)
                {
                    var recommendation = new SummonerSpellRecommendation
                    {
                        ChampionId = champion.Id,
                        ChampionName = champion.Name,
                        Role = role,
                        Spell1Id = spells.Spell1Id,
                        Spell2Id = spells.Spell2Id,
                        Spell1Name = spells.Spell1Name,
                        Spell2Name = spells.Spell2Name,
                        WinRate = spells.WinRate * 100,
                        PickRate = spells.PickRate * 100,
                        SampleSize = spells.SampleSize,
                        Explanation = GenerateSummonerSpellExplanation(champion, role, spells, enemyTeam),
                        LastUpdated = spells.LastUpdated
                    };
                    
                    recommendations.Add(recommendation);
                }

                // Tri par taux de victoire décroissant
                recommendations = recommendations.OrderByDescending(r => r.WinRate).ToList();

                // Limiter à 3 recommandations
                recommendations = recommendations.Take(3).ToList();

                _logger.Log(LogLevel.Info, $"Recommandations de sorts d'invocateur générées: {recommendations.Count} recommandations");
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la génération des recommandations de sorts d'invocateur: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Génère des recommandations de runes génériques pour un champion et un rôle donnés
        /// </summary>
        private List<RuneRecommendation> GenerateGenericRuneRecommendations(Champion champion, string role, List<TeamMember> enemyTeam)
        {
            var recommendations = new List<RuneRecommendation>();
            
            // Détermination des chemins de runes appropriés en fonction des attributs du champion
            int primaryPathId = DeterminePrimaryRunePath(champion, role);
            int secondaryPathId = DetermineSecondaryRunePath(champion, role, primaryPathId);
            
            // Création d'une recommandation générique
            var recommendation = new RuneRecommendation
            {
                ChampionId = champion.Id,
                ChampionName = champion.Name,
                Role = role,
                PrimaryPathId = primaryPathId,
                SecondaryPathId = secondaryPathId,
                PrimaryPathName = GetRunePathName(primaryPathId),
                SecondaryPathName = GetRunePathName(secondaryPathId),
                RuneIds = new int[] { 0, 0, 0, 0, 0, 0 }, // Valeurs par défaut
                RuneNames = new string[] { 
                    "Rune principale générique", 
                    "Rune secondaire générique", 
                    "Rune tertiaire générique", 
                    "Rune quaternaire générique", 
                    "Rune quinaire générique", 
                    "Rune sénaire générique" 
                },
                StatMods = new string[] { "Adaptative", "Adaptative", "Santé" },
                WinRate = 50.0, // Valeur par défaut
                PickRate = 10.0, // Valeur par défaut
                SampleSize = 1000, // Valeur par défaut
                Explanation = $"Recommandation générique pour {champion.Name} en {role} basée sur les attributs du champion.",
                LastUpdated = DateTime.Now
            };
            
            recommendations.Add(recommendation);
            
            return recommendations;
        }

        /// <summary>
        /// Génère des recommandations de sorts d'invocateur génériques pour un champion et un rôle donnés
        /// </summary>
        private List<SummonerSpellRecommendation> GenerateGenericSummonerSpellRecommendations(Champion champion, string role, List<TeamMember> enemyTeam)
        {
            var recommendations = new List<SummonerSpellRecommendation>();
            
            // Détermination des sorts d'invocateur appropriés en fonction du rôle
            string spell1Name = "Flash"; // Par défaut
            string spell2Name = DetermineSecondSummonerSpell(champion, role);
            
            // Création d'une recommandation générique
            var recommendation = new SummonerSpellRecommendation
            {
                ChampionId = champion.Id,
                ChampionName = champion.Name,
                Role = role,
                Spell1Id = 1, // ID de Flash (valeur arbitraire)
                Spell2Id = 2, // ID du second sort (valeur arbitraire)
                Spell1Name = spell1Name,
                Spell2Name = spell2Name,
                WinRate = 50.0, // Valeur par défaut
                PickRate = 10.0, // Valeur par défaut
                SampleSize = 1000, // Valeur par défaut
                Explanation = $"Recommandation générique pour {champion.Name} en {role} basée sur le rôle.",
                LastUpdated = DateTime.Now
            };
            
            recommendations.Add(recommendation);
            
            return recommendations;
        }

        /// <summary>
        /// Détermine le chemin de runes principal approprié pour un champion et un rôle donnés
        /// </summary>
        private int DeterminePrimaryRunePath(Champion champion, string role)
        {
            // Détermination du chemin de runes principal en fonction des attributs du champion
            
            // Précision (AD/AS)
            if (champion.PhysicalDamage > 7 || role == "ADC")
                return 1; // ID de Précision (valeur arbitraire)
            
            // Domination (Burst/Assassin)
            if (champion.Mobility > 7 && (champion.PhysicalDamage > 5 || champion.MagicalDamage > 5))
                return 2; // ID de Domination (valeur arbitraire)
            
            // Sorcellerie (AP/Mage)
            if (champion.MagicalDamage > 7)
                return 3; // ID de Sorcellerie (valeur arbitraire)
            
            // Détermination (Tank)
            if (champion.Tankiness > 7)
                return 4; // ID de Détermination (valeur arbitraire)
            
            // Inspiration (Utility)
            if (champion.Utility > 7 || role == "SUPPORT")
                return 5; // ID d'Inspiration (valeur arbitraire)
            
            // Par défaut, utiliser Précision
            return 1;
        }

        /// <summary>
        /// Détermine le chemin de runes secondaire approprié pour un champion et un rôle donnés
        /// </summary>
        private int DetermineSecondaryRunePath(Champion champion, string role, int primaryPathId)
        {
            // Détermination du chemin de runes secondaire en fonction des attributs du champion
            // et du chemin principal (doit être différent)
            
            // Si le champion est un tank, privilégier Détermination
            if (champion.Tankiness > 7 && primaryPathId != 4)
                return 4; // ID de Détermination (valeur arbitraire)
            
            // Si le champion a beaucoup de dégâts magiques, privilégier Sorcellerie
            if (champion.MagicalDamage > 5 && primaryPathId != 3)
                return 3; // ID de Sorcellerie (valeur arbitraire)
            
            // Si le champion a beaucoup de dégâts physiques, privilégier Précision
            if (champion.PhysicalDamage > 5 && primaryPathId != 1)
                return 1; // ID de Précision (valeur arbitraire)
            
            // Si le champion est un assassin, privilégier Domination
            if (champion.Mobility > 5 && (champion.PhysicalDamage > 5 || champion.MagicalDamage > 5) && primaryPathId != 2)
                return 2; // ID de Domination (valeur arbitraire)
            
            // Si le champion est un support, privilégier Inspiration
            if (champion.Utility > 5 && primaryPathId != 5)
                return 5; // ID d'Inspiration (valeur arbitraire)
            
            // Par défaut, utiliser un chemin différent du chemin principal
            return primaryPathId == 1 ? 3 : 1;
        }

        /// <summary>
        /// Détermine le second sort d'invocateur approprié pour un champion et un rôle donnés
        /// </summary>
        private string DetermineSecondSummonerSpell(Champion champion, string role)
        {
            switch (role)
            {
                case "TOP":
                    return "Téléportation";
                
                case "JUNGLE":
                    return "Châtiment";
                
                case "MID":
                    return champion.Mobility < 5 ? "Téléportation" : "Ignite";
                
                case "ADC":
                    return "Soins";
                
                case "SUPPORT":
                    return "Ignite";
                
                default:
                    return "Téléportation";
            }
        }

        /// <summary>
        /// Génère une explication pour une recommandation de runes
        /// </summary>
        private string GenerateRuneExplanation(Champion champion, string role, ChampionRunes runes, List<TeamMember> enemyTeam)
        {
            string explanation = $"Configuration de runes optimale pour {champion.Name} en {role} ";
            
            // Ajouter des informations sur le taux de victoire et de sélection
            explanation += $"(WR: {Math.Round(runes.WinRate * 100, 1)}%, PR: {Math.Round(runes.PickRate * 100, 1)}%).\n\n";
            
            // Ajouter des informations sur les chemins de runes
            explanation += $"Chemin principal: {GetRunePathName(runes.PrimaryPathId)} - ";
            
            // Explication du choix du chemin principal
            switch (runes.PrimaryPathId)
            {
                case 1: // Précision
                    explanation += "Optimise les dégâts soutenus et la vitesse d'attaque.\n";
                    break;
                case 2: // Domination
                    explanation += "Maximise les dégâts en rafale et la mobilité.\n";
                    break;
                case 3: // Sorcellerie
                    explanation += "Augmente la puissance des sorts et la gestion de mana.\n";
                    break;
                case 4: // Détermination
                    explanation += "Renforce la résistance et la survie en combat.\n";
                    break;
                case 5: // Inspiration
                    explanation += "Améliore l'utilité et les interactions avec les objets.\n";
                    break;
                default:
                    explanation += "Fournit un bon équilibre de statistiques.\n";
                    break;
            }
            
            explanation += $"Chemin secondaire: {GetRunePathName(runes.SecondaryPathId)} - ";
            
            // Explication du choix du chemin secondaire
            switch (runes.SecondaryPathId)
            {
                case 1: // Précision
                    explanation += "Complète avec des bonus d'attaque et de vitesse.\n";
                    break;
                case 2: // Domination
                    explanation += "Ajoute des capacités de burst et de vision.\n";
                    break;
                case 3: // Sorcellerie
                    explanation += "Renforce la puissance magique et la vitesse de déplacement.\n";
                    break;
                case 4: // Détermination
                    explanation += "Apporte de la résistance supplémentaire et de la régénération.\n";
                    break;
                case 5: // Inspiration
                    explanation += "Offre des options utilitaires et de réduction de temps de recharge.\n";
                    break;
                default:
                    explanation += "Complète avec des statistiques polyvalentes.\n";
                    break;
            }
            
            // Ajouter des informations sur les modificateurs de statistiques
            explanation += "\nModificateurs de statistiques: ";
            explanation += $"{runes.StatMod1}, {runes.StatMod2}, {runes.StatMod3}.\n";
            
            // Ajouter des conseils spécifiques en fonction de l'équipe ennemie
            if (enemyTeam != null && enemyTeam.Count > 0)
            {
                bool hasHighCC = AnalyzeEnemyCC(enemyTeam);
                bool hasTanks = AnalyzeEnemyTanks(enemyTeam);
                bool hasMobility = AnalyzeEnemyMobility(enemyTeam);
                
                explanation += "\nConseils contre cette composition ennemie:\n";
                
                if (hasHighCC)
                {
                    explanation += "- Envisagez des runes de ténacité contre le CC ennemi élevé.\n";
                }
                
                if (hasTanks)
                {
                    explanation += "- Privilégiez des runes qui augmentent les dégâts contre les cibles à haute santé.\n";
                }
                
                if (hasMobility)
                {
                    explanation += "- Utilisez des runes qui ralentissent ou immobilisent contre leur mobilité élevée.\n";
                }
            }
            
            return explanation;
        }

        /// <summary>
        /// Génère une explication pour une recommandation de sorts d'invocateur
        /// </summary>
        private string GenerateSummonerSpellExplanation(Champion champion, string role, ChampionSummonerSpells spells, List<TeamMember> enemyTeam)
        {
            string explanation = $"Sorts d'invocateur optimaux pour {champion.Name} en {role} ";
            
            // Ajouter des informations sur le taux de victoire et de sélection
            explanation += $"(WR: {Math.Round(spells.WinRate * 100, 1)}%, PR: {Math.Round(spells.PickRate * 100, 1)}%).\n\n";
            
            // Ajouter des informations sur les sorts d'invocateur
            explanation += $"{spells.Spell1Name} + {spells.Spell2Name}\n\n";
            
            // Explication du choix des sorts
            explanation += "Raisons de ce choix:\n";
            
            // Flash
            if (spells.Spell1Name == "Flash" || spells.Spell2Name == "Flash")
            {
                explanation += "- Flash: Sort polyvalent essentiel pour les engagements, les échappatoires et les jeux offensifs.\n";
            }
            
            // Téléportation
            if (spells.Spell1Name == "Téléportation" || spells.Spell2Name == "Téléportation")
            {
                explanation += "- Téléportation: Permet de rejoindre rapidement les teamfights et d'appliquer une pression sur la carte.\n";
            }
            
            // Ignite
            if (spells.Spell1Name == "Ignite" || spells.Spell2Name == "Ignite")
            {
                explanation += "- Ignite: Augmente le potentiel de kill en early game et réduit les soins ennemis.\n";
            }
            
            // Soins
            if (spells.Spell1Name == "Soins" || spells.Spell2Name == "Soins")
            {
                explanation += "- Soins: Fournit une survie cruciale en lane et dans les escarmouches.\n";
            }
            
            // Châtiment
            if (spells.Spell1Name == "Châtiment" || spells.Spell2Name == "Châtiment")
            {
                explanation += "- Châtiment: Essentiel pour le clear de jungle et le contrôle des objectifs.\n";
            }
            
            // Purge
            if (spells.Spell1Name == "Purge" || spells.Spell2Name == "Purge")
            {
                explanation += "- Purge: Permet de se libérer des effets de contrôle et des ralentissements.\n";
            }
            
            // Barrière
            if (spells.Spell1Name == "Barrière" || spells.Spell2Name == "Barrière")
            {
                explanation += "- Barrière: Offre une protection instantanée contre les bursts de dégâts.\n";
            }
            
            // Fantôme
            if (spells.Spell1Name == "Fantôme" || spells.Spell2Name == "Fantôme")
            {
                explanation += "- Fantôme: Augmente la mobilité en combat et permet de poursuivre ou fuir efficacement.\n";
            }
            
            // Ajouter des conseils spécifiques en fonction de l'équipe ennemie
            if (enemyTeam != null && enemyTeam.Count > 0)
            {
                bool hasHighCC = AnalyzeEnemyCC(enemyTeam);
                bool hasMobility = AnalyzeEnemyMobility(enemyTeam);
                bool hasHighBurst = AnalyzeEnemyBurst(enemyTeam);
                
                explanation += "\nConseils contre cette composition ennemie:\n";
                
                if (hasHighCC && !(spells.Spell1Name == "Purge" || spells.Spell2Name == "Purge"))
                {
                    explanation += "- Envisagez de prendre Purge contre leur CC élevé.\n";
                }
                
                if (hasMobility && !(spells.Spell1Name == "Fantôme" || spells.Spell2Name == "Fantôme"))
                {
                    explanation += "- Fantôme pourrait être utile contre leur mobilité élevée.\n";
                }
                
                if (hasHighBurst && !(spells.Spell1Name == "Barrière" || spells.Spell2Name == "Barrière"))
                {
                    explanation += "- Barrière pourrait vous aider contre leurs dégâts en rafale.\n";
                }
            }
            
            return explanation;
        }

        /// <summary>
        /// Analyse le niveau de CC de la composition ennemie
        /// </summary>
        private bool AnalyzeEnemyCC(List<TeamMember> enemyTeam)
        {
            double totalCC = 0;
            int count = 0;
            
            foreach (var member in enemyTeam)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null)
                {
                    totalCC += champion.CC;
                    count++;
                }
            }
            
            return count > 0 && (totalCC / count) > 6;
        }

        /// <summary>
        /// Analyse la présence de tanks dans la composition ennemie
        /// </summary>
        private bool AnalyzeEnemyTanks(List<TeamMember> enemyTeam)
        {
            foreach (var member in enemyTeam)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null && champion.Tankiness > 7)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Analyse le niveau de mobilité de la composition ennemie
        /// </summary>
        private bool AnalyzeEnemyMobility(List<TeamMember> enemyTeam)
        {
            double totalMobility = 0;
            int count = 0;
            
            foreach (var member in enemyTeam)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null)
                {
                    totalMobility += champion.Mobility;
                    count++;
                }
            }
            
            return count > 0 && (totalMobility / count) > 6;
        }

        /// <summary>
        /// Analyse le niveau de burst de la composition ennemie
        /// </summary>
        private bool AnalyzeEnemyBurst(List<TeamMember> enemyTeam)
        {
            int burstChampions = 0;
            
            foreach (var member in enemyTeam)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null && ((champion.PhysicalDamage > 7 || champion.MagicalDamage > 7) && champion.Mobility > 5))
                {
                    burstChampions++;
                }
            }
            
            return burstChampions >= 2;
        }

        /// <summary>
        /// Récupère le nom d'un chemin de runes à partir de son ID
        /// </summary>
        private string GetRunePathName(int pathId)
        {
            switch (pathId)
            {
                case 1:
                    return "Précision";
                case 2:
                    return "Domination";
                case 3:
                    return "Sorcellerie";
                case 4:
                    return "Détermination";
                case 5:
                    return "Inspiration";
                default:
                    return "Inconnu";
            }
        }

        /// <summary>
        /// Récupère le nom d'une rune à partir de son ID
        /// </summary>
        private string GetRuneName(int runeId)
        {
            // Cette méthode devrait idéalement récupérer le nom de la rune depuis la base de données
            // Pour l'instant, on retourne une valeur par défaut
            return $"Rune {runeId}";
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
