using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TelegramDigest.Backend.Features;
using TelegramDigest.Backend.Models;
using TelegramDigest.Backend.Options;

namespace TelegramDigest.Backend.Tests.UnitTests;

[TestFixture]
public sealed class FileSettingsManagerTests
{
    private Mock<IOptions<BackendDeploymentOptions>> _mockOptions;
    private Mock<ILogger<FileSettingsManager>> _mockLogger;
    private FileSettingsManager _settingsManager;
    private string _settingsFilePath;

    [SetUp]
    public void SetUp()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        var options = new BackendDeploymentOptions
        {
            SettingsFilePath = _settingsFilePath,
            MaxConcurrentAiTasks = default,
            SqlLiteConnectionString = string.Empty,
        };
        _mockOptions = new();
        _mockOptions.Setup(o => o.Value).Returns(options);
        _mockLogger = new();
        _settingsManager = new(_mockOptions.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_settingsFilePath))
        {
            File.Delete(_settingsFilePath);
        }
    }

    private static SettingsModel CreateDefaultSettings() =>
        new(
            "email@example.com",
            new(new(0, 0)),
            new(new("smtp.example.com"), 22, "username", "password", true),
            new("apikey", "model", 2048, new("https://generativelanguage.googleapis.com/v1beta/")),
            new(
                PostSummarySystemPrompt: "You are a summarizer of media posts. Use english language.",
                PostSummaryUserPrompt: new(
                    "Summarize the following post in one sentence:\n\n{Content}"
                ),
                PostImportanceSystemPrompt: "You are a post reviewer.",
                PostImportanceUserPrompt: new(
                    "Please rate the importance of the following post on a scale of 1 to 10, where 1 is least important and 10 is most important.\n\n{Content}"
                ),
                DigestSummarySystemPrompt: "You are a summarizer of big digests. Use english language.",
                DigestSummaryUserPrompt: new("Summarize the digest in one sentence:\n\n{Content}")
            )
        );

    private static JsonObject CreateDefaultSettingsJson()
    {
        var obj = new
        {
            EmailRecipient = "email@example.com",
            DigestTime = "00:00:00",
            SmtpSettings = new
            {
                Host = "smtp.example.com",
                Port = 22,
                Username = "username",
                Password = "password",
                UseSsl = true,
            },
            OpenAiSettings = new
            {
                ApiKey = "apikey",
                Model = "model",
                MaxTokens = 2048,
                Endpoint = "https://generativelanguage.googleapis.com/v1beta/",
            },
            PromptSettings = new
            {
                PostSummarySystemPrompt = "You are a summarizer of media posts. Use english language.",
                PostSummaryUserPrompt = "Summarize the following post in one sentence:\n\n{Content}",
                PostImportanceSystemPrompt = "You are a post reviewer.",
                PostImportanceUserPrompt = "Please rate the importance of the following post on a scale of 1 to 10, where 1 is least important and 10 is most important.\n\n{Content}",
                DigestSummarySystemPrompt = "You are a summarizer of big digests. Use english language.",
                DigestSummaryUserPrompt = "Summarize the digest in one sentence:\n\n{Content}",
            },
        };
        return JsonSerializer.SerializeToNode(obj)!.AsObject();
    }

    [Test]
    public async Task LoadSettings_ShouldCreateAndReturnDefaultSettings_WhenFileDoesNotExist()
    {
        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var settings = result.Value;
        settings.Should().NotBeNull();
        File.Exists(_settingsFilePath).Should().BeTrue();
    }

    [Test]
    public async Task LoadSettings_ShouldLoadAndReturnSettings_AfterSaveSettings()
    {
        // Arrange
        var expectedSettings = CreateDefaultSettings();
        await _settingsManager.SaveSettings(expectedSettings, CancellationToken.None);

        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result
            .Value.Should()
            .BeEquivalentTo(
                expectedSettings,
                options => options.ComparingByMembers<SettingsModel>()
            );
    }

    [Test]
    public async Task LoadSettings_ShouldLoadAndReturnSettings_WhenCorrectJsonFileExists()
    {
        // Arrange
        var json = CreateDefaultSettingsJson();
        await File.WriteAllTextAsync(_settingsFilePath, json.ToString());

        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(CreateDefaultSettings());
    }

    [Test]
    public async Task LoadSettings_ShouldReturnError_WhenFileIsCorrupted()
    {
        // Arrange
        await File.WriteAllTextAsync(_settingsFilePath, "invalid json");

        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Errors.Should()
            .Contain(e => e.Message.Contains("Failed to deserialize settings JSON"));
    }

    [Test]
    public async Task LoadSettings_ShouldReturnError_WhenFileHasMissingFields()
    {
        // Arrange
        var settings = new { EmailRecipient = "test@test.com" };
        var json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_settingsFilePath, json);

        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Errors.Should()
            .Contain(e => e.Message.Contains("Failed to deserialize settings JSON"));
    }

    [Test]
    public async Task LoadSettings_ShouldReturnError_WhenFileHasIncorrectFields()
    {
        // Arrange
        var json = CreateDefaultSettingsJson();
        json["SmtpSettings"]!["Host"] = "incorrect host @#$%";
        await File.WriteAllTextAsync(_settingsFilePath, json.ToString());

        // Act
        var result = await _settingsManager.LoadSettings(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Errors.Should()
            .Contain(e => e.Message.Contains("Looks like settings in json are invalid"));
    }

    [Test]
    public async Task SaveSettings_ShouldCreateFileWithCorrectContent()
    {
        // Arrange
        var settings = CreateDefaultSettings();

        // Act
        var result = await _settingsManager.SaveSettings(settings, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(_settingsFilePath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(_settingsFilePath);
        var loadedSettings = JsonSerializer.Deserialize<JsonElement>(json);

        loadedSettings
            .GetProperty("EmailRecipient")
            .GetString()
            .Should()
            .Be(settings.EmailRecipient);
    }

    [Test]
    public async Task SaveSettings_ShouldCreateDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _settingsFilePath = Path.Combine(directory, "settings.json");
        var options = new BackendDeploymentOptions
        {
            SettingsFilePath = _settingsFilePath,
            MaxConcurrentAiTasks = default,
            SqlLiteConnectionString = string.Empty,
        };
        _mockOptions.Setup(o => o.Value).Returns(options);
        _settingsManager = new(_mockOptions.Object, _mockLogger.Object);

        // Act
        var result = await _settingsManager.SaveSettings(settings, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(directory).Should().BeTrue();
        File.Exists(_settingsFilePath).Should().BeTrue();

        // Cleanup
        Directory.Delete(directory, true);
    }
}
