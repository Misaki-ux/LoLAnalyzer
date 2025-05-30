# Documentation Technique - LoL Analyzer (Version Améliorée)

## Introduction

Cette documentation technique décrit l'architecture et les fonctionnalités de l'application LoL Analyzer en C#, spécialement conçue pour analyser les compositions d'équipe dans League of Legends, suggérer des contre-picks optimaux et détecter les picks problématiques.

La version améliorée intègre une base de données enrichie pour les compositions, synergies, contre-picks et méta-compositions, ainsi qu'une interface utilisateur avancée avec des fonctionnalités d'analyse plus poussées.

## Architecture

L'application est structurée selon une architecture modulaire en couches :

### 1. Couche Présentation (UI)
- **Windows** : Fenêtres WPF pour l'interface utilisateur
  - `MainWindow.xaml/cs` : Fenêtre principale de l'application
  - `OverlayWindow.xaml/cs` : Fenêtre d'overlay de base
  - `EnhancedOverlayWindow.xaml/cs` : Fenêtre d'overlay améliorée avec fonctionnalités avancées

### 2. Couche Métier (Core)
- **Models** : Classes de modèles de données
  - `Champion.cs` : Représentation d'un champion
  - `ChampionRole.cs` : Rôles des champions
  - `TeamComposition.cs` : Composition d'équipe
  - `TeamMember.cs` : Membre d'une équipe
  - `Match.cs` : Données de match
  - `UserProfile.cs` : Profil utilisateur
- **Services** : Services métier
  - `RiotApiService.cs` : Service d'accès à l'API Riot
  - `CompositionAnalyzer.cs` : Analyse de composition de base
  - `EnhancedCompositionAnalyzer.cs` : Analyse de composition avancée
  - `BanSuggestionService.cs` : Suggestions de bans
  - `RunesRecommendationService.cs` : Recommandations de runes
  - `UserProfileService.cs` : Gestion des profils utilisateur
- **Data** : Accès aux données
  - `DatabaseManager.cs` : Gestionnaire de base de données de base
  - `EnhancedDatabaseManager.cs` : Gestionnaire de base de données enrichie

### 3. Couche Utilitaire
- **Utils** : Classes utilitaires
  - `RateLimiter.cs` : Gestion des limites de requêtes API
  - `ApiCache.cs` : Mise en cache des données API
  - `Logger.cs` : Journalisation des événements

## Base de données enrichie

La base de données enrichie (`EnhancedDatabaseManager.cs`) contient les tables suivantes :

### 1. Champions
- ID, Nom, ImageUrl, Tags, Description
- Statistiques de base (dégâts, mobilité, contrôle, etc.)

### 2. ChampionRoles
- ChampionID, Role, WinRate, PickRate, BanRate
- Statistiques spécifiques au rôle

### 3. ChampionPhaseStats
- ChampionID, Role, Phase (EARLY, MID, LATE), Score
- Performance du champion par phase de jeu

### 4. ChampionSynergies
- Champion1ID, Champion2ID, SynergyScore, Description
- Synergies entre champions

### 5. CounterPicks
- ChampionID, CounterChampionID, Role, WinRate, CounterScore
- Difficulté, Explication

### 6. TrollPicks
- ChampionID, Role, TrollScore, Reason
- Picks problématiques

### 7. TrollPickAlternatives
- TrollPickID, AlternativeChampionID, RecommendationScore
- Alternatives aux picks problématiques

### 8. MetaCompositions
- ID, Name, Description, Patch, WinRate
- Compositions méta populaires

### 9. MetaCompositionChampions
- CompositionID, ChampionID, Role
- Champions dans les compositions méta

## Fonctionnalités avancées

### 1. Analyse de composition d'équipe
- Distribution des dégâts (physique, magique, vrai)
- Forces et faiblesses de la composition
- Performance par phase de jeu (early, mid, late)
- Score de synergie entre champions
- Correspondance avec les compositions méta

### 2. Suggestion de contre-picks
- 3 contre-picks optimaux par rôle
- Taux de victoire contre l'adversaire
- Difficulté d'utilisation
- Explication de l'efficacité

### 3. Détection des picks problématiques
- Identification des picks non-méta ou "troll"
- Suggestion d'alternatives viables
- Explication des problèmes

### 4. Interface overlay améliorée
- Fenêtre transparente et déplaçable
- Redimensionnement interactif
- Mode compact pour un affichage minimal
- Onglets pour les différentes fonctionnalités

## Intégration avec l'API Riot Games

L'application utilise l'API Riot Games pour récupérer les données en temps réel :

```csharp
// Configuration de l'API
private const string API_KEY = "RGAPI-cb458673-83bb-4e66-8d13-02dc0f66845a";
private const int REQUESTS_PER_SECOND = 20;
private const int REQUESTS_PER_TWO_MINUTES = 100;
```

Le service `RiotApiService.cs` gère les appels API avec un système de limitation de requêtes (`RateLimiter.cs`) pour respecter les limites imposées par Riot Games.

## Compilation et déploiement

1. Ouvrir la solution `LoLAnalyzer.sln` dans Visual Studio
2. Restaurer les packages NuGet
3. Compiler le projet
4. Exécuter l'application

## Dépendances

- .NET Framework 4.7.2 ou supérieur
- SQLite.NET
- Newtonsoft.Json
- System.Windows.Interactivity

## Évolutions futures

- Intégration avec l'historique des matchs
- Analyse prédictive des compositions
- Recommandations d'objets
- Statistiques personnelles
- Synchronisation avec le client LoL
