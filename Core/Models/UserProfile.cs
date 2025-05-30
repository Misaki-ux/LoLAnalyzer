using System;
using System.Collections.Generic;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant un profil utilisateur dans l'application LoL Analyzer
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Identifiant unique du profil utilisateur
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom d'invocateur de l'utilisateur
        /// </summary>
        public string SummonerName { get; set; }

        /// <summary>
        /// Identifiant d'invocateur (fourni par l'API Riot)
        /// </summary>
        public string SummonerId { get; set; }

        /// <summary>
        /// PUUID de l'invocateur (identifiant unique persistant fourni par l'API Riot)
        /// </summary>
        public string Puuid { get; set; }

        /// <summary>
        /// Région du joueur (EUW, NA, KR, etc.)
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Niveau d'invocateur
        /// </summary>
        public int SummonerLevel { get; set; }

        /// <summary>
        /// URL de l'icône de profil
        /// </summary>
        public string ProfileIconUrl { get; set; }

        /// <summary>
        /// Rang en solo/duo queue (IRON, BRONZE, SILVER, etc.)
        /// </summary>
        public string SoloQueueRank { get; set; }

        /// <summary>
        /// Division en solo/duo queue (I, II, III, IV)
        /// </summary>
        public string SoloQueueDivision { get; set; }

        /// <summary>
        /// Points de ligue en solo/duo queue
        /// </summary>
        public int SoloQueueLP { get; set; }

        /// <summary>
        /// Rang en flex queue (IRON, BRONZE, SILVER, etc.)
        /// </summary>
        public string FlexQueueRank { get; set; }

        /// <summary>
        /// Division en flex queue (I, II, III, IV)
        /// </summary>
        public string FlexQueueDivision { get; set; }

        /// <summary>
        /// Points de ligue en flex queue
        /// </summary>
        public int FlexQueueLP { get; set; }

        /// <summary>
        /// Rôle principal du joueur (TOP, JUNGLE, MID, ADC, SUPPORT)
        /// </summary>
        public string MainRole { get; set; }

        /// <summary>
        /// Rôle secondaire du joueur (TOP, JUNGLE, MID, ADC, SUPPORT)
        /// </summary>
        public string SecondaryRole { get; set; }

        /// <summary>
        /// Liste des champions préférés (IDs)
        /// </summary>
        public List<int> PreferredChampions { get; set; }

        /// <summary>
        /// Liste des champions les plus joués
        /// </summary>
        public List<ChampionPlayStats> MostPlayedChampions { get; set; }

        /// <summary>
        /// Style de jeu du joueur (Agressif, Passif, Équilibré, etc.)
        /// </summary>
        public string PlayStyle { get; set; }

        /// <summary>
        /// Préférences d'interface utilisateur
        /// </summary>
        public UserPreferences Preferences { get; set; }

        /// <summary>
        /// Date de création du profil
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date de dernière mise à jour du profil
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public UserProfile()
        {
            PreferredChampions = new List<int>();
            MostPlayedChampions = new List<ChampionPlayStats>();
            Preferences = new UserPreferences();
            CreatedAt = DateTime.Now;
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public UserProfile(string summonerName, string region)
        {
            SummonerName = summonerName;
            Region = region;
            PreferredChampions = new List<int>();
            MostPlayedChampions = new List<ChampionPlayStats>();
            Preferences = new UserPreferences();
            CreatedAt = DateTime.Now;
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Ajoute un champion aux champions préférés
        /// </summary>
        public void AddPreferredChampion(int championId)
        {
            if (!PreferredChampions.Contains(championId))
            {
                PreferredChampions.Add(championId);
            }
        }

        /// <summary>
        /// Retire un champion des champions préférés
        /// </summary>
        public void RemovePreferredChampion(int championId)
        {
            if (PreferredChampions.Contains(championId))
            {
                PreferredChampions.Remove(championId);
            }
        }

        /// <summary>
        /// Détermine si le profil est complet (avec toutes les informations essentielles)
        /// </summary>
        public bool IsComplete()
        {
            return !string.IsNullOrEmpty(SummonerName) && 
                   !string.IsNullOrEmpty(Region) && 
                   !string.IsNullOrEmpty(SummonerId) && 
                   !string.IsNullOrEmpty(Puuid);
        }

        /// <summary>
        /// Retourne une représentation textuelle du profil utilisateur
        /// </summary>
        public override string ToString()
        {
            return $"{SummonerName} ({Region}) - {SoloQueueRank} {SoloQueueDivision}";
        }
    }

    /// <summary>
    /// Statistiques de jeu d'un champion pour un utilisateur
    /// </summary>
    public class ChampionPlayStats
    {
        /// <summary>
        /// Identifiant du champion
        /// </summary>
        public int ChampionId { get; set; }

        /// <summary>
        /// Nom du champion
        /// </summary>
        public string ChampionName { get; set; }

        /// <summary>
        /// URL de l'image du champion
        /// </summary>
        public string ChampionImageUrl { get; set; }

        /// <summary>
        /// Nombre de parties jouées avec ce champion
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Nombre de victoires avec ce champion
        /// </summary>
        public int Wins { get; set; }

        /// <summary>
        /// Nombre de défaites avec ce champion
        /// </summary>
        public int Losses { get; set; }

        /// <summary>
        /// KDA moyen avec ce champion
        /// </summary>
        public double AverageKDA { get; set; }

        /// <summary>
        /// CS moyen par minute avec ce champion
        /// </summary>
        public double AverageCSPerMinute { get; set; }

        /// <summary>
        /// Niveau de maîtrise du champion (1-7)
        /// </summary>
        public int MasteryLevel { get; set; }

        /// <summary>
        /// Points de maîtrise du champion
        /// </summary>
        public int MasteryPoints { get; set; }

        /// <summary>
        /// Rôle le plus joué avec ce champion
        /// </summary>
        public string MostPlayedRole { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public ChampionPlayStats()
        {
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public ChampionPlayStats(int championId, string championName, string championImageUrl)
        {
            ChampionId = championId;
            ChampionName = championName;
            ChampionImageUrl = championImageUrl;
        }

        /// <summary>
        /// Calcule le taux de victoire avec ce champion
        /// </summary>
        public double CalculateWinRate()
        {
            if (GamesPlayed == 0)
                return 0;
            
            return Math.Round((double)Wins / GamesPlayed * 100, 2);
        }

        /// <summary>
        /// Retourne une représentation textuelle des statistiques du champion
        /// </summary>
        public override string ToString()
        {
            return $"{ChampionName} - {Wins}W {Losses}L ({CalculateWinRate()}%)";
        }
    }

    /// <summary>
    /// Préférences d'interface utilisateur
    /// </summary>
    public class UserPreferences
    {
        /// <summary>
        /// Thème de l'interface (Light, Dark, League)
        /// </summary>
        public string Theme { get; set; }

        /// <summary>
        /// Niveau d'opacité de l'overlay (0.0 - 1.0)
        /// </summary>
        public double OverlayOpacity { get; set; }

        /// <summary>
        /// Position X de l'overlay
        /// </summary>
        public int OverlayPositionX { get; set; }

        /// <summary>
        /// Position Y de l'overlay
        /// </summary>
        public int OverlayPositionY { get; set; }

        /// <summary>
        /// Largeur de l'overlay
        /// </summary>
        public int OverlayWidth { get; set; }

        /// <summary>
        /// Hauteur de l'overlay
        /// </summary>
        public int OverlayHeight { get; set; }

        /// <summary>
        /// Indique si l'overlay doit être affiché automatiquement
        /// </summary>
        public bool AutoShowOverlay { get; set; }

        /// <summary>
        /// Indique si l'overlay doit être masqué automatiquement
        /// </summary>
        public bool AutoHideOverlay { get; set; }

        /// <summary>
        /// Indique si le mode compact doit être utilisé
        /// </summary>
        public bool UseCompactMode { get; set; }

        /// <summary>
        /// Indique si les notifications sonores sont activées
        /// </summary>
        public bool EnableSoundNotifications { get; set; }

        /// <summary>
        /// Constructeur par défaut avec valeurs par défaut
        /// </summary>
        public UserPreferences()
        {
            Theme = "Dark";
            OverlayOpacity = 0.9;
            OverlayPositionX = 0;
            OverlayPositionY = 0;
            OverlayWidth = 400;
            OverlayHeight = 600;
            AutoShowOverlay = true;
            AutoHideOverlay = false;
            UseCompactMode = false;
            EnableSoundNotifications = true;
        }
    }
}
