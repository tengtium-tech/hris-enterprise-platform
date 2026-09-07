using FluentAssertions;
using Hris.Api.Logging;
using Hris.SharedKernel;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// Sprint 9 (HEP-89): confirms <see cref="SensitiveDataDestructuringPolicy"/> redacts
/// a <see cref="SensitiveDataAttribute"/>-marked property while destructuring every
/// other property normally, and that a type with no such property is left to
/// Serilog's own default destructuring untouched. No concrete Domain/DTO type in this
/// backend carries the attribute yet (see that attribute's own remarks) -- this test
/// type stands in for one, the same role <c>TestDomainEvent</c> plays for
/// <c>OutboxDispatchEnrichmentTests</c>.
/// </summary>
public sealed class SensitiveDataDestructuringPolicyTests
{
    private sealed record RecordWithSensitiveField(string Name, [property: SensitiveData] string TaxIdentificationNumber);

    private sealed record RecordWithNoSensitiveField(string Name, string Department);

    [Fact]
    public void LoggingAnObject_RedactsTheSensitiveDataMarkedProperty_ButNotOthers()
    {
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();
        var value = new RecordWithSensitiveField("Ada Lovelace", "123-45-6789");

        logger.Information("Employee record: {@Employee}", value);

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        var rendered = logEvent.Properties["Employee"].ToString();
        rendered.Should().Contain("Ada Lovelace");
        rendered.Should().NotContain("123-45-6789");
        rendered.Should().Contain("REDACTED");
    }

    [Fact]
    public void LoggingAnObject_WithNoSensitiveProperty_DestructuresNormally()
    {
        var capturedEvents = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(new DelegatingSink(capturedEvents.Add))
            .CreateLogger();
        var value = new RecordWithNoSensitiveField("Ada Lovelace", "Engineering");

        logger.Information("Profile: {@Profile}", value);

        var logEvent = capturedEvents.Should().ContainSingle().Subject;
        var rendered = logEvent.Properties["Profile"].ToString();
        rendered.Should().Contain("Ada Lovelace");
        rendered.Should().Contain("Engineering");
        rendered.Should().NotContain("REDACTED");
    }

    private sealed class DelegatingSink : ILogEventSink
    {
        private readonly Action<LogEvent> _onEmit;

        public DelegatingSink(Action<LogEvent> onEmit)
        {
            _onEmit = onEmit;
        }

        public void Emit(LogEvent logEvent) => _onEmit(logEvent);
    }
}
