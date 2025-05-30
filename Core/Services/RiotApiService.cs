using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Net;
using System.Threading;
using System.Linq;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.Core.Services
{
    /// <summary>
    /// Service d'accès à l'API Riot Games pour League of Legends
    /// </summary>
    public class RiotApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly Logger _logger;
        private readonly RateLimiter _rateLimiter;
        private readonly ApiCache _apiCache;

        // Constantes pour les URLs de base des API Riot
        private const string RIOT_API_BASE_URL = "https://{0}.api.riotgames.com";
        private const string LOL_API_VERSION = "v4";
        private const string MATCH_API_VERSION = "v5";
        private const string DATADRAGON_BASE_URL = "https://ddragon.leagueoflegends.com";

        /// <summary>
        /// Constructeur avec clé API et logger
        /// </summary>
        public RiotApiService(string apiKey, Logger logger)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _logger = logger;
            
            // Initialisation du gestionnaire de limites de requêtes
            // Limites fournies par l'utilisateur: 20 requêtes/seconde et 100 requêtes/2 minutes
            _rateLimiter = new RateLimiter(
                new Dictionary<TimeSpan, int>
                {
                    { TimeSpan.FromSeconds(1), 20 },
                    { TimeSpan.FromMinutes(2), 100 }
                }
            );
            
            _apiCache = new ApiCache();
            
            _logger.Log(LogLevel.Info, "Service d'API Riot initialisé avec la clé fournie");
        }

        /// <summary>
        /// Obtient les informations d'un invocateur par son nom
        /// </summary>
        public async Task<SummonerDto> GetSummonerByNameAsync(string summonerName, string region)
        {
            string endpoint = $"/lol/summoner/{LOL_API_VERSION}/summoners/by-name/{WebUtility.UrlEncode(summonerName)}";
            string cacheKey = $"summoner_{region}_{summonerName}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out SummonerDto cachedSummoner))
            {
                _logger.Log(LogLevel.Debug, $"Données d'invocateur récupérées du cache pour {summonerName}");
                return cachedSummoner;
            }
            
            try
            {
                var response = await SendApiRequestAsync<SummonerDto>(endpoint, region);
                
                // Mettre en cache pour 1 heure
                _apiCache.Set(cacheKey, response, TimeSpan.FromHours(1));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des informations de l'invocateur {summonerName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient les informations de rang d'un invocateur
        /// </summary>
        public async Task<List<LeagueEntryDto>> GetSummonerRankAsync(string summonerId, string region)
        {
            string endpoint = $"/lol/league/{LOL_API_VERSION}/entries/by-summoner/{summonerId}";
            string cacheKey = $"rank_{region}_{summonerId}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out List<LeagueEntryDto> cachedRanks))
            {
                _logger.Log(LogLevel.Debug, $"Données de rang récupérées du cache pour {summonerId}");
                return cachedRanks;
            }
            
            try
            {
                var response = await SendApiRequestAsync<List<LeagueEntryDto>>(endpoint, region);
                
                // Mettre en cache pour 30 minutes
                _apiCache.Set(cacheKey, response, TimeSpan.FromMinutes(30));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des rangs de l'invocateur {summonerId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient la liste des IDs de match d'un joueur
        /// </summary>
        public async Task<List<string>> GetMatchIdsAsync(string puuid, string region, int count = 20, int start = 0, string queue = null)
        {
            // Conversion de la région en région continentale pour l'API de match
            string continentalRegion = GetContinentalRegion(region);
            
            string endpoint = $"/lol/match/{MATCH_API_VERSION}/matches/by-puuid/{puuid}/ids?start={start}&count={count}";
            
            if (!string.IsNullOrEmpty(queue))
            {
                endpoint += $"&queue={queue}";
            }
            
            string cacheKey = $"matchids_{continentalRegion}_{puuid}_{start}_{count}_{queue}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out List<string> cachedMatchIds))
            {
                _logger.Log(LogLevel.Debug, $"IDs de match récupérés du cache pour {puuid}");
                return cachedMatchIds;
            }
            
            try
            {
                var response = await SendApiRequestAsync<List<string>>(endpoint, continentalRegion);
                
                // Mettre en cache pour 5 minutes
                _apiCache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des IDs de match pour {puuid}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient les détails d'un match
        /// </summary>
        public async Task<MatchDto> GetMatchDetailsAsync(string matchId, string region)
        {
            // Conversion de la région en région continentale pour l'API de match
            string continentalRegion = GetContinentalRegion(region);
            
            string endpoint = $"/lol/match/{MATCH_API_VERSION}/matches/{matchId}";
            string cacheKey = $"match_{continentalRegion}_{matchId}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out MatchDto cachedMatch))
            {
                _logger.Log(LogLevel.Debug, $"Détails du match récupérés du cache pour {matchId}");
                return cachedMatch;
            }
            
            try
            {
                var response = await SendApiRequestAsync<MatchDto>(endpoint, continentalRegion);
                
                // Mettre en cache pour 1 jour (les matchs ne changent pas)
                _apiCache.Set(cacheKey, response, TimeSpan.FromDays(1));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des détails du match {matchId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient la liste des champions
        /// </summary>
        public async Task<ChampionListDto> GetChampionsAsync(string version = "latest")
        {
            string endpoint = $"/cdn/{version}/data/fr_FR/champion.json";
            string cacheKey = $"champions_{version}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out ChampionListDto cachedChampions))
            {
                _logger.Log(LogLevel.Debug, $"Liste des champions récupérée du cache pour la version {version}");
                return cachedChampions;
            }
            
            try
            {
                var response = await SendDataDragonRequestAsync<ChampionListDto>(endpoint);
                
                // Mettre en cache pour 1 jour (ou jusqu'à la prochaine mise à jour)
                _apiCache.Set(cacheKey, response, TimeSpan.FromDays(1));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération de la liste des champions: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient la dernière version de Data Dragon
        /// </summary>
        public async Task<List<string>> GetLatestVersionAsync()
        {
            string endpoint = "/api/versions.json";
            string cacheKey = "versions";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out List<string> cachedVersions))
            {
                _logger.Log(LogLevel.Debug, "Versions récupérées du cache");
                return cachedVersions;
            }
            
            try
            {
                var response = await SendDataDragonRequestAsync<List<string>>(endpoint);
                
                // Mettre en cache pour 1 heure
                _apiCache.Set(cacheKey, response, TimeSpan.FromHours(1));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des versions: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtient les données de maîtrise de champion d'un invocateur
        /// </summary>
        public async Task<List<ChampionMasteryDto>> GetChampionMasteryAsync(string summonerId, string region)
        {
            string endpoint = $"/lol/champion-mastery/{LOL_API_VERSION}/champion-masteries/by-summoner/{summonerId}";
            string cacheKey = $"mastery_{region}_{summonerId}";
            
            // Vérifier si les données sont en cache
            if (_apiCache.TryGetValue(cacheKey, out List<ChampionMasteryDto> cachedMastery))
            {
                _logger.Log(LogLevel.Debug, $"Données de maîtrise récupérées du cache pour {summonerId}");
                return cachedMastery;
            }
            
            try
            {
                var response = await SendApiRequestAsync<List<ChampionMasteryDto>>(endpoint, region);
                
                // Mettre en cache pour 1 heure
                _apiCache.Set(cacheKey, response, TimeSpan.FromHours(1));
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la récupération des données de maîtrise pour {summonerId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envoie une requête à l'API Riot Games
        /// </summary>
        private async Task<T> SendApiRequestAsync<T>(string endpoint, string region)
        {
            // Attendre que le rate limiter autorise la requête
            await _rateLimiter.WaitForPermissionAsync();
            
            string url = string.Format(RIOT_API_BASE_URL, region.ToLower()) + endpoint;
            
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("X-Riot-Token", _apiKey);
                
                _logger.Log(LogLevel.Debug, $"Envoi d'une requête à {url}");
                
                using (var response = await _httpClient.SendAsync(request))
                {
                    string content = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Log(LogLevel.Debug, $"Réponse reçue avec succès de {url}");
                        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        _logger.Log(LogLevel.Error, $"Erreur lors de la requête à {url}: {response.StatusCode} - {content}");
                        
                        // Gestion spécifique des erreurs
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                throw new ApiException("Ressource non trouvée", response.StatusCode);
                            case HttpStatusCode.Unauthorized:
                                throw new ApiException("Clé API non valide", response.StatusCode);
                            case (HttpStatusCode)429:
                                // Rate limit atteint, attendre et réessayer
                                int retryAfter = 1;
                                if (response.Headers.Contains("Retry-After"))
                                {
                                    int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out retryAfter);
                                }
                                
                                _logger.Log(LogLevel.Warning, $"Rate limit atteint, attente de {retryAfter} secondes avant de réessayer");
                                
                                // Mettre à jour le rate limiter
                                _rateLimiter.UpdateLimits(retryAfter);
                                
                                // Attendre et réessayer
                                await Task.Delay(retryAfter * 1000);
                                return await SendApiRequestAsync<T>(endpoint, region);
                            default:
                                throw new ApiException($"Erreur API: {response.StatusCode} - {content}", response.StatusCode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Envoie une requête à Data Dragon
        /// </summary>
        private async Task<T> SendDataDragonRequestAsync<T>(string endpoint)
        {
            string url = DATADRAGON_BASE_URL + endpoint;
            
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                _logger.Log(LogLevel.Debug, $"Envoi d'une requête à Data Dragon: {url}");
                
                using (var response = await _httpClient.SendAsync(request))
                {
                    string content = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Log(LogLevel.Debug, $"Réponse reçue avec succès de Data Dragon: {url}");
                        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        _logger.Log(LogLevel.Error, $"Erreur lors de la requête à Data Dragon: {url} - {response.StatusCode} - {content}");
                        throw new ApiException($"Erreur Data Dragon: {response.StatusCode} - {content}", response.StatusCode);
                    }
                }
            }
        }

        /// <summary>
        /// Convertit une région en région continentale pour l'API de match
        /// </summary>
        private string GetContinentalRegion(string region)
        {
            switch (region.ToUpper())
            {
                case "BR1":
                case "LA1":
                case "LA2":
                case "NA1":
                    return "AMERICAS";
                case "JP1":
                case "KR":
                    return "ASIA";
                case "EUN1":
                case "EUW1":
                case "TR1":
                case "RU":
                    return "EUROPE";
                case "OC1":
                case "PH2":
                case "SG2":
                case "TH2":
                case "TW2":
                case "VN2":
                    return "SEA";
                default:
                    return "EUROPE"; // Par défaut
            }
        }
    }

    /// <summary>
    /// Exception spécifique pour les erreurs d'API
    /// </summary>
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public ApiException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    #region DTOs

    /// <summary>
    /// DTO pour les informations d'un invocateur
    /// </summary>
    public class SummonerDto
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Puuid { get; set; }
        public string Name { get; set; }
        public int ProfileIconId { get; set; }
        public long RevisionDate { get; set; }
        public int SummonerLevel { get; set; }
    }

    /// <summary>
    /// DTO pour les informations de rang d'un invocateur
    /// </summary>
    public class LeagueEntryDto
    {
        public string LeagueId { get; set; }
        public string SummonerId { get; set; }
        public string SummonerName { get; set; }
        public string QueueType { get; set; }
        public string Tier { get; set; }
        public string Rank { get; set; }
        public int LeaguePoints { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public bool HotStreak { get; set; }
        public bool Veteran { get; set; }
        public bool FreshBlood { get; set; }
        public bool Inactive { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'un match
    /// </summary>
    public class MatchDto
    {
        public MetadataDto Metadata { get; set; }
        public InfoDto Info { get; set; }
    }

    /// <summary>
    /// DTO pour les métadonnées d'un match
    /// </summary>
    public class MetadataDto
    {
        public string DataVersion { get; set; }
        public string MatchId { get; set; }
        public List<string> Participants { get; set; }
    }

    /// <summary>
    /// DTO pour les informations détaillées d'un match
    /// </summary>
    public class InfoDto
    {
        public long GameCreation { get; set; }
        public long GameDuration { get; set; }
        public long GameEndTimestamp { get; set; }
        public long GameId { get; set; }
        public string GameMode { get; set; }
        public string GameName { get; set; }
        public long GameStartTimestamp { get; set; }
        public string GameType { get; set; }
        public string GameVersion { get; set; }
        public int MapId { get; set; }
        public List<ParticipantDto> Participants { get; set; }
        public string PlatformId { get; set; }
        public int QueueId { get; set; }
        public List<TeamDto> Teams { get; set; }
        public string TournamentCode { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'un participant dans un match
    /// </summary>
    public class ParticipantDto
    {
        public int Assists { get; set; }
        public int ChampionId { get; set; }
        public string ChampionName { get; set; }
        public int ChampionLevel { get; set; }
        public int Deaths { get; set; }
        public int Item0 { get; set; }
        public int Item1 { get; set; }
        public int Item2 { get; set; }
        public int Item3 { get; set; }
        public int Item4 { get; set; }
        public int Item5 { get; set; }
        public int Item6 { get; set; }
        public int Kills { get; set; }
        public int ParticipantId { get; set; }
        public string Puuid { get; set; }
        public int Spell1Casts { get; set; }
        public int Spell2Casts { get; set; }
        public int Spell3Casts { get; set; }
        public int Spell4Casts { get; set; }
        public int SummonerLevel { get; set; }
        public string SummonerName { get; set; }
        public string TeamPosition { get; set; }
        public int TeamId { get; set; }
        public bool Win { get; set; }
        public int TotalDamageDealtToChampions { get; set; }
        public int TotalDamageTaken { get; set; }
        public int TotalMinionsKilled { get; set; }
        public int VisionScore { get; set; }
        public int GoldEarned { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'une équipe dans un match
    /// </summary>
    public class TeamDto
    {
        public List<BanDto> Bans { get; set; }
        public ObjectivesDto Objectives { get; set; }
        public int TeamId { get; set; }
        public bool Win { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'un bannissement
    /// </summary>
    public class BanDto
    {
        public int ChampionId { get; set; }
        public int PickTurn { get; set; }
    }

    /// <summary>
    /// DTO pour les informations des objectifs d'une équipe
    /// </summary>
    public class ObjectivesDto
    {
        public ObjectiveDto Baron { get; set; }
        public ObjectiveDto Champion { get; set; }
        public ObjectiveDto Dragon { get; set; }
        public ObjectiveDto Inhibitor { get; set; }
        public ObjectiveDto RiftHerald { get; set; }
        public ObjectiveDto Tower { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'un objectif
    /// </summary>
    public class ObjectiveDto
    {
        public bool First { get; set; }
        public int Kills { get; set; }
    }

    /// <summary>
    /// DTO pour la liste des champions
    /// </summary>
    public class ChampionListDto
    {
        public string Type { get; set; }
        public string Version { get; set; }
        public Dictionary<string, ChampionDataDto> Data { get; set; }
    }

    /// <summary>
    /// DTO pour les données d'un champion
    /// </summary>
    public class ChampionDataDto
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public ImageDto Image { get; set; }
        public List<string> Tags { get; set; }
        public StatsDto Stats { get; set; }
    }

    /// <summary>
    /// DTO pour les informations d'une image
    /// </summary>
    public class ImageDto
    {
        public string Full { get; set; }
        public string Sprite { get; set; }
        public string Group { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    /// <summary>
    /// DTO pour les statistiques d'un champion
    /// </summary>
    public class StatsDto
    {
        public double Hp { get; set; }
        public double HpPerLevel { get; set; }
        public double Mp { get; set; }
        public double MpPerLevel { get; set; }
        public double Armor { get; set; }
        public double ArmorPerLevel { get; set; }
        public double SpellBlock { get; set; }
        public double SpellBlockPerLevel { get; set; }
        public double AttackDamage { get; set; }
        public double AttackDamagePerLevel { get; set; }
        public double AttackSpeed { get; set; }
        public double AttackSpeedPerLevel { get; set; }
    }

    /// <summary>
    /// DTO pour les informations de maîtrise d'un champion
    /// </summary>
    public class ChampionMasteryDto
    {
        public long ChampionId { get; set; }
        public int ChampionLevel { get; set; }
        public int ChampionPoints { get; set; }
        public long LastPlayTime { get; set; }
        public int ChampionPointsSinceLastLevel { get; set; }
        public int ChampionPointsUntilNextLevel { get; set; }
        public bool ChestGranted { get; set; }
        public int TokensEarned { get; set; }
        public string SummonerId { get; set; }
    }

    #endregion
}
