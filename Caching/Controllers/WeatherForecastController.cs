using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caching.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMemoryCache _cache;
        private const string WEATHER_FORECAST_LIST = "WF_List";
        private const string REASON_MSG = "Reason_msg";
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        #region In memory caching
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            if (!_cache.TryGetValue(WEATHER_FORECAST_LIST, out WeatherForecast[] cacheEntry))
            {
                cacheEntry = GetWeatherForecastData();
                _cache.Set(WEATHER_FORECAST_LIST,
                           cacheEntry,
                           new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(60))  // if not access again in 3 sec it will be removed
                           );
            }

            return cacheEntry;
        }

        [HttpGet]
        public Task<WeatherForecast[]> CacheGetOrCreate()
        {
            return _cache.GetOrCreateAsync(WEATHER_FORECAST_LIST, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromSeconds(60);
                return Task.FromResult(GetWeatherForecastData());
            });
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> CacheGet()
        {
            return _cache.Get<IEnumerable<WeatherForecast>>(WEATHER_FORECAST_LIST);
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> CacheGetOrCreateAbs()
        {
            return _cache.GetOrCreate(WEATHER_FORECAST_LIST, entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(40));
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(90);   // hetshal 5als, w l SlidingExpiration msh he2dr y2sr 3leh ya3ni ma y2dresh ykbro
                return GetWeatherForecastData();
            });
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> CreateCacheWithCallbackEntry()
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove)                             // Pin to cache.
                .RegisterPostEvictionCallback(callback: EvictionCallback, state: this); // Add eviction callback

            _cache.Set(WEATHER_FORECAST_LIST, GetWeatherForecastData(), cacheEntryOptions);

            return CacheGet();
        }

        [HttpGet]
        public object GetCallbackEntry()
        {
            return new
            {
                entry = _cache.Get<WeatherForecast[]>(WEATHER_FORECAST_LIST),
                Message = _cache.Get<string>(REASON_MSG)
            };
        }

        [HttpDelete]
        public IActionResult RemoveCallbackEntry()
        {
            _cache.Remove(WEATHER_FORECAST_LIST);
            return NoContent();
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            var message = $"Entry was evicted. Reason: {reason}.";
            _cache.Set(REASON_MSG, message);
        }

        #endregion

        private WeatherForecast[] GetWeatherForecastData()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
