using Xunit;
using Moq;
using FluentAssertions;
using MyWebPagesDownloader.Application.Validation;
using MyWebPagesDownloader.Core.Exceptions;

namespace MyWebPagesDownloader.Tests.Unit;

public class UrlValidatorTests
{
    [Fact]
    public void Validate_WithValidHttpUrl_ShouldNotThrow()
    {
        // Arrange
        var validator = new UrlValidator();
        var url = "https://example.com";

        // Act
        var act = () => validator.Validate(url);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyUrl_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new UrlValidator();

        // Act & Assert
        Assert.Throws<ValidationException>(() => validator.Validate(string.Empty));
    }

    [Fact]
    public void Validate_WithInvalidUrl_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new UrlValidator();

        // Act & Assert
        Assert.Throws<ValidationException>(() => validator.Validate("not a url"));
    }

    [Fact]
    public void Validate_WithFtpUrl_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new UrlValidator();

        // Act & Assert
        Assert.Throws<ValidationException>(() => validator.Validate("ftp://example.com/file.txt"));
    }

    [Fact]
    public void IsUrlDuplicate_WithDuplicateUrl_ShouldReturnTrue()
    {
        // Arrange
        var validator = new UrlValidator();
        var url = "https://example.com";
        validator.Validate(url);
        validator.IsUrlDuplicate(url);

        // Act
        var result = validator.IsUrlDuplicate(url);

        // Assert
        result.Should().BeTrue();
    }
}
