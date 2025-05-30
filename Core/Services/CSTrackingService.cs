using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service de suivi des statistiques CS (Creep Score) en temps réel
    /// </summary>
    public class CSTrackingService
    {
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private readonly Data.EnhancedDatabaseManager _dbManager;
        
        // Constantes pour les seuils de CS par minute selon les rangs
        private readonly Dictionary<string, double> _csThresholdsByRank = new Dictionary<string, double>
        {
            { "IRON", 5.0 },
            { "BRONZE", 5.5 },
            { "SILVER", 6.0 },
            { "GOLD", 6.5 },
            { "PLATINUM", 7.0 },
            { "DIAMOND", 7.5 },
            { "MASTER", 8.0 },
            { "GRANDMASTER", 8.5 },
            { "CHALLENGER", 9.0 }
        };

        // Données de suivi pour la partie en cours
        private int _currentCS;
        private double _currentGameTime; // en minutes
        private string _targetRank;
        private double _targetCSPerMin;
        private List<CSDataPoint> _csHistory;

        /// <summary>
        /// Constructeur du service de suivi CS
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        /// <param name="dbManager">Gestionnaire de base de données</param>
        public CSTrackingService(Logger logger, RiotApiService riotApiService, Data.EnhancedDatabaseManager dbManager)
        {
            _logger = logger;
            _riotApiService = riotApiService;
            _dbManager = dbManager;
            _csHistory = new List<CSDataPoint>();
            _targetRank = "GOLD"; // Rang cible par défaut
            _targetCSPerMin = _csThresholdsByRank[_targetRank];
        }

        /// <summary>
        /// Initialise le service de suivi CS pour une nouvelle partie
        /// </summary>
        /// <param name="summonerName">Nom d'invocateur du joueur</param>
        /// <param name="targetRank">Rang cible pour les comparaisons</param>
        /// <returns>Tâche asynchrone</returns>
        public async Task InitializeTrackingAsync(string summonerName, string targetRank = null)
        {
            try
            {
                _logger.Info($"Initialisation du suivi CS pour {summonerName}");
                
                // Réinitialiser les données de suivi
                _currentCS = 0;
                _currentGameTime = 0;
                _csHistory.Clear();
                
                // Définir le rang cible si spécifié
                if (!string.IsNullOrEmpty(targetRank) && _csThresholdsByRank.ContainsKey(targetRank))
                {
                    _targetRank = targetRank;
                    _targetCSPerMin = _csThresholdsByRank[_targetRank];
                }
                
                _logger.Info($"Suivi CS initialisé pour {summonerName} avec rang cible {_targetRank} (CS/min: {_targetCSPerMin})");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation du suivi CS", ex);
                throw;
            }
        }

        /// <summary>
        /// Met à jour les données CS avec les informations en temps réel
        /// </summary>
        /// <param name="currentCS">CS actuel du joueur</param>
        /// <param name="gameTimeMinutes">Temps de jeu en minutes</param>
        public void UpdateCSData(int currentCS, double gameTimeMinutes)
        {
            try
            {
                _currentCS = currentCS;
                _currentGameTime = gameTimeMinutes;
                
                // Ajouter un point de données à l'historique
                _csHistory.Add(new CSDataPoint
                {
                    CS = currentCS,
                    GameTimeMinutes = gameTimeMinutes,
                    CSPerMinute = gameTimeMinutes > 0 ? currentCS / gameTimeMinutes : 0
                });
                
                _logger.Info($"Données CS mises à jour: {currentCS} CS à {gameTimeMinutes:F1} minutes");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de la mise à jour des données CS", ex);
            }
        }

        /// <summary>
        /// Obtient le taux de CS par minute actuel
        /// </summary>
        /// <returns>CS par minute</returns>
        public double GetCurrentCSPerMinute()
        {
            if (_currentGameTime <= 0)
                return 0;
                
            return _currentCS / _currentGameTime;
        }

        /// <summary>
        /// Obtient la différence entre le taux de CS actuel et le taux cible
        /// </summary>
        /// <returns>Différence de CS par minute (positif = au-dessus de la cible)</returns>
        public double GetCSPerMinuteDifference()
        {
            return GetCurrentCSPerMinute() - _targetCSPerMin;
        }

        /// <summary>
        /// Vérifie si le joueur est en dessous du seuil de CS pour son rang cible
        /// </summary>
        /// <returns>True si le joueur est en dessous du seuil</returns>
        public bool IsBelowTargetThreshold()
        {
            return GetCurrentCSPerMinute() < _targetCSPerMin;
        }

        /// <summary>
        /// Obtient des conseils pour améliorer le CS en fonction des performances actuelles
        /// </summary>
        /// <returns>Liste de conseils</returns>
        public List<string> GetCSImprovementTips()
        {
            var tips = new List<string>();
            
            double csPerMin = GetCurrentCSPerMinute();
            double difference = GetCSPerMinuteDifference();
            
            if (difference < -2.0)
            {
                tips.Add("Votre CS est très en dessous de l'objectif. Concentrez-vous sur le last hit des minions.");
                tips.Add("Essayez de rester en lane plus longtemps avant de roam.");
            }
            else if (difference < -1.0)
            {
                tips.Add($"Votre CS ({csPerMin:F1}/min) est en dessous de l'objectif pour {_targetRank} ({_targetCSPerMin:F1}/min).");
                tips.Add("Assurez-vous de ne pas manquer de minions sous la tour.");
            }
            else if (difference < 0)
            {
                tips.Add($"Votre CS ({csPerMin:F1}/min) est légèrement en dessous de l'objectif ({_targetCSPerMin:F1}/min).");
                tips.Add("Optimisez vos rotations pour ne pas manquer de vagues de minions.");
            }
            else
            {
                tips.Add($"Excellent travail! Votre CS ({csPerMin:F1}/min) est au-dessus de l'objectif pour {_targetRank}.");
            }
            
            return tips;
        }

        /// <summary>
        /// Obtient l'historique des données CS
        /// </summary>
        /// <returns>Historique des données CS</returns>
        public List<CSDataPoint> GetCSHistory()
        {
            return _csHistory;
        }

        /// <summary>
        /// Définit le rang cible pour les comparaisons
        /// </summary>
        /// <param name="targetRank">Rang cible</param>
        /// <returns>True si le rang est valide et a été défini</returns>
        public bool SetTargetRank(string targetRank)
        {
            if (_csThresholdsByRank.ContainsKey(targetRank))
            {
                _targetRank = targetRank;
                _targetCSPerMin = _csThresholdsByRank[_targetRank];
                _logger.Info($"Rang cible défini à {_targetRank} (CS/min: {_targetCSPerMin})");
                return true;
            }
            
            _logger.Warning($"Rang cible invalide: {targetRank}");
            return false;
        }

        /// <summary>
        /// Obtient les statistiques CS moyennes pour un rang spécifique
        /// </summary>
        /// <param name="rank">Rang</param>
        /// <returns>Statistiques CS moyennes</returns>
        public async Task<CSRankStatistics> GetRankCSStatisticsAsync(string rank)
        {
            try
            {
                if (!_csThresholdsByRank.ContainsKey(rank))
                {
                    throw new ArgumentException($"Rang invalide: {rank}");
                }
                
                // Dans une version complète, ces données seraient récupérées depuis la base de données
                // Pour cette version, nous utilisons les valeurs prédéfinies
                return new CSRankStatistics
                {
                    Rank = rank,
                    AverageCSPerMinute = _csThresholdsByRank[rank],
                    AverageCSAt10Min = _csThresholdsByRank[rank] * 10,
                    AverageCSAt20Min = _csThresholdsByRank[rank] * 20,
                    AverageCSAt30Min = _csThresholdsByRank[rank] * 30
                };
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de la récupération des statistiques CS pour le rang {rank}", ex);
                throw;
            }
        }

        /// <summary>
        /// Enregistre les données CS de la partie dans la base de données
        /// </summary>
        /// <param name="summonerId">ID de l'invocateur</param>
        /// <param name="championId">ID du champion</param>
        /// <param name="role">Rôle joué</param>
        /// <returns>Tâche asynchrone</returns>
        public async Task SaveGameCSDataAsync(string summonerId, int championId, string role)
        {
            try
            {
                if (_csHistory.Count == 0)
                {
                    _logger.Warning("Aucune donnée CS à enregistrer");
                    return;
                }
                
                // Calculer les statistiques finales
                int finalCS = _csHistory.Last().CS;
                double finalGameTime = _csHistory.Last().GameTimeMinutes;
                double finalCSPerMin = finalGameTime > 0 ? finalCS / finalGameTime : 0;
                
                // Dans une version complète, ces données seraient enregistrées dans la base de données
                _logger.Info($"Données CS enregistrées: {finalCS} CS en {finalGameTime:F1} minutes ({finalCSPerMin:F1} CS/min)");
                
                // Exemple d'enregistrement dans la base de données
                // await _dbManager.SaveCSDataAsync(summonerId, championId, role, finalCS, finalGameTime, finalCSPerMin);
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'enregistrement des données CS", ex);
            }
        }
    }

    /// <summary>
    /// Représente un point de données CS à un moment donné de la partie
    /// </summary>
    public class CSDataPoint
    {
        /// <summary>
        /// Nombre de CS
        /// </summary>
        public int CS { get; set; }
        
        /// <summary>
        /// Temps de jeu en minutes
        /// </summary>
        public double GameTimeMinutes { get; set; }
        
        /// <summary>
        /// Taux de CS par minute
        /// </summary>
        public double CSPerMinute { get; set; }
    }

    /// <summary>
    /// Représente les statistiques CS moyennes pour un rang spécifique
    /// </summary>
    public class CSRankStatistics
    {
        /// <summary>
        /// Rang
        /// </summary>
        public string Rank { get; set; }
        
        /// <summary>
        /// CS par minute moyen
        /// </summary>
        public double AverageCSPerMinute { get; set; }
        
        /// <summary>
        /// CS moyen à 10 minutes
        /// </summary>
        public double AverageCSAt10Min { get; set; }
        
        /// <summary>
        /// CS moyen à 20 minutes
        /// </summary>
        public double AverageCSAt20Min { get; set; }
        
        /// <summary>
        /// CS moyen à 30 minutes
        /// </summary>
        public double AverageCSAt30Min { get; set; }
    }
}
