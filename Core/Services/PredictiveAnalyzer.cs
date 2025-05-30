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
    /// Service d'analyse prédictive pour les compositions d'équipe
    /// </summary>
    public class PredictiveAnalyzer
    {
        private readonly DatabaseManager _databaseManager;
        private readonly Logger _logger;
        private List<Champion> _champions;
        private List<ChampionRole> _championRoles;
        private List<Synergy> _synergies;
        private List<CounterPick> _counterPicks;
        private bool _isInitialized = false;

        // Facteurs de pondération pour l'analyse prédictive
        private const double WEIGHT_COUNTER_PICKS = 0.25;
        private const double WEIGHT_SYNERGIES = 0.20;
        private const double WEIGHT_META_STRENGTH = 0.15;
        private const double WEIGHT_ROLE_PERFORMANCE = 0.15;
        private const double WEIGHT_TEAM_COMPOSITION = 0.15;
        private const double WEIGHT_GAME_PHASE_MATCH = 0.10;

        /// <summary>
        /// Constructeur du service d'analyse prédictive
        /// </summary>
        /// <param name="databaseManager">Gestionnaire de base de données</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        public PredictiveAnalyzer(DatabaseManager databaseManager, Logger logger)
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
                _logger.Log(LogLevel.Info, "Initialisation du service d'analyse prédictive...");

                // Chargement des données depuis la base de données
                _champions = await _databaseManager.GetAllChampionsAsync();
                _championRoles = await _databaseManager.GetAllChampionRolesAsync();
                _synergies = await _databaseManager.GetAllSynergiesAsync();
                _counterPicks = await _databaseManager.GetAllCounterPicksAsync();

                _isInitialized = true;
                _logger.Log(LogLevel.Info, "Service d'analyse prédictive initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'initialisation du service d'analyse prédictive: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Analyse prédictive des chances de victoire pour deux compositions d'équipe
        /// </summary>
        /// <param name="blueTeam">Composition de l'équipe bleue</param>
        /// <param name="redTeam">Composition de l'équipe rouge</param>
        /// <returns>Résultat de l'analyse prédictive</returns>
        public async Task<PredictiveResult> PredictWinChanceAsync(List<TeamMember> blueTeam, List<TeamMember> redTeam)
        {
            if (!_isInitialized)
                await InitializeAsync();

            if (blueTeam == null || blueTeam.Count == 0)
                throw new ArgumentException("La composition de l'équipe bleue ne peut pas être vide", nameof(blueTeam));

            if (redTeam == null || redTeam.Count == 0)
                throw new ArgumentException("La composition de l'équipe rouge ne peut pas être vide", nameof(redTeam));

            try
            {
                _logger.Log(LogLevel.Info, "Analyse prédictive des chances de victoire...");

                // Récupération des données des champions
                var blueChampions = GetChampionsData(blueTeam);
                var redChampions = GetChampionsData(redTeam);

                // Analyse des contre-picks
                double blueCounterScore = AnalyzeCounterPicks(blueTeam, redTeam);
                double redCounterScore = AnalyzeCounterPicks(redTeam, blueTeam);

                // Analyse des synergies
                double blueSynergyScore = AnalyzeSynergies(blueTeam);
                double redSynergyScore = AnalyzeSynergies(redTeam);

                // Analyse de la force méta
                double blueMetaScore = AnalyzeMetaStrength(blueTeam);
                double redMetaScore = AnalyzeMetaStrength(redTeam);

                // Analyse des performances par rôle
                double blueRoleScore = AnalyzeRolePerformance(blueTeam);
                double redRoleScore = AnalyzeRolePerformance(redTeam);

                // Analyse de la composition d'équipe
                double blueCompositionScore = AnalyzeTeamComposition(blueChampions);
                double redCompositionScore = AnalyzeTeamComposition(redChampions);

                // Analyse de la correspondance des phases de jeu
                double blueGamePhaseScore = AnalyzeGamePhaseMatch(blueChampions);
                double redGamePhaseScore = AnalyzeGamePhaseMatch(redChampions);

                // Calcul du score global pondéré
                double blueScore = (blueCounterScore * WEIGHT_COUNTER_PICKS) +
                                  (blueSynergyScore * WEIGHT_SYNERGIES) +
                                  (blueMetaScore * WEIGHT_META_STRENGTH) +
                                  (blueRoleScore * WEIGHT_ROLE_PERFORMANCE) +
                                  (blueCompositionScore * WEIGHT_TEAM_COMPOSITION) +
                                  (blueGamePhaseScore * WEIGHT_GAME_PHASE_MATCH);

                double redScore = (redCounterScore * WEIGHT_COUNTER_PICKS) +
                                 (redSynergyScore * WEIGHT_SYNERGIES) +
                                 (redMetaScore * WEIGHT_META_STRENGTH) +
                                 (redRoleScore * WEIGHT_ROLE_PERFORMANCE) +
                                 (redCompositionScore * WEIGHT_TEAM_COMPOSITION) +
                                 (redGamePhaseScore * WEIGHT_GAME_PHASE_MATCH);

                // Normalisation des scores pour obtenir un pourcentage
                double totalScore = blueScore + redScore;
                double blueWinChance = (blueScore / totalScore) * 100;
                double redWinChance = (redScore / totalScore) * 100;

                // Arrondi à 1 décimale
                blueWinChance = Math.Round(blueWinChance, 1);
                redWinChance = Math.Round(redWinChance, 1);

                // Détermination des forces et faiblesses
                var blueStrengths = DetermineTeamStrengths(blueTeam, redTeam, blueChampions);
                var blueWeaknesses = DetermineTeamWeaknesses(blueTeam, redTeam, blueChampions);
                var redStrengths = DetermineTeamStrengths(redTeam, blueTeam, redChampions);
                var redWeaknesses = DetermineTeamWeaknesses(redTeam, blueTeam, redChampions);

                // Détermination des facteurs clés
                var keyFactors = DetermineKeyFactors(blueTeam, redTeam, blueScore, redScore);

                // Création du résultat
                var result = new PredictiveResult
                {
                    BlueTeamWinChance = blueWinChance,
                    RedTeamWinChance = redWinChance,
                    BlueTeamStrengths = blueStrengths,
                    BlueTeamWeaknesses = blueWeaknesses,
                    RedTeamStrengths = redStrengths,
                    RedTeamWeaknesses = redWeaknesses,
                    KeyFactors = keyFactors,
                    EarlyGameFavorite = DetermineEarlyGameFavorite(blueChampions, redChampions),
                    MidGameFavorite = DetermineMidGameFavorite(blueChampions, redChampions),
                    LateGameFavorite = DetermineLateGameFavorite(blueChampions, redChampions)
                };

                _logger.Log(LogLevel.Info, $"Analyse prédictive terminée: Bleu {blueWinChance}% - Rouge {redWinChance}%");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de l'analyse prédictive: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère les données complètes des champions de la composition
        /// </summary>
        private List<Champion> GetChampionsData(List<TeamMember> teamComposition)
        {
            var result = new List<Champion>();
            
            foreach (var member in teamComposition)
            {
                var champion = _champions.FirstOrDefault(c => 
                    c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null)
                {
                    // Ajouter le rôle actuel au champion
                    champion.CurrentRole = _championRoles.FirstOrDefault(r => 
                        r.ChampionId == champion.Id && 
                        r.Role.Equals(member.Role, StringComparison.OrdinalIgnoreCase));
                    
                    result.Add(champion);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Analyse les contre-picks entre deux équipes
        /// </summary>
        private double AnalyzeCounterPicks(List<TeamMember> team1, List<TeamMember> team2)
        {
            double counterScore = 0;
            int counterCount = 0;

            // Pour chaque membre de l'équipe 1
            foreach (var member1 in team1)
            {
                // Trouver l'adversaire direct dans l'équipe 2
                var directOpponent = team2.FirstOrDefault(m => NormalizeRole(m.Role) == NormalizeRole(member1.Role));
                
                if (directOpponent != null)
                {
                    // Vérifier si member1 counter directOpponent
                    var champion1 = _champions.FirstOrDefault(c => c.Name.Equals(member1.ChampionName, StringComparison.OrdinalIgnoreCase));
                    var champion2 = _champions.FirstOrDefault(c => c.Name.Equals(directOpponent.ChampionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (champion1 != null && champion2 != null)
                    {
                        var counterPick = _counterPicks.FirstOrDefault(cp => 
                            cp.ChampionId == champion2.Id && 
                            cp.CounterId == champion1.Id && 
                            cp.Role.Equals(NormalizeRole(member1.Role), StringComparison.OrdinalIgnoreCase));
                        
                        if (counterPick != null)
                        {
                            counterScore += counterPick.Effectiveness * 10; // Échelle de 0-10
                            counterCount++;
                        }
                    }
                }
            }

            // Si aucun counter n'a été trouvé, retourner un score neutre
            if (counterCount == 0)
                return 5.0;

            return counterScore / counterCount;
        }

        /// <summary>
        /// Analyse les synergies au sein d'une équipe
        /// </summary>
        private double AnalyzeSynergies(List<TeamMember> team)
        {
            double synergyScore = 0;
            int synergyCount = 0;

            // Pour chaque paire de champions dans l'équipe
            for (int i = 0; i < team.Count; i++)
            {
                for (int j = i + 1; j < team.Count; j++)
                {
                    var champion1 = _champions.FirstOrDefault(c => c.Name.Equals(team[i].ChampionName, StringComparison.OrdinalIgnoreCase));
                    var champion2 = _champions.FirstOrDefault(c => c.Name.Equals(team[j].ChampionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (champion1 != null && champion2 != null)
                    {
                        var synergy = _synergies.FirstOrDefault(s => 
                            (s.Champion1Id == champion1.Id && s.Champion2Id == champion2.Id) ||
                            (s.Champion1Id == champion2.Id && s.Champion2Id == champion1.Id));
                        
                        if (synergy != null)
                        {
                            synergyScore += synergy.Strength * 10; // Échelle de 0-10
                            synergyCount++;
                        }
                    }
                }
            }

            // Si aucune synergie n'a été trouvée, retourner un score neutre
            if (synergyCount == 0)
                return 5.0;

            return synergyScore / synergyCount;
        }

        /// <summary>
        /// Analyse la force méta des champions d'une équipe
        /// </summary>
        private double AnalyzeMetaStrength(List<TeamMember> team)
        {
            double metaScore = 0;

            foreach (var member in team)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null)
                {
                    var role = _championRoles.FirstOrDefault(r => 
                        r.ChampionId == champion.Id && 
                        r.Role.Equals(NormalizeRole(member.Role), StringComparison.OrdinalIgnoreCase));
                    
                    if (role != null)
                    {
                        // Calcul du score méta basé sur le taux de victoire, le taux de sélection et le taux de bannissement
                        double winRateScore = role.WinRate * 100; // 0-100
                        double pickRateScore = role.PickRate * 100; // 0-100
                        double banRateScore = role.BanRate * 100; // 0-100
                        
                        // Normalisation sur une échelle de 0-10
                        double normalizedScore = ((winRateScore - 45) * 0.6 + // Win rate au-dessus de 45% est positif
                                                 (pickRateScore * 0.3) +      // Pick rate élevé est positif
                                                 (banRateScore * 0.1)) / 10;  // Ban rate élevé est légèrement positif
                        
                        // Limiter le score entre 0 et 10
                        normalizedScore = Math.Max(0, Math.Min(10, normalizedScore));
                        
                        metaScore += normalizedScore;
                    }
                    else
                    {
                        // Si le rôle n'est pas trouvé, c'est probablement un pick hors méta
                        metaScore += 3.0; // Score faible mais pas nul
                    }
                }
            }

            return metaScore / team.Count;
        }

        /// <summary>
        /// Analyse les performances par rôle des champions d'une équipe
        /// </summary>
        private double AnalyzeRolePerformance(List<TeamMember> team)
        {
            double roleScore = 0;

            foreach (var member in team)
            {
                var champion = _champions.FirstOrDefault(c => c.Name.Equals(member.ChampionName, StringComparison.OrdinalIgnoreCase));
                
                if (champion != null)
                {
                    var role = _championRoles.FirstOrDefault(r => 
                        r.ChampionId == champion.Id && 
                        r.Role.Equals(NormalizeRole(member.Role), StringComparison.OrdinalIgnoreCase));
                    
                    if (role != null)
                    {
                        // Score basé sur la viabilité du champion dans ce rôle
                        roleScore += role.Viability * 10; // Échelle de 0-10
                    }
                    else
                    {
                        // Si le rôle n'est pas trouvé, c'est probablement un pick non viable
                        roleScore += 2.0; // Score très faible
                    }
                }
            }

            return roleScore / team.Count;
        }

        /// <summary>
        /// Analyse la composition d'équipe (équilibre des dégâts, CC, mobilité, etc.)
        /// </summary>
        private double AnalyzeTeamComposition(List<Champion> champions)
        {
            if (champions == null || champions.Count == 0)
                return 5.0; // Score neutre

            // Calcul des moyennes des attributs de l'équipe
            double avgPhysicalDamage = champions.Average(c => c.PhysicalDamage);
            double avgMagicalDamage = champions.Average(c => c.MagicalDamage);
            double avgTrueDamage = champions.Average(c => c.TrueDamage);
            double avgTankiness = champions.Average(c => c.Tankiness);
            double avgMobility = champions.Average(c => c.Mobility);
            double avgCC = champions.Average(c => c.CC);
            double avgSustain = champions.Average(c => c.Sustain);
            double avgUtility = champions.Average(c => c.Utility);

            // Vérification de l'équilibre des dégâts
            double totalDamage = avgPhysicalDamage + avgMagicalDamage + avgTrueDamage;
            double physicalRatio = avgPhysicalDamage / totalDamage;
            double magicalRatio = avgMagicalDamage / totalDamage;
            
            // Score d'équilibre des dégâts (meilleur quand proche de 50/50)
            double damageBalanceScore = 10 - (Math.Abs(physicalRatio - 0.5) + Math.Abs(magicalRatio - 0.5)) * 10;
            
            // Score de présence de tank
            double tankScore = avgTankiness > 5 ? 10 : avgTankiness * 2;
            
            // Score de CC
            double ccScore = avgCC * 1.25; // Bonus pour le CC
            
            // Score de mobilité
            double mobilityScore = avgMobility;
            
            // Score de sustain
            double sustainScore = avgSustain;
            
            // Score d'utilité
            double utilityScore = avgUtility;
            
            // Score global de composition
            double compositionScore = (damageBalanceScore * 0.3) +
                                     (tankScore * 0.2) +
                                     (ccScore * 0.2) +
                                     (mobilityScore * 0.1) +
                                     (sustainScore * 0.1) +
                                     (utilityScore * 0.1);
            
            return Math.Min(10, compositionScore); // Limiter à 10
        }

        /// <summary>
        /// Analyse la correspondance des phases de jeu d'une équipe
        /// </summary>
        private double AnalyzeGamePhaseMatch(List<Champion> champions)
        {
            if (champions == null || champions.Count == 0)
                return 5.0; // Score neutre

            // Calcul des moyennes par phase de jeu
            double avgEarlyGame = champions.Average(c => c.EarlyGame);
            double avgMidGame = champions.Average(c => c.MidGame);
            double avgLateGame = champions.Average(c => c.LateGame);

            // Recherche de la phase de jeu la plus forte
            double maxPhase = Math.Max(avgEarlyGame, Math.Max(avgMidGame, avgLateGame));
            
            // Score basé sur la cohérence des phases de jeu
            double phaseConsistencyScore;
            
            if (maxPhase == avgEarlyGame)
            {
                // Équipe early game
                phaseConsistencyScore = (avgEarlyGame * 0.6) + (avgMidGame * 0.3) + (avgLateGame * 0.1);
            }
            else if (maxPhase == avgMidGame)
            {
                // Équipe mid game
                phaseConsistencyScore = (avgEarlyGame * 0.3) + (avgMidGame * 0.6) + (avgLateGame * 0.3);
            }
            else
            {
                // Équipe late game
                phaseConsistencyScore = (avgEarlyGame * 0.1) + (avgMidGame * 0.3) + (avgLateGame * 0.6);
            }
            
            return phaseConsistencyScore;
        }

        /// <summary>
        /// Détermine les forces d'une équipe par rapport à l'équipe adverse
        /// </summary>
        private string[] DetermineTeamStrengths(List<TeamMember> team, List<TeamMember> enemyTeam, List<Champion> champions)
        {
            var strengths = new List<string>();
            
            // Analyse des contre-picks
            double counterScore = AnalyzeCounterPicks(team, enemyTeam);
            if (counterScore > 7)
            {
                strengths.Add("Avantage significatif en matchups individuels");
            }
            else if (counterScore > 6)
            {
                strengths.Add("Bons matchups individuels");
            }
            
            // Analyse des synergies
            double synergyScore = AnalyzeSynergies(team);
            if (synergyScore > 7)
            {
                strengths.Add("Excellentes synergies entre champions");
            }
            else if (synergyScore > 6)
            {
                strengths.Add("Bonnes synergies d'équipe");
            }
            
            // Analyse de la composition
            if (champions != null && champions.Count > 0)
            {
                // Vérification du CC
                double totalCC = champions.Sum(c => c.CC);
                if (totalCC > 25)
                {
                    strengths.Add("Contrôle de foule exceptionnel");
                }
                else if (totalCC > 20)
                {
                    strengths.Add("Bon contrôle de foule");
                }
                
                // Vérification de la mobilité
                double totalMobility = champions.Sum(c => c.Mobility);
                if (totalMobility > 25)
                {
                    strengths.Add("Mobilité exceptionnelle");
                }
                else if (totalMobility > 20)
                {
                    strengths.Add("Bonne mobilité d'équipe");
                }
                
                // Vérification du sustain
                double totalSustain = champions.Sum(c => c.Sustain);
                if (totalSustain > 25)
                {
                    strengths.Add("Sustain exceptionnel");
                }
                else if (totalSustain > 20)
                {
                    strengths.Add("Bon sustain en combat");
                }
                
                // Vérification de l'équilibre des dégâts
                double totalPhysical = champions.Sum(c => c.PhysicalDamage);
                double totalMagical = champions.Sum(c => c.MagicalDamage);
                double totalDamage = totalPhysical + totalMagical + champions.Sum(c => c.TrueDamage);
                double physicalRatio = totalPhysical / totalDamage;
                double magicalRatio = totalMagical / totalDamage;
                
                if (Math.Abs(physicalRatio - magicalRatio) < 0.2)
                {
                    strengths.Add("Excellent équilibre de dégâts physiques/magiques");
                }
                else if (Math.Abs(physicalRatio - magicalRatio) < 0.3)
                {
                    strengths.Add("Bon équilibre de dégâts");
                }
                
                // Vérification des phases de jeu
                double avgEarlyGame = champions.Average(c => c.EarlyGame);
                double avgMidGame = champions.Average(c => c.MidGame);
                double avgLateGame = champions.Average(c => c.LateGame);
                
                if (avgEarlyGame > 7)
                {
                    strengths.Add("Early game très fort");
                }
                
                if (avgMidGame > 7)
                {
                    strengths.Add("Mid game très fort");
                }
                
                if (avgLateGame > 7)
                {
                    strengths.Add("Late game très fort");
                }
                
                // Vérification de la présence d'un tank
                bool hasTank = champions.Any(c => c.Tankiness > 7);
                if (hasTank)
                {
                    strengths.Add("Bonne frontline/tank");
                }
            }
            
            return strengths.ToArray();
        }

        /// <summary>
        /// Détermine les faiblesses d'une équipe par rapport à l'équipe adverse
        /// </summary>
        private string[] DetermineTeamWeaknesses(List<TeamMember> team, List<TeamMember> enemyTeam, List<Champion> champions)
        {
            var weaknesses = new List<string>();
            
            // Analyse des contre-picks
            double counterScore = AnalyzeCounterPicks(team, enemyTeam);
            if (counterScore < 4)
            {
                weaknesses.Add("Désavantage significatif en matchups individuels");
            }
            else if (counterScore < 5)
            {
                weaknesses.Add("Matchups individuels difficiles");
            }
            
            // Analyse des synergies
            double synergyScore = AnalyzeSynergies(team);
            if (synergyScore < 4)
            {
                weaknesses.Add("Peu de synergies entre champions");
            }
            else if (synergyScore < 5)
            {
                weaknesses.Add("Synergies d'équipe limitées");
            }
            
            // Analyse de la composition
            if (champions != null && champions.Count > 0)
            {
                // Vérification du CC
                double totalCC = champions.Sum(c => c.CC);
                if (totalCC < 15)
                {
                    weaknesses.Add("Manque de contrôle de foule");
                }
                
                // Vérification de la mobilité
                double totalMobility = champions.Sum(c => c.Mobility);
                if (totalMobility < 15)
                {
                    weaknesses.Add("Mobilité d'équipe limitée");
                }
                
                // Vérification du sustain
                double totalSustain = champions.Sum(c => c.Sustain);
                if (totalSustain < 15)
                {
                    weaknesses.Add("Sustain limité en combat");
                }
                
                // Vérification de l'équilibre des dégâts
                double totalPhysical = champions.Sum(c => c.PhysicalDamage);
                double totalMagical = champions.Sum(c => c.MagicalDamage);
                double totalDamage = totalPhysical + totalMagical + champions.Sum(c => c.TrueDamage);
                double physicalRatio = totalPhysical / totalDamage;
                double magicalRatio = totalMagical / totalDamage;
                
                if (physicalRatio > 0.7)
                {
                    weaknesses.Add("Dégâts trop physiques (facile à counter avec de l'armure)");
                }
                else if (magicalRatio > 0.7)
                {
                    weaknesses.Add("Dégâts trop magiques (facile à counter avec de la MR)");
                }
                
                // Vérification des phases de jeu
                double avgEarlyGame = champions.Average(c => c.EarlyGame);
                double avgMidGame = champions.Average(c => c.MidGame);
                double avgLateGame = champions.Average(c => c.LateGame);
                
                if (avgEarlyGame < 4)
                {
                    weaknesses.Add("Early game faible");
                }
                
                if (avgMidGame < 4)
                {
                    weaknesses.Add("Mid game faible");
                }
                
                if (avgLateGame < 4)
                {
                    weaknesses.Add("Late game faible");
                }
                
                // Vérification de l'absence de tank
                bool hasTank = champions.Any(c => c.Tankiness > 7);
                if (!hasTank)
                {
                    weaknesses.Add("Absence de tank/frontline");
                }
            }
            
            return weaknesses.ToArray();
        }

        /// <summary>
        /// Détermine les facteurs clés qui influencent le résultat du match
        /// </summary>
        private string[] DetermineKeyFactors(List<TeamMember> blueTeam, List<TeamMember> redTeam, double blueScore, double redScore)
        {
            var keyFactors = new List<string>();
            
            // Analyse des contre-picks
            double blueCounterScore = AnalyzeCounterPicks(blueTeam, redTeam);
            double redCounterScore = AnalyzeCounterPicks(redTeam, blueTeam);
            
            if (Math.Abs(blueCounterScore - redCounterScore) > 2)
            {
                if (blueCounterScore > redCounterScore)
                {
                    keyFactors.Add("L'équipe bleue a un avantage significatif en matchups individuels");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a un avantage significatif en matchups individuels");
                }
            }
            
            // Analyse des synergies
            double blueSynergyScore = AnalyzeSynergies(blueTeam);
            double redSynergyScore = AnalyzeSynergies(redTeam);
            
            if (Math.Abs(blueSynergyScore - redSynergyScore) > 2)
            {
                if (blueSynergyScore > redSynergyScore)
                {
                    keyFactors.Add("L'équipe bleue a de meilleures synergies entre champions");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a de meilleures synergies entre champions");
                }
            }
            
            // Analyse de la force méta
            double blueMetaScore = AnalyzeMetaStrength(blueTeam);
            double redMetaScore = AnalyzeMetaStrength(redTeam);
            
            if (Math.Abs(blueMetaScore - redMetaScore) > 2)
            {
                if (blueMetaScore > redMetaScore)
                {
                    keyFactors.Add("L'équipe bleue a des champions plus forts dans la méta actuelle");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a des champions plus forts dans la méta actuelle");
                }
            }
            
            // Analyse des phases de jeu
            var blueChampions = GetChampionsData(blueTeam);
            var redChampions = GetChampionsData(redTeam);
            
            double blueEarlyGame = blueChampions.Average(c => c.EarlyGame);
            double blueMidGame = blueChampions.Average(c => c.MidGame);
            double blueLateGame = blueChampions.Average(c => c.LateGame);
            
            double redEarlyGame = redChampions.Average(c => c.EarlyGame);
            double redMidGame = redChampions.Average(c => c.MidGame);
            double redLateGame = redChampions.Average(c => c.LateGame);
            
            if (Math.Abs(blueEarlyGame - redEarlyGame) > 1.5)
            {
                if (blueEarlyGame > redEarlyGame)
                {
                    keyFactors.Add("L'équipe bleue a un early game plus fort");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a un early game plus fort");
                }
            }
            
            if (Math.Abs(blueMidGame - redMidGame) > 1.5)
            {
                if (blueMidGame > redMidGame)
                {
                    keyFactors.Add("L'équipe bleue a un mid game plus fort");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a un mid game plus fort");
                }
            }
            
            if (Math.Abs(blueLateGame - redLateGame) > 1.5)
            {
                if (blueLateGame > redLateGame)
                {
                    keyFactors.Add("L'équipe bleue a un late game plus fort");
                }
                else
                {
                    keyFactors.Add("L'équipe rouge a un late game plus fort");
                }
            }
            
            // Facteur global
            if (Math.Abs(blueScore - redScore) < 1)
            {
                keyFactors.Add("Match très équilibré, les petites erreurs seront décisives");
            }
            else if (blueScore > redScore)
            {
                keyFactors.Add("L'équipe bleue a un avantage global en composition");
            }
            else
            {
                keyFactors.Add("L'équipe rouge a un avantage global en composition");
            }
            
            return keyFactors.ToArray();
        }

        /// <summary>
        /// Détermine l'équipe favorite en early game
        /// </summary>
        private string DetermineEarlyGameFavorite(List<Champion> blueChampions, List<Champion> redChampions)
        {
            double blueEarlyGame = blueChampions.Average(c => c.EarlyGame);
            double redEarlyGame = redChampions.Average(c => c.EarlyGame);
            
            if (Math.Abs(blueEarlyGame - redEarlyGame) < 0.5)
                return "Équilibré";
            
            return blueEarlyGame > redEarlyGame ? "Équipe Bleue" : "Équipe Rouge";
        }

        /// <summary>
        /// Détermine l'équipe favorite en mid game
        /// </summary>
        private string DetermineMidGameFavorite(List<Champion> blueChampions, List<Champion> redChampions)
        {
            double blueMidGame = blueChampions.Average(c => c.MidGame);
            double redMidGame = redChampions.Average(c => c.MidGame);
            
            if (Math.Abs(blueMidGame - redMidGame) < 0.5)
                return "Équilibré";
            
            return blueMidGame > redMidGame ? "Équipe Bleue" : "Équipe Rouge";
        }

        /// <summary>
        /// Détermine l'équipe favorite en late game
        /// </summary>
        private string DetermineLateGameFavorite(List<Champion> blueChampions, List<Champion> redChampions)
        {
            double blueLateGame = blueChampions.Average(c => c.LateGame);
            double redLateGame = redChampions.Average(c => c.LateGame);
            
            if (Math.Abs(blueLateGame - redLateGame) < 0.5)
                return "Équilibré";
            
            return blueLateGame > redLateGame ? "Équipe Bleue" : "Équipe Rouge";
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
