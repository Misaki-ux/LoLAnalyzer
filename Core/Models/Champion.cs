using System;
using System.Collections.Generic;

namespace LoLAnalyzer.Core.Models
{
    /// <summary>
    /// Modèle représentant un champion de League of Legends
    /// </summary>
    public class Champion
    {
        /// <summary>
        /// Identifiant unique du champion
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom du champion
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Titre du champion
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// URL de l'image du champion
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// URL de l'image splash du champion
        /// </summary>
        public string SplashUrl { get; set; }

        /// <summary>
        /// Niveau de dégâts physiques (échelle 0-10)
        /// </summary>
        public double PhysicalDamage { get; set; }

        /// <summary>
        /// Niveau de dégâts magiques (échelle 0-10)
        /// </summary>
        public double MagicalDamage { get; set; }

        /// <summary>
        /// Niveau de dégâts vrais (échelle 0-10)
        /// </summary>
        public double TrueDamage { get; set; }

        /// <summary>
        /// Niveau de résistance/tank (échelle 0-10)
        /// </summary>
        public double Tankiness { get; set; }

        /// <summary>
        /// Niveau de mobilité (échelle 0-10)
        /// </summary>
        public double Mobility { get; set; }

        /// <summary>
        /// Niveau de contrôle de foule (échelle 0-10)
        /// </summary>
        public double CC { get; set; }

        /// <summary>
        /// Niveau de sustain/régénération (échelle 0-10)
        /// </summary>
        public double Sustain { get; set; }

        /// <summary>
        /// Niveau d'utilité pour l'équipe (échelle 0-10)
        /// </summary>
        public double Utility { get; set; }

        /// <summary>
        /// Force en early game (échelle 0-10)
        /// </summary>
        public double EarlyGame { get; set; }

        /// <summary>
        /// Force en mid game (échelle 0-10)
        /// </summary>
        public double MidGame { get; set; }

        /// <summary>
        /// Force en late game (échelle 0-10)
        /// </summary>
        public double LateGame { get; set; }

        /// <summary>
        /// Difficulté de jeu (échelle 0-10)
        /// </summary>
        public double Difficulty { get; set; }

        /// <summary>
        /// Tags du champion (Fighter, Mage, etc.)
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Rôle actuel du champion (utilisé pour les analyses)
        /// </summary>
        public ChampionRole CurrentRole { get; set; }

        /// <summary>
        /// Date de dernière mise à jour des données
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public Champion()
        {
            Tags = new List<string>();
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Constructeur avec paramètres de base
        /// </summary>
        public Champion(int id, string name, string title, string imageUrl)
        {
            Id = id;
            Name = name;
            Title = title;
            ImageUrl = imageUrl;
            Tags = new List<string>();
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Calcule le score global du champion (moyenne pondérée de ses attributs)
        /// </summary>
        public double CalculateOverallScore()
        {
            return (PhysicalDamage + MagicalDamage + TrueDamage + Tankiness + 
                   Mobility + CC + Sustain + Utility) / 8.0;
        }

        /// <summary>
        /// Détermine si le champion est principalement un carry AD
        /// </summary>
        public bool IsADCarry()
        {
            return PhysicalDamage >= 7.0 && Tags.Contains("Marksman");
        }

        /// <summary>
        /// Détermine si le champion est principalement un mage
        /// </summary>
        public bool IsMage()
        {
            return MagicalDamage >= 7.0 && Tags.Contains("Mage");
        }

        /// <summary>
        /// Détermine si le champion est principalement un tank
        /// </summary>
        public bool IsTank()
        {
            return Tankiness >= 7.0 && (Tags.Contains("Tank") || Tags.Contains("Fighter"));
        }

        /// <summary>
        /// Détermine si le champion est principalement un assassin
        /// </summary>
        public bool IsAssassin()
        {
            return (PhysicalDamage >= 7.0 || MagicalDamage >= 7.0) && 
                   Mobility >= 7.0 && Tags.Contains("Assassin");
        }

        /// <summary>
        /// Détermine si le champion est principalement un support
        /// </summary>
        public bool IsSupport()
        {
            return Utility >= 7.0 && (Tags.Contains("Support") || CC >= 7.0);
        }

        /// <summary>
        /// Retourne une représentation textuelle du champion
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {Title}";
        }
    }
}
