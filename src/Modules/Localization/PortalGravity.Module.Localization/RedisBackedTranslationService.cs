using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;

namespace PortalGravity.Module.Localization;

public interface ITranslationService
{
    Task<Dictionary<string, string>> GetTranslationsAsync(string namespaceName, string locale, CancellationToken ct = default);
    Task UpsertTranslationAsync(string namespaceName, string locale, string key, string value, CancellationToken ct = default);
}

public class RedisBackedTranslationService : ITranslationService
{
    private readonly AppDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public RedisBackedTranslationService(AppDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string namespaceName, string locale, CancellationToken ct = default)
    {
        var cacheKey = $"i18n:{namespaceName}:{locale}";
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(cachedData) ?? new Dictionary<string, string>();
        }

        // Cache miss -> DB Query
        var ns = await _dbContext.Set<TranslationNamespaceEntity>().AsNoTracking()
            .FirstOrDefaultAsync(n => n.Name == namespaceName, ct);

        var translations = new Dictionary<string, string>();

        if (ns != null)
        {
            var pagedTranslations = await _dbContext.Set<TranslationEntity>().AsNoTracking()
                .Where(t => t.NamespaceId == ns.Id && t.Locale == locale)
                .ToListAsync(ct);

            foreach (var t in pagedTranslations)
            {
                translations[t.Key] = t.Value;
            }
        }

        // Save to cache (TTL: 1 hour)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(translations), cacheOptions, ct);

        return translations;
    }

    public async Task UpsertTranslationAsync(string namespaceName, string locale, string key, string value, CancellationToken ct = default)
    {
        var ns = await _dbContext.Set<TranslationNamespaceEntity>()
            .FirstOrDefaultAsync(n => n.Name == namespaceName, ct);

        if (ns == null)
        {
            ns = new TranslationNamespaceEntity { Name = namespaceName };
            _dbContext.Set<TranslationNamespaceEntity>().Add(ns);
            await _dbContext.SaveChangesAsync(ct);
        }

        var translation = await _dbContext.Set<TranslationEntity>()
            .FirstOrDefaultAsync(t => t.NamespaceId == ns.Id && t.Locale == locale && t.Key == key, ct);

        if (translation != null)
        {
            translation.Value = value;
        }
        else
        {
            _dbContext.Set<TranslationEntity>().Add(new TranslationEntity
            {
                NamespaceId = ns.Id,
                Locale = locale,
                Key = key,
                Value = value
            });
        }

        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var cacheKey = $"i18n:{namespaceName}:{locale}";
        await _cache.RemoveAsync(cacheKey, ct);
    }
}
