# Documentation Technique - LoL Analyzer

## Architecture du projet

LoL Analyzer est structuré selon une architecture modulaire qui sépare clairement les responsabilités :

### Structure des dossiers

```
LoLAnalyzer/
├── Core/
│   ├── Data/           # Accès aux données et gestion de la base de données
│   ├── Models/         # Modèles de données
│   ├── Services/       # Services métier
│   └── Utils/          # Utilitaires
├── UI/
│   ├── Windows/        # Fenêtres de l'application
│   ├── Controls/       # Contrôles personnalisés
│   ├── ViewModels/     # ViewModels (pattern MVVM)
│   └── Styles/         # Styles et ressources
├── Resources/
│   ├── Images/         # Images et icônes
│   └── Data/           # Données statiques
└── Tests/              # Tests unitaires et d'intégration
```

## Modèles de données

### Champion

Représente un champion de League of Legends avec ses attributs et statistiques.

```csharp
public class Champion
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, double> Stats { get; set; }
    // ...
}
```

### ChampionRole

Définit les rôles possibles pour un champion et leurs statistiques associées.

```csharp
public enum Role
{
    TOP,
    JUNGLE,
    MID,
    ADC,
    SUPPORT
}

public class ChampionRole
{
    public int ChampionId { get; set; }
    public Role Role { get; set; }
    public double WinRate { get; set; }
    public double PickRate { get; set; }
    public double BanRate { get; set; }
    // ...
}
```

### TeamComposition

Représente une composition d'équipe complète avec analyse.

```csharp
public class TeamComposition
{
    public string TeamName { get; set; }
    public string TeamColor { get; set; }
    public List<TeamMember> Members { get; set; }
    public Dictionary<string, double> DamageDistribution { get; set; }
    public List<string> Strengths { get; set; }
    public List<string> Weaknesses { get; set; }
    // ...
}
```

### Match

Représente un match de League of Legends avec ses détails.

```csharp
public class Match
{
    public string MatchId { get; set; }
    public string PlatformId { get; set; }
    public DateTime GameStartTime { get; set; }
    public int GameDuration { get; set; }
    public TeamComposition BlueTeam { get; set; }
    public TeamComposition RedTeam { get; set; }
    // ...
}
```

### UserProfile

Représente le profil d'un utilisateur de l'application.

```csharp
public class UserProfile
{
    public int Id { get; set; }
    public string SummonerName { get; set; }
    public string Region { get; set; }
    public string MainRole { get; set; }
    public List<int> PreferredChampions { get; set; }
    public UserPreferences Preferences { get; set; }
    // ...
}
```

## Services

### RiotApiService

Service principal pour l'accès à l'API Riot Games.

```csharp
public class RiotApiService
{
    private readonly string _apiKey;
    private readonly Logger _logger;
    private readonly RateLimiter _rateLimiter;
    private readonly ApiCache _apiCache;

    // Méthodes principales
    public async Task<SummonerDto> GetSummonerByNameAsync(string summonerName, string region);
    public async Task<List<LeagueEntryDto>> GetSummonerRankAsync(string summonerId, string region);
    public async Task<List<string>> GetMatchIdsAsync(string puuid, string region, int count = 20);
    public async Task<MatchDto> GetMatchDetailsAsync(string matchId, string region);
    public async Task<ChampionListDto> GetChampionsAsync(string version = "latest");
    // ...
}
```

### CompositionAnalyzer

Service d'analyse de composition d'équipe.

```csharp
public class CompositionAnalyzer
{
    public TeamCompositionAnalysis AnalyzeComposition(TeamComposition composition);
    public ComparisonResult CompareCompositions(TeamComposition team1, TeamComposition team2);
    public List<string> IdentifyStrengths(TeamComposition composition);
    public List<string> IdentifyWeaknesses(TeamComposition composition);
    // ...
}
```

### CounterPickSuggester

Service de suggestion de contre-picks.

```csharp
public class CounterPickSuggester
{
    public List<CounterPickSuggestion> SuggestCounterPicks(Champion enemyChampion, Role role);
    public List<CounterPickSuggestion> SuggestCounterPicksForTeam(TeamComposition enemyTeam, Role role);
    // ...
}
```

### TrollDetector

Service de détection des picks problématiques.

```csharp
public class TrollDetector
{
    public bool IsTrollPick(Champion champion, Role role);
    public double GetPickViability(Champion champion, Role role);
    public List<Champion> SuggestAlternatives(Champion champion, Role role);
    // ...
}
```

