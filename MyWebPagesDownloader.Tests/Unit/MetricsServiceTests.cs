using Xunit;
using FluentAssertions;
using MyWebPagesDownloader.Application.Services;

namespace MyWebPagesDownloader.Tests.Unit;

public class MetricsServiceTests
{
    [Fact]
    public void IncrementSuccess_ShouldIncreaseSuccessCount()
    {
        // Arrange
        var metrics = new MetricsService();

        // Act
        metrics.IncrementSuccess();
        metrics.IncrementSuccess();

        // Assert
        metrics.SuccessCount.Should().Be(2);
    }

    [Fact]
    public void IncrementFailure_ShouldIncreaseFailureCount()
    {
        // Arrange
        var metrics = new MetricsService();

        // Act
        metrics.IncrementFailure();
        metrics.IncrementFailure();
        metrics.IncrementFailure();

        // Assert
        metrics.FailureCount.Should().Be(3);
    }

    [Fact]
    public void RecordDuration_ShouldAccumulateDuration()
    {
        // Arrange
        var metrics = new MetricsService();

        // Act
        metrics.RecordDuration(100);
        metrics.RecordDuration(200);
        metrics.RecordDuration(300);

        // Assert
        metrics.TotalDurationMilliseconds.Should().Be(600);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var metrics = new MetricsService();
        metrics.IncrementSuccess();
        metrics.IncrementFailure();
        metrics.RecordDuration(500);

        // Act
        metrics.Reset();

        // Assert
        metrics.SuccessCount.Should().Be(0);
        metrics.FailureCount.Should().Be(0);
        metrics.TotalDurationMilliseconds.Should().Be(0);
    }
}
