using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LoLAnalyzer.Core.Utils
{
    /// <summary>
    /// Gestionnaire de limites de requêtes pour l'API Riot Games
    /// </summary>
    public class RateLimiter
    {
        private readonly Dictionary<TimeSpan, TokenBucket> _buckets;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructeur avec limites de requêtes
        /// </summary>
        public RateLimiter(Dictionary<TimeSpan, int> limits)
        {
            _buckets = new Dictionary<TimeSpan, TokenBucket>();
            
            foreach (var limit in limits)
            {
                _buckets[limit.Key] = new TokenBucket(limit.Value, limit.Key);
            }
        }

        /// <summary>
        /// Attend l'autorisation d'envoyer une requête
        /// </summary>
        public async Task WaitForPermissionAsync()
        {
            await _semaphore.WaitAsync();
            
            try
            {
                foreach (var bucket in _buckets.Values)
                {
                    await bucket.WaitForTokenAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Met à jour les limites de requêtes en fonction des en-têtes de réponse
        /// </summary>
        public void UpdateLimits(int retryAfterSeconds)
        {
            foreach (var bucket in _buckets.Values)
            {
                bucket.Pause(TimeSpan.FromSeconds(retryAfterSeconds));
            }
        }

        /// <summary>
        /// Classe interne pour gérer un bucket de jetons
        /// </summary>
        private class TokenBucket
        {
            private readonly int _capacity;
            private readonly TimeSpan _refillTime;
            private int _tokens;
            private DateTime _lastRefill;
            private DateTime _pauseUntil;
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

            /// <summary>
            /// Constructeur avec capacité et temps de remplissage
            /// </summary>
            public TokenBucket(int capacity, TimeSpan refillTime)
            {
                _capacity = capacity;
                _refillTime = refillTime;
                _tokens = capacity;
                _lastRefill = DateTime.UtcNow;
                _pauseUntil = DateTime.MinValue;
            }

            /// <summary>
            /// Attend qu'un jeton soit disponible
            /// </summary>
            public async Task WaitForTokenAsync()
            {
                await _semaphore.WaitAsync();
                
                try
                {
                    // Si le bucket est en pause, attendre la fin de la pause
                    if (DateTime.UtcNow < _pauseUntil)
                    {
                        var waitTime = _pauseUntil - DateTime.UtcNow;
                        await Task.Delay(waitTime);
                    }
                    
                    // Remplir le bucket si nécessaire
                    RefillBucket();
                    
                    // Attendre qu'un jeton soit disponible
                    while (_tokens <= 0)
                    {
                        var timeToNextToken = _refillTime / _capacity;
                        await Task.Delay(timeToNextToken);
                        RefillBucket();
                    }
                    
                    // Consommer un jeton
                    _tokens--;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            /// <summary>
            /// Met le bucket en pause pendant une durée spécifiée
            /// </summary>
            public void Pause(TimeSpan duration)
            {
                _pauseUntil = DateTime.UtcNow + duration;
                _tokens = 0;
            }

            /// <summary>
            /// Remplit le bucket en fonction du temps écoulé
            /// </summary>
            private void RefillBucket()
            {
                var now = DateTime.UtcNow;
                var elapsed = now - _lastRefill;
                
                if (elapsed >= _refillTime)
                {
                    // Remplir complètement le bucket
                    _tokens = _capacity;
                    _lastRefill = now;
                }
                else
                {
                    // Remplir partiellement le bucket
                    var tokensToAdd = (int)((elapsed.TotalMilliseconds / _refillTime.TotalMilliseconds) * _capacity);
                    
                    if (tokensToAdd > 0)
                    {
                        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                        _lastRefill = now - TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds % _refillTime.TotalMilliseconds);
                    }
                }
            }
        }
    }
}
