using System;
using System.Collections.Generic;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant un match de League of Legends
    /// </summary>
    public class Match
    {
        /// <summary>
        /// Identifiant unique du match (fourni par l'API Riot)
        /// </summary>
        public string MatchId { get; set; }

        /// <summary>
        /// Plateforme sur laquelle le match a été joué (EUW1, NA1, etc.)
        /// </summary>
        public string PlatformId { get; set; }

        /// <summary>
        /// Date et heure de début du match
        /// </summary>
        public DateTime GameStartTime { get; set; }

        /// <summary>
        /// Durée du match en secondes
        /// </summary>
        public int GameDuration { get; set; }

        /// <summary>
        /// Version du jeu (patch)
        /// </summary>
        public string GameVersion { get; set; }

        /// <summary>
        /// Mode de jeu (CLASSIC, ARAM, etc.)
        /// </summary>
        public string GameMode { get; set; }

        /// <summary>
        /// Type de match (RANKED_SOLO_5x5, NORMAL_DRAFT, etc.)
        /// </summary>
        public string GameType { get; set; }

        /// <summary>
        /// Composition de l'équipe bleue
        /// </summary>
        public TeamComposition BlueTeam { get; set; }

        /// <summary>
        /// Composition de l'équipe rouge
        /// </summary>
        public TeamComposition RedTeam { get; set; }

        /// <summary>
        /// Identifiant de l'équipe gagnante (100 pour bleue, 200 pour rouge)
        /// </summary>
        public int WinningTeamId { get; set; }

        /// <summary>
        /// Liste des bannissements de l'équipe bleue
        /// </summary>
        public List<int> BlueBans { get; set; }

        /// <summary>
        /// Liste des bannissements de l'équipe rouge
        /// </summary>
        public List<int> RedBans { get; set; }

        /// <summary>
        /// Statistiques détaillées du match (optionnel)
        /// </summary>
        public MatchStats MatchStats { get; set; }

        /// <summary>
        /// Date de récupération/analyse du match
        /// </summary>
        public DateTime AnalyzedAt { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public Match()
        {
            BlueBans = new List<int>();
            RedBans = new List<int>();
            BlueTeam = new TeamComposition("Blue Team", "blue");
            RedTeam = new TeamComposition("Red Team", "red");
            AnalyzedAt = DateTime.Now;
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public Match(string matchId, string platformId, DateTime gameStartTime, int gameDuration)
        {
            MatchId = matchId;
            PlatformId = platformId;
            GameStartTime = gameStartTime;
            GameDuration = gameDuration;
            BlueBans = new List<int>();
            RedBans = new List<int>();
            BlueTeam = new TeamComposition("Blue Team", "blue");
            RedTeam = new TeamComposition("Red Team", "red");
            AnalyzedAt = DateTime.Now;
        }

        /// <summary>
        /// Détermine si l'équipe bleue a gagné
        /// </summary>
        public bool BlueTeamWon()
        {
            return WinningTeamId == 100;
        }

        /// <summary>
        /// Détermine si l'équipe rouge a gagné
        /// </summary>
        public bool RedTeamWon()
        {
            return WinningTeamId == 200;
        }

        /// <summary>
        /// Calcule la durée du match au format mm:ss
        /// </summary>
        public string GetFormattedDuration()
        {
            TimeSpan time = TimeSpan.FromSeconds(GameDuration);
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        }

        /// <summary>
        /// Retourne une représentation textuelle du match
        /// </summary>
        public override string ToString()
        {
            string winner = BlueTeamWon() ? "Blue Team" : "Red Team";
            return $"Match {MatchId} - {GameMode} - {GetFormattedDuration()} - Winner: {winner}";
        }
    }

    /// <summary>
    /// Statistiques détaillées d'un match
    /// </summary>
    public class MatchStats
    {
        /// <summary>
        /// Nombre total de kills de l'équipe bleue
        /// </summary>
        public int BlueTeamKills { get; set; }

        /// <summary>
        /// Nombre total de morts de l'équipe bleue
        /// </summary>
        public int BlueTeamDeaths { get; set; }

        /// <summary>
        /// Nombre total d'assists de l'équipe bleue
        /// </summary>
        public int BlueTeamAssists { get; set; }

        /// <summary>
        /// Nombre de tours détruites par l'équipe bleue
        /// </summary>
        public int BlueTeamTowers { get; set; }

        /// <summary>
        /// Nombre de dragons tués par l'équipe bleue
        /// </summary>
        public int BlueTeamDragons { get; set; }

        /// <summary>
        /// Nombre de barons tués par l'équipe bleue
        /// </summary>
        public int BlueTeamBarons { get; set; }

        /// <summary>
        /// Or total collecté par l'équipe bleue
        /// </summary>
        public int BlueTeamGold { get; set; }

        /// <summary>
        /// Nombre total de kills de l'équipe rouge
        /// </summary>
        public int RedTeamKills { get; set; }

        /// <summary>
        /// Nombre total de morts de l'équipe rouge
        /// </summary>
        public int RedTeamDeaths { get; set; }

        /// <summary>
        /// Nombre total d'assists de l'équipe rouge
        /// </summary>
        public int RedTeamAssists { get; set; }

        /// <summary>
        /// Nombre de tours détruites par l'équipe rouge
        /// </summary>
        public int RedTeamTowers { get; set; }

        /// <summary>
        /// Nombre de dragons tués par l'équipe rouge
        /// </summary>
        public int RedTeamDragons { get; set; }

        /// <summary>
        /// Nombre de barons tués par l'équipe rouge
        /// </summary>
        public int RedTeamBarons { get; set; }

        /// <summary>
        /// Or total collecté par l'équipe rouge
        /// </summary>
        public int RedTeamGold { get; set; }

        /// <summary>
        /// Statistiques détaillées des joueurs
        /// </summary>
        public List<PlayerStats> PlayerStats { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public MatchStats()
        {
            PlayerStats = new List<PlayerStats>();
        }

        /// <summary>
        /// Calcule le KDA de l'équipe bleue
        /// </summary>
        public double CalculateBlueTeamKDA()
        {
            if (BlueTeamDeaths == 0)
                return (BlueTeamKills + BlueTeamAssists);
            
            return Math.Round((BlueTeamKills + BlueTeamAssists) / (double)BlueTeamDeaths, 2);
        }

        /// <summary>
        /// Calcule le KDA de l'équipe rouge
        /// </summary>
        public double CalculateRedTeamKDA()
        {
            if (RedTeamDeaths == 0)
                return (RedTeamKills + RedTeamAssists);
            
            return Math.Round((RedTeamKills + RedTeamAssists) / (double)RedTeamDeaths, 2);
        }
    }

    /// <summary>
    /// Statistiques d'un joueur dans un match
    /// </summary>
    public class PlayerStats
    {
        /// <summary>
        /// Identifiant du participant
        /// </summary>
        public int ParticipantId { get; set; }

        /// <summary>
        /// Identifiant de l'équipe (100 pour bleue, 200 pour rouge)
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// Identifiant du champion joué
        /// </summary>
        public int ChampionId { get; set; }

        /// <summary>
        /// Nom du champion joué
        /// </summary>
        public string ChampionName { get; set; }

        /// <summary>
        /// Rôle du joueur
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Nom d'invocateur du joueur
        /// </summary>
        public string SummonerName { get; set; }

        /// <summary>
        /// Identifiant d'invocateur du joueur
        /// </summary>
        public string SummonerId { get; set; }

        /// <summary>
        /// Nombre de kills
        /// </summary>
        public int Kills { get; set; }

        /// <summary>
        /// Nombre de morts
        /// </summary>
        public int Deaths { get; set; }

        /// <summary>
        /// Nombre d'assists
        /// </summary>
        public int Assists { get; set; }

        /// <summary>
        /// Nombre de minions tués
        /// </summary>
        public int CS { get; set; }

        /// <summary>
        /// Vision score
        /// </summary>
        public int VisionScore { get; set; }

        /// <summary>
        /// Or collecté
        /// </summary>
        public int Gold { get; set; }

        /// <summary>
        /// Dégâts infligés aux champions
        /// </summary>
        public int DamageDealt { get; set; }

        /// <summary>
        /// Dégâts subis
        /// </summary>
        public int DamageTaken { get; set; }

        /// <summary>
        /// Liste des objets achetés (IDs)
        /// </summary>
        public List<int> Items { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public PlayerStats()
        {
            Items = new List<int>();
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public PlayerStats(int participantId, int teamId, int championId, string championName, string role, string summonerName)
        {
            ParticipantId = participantId;
            TeamId = teamId;
            ChampionId = championId;
            ChampionName = championName;
            Role = role;
            SummonerName = summonerName;
            Items = new List<int>();
        }

        /// <summary>
        /// Calcule le KDA du joueur
        /// </summary>
        public double CalculateKDA()
        {
            if (Deaths == 0)
                return (Kills + Assists);
            
            return Math.Round((Kills + Assists) / (double)Deaths, 2);
        }

        /// <summary>
        /// Retourne une représentation textuelle des statistiques du joueur
        /// </summary>
        public override string ToString()
        {
            return $"{SummonerName} ({ChampionName}) - {Kills}/{Deaths}/{Assists} - CS: {CS}";
        }
    }
}
