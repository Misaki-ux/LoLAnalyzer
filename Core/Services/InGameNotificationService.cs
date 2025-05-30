using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service de notifications intelligentes en temps réel pendant les parties
    /// </summary>
    public class InGameNotificationService
    {
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private readonly Data.EnhancedDatabaseManager _dbManager;
        private readonly CSTrackingService _csTrackingService;
        
        // Timer pour les vérifications périodiques
        private Timer _notificationTimer;
        
        // État du jeu
        private GameState _currentGameState;
        
        // Historique des notifications pour éviter les doublons
        private Dictionary<NotificationType, DateTime> _lastNotificationTimes;
        
        // Délai minimum entre les notifications du même type (en secondes)
        private readonly Dictionary<NotificationType, int> _notificationCooldowns = new Dictionary<NotificationType, int>
        {
            { NotificationType.BackSuggestion, 60 },
            { NotificationType.WaveClear, 30 },
            { NotificationType.ObjectiveSpawn, 30 },
            { NotificationType.FightWarning, 20 },
            { NotificationType.CSReminder, 60 },
            { NotificationType.VisionReminder, 90 },
            { NotificationType.RotationSuggestion, 45 }
        };
        
        // Événements
        public event EventHandler<NotificationEventArgs> NotificationTriggered;

        /// <summary>
        /// Constructeur du service de notifications
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        /// <param name="dbManager">Gestionnaire de base de données</param>
        /// <param name="csTrackingService">Service de suivi CS</param>
        public InGameNotificationService(Logger logger, RiotApiService riotApiService, 
            Data.EnhancedDatabaseManager dbManager, CSTrackingService csTrackingService)
        {
            _logger = logger;
            _riotApiService = riotApiService;
            _dbManager = dbManager;
            _csTrackingService = csTrackingService;
            
            _lastNotificationTimes = new Dictionary<NotificationType, DateTime>();
            foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
            {
                _lastNotificationTimes[type] = DateTime.MinValue;
            }
            
            _currentGameState = new GameState();
            
            // Initialiser le timer (vérification toutes les 5 secondes)
            _notificationTimer = new Timer(5000);
            _notificationTimer.Elapsed += OnNotificationTimerElapsed;
        }

        /// <summary>
        /// Initialise le service de notifications pour une nouvelle partie
        /// </summary>
        /// <param name="summonerName">Nom d'invocateur du joueur</param>
        /// <returns>Tâche asynchrone</returns>
        public async Task InitializeAsync(string summonerName)
        {
            try
            {
                _logger.Info($"Initialisation du service de notifications pour {summonerName}");
                
                // Réinitialiser l'état du jeu
                _currentGameState = new GameState
                {
                    SummonerName = summonerName,
                    GameTime = 0,
                    PlayerGold = 0,
                    PlayerHealth = 100,
                    PlayerMana = 100,
                    PlayerPosition = new Position { X = 0, Y = 0 },
                    PlayerRole = "",
                    PlayerChampionId = 0,
                    TeamMembers = new List<TeamMember>(),
                    EnemyTeamMembers = new List<TeamMember>(),
                    Objectives = new Dictionary<ObjectiveType, ObjectiveState>(),
                    Waves = new Dictionary<LaneType, WaveState>()
                };
                
                // Initialiser les objectifs
                _currentGameState.Objectives[ObjectiveType.Dragon] = new ObjectiveState { NextSpawnTime = 5 * 60 }; // 5 minutes
                _currentGameState.Objectives[ObjectiveType.Herald] = new ObjectiveState { NextSpawnTime = 8 * 60 }; // 8 minutes
                _currentGameState.Objectives[ObjectiveType.Baron] = new ObjectiveState { NextSpawnTime = 20 * 60 }; // 20 minutes
                
                // Initialiser les vagues
                _currentGameState.Waves[LaneType.Top] = new WaveState { Position = WavePosition.Middle };
                _currentGameState.Waves[LaneType.Mid] = new WaveState { Position = WavePosition.Middle };
                _currentGameState.Waves[LaneType.Bot] = new WaveState { Position = WavePosition.Middle };
                
                // Réinitialiser les temps de notification
                foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
                {
                    _lastNotificationTimes[type] = DateTime.MinValue;
                }
                
                // Démarrer le timer
                _notificationTimer.Start();
                
                _logger.Info("Service de notifications initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation du service de notifications", ex);
                throw;
            }
        }

        /// <summary>
        /// Met à jour l'état du jeu avec les informations en temps réel
        /// </summary>
        /// <param name="gameState">Nouvel état du jeu</param>
        public void UpdateGameState(GameState gameState)
        {
            try
            {
                _currentGameState = gameState;
                _logger.Info($"État du jeu mis à jour: {gameState.GameTime} secondes");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de la mise à jour de l'état du jeu", ex);
            }
        }

        /// <summary>
        /// Arrête le service de notifications
        /// </summary>
        public void Stop()
        {
            _notificationTimer.Stop();
            _logger.Info("Service de notifications arrêté");
        }

        /// <summary>
        /// Gestionnaire d'événement pour le timer de notifications
        /// </summary>
        private void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Vérifier les différentes conditions pour les notifications
                CheckBackSuggestion();
                CheckWaveClearSuggestion();
                CheckObjectiveSpawns();
                CheckFightWarnings();
                CheckCSReminder();
                CheckVisionReminder();
                CheckRotationSuggestion();
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de la vérification des notifications", ex);
            }
        }

        /// <summary>
        /// Vérifie si le joueur devrait retourner en base
        /// </summary>
        private void CheckBackSuggestion()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.BackSuggestion))
                return;
                
            // Conditions pour suggérer un retour en base:
            // 1. Beaucoup d'or non dépensé
            // 2. Santé basse
            // 3. Mana bas
            // 4. Moment opportun (après avoir tué un ennemi, poussé une vague, etc.)
            
            bool shouldBack = false;
            string reason = "";
            
            // Vérifier l'or
            if (_currentGameState.PlayerGold >= 1500)
            {
                shouldBack = true;
                reason = "Vous avez beaucoup d'or non dépensé (1500+). Retournez en base pour créer un avantage d'objets.";
            }
            
            // Vérifier la santé
            else if (_currentGameState.PlayerHealth <= 30)
            {
                shouldBack = true;
                reason = "Votre santé est basse. Retournez en base pour éviter de mourir.";
            }
            
            // Vérifier le mana
            else if (_currentGameState.PlayerMana <= 20)
            {
                shouldBack = true;
                reason = "Votre mana est bas. Retournez en base pour restaurer vos ressources.";
            }
            
            // Vérifier le moment opportun
            else if (_currentGameState.RecentEvents.Contains(GameEvent.EnemyKilled) && 
                     !IsLaneUnderPressure(_currentGameState.PlayerRole))
            {
                shouldBack = true;
                reason = "Vous venez de tuer un ennemi et votre lane n'est pas sous pression. C'est un bon moment pour retourner en base.";
            }
            
            if (shouldBack)
            {
                TriggerNotification(NotificationType.BackSuggestion, reason);
            }
        }

        /// <summary>
        /// Vérifie si le joueur devrait nettoyer une vague de minions
        /// </summary>
        private void CheckWaveClearSuggestion()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.WaveClear))
                return;
                
            // Déterminer la lane du joueur
            LaneType playerLane = GetPlayerLane();
            
            // Vérifier si une vague importante est proche
            foreach (var lane in _currentGameState.Waves.Keys)
            {
                WaveState waveState = _currentGameState.Waves[lane];
                
                // Vérifier si la vague est importante (grosse vague, super-minions, etc.)
                if (waveState.IsImportant && IsPlayerCloseToLane(lane) && 
                    (lane == playerLane || _currentGameState.GameTime >= 15 * 60)) // Après 15 minutes, suggérer de nettoyer d'autres lanes
                {
                    string reason = $"Une vague importante est en train de pousser en {GetLaneName(lane)}. Nettoyez-la pour ne pas perdre de CS et d'expérience.";
                    TriggerNotification(NotificationType.WaveClear, reason);
                    return;
                }
            }
        }

        /// <summary>
        /// Vérifie si un objectif va bientôt apparaître
        /// </summary>
        private void CheckObjectiveSpawns()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.ObjectiveSpawn))
                return;
                
            // Vérifier chaque objectif
            foreach (var objective in _currentGameState.Objectives.Keys)
            {
                ObjectiveState state = _currentGameState.Objectives[objective];
                
                // Vérifier si l'objectif va apparaître dans les 30 secondes
                if (state.NextSpawnTime > 0 && 
                    state.NextSpawnTime - _currentGameState.GameTime <= 30 && 
                    state.NextSpawnTime - _currentGameState.GameTime > 0)
                {
                    int timeUntilSpawn = (int)(state.NextSpawnTime - _currentGameState.GameTime);
                    string reason = $"{GetObjectiveName(objective)} va apparaître dans {timeUntilSpawn} secondes. Préparez-vous à contester.";
                    TriggerNotification(NotificationType.ObjectiveSpawn, reason);
                    return;
                }
            }
        }

        /// <summary>
        /// Vérifie si un combat défavorable est imminent
        /// </summary>
        private void CheckFightWarnings()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.FightWarning))
                return;
                
            // Vérifier les conditions de combat défavorable:
            // 1. Désavantage numérique
            // 2. Désavantage d'objets
            // 3. Désavantage de niveau
            // 4. Position vulnérable
            
            // Exemple: Vérifier le désavantage numérique
            int alliesNearby = CountAlliesNearPlayer();
            int enemiesNearby = CountEnemiesNearPlayer();
            
            if (enemiesNearby > alliesNearby + 1)
            {
                string reason = $"Combat défavorable: {enemiesNearby} ennemis contre {alliesNearby} alliés. Reculez et évitez l'engagement.";
                TriggerNotification(NotificationType.FightWarning, reason);
                return;
            }
            
            // Exemple: Vérifier la position vulnérable
            if (IsPlayerInDangerousPosition() && enemiesNearby >= 1)
            {
                string reason = "Votre position est vulnérable et des ennemis sont à proximité. Reculez vers une zone plus sûre.";
                TriggerNotification(NotificationType.FightWarning, reason);
                return;
            }
        }

        /// <summary>
        /// Vérifie si le joueur a besoin d'un rappel sur le CS
        /// </summary>
        private void CheckCSReminder()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.CSReminder))
                return;
                
            // Utiliser le service de tracking CS pour vérifier les performances
            if (_csTrackingService.IsBelowTargetThreshold())
            {
                double currentCSPerMin = _csTrackingService.GetCurrentCSPerMinute();
                double targetCSPerMin = _csTrackingService.GetCSPerMinuteDifference() + currentCSPerMin;
                
                string reason = $"Votre CS ({currentCSPerMin:F1}/min) est en dessous de l'objectif ({targetCSPerMin:F1}/min). Concentrez-vous sur le last hit des minions.";
                TriggerNotification(NotificationType.CSReminder, reason);
            }
        }

        /// <summary>
        /// Vérifie si le joueur a besoin d'un rappel sur la vision
        /// </summary>
        private void CheckVisionReminder()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.VisionReminder))
                return;
                
            // Vérifier si le joueur a des wards disponibles et si la vision est faible
            if (_currentGameState.AvailableWards > 0 && _currentGameState.TeamVisionScore < _currentGameState.GameTime / 60)
            {
                string reason = "Vous avez des wards disponibles et la vision de votre équipe est faible. Placez des wards dans des endroits stratégiques.";
                TriggerNotification(NotificationType.VisionReminder, reason);
            }
        }

        /// <summary>
        /// Vérifie si le joueur devrait faire une rotation
        /// </summary>
        private void CheckRotationSuggestion()
        {
            // Vérifier si le cooldown est écoulé
            if (!IsNotificationReady(NotificationType.RotationSuggestion))
                return;
                
            // Vérifier les conditions pour suggérer une rotation:
            // 1. Objectif important bientôt disponible
            // 2. Allié en difficulté
            // 3. Opportunité de gank
            
            // Exemple: Vérifier si un allié est en difficulté
            foreach (var member in _currentGameState.TeamMembers)
            {
                if (member.IsInDanger && IsPlayerCloseToLane(GetLaneFromRole(member.Role)))
                {
                    string reason = $"Votre allié {member.Champion.Name} en {member.Role} est en difficulté. Envisagez de vous déplacer pour l'aider.";
                    TriggerNotification(NotificationType.RotationSuggestion, reason);
                    return;
                }
            }
        }

        /// <summary>
        /// Déclenche une notification
        /// </summary>
        /// <param name="type">Type de notification</param>
        /// <param name="message">Message de la notification</param>
        private void TriggerNotification(NotificationType type, string message)
        {
            // Mettre à jour le temps de la dernière notification
            _lastNotificationTimes[type] = DateTime.Now;
            
            // Créer l'événement de notification
            var notification = new NotificationEventArgs
            {
                Type = type,
                Message = message,
                Timestamp = DateTime.Now,
                GameTime = _currentGameState.GameTime,
                Priority = GetNotificationPriority(type)
            };
            
            // Déclencher l'événement
            _logger.Info($"Notification déclenchée: {type} - {message}");
            NotificationTriggered?.Invoke(this, notification);
        }

        /// <summary>
        /// Vérifie si une notification est prête à être déclenchée (cooldown écoulé)
        /// </summary>
        /// <param name="type">Type de notification</param>
        /// <returns>True si la notification est prête</returns>
        private bool IsNotificationReady(NotificationType type)
        {
            DateTime lastTime = _lastNotificationTimes[type];
            int cooldown = _notificationCooldowns[type];
            
            return (DateTime.Now - lastTime).TotalSeconds >= cooldown;
        }

        /// <summary>
        /// Obtient la priorité d'une notification
        /// </summary>
        /// <param name="type">Type de notification</param>
        /// <returns>Priorité de la notification</returns>
        private NotificationPriority GetNotificationPriority(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.FightWarning:
                    return NotificationPriority.High;
                case NotificationType.ObjectiveSpawn:
                case NotificationType.BackSuggestion:
                    return NotificationPriority.Medium;
                default:
                    return NotificationPriority.Low;
            }
        }

        /// <summary>
        /// Vérifie si une lane est sous pression
        /// </summary>
        /// <param name="role">Rôle du joueur</param>
        /// <returns>True si la lane est sous pression</returns>
        private bool IsLaneUnderPressure(string role)
        {
            LaneType lane = GetLaneFromRole(role);
            
            if (!_currentGameState.Waves.ContainsKey(lane))
                return false;
                
            WaveState waveState = _currentGameState.Waves[lane];
            
            // La lane est sous pression si la vague est proche de notre tour
            return waveState.Position == WavePosition.NearAllyTower;
        }

        /// <summary>
        /// Obtient la lane du joueur
        /// </summary>
        /// <returns>Lane du joueur</returns>
        private LaneType GetPlayerLane()
        {
            return GetLaneFromRole(_currentGameState.PlayerRole);
        }

        /// <summary>
        /// Convertit un rôle en lane
        /// </summary>
        /// <param name="role">Rôle</param>
        /// <returns>Lane correspondante</returns>
        private LaneType GetLaneFromRole(string role)
        {
            switch (role)
            {
                case "TOP":
                    return LaneType.Top;
                case "JUNGLE":
                    return LaneType.Jungle;
                case "MID":
                    return LaneType.Mid;
                case "ADC":
                case "SUPPORT":
                    return LaneType.Bot;
                default:
                    return LaneType.Mid;
            }
        }

        /// <summary>
        /// Vérifie si le joueur est proche d'une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>True si le joueur est proche de la lane</returns>
        private bool IsPlayerCloseToLane(LaneType lane)
        {
            // Dans une version complète, cette méthode utiliserait les coordonnées du joueur
            // Pour cette version, nous supposons que le joueur est proche de sa lane
            return lane == GetPlayerLane() || _currentGameState.GameTime >= 15 * 60;
        }

        /// <summary>
        /// Obtient le nom d'une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>Nom de la lane</returns>
        private string GetLaneName(LaneType lane)
        {
            switch (lane)
            {
                case LaneType.Top:
                    return "Top";
                case LaneType.Jungle:
                    return "Jungle";
                case LaneType.Mid:
                    return "Mid";
                case LaneType.Bot:
                    return "Bot";
                default:
                    return "Inconnu";
            }
        }

        /// <summary>
        /// Obtient le nom d'un objectif
        /// </summary>
        /// <param name="objective">Objectif</param>
        /// <returns>Nom de l'objectif</returns>
        private string GetObjectiveName(ObjectiveType objective)
        {
            switch (objective)
            {
                case ObjectiveType.Dragon:
                    return "Dragon";
                case ObjectiveType.Herald:
                    return "Héraut";
                case ObjectiveType.Baron:
                    return "Baron";
                default:
                    return "Inconnu";
            }
        }

        /// <summary>
        /// Compte le nombre d'alliés proches du joueur
        /// </summary>
        /// <returns>Nombre d'alliés proches</returns>
        private int CountAlliesNearPlayer()
        {
            // Dans une version complète, cette méthode utiliserait les coordonnées des joueurs
            // Pour cette version, nous utilisons une valeur simulée
            return _currentGameState.NearbyAllies;
        }

        /// <summary>
        /// Compte le nombre d'ennemis proches du joueur
        /// </summary>
        /// <returns>Nombre d'ennemis proches</returns>
        private int CountEnemiesNearPlayer()
        {
            // Dans une version complète, cette méthode utiliserait les coordonnées des joueurs
            // Pour cette version, nous utilisons une valeur simulée
            return _currentGameState.NearbyEnemies;
        }

        /// <summary>
        /// Vérifie si le joueur est dans une position dangereuse
        /// </summary>
        /// <returns>True si le joueur est dans une position dangereuse</returns>
        private bool IsPlayerInDangerousPosition()
        {
            // Dans une version complète, cette méthode analyserait la position du joueur
            // Pour cette version, nous utilisons une valeur simulée
            return _currentGameState.IsInDangerousPosition;
        }
    }

    /// <summary>
    /// Types de notifications
    /// </summary>
    public enum NotificationType
    {
        BackSuggestion,
        WaveClear,
        ObjectiveSpawn,
        FightWarning,
        CSReminder,
        VisionReminder,
        RotationSuggestion
    }

    /// <summary>
    /// Priorités des notifications
    /// </summary>
    public enum NotificationPriority
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Arguments d'événement pour les notifications
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Type de notification
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Message de la notification
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Horodatage de la notification
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Temps de jeu au moment de la notification
        /// </summary>
        public double GameTime { get; set; }
        
        /// <summary>
        /// Priorité de la notification
        /// </summary>
        public NotificationPriority Priority { get; set; }
    }

    /// <summary>
    /// État du jeu
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Nom d'invocateur du joueur
        /// </summary>
        public string SummonerName { get; set; }
        
        /// <summary>
        /// Temps de jeu en secondes
        /// </summary>
        public double GameTime { get; set; }
        
        /// <summary>
        /// Or du joueur
        /// </summary>
        public int PlayerGold { get; set; }
        
        /// <summary>
        /// Santé du joueur (pourcentage)
        /// </summary>
        public int PlayerHealth { get; set; }
        
        /// <summary>
        /// Mana du joueur (pourcentage)
        /// </summary>
        public int PlayerMana { get; set; }
        
        /// <summary>
        /// Position du joueur
        /// </summary>
        public Position PlayerPosition { get; set; }
        
        /// <summary>
        /// Rôle du joueur
        /// </summary>
        public string PlayerRole { get; set; }
        
        /// <summary>
        /// ID du champion du joueur
        /// </summary>
        public int PlayerChampionId { get; set; }
        
        /// <summary>
        /// Membres de l'équipe du joueur
        /// </summary>
        public List<TeamMember> TeamMembers { get; set; }
        
        /// <summary>
        /// Membres de l'équipe ennemie
        /// </summary>
        public List<TeamMember> EnemyTeamMembers { get; set; }
        
        /// <summary>
        /// État des objectifs
        /// </summary>
        public Dictionary<ObjectiveType, ObjectiveState> Objectives { get; set; }
        
        /// <summary>
        /// État des vagues de minions
        /// </summary>
        public Dictionary<LaneType, WaveState> Waves { get; set; }
        
        /// <summary>
        /// Événements récents
        /// </summary>
        public List<GameEvent> RecentEvents { get; set; } = new List<GameEvent>();
        
        /// <summary>
        /// Nombre d'alliés à proximité
        /// </summary>
        public int NearbyAllies { get; set; }
        
        /// <summary>
        /// Nombre d'ennemis à proximité
        /// </summary>
        public int NearbyEnemies { get; set; }
        
        /// <summary>
        /// Indique si le joueur est dans une position dangereuse
        /// </summary>
        public bool IsInDangerousPosition { get; set; }
        
        /// <summary>
        /// Nombre de wards disponibles
        /// </summary>
        public int AvailableWards { get; set; }
        
        /// <summary>
        /// Score de vision de l'équipe
        /// </summary>
        public int TeamVisionScore { get; set; }
    }

    /// <summary>
    /// Position sur la carte
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Coordonnée X
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Coordonnée Y
        /// </summary>
        public float Y { get; set; }
    }

    /// <summary>
    /// État d'un objectif
    /// </summary>
    public class ObjectiveState
    {
        /// <summary>
        /// Temps avant le prochain spawn (en secondes)
        /// </summary>
        public double NextSpawnTime { get; set; }
        
        /// <summary>
        /// Indique si l'objectif est contesté
        /// </summary>
        public bool IsContested { get; set; }
    }

    /// <summary>
    /// État d'une vague de minions
    /// </summary>
    public class WaveState
    {
        /// <summary>
        /// Position de la vague
        /// </summary>
        public WavePosition Position { get; set; }
        
        /// <summary>
        /// Indique si la vague est importante (grosse vague, super-minions, etc.)
        /// </summary>
        public bool IsImportant { get; set; }
    }

    /// <summary>
    /// Types d'objectifs
    /// </summary>
    public enum ObjectiveType
    {
        Dragon,
        Herald,
        Baron
    }

    /// <summary>
    /// Types de lanes
    /// </summary>
    public enum LaneType
    {
        Top,
        Jungle,
        Mid,
        Bot
    }

    /// <summary>
    /// Positions des vagues de minions
    /// </summary>
    public enum WavePosition
    {
        NearAllyTower,
        Middle,
        NearEnemyTower
    }

    /// <summary>
    /// Événements de jeu
    /// </summary>
    public enum GameEvent
    {
        EnemyKilled,
        AllyKilled,
        TowerDestroyed,
        ObjectiveTaken
    }
}
