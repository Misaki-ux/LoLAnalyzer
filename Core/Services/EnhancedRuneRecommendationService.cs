using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service amélioré de recommandation de runes basé sur les matchups et le style de jeu
    /// </summary>
    public class EnhancedRuneRecommendationService
    {
        private readonly Logger _logger;
        private readonly RiotApiService _riotApiService;
        private readonly Data.EnhancedDatabaseManager _dbManager;
        
        // Cache des recommandations de runes
        private Dictionary<string, Dictionary<string, RuneSet>> _runeRecommendationsCache;
        
        // Styles de jeu pour les recommandations personnalisées
        private readonly List<PlayStyle> _playStyles = new List<PlayStyle>
        {
            new PlayStyle { Id = "AGGRESSIVE", Name = "Agressif", Description = "Favorise les trades et les all-ins" },
            new PlayStyle { Id = "PASSIVE", Name = "Passif", Description = "Favorise le farm et les trades courts" },
            new PlayStyle { Id = "ROAMING", Name = "Roaming", Description = "Favorise les déplacements sur la carte" },
            new PlayStyle { Id = "SCALING", Name = "Scaling", Description = "Favorise le jeu tardif" },
            new PlayStyle { Id = "UTILITY", Name = "Utilitaire", Description = "Favorise le soutien à l'équipe" }
        };

        /// <summary>
        /// Constructeur du service de recommandation de runes
        /// </summary>
        /// <param name="logger">Logger pour tracer les opérations</param>
        /// <param name="riotApiService">Service d'accès à l'API Riot</param>
        /// <param name="dbManager">Gestionnaire de base de données</param>
        public EnhancedRuneRecommendationService(Logger logger, RiotApiService riotApiService, Data.EnhancedDatabaseManager dbManager)
        {
            _logger = logger;
            _riotApiService = riotApiService;
            _dbManager = dbManager;
            _runeRecommendationsCache = new Dictionary<string, Dictionary<string, RuneSet>>();
        }

        /// <summary>
        /// Initialise le service de recommandation de runes
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.Info("Initialisation du service de recommandation de runes");
                
                // Charger les données de runes depuis la base de données
                await LoadRuneDataAsync();
                
                _logger.Info("Service de recommandation de runes initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation du service de recommandation de runes", ex);
                throw;
            }
        }

        /// <summary>
        /// Charge les données de runes depuis la base de données
        /// </summary>
        /// <returns>Tâche asynchrone</returns>
        private async Task LoadRuneDataAsync()
        {
            try
            {
                _logger.Info("Chargement des données de runes");
                
                // Dans une version complète, ces données seraient chargées depuis la base de données
                // Pour cette version, nous initialisons un cache avec des données prédéfinies
                
                _runeRecommendationsCache = new Dictionary<string, Dictionary<string, RuneSet>>();
                
                // Exemple de données pour quelques champions
                InitializeRuneDataForChampion("Teemo");
                InitializeRuneDataForChampion("Zed");
                InitializeRuneDataForChampion("Yasuo");
                InitializeRuneDataForChampion("Shen");
                InitializeRuneDataForChampion("Lux");
                InitializeRuneDataForChampion("Jinx");
                InitializeRuneDataForChampion("Thresh");
                InitializeRuneDataForChampion("LeeSin");
                
                _logger.Info($"Données de runes chargées pour {_runeRecommendationsCache.Count} champions");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors du chargement des données de runes", ex);
                throw;
            }
        }

        /// <summary>
        /// Initialise les données de runes pour un champion spécifique
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        private void InitializeRuneDataForChampion(string championName)
        {
            _runeRecommendationsCache[championName] = new Dictionary<string, RuneSet>();
            
            // Runes par défaut (sans matchup spécifique)
            _runeRecommendationsCache[championName]["DEFAULT"] = CreateDefaultRuneSet(championName);
            
            // Runes pour différents styles de jeu
            foreach (var style in _playStyles)
            {
                _runeRecommendationsCache[championName][style.Id] = CreateStyleSpecificRuneSet(championName, style.Id);
            }
            
            // Runes pour quelques matchups spécifiques
            switch (championName)
            {
                case "Teemo":
                    _runeRecommendationsCache[championName]["vs_Darius"] = CreateMatchupSpecificRuneSet("Teemo", "Darius");
                    _runeRecommendationsCache[championName]["vs_Garen"] = CreateMatchupSpecificRuneSet("Teemo", "Garen");
                    _runeRecommendationsCache[championName]["vs_Nasus"] = CreateMatchupSpecificRuneSet("Teemo", "Nasus");
                    break;
                case "Zed":
                    _runeRecommendationsCache[championName]["vs_Syndra"] = CreateMatchupSpecificRuneSet("Zed", "Syndra");
                    _runeRecommendationsCache[championName]["vs_Ahri"] = CreateMatchupSpecificRuneSet("Zed", "Ahri");
                    _runeRecommendationsCache[championName]["vs_Yasuo"] = CreateMatchupSpecificRuneSet("Zed", "Yasuo");
                    break;
                case "Yasuo":
                    _runeRecommendationsCache[championName]["vs_Zed"] = CreateMatchupSpecificRuneSet("Yasuo", "Zed");
                    _runeRecommendationsCache[championName]["vs_Syndra"] = CreateMatchupSpecificRuneSet("Yasuo", "Syndra");
                    _runeRecommendationsCache[championName]["vs_Ahri"] = CreateMatchupSpecificRuneSet("Yasuo", "Ahri");
                    break;
                case "Shen":
                    _runeRecommendationsCache[championName]["vs_Darius"] = CreateMatchupSpecificRuneSet("Shen", "Darius");
                    _runeRecommendationsCache[championName]["vs_Garen"] = CreateMatchupSpecificRuneSet("Shen", "Garen");
                    _runeRecommendationsCache[championName]["vs_Fiora"] = CreateMatchupSpecificRuneSet("Shen", "Fiora");
                    break;
                case "Lux":
                    _runeRecommendationsCache[championName]["vs_Zed"] = CreateMatchupSpecificRuneSet("Lux", "Zed");
                    _runeRecommendationsCache[championName]["vs_Yasuo"] = CreateMatchupSpecificRuneSet("Lux", "Yasuo");
                    _runeRecommendationsCache[championName]["vs_Syndra"] = CreateMatchupSpecificRuneSet("Lux", "Syndra");
                    break;
                case "Jinx":
                    _runeRecommendationsCache[championName]["vs_Caitlyn"] = CreateMatchupSpecificRuneSet("Jinx", "Caitlyn");
                    _runeRecommendationsCache[championName]["vs_Draven"] = CreateMatchupSpecificRuneSet("Jinx", "Draven");
                    _runeRecommendationsCache[championName]["vs_Ezreal"] = CreateMatchupSpecificRuneSet("Jinx", "Ezreal");
                    break;
                case "Thresh":
                    _runeRecommendationsCache[championName]["vs_Blitzcrank"] = CreateMatchupSpecificRuneSet("Thresh", "Blitzcrank");
                    _runeRecommendationsCache[championName]["vs_Leona"] = CreateMatchupSpecificRuneSet("Thresh", "Leona");
                    _runeRecommendationsCache[championName]["vs_Morgana"] = CreateMatchupSpecificRuneSet("Thresh", "Morgana");
                    break;
                case "LeeSin":
                    _runeRecommendationsCache[championName]["vs_Elise"] = CreateMatchupSpecificRuneSet("LeeSin", "Elise");
                    _runeRecommendationsCache[championName]["vs_Graves"] = CreateMatchupSpecificRuneSet("LeeSin", "Graves");
                    _runeRecommendationsCache[championName]["vs_Kha'Zix"] = CreateMatchupSpecificRuneSet("LeeSin", "Kha'Zix");
                    break;
            }
        }

        /// <summary>
        /// Crée un ensemble de runes par défaut pour un champion
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <returns>Ensemble de runes</returns>
        private RuneSet CreateDefaultRuneSet(string championName)
        {
            // Dans une version complète, ces données seraient basées sur des statistiques réelles
            // Pour cette version, nous utilisons des données prédéfinies
            
            switch (championName)
            {
                case "Teemo":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Domination",
                        PrimaryKeystone = "Électrocuter",
                        PrimaryRunes = new List<string> { "Goût du sang", "Zombie Ward", "Chasseur de trésors" },
                        SecondaryTree = "Sorcellerie",
                        SecondaryRunes = new List<string> { "Transcendance", "Brûlure" },
                        StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" },
                        WinRate = 52.3,
                        PickRate = 65.8,
                        Description = "Configuration standard pour Teemo, offrant un bon équilibre entre dégâts et survie."
                    };
                case "Zed":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Domination",
                        PrimaryKeystone = "Électrocuter",
                        PrimaryRunes = new List<string> { "Goût du sang", "Zombie Ward", "Chasseur de primes" },
                        SecondaryTree = "Sorcellerie",
                        SecondaryRunes = new List<string> { "Transcendance", "Concentration absolue" },
                        StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" },
                        WinRate = 51.5,
                        PickRate = 72.3,
                        Description = "Configuration standard pour Zed, maximisant les dégâts en burst."
                    };
                case "Yasuo":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Précision",
                        PrimaryKeystone = "Conquérant",
                        PrimaryRunes = new List<string> { "Triomphe", "Légende : Alacrité", "Coup de grâce" },
                        SecondaryTree = "Domination",
                        SecondaryRunes = new List<string> { "Goût du sang", "Chasseur de primes" },
                        StatRunes = new List<string> { "Vitesse d'attaque", "Attaque adaptative", "Armure" },
                        WinRate = 50.2,
                        PickRate = 68.7,
                        Description = "Configuration standard pour Yasuo, équilibrant dégâts et survie."
                    };
                case "Shen":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Résolution",
                        PrimaryKeystone = "Poigne de l'immortel",
                        PrimaryRunes = new List<string> { "Démolition", "Conditionnement", "Croissance excessive" },
                        SecondaryTree = "Précision",
                        SecondaryRunes = new List<string> { "Triomphe", "Légende : Ténacité" },
                        StatRunes = new List<string> { "Attaque adaptative", "Armure", "Résistance magique" },
                        WinRate = 51.8,
                        PickRate = 62.5,
                        Description = "Configuration standard pour Shen, maximisant la tankiness et l'utilité."
                    };
                case "Lux":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Sorcellerie",
                        PrimaryKeystone = "Invocation d'Aery",
                        PrimaryRunes = new List<string> { "Orbe du débit", "Transcendance", "Brûlure" },
                        SecondaryTree = "Inspiration",
                        SecondaryRunes = new List<string> { "Biscuits de livraison", "Perspicacité cosmique" },
                        StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" },
                        WinRate = 52.0,
                        PickRate = 70.2,
                        Description = "Configuration standard pour Lux, maximisant les dégâts et le poke."
                    };
                case "Jinx":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Précision",
                        PrimaryKeystone = "Tempo mortel",
                        PrimaryRunes = new List<string> { "Triomphe", "Légende : Alacrité", "Coup de grâce" },
                        SecondaryTree = "Domination",
                        SecondaryRunes = new List<string> { "Goût du sang", "Chasseur de trésors" },
                        StatRunes = new List<string> { "Vitesse d'attaque", "Attaque adaptative", "Armure" },
                        WinRate = 51.3,
                        PickRate = 68.9,
                        Description = "Configuration standard pour Jinx, maximisant les dégâts auto-attaques."
                    };
                case "Thresh":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Résolution",
                        PrimaryKeystone = "Gardien",
                        PrimaryRunes = new List<string> { "Fontaine de vie", "Ossature", "Croissance excessive" },
                        SecondaryTree = "Inspiration",
                        SecondaryRunes = new List<string> { "Chaussures magiques", "Perspicacité cosmique" },
                        StatRunes = new List<string> { "Vitesse d'attaque", "Armure", "Résistance magique" },
                        WinRate = 50.5,
                        PickRate = 65.3,
                        Description = "Configuration standard pour Thresh, maximisant l'utilité et la survie."
                    };
                case "LeeSin":
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Domination",
                        PrimaryKeystone = "Électrocuter",
                        PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" },
                        SecondaryTree = "Précision",
                        SecondaryRunes = new List<string> { "Triomphe", "Légende : Ténacité" },
                        StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" },
                        WinRate = 49.8,
                        PickRate = 71.5,
                        Description = "Configuration standard pour Lee Sin, équilibrant dégâts et survie."
                    };
                default:
                    return new RuneSet
                    {
                        ChampionName = championName,
                        MatchupChampionName = "DEFAULT",
                        PrimaryTree = "Précision",
                        PrimaryKeystone = "Conquérant",
                        PrimaryRunes = new List<string> { "Triomphe", "Légende : Alacrité", "Coup de grâce" },
                        SecondaryTree = "Domination",
                        SecondaryRunes = new List<string> { "Goût du sang", "Chasseur de primes" },
                        StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" },
                        WinRate = 50.0,
                        PickRate = 60.0,
                        Description = "Configuration générique, adaptée à la plupart des situations."
                    };
            }
        }

        /// <summary>
        /// Crée un ensemble de runes spécifique à un style de jeu pour un champion
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="styleId">ID du style de jeu</param>
        /// <returns>Ensemble de runes</returns>
        private RuneSet CreateStyleSpecificRuneSet(string championName, string styleId)
        {
            // Dans une version complète, ces données seraient basées sur des statistiques réelles
            // Pour cette version, nous utilisons des données prédéfinies
            
            // Commencer avec les runes par défaut
            RuneSet baseRuneSet = CreateDefaultRuneSet(championName);
            RuneSet styleRuneSet = new RuneSet
            {
                ChampionName = championName,
                MatchupChampionName = "DEFAULT",
                StyleId = styleId,
                WinRate = baseRuneSet.WinRate,
                PickRate = baseRuneSet.PickRate / 2 // Moins populaire que la configuration par défaut
            };
            
            switch (styleId)
            {
                case "AGGRESSIVE":
                    styleRuneSet.Description = $"Configuration agressive pour {championName}, maximisant les dégâts et les trades.";
                    
                    // Adapter les runes en fonction du champion
                    switch (championName)
                    {
                        case "Teemo":
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Électrocuter";
                            styleRuneSet.PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" };
                            styleRuneSet.SecondaryTree = "Sorcellerie";
                            styleRuneSet.SecondaryRunes = new List<string> { "Concentration absolue", "Brûlure" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                            break;
                        case "Zed":
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Électrocuter";
                            styleRuneSet.PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" };
                            styleRuneSet.SecondaryTree = "Précision";
                            styleRuneSet.SecondaryRunes = new List<string> { "Triomphe", "Coup de grâce" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                            break;
                        default:
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Électrocuter";
                            styleRuneSet.PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" };
                            styleRuneSet.SecondaryTree = "Précision";
                            styleRuneSet.SecondaryRunes = new List<string> { "Triomphe", "Coup de grâce" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                            break;
                    }
                    break;
                
                case "PASSIVE":
                    styleRuneSet.Description = $"Configuration passive pour {championName}, favorisant le farm et la survie.";
                    
                    // Adapter les runes en fonction du champion
                    switch (championName)
                    {
                        case "Teemo":
                            styleRuneSet.PrimaryTree = "Sorcellerie";
                            styleRuneSet.PrimaryKeystone = "Invocation d'Aery";
                            styleRuneSet.PrimaryRunes = new List<string> { "Orbe du débit", "Transcendance", "Brûlure" };
                            styleRuneSet.SecondaryTree = "Résolution";
                            styleRuneSet.SecondaryRunes = new List<string> { "Conditionnement", "Revitalisation" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Armure", "Résistance magique" };
                            break;
                        case "Zed":
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Électrocuter";
                            styleRuneSet.PrimaryRunes = new List<string> { "Goût du sang", "Zombie Ward", "Chasseur de trésors" };
                            styleRuneSet.SecondaryTree = "Résolution";
                            styleRuneSet.SecondaryRunes = new List<string> { "Démolition", "Revitalisation" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Armure", "Résistance magique" };
                            break;
                        default:
                            styleRuneSet.PrimaryTree = "Résolution";
                            styleRuneSet.PrimaryKeystone = "Poigne de l'immortel";
                            styleRuneSet.PrimaryRunes = new List<string> { "Démolition", "Conditionnement", "Croissance excessive" };
                            styleRuneSet.SecondaryTree = "Inspiration";
                            styleRuneSet.SecondaryRunes = new List<string> { "Biscuits de livraison", "Perspicacité cosmique" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Armure", "Résistance magique" };
                            break;
                    }
                    break;
                
                case "ROAMING":
                    styleRuneSet.Description = $"Configuration roaming pour {championName}, favorisant la mobilité et les ganks.";
                    
                    // Adapter les runes en fonction du champion
                    switch (championName)
                    {
                        case "Zed":
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Prédateur";
                            styleRuneSet.PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" };
                            styleRuneSet.SecondaryTree = "Sorcellerie";
                            styleRuneSet.SecondaryRunes = new List<string> { "Célérité", "Marche sur l'eau" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                            break;
                        case "Shen":
                            styleRuneSet.PrimaryTree = "Résolution";
                            styleRuneSet.PrimaryKeystone = "Gardien";
                            styleRuneSet.PrimaryRunes = new List<string> { "Fontaine de vie", "Conditionnement", "Croissance excessive" };
                            styleRuneSet.SecondaryTree = "Inspiration";
                            styleRuneSet.SecondaryRunes = new List<string> { "Chaussures magiques", "Perspicacité cosmique" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Armure", "Résistance magique" };
                            break;
                        default:
                            styleRuneSet.PrimaryTree = "Domination";
                            styleRuneSet.PrimaryKeystone = "Prédateur";
                            styleRuneSet.PrimaryRunes = new List<string> { "Impact soudain", "Zombie Ward", "Chasseur de primes" };
                            styleRuneSet.SecondaryTree = "Sorcellerie";
                            styleRuneSet.SecondaryRunes = new List<string> { "Célérité", "Marche sur l'eau" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Attaque adaptative", "Armure" };
                            break;
                    }
                    break;
                
                case "SCALING":
                    styleRuneSet.Description = $"Configuration scaling pour {championName}, favorisant le jeu tardif.";
                    
                    // Adapter les runes en fonction du champion
                    switch (championName)
                    {
                        case "Jinx":
                            styleRuneSet.PrimaryTree = "Précision";
                            styleRuneSet.PrimaryKeystone = "Tempo mortel";
                            styleRuneSet.PrimaryRunes = new List<string> { "Triomphe", "Légende : Alacrité", "Baroud d'honneur" };
                            styleRuneSet.SecondaryTree = "Sorcellerie";
                            styleRuneSet.SecondaryRunes = new List<string> { "Concentration absolue", "Tempête grandissante" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse d'attaque", "Attaque adaptative", "Armure" };
                            break;
                        case "Yasuo":
                            styleRuneSet.PrimaryTree = "Précision";
                            styleRuneSet.PrimaryKeystone = "Conquérant";
                            styleRuneSet.PrimaryRunes = new List<string> { "Triomphe", "Légende : Alacrité", "Baroud d'honneur" };
                            styleRuneSet.SecondaryTree = "Résolution";
                            styleRuneSet.SecondaryRunes = new List<string> { "Conditionnement", "Croissance excessive" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse d'attaque", "Attaque adaptative", "Armure" };
                            break;
                        default:
                            styleRuneSet.PrimaryTree = "Sorcellerie";
                            styleRuneSet.PrimaryKeystone = "Phase Rush";
                            styleRuneSet.PrimaryRunes = new List<string> { "Orbe du débit", "Transcendance", "Tempête grandissante" };
                            styleRuneSet.SecondaryTree = "Inspiration";
                            styleRuneSet.SecondaryRunes = new List<string> { "Biscuits de livraison", "Perspicacité cosmique" };
                            styleRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                            break;
                    }
                    break;
                
                case "UTILITY":
                    styleRuneSet.Description = $"Configuration utilitaire pour {championName}, favorisant le soutien à l'équipe.";
                    
                    // Adapter les runes en fonction du champion
                    switch (championName)
                    {
                        case "Thresh":
                            styleRuneSet.PrimaryTree = "Résolution";
                            styleRuneSet.PrimaryKeystone = "Gardien";
                            styleRuneSet.PrimaryRunes = new List<string> { "Fontaine de vie", "Conditionnement", "Revitalisation" };
                            styleRuneSet.SecondaryTree = "Inspiration";
                            styleRuneSet.SecondaryRunes = new List<string> { "Chaussures magiques", "Perspicacité cosmique" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Armure", "Résistance magique" };
                            break;
                        case "Shen":
                            styleRuneSet.PrimaryTree = "Résolution";
                            styleRuneSet.PrimaryKeystone = "Gardien";
                            styleRuneSet.PrimaryRunes = new List<string> { "Fontaine de vie", "Conditionnement", "Revitalisation" };
                            styleRuneSet.SecondaryTree = "Inspiration";
                            styleRuneSet.SecondaryRunes = new List<string> { "Chaussures magiques", "Perspicacité cosmique" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Armure", "Résistance magique" };
                            break;
                        default:
                            styleRuneSet.PrimaryTree = "Inspiration";
                            styleRuneSet.PrimaryKeystone = "Glacial";
                            styleRuneSet.PrimaryRunes = new List<string> { "Chaussures magiques", "Biscuits de livraison", "Perspicacité cosmique" };
                            styleRuneSet.SecondaryTree = "Sorcellerie";
                            styleRuneSet.SecondaryRunes = new List<string> { "Transcendance", "Marche sur l'eau" };
                            styleRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Armure", "Résistance magique" };
                            break;
                    }
                    break;
                
                default:
                    // Utiliser les runes par défaut
                    styleRuneSet.PrimaryTree = baseRuneSet.PrimaryTree;
                    styleRuneSet.PrimaryKeystone = baseRuneSet.PrimaryKeystone;
                    styleRuneSet.PrimaryRunes = baseRuneSet.PrimaryRunes;
                    styleRuneSet.SecondaryTree = baseRuneSet.SecondaryTree;
                    styleRuneSet.SecondaryRunes = baseRuneSet.SecondaryRunes;
                    styleRuneSet.StatRunes = baseRuneSet.StatRunes;
                    styleRuneSet.Description = baseRuneSet.Description;
                    break;
            }
            
            return styleRuneSet;
        }

        /// <summary>
        /// Crée un ensemble de runes spécifique à un matchup pour un champion
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="enemyChampionName">Nom du champion ennemi</param>
        /// <returns>Ensemble de runes</returns>
        private RuneSet CreateMatchupSpecificRuneSet(string championName, string enemyChampionName)
        {
            // Dans une version complète, ces données seraient basées sur des statistiques réelles
            // Pour cette version, nous utilisons des données prédéfinies
            
            // Commencer avec les runes par défaut
            RuneSet baseRuneSet = CreateDefaultRuneSet(championName);
            RuneSet matchupRuneSet = new RuneSet
            {
                ChampionName = championName,
                MatchupChampionName = enemyChampionName,
                WinRate = baseRuneSet.WinRate,
                PickRate = baseRuneSet.PickRate / 3 // Moins populaire que la configuration par défaut
            };
            
            // Quelques exemples de matchups spécifiques
            if (championName == "Teemo" && enemyChampionName == "Darius")
            {
                matchupRuneSet.PrimaryTree = "Domination";
                matchupRuneSet.PrimaryKeystone = "Phase Rush";
                matchupRuneSet.PrimaryRunes = new List<string> { "Orbe du débit", "Célérité", "Brûlure" };
                matchupRuneSet.SecondaryTree = "Résolution";
                matchupRuneSet.SecondaryRunes = new List<string> { "Ossature", "Revitalisation" };
                matchupRuneSet.StatRunes = new List<string> { "Vitesse de déplacement", "Attaque adaptative", "Armure" };
                matchupRuneSet.Description = "Configuration pour contrer Darius, maximisant la mobilité pour éviter ses attaques.";
                matchupRuneSet.WinRate = 54.2;
            }
            else if (championName == "Zed" && enemyChampionName == "Syndra")
            {
                matchupRuneSet.PrimaryTree = "Domination";
                matchupRuneSet.PrimaryKeystone = "Électrocuter";
                matchupRuneSet.PrimaryRunes = new List<string> { "Goût du sang", "Zombie Ward", "Chasseur de primes" };
                matchupRuneSet.SecondaryTree = "Résolution";
                matchupRuneSet.SecondaryRunes = new List<string> { "Ossature", "Revitalisation" };
                matchupRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Résistance magique" };
                matchupRuneSet.Description = "Configuration pour contrer Syndra, offrant plus de résistance magique et de sustain.";
                matchupRuneSet.WinRate = 53.5;
            }
            else if (championName == "Yasuo" && enemyChampionName == "Zed")
            {
                matchupRuneSet.PrimaryTree = "Résolution";
                matchupRuneSet.PrimaryKeystone = "Poigne de l'immortel";
                matchupRuneSet.PrimaryRunes = new List<string> { "Démolition", "Ossature", "Revitalisation" };
                matchupRuneSet.SecondaryTree = "Précision";
                matchupRuneSet.SecondaryRunes = new List<string> { "Triomphe", "Légende : Ténacité" };
                matchupRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                matchupRuneSet.Description = "Configuration pour contrer Zed, maximisant la survie en early game.";
                matchupRuneSet.WinRate = 52.8;
            }
            else if (championName == "Lux" && enemyChampionName == "Zed")
            {
                matchupRuneSet.PrimaryTree = "Inspiration";
                matchupRuneSet.PrimaryKeystone = "Glacial";
                matchupRuneSet.PrimaryRunes = new List<string> { "Chaussures magiques", "Biscuits de livraison", "Perspicacité cosmique" };
                matchupRuneSet.SecondaryTree = "Résolution";
                matchupRuneSet.SecondaryRunes = new List<string> { "Conditionnement", "Revitalisation" };
                matchupRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Armure", "Armure" };
                matchupRuneSet.Description = "Configuration pour contrer Zed, maximisant la survie et le contrôle.";
                matchupRuneSet.WinRate = 51.2;
            }
            else
            {
                // Utiliser les runes par défaut avec une légère adaptation
                matchupRuneSet.PrimaryTree = baseRuneSet.PrimaryTree;
                matchupRuneSet.PrimaryKeystone = baseRuneSet.PrimaryKeystone;
                matchupRuneSet.PrimaryRunes = baseRuneSet.PrimaryRunes;
                matchupRuneSet.SecondaryTree = baseRuneSet.SecondaryTree;
                matchupRuneSet.SecondaryRunes = baseRuneSet.SecondaryRunes;
                
                // Adapter les runes de statistiques en fonction du type de dégâts de l'ennemi
                if (IsMagicDamageDealer(enemyChampionName))
                {
                    matchupRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Résistance magique" };
                }
                else
                {
                    matchupRuneSet.StatRunes = new List<string> { "Attaque adaptative", "Attaque adaptative", "Armure" };
                }
                
                matchupRuneSet.Description = $"Configuration adaptée pour affronter {enemyChampionName}.";
            }
            
            return matchupRuneSet;
        }

        /// <summary>
        /// Obtient les recommandations de runes pour un champion
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <param name="enemyChampionName">Nom du champion ennemi (optionnel)</param>
        /// <param name="playStyleId">ID du style de jeu (optionnel)</param>
        /// <returns>Liste des ensembles de runes recommandés</returns>
        public async Task<List<RuneSet>> GetRuneRecommendationsAsync(string championName, string enemyChampionName = null, string playStyleId = null)
        {
            try
            {
                _logger.Info($"Obtention des recommandations de runes pour {championName} contre {enemyChampionName ?? "DEFAULT"} avec style {playStyleId ?? "DEFAULT"}");
                
                List<RuneSet> recommendations = new List<RuneSet>();
                
                // Vérifier si le champion existe dans le cache
                if (!_runeRecommendationsCache.ContainsKey(championName))
                {
                    _logger.Warning($"Aucune donnée de runes pour {championName}, utilisation de données génériques");
                    
                    // Créer des données génériques pour ce champion
                    _runeRecommendationsCache[championName] = new Dictionary<string, RuneSet>
                    {
                        { "DEFAULT", CreateDefaultRuneSet(championName) }
                    };
                }
                
                // Ajouter la recommandation spécifique au matchup si disponible
                string matchupKey = $"vs_{enemyChampionName}";
                if (!string.IsNullOrEmpty(enemyChampionName) && _runeRecommendationsCache[championName].ContainsKey(matchupKey))
                {
                    recommendations.Add(_runeRecommendationsCache[championName][matchupKey]);
                }
                
                // Ajouter la recommandation spécifique au style de jeu si disponible
                if (!string.IsNullOrEmpty(playStyleId) && _runeRecommendationsCache[championName].ContainsKey(playStyleId))
                {
                    recommendations.Add(_runeRecommendationsCache[championName][playStyleId]);
                }
                
                // Ajouter la recommandation par défaut
                recommendations.Add(_runeRecommendationsCache[championName]["DEFAULT"]);
                
                // Trier les recommandations par taux de victoire
                recommendations = recommendations.OrderByDescending(r => r.WinRate).ToList();
                
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, $"Erreur lors de l'obtention des recommandations de runes pour {championName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obtient les styles de jeu disponibles
        /// </summary>
        /// <returns>Liste des styles de jeu</returns>
        public List<PlayStyle> GetPlayStyles()
        {
            return _playStyles;
        }

        /// <summary>
        /// Vérifie si un champion inflige principalement des dégâts magiques
        /// </summary>
        /// <param name="championName">Nom du champion</param>
        /// <returns>True si le champion inflige principalement des dégâts magiques</returns>
        private bool IsMagicDamageDealer(string championName)
        {
            // Liste de champions qui infligent principalement des dégâts magiques
            List<string> magicDamageChampions = new List<string>
            {
                "Ahri", "Annie", "Anivia", "Aurelion Sol", "Azir", "Brand", "Cassiopeia", "Fiddlesticks",
                "Heimerdinger", "Karma", "Karthus", "Kassadin", "Katarina", "LeBlanc", "Lissandra", "Lux",
                "Malzahar", "Morgana", "Neeko", "Orianna", "Ryze", "Syndra", "Taliyah", "Twisted Fate",
                "Veigar", "Vel'Koz", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zilean", "Zoe", "Zyra",
                "Amumu", "Cho'Gath", "Diana", "Ekko", "Elise", "Evelynn", "Gragas", "Lillia", "Maokai",
                "Mordekaiser", "Rumble", "Sejuani", "Shyvana", "Singed", "Sylas", "Teemo", "Volibear"
            };
            
            return magicDamageChampions.Contains(championName);
        }
    }

    /// <summary>
    /// Ensemble de runes
    /// </summary>
    public class RuneSet
    {
        /// <summary>
        /// Nom du champion
        /// </summary>
        public string ChampionName { get; set; }
        
        /// <summary>
        /// Nom du champion ennemi (matchup)
        /// </summary>
        public string MatchupChampionName { get; set; }
        
        /// <summary>
        /// ID du style de jeu
        /// </summary>
        public string StyleId { get; set; }
        
        /// <summary>
        /// Arbre de runes principal
        /// </summary>
        public string PrimaryTree { get; set; }
        
        /// <summary>
        /// Rune clé de l'arbre principal
        /// </summary>
        public string PrimaryKeystone { get; set; }
        
        /// <summary>
        /// Runes de l'arbre principal
        /// </summary>
        public List<string> PrimaryRunes { get; set; }
        
        /// <summary>
        /// Arbre de runes secondaire
        /// </summary>
        public string SecondaryTree { get; set; }
        
        /// <summary>
        /// Runes de l'arbre secondaire
        /// </summary>
        public List<string> SecondaryRunes { get; set; }
        
        /// <summary>
        /// Runes de statistiques
        /// </summary>
        public List<string> StatRunes { get; set; }
        
        /// <summary>
        /// Taux de victoire avec cet ensemble de runes
        /// </summary>
        public double WinRate { get; set; }
        
        /// <summary>
        /// Taux de sélection de cet ensemble de runes
        /// </summary>
        public double PickRate { get; set; }
        
        /// <summary>
        /// Description de l'ensemble de runes
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Style de jeu
    /// </summary>
    public class PlayStyle
    {
        /// <summary>
        /// ID du style de jeu
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Nom du style de jeu
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description du style de jeu
        /// </summary>
        public string Description { get; set; }
    }
}
