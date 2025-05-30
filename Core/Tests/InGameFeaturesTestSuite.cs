using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;
using LoLAnalyzer.Core.Services;

namespace LoLAnalyzer.Core.Tests
{
    /// <summary>
    /// Suite de tests avancés pour valider les fonctionnalités in-game
    /// </summary>
    public class InGameFeaturesTestSuite
    {
        private readonly Logger _logger;
        private readonly CSTrackingService _csTrackingService;
        private readonly InGameNotificationService _notificationService;
        private readonly MapAnalysisService _mapAnalysisService;
        private readonly EnhancedRuneRecommendationService _runeRecommendationService;
        private readonly EnhancedTrollDetectorService _trollDetectorService;
        
        // Compteurs de tests
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;
        
        // Liste des notifications reçues pendant les tests
        private List<NotificationEventArgs> _receivedNotifications;

        /// <summary>
        /// Constructeur de la suite de tests
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="csTrackingService">Service de suivi CS</param>
        /// <param name="notificationService">Service de notifications</param>
        /// <param name="mapAnalysisService">Service d'analyse de la carte</param>
        /// <param name="runeRecommendationService">Service de recommandation de runes</param>
        /// <param name="trollDetectorService">Service de détection des picks problématiques</param>
        public InGameFeaturesTestSuite(
            Logger logger,
            CSTrackingService csTrackingService,
            InGameNotificationService notificationService,
            MapAnalysisService mapAnalysisService,
            EnhancedRuneRecommendationService runeRecommendationService,
            EnhancedTrollDetectorService trollDetectorService)
        {
            _logger = logger;
            _csTrackingService = csTrackingService;
            _notificationService = notificationService;
            _mapAnalysisService = mapAnalysisService;
            _runeRecommendationService = runeRecommendationService;
            _trollDetectorService = trollDetectorService;
            
            _receivedNotifications = new List<NotificationEventArgs>();
            
            // S'abonner à l'événement de notification
            _notificationService.NotificationTriggered += OnNotificationTriggered;
        }

