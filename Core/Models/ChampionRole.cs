using System;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant un rôle de champion dans League of Legends
    /// </summary>
    public class ChampionRole
    {
        /// <summary>
        /// Identifiant unique du rôle de champion
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identifiant du champion associé
        /// </summary>
        public int ChampionId { get; set; }

        /// <summary>
        /// Nom du champion (pour faciliter l'affichage)
        /// </summary>
        public string ChampionName { get; set; }

        /// <summary>
        /// Rôle du champion (TOP, JUNGLE, MID, ADC, SUPPORT)
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Taux de victoire dans ce rôle
        /// </summary>
        public double WinRate { get; set; }

        /// <summary>
        /// Taux de sélection dans ce rôle
        /// </summary>
        public double PickRate { get; set; }

        /// <summary>
        /// Taux de bannissement dans ce rôle
        /// </summary>
        public double BanRate { get; set; }

        /// <summary>
        /// Viabilité du champion dans ce rôle (échelle 0-10)
        /// </summary>
        public double Viability { get; set; }

        /// <summary>
        /// Tier du champion dans ce rôle (S, A, B, C, D)
        /// </summary>
        public string Tier { get; set; }

        /// <summary>
        /// Rang du champion dans ce rôle (1, 2, 3, etc.)
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Nombre de parties analysées pour ces statistiques
        /// </summary>
        public int GamesAnalyzed { get; set; }

        /// <summary>
        /// Date de dernière mise à jour des données
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public ChampionRole()
        {
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public ChampionRole(int championId, string championName, string role)
        {
            ChampionId = championId;
            ChampionName = championName;
            Role = role;
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Calcule le score de méta pour ce champion dans ce rôle
        /// </summary>
        public double CalculateMetaScore()
        {
            // Formule pondérée pour déterminer la force du champion dans la méta actuelle
            return (WinRate * 0.4) + (PickRate * 0.3) + (BanRate * 0.3);
        }

        /// <summary>
        /// Détermine si ce champion dans ce rôle est considéré comme "méta"
        /// </summary>
        public bool IsMetaPick()
        {
            return CalculateMetaScore() >= 0.52; // Seuil arbitraire pour définir un pick "méta"
        }

        /// <summary>
        /// Détermine si ce champion dans ce rôle est considéré comme "troll" ou non-méta
        /// </summary>
        public bool IsTrollPick()
        {
            return Viability < 3.0 || CalculateMetaScore() < 0.45;
        }

        /// <summary>
        /// Retourne une représentation textuelle du rôle de champion
        /// </summary>
        public override string ToString()
        {
            return $"{ChampionName} - {Role} (WR: {WinRate:P2}, PR: {PickRate:P2})";
        }
    }
}
