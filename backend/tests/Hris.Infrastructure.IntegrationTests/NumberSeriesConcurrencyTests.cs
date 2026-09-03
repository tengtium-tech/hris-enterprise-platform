using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies numbering-framework.md's own AI Implementation Guidance directly: "Two
/// simultaneous requests must never receive the same number" (CTR-DAT-001).
/// <see cref="INumberSeriesRepository.IncrementAndGetNextSequenceValueAsync"/>'s own
/// remarks explain why an ordinary load-mutate-save round trip through EF Core's change
/// tracker cannot provide this guarantee; this is the test that actually proves the
/// atomic-SQL alternative does, under genuine concurrent load against a real,
/// disposable PostgreSQL instance -- an in-process fake repository (what
/// <c>Hris.Foundation.Numbering.Tests</c> uses throughout) cannot exercise this
/// property no matter how it is written, since the whole risk is about what two
/// separate database connections racing against the same row actually do, not about
/// anything expressible in C# alone.
/// </summary>
public sealed class NumberSeriesConcurrencyTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public NumberSeriesConcurrencyTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IncrementAndGetNextSequenceValueAsync_ProducesSequentialValues_WhenCalledSequentially()
    {
        var seriesId = await SeedNumberSeriesAsync();

        long first;
        long second;
        long third;

        using (var scope = _fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<INumberSeriesRepository>();
            first = await repository.IncrementAndGetNextSequenceValueAsync(seriesId, CancellationToken.None);
            second = await repository.IncrementAndGetNextSequenceValueAsync(seriesId, CancellationToken.None);
            third = await repository.IncrementAndGetNextSequenceValueAsync(seriesId, CancellationToken.None);
        }

        first.Should().Be(1);
        second.Should().Be(2);
        third.Should().Be(3);
    }

    [Fact]
    public async Task IncrementAndGetNextSequenceValueAsync_NeverProducesADuplicate_UnderGenuineConcurrentLoad()
    {
        var seriesId = await SeedNumberSeriesAsync();

        // Each concurrent caller gets its own DI scope, and therefore its own
        // HrisDbContext and its own underlying Npgsql connection -- genuinely separate
        // database connections racing against the same row, not just concurrent
        // .NET tasks sharing one connection, which would not exercise the real risk
        // this test exists to rule out.
        const int concurrentRequestCount = 50;

        var tasks = Enumerable.Range(0, concurrentRequestCount).Select(async _ =>
        {
            using var scope = _fixture.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INumberSeriesRepository>();
            return await repository.IncrementAndGetNextSequenceValueAsync(seriesId, CancellationToken.None).ConfigureAwait(false);
        });

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(concurrentRequestCount);
        results.Distinct().Should().HaveCount(concurrentRequestCount, "two simultaneous requests must never receive the same number (CTR-DAT-001)");
        results.Min().Should().Be(1);
        results.Max().Should().Be(concurrentRequestCount);
    }

    private async Task<NumberSeriesId> SeedNumberSeriesAsync()
    {
        using var writeScope = _fixture.CreateScope();
        var repository = writeScope.ServiceProvider.GetRequiredService<INumberSeriesRepository>();
        var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

        var key = SeriesKey.Create($"IntegrationTests.Concurrency.{Guid.NewGuid():N}").Value;
        var prefix = NumberPrefix.Create("EMP").Value;
        var format = NumberFormat.Create(6, includeYear: true, includeMonth: false, "-").Value;

        var series = NumberSeries.Register(key, prefix, format, SequenceResetPolicy.Never).Value;

        await repository.AddAsync(series, CancellationToken.None).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return series.Id;
    }
}
