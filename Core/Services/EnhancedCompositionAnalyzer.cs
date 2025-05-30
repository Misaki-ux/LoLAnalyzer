using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service d'analyse de composition d'équipe enrichi
    /// </summary>
    public class EnhancedCompositionAnalyzer
    {
        private readonly Data.EnhancedDatabaseManager _dbManager;
        private readonly Logger _logger;

        /// <summary>
        /// Constructeur du service d'analyse de composition enrichi
        /// </summary>
        /// <param name="dbManager">Gestionnaire de base de données enrichie</param>
        /// <param name="logger">Logger pour tracer les opérations</param>
        public EnhancedCompositionAnalyzer(Data.EnhancedDatabaseManager dbManager, Logger logger)
        {
            _dbManager = dbManager;
            _logger = logger;
        }

        /// <summary>
        /// Analyse une composition d'équipe complète
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Résultat de l'analyse de composition</returns>
        public async Task<TeamCompositionAnalysis> AnalyzeTeamCompositionAsync(List<TeamMember> teamMembers)
        {
            try
            {
                _logger.Info("Analyse de composition d'équipe en cours...");

                // Vérifier que l'équipe est complète
                if (teamMembers == null || teamMembers.Count != 5)
                {
                    throw new ArgumentException("L'équipe doit contenir exactement 5 membres.");
                }

                // Créer l'analyse de composition
                var analysis = new TeamCompositionAnalysis
                {
                    TeamMembers = teamMembers,
                    DamageDistribution = await CalculateDamageDistributionAsync(teamMembers),
                    Strengths = await IdentifyStrengthsAsync(teamMembers),
                    Weaknesses = await IdentifyWeaknessesAsync(teamMembers),
                    PhasePerformance = await CalculatePhasePerformanceAsync(teamMembers),
                    SynergyScore = await CalculateSynergyScoreAsync(teamMembers),
                    MetaMatchScore = await CalculateMetaMatchScoreAsync(teamMembers),
                    TrollPicks = await IdentifyTrollPicksAsync(teamMembers)
                };

                _logger.Info("Analyse de composition d'équipe terminée avec succès.");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'analyse de composition d'équipe", ex);
                throw;
            }
        }

        /// <summary>
        /// Calcule la distribution des dégâts de l'équipe
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Distribution des dégâts (physique, magique, vrai)</returns>
        private async Task<DamageDistribution> CalculateDamageDistributionAsync(List<TeamMember> teamMembers)
        {
            double physical = 0;
            double magical = 0;
            double trueDamage = 0;

            foreach (var member in teamMembers)
            {
                var champion = await _dbManager.GetChampionByIdAsync(member.Champion.Id);
                var tags = champion["Tags"].ToString().Split(',');

                // Calculer la distribution des dégâts en fonction des tags et du rôle
                if (tags.Contains("Marksman") || tags.Contains("Fighter") || tags.Contains("Assassin"))
                {
                    if (member.Role == "ADC")
                    {
                        physical += 0.25;
                    }
                    else if (member.Role == "TOP" || member.Role == "JUNGLE")
                    {
                        physical += 0.15;
                        trueDamage += 0.05;
                    }
                    else
                    {
                        physical += 0.10;
                    }
                }

                if (tags.Contains("Mage") || tags.Contains("Support"))
                {
                    if (member.Role == "MID")
                    {
                        magical += 0.25;
                    }
                    else if (member.Role == "SUPPORT")
                    {
                        magical += 0.15;
                    }
                    else
                    {
                        magical += 0.10;
                    }
                }

                if (tags.Contains("Tank"))
                {
                    physical += 0.05;
                    magical += 0.05;
                    trueDamage += 0.02;
                }

                // Ajustements spécifiques pour certains champions
                switch (member.Champion.Name)
                {
                    case "Fiora":
                        trueDamage += 0.10;
                        break;
                    case "Vayne":
                        trueDamage += 0.08;
                        break;
                    case "Camille":
                        trueDamage += 0.07;
                        break;
                    case "Darius":
                        trueDamage += 0.06;
                        break;
                    case "Ahri":
                        trueDamage += 0.03;
                        break;
                }
            }

            // Normaliser les valeurs pour qu'elles totalisent 1.0
            double total = physical + magical + trueDamage;
            physical /= total;
            magical /= total;
            trueDamage /= total;

            return new DamageDistribution
            {
                Physical = physical,
                Magical = magical,
                True = trueDamage
            };
        }

        /// <summary>
        /// Identifie les forces de la composition d'équipe
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Liste des forces de la composition</returns>
        private async Task<List<string>> IdentifyStrengthsAsync(List<TeamMember> teamMembers)
        {
            var strengths = new List<string>();
            var damageDistribution = await CalculateDamageDistributionAsync(teamMembers);
            var phasePerformance = await CalculatePhasePerformanceAsync(teamMembers);
            var synergyScore = await CalculateSynergyScoreAsync(teamMembers);

            // Vérifier l'équilibre des dégâts
            if (damageDistribution.Physical >= 0.3 && damageDistribution.Magical >= 0.3)
            {
                strengths.Add("Bonne répartition des dégâts physiques et magiques");
            }

            if (damageDistribution.True >= 0.15)
            {
                strengths.Add("Présence significative de dégâts vrais");
            }

            // Vérifier les performances par phase de jeu
            if (phasePerformance.Early >= 0.7)
            {
                strengths.Add("Forte présence en early game");
            }

            if (phasePerformance.Mid >= 0.7)
            {
                strengths.Add("Excellente transition en mid game");
            }

            if (phasePerformance.Late >= 0.7)
            {
                strengths.Add("Scaling puissant en late game");
            }

            // Vérifier les synergies
            if (synergyScore >= 0.8)
            {
                strengths.Add("Excellente synergie entre les champions");
            }
            else if (synergyScore >= 0.6)
            {
                strengths.Add("Bonne synergie globale");
            }

            // Vérifier les rôles et les capacités
            bool hasEngage = false;
            bool hasPeel = false;
            bool hasSplitPush = false;
            bool hasCC = false;
            bool hasPoke = false;
            bool hasSustain = false;

            foreach (var member in teamMembers)
            {
                var champion = await _dbManager.GetChampionByIdAsync(member.Champion.Id);
                var tags = champion["Tags"].ToString().Split(',');

                // Vérifier les capacités en fonction des champions
                switch (member.Champion.Name)
                {
                    case "Malphite":
                    case "Amumu":
                    case "Leona":
                        hasEngage = true;
                        hasCC = true;
                        break;
                    case "Thresh":
                    case "Lulu":
                    case "Janna":
                        hasPeel = true;
                        hasCC = true;
                        break;
                    case "Fiora":
                    case "Jax":
                    case "Tryndamere":
                        hasSplitPush = true;
                        break;
                    case "Ezreal":
                    case "Lux":
                    case "Xerath":
                        hasPoke = true;
                        break;
                    case "Soraka":
                    case "Sona":
                    case "Yuumi":
                        hasSustain = true;
                        break;
                }

                // Vérifier les capacités en fonction des tags
                if (tags.Contains("Tank"))
                {
                    hasEngage = true;
                }

                if (tags.Contains("Support"))
                {
                    hasPeel = true;
                }

                if (tags.Contains("Fighter") && member.Role == "TOP")
                {
                    hasSplitPush = true;
                }
            }

            // Ajouter les forces en fonction des capacités
            if (hasEngage)
            {
                strengths.Add("Bonne capacité d'engagement");
            }

            if (hasPeel)
            {
                strengths.Add("Excellente protection des carries");
            }

            if (hasSplitPush)
            {
                strengths.Add("Option de split push disponible");
            }

            if (hasCC)
            {
                strengths.Add("Contrôle de foule abondant");
            }

            if (hasPoke)
            {
                strengths.Add("Capacité de poke à distance");
            }

            if (hasSustain)
            {
                strengths.Add("Bonne sustain en combat prolongé");
            }

            return strengths;
        }

        /// <summary>
        /// Identifie les faiblesses de la composition d'équipe
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Liste des faiblesses de la composition</returns>
        private async Task<List<string>> IdentifyWeaknessesAsync(List<TeamMember> teamMembers)
        {
            var weaknesses = new List<string>();
            var damageDistribution = await CalculateDamageDistributionAsync(teamMembers);
            var phasePerformance = await CalculatePhasePerformanceAsync(teamMembers);
            var synergyScore = await CalculateSynergyScoreAsync(teamMembers);
            var trollPicks = await IdentifyTrollPicksAsync(teamMembers);

            // Vérifier l'équilibre des dégâts
            if (damageDistribution.Physical < 0.2)
            {
                weaknesses.Add("Manque de dégâts physiques");
            }

            if (damageDistribution.Magical < 0.2)
            {
                weaknesses.Add("Manque de dégâts magiques");
            }

            if (damageDistribution.True < 0.05)
            {
                weaknesses.Add("Peu de dégâts vrais");
            }

            // Vérifier les performances par phase de jeu
            if (phasePerformance.Early < 0.5)
            {
                weaknesses.Add("Early game faible");
            }

            if (phasePerformance.Mid < 0.5)
            {
                weaknesses.Add("Transition difficile en mid game");
            }

            if (phasePerformance.Late < 0.5)
            {
                weaknesses.Add("Scaling limité en late game");
            }

            // Vérifier les synergies
            if (synergyScore < 0.4)
            {
                weaknesses.Add("Faible synergie entre les champions");
            }

            // Vérifier les picks problématiques
            if (trollPicks.Count > 0)
            {
                foreach (var trollPick in trollPicks)
                {
                    weaknesses.Add($"{trollPick.Champion.Name} {trollPick.Role} est un pick problématique");
                }
            }

            // Vérifier les rôles et les capacités
            bool hasEngage = false;
            bool hasPeel = false;
            bool hasTank = false;
            bool hasCC = false;
            bool hasSustain = false;

            foreach (var member in teamMembers)
            {
                var champion = await _dbManager.GetChampionByIdAsync(member.Champion.Id);
                var tags = champion["Tags"].ToString().Split(',');

                // Vérifier les capacités en fonction des champions
                switch (member.Champion.Name)
                {
                    case "Malphite":
                    case "Amumu":
                    case "Leona":
                        hasEngage = true;
                        hasCC = true;
                        break;
                    case "Thresh":
                    case "Lulu":
                    case "Janna":
                        hasPeel = true;
                        hasCC = true;
                        break;
                    case "Soraka":
                    case "Sona":
                    case "Yuumi":
                        hasSustain = true;
                        break;
                }

                // Vérifier les capacités en fonction des tags
                if (tags.Contains("Tank"))
                {
                    hasTank = true;
                }

                if (tags.Contains("Support"))
                {
                    hasPeel = true;
                }
            }

            // Ajouter les faiblesses en fonction des capacités manquantes
            if (!hasEngage)
            {
                weaknesses.Add("Manque de capacité d'engagement");
            }

            if (!hasPeel)
            {
                weaknesses.Add("Protection des carries limitée");
            }

            if (!hasTank)
            {
                weaknesses.Add("Absence de tank/frontline");
            }

            if (!hasCC)
            {
                weaknesses.Add("Contrôle de foule limité");
            }

            if (!hasSustain)
            {
                weaknesses.Add("Faible sustain en combat prolongé");
            }

            return weaknesses;
        }

        /// <summary>
        /// Calcule les performances de l'équipe par phase de jeu
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Performances par phase de jeu</returns>
        private async Task<PhasePerformance> CalculatePhasePerformanceAsync(List<TeamMember> teamMembers)
        {
            double earlyScore = 0;
            double midScore = 0;
            double lateScore = 0;

            foreach (var member in teamMembers)
            {
                var phaseStats = await _dbManager.GetChampionPhaseStatsAsync(member.Champion.Id, member.Role);

                if (phaseStats.ContainsKey("EARLY"))
                {
                    earlyScore += phaseStats["EARLY"] * GetRoleWeight(member.Role);
                }

                if (phaseStats.ContainsKey("MID"))
                {
                    midScore += phaseStats["MID"] * GetRoleWeight(member.Role);
                }

                if (phaseStats.ContainsKey("LATE"))
                {
                    lateScore += phaseStats["LATE"] * GetRoleWeight(member.Role);
                }
            }

            // Normaliser les scores
            double totalWeight = teamMembers.Sum(m => GetRoleWeight(m.Role));
            earlyScore /= totalWeight;
            midScore /= totalWeight;
            lateScore /= totalWeight;

            return new PhasePerformance
            {
                Early = earlyScore,
                Mid = midScore,
                Late = lateScore
            };
        }

        /// <summary>
        /// Calcule le score de synergie de l'équipe
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Score de synergie</returns>
        private async Task<double> CalculateSynergyScoreAsync(List<TeamMember> teamMembers)
        {
            double synergyScore = 0;
            int synergyCount = 0;

            // Vérifier les synergies entre chaque paire de champions
            for (int i = 0; i < teamMembers.Count; i++)
            {
                for (int j = i + 1; j < teamMembers.Count; j++)
                {
                    var synergies = await _dbManager.GetChampionSynergiesAsync(teamMembers[i].Champion.Id);
                    var synergy = synergies.FirstOrDefault(s => (int)s["Champion2Id"] == teamMembers[j].Champion.Id);

                    if (synergy != null)
                    {
                        synergyScore += (double)synergy["SynergyScore"];
                        synergyCount++;
                    }
                }
            }

            // Calculer le score moyen de synergie
            if (synergyCount > 0)
            {
                synergyScore /= synergyCount;
            }
            else
            {
                // Score par défaut si aucune synergie n'est trouvée
                synergyScore = 0.5;
            }

            return synergyScore;
        }

        /// <summary>
        /// Calcule le score de correspondance avec les compositions méta
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Score de correspondance méta</returns>
        private async Task<double> CalculateMetaMatchScoreAsync(List<TeamMember> teamMembers)
        {
            double bestMatchScore = 0;
            string bestMatchName = "";

            // Récupérer toutes les compositions méta
            var metaCompositions = await _dbManager.GetMetaCompositionsAsync();

            foreach (var composition in metaCompositions)
            {
                int compositionId = (int)composition["Id"];
                var metaChampions = await _dbManager.GetMetaCompositionChampionsAsync(compositionId);

                // Calculer le score de correspondance pour cette composition méta
                double matchScore = 0;
                int matchCount = 0;

                foreach (var member in teamMembers)
                {
                    var metaChampion = metaChampions.FirstOrDefault(c => 
                        (string)c["Role"] == member.Role && 
                        (int)c["ChampionId"] == member.Champion.Id);

                    if (metaChampion != null)
                    {
                        matchScore += 1.0;
                    }
                    else
                    {
                        // Vérifier si le rôle correspond même si le champion est différent
                        var roleMatch = metaChampions.FirstOrDefault(c => (string)c["Role"] == member.Role);
                        if (roleMatch != null)
                        {
                            matchScore += 0.5;
                        }
                    }

                    matchCount++;
                }

                // Normaliser le score
                matchScore /= matchCount;

                // Mettre à jour le meilleur score
                if (matchScore > bestMatchScore)
                {
                    bestMatchScore = matchScore;
                    bestMatchName = (string)composition["Name"];
                }
            }

            return bestMatchScore;
        }

        /// <summary>
        /// Identifie les picks problématiques dans l'équipe
        /// </summary>
        /// <param name="teamMembers">Liste des membres de l'équipe</param>
        /// <returns>Liste des picks problématiques</returns>
        private async Task<List<TeamMember>> IdentifyTrollPicksAsync(List<TeamMember> teamMembers)
        {
            var trollPicks = new List<TeamMember>();

            foreach (var member in teamMembers)
            {
                var trollPick = await _dbManager.GetTrollPickAsync(member.Champion.Id, member.Role);

                if (trollPick != null)
                {
                    trollPicks.Add(member);
                }
            }

            return trollPicks;
        }

        /// <summary>
        /// Obtient le poids d'un rôle pour les calculs de performance
        /// </summary>
        /// <param name="role">Rôle du champion</param>
        /// <returns>Poids du rôle</returns>
        private double GetRoleWeight(string role)
        {
            switch (role)
            {
                case "JUNGLE":
                    return 1.2; // Le jungler a un impact plus important en early game
                case "MID":
                    return 1.1; // Le mid a un impact important en mid game
                case "ADC":
                    return 1.3; // L'ADC a un impact crucial en late game
                case "SUPPORT":
                    return 0.9; // Le support a un impact moindre en termes de dégâts
                case "TOP":
                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Suggère des contre-picks pour un rôle spécifique contre une équipe ennemie
        /// </summary>
        /// <param name="enemyTeam">Liste des membres de l'équipe ennemie</param>
        /// <param name="role">Rôle pour lequel suggérer des contre-picks</param>
        /// <returns>Liste des suggestions de contre-picks</returns>
        public async Task<List<CounterPickSuggestion>> SuggestCounterPicksAsync(List<TeamMember> enemyTeam, string role)
        {
            try
            {
                _logger.Info($"Suggestion de contre-picks pour le rôle {role}...");

                // Vérifier que l'équipe ennemie est valide
                if (enemyTeam == null || enemyTeam.Count == 0)
                {
                    throw new ArgumentException("L'équipe ennemie ne peut pas être vide.");
                }

                // Trouver l'adversaire direct en fonction du rôle
                var directOpponent = enemyTeam.FirstOrDefault(m => m.Role == role);
                
                // Récupérer les contre-picks pour l'adversaire direct
                var counterPicks = new List<CounterPickSuggestion>();
                
                if (directOpponent != null)
                {
                    var directCounters = await _dbManager.GetCounterPicksAsync(directOpponent.Champion.Id, role);
                    
                    foreach (var counter in directCounters)
                    {
                        var champion = new Champion
                        {
                            Id = (int)counter["CounterChampionId"],
                            Name = (string)counter["ChampionName"],
                            ImageUrl = (string)counter["ChampionImageUrl"]
                        };
                        
                        counterPicks.Add(new CounterPickSuggestion
                        {
                            Champion = champion,
                            WinRate = (double)counter["WinRate"],
                            CounterScore = (double)counter["CounterScore"],
                            Difficulty = (string)counter["Difficulty"],
                            Explanation = (string)counter["Explanation"]
                        });
                    }
                }
                
                // Si nous n'avons pas assez de contre-picks directs, ajouter des champions forts dans ce rôle
                if (counterPicks.Count < 3)
                {
                    // Logique pour ajouter des champions forts dans ce rôle
                    // Cette partie serait implémentée dans une version complète
                }
                
                // Trier les contre-picks par score de contre et limiter à 3
                counterPicks = counterPicks
                    .OrderByDescending(c => c.CounterScore)
                    .Take(3)
                    .ToList();
                
                _logger.Info($"Suggestion de contre-picks pour le rôle {role} terminée avec succès.");
                return counterPicks;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de la suggestion de contre-picks pour le rôle {role}", ex);
                throw;
            }
        }

        /// <summary>
        /// Suggère des alternatives pour un pick problématique
        /// </summary>
        /// <param name="trollPick">Pick problématique</param>
        /// <returns>Liste des alternatives suggérées</returns>
        public async Task<List<Champion>> SuggestTrollPickAlternativesAsync(TeamMember trollPick)
        {
            try
            {
                _logger.Info($"Suggestion d'alternatives pour {trollPick.Champion.Name} {trollPick.Role}...");

                var alternatives = await _dbManager.GetTrollPickAlternativesAsync(trollPick.Champion.Id, trollPick.Role);
                var result = new List<Champion>();

                foreach (var alternative in alternatives)
                {
                    var champion = new Champion
                    {
                        Id = (int)alternative["AlternativeChampionId"],
                        Name = (string)alternative["ChampionName"],
                        ImageUrl = (string)alternative["ChampionImageUrl"]
                    };

                    result.Add(champion);
                }

                _logger.Info($"Suggestion d'alternatives pour {trollPick.Champion.Name} {trollPick.Role} terminée avec succès.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de la suggestion d'alternatives pour {trollPick.Champion.Name} {trollPick.Role}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Représente l'analyse d'une composition d'équipe
    /// </summary>
    public class TeamCompositionAnalysis
    {
        /// <summary>
        /// Liste des membres de l'équipe
        /// </summary>
        public List<TeamMember> TeamMembers { get; set; }

        /// <summary>
        /// Distribution des dégâts de l'équipe
        /// </summary>
        public DamageDistribution DamageDistribution { get; set; }

        /// <summary>
        /// Forces de la composition
        /// </summary>
        public List<string> Strengths { get; set; }

        /// <summary>
        /// Faiblesses de la composition
        /// </summary>
        public List<string> Weaknesses { get; set; }

        /// <summary>
        /// Performance par phase de jeu
        /// </summary>
        public PhasePerformance PhasePerformance { get; set; }

        /// <summary>
        /// Score de synergie entre les champions
        /// </summary>
        public double SynergyScore { get; set; }

        /// <summary>
        /// Score de correspondance avec les compositions méta
        /// </summary>
        public double MetaMatchScore { get; set; }

        /// <summary>
        /// Liste des picks problématiques
        /// </summary>
        public List<TeamMember> TrollPicks { get; set; }
    }

    /// <summary>
    /// Représente la distribution des dégâts d'une équipe
    /// </summary>
    public class DamageDistribution
    {
        /// <summary>
        /// Pourcentage de dégâts physiques
        /// </summary>
        public double Physical { get; set; }

        /// <summary>
        /// Pourcentage de dégâts magiques
        /// </summary>
        public double Magical { get; set; }

        /// <summary>
        /// Pourcentage de dégâts vrais
        /// </summary>
        public double True { get; set; }
    }

    /// <summary>
    /// Représente la performance d'une équipe par phase de jeu
    /// </summary>
    public class PhasePerformance
    {
        /// <summary>
        /// Score de performance en early game
        /// </summary>
        public double Early { get; set; }

        /// <summary>
        /// Score de performance en mid game
        /// </summary>
        public double Mid { get; set; }

        /// <summary>
        /// Score de performance en late game
        /// </summary>
        public double Late { get; set; }
    }

    /// <summary>
    /// Représente une suggestion de contre-pick
    /// </summary>
    public class CounterPickSuggestion
    {
        /// <summary>
        /// Champion suggéré
        /// </summary>
        public Champion Champion { get; set; }

        /// <summary>
        /// Taux de victoire contre le champion adverse
        /// </summary>
        public double WinRate { get; set; }

        /// <summary>
        /// Score de contre (efficacité du contre-pick)
        /// </summary>
        public double CounterScore { get; set; }

        /// <summary>
        /// Difficulté d'utilisation du champion
        /// </summary>
        public string Difficulty { get; set; }

        /// <summary>
        /// Explication de l'efficacité du contre-pick
        /// </summary>
        public string Explanation { get; set; }
    }
}
