using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using TelegramDigest.Backend.Core;
using TelegramDigest.Backend.Db;

namespace TelegramDigest.Backend.Tests.UnitTests;

[TestFixture]
public class DigestServiceTests
{
    private Mock<IDigestRepository> _digestRepositoryMock;
    private Mock<IFeedReader> _feedReaderMock;
    private Mock<IFeedsRepository> _feedsRepositoryMock;
    private Mock<IAiSummarizer> _aiSummarizerMock;
    private Mock<IDigestStepsService> _digestStepsServiceMock;
    private Mock<ILogger<DigestService>> _loggerMock;

    private DigestService _digestService;

    private readonly DateOnly _dateFrom = new(2023, 1, 1);
    private readonly DateOnly _dateTo = new(2023, 1, 2);

    [SetUp]
    public void SetUp()
    {
        _digestRepositoryMock = new();
        _feedReaderMock = new();
        _feedsRepositoryMock = new();
        _aiSummarizerMock = new();
        _digestStepsServiceMock = new();
        _loggerMock = new();

        _digestService = new(
            _digestRepositoryMock.Object,
            _feedReaderMock.Object,
            _feedsRepositoryMock.Object,
            _aiSummarizerMock.Object,
            _digestStepsServiceMock.Object,
            _loggerMock.Object
        );
    }

    // High Importance Tests

