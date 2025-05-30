using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service d'analyse de la carte et de génération de conseils contextuels
    /// </summary>
    public class MapAnalysisService
    {
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private readonly Data.EnhancedDatabaseManager _dbManager;
        private readonly InGameNotificationService _notificationService;
        
        // État de la carte
        private MapState _currentMapState;
        
        // Historique des positions des joueurs
        private Dictionary<string, List<PositionHistory>> _playerPositionHistory;
        
        // Zones de danger et d'intérêt
        private List<MapZone> _dangerZones;
        private List<MapZone> _interestZones;
        
        // Constantes pour l'analyse de la carte
        private const int POSITION_HISTORY_MAX_SIZE = 100;
        private const double DANGER_THRESHOLD = 0.7;
        private const double OPPORTUNITY_THRESHOLD = 0.6;

        /// <summary>
        /// Constructeur du service d'analyse de la carte
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        /// <param name="dbManager">Gestionnaire de base de données</param>
        /// <param name="notificationService">Service de notifications</param>
        public MapAnalysisService(Logger logger, RiotApiService riotApiService, 
            Data.EnhancedDatabaseManager dbManager, InGameNotificationService notificationService)
        {
            _logger = logger;
            _riotApiService = riotApiService;
            _dbManager = dbManager;
            _notificationService = notificationService;
            
            _playerPositionHistory = new Dictionary<string, List<PositionHistory>>();
            _dangerZones = new List<MapZone>();
            _interestZones = new List<MapZone>();
            _currentMapState = new MapState();
        }

        /// <summary>
        /// Initialise le service d'analyse de la carte pour une nouvelle partie
        /// </summary>
        /// <param name="gameState">État initial du jeu</param>
        /// <returns>Tâche asynchrone</returns>
        public async Task InitializeAsync(GameState gameState)
        {
            try
            {
                _logger.Info("Initialisation du service d'analyse de la carte");
                
                // Réinitialiser les données
                _playerPositionHistory.Clear();
                _dangerZones.Clear();
                _interestZones.Clear();
                
                // Initialiser l'historique des positions pour chaque joueur
                foreach (var member in gameState.TeamMembers)
                {
                    _playerPositionHistory[member.Champion.Name] = new List<PositionHistory>();
                }
                
                foreach (var member in gameState.EnemyTeamMembers)
                {
                    _playerPositionHistory[member.Champion.Name] = new List<PositionHistory>();
                }
                
                // Initialiser les zones d'intérêt (objectifs, buffs, etc.)
                InitializeInterestZones();
                
                // Initialiser l'état de la carte
                _currentMapState = new MapState
                {
                    GameTime = gameState.GameTime,
                    TeamVisionScore = gameState.TeamVisionScore,
                    WardPositions = new List<Position>(),
                    ControlWardPositions = new List<Position>(),
                    TeamMemberPositions = new Dictionary<string, Position>(),
                    EnemyMemberPositions = new Dictionary<string, Position>(),
                    JungleCreepStatus = new Dictionary<JungleCreepType, JungleCreepState>(),
                    LaneStates = new Dictionary<LaneType, LaneState>()
                };
                
                // Initialiser les états des camps de jungle
                InitializeJungleCreeps();
                
                // Initialiser les états des lanes
                foreach (LaneType lane in Enum.GetValues(typeof(LaneType)))
                {
                    if (lane != LaneType.Jungle)
                    {
                        _currentMapState.LaneStates[lane] = new LaneState
                        {
                            PushStatus = LanePushStatus.Neutral,
                            DangerLevel = 0.0,
                            OpportunityLevel = 0.0
                        };
                    }
                }
                
                _logger.Info("Service d'analyse de la carte initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation du service d'analyse de la carte", ex);
                throw;
            }
        }

        /// <summary>
        /// Met à jour l'état de la carte avec les informations en temps réel
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        public void UpdateMapState(GameState gameState)
        {
            try
            {
                _logger.Info($"Mise à jour de l'état de la carte à {gameState.GameTime} secondes");
                
                // Mettre à jour l'état de la carte
                _currentMapState.GameTime = gameState.GameTime;
                _currentMapState.TeamVisionScore = gameState.TeamVisionScore;
                
                // Mettre à jour les positions des joueurs
                UpdatePlayerPositions(gameState);
                
                // Mettre à jour les états des lanes
                UpdateLaneStates(gameState);
                
                // Mettre à jour les zones de danger
                UpdateDangerZones();
                
                // Mettre à jour les zones d'intérêt
                UpdateInterestZones(gameState);
                
                // Analyser la carte pour générer des conseils
                AnalyzeMap(gameState);
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de la mise à jour de l'état de la carte", ex);
            }
        }

        /// <summary>
        /// Initialise les zones d'intérêt sur la carte
        /// </summary>
        private void InitializeInterestZones()
        {
            // Ajouter les objectifs majeurs
            _interestZones.Add(new MapZone
            {
                Name = "Dragon Pit",
                Center = new Position { X = 9800, Y = 4400 },
                Radius = 1000,
                Type = ZoneType.MajorObjective,
                AvailableFrom = 5 * 60 // 5 minutes
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Baron Pit",
                Center = new Position { X = 5000, Y = 10300 },
                Radius = 1000,
                Type = ZoneType.MajorObjective,
                AvailableFrom = 20 * 60 // 20 minutes
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Herald Pit",
                Center = new Position { X = 5000, Y = 10300 },
                Radius = 1000,
                Type = ZoneType.MajorObjective,
                AvailableFrom = 8 * 60, // 8 minutes
                AvailableUntil = 20 * 60 // 20 minutes
            });
            
            // Ajouter les buffs
            _interestZones.Add(new MapZone
            {
                Name = "Blue Buff (Ally)",
                Center = new Position { X = 3800, Y = 8000 },
                Radius = 500,
                Type = ZoneType.Buff
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Red Buff (Ally)",
                Center = new Position { X = 7800, Y = 4000 },
                Radius = 500,
                Type = ZoneType.Buff
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Blue Buff (Enemy)",
                Center = new Position { X = 11200, Y = 7000 },
                Radius = 500,
                Type = ZoneType.Buff
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Red Buff (Enemy)",
                Center = new Position { X = 7200, Y = 11000 },
                Radius = 500,
                Type = ZoneType.Buff
            });
            
            // Ajouter les points de vision stratégiques
            _interestZones.Add(new MapZone
            {
                Name = "Dragon Vision",
                Center = new Position { X = 9000, Y = 5000 },
                Radius = 300,
                Type = ZoneType.VisionPoint
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Baron Vision",
                Center = new Position { X = 6000, Y = 10000 },
                Radius = 300,
                Type = ZoneType.VisionPoint
            });
            
            _interestZones.Add(new MapZone
            {
                Name = "Mid River Vision",
                Center = new Position { X = 7500, Y = 7500 },
                Radius = 300,
                Type = ZoneType.VisionPoint
            });
        }

        /// <summary>
        /// Initialise les camps de jungle
        /// </summary>
        private void InitializeJungleCreeps()
        {
            foreach (JungleCreepType creepType in Enum.GetValues(typeof(JungleCreepType)))
            {
                _currentMapState.JungleCreepStatus[creepType] = new JungleCreepState
                {
                    IsAlive = true,
                    RespawnTime = 0
                };
            }
        }

        /// <summary>
        /// Met à jour les positions des joueurs
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void UpdatePlayerPositions(GameState gameState)
        {
            // Mettre à jour les positions des alliés
            foreach (var member in gameState.TeamMembers)
            {
                if (member.Position != null)
                {
                    _currentMapState.TeamMemberPositions[member.Champion.Name] = member.Position;
                    
                    // Ajouter à l'historique des positions
                    if (_playerPositionHistory.ContainsKey(member.Champion.Name))
                    {
                        _playerPositionHistory[member.Champion.Name].Add(new PositionHistory
                        {
                            Position = member.Position,
                            Timestamp = DateTime.Now,
                            GameTime = gameState.GameTime
                        });
                        
                        // Limiter la taille de l'historique
                        if (_playerPositionHistory[member.Champion.Name].Count > POSITION_HISTORY_MAX_SIZE)
                        {
                            _playerPositionHistory[member.Champion.Name].RemoveAt(0);
                        }
                    }
                }
            }
            
            // Mettre à jour les positions des ennemis (si visibles)
            foreach (var member in gameState.EnemyTeamMembers)
            {
                if (member.Position != null && member.IsVisible)
                {
                    _currentMapState.EnemyMemberPositions[member.Champion.Name] = member.Position;
                    
                    // Ajouter à l'historique des positions
                    if (_playerPositionHistory.ContainsKey(member.Champion.Name))
                    {
                        _playerPositionHistory[member.Champion.Name].Add(new PositionHistory
                        {
                            Position = member.Position,
                            Timestamp = DateTime.Now,
                            GameTime = gameState.GameTime
                        });
                        
                        // Limiter la taille de l'historique
                        if (_playerPositionHistory[member.Champion.Name].Count > POSITION_HISTORY_MAX_SIZE)
                        {
                            _playerPositionHistory[member.Champion.Name].RemoveAt(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Met à jour les états des lanes
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void UpdateLaneStates(GameState gameState)
        {
            foreach (var lane in gameState.Waves.Keys)
            {
                if (!_currentMapState.LaneStates.ContainsKey(lane))
                {
                    _currentMapState.LaneStates[lane] = new LaneState();
                }
                
                // Déterminer l'état de push de la lane
                switch (gameState.Waves[lane].Position)
                {
                    case WavePosition.NearAllyTower:
                        _currentMapState.LaneStates[lane].PushStatus = LanePushStatus.EnemyPushing;
                        break;
                    case WavePosition.Middle:
                        _currentMapState.LaneStates[lane].PushStatus = LanePushStatus.Neutral;
                        break;
                    case WavePosition.NearEnemyTower:
                        _currentMapState.LaneStates[lane].PushStatus = LanePushStatus.AllyPushing;
                        break;
                }
                
                // Calculer le niveau de danger
                _currentMapState.LaneStates[lane].DangerLevel = CalculateLaneDangerLevel(lane, gameState);
                
                // Calculer le niveau d'opportunité
                _currentMapState.LaneStates[lane].OpportunityLevel = CalculateLaneOpportunityLevel(lane, gameState);
            }
        }

        /// <summary>
        /// Met à jour les zones de danger
        /// </summary>
        private void UpdateDangerZones()
        {
            _dangerZones.Clear();
            
            // Créer des zones de danger autour des ennemis visibles
            foreach (var enemyPosition in _currentMapState.EnemyMemberPositions)
            {
                _dangerZones.Add(new MapZone
                {
                    Name = $"Enemy {enemyPosition.Key}",
                    Center = enemyPosition.Value,
                    Radius = 1000, // Rayon de danger autour d'un ennemi
                    Type = ZoneType.Danger,
                    DangerLevel = 0.8
                });
            }
            
            // Créer des zones de danger basées sur les dernières positions connues des ennemis non visibles
            foreach (var enemy in _playerPositionHistory.Keys)
            {
                if (!_currentMapState.EnemyMemberPositions.ContainsKey(enemy) && 
                    _playerPositionHistory[enemy].Count > 0)
                {
                    var lastPosition = _playerPositionHistory[enemy].Last();
                    
                    // Ne considérer que les positions récentes (moins de 30 secondes)
                    if (DateTime.Now - lastPosition.Timestamp < TimeSpan.FromSeconds(30))
                    {
                        _dangerZones.Add(new MapZone
                        {
                            Name = $"Last Known {enemy}",
                            Center = lastPosition.Position,
                            Radius = 1500, // Rayon plus large pour l'incertitude
                            Type = ZoneType.PotentialDanger,
                            DangerLevel = 0.5
                        });
                    }
                }
            }
            
            // Ajouter des zones de danger pour les lanes sous pression ennemie
            foreach (var lane in _currentMapState.LaneStates.Keys)
            {
                if (_currentMapState.LaneStates[lane].DangerLevel > DANGER_THRESHOLD)
                {
                    _dangerZones.Add(new MapZone
                    {
                        Name = $"{lane} Lane Danger",
                        Center = GetLaneCenter(lane),
                        Radius = 1200,
                        Type = ZoneType.Danger,
                        DangerLevel = _currentMapState.LaneStates[lane].DangerLevel
                    });
                }
            }
        }

        /// <summary>
        /// Met à jour les zones d'intérêt
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void UpdateInterestZones(GameState gameState)
        {
            // Mettre à jour la disponibilité des zones d'intérêt en fonction du temps de jeu
            foreach (var zone in _interestZones)
            {
                zone.IsAvailable = (zone.AvailableFrom <= gameState.GameTime) && 
                                  (zone.AvailableUntil == 0 || zone.AvailableUntil > gameState.GameTime);
            }
            
            // Ajouter des zones d'intérêt pour les lanes avec opportunités
            foreach (var lane in _currentMapState.LaneStates.Keys)
            {
                if (_currentMapState.LaneStates[lane].OpportunityLevel > OPPORTUNITY_THRESHOLD)
                {
                    // Vérifier si une zone d'intérêt existe déjà pour cette lane
                    bool zoneExists = _interestZones.Any(z => z.Name == $"{lane} Lane Opportunity");
                    
                    if (!zoneExists)
                    {
                        _interestZones.Add(new MapZone
                        {
                            Name = $"{lane} Lane Opportunity",
                            Center = GetLaneCenter(lane),
                            Radius = 1200,
                            Type = ZoneType.Opportunity,
                            OpportunityLevel = _currentMapState.LaneStates[lane].OpportunityLevel,
                            IsAvailable = true
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Analyse la carte pour générer des conseils
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeMap(GameState gameState)
        {
            try
            {
                // Analyser les zones de danger
                AnalyzeDangerZones(gameState);
                
                // Analyser les opportunités de vision
                AnalyzeVisionOpportunities(gameState);
                
                // Analyser les opportunités de gank
                AnalyzeGankOpportunities(gameState);
                
                // Analyser les objectifs disponibles
                AnalyzeObjectives(gameState);
                
                // Analyser les rotations possibles
                AnalyzeRotations(gameState);
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'analyse de la carte", ex);
            }
        }

        /// <summary>
        /// Analyse les zones de danger
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeDangerZones(GameState gameState)
        {
            // Vérifier si le joueur est dans une zone de danger
            Position playerPosition = gameState.PlayerPosition;
            
            foreach (var zone in _dangerZones)
            {
                if (IsPositionInZone(playerPosition, zone) && zone.DangerLevel > DANGER_THRESHOLD)
                {
                    // Générer une notification de danger
                    string message = $"Attention! Vous êtes dans une zone dangereuse ({zone.Name}). Reculez vers une position plus sûre.";
                    GenerateMapAdvice(MapAdviceType.Danger, message, zone.DangerLevel);
                    break;
                }
            }
            
            // Vérifier si le joueur se dirige vers une zone de danger
            if (_playerPositionHistory.ContainsKey(gameState.SummonerName) && 
                _playerPositionHistory[gameState.SummonerName].Count >= 2)
            {
                var lastPositions = _playerPositionHistory[gameState.SummonerName]
                    .OrderByDescending(p => p.GameTime)
                    .Take(2)
                    .ToList();
                
                if (lastPositions.Count == 2)
                {
                    Position currentPos = lastPositions[0].Position;
                    Position previousPos = lastPositions[1].Position;
                    
                    // Calculer la direction du mouvement
                    float directionX = currentPos.X - previousPos.X;
                    float directionY = currentPos.Y - previousPos.Y;
                    
                    // Prédire la position future
                    Position predictedPosition = new Position
                    {
                        X = currentPos.X + directionX,
                        Y = currentPos.Y + directionY
                    };
                    
                    foreach (var zone in _dangerZones)
                    {
                        if (IsPositionInZone(predictedPosition, zone) && zone.DangerLevel > DANGER_THRESHOLD)
                        {
                            // Générer une notification de danger imminent
                            string message = $"Danger imminent! Vous vous dirigez vers {zone.Name}. Changez de direction.";
                            GenerateMapAdvice(MapAdviceType.Danger, message, zone.DangerLevel);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyse les opportunités de vision
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeVisionOpportunities(GameState gameState)
        {
            // Vérifier si le joueur a des wards disponibles
            if (gameState.AvailableWards <= 0)
                return;
                
            // Trouver les zones d'intérêt non couvertes par la vision
            foreach (var zone in _interestZones.Where(z => z.Type == ZoneType.VisionPoint && z.IsAvailable))
            {
                bool hasVision = false;
                
                // Vérifier si une ward existe déjà dans cette zone
                foreach (var wardPos in _currentMapState.WardPositions.Concat(_currentMapState.ControlWardPositions))
                {
                    if (IsPositionInZone(wardPos, zone))
                    {
                        hasVision = true;
                        break;
                    }
                }
                
                if (!hasVision)
                {
                    // Vérifier si le joueur est proche de la zone
                    double distance = CalculateDistance(gameState.PlayerPosition, zone.Center);
                    
                    if (distance < 1500) // Si le joueur est à moins de 1500 unités
                    {
                        // Générer une suggestion de ward
                        string message = $"Placez une ward à {zone.Name} pour sécuriser la vision de cette zone stratégique.";
                        GenerateMapAdvice(MapAdviceType.Vision, message, 0.7);
                    }
                }
            }
            
            // Vérifier si un objectif important va bientôt être disponible
            foreach (var zone in _interestZones.Where(z => z.Type == ZoneType.MajorObjective))
            {
                if (zone.IsAvailable || (zone.AvailableFrom - gameState.GameTime <= 60 && zone.AvailableFrom - gameState.GameTime > 0))
                {
                    bool hasVision = false;
                    
                    // Vérifier si une ward existe déjà dans cette zone
                    foreach (var wardPos in _currentMapState.WardPositions.Concat(_currentMapState.ControlWardPositions))
                    {
                        if (IsPositionInZone(wardPos, zone))
                        {
                            hasVision = true;
                            break;
                        }
                    }
                    
                    if (!hasVision)
                    {
                        // Générer une suggestion de ward pour l'objectif
                        string message = $"Placez une ward à {zone.Name} pour préparer le contrôle de cet objectif.";
                        GenerateMapAdvice(MapAdviceType.Vision, message, 0.8);
                    }
                }
            }
        }

        /// <summary>
        /// Analyse les opportunités de gank
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeGankOpportunities(GameState gameState)
        {
            // Vérifier si le joueur est jungler
            if (gameState.PlayerRole != "JUNGLE")
                return;
                
            // Analyser chaque lane pour les opportunités de gank
            foreach (var lane in _currentMapState.LaneStates.Keys)
            {
                if (lane != LaneType.Jungle)
                {
                    double opportunityLevel = _currentMapState.LaneStates[lane].OpportunityLevel;
                    
                    if (opportunityLevel > OPPORTUNITY_THRESHOLD)
                    {
                        // Vérifier si le joueur est proche de la lane
                        double distance = CalculateDistance(gameState.PlayerPosition, GetLaneCenter(lane));
                        
                        if (distance < 2000) // Si le joueur est à moins de 2000 unités
                        {
                            // Générer une suggestion de gank
                            string message = $"Opportunité de gank en {GetLaneName(lane)}. L'ennemi est vulnérable.";
                            GenerateMapAdvice(MapAdviceType.Gank, message, opportunityLevel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyse les objectifs disponibles
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeObjectives(GameState gameState)
        {
            // Vérifier les objectifs disponibles
            foreach (var objective in gameState.Objectives.Keys)
            {
                ObjectiveState state = gameState.Objectives[objective];
                
                // Si l'objectif est disponible ou va bientôt l'être
                if (state.NextSpawnTime <= gameState.GameTime || 
                    (state.NextSpawnTime - gameState.GameTime <= 30 && state.NextSpawnTime - gameState.GameTime > 0))
                {
                    // Vérifier si l'équipe a un avantage pour prendre l'objectif
                    bool hasAdvantage = HasTeamAdvantageForObjective(objective, gameState);
                    
                    if (hasAdvantage)
                    {
                        // Générer une suggestion d'objectif
                        string message = $"{GetObjectiveName(objective)} est disponible et votre équipe a l'avantage. Préparez-vous à le prendre.";
                        GenerateMapAdvice(MapAdviceType.Objective, message, 0.9);
                    }
                }
            }
        }

        /// <summary>
        /// Analyse les rotations possibles
        /// </summary>
        /// <param name="gameState">État actuel du jeu</param>
        private void AnalyzeRotations(GameState gameState)
        {
            // Vérifier si une rotation est nécessaire
            
            // 1. Vérifier si un allié est en difficulté
            foreach (var member in gameState.TeamMembers)
            {
                if (member.IsInDanger && member.Champion.Name != gameState.SummonerName)
                {
                    // Vérifier si le joueur peut aider
                    double distance = CalculateDistance(gameState.PlayerPosition, member.Position);
                    
                    if (distance < 3000) // Si le joueur est à moins de 3000 unités
                    {
                        // Générer une suggestion de rotation
                        string message = $"Votre allié {member.Champion.Name} est en difficulté. Déplacez-vous pour l'aider.";
                        GenerateMapAdvice(MapAdviceType.Rotation, message, 0.8);
                        return;
                    }
                }
            }
            
            // 2. Vérifier si une tour est menacée
            foreach (var lane in _currentMapState.LaneStates.Keys)
            {
                if (_currentMapState.LaneStates[lane].PushStatus == LanePushStatus.EnemyPushing && 
                    _currentMapState.LaneStates[lane].DangerLevel > 0.7)
                {
                    // Vérifier si le joueur peut défendre
                    double distance = CalculateDistance(gameState.PlayerPosition, GetLaneCenter(lane));
                    
                    if (distance < 3000) // Si le joueur est à moins de 3000 unités
                    {
                        // Générer une suggestion de défense
                        string message = $"La tour en {GetLaneName(lane)} est menacée. Déplacez-vous pour la défendre.";
                        GenerateMapAdvice(MapAdviceType.Defense, message, 0.8);
                        return;
                    }
                }
            }
            
            // 3. Vérifier si un objectif est contestable
            foreach (var objective in gameState.Objectives.Keys)
            {
                ObjectiveState state = gameState.Objectives[objective];
                
                if (state.IsContested)
                {
                    // Vérifier si le joueur peut contester
                    MapZone objectiveZone = _interestZones.FirstOrDefault(z => z.Name.Contains(GetObjectiveName(objective)));
                    
                    if (objectiveZone != null)
                    {
                        double distance = CalculateDistance(gameState.PlayerPosition, objectiveZone.Center);
                        
                        if (distance < 3000) // Si le joueur est à moins de 3000 unités
                        {
                            // Générer une suggestion de contestation
                            string message = $"{GetObjectiveName(objective)} est contesté. Rejoignez votre équipe pour le combat.";
                            GenerateMapAdvice(MapAdviceType.Contest, message, 0.9);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Génère un conseil basé sur l'analyse de la carte
        /// </summary>
        /// <param name="type">Type de conseil</param>
        /// <param name="message">Message du conseil</param>
        /// <param name="priority">Priorité du conseil (0.0 à 1.0)</param>
        private void GenerateMapAdvice(MapAdviceType type, string message, double priority)
        {
            // Convertir le type de conseil en type de notification
            NotificationType notificationType;
            
            switch (type)
            {
                case MapAdviceType.Danger:
                    notificationType = NotificationType.FightWarning;
                    break;
                case MapAdviceType.Vision:
                    notificationType = NotificationType.VisionReminder;
                    break;
                case MapAdviceType.Gank:
                case MapAdviceType.Rotation:
                    notificationType = NotificationType.RotationSuggestion;
                    break;
                case MapAdviceType.Objective:
                case MapAdviceType.Contest:
                    notificationType = NotificationType.ObjectiveSpawn;
                    break;
                case MapAdviceType.Defense:
                    notificationType = NotificationType.WaveClear;
                    break;
                default:
                    notificationType = NotificationType.RotationSuggestion;
                    break;
            }
            
            // Créer un événement GameState pour déclencher la notification
            GameState notificationState = new GameState
            {
                GameTime = _currentMapState.GameTime,
                RecentEvents = new List<GameEvent>()
            };
            
            // Ajouter l'événement approprié
            switch (type)
            {
                case MapAdviceType.Danger:
                    notificationState.IsInDangerousPosition = true;
                    break;
                case MapAdviceType.Vision:
                    notificationState.AvailableWards = 1;
                    break;
                case MapAdviceType.Objective:
                    notificationState.RecentEvents.Add(GameEvent.ObjectiveTaken);
                    break;
                default:
                    break;
            }
            
            // Mettre à jour l'état du jeu pour déclencher la notification
            _notificationService.UpdateGameState(notificationState);
            
            // Enregistrer le conseil dans les logs
            _logger.Info($"Conseil de carte généré: {type} - {message} (Priorité: {priority:F2})");
        }

        /// <summary>
        /// Calcule le niveau de danger d'une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <param name="gameState">État actuel du jeu</param>
        /// <returns>Niveau de danger (0.0 à 1.0)</returns>
        private double CalculateLaneDangerLevel(LaneType lane, GameState gameState)
        {
            double dangerLevel = 0.0;
            
            // Facteur 1: Position de la vague
            if (gameState.Waves.ContainsKey(lane))
            {
                switch (gameState.Waves[lane].Position)
                {
                    case WavePosition.NearAllyTower:
                        dangerLevel += 0.3; // Danger modéré
                        break;
                    case WavePosition.Middle:
                        dangerLevel += 0.1; // Danger faible
                        break;
                    case WavePosition.NearEnemyTower:
                        dangerLevel += 0.0; // Pas de danger supplémentaire
                        break;
                }
            }
            
            // Facteur 2: Présence d'ennemis
            int enemiesInLane = CountEnemiesInLane(lane);
            dangerLevel += enemiesInLane * 0.2; // Chaque ennemi ajoute 0.2 au danger
            
            // Facteur 3: Absence d'alliés
            int alliesInLane = CountAlliesInLane(lane);
            if (alliesInLane == 0)
                dangerLevel += 0.3; // Lane sans défense
            else if (alliesInLane < enemiesInLane)
                dangerLevel += 0.2; // Désavantage numérique
                
            // Facteur 4: Absence de vision
            if (!HasVisionInLane(lane))
                dangerLevel += 0.2; // Pas de vision
                
            // Limiter le niveau de danger entre 0.0 et 1.0
            return Math.Min(1.0, dangerLevel);
        }

        /// <summary>
        /// Calcule le niveau d'opportunité d'une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <param name="gameState">État actuel du jeu</param>
        /// <returns>Niveau d'opportunité (0.0 à 1.0)</returns>
        private double CalculateLaneOpportunityLevel(LaneType lane, GameState gameState)
        {
            double opportunityLevel = 0.0;
            
            // Facteur 1: Position de la vague
            if (gameState.Waves.ContainsKey(lane))
            {
                switch (gameState.Waves[lane].Position)
                {
                    case WavePosition.NearAllyTower:
                        opportunityLevel += 0.0; // Pas d'opportunité
                        break;
                    case WavePosition.Middle:
                        opportunityLevel += 0.1; // Opportunité faible
                        break;
                    case WavePosition.NearEnemyTower:
                        opportunityLevel += 0.3; // Opportunité modérée
                        break;
                }
            }
            
            // Facteur 2: Présence d'ennemis vulnérables
            int vulnerableEnemies = CountVulnerableEnemiesInLane(lane);
            opportunityLevel += vulnerableEnemies * 0.3; // Chaque ennemi vulnérable ajoute 0.3 à l'opportunité
            
            // Facteur 3: Avantage numérique
            int alliesInLane = CountAlliesInLane(lane);
            int enemiesInLane = CountEnemiesInLane(lane);
            if (alliesInLane > enemiesInLane)
                opportunityLevel += 0.3; // Avantage numérique
                
            // Facteur 4: Présence de vision
            if (HasVisionInLane(lane))
                opportunityLevel += 0.1; // Bonne vision
                
            // Limiter le niveau d'opportunité entre 0.0 et 1.0
            return Math.Min(1.0, opportunityLevel);
        }

        /// <summary>
        /// Compte le nombre d'ennemis dans une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>Nombre d'ennemis</returns>
        private int CountEnemiesInLane(LaneType lane)
        {
            int count = 0;
            Position laneCenter = GetLaneCenter(lane);
            
            foreach (var enemyPosition in _currentMapState.EnemyMemberPositions)
            {
                double distance = CalculateDistance(enemyPosition.Value, laneCenter);
                if (distance < 2000) // Si l'ennemi est à moins de 2000 unités du centre de la lane
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Compte le nombre d'alliés dans une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>Nombre d'alliés</returns>
        private int CountAlliesInLane(LaneType lane)
        {
            int count = 0;
            Position laneCenter = GetLaneCenter(lane);
            
            foreach (var allyPosition in _currentMapState.TeamMemberPositions)
            {
                double distance = CalculateDistance(allyPosition.Value, laneCenter);
                if (distance < 2000) // Si l'allié est à moins de 2000 unités du centre de la lane
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Compte le nombre d'ennemis vulnérables dans une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>Nombre d'ennemis vulnérables</returns>
        private int CountVulnerableEnemiesInLane(LaneType lane)
        {
            // Dans une version complète, cette méthode analyserait la santé, le mana, etc.
            // Pour cette version, nous supposons qu'un ennemi est vulnérable s'il est seul dans la lane
            return CountEnemiesInLane(lane) == 1 && CountAlliesInLane(lane) >= 1 ? 1 : 0;
        }

        /// <summary>
        /// Vérifie si une lane a de la vision
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>True si la lane a de la vision</returns>
        private bool HasVisionInLane(LaneType lane)
        {
            Position laneCenter = GetLaneCenter(lane);
            
            foreach (var wardPos in _currentMapState.WardPositions.Concat(_currentMapState.ControlWardPositions))
            {
                double distance = CalculateDistance(wardPos, laneCenter);
                if (distance < 1500) // Si une ward est à moins de 1500 unités du centre de la lane
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Vérifie si l'équipe a un avantage pour prendre un objectif
        /// </summary>
        /// <param name="objective">Objectif</param>
        /// <param name="gameState">État actuel du jeu</param>
        /// <returns>True si l'équipe a un avantage</returns>
        private bool HasTeamAdvantageForObjective(ObjectiveType objective, GameState gameState)
        {
            // Dans une version complète, cette méthode analyserait la position des joueurs, leur santé, etc.
            // Pour cette version, nous utilisons une logique simplifiée
            
            MapZone objectiveZone = _interestZones.FirstOrDefault(z => z.Name.Contains(GetObjectiveName(objective)));
            
            if (objectiveZone == null)
                return false;
                
            int alliesNearObjective = 0;
            int enemiesNearObjective = 0;
            
            // Compter les alliés près de l'objectif
            foreach (var allyPosition in _currentMapState.TeamMemberPositions)
            {
                double distance = CalculateDistance(allyPosition.Value, objectiveZone.Center);
                if (distance < objectiveZone.Radius + 1000) // Si l'allié est à proximité de l'objectif
                {
                    alliesNearObjective++;
                }
            }
            
            // Compter les ennemis près de l'objectif
            foreach (var enemyPosition in _currentMapState.EnemyMemberPositions)
            {
                double distance = CalculateDistance(enemyPosition.Value, objectiveZone.Center);
                if (distance < objectiveZone.Radius + 1000) // Si l'ennemi est à proximité de l'objectif
                {
                    enemiesNearObjective++;
                }
            }
            
            // L'équipe a un avantage si elle a plus de joueurs près de l'objectif
            return alliesNearObjective > enemiesNearObjective;
        }

        /// <summary>
        /// Obtient le centre d'une lane
        /// </summary>
        /// <param name="lane">Lane</param>
        /// <returns>Position du centre de la lane</returns>
        private Position GetLaneCenter(LaneType lane)
        {
            switch (lane)
            {
                case LaneType.Top:
                    return new Position { X = 5000, Y = 12000 };
                case LaneType.Mid:
                    return new Position { X = 7500, Y = 7500 };
                case LaneType.Bot:
                    return new Position { X = 12000, Y = 5000 };
                case LaneType.Jungle:
                default:
                    return new Position { X = 7500, Y = 7500 };
            }
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
        /// Calcule la distance entre deux positions
        /// </summary>
        /// <param name="pos1">Position 1</param>
        /// <param name="pos2">Position 2</param>
        /// <returns>Distance</returns>
        private double CalculateDistance(Position pos1, Position pos2)
        {
            return Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2));
        }

        /// <summary>
        /// Vérifie si une position est dans une zone
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="zone">Zone</param>
        /// <returns>True si la position est dans la zone</returns>
        private bool IsPositionInZone(Position position, MapZone zone)
        {
            return CalculateDistance(position, zone.Center) <= zone.Radius;
        }
    }

    /// <summary>
    /// État de la carte
    /// </summary>
    public class MapState
    {
        /// <summary>
        /// Temps de jeu en secondes
        /// </summary>
        public double GameTime { get; set; }
        
        /// <summary>
        /// Score de vision de l'équipe
        /// </summary>
        public int TeamVisionScore { get; set; }
        
        /// <summary>
        /// Positions des wards
        /// </summary>
        public List<Position> WardPositions { get; set; }
        
        /// <summary>
        /// Positions des wards de contrôle
        /// </summary>
        public List<Position> ControlWardPositions { get; set; }
        
        /// <summary>
        /// Positions des membres de l'équipe
        /// </summary>
        public Dictionary<string, Position> TeamMemberPositions { get; set; }
        
        /// <summary>
        /// Positions des membres de l'équipe ennemie
        /// </summary>
        public Dictionary<string, Position> EnemyMemberPositions { get; set; }
        
        /// <summary>
        /// État des camps de jungle
        /// </summary>
        public Dictionary<JungleCreepType, JungleCreepState> JungleCreepStatus { get; set; }
        
        /// <summary>
        /// État des lanes
        /// </summary>
        public Dictionary<LaneType, LaneState> LaneStates { get; set; }
    }

    /// <summary>
    /// Historique des positions
    /// </summary>
    public class PositionHistory
    {
        /// <summary>
        /// Position
        /// </summary>
        public Position Position { get; set; }
        
        /// <summary>
        /// Horodatage
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Temps de jeu
        /// </summary>
        public double GameTime { get; set; }
    }

    /// <summary>
    /// Zone sur la carte
    /// </summary>
    public class MapZone
    {
        /// <summary>
        /// Nom de la zone
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Centre de la zone
        /// </summary>
        public Position Center { get; set; }
        
        /// <summary>
        /// Rayon de la zone
        /// </summary>
        public double Radius { get; set; }
        
        /// <summary>
        /// Type de zone
        /// </summary>
        public ZoneType Type { get; set; }
        
        /// <summary>
        /// Niveau de danger (0.0 à 1.0)
        /// </summary>
        public double DangerLevel { get; set; }
        
        /// <summary>
        /// Niveau d'opportunité (0.0 à 1.0)
        /// </summary>
        public double OpportunityLevel { get; set; }
        
        /// <summary>
        /// Temps de jeu à partir duquel la zone est disponible (en secondes)
        /// </summary>
        public double AvailableFrom { get; set; }
        
        /// <summary>
        /// Temps de jeu jusqu'auquel la zone est disponible (en secondes, 0 = toujours disponible)
        /// </summary>
        public double AvailableUntil { get; set; }
        
        /// <summary>
        /// Indique si la zone est disponible
        /// </summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// État d'un camp de jungle
    /// </summary>
    public class JungleCreepState
    {
        /// <summary>
        /// Indique si le camp est vivant
        /// </summary>
        public bool IsAlive { get; set; }
        
        /// <summary>
        /// Temps de réapparition (en secondes)
        /// </summary>
        public double RespawnTime { get; set; }
    }

    /// <summary>
    /// État d'une lane
    /// </summary>
    public class LaneState
    {
        /// <summary>
        /// État de push de la lane
        /// </summary>
        public LanePushStatus PushStatus { get; set; }
        
        /// <summary>
        /// Niveau de danger (0.0 à 1.0)
        /// </summary>
        public double DangerLevel { get; set; }
        
        /// <summary>
        /// Niveau d'opportunité (0.0 à 1.0)
        /// </summary>
        public double OpportunityLevel { get; set; }
    }

    /// <summary>
    /// Types de zones
    /// </summary>
    public enum ZoneType
    {
        Danger,
        PotentialDanger,
        MajorObjective,
        Buff,
        VisionPoint,
        Opportunity
    }

    /// <summary>
    /// Types de camps de jungle
    /// </summary>
    public enum JungleCreepType
    {
        BlueBuffAlly,
        RedBuffAlly,
        GrompAlly,
        WolvesAlly,
        RaptorsAlly,
        KrugsAlly,
        BlueBuffEnemy,
        RedBuffEnemy,
        GrompEnemy,
        WolvesEnemy,
        RaptorsEnemy,
        KrugsEnemy
    }

    /// <summary>
    /// États de push des lanes
    /// </summary>
    public enum LanePushStatus
    {
        AllyPushing,
        Neutral,
        EnemyPushing
    }

    /// <summary>
    /// Types de conseils de carte
    /// </summary>
    public enum MapAdviceType
    {
        Danger,
        Vision,
        Gank,
        Objective,
        Rotation,
        Defense,
        Contest
    }
}