## Utilitaires

### RateLimiter

Gère les limites de requêtes pour l'API Riot Games.

```csharp
public class RateLimiter
{
    private readonly Dictionary<TimeSpan, TokenBucket> _buckets;
    
    public RateLimiter(Dictionary<TimeSpan, int> limits);
    public async Task WaitForPermissionAsync();
    public void UpdateLimits(int retryAfterSeconds);
    // ...
}
```

### ApiCache

Système de cache pour les réponses de l'API.

```csharp
public class ApiCache
{
    private readonly Dictionary<string, CacheItem> _cache;
    
    public bool TryGetValue<T>(string key, out T value);
    public void Set<T>(string key, T value, TimeSpan expiration);
    public void Remove(string key);
    public void Clear();
    // ...
}
```

### Logger

Système de journalisation pour l'application.

```csharp
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

public class Logger
{
    public void Log(LogLevel level, string message);
    public void Debug(string message);
    public void Info(string message);
    public void Warning(string message);
    public void Error(string message);
    public void Fatal(string message);
    public void LogException(LogLevel level, string message, Exception exception);
    // ...
}
```

## Interface utilisateur

### OverlayWindow

Fenêtre d'overlay transparente qui se superpose au client League of Legends.

```csharp
public partial class OverlayWindow : Window
{
    private readonly Logger _logger;
    private Point _lastPosition;
    private bool _isDragging = false;
    private bool _isResizing = false;
    
    public OverlayWindow(Logger logger);
    public void SetOpacity(double opacity);
    public void SetCompactMode(bool isCompact);
    // ...
}
```

### MainWindow

Fenêtre principale de l'application.

```csharp
public partial class MainWindow : Window
{
    private OverlayWindow _overlayWindow;
    
    public MainWindow();
    private void LaunchOverlay_Click(object sender, RoutedEventArgs e);
    private void AdvancedSettings_Click(object sender, RoutedEventArgs e);
    private void UpdateStatus(string status);
    // ...
}
```

## Intégration avec l'API Riot Games

### Configuration de l'API

La clé API Riot Games est configurée dans la classe `App.xaml.cs` :

```csharp
// Clé API Riot Games
string apiKey = "RGAPI-cb458673-83bb-4e66-8d13-02dc0f66845a";
```

### Limites de requêtes

Les limites de requêtes sont configurées dans le constructeur de `RiotApiService` :

```csharp
// Limites fournies par l'utilisateur: 20 requêtes/seconde et 100 requêtes/2 minutes
_rateLimiter = new RateLimiter(
    new Dictionary<TimeSpan, int>
    {
        { TimeSpan.FromSeconds(1), 20 },
        { TimeSpan.FromMinutes(2), 100 }
    }
);
```

## Compilation et déploiement

### Prérequis

- Visual Studio 2019 ou supérieur
- .NET Framework 4.7.2 ou supérieur
- NuGet packages:
  - Newtonsoft.Json
  - System.Data.SQLite

### Étapes de compilation

1. Ouvrez la solution `LoLAnalyzer.sln` dans Visual Studio
2. Restaurez les packages NuGet (clic droit sur la solution > Restaurer les packages NuGet)
3. Compilez la solution (F6 ou Build > Build Solution)
4. Exécutez l'application (F5 ou Debug > Start Debugging)

### Déploiement

Pour créer un package de déploiement :

1. Clic droit sur le projet > Publier
2. Sélectionnez "Dossier"
3. Configurez le chemin de publication
4. Cliquez sur "Publier"

## Extensibilité

Le projet est conçu pour être facilement extensible :

- Ajout de nouveaux services dans le dossier `Core/Services`
- Ajout de nouveaux modèles dans le dossier `Core/Models`
- Ajout de nouvelles fenêtres dans le dossier `UI/Windows`
- Ajout de nouveaux contrôles dans le dossier `UI/Controls`

## Bonnes pratiques

- Utilisez le logger pour tracer les actions importantes
- Respectez les limites de requêtes de l'API Riot Games
- Utilisez le cache pour éviter les requêtes redondantes
- Suivez le pattern MVVM pour l'interface utilisateur
- Écrivez des tests unitaires pour les nouvelles fonctionnalités

---

© 2025 LoL Analyzer. Tous droits réservés.