    [Test]
    public async Task GenerateDigest_WithValidInputs_SavesDigest()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feeds = new List<FeedModel>
        {
            new(feedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(feedUrl, new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedUrl,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        var savedDigest = (DigestModel?)null;

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(It.IsAny<List<PostModel>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x => x.SaveDigest(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, ct) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        _digestStepsServiceMock.Verify(
            x => x.AddStep(It.Is<SimpleStepModel>(s => s.Type == DigestStepTypeModelEnum.Success)),
            Times.Once
        );
        _digestRepositoryMock.Verify(
            x =>
                x.SaveDigest(
                    It.Is<DigestModel>(d => d.DigestId == digestId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
    }

    [Test]
    public async Task GenerateDigest_WithNoPostsFound_ReturnsNoPosts()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feeds = new List<FeedModel>
        {
            new(feedUrl, "desc", "title", new("https://example.com/image.png")),
        };

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(new List<PostModel>()));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.NoPosts);
        _digestStepsServiceMock.Verify(
            x =>
                x.AddStep(
                    It.Is<SimpleStepModel>(s => s.Type == DigestStepTypeModelEnum.NoPostsFound)
                ),
            Times.Once
        );
        _digestRepositoryMock.Verify(
            x =>
                x.SaveDigest(
                    It.Is<DigestModel>(d => d.DigestId == digestId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task GenerateDigest_WithSomeFeedsFailing_StillProcessesSuccessfully()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var successFeedUrl = new FeedUrl("https://success.com/feed");
        var failFeedUrl = new FeedUrl("https://fail.com/feed");
        var feeds = new List<FeedModel>
        {
            new(successFeedUrl, "desc", "title", new("https://example.com/image.png")),
            new(failFeedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(successFeedUrl, new("content"), new("https://success.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            successFeedUrl,
            "summary",
            new("https://success.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        var savedDigest = (DigestModel?)null;

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    successFeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    failFeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<List<PostModel>>("Failed to fetch"));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(It.IsAny<List<PostModel>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x => x.SaveDigest(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, ct) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
    }

    [Test]
    public async Task GenerateDigest_WithFeedFilter_ProcessesOnlySelectedFeeds()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var selectedFeedUrl = new FeedUrl("https://selected.com/feed");
        var notSelectedFeedUrl = new FeedUrl("https://not-selected.com/feed");
        var filter = new DigestFilterModel(
            _dateFrom,
            _dateTo,
            new[] { selectedFeedUrl }.ToHashSet()
        );
        var feeds = new List<FeedModel>
        {
            new(selectedFeedUrl, "desc", "title", new("https://example.com/image.png")),
            new(notSelectedFeedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(
                selectedFeedUrl,
                new("content"),
                new("https://selected.com/post1"),
                DateTime.UtcNow
            ),
        };
        var postSummary = new PostSummaryModel(
            selectedFeedUrl,
            "summary",
            new("https://selected.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );
        var savedDigest = (DigestModel?)null;

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    selectedFeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(It.IsAny<List<PostModel>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x => x.SaveDigest(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok())
            .Callback<DigestModel, CancellationToken>((digest, ct) => savedDigest = digest);

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DigestGenerationResultModelEnum.Success);
        _feedReaderMock.Verify(
            x =>
                x.FetchPosts(
                    selectedFeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _feedReaderMock.Verify(
            x =>
                x.FetchPosts(
                    notSelectedFeedUrl,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        savedDigest.Should().NotBeNull();
        savedDigest.DigestId.Should().Be(digestId);
        savedDigest.PostsSummaries.Should().ContainSingle().And.Contain(postSummary);
        savedDigest.DigestSummary.Should().BeEquivalentTo(digestSummary);
    }

    // Medium Importance Tests

    [Test]
    public async Task GenerateDigest_WhenFeedsRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<List<FeedModel>>("Failed to load feeds"));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Failed to load feeds");
    }

    [Test]
    public async Task GenerateDigest_WithNoFeedsSelected_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo, new HashSet<FeedUrl>());
        var feeds = new List<FeedModel>
        {
            new(
                new("https://example.com/feed"),
                "desc",
                "title",
                new("https://example.com/image.png")
            ),
        };

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("No feeds selected");
    }

    [Test]
    public async Task GenerateDigest_WhenAllFeedsFailToLoad_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feeds = new List<FeedModel>
        {
            new(
                new("https://fail1.com/feed"),
                "desc",
                "title",
                new("https://example.com/image.png")
            ),
            new(
                new("https://fail2.com/feed"),
                "desc",
                "title",
                new("https://example.com/image.png")
            ),
        };

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Fail<List<PostModel>>("Failed to fetch"));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Failed to read all feeds");
    }

    [Test]
    public async Task GenerateDigest_WhenAiSummarizerFailsOnPostSummary_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feeds = new List<FeedModel>
        {
            new(feedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(feedUrl, new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<PostSummaryModel>("AI failure"));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("AI failure");
    }

    [Test]
    public async Task GenerateDigest_WhenAiSummarizerFailsOnDigestSummary_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feeds = new List<FeedModel>
        {
            new(feedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(feedUrl, new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedUrl,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(It.IsAny<List<PostModel>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Fail<DigestSummaryModel>("AI failure"));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("AI failure");
    }

    [Test]
    public async Task GenerateDigest_WhenDigestRepositoryFailsToSave_ReturnsFailure()
    {
        // Arrange
        var digestId = new DigestId(Guid.NewGuid());
        var filter = new DigestFilterModel(_dateFrom, _dateTo);
        var feedUrl = new FeedUrl("https://example.com/feed");
        var feeds = new List<FeedModel>
        {
            new(feedUrl, "desc", "title", new("https://example.com/image.png")),
        };
        var posts = new List<PostModel>
        {
            new(feedUrl, new("content"), new("https://example.com/post1"), DateTime.UtcNow),
        };
        var postSummary = new PostSummaryModel(
            feedUrl,
            "summary",
            new("https://example.com/post1"),
            DateTime.UtcNow,
            new(5)
        );
        var digestSummary = new DigestSummaryModel(
            digestId,
            "title",
            "summary",
            1,
            5,
            DateTime.UtcNow,
            _dateFrom.ToDateTime(TimeOnly.MinValue),
            _dateTo.ToDateTime(TimeOnly.MinValue)
        );

        _feedsRepositoryMock
            .Setup(x => x.LoadFeeds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(feeds));
        _feedReaderMock
            .Setup(x =>
                x.FetchPosts(
                    It.IsAny<FeedUrl>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Ok(posts));
        _aiSummarizerMock
            .Setup(x => x.GenerateSummary(It.IsAny<PostModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(postSummary));
        _aiSummarizerMock
            .Setup(x =>
                x.GeneratePostsSummary(It.IsAny<List<PostModel>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok(digestSummary));
        _digestRepositoryMock
            .Setup(x => x.SaveDigest(It.IsAny<DigestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("DB error"));

        // Act
        var result = await _digestService.GenerateDigest(digestId, filter, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("DB error");
    }
}
