using System;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant un membre d'une équipe dans League of Legends
    /// </summary>
    public class TeamMember
    {
        /// <summary>
        /// Identifiant unique du membre d'équipe
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifiant du champion sélectionné
        /// </summary>
        public int ChampionId { get; set; }

        /// <summary>
        /// Nom du champion sélectionné
        /// </summary>
        public string ChampionName { get; set; }

        /// <summary>
        /// URL de l'image du champion
        /// </summary>
        public string ChampionImageUrl { get; set; }

        /// <summary>
        /// Rôle du joueur (TOP, JUNGLE, MID, ADC, SUPPORT)
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Nom d'invocateur du joueur (optionnel)
        /// </summary>
        public string SummonerName { get; set; }

        /// <summary>
        /// Identifiant d'invocateur du joueur (optionnel)
        /// </summary>
        public string SummonerId { get; set; }

        /// <summary>
        /// Rang du joueur (IRON, BRONZE, SILVER, GOLD, etc.) (optionnel)
        /// </summary>
        public string Rank { get; set; }

        /// <summary>
        /// Niveau de maîtrise du champion (1-7) (optionnel)
        /// </summary>
        public int ChampionMastery { get; set; }

        /// <summary>
        /// Points de maîtrise du champion (optionnel)
        /// </summary>
        public int ChampionMasteryPoints { get; set; }

        /// <summary>
        /// Taux de victoire du joueur avec ce champion (optionnel)
        /// </summary>
        public double WinRate { get; set; }

        /// <summary>
        /// Nombre de parties jouées avec ce champion (optionnel)
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Score de performance estimé pour ce joueur avec ce champion (échelle 0-10) (optionnel)
        /// </summary>
        public double PerformanceScore { get; set; }

        /// <summary>
        /// Indique si ce pick est considéré comme "troll" ou non-méta
        /// </summary>
        public bool IsTrollPick { get; set; }

        /// <summary>
        /// Raison pour laquelle ce pick est considéré comme "troll" (si applicable)
        /// </summary>
        public string TrollPickReason { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public TeamMember()
        {
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public TeamMember(string championName, string role)
        {
            ChampionName = championName;
            Role = role;
        }

        /// <summary>
        /// Constructeur complet
        /// </summary>
        public TeamMember(int championId, string championName, string championImageUrl, string role)
        {
            ChampionId = championId;
            ChampionName = championName;
            ChampionImageUrl = championImageUrl;
            Role = role;
        }

        /// <summary>
        /// Détermine si ce membre est un joueur réel (avec des informations d'invocateur)
        /// </summary>
        public bool IsRealPlayer()
        {
            return !string.IsNullOrEmpty(SummonerName) && !string.IsNullOrEmpty(SummonerId);
        }

        /// <summary>
        /// Détermine si ce joueur est expérimenté avec ce champion
        /// </summary>
        public bool IsExperiencedWithChampion()
        {
            return ChampionMastery >= 5 || GamesPlayed >= 20;
        }

        /// <summary>
        /// Retourne une représentation textuelle du membre d'équipe
        /// </summary>
        public override string ToString()
        {
            return $"{ChampionName} ({Role})" + (IsRealPlayer() ? $" - {SummonerName}" : "");
        }
    }
}
