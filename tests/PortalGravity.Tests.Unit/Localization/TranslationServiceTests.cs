using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PortalGravity.Module.Localization;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using Xunit;
using System.Text.Json;
using System.Text;

namespace PortalGravity.Tests.Unit.Localization;

public class TranslationServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly RedisBackedTranslationService _sut;

    public TranslationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new AppDbContext(options);
        _cacheMock = new Mock<IDistributedCache>();
        _sut = new RedisBackedTranslationService(_db, _cacheMock.Object);
    }

    [Fact]
    public async Task GetTranslationsAsync_ShouldReturnFromCache_WhenAvailable()
    {
        // Arrange
        var ns = "auth";
        var locale = "en";
        var expected = new Dictionary<string, string> { { "key1", "value1" } };
        var cachedJson = JsonSerializer.Serialize(expected);
        var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        // Act
        var result = await _sut.GetTranslationsAsync(ns, locale, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _cacheMock.Verify(c => c.GetAsync(It.Is<string>(s => s.Contains(ns)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTranslationsAsync_ShouldReturnFromDbAndCacheIt_WhenCacheMisses()
    {
        // Arrange
        var nsName = "billing";
        var locale = "tr";
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var ns = new TranslationNamespaceEntity { Name = nsName };
        _db.Set<TranslationNamespaceEntity>().Add(ns);
        await _db.SaveChangesAsync();

        _db.Set<TranslationEntity>().Add(new TranslationEntity
        {
            NamespaceId = ns.Id,
            Locale = locale,
            Key = "invoice.title",
            Value = "Fatura Detayı"
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetTranslationsAsync(nsName, locale, CancellationToken.None);

        // Assert
        result.Should().ContainKey("invoice.title").WhoseValue.Should().Be("Fatura Detayı");
        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(s => s.Contains(nsName)), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
