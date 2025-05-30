using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service amélioré de détection des picks problématiques ou non-méta
    /// </summary>
    public class EnhancedTrollDetectorService
    {
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private readonly Data.EnhancedDatabaseManager _dbManager;
        
        // Base de données des picks problématiques
        private Dictionary<string, Dictionary<string, TrollPickData>> _trollPicksDatabase;
        
        // Seuil de détection (taux de victoire en dessous duquel un pick est considéré comme problématique)
        private const double TROLL_THRESHOLD = 45.0;
        
        // Seuil de détection pour les picks très problématiques
        private const double SEVERE_TROLL_THRESHOLD = 40.0;

        /// <summary>
        /// Constructeur du service de détection des picks problématiques
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        /// <param name="dbManager">Gestionnaire de base de données</param>
        public EnhancedTrollDetectorService(Logger logger, RiotApiService riotApiService, Data.EnhancedDatabaseManager dbManager)
        {
            _logger = logger;
            _riotApiService = riotApiService;
            _dbManager = dbManager;
            _trollPicksDatabase = new Dictionary<string, Dictionary<string, TrollPickData>>();
        }

        /// <summary>
        /// Initialise le service de détection des picks problématiques
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.Info("Initialisation du service de détection des picks problématiques");
                
                // Charger la base de données des picks problématiques
                await LoadTrollPicksDataAsync();
                
                _logger.Info("Service de détection des picks problématiques initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation du service de détection des picks problématiques", ex);
                throw;
            }
        }

        /// <summary>
        /// Charge la base de données des picks problématiques
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task LoadTrollPicksDataAsync()
        {
            try
            {
                _logger.Info("Chargement de la base de données des picks problématiques");
                
                // Dans une version complète, ces données seraient chargées depuis la base de données
                // Pour cette version, nous initialisons une base de données prédéfinie
                
                _trollPicksDatabase = new Dictionary<string, Dictionary<string, TrollPickData>>();
                
                // Initialiser la base de données pour chaque champion
                InitializeTrollPicksForChampion("Teemo");
                InitializeTrollPicksForChampion("Zed");
                InitializeTrollPicksForChampion("Yuumi");
                InitializeTrollPicksForChampion("Shen");
                InitializeTrollPicksForChampion("Soraka");
                InitializeTrollPicksForChampion("Ivern");
                InitializeTrollPicksForChampion("Yasuo");
                InitializeTrollPicksForChampion("Vayne");
                InitializeTrollPicksForChampion("Nunu");
                InitializeTrollPicksForChampion("Singed");
                
                _logger.Info($"Base de données des picks problématiques chargée pour {_trollPicksDatabase.Count} champions");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors du chargement de la base de données des picks problématiques", ex);
                throw;
            }
        }

        /// <summary>
        /// Initialise les données de picks problématiques pour un champion spécifique
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        private void InitializeTrollPicksForChampion(string championName)
        {
            _trollPicksDatabase[championName] = new Dictionary<string, TrollPickData>();
            
            switch (championName)
            {
                case "Teemo":
                    // Teemo est viable en TOP
                    _trollPicksDatabase[championName]["TOP"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "TOP",
                        WinRate = 49.5,
                        PickRate = 2.3,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Teemo est un pick viable en TOP, bien qu'il soit situationnel.",
                        AlternativeChampions = new List<string> { "Jayce", "Quinn", "Kennen" }
                    };
                    
                    // Teemo est problématique en JUNGLE
                    _trollPicksDatabase[championName]["JUNGLE"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "JUNGLE",
                        WinRate = 38.2,
                        PickRate = 0.1,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "Teemo jungle a un clear très lent, une faible mobilité et des ganks peu efficaces. Il est très vulnérable aux invasions et a du mal à contester les objectifs.",
                        AlternativeChampions = new List<string> { "Elise", "Nidalee", "Kindred" }
                    };
                    
                    // Teemo est problématique en MID
                    _trollPicksDatabase[championName]["MID"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "MID",
                        WinRate = 43.8,
                        PickRate = 0.3,
                        IsTroll = true,
                        TrollLevel = 1,
                        Explanation = "Teemo mid manque de waveclear et de mobilité pour roam efficacement. Il est facilement ganké et a du mal à impacter la carte.",
                        AlternativeChampions = new List<string> { "Heimerdinger", "Neeko", "Annie" }
                    };
                    
                    // Teemo est très problématique en ADC
                    _trollPicksDatabase[championName]["ADC"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "ADC",
                        WinRate = 36.5,
                        PickRate = 0.05,
                        IsTroll = true,
                        TrollLevel = 3,
                        Explanation = "Teemo ADC manque de portée, de scaling et de dégâts soutenus nécessaires pour ce rôle. Il est facilement dominé en lane et devient inutile en mid/late game.",
                        AlternativeChampions = new List<string> { "Vayne", "Quinn", "Twitch" }
                    };
                    
                    // Teemo est problématique en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 41.2,
                        PickRate = 0.2,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "Teemo support manque d'utilité, de CC et de protection pour son ADC. Il vole facilement les kills et les CS, et a besoin d'items pour être efficace.",
                        AlternativeChampions = new List<string> { "Zyra", "Brand", "Shaco" }
                    };
                    break;
                
                case "Zed":
                    // Zed est viable en MID
                    _trollPicksDatabase[championName]["MID"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "MID",
                        WinRate = 50.2,
                        PickRate = 8.5,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Zed est un pick méta en MID, très efficace contre les mages.",
                        AlternativeChampions = new List<string> { "Talon", "Qiyana", "LeBlanc" }
                    };
                    
                    // Zed est situationnel en TOP
                    _trollPicksDatabase[championName]["TOP"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "TOP",
                        WinRate = 46.8,
                        PickRate = 1.2,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Zed top peut être viable dans certains matchups, mais il est vulnérable contre les tanks et les bruisers.",
                        AlternativeChampions = new List<string> { "Jayce", "Akali", "Rengar" }
                    };
                    
                    // Zed est problématique en JUNGLE
                    _trollPicksDatabase[championName]["JUNGLE"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "JUNGLE",
                        WinRate = 39.5,
                        PickRate = 0.3,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "Zed jungle a un premier clear très lent et dangereux, manque de sustain et de CC pour des ganks efficaces. Il est très dépendant des kills pour être utile.",
                        AlternativeChampions = new List<string> { "Kha'Zix", "Rengar", "Kayn" }
                    };
                    
                    // Zed est très problématique en ADC
                    _trollPicksDatabase[championName]["ADC"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "ADC",
                        WinRate = 35.2,
                        PickRate = 0.05,
                        IsTroll = true,
                        TrollLevel = 3,
                        Explanation = "Zed ADC n'a pas de dégâts soutenus à distance, ce qui est essentiel pour ce rôle. Il est facilement contré en lane et ne peut pas remplir le rôle d'un ADC en teamfight.",
                        AlternativeChampions = new List<string> { "Lucian", "Samira", "Tristana" }
                    };
                    
                    // Zed est très problématique en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 32.8,
                        PickRate = 0.02,
                        IsTroll = true,
                        TrollLevel = 3,
                        Explanation = "Zed support n'offre aucune utilité, protection ou CC pour son équipe. Il a besoin d'or pour être efficace et ne peut pas remplir le rôle de support.",
                        AlternativeChampions = new List<string> { "Pyke", "Pantheon", "Sett" }
                    };
                    break;
                
                case "Shen":
                    // Shen est viable en TOP
                    _trollPicksDatabase[championName]["TOP"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "TOP",
                        WinRate = 51.5,
                        PickRate = 4.2,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Shen est un pick méta en TOP, offrant une bonne présence globale avec son ultime.",
                        AlternativeChampions = new List<string> { "Ornn", "Maokai", "Malphite" }
                    };
                    
                    // Shen est viable en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 48.3,
                        PickRate = 1.8,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Shen support est viable, offrant une bonne protection et présence globale.",
                        AlternativeChampions = new List<string> { "Braum", "Taric", "Alistar" }
                    };
                    
                    // Shen est situationnel en JUNGLE
                    _trollPicksDatabase[championName]["JUNGLE"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "JUNGLE",
                        WinRate = 45.2,
                        PickRate = 0.4,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Shen jungle peut fonctionner mais son clear est lent et il est mieux utilisé dans d'autres rôles.",
                        AlternativeChampions = new List<string> { "Rammus", "Zac", "Sejuani" }
                    };
                    
                    // Shen est problématique en MID
                    _trollPicksDatabase[championName]["MID"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "MID",
                        WinRate = 42.7,
                        PickRate = 0.1,
                        IsTroll = true,
                        TrollLevel = 1,
                        Explanation = "Shen mid manque de waveclear et de dégâts pour être efficace dans ce rôle. Il est facilement poussé sous tour et perd en CS.",
                        AlternativeChampions = new List<string> { "Galio", "Pantheon", "Malphite" }
                    };
                    
                    // Shen est très problématique en ADC
                    _trollPicksDatabase[championName]["ADC"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "ADC",
                        WinRate = 34.8,
                        PickRate = 0.01,
                        IsTroll = true,
                        TrollLevel = 3,
                        Explanation = "Shen ADC n'a pas de dégâts à distance et ne peut pas remplir le rôle d'un ADC. Il est complètement inefficace dans cette position.",
                        AlternativeChampions = new List<string> { "Urgot", "Graves", "Kindred" }
                    };
                    
                    // Shen est problématique en RIVER
                    _trollPicksDatabase[championName]["RIVER"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "RIVER",
                        WinRate = 41.5,
                        PickRate = 0.05,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "River Shen est un pick non-méta qui sacrifie l'expérience et l'or pour une présence sur la carte. Il devient rapidement sous-leveled et inefficace en mid/late game.",
                        AlternativeChampions = new List<string> { "Nunu", "Bard", "Pyke" }
                    };
                    break;
                
                case "Yuumi":
                    // Yuumi est viable en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 49.8,
                        PickRate = 6.5,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Yuumi est un pick méta en SUPPORT, offrant des soins et de l'utilité.",
                        AlternativeChampions = new List<string> { "Soraka", "Sona", "Nami" }
                    };
                    
                    // Yuumi est très problématique dans tous les autres rôles
                    foreach (string role in new[] { "TOP", "JUNGLE", "MID", "ADC" })
                    {
                        _trollPicksDatabase[championName][role] = new TrollPickData
                        {
                            ChampionName = championName,
                            Role = role,
                            WinRate = 30.0,
                            PickRate = 0.01,
                            IsTroll = true,
                            TrollLevel = 3,
                            Explanation = $"Yuumi {role} est complètement inefficace. Elle ne peut pas last hit, farmer ou trader efficacement seule, et son kit est conçu pour s'attacher à un allié.",
                            AlternativeChampions = new List<string> { "Soraka", "Lulu", "Karma" }
                        };
                    }
                    break;
                
                case "Singed":
                    // Singed est viable en TOP
                    _trollPicksDatabase[championName]["TOP"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "TOP",
                        WinRate = 50.5,
                        PickRate = 1.8,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Singed est un pick viable en TOP, efficace pour le proxy farming et le split push.",
                        AlternativeChampions = new List<string> { "Yorick", "Tryndamere", "Nasus" }
                    };
                    
                    // Singed est situationnel en MID
                    _trollPicksDatabase[championName]["MID"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "MID",
                        WinRate = 46.2,
                        PickRate = 0.3,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Singed mid peut fonctionner comme un counter pick contre certains champions, mais il est généralement mieux en TOP.",
                        AlternativeChampions = new List<string> { "Galio", "Malphite", "Gragas" }
                    };
                    
                    // Singed est problématique en JUNGLE
                    _trollPicksDatabase[championName]["JUNGLE"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "JUNGLE",
                        WinRate = 42.8,
                        PickRate = 0.1,
                        IsTroll = true,
                        TrollLevel = 1,
                        Explanation = "Singed jungle a un clear lent et des ganks prévisibles. Il est vulnérable aux invasions et manque d'impact en early game.",
                        AlternativeChampions = new List<string> { "Rammus", "Volibear", "Dr. Mundo" }
                    };
                    
                    // Singed est très problématique en ADC
                    _trollPicksDatabase[championName]["ADC"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "ADC",
                        WinRate = 35.5,
                        PickRate = 0.02,
                        IsTroll = true,
                        TrollLevel = 3,
                        Explanation = "Singed ADC n'a pas de dégâts à distance et ne peut pas remplir le rôle d'un ADC. Il est complètement inefficace dans cette position.",
                        AlternativeChampions = new List<string> { "Urgot", "Mordekaiser", "Swain" }
                    };
                    
                    // Singed est problématique en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 41.3,
                        PickRate = 0.1,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "Singed support manque d'utilité et de protection pour son ADC. Son seul CC est son lancer, qui est difficile à utiliser efficacement en lane.",
                        AlternativeChampions = new List<string> { "Alistar", "Blitzcrank", "Leona" }
                    };
                    break;
                
                case "Nunu":
                    // Nunu est viable en JUNGLE
                    _trollPicksDatabase[championName]["JUNGLE"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "JUNGLE",
                        WinRate = 51.2,
                        PickRate = 3.5,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Nunu est un pick méta en JUNGLE, excellent pour le contrôle des objectifs et les ganks.",
                        AlternativeChampions = new List<string> { "Rammus", "Zac", "Sejuani" }
                    };
                    
                    // Nunu est situationnel en TOP
                    _trollPicksDatabase[championName]["TOP"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "TOP",
                        WinRate = 47.5,
                        PickRate = 0.5,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Nunu top peut fonctionner dans certains matchups, offrant une bonne sustain et du CC.",
                        AlternativeChampions = new List<string> { "Cho'Gath", "Maokai", "Ornn" }
                    };
                    
                    // Nunu est situationnel en MID
                    _trollPicksDatabase[championName]["MID"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "MID",
                        WinRate = 46.8,
                        PickRate = 0.4,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Nunu mid peut fonctionner avec son bon waveclear et sa mobilité pour roam.",
                        AlternativeChampions = new List<string> { "Galio", "Aurelion Sol", "Talon" }
                    };
                    
                    // Nunu est problématique en ADC
                    _trollPicksDatabase[championName]["ADC"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "ADC",
                        WinRate = 38.2,
                        PickRate = 0.05,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = "Nunu ADC n'a pas de dégâts à distance soutenus et ne peut pas remplir le rôle d'un ADC efficacement.",
                        AlternativeChampions = new List<string> { "Mordekaiser", "Swain", "Karthus" }
                    };
                    
                    // Nunu est situationnel en SUPPORT
                    _trollPicksDatabase[championName]["SUPPORT"] = new TrollPickData
                    {
                        ChampionName = championName,
                        Role = "SUPPORT",
                        WinRate = 45.5,
                        PickRate = 0.3,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = "Nunu support peut fonctionner avec son CC et sa capacité à roam pour aider le jungler.",
                        AlternativeChampions = new List<string> { "Alistar", "Braum", "Leona" }
                    };
                    break;
                
                default:
                    // Pour les autres champions, initialiser des données génériques
                    foreach (string role in new[] { "TOP", "JUNGLE", "MID", "ADC", "SUPPORT" })
                    {
                        bool isViable = (championName == "Yasuo" && (role == "MID" || role == "TOP")) ||
                                       (championName == "Vayne" && (role == "ADC" || role == "TOP")) ||
                                       (championName == "Soraka" && role == "SUPPORT") ||
                                       (championName == "Ivern" && role == "JUNGLE");
                        
                        _trollPicksDatabase[championName][role] = new TrollPickData
                        {
                            ChampionName = championName,
                            Role = role,
                            WinRate = isViable ? 50.0 : 38.0,
                            PickRate = isViable ? 3.0 : 0.1,
                            IsTroll = !isViable,
                            TrollLevel = isViable ? 0 : 2,
                            Explanation = isViable ? 
                                $"{championName} est un pick viable en {role}." : 
                                $"{championName} n'est pas recommandé en {role}, car son kit n'est pas adapté à ce rôle.",
                            AlternativeChampions = new List<string> { "Champion1", "Champion2", "Champion3" }
                        };
                    }
                    break;
            }
        }

        /// <summary>
        /// Analyse une composition d'équipe pour détecter les picks problématiques
        /// </summary>
        /// <param name="teamComposition">Composition d'équipe</param>
        /// <returns>Liste des picks problématiques détectés</returns>
        public async Task<List<TrollPickAnalysis>> AnalyzeTeamCompositionAsync(TeamComposition teamComposition)
        {
            try
            {
                _logger.Info($"Analyse de la composition d'équipe pour détecter les picks problématiques");
                
                List<TrollPickAnalysis> trollPicks = new List<TrollPickAnalysis>();
                
                // Analyser chaque membre de l'équipe
                foreach (var member in teamComposition.TeamMembers)
                {
                    TrollPickAnalysis analysis = await AnalyzePickAsync(member.Champion.Name, member.Role);
                    
                    if (analysis.IsTroll)
                    {
                        trollPicks.Add(analysis);
                    }
                }
                
                // Trier les picks problématiques par niveau de gravité
                trollPicks = trollPicks.OrderByDescending(p => p.TrollLevel).ToList();
                
                _logger.Info($"Analyse terminée: {trollPicks.Count} picks problématiques détectés");
                
                return trollPicks;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'analyse de la composition d'équipe", ex);
                throw;
            }
        }

        /// <summary>
        /// Analyse un pick spécifique pour déterminer s'il est problématique
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="role">Rôle</param>
        /// <returns>Analyse du pick</returns>
        public async Task<TrollPickAnalysis> AnalyzePickAsync(string championName, string role)
        {
            try
            {
                _logger.Info($"Analyse du pick {championName} {role}");
                
                // Vérifier si le champion existe dans la base de données
                if (!_trollPicksDatabase.ContainsKey(championName))
                {
                    _logger.Warning($"Aucune donnée pour {championName}, utilisation de données génériques");
                    
                    // Créer une analyse générique
                    return new TrollPickAnalysis
                    {
                        ChampionName = championName,
                        Role = role,
                        WinRate = 50.0,
                        PickRate = 1.0,
                        IsTroll = false,
                        TrollLevel = 0,
                        Explanation = $"Pas assez de données pour analyser {championName} en {role}.",
                        AlternativeChampions = new List<string>()
                    };
                }
                
                // Vérifier si le rôle existe pour ce champion
                if (!_trollPicksDatabase[championName].ContainsKey(role))
                {
                    _logger.Warning($"Aucune donnée pour {championName} en {role}, utilisation de données génériques");
                    
                    // Créer une analyse générique pour ce rôle
                    return new TrollPickAnalysis
                    {
                        ChampionName = championName,
                        Role = role,
                        WinRate = 40.0,
                        PickRate = 0.1,
                        IsTroll = true,
                        TrollLevel = 2,
                        Explanation = $"{championName} n'est généralement pas joué en {role} car son kit n'est pas adapté à ce rôle.",
                        AlternativeChampions = GetDefaultAlternativesForRole(role)
                    };
                }
                
                // Récupérer les données pour ce champion et ce rôle
                TrollPickData data = _trollPicksDatabase[championName][role];
                
                // Créer l'analyse
                TrollPickAnalysis analysis = new TrollPickAnalysis
                {
                    ChampionName = data.ChampionName,
                    Role = data.Role,
                    WinRate = data.WinRate,
                    PickRate = data.PickRate,
                    IsTroll = data.IsTroll,
                    TrollLevel = data.TrollLevel,
                    Explanation = data.Explanation,
                    AlternativeChampions = data.AlternativeChampions
                };
                
                _logger.Info($"Analyse terminée pour {championName} {role}: {(analysis.IsTroll ? "Problématique" : "Viable")}");
                
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de l'analyse du pick {championName} {role}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obtient les alternatives par défaut pour un rôle
        /// </summary>
        /// <param name="role">Rôle</param>
        /// <returns>Liste des champions alternatifs</returns>
        private List<string> GetDefaultAlternativesForRole(string role)
        {
            switch (role)
            {
                case "TOP":
                    return new List<string> { "Darius", "Garen", "Malphite" };
                case "JUNGLE":
                    return new List<string> { "Warwick", "Amumu", "Vi" };
                case "MID":
                    return new List<string> { "Annie", "Lux", "Ahri" };
                case "ADC":
                    return new List<string> { "Ashe", "Miss Fortune", "Caitlyn" };
                case "SUPPORT":
                    return new List<string> { "Soraka", "Leona", "Blitzcrank" };
                default:
                    return new List<string> { "Champion1", "Champion2", "Champion3" };
            }
        }

        /// <summary>
        /// Obtient les picks problématiques connus pour un rôle
        /// </summary>
        /// <param name="role">Rôle</param>
        /// <returns>Liste des picks problématiques</returns>
        public async Task<List<TrollPickData>> GetKnownTrollPicksForRoleAsync(string role)
        {
            try
            {
                _logger.Info($"Obtention des picks problématiques connus pour le rôle {role}");
                
                List<TrollPickData> trollPicks = new List<TrollPickData>();
                
                // Parcourir tous les champions et rôles dans la base de données
                foreach (var champion in _trollPicksDatabase.Keys)
                {
                    if (_trollPicksDatabase[champion].ContainsKey(role) && _trollPicksDatabase[champion][role].IsTroll)
                    {
                        trollPicks.Add(_trollPicksDatabase[champion][role]);
                    }
                }
                
                // Trier les picks problématiques par niveau de gravité
                trollPicks = trollPicks.OrderByDescending(p => p.TrollLevel).ToList();
                
                _logger.Info($"{trollPicks.Count} picks problématiques trouvés pour le rôle {role}");
                
                return trollPicks;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de l'obtention des picks problématiques pour le rôle {role}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obtient tous les picks problématiques connus
        /// </summary>
        /// <returns>Liste des picks problématiques</returns>
        public async Task<List<TrollPickData>> GetAllKnownTrollPicksAsync()
        {
            try
            {
                _logger.Info("Obtention de tous les picks problématiques connus");
                
                List<TrollPickData> trollPicks = new List<TrollPickData>();
                
                // Parcourir tous les champions et rôles dans la base de données
                foreach (var champion in _trollPicksDatabase.Keys)
                {
                    foreach (var role in _trollPicksDatabase[champion].Keys)
                    {
                        if (_trollPicksDatabase[champion][role].IsTroll)
                        {
                            trollPicks.Add(_trollPicksDatabase[champion][role]);
                        }
                    }
                }
                
                // Trier les picks problématiques par niveau de gravité
                trollPicks = trollPicks.OrderByDescending(p => p.TrollLevel).ToList();
                
                _logger.Info($"{trollPicks.Count} picks problématiques trouvés au total");
                
                return trollPicks;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'obtention de tous les picks problématiques", ex);
                throw;
            }
        }

        /// <summary>
        /// Obtient les picks les plus problématiques (niveau 3)
        /// </summary>
        /// <returns>Liste des picks les plus problématiques</returns>
        public async Task<List<TrollPickData>> GetMostSevereTrollPicksAsync()
        {
            try
            {
                _logger.Info("Obtention des picks les plus problématiques");
                
                List<TrollPickData> trollPicks = new List<TrollPickData>();
                
                // Parcourir tous les champions et rôles dans la base de données
                foreach (var champion in _trollPicksDatabase.Keys)
                {
                    foreach (var role in _trollPicksDatabase[champion].Keys)
                    {
                        if (_trollPicksDatabase[champion][role].TrollLevel == 3)
                        {
                            trollPicks.Add(_trollPicksDatabase[champion][role]);
                        }
                    }
                }
                
                // Trier les picks problématiques par taux de victoire
                trollPicks = trollPicks.OrderBy(p => p.WinRate).ToList();
                
                _logger.Info($"{trollPicks.Count} picks très problématiques trouvés");
                
                return trollPicks;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'obtention des picks les plus problématiques", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Données d'un pick problématique
    /// </summary>
    public class TrollPickData
    {
        /// <summary>
        /// Nom du champion
        /// </summary>
        public string ChampionName { get; set; }
        
        /// <summary>
        /// Rôle
        /// </summary>
        public string Role { get; set; }
        
        /// <summary>
        /// Taux de victoire
        /// </summary>
        public double WinRate { get; set; }
        
        /// <summary>
        /// Taux de sélection
        /// </summary>
        public double PickRate { get; set; }
        
        /// <summary>
        /// Indique si le pick est problématique
        /// </summary>
        public bool IsTroll { get; set; }
        
        /// <summary>
        /// Niveau de gravité du pick problématique (0-3)
        /// </summary>
        public int TrollLevel { get; set; }
        
        /// <summary>
        /// Explication du problème
        /// </summary>
        public string Explanation { get; set; }
        
        /// <summary>
        /// Champions alternatifs recommandés
        /// </summary>
        public List<string> AlternativeChampions { get; set; }
    }

    /// <summary>
    /// Analyse d'un pick potentiellement problématique
    /// </summary>
    public class TrollPickAnalysis : TrollPickData
    {
        // Hérite de toutes les propriétés de TrollPickData
    }
}