        /// <summary>
        /// Exécute tous les tests
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        public async Task RunAllTestsAsync()
        {
            try
            {
                _logger.Info("Démarrage de la suite de tests des fonctionnalités in-game");
                
                // Réinitialiser les compteurs
                _totalTests = 0;
                _passedTests = 0;
                _failedTests = 0;
                _receivedNotifications.Clear();
                
                // Tester le service de suivi CS
                await TestCSTrackingServiceAsync();
                
                // Tester le service de notifications
                await TestNotificationServiceAsync();
                
                // Tester le service d'analyse de la carte
                await TestMapAnalysisServiceAsync();
                
                // Tester le service de recommandation de runes
                await TestRuneRecommendationServiceAsync();
                
                // Tester le service de détection des picks problématiques
                await TestTrollDetectorServiceAsync();
                
                // Tester l'intégration des services
                await TestServicesIntegrationAsync();
                
                // Afficher le résumé des tests
                _logger.Info($"Tests terminés: {_totalTests} tests exécutés, {_passedTests} réussis, {_failedTests} échoués");
                
                if (_failedTests == 0)
                {
                    _logger.Info("Tous les tests ont réussi!");
                }
                else
                {
                    _logger.Warning($"{_failedTests} tests ont échoué!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'exécution des tests", ex);
                throw;
            }
        }

        /// <summary>
        /// Teste le service de suivi CS
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestCSTrackingServiceAsync()
        {
            _logger.Info("Test du service de suivi CS");
            
            try
            {
                // Test 1: Initialisation du service
                _totalTests++;
                await _csTrackingService.InitializeTrackingAsync("TestSummoner", "GOLD");
                _logger.Info("Test 1: Initialisation du service - Réussi");
                _passedTests++;
                
                // Test 2: Mise à jour des données CS
                _totalTests++;
                _csTrackingService.UpdateCSData(50, 10); // 50 CS à 10 minutes
                double csPerMin = _csTrackingService.GetCurrentCSPerMinute();
                Assert(Math.Abs(csPerMin - 5.0) < 0.1, "Le taux de CS par minute devrait être de 5.0");
                _logger.Info("Test 2: Mise à jour des données CS - Réussi");
                _passedTests++;
                
                // Test 3: Vérification du seuil cible
                _totalTests++;
                bool belowThreshold = _csTrackingService.IsBelowTargetThreshold();
                Assert(belowThreshold, "Le taux de CS devrait être en dessous du seuil pour GOLD (6.5 CS/min)");
                _logger.Info("Test 3: Vérification du seuil cible - Réussi");
                _passedTests++;
                
                // Test 4: Obtention des conseils d'amélioration
                _totalTests++;
                List<string> tips = _csTrackingService.GetCSImprovementTips();
                Assert(tips.Count > 0, "Des conseils d'amélioration devraient être fournis");
                _logger.Info("Test 4: Obtention des conseils d'amélioration - Réussi");
                _passedTests++;
                
                // Test 5: Changement de rang cible
                _totalTests++;
                bool rankChanged = _csTrackingService.SetTargetRank("PLATINUM");
                Assert(rankChanged, "Le rang cible devrait être changé avec succès");
                _logger.Info("Test 5: Changement de rang cible - Réussi");
                _passedTests++;
                
                // Test 6: Mise à jour des données CS avec un meilleur taux
                _totalTests++;
                _csTrackingService.UpdateCSData(150, 20); // 150 CS à 20 minutes
                csPerMin = _csTrackingService.GetCurrentCSPerMinute();
                Assert(Math.Abs(csPerMin - 7.5) < 0.1, "Le taux de CS par minute devrait être de 7.5");
                _logger.Info("Test 6: Mise à jour des données CS avec un meilleur taux - Réussi");
                _passedTests++;
                
                // Test 7: Vérification du seuil cible après amélioration
                _totalTests++;
                belowThreshold = _csTrackingService.IsBelowTargetThreshold();
                Assert(!belowThreshold, "Le taux de CS devrait être au-dessus du seuil pour PLATINUM (7.0 CS/min)");
                _logger.Info("Test 7: Vérification du seuil cible après amélioration - Réussi");
                _passedTests++;
                
                _logger.Info("Tests du service de suivi CS terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests du service de suivi CS", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Teste le service de notifications
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestNotificationServiceAsync()
        {
            _logger.Info("Test du service de notifications");
            
            try
            {
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 1: Initialisation du service
                _totalTests++;
                await _notificationService.InitializeAsync("TestSummoner");
                _logger.Info("Test 1: Initialisation du service - Réussi");
                _passedTests++;
                
                // Test 2: Notification de retour en base
                _totalTests++;
                GameState backState = CreateGameState();
                backState.PlayerGold = 2000; // Beaucoup d'or non dépensé
                _notificationService.UpdateGameState(backState);
                await Task.Delay(100); // Attendre que la notification soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.BackSuggestion), 
                       "Une notification de retour en base devrait être déclenchée");
                _logger.Info("Test 2: Notification de retour en base - Réussi");
                _passedTests++;
                
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 3: Notification d'objectif
                _totalTests++;
                GameState objectiveState = CreateGameState();
                objectiveState.Objectives[ObjectiveType.Dragon].NextSpawnTime = objectiveState.GameTime + 20; // Dragon dans 20 secondes
                _notificationService.UpdateGameState(objectiveState);
                await Task.Delay(100); // Attendre que la notification soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.ObjectiveSpawn), 
                       "Une notification d'objectif devrait être déclenchée");
                _logger.Info("Test 3: Notification d'objectif - Réussi");
                _passedTests++;
                
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 4: Notification de combat défavorable
                _totalTests++;
                GameState fightState = CreateGameState();
                fightState.NearbyAllies = 1;
                fightState.NearbyEnemies = 3; // Désavantage numérique
                _notificationService.UpdateGameState(fightState);
                await Task.Delay(100); // Attendre que la notification soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.FightWarning), 
                       "Une notification de combat défavorable devrait être déclenchée");
                _logger.Info("Test 4: Notification de combat défavorable - Réussi");
                _passedTests++;
                
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 5: Notification de vague de minions
                _totalTests++;
                GameState waveState = CreateGameState();
                waveState.Waves[LaneType.Bot].IsImportant = true; // Vague importante en bot
                _notificationService.UpdateGameState(waveState);
                await Task.Delay(100); // Attendre que la notification soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.WaveClear), 
                       "Une notification de vague de minions devrait être déclenchée");
                _logger.Info("Test 5: Notification de vague de minions - Réussi");
                _passedTests++;
                
                // Arrêter le service
                _notificationService.Stop();
                
                _logger.Info("Tests du service de notifications terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests du service de notifications", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Teste le service d'analyse de la carte
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestMapAnalysisServiceAsync()
        {
            _logger.Info("Test du service d'analyse de la carte");
            
            try
            {
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 1: Initialisation du service
                _totalTests++;
                GameState initialState = CreateGameState();
                await _mapAnalysisService.InitializeAsync(initialState);
                _logger.Info("Test 1: Initialisation du service - Réussi");
                _passedTests++;
                
                // Test 2: Mise à jour de l'état de la carte
                _totalTests++;
                GameState updatedState = CreateGameState();
                updatedState.GameTime = 300; // 5 minutes
                updatedState.PlayerPosition = new Position { X = 9000, Y = 4000 }; // Près du dragon
                _mapAnalysisService.UpdateMapState(updatedState);
                await Task.Delay(100); // Attendre que l'analyse soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.VisionReminder || n.Type == NotificationType.ObjectiveSpawn), 
                       "Une notification de vision ou d'objectif devrait être déclenchée");
                _logger.Info("Test 2: Mise à jour de l'état de la carte - Réussi");
                _passedTests++;
                
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 3: Détection de zone dangereuse
                _totalTests++;
                GameState dangerState = CreateGameState();
                dangerState.IsInDangerousPosition = true;
                dangerState.NearbyEnemies = 2;
                _mapAnalysisService.UpdateMapState(dangerState);
                await Task.Delay(100); // Attendre que l'analyse soit traitée
                Assert(_receivedNotifications.Any(n => n.Type == NotificationType.FightWarning), 
                       "Une notification de danger devrait être déclenchée");
                _logger.Info("Test 3: Détection de zone dangereuse - Réussi");
                _passedTests++;
                
                _logger.Info("Tests du service d'analyse de la carte terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests du service d'analyse de la carte", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Teste le service de recommandation de runes
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestRuneRecommendationServiceAsync()
        {
            _logger.Info("Test du service de recommandation de runes");
            
            try
            {
                // Test 1: Initialisation du service
                _totalTests++;
                await _runeRecommendationService.InitializeAsync();
                _logger.Info("Test 1: Initialisation du service - Réussi");
                _passedTests++;
                
                // Test 2: Recommandation de runes par défaut
                _totalTests++;
                List<RuneSet> defaultRunes = await _runeRecommendationService.GetRuneRecommendationsAsync("Teemo");
                Assert(defaultRunes.Count > 0, "Des recommandations de runes par défaut devraient être fournies");
                _logger.Info("Test 2: Recommandation de runes par défaut - Réussi");
                _passedTests++;
                
                // Test 3: Recommandation de runes pour un matchup spécifique
                _totalTests++;
                List<RuneSet> matchupRunes = await _runeRecommendationService.GetRuneRecommendationsAsync("Teemo", "Darius");
                Assert(matchupRunes.Count > 0, "Des recommandations de runes pour un matchup spécifique devraient être fournies");
                Assert(matchupRunes[0].MatchupChampionName == "Darius", "La première recommandation devrait être spécifique au matchup contre Darius");
                _logger.Info("Test 3: Recommandation de runes pour un matchup spécifique - Réussi");
                _passedTests++;
                
                // Test 4: Recommandation de runes pour un style de jeu spécifique
                _totalTests++;
                List<RuneSet> styleRunes = await _runeRecommendationService.GetRuneRecommendationsAsync("Teemo", null, "AGGRESSIVE");
                Assert(styleRunes.Count > 0, "Des recommandations de runes pour un style de jeu spécifique devraient être fournies");
                Assert(styleRunes.Any(r => r.StyleId == "AGGRESSIVE"), "Une recommandation pour le style de jeu agressif devrait être fournie");
                _logger.Info("Test 4: Recommandation de runes pour un style de jeu spécifique - Réussi");
                _passedTests++;
                
                // Test 5: Obtention des styles de jeu disponibles
                _totalTests++;
                List<PlayStyle> playStyles = _runeRecommendationService.GetPlayStyles();
                Assert(playStyles.Count > 0, "Des styles de jeu devraient être disponibles");
                _logger.Info("Test 5: Obtention des styles de jeu disponibles - Réussi");
                _passedTests++;
                
                _logger.Info("Tests du service de recommandation de runes terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests du service de recommandation de runes", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Teste le service de détection des picks problématiques
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestTrollDetectorServiceAsync()
        {
            _logger.Info("Test du service de détection des picks problématiques");
            
            try
            {
                // Test 1: Initialisation du service
                _totalTests++;
                await _trollDetectorService.InitializeAsync();
                _logger.Info("Test 1: Initialisation du service - Réussi");
                _passedTests++;
                
                // Test 2: Analyse d'un pick viable
                _totalTests++;
                TrollPickAnalysis viablePick = await _trollDetectorService.AnalyzePickAsync("Teemo", "TOP");
                Assert(!viablePick.IsTroll, "Teemo TOP devrait être un pick viable");
                _logger.Info("Test 2: Analyse d'un pick viable - Réussi");
                _passedTests++;
                
                // Test 3: Analyse d'un pick problématique
                _totalTests++;
                TrollPickAnalysis trollPick = await _trollDetectorService.AnalyzePickAsync("Teemo", "JUNGLE");
                Assert(trollPick.IsTroll, "Teemo JUNGLE devrait être un pick problématique");
                _logger.Info("Test 3: Analyse d'un pick problématique - Réussi");
                _passedTests++;
                
                // Test 4: Analyse d'un pick très problématique
                _totalTests++;
                TrollPickAnalysis veryTrollPick = await _trollDetectorService.AnalyzePickAsync("Yuumi", "ADC");
                Assert(veryTrollPick.IsTroll && veryTrollPick.TrollLevel >= 3, "Yuumi ADC devrait être un pick très problématique");
                _logger.Info("Test 4: Analyse d'un pick très problématique - Réussi");
                _passedTests++;
                
                // Test 5: Analyse d'une composition d'équipe
                _totalTests++;
                TeamComposition composition = CreateTeamComposition();
                List<TrollPickAnalysis> trollPicks = await _trollDetectorService.AnalyzeTeamCompositionAsync(composition);
                Assert(trollPicks.Count > 0, "Des picks problématiques devraient être détectés dans la composition");
                _logger.Info("Test 5: Analyse d'une composition d'équipe - Réussi");
                _passedTests++;
                
                // Test 6: Obtention des picks problématiques connus pour un rôle
                _totalTests++;
                List<TrollPickData> jungleTrollPicks = await _trollDetectorService.GetKnownTrollPicksForRoleAsync("JUNGLE");
                Assert(jungleTrollPicks.Count > 0, "Des picks problématiques connus devraient être disponibles pour le rôle JUNGLE");
                _logger.Info("Test 6: Obtention des picks problématiques connus pour un rôle - Réussi");
                _passedTests++;
                
                // Test 7: Obtention des picks les plus problématiques
                _totalTests++;
                List<TrollPickData> mostSevereTrollPicks = await _trollDetectorService.GetMostSevereTrollPicksAsync();
                Assert(mostSevereTrollPicks.Count > 0, "Des picks très problématiques devraient être disponibles");
                _logger.Info("Test 7: Obtention des picks les plus problématiques - Réussi");
                _passedTests++;
                
                _logger.Info("Tests du service de détection des picks problématiques terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests du service de détection des picks problématiques", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Teste l'intégration des services
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task TestServicesIntegrationAsync()
        {
            _logger.Info("Test de l'intégration des services");
            
            try
            {
                // Réinitialiser les notifications reçues
                _receivedNotifications.Clear();
                
                // Test 1: Initialisation de tous les services
                _totalTests++;
                GameState initialState = CreateGameState();
                await _csTrackingService.InitializeTrackingAsync("TestSummoner", "GOLD");
                await _notificationService.InitializeAsync("TestSummoner");
                await _mapAnalysisService.InitializeAsync(initialState);
                await _runeRecommendationService.InitializeAsync();
                await _trollDetectorService.InitializeAsync();
                _logger.Info("Test 1: Initialisation de tous les services - Réussi");
                _passedTests++;
                
                // Test 2: Simulation d'une partie complète
                _totalTests++;
                
                // Phase de sélection des champions
                TeamComposition composition = CreateTeamComposition();
                List<TrollPickAnalysis> trollPicks = await _trollDetectorService.AnalyzeTeamCompositionAsync(composition);
                
                // Phase de chargement
                List<RuneSet> recommendedRunes = await _runeRecommendationService.GetRuneRecommendationsAsync("Teemo", "Darius");
                
                // Early game (0-10 minutes)
                GameState earlyGameState = CreateGameState();
                earlyGameState.GameTime = 300; // 5 minutes
                _csTrackingService.UpdateCSData(25, 5); // 25 CS à 5 minutes (5 CS/min)
                _notificationService.UpdateGameState(earlyGameState);
                _mapAnalysisService.UpdateMapState(earlyGameState);
                
                // Mid game (10-20 minutes)
                GameState midGameState = CreateGameState();
                midGameState.GameTime = 900; // 15 minutes
                midGameState.PlayerGold = 1800; // Beaucoup d'or non dépensé
                _csTrackingService.UpdateCSData(90, 15); // 90 CS à 15 minutes (6 CS/min)
                _notificationService.UpdateGameState(midGameState);
                _mapAnalysisService.UpdateMapState(midGameState);
                
                // Late game (20+ minutes)
                GameState lateGameState = CreateGameState();
                lateGameState.GameTime = 1800; // 30 minutes
                lateGameState.Objectives[ObjectiveType.Baron].NextSpawnTime = 1800; // Baron disponible
                _csTrackingService.UpdateCSData(210, 30); // 210 CS à 30 minutes (7 CS/min)
                _notificationService.UpdateGameState(lateGameState);
                _mapAnalysisService.UpdateMapState(lateGameState);
                
                await Task.Delay(100); // Attendre que toutes les notifications soient traitées
                
                Assert(_receivedNotifications.Count > 0, "Des notifications devraient être déclenchées pendant la simulation");
                _logger.Info("Test 2: Simulation d'une partie complète - Réussi");
                _passedTests++;
                
                _logger.Info("Tests de l'intégration des services terminés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors des tests de l'intégration des services", ex);
                _failedTests++;
            }
        }

        /// <summary>
        /// Gestionnaire d'événement pour les notifications
        /// </summary>
        /// <param name="sender">Expéditeur de l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void OnNotificationTriggered(object sender, NotificationEventArgs e)
        {
            _receivedNotifications.Add(e);
            _logger.Info($"Notification reçue: {e.Type} - {e.Message}");
        }

        /// <summary>
        /// Crée un état de jeu pour les tests
        /// </summary>
        /// <returns>État de jeu</returns>
        private GameState CreateGameState()
        {
            return new GameState
            {
                SummonerName = "TestSummoner",
                GameTime = 0,
                PlayerGold = 500,
                PlayerHealth = 100,
                PlayerMana = 100,
                PlayerPosition = new Position { X = 0, Y = 0 },
                PlayerRole = "TOP",
                PlayerChampionId = 17, // Teemo
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 17, Name = "Teemo" }, Role = "TOP", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 64, Name = "Lee Sin" }, Role = "JUNGLE", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 157, Name = "Yasuo" }, Role = "MID", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 51, Name = "Caitlyn" }, Role = "ADC", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 412, Name = "Thresh" }, Role = "SUPPORT", Position = new Position { X = 0, Y = 0 } }
                },
                EnemyTeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 122, Name = "Darius" }, Role = "TOP", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 60, Name = "Elise" }, Role = "JUNGLE", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 238, Name = "Zed" }, Role = "MID", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 119, Name = "Draven" }, Role = "ADC", Position = new Position { X = 0, Y = 0 } },
                    new TeamMember { Champion = new Champion { Id = 89, Name = "Leona" }, Role = "SUPPORT", Position = new Position { X = 0, Y = 0 } }
                },
                Objectives = new Dictionary<ObjectiveType, ObjectiveState>
                {
                    { ObjectiveType.Dragon, new ObjectiveState { NextSpawnTime = 300 } }, // 5 minutes
                    { ObjectiveType.Herald, new ObjectiveState { NextSpawnTime = 480 } }, // 8 minutes
                    { ObjectiveType.Baron, new ObjectiveState { NextSpawnTime = 1200 } } // 20 minutes
                },
                Waves = new Dictionary<LaneType, WaveState>
                {
                    { LaneType.Top, new WaveState { Position = WavePosition.Middle, IsImportant = false } },
                    { LaneType.Mid, new WaveState { Position = WavePosition.Middle, IsImportant = false } },
                    { LaneType.Bot, new WaveState { Position = WavePosition.Middle, IsImportant = false } }
                },
                RecentEvents = new List<GameEvent>(),
                NearbyAllies = 0,
                NearbyEnemies = 0,
                IsInDangerousPosition = false,
                AvailableWards = 2,
                TeamVisionScore = 0
            };
        }

        /// <summary>
        /// Crée une composition d'équipe pour les tests
        /// </summary>
        /// <returns>Composition d'équipe</returns>
        private TeamComposition CreateTeamComposition()
        {
            return new TeamComposition
            {
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 17, Name = "Teemo" }, Role = "TOP" },
                    new TeamMember { Champion = new Champion { Id = 64, Name = "Lee Sin" }, Role = "JUNGLE" },
                    new TeamMember { Champion = new Champion { Id = 157, Name = "Yasuo" }, Role = "MID" },
                    new TeamMember { Champion = new Champion { Id = 51, Name = "Caitlyn" }, Role = "ADC" },
                    new TeamMember { Champion = new Champion { Id = 350, Name = "Yuumi" }, Role = "JUNGLE" } // Pick problématique
                },
                EnemyTeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 122, Name = "Darius" }, Role = "TOP" },
                    new TeamMember { Champion = new Champion { Id = 60, Name = "Elise" }, Role = "JUNGLE" },
                    new TeamMember { Champion = new Champion { Id = 238, Name = "Zed" }, Role = "MID" },
                    new TeamMember { Champion = new Champion { Id = 119, Name = "Draven" }, Role = "ADC" },
                    new TeamMember { Champion = new Champion { Id = 89, Name = "Leona" }, Role = "SUPPORT" }
                }
            };
        }

        /// <summary>
        /// Vérifie une assertion
        /// </summary>
        /// <param name="condition">Condition à vérifier</param>
        /// <param name="message">Message d'erreur</param>
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                _logger.Error($"Assertion échouée: {message}");
                _failedTests++;
                throw new Exception($"Assertion échouée: {message}");
            }
        }
    }
}
