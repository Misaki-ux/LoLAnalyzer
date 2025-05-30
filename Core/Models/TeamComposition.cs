using System;
using System.Collections.Generic;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant une composition d'équipe dans League of Legends
    /// </summary>
    public class TeamComposition
    {
        /// <summary>
        /// Identifiant unique de la composition
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom de l'équipe (optionnel)
        /// </summary>
        public string TeamName { get; set; }

        /// <summary>
        /// Côté de l'équipe (blue ou red)
        /// </summary>
        public string Side { get; set; }

        /// <summary>
        /// Liste des membres de l'équipe
        /// </summary>
        public List<TeamMember> Members { get; set; }

        /// <summary>
        /// Pourcentage de dégâts physiques de la composition
        /// </summary>
        public double PhysicalDamagePercentage { get; set; }

        /// <summary>
        /// Pourcentage de dégâts magiques de la composition
        /// </summary>
        public double MagicalDamagePercentage { get; set; }

        /// <summary>
        /// Pourcentage de dégâts vrais de la composition
        /// </summary>
        public double TrueDamagePercentage { get; set; }

        /// <summary>
        /// Score de tankiness global de la composition (échelle 0-10)
        /// </summary>
        public double TankinessScore { get; set; }

        /// <summary>
        /// Score de mobilité global de la composition (échelle 0-10)
        /// </summary>
        public double MobilityScore { get; set; }

        /// <summary>
        /// Score de contrôle de foule global de la composition (échelle 0-10)
        /// </summary>
        public double CCScore { get; set; }

        /// <summary>
        /// Score d'early game de la composition (échelle 0-10)
        /// </summary>
        public double EarlyGameScore { get; set; }

        /// <summary>
        /// Score de mid game de la composition (échelle 0-10)
        /// </summary>
        public double MidGameScore { get; set; }

        /// <summary>
        /// Score de late game de la composition (échelle 0-10)
        /// </summary>
        public double LateGameScore { get; set; }

        /// <summary>
        /// Score de synergie entre les champions de la composition (échelle 0-10)
        /// </summary>
        public double SynergyScore { get; set; }

        /// <summary>
        /// Forces de la composition (liste de descriptions textuelles)
        /// </summary>
        public List<string> Strengths { get; set; }

        /// <summary>
        /// Faiblesses de la composition (liste de descriptions textuelles)
        /// </summary>
        public List<string> Weaknesses { get; set; }

        /// <summary>
        /// Date de création/analyse de la composition
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public TeamComposition()
        {
            Members = new List<TeamMember>();
            Strengths = new List<string>();
            Weaknesses = new List<string>();
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public TeamComposition(string teamName, string side)
        {
            TeamName = teamName;
            Side = side;
            Members = new List<TeamMember>();
            Strengths = new List<string>();
            Weaknesses = new List<string>();
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Ajoute un membre à la composition d'équipe
        /// </summary>
        public void AddMember(TeamMember member)
        {
            if (Members.Count < 5)
            {
                Members.Add(member);
            }
            else
            {
                throw new InvalidOperationException("Une équipe ne peut pas avoir plus de 5 membres.");
            }
        }

        /// <summary>
        /// Vérifie si la composition est complète (5 membres)
        /// </summary>
        public bool IsComplete()
        {
            return Members.Count == 5;
        }

        /// <summary>
        /// Vérifie si la composition a tous les rôles requis
        /// </summary>
        public bool HasAllRoles()
        {
            bool hasTop = false;
            bool hasJungle = false;
            bool hasMid = false;
            bool hasAdc = false;
            bool hasSupport = false;

            foreach (var member in Members)
            {
                switch (member.Role.ToUpper())
                {
                    case "TOP":
                        hasTop = true;
                        break;
                    case "JUNGLE":
                        hasJungle = true;
                        break;
                    case "MID":
                        hasMid = true;
                        break;
                    case "ADC":
                        hasAdc = true;
                        break;
                    case "SUPPORT":
                        hasSupport = true;
                        break;
                }
            }

            return hasTop && hasJungle && hasMid && hasAdc && hasSupport;
        }

        /// <summary>
        /// Calcule le score global de la composition
        /// </summary>
        public double CalculateOverallScore()
        {
            // Formule pondérée pour déterminer la force globale de la composition
            return (TankinessScore * 0.15) + 
                   (CCScore * 0.15) + 
                   (MobilityScore * 0.1) + 
                   (SynergyScore * 0.3) + 
                   ((EarlyGameScore + MidGameScore + LateGameScore) / 3 * 0.3);
        }

        /// <summary>
        /// Retourne une représentation textuelle de la composition
        /// </summary>
        public override string ToString()
        {
            return $"{TeamName} ({Side}) - Score: {CalculateOverallScore():F2}";
        }
    }
}
