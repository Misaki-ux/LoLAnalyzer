using System;
using System.Collections.Generic;

namespace LoLAnalyzer.Core.Utils
{
    /// <summary>
    /// Système de cache pour les réponses de l'API
    /// </summary>
    public class ApiCache
    {
        private readonly Dictionary<string, CacheItem> _cache;

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public ApiCache()
        {
            _cache = new Dictionary<string, CacheItem>();
        }

        /// <summary>
        /// Tente de récupérer une valeur du cache
        /// </summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && !cacheItem.IsExpired)
            {
                value = (T)cacheItem.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Ajoute ou met à jour une valeur dans le cache
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            var cacheItem = new CacheItem
            {
                Value = value,
                ExpirationTime = DateTime.UtcNow + expiration
            };

            _cache[key] = cacheItem;
        }

        /// <summary>
        /// Supprime une valeur du cache
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// Vide le cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Supprime les éléments expirés du cache
        /// </summary>
        public void CleanExpired()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Classe interne pour stocker les éléments du cache
        /// </summary>
        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime ExpirationTime { get; set; }

            public bool IsExpired => DateTime.UtcNow > ExpirationTime;
        }
    }
}
