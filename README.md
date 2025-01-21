# Telegram Digest

Telegram Digest is a simple application that will create a summary digest from multiple Telegram channels and send this digest daily to my email.

### Features

- Simple web UI
  - Channels list
  - Settings
  - Digest history
  - Digest page
- Summary generation
  - Summarize posts from channels
  - Evaluate post quality and importance
  - Send summary preview to email
- Email sending
  - Email has only a link to the summary page and minimalistic text, to avoid issues with email rendering
- Telegram channels data
  - Channels are accessible via RSS feed
  - Automatic url generation as `https://rsshub.app/telegram/channel/channelname`
- Management and deployment
  - Settings are stored in json file

### Technologies

- .NET 8
- C# 12
- ASP.NET
- Razor Pages
- EF Core (sqlite)
- Swagger
- RSSHub as a way to get RSS feed from Telegram channels
- Built-in JSON tools
- openai-dotnet package
- FluentResults library for error handling
- System.ServiceModel.Syndication for RSS feed parsing
- System.Net.Mail for email sending
- IHostedService for background scheduling
- Docker Compose

### Code style and practices

- immutable data structures (record types)
- Formatting (CSharpier)
- Testing (nunit)
- Logging
- Result pattern for error handling
- Avoid exception-based error handling
- Global exception handlers
- Readme and documentation
- Flat directory structure in each project
- Avoiding primitive obsession

### Architecture

Application is split into two dotnet projects under one solution.

1. **Application**
   - **Responsibilities:**
     - **Channel Reader:** Reads RSS feeds from Telegram channels.
     - **Summary Generator:** Summarizes posts using the dotnet openai package.
     - **Settings Manager:** Manages application settings stored in a JSON file.
     - **Scheduler:** Schedules daily digest generation and email sending.
     - **Email Sender:** Sends daily digest emails.
     - **Digests Service:** Creates/reads/writes digests/summaries/posts and uses other services to do so.
     - **Database** Stores summaries, channels list, digest history.
     - **Main Service** Coordinates all services and contains business logic.
     - **Public Facade** Serves as dotnet-level API for other dotnet projects.
   - **Technologies:**
     - dotnet openai package for summarization.
     - SMTP client for email sending.
     - Service-Repository pattern for db access.
     - EF Core (sqlite)
2. **Web UI**
   - **Responsibilities:**
     - Renders UI for the application.
     - Interacts with Application via public facade.
   - **Technologies:**
     - Separate dotnet project
     - Razor Pages

```mermaid

graph TB
    subgraph "Web UI Project"
        UI[Razor Pages UI]
        API[API Controllers]
    end

    subgraph "Application Project"
        PF[Public Facade]
        MS[Main Service]

        subgraph "Core Services"
            CR[Channel Reader]
            SG[Summary Generator]
            SM[Settings Manager]
            SCH[Scheduler]
            ES[Email Sender]
            DS[Digests Service]
            REP[Repositories]
        end

        DB[(SQLite Database)]
        JSON[Settings.json]

        RSS[Telegram Channels RSS]
        SMTP[SMTP Server]
        OAI[OpenAI Library]
    end

        

    UI --> API
    API --> PF
    PF --> MS
    MS --> SM & SCH & ES & DS
    CR --> RSS
    SG --> OAI
    ES --> SMTP
    DS --> REP & CR & SG
    REP --> DB
    SM --> JSON
```

### Contracts

```mermaid
classDiagram

    class PublicFacade {
        +GetChannels() Result~List~ChannelDto~~
        +GetChannel(string channelName) Result~ChannelDto~
        +AddChannel(string channelName) Result
        +RemoveChannel(string channelName) Result
        +GetDigestsSummaries() Result~List~DigestPreviewDto~~
        +GetDigest(Guid digestId) Result~DigestDto~
        +GenerateDigest() Result~DigestPreviewDto~
        +GetSettings() Result~SettingsDto~
        +UpdateSettings(SettingsDto settingsDto) Result
    }

    class MainService {
        +ProcessDailyDigest() Result
        +GetChannels() Result~List~ChannelModel~~
        +GetChannel(ChannelId channelId) Result~ChannelModel~
        +AddChannel(ChannelId channelId) Result
        +RemoveChannel(ChannelId channelId) Result
        +GetDigestsSummaries() Result~List~DigestSummaryModel~~
        +GetDigest(DigestId digestId) Result~DigestModel~
        +GenerateDigest() Result~DigestSummaryModel~
        +GetSettings() Result~SettingsModel~
        +UpdateSettings(SettingsUpdateModel settingsUpdateModel) Result
    }

    class ChannelReader {
        +FetchPosts(ChannelId channelId) Result~List~PostModel~~
        +FetchChannelInfo(ChannelId channelId) Result~ChannelModel~
    }

    class SummaryGenerator {
        +GenerateSummary(PostModel post) Result~PostSummaryModel~
        +EvaluatePostImportance(PostModel post) Result~ImportanceModel~
        +GeneratePostsSummary(List~PostModel~ posts) Result~PostsSummaryModel~
    }

    class SettingsManager {
        +LoadSettings() Result~SettingsModel~
        +SaveSettings(SettingsUpdateModel settingsUpdateModel) Result
    }

    class Scheduler {
        +ScheduleDigestGeneration(TimeOnly time)
        +CancelScheduledTasks()
    }

    class EmailSender {
        +SendDigest(DigestSummaryModel DigestSummaryModel, Email emailTo) Result
    }

    class ChannelsService {
        +AddChannel(ChannelId channelId) Result
        +GetChannels() Result~List~ChannelModel~~
        +RemoveChannel(ChannelId channelName) Result
    }

    class DigestsService {
        +GenerateDigest(DateTime from, DateTime to) Result~DigestId~
        +GetDigest(DigestId digestId) Result~DigestModel~
        +GetDigestsSummaries() Result~List~DigestSummaryModel~~
        -GenerateDigestPreview(DigestModel digestModel) Result~DigestSummaryModel~
    }

    class DigestRepository {
        +LoadDigest(DigestId digestId) Result~DigestModel~
        +SaveDigest(DigestModel digestModel) Result
        +LoadAllDigestSummaries() Result~List~DigestSummaryModel~~
        +SaveDigestSummary(DigestSummaryModel DigestSummaryModel) Result
        +CheckIfPostIsSaved(PostId postId) Result~bool~
    }

    class ChannelsRepository {
        +SaveChannel(ChannelModel channelModel) Result
        +LoadChannels() Result~List~ChannelModel~
    }

    class PostImportance {
        <<struct>>
        +int Importance
    }

    class ChannelId {
        <<struct>>
        +string ChannelName
    }

    class DigestId {
        <<struct>>
        +Guid DigestId
    }

    class PostId {
        <<struct>>
        +Guid PostId
    }

    class ChannelModel {
        <<record>>
        +ChannelId ChannelId
        +string Description
        +string Name
        +Url ImageUrl
    }

    class PostModel {
        <<record>>
        +string Title
        +string Description
    }

    class PostSummaryModel {
        <<record>>
        +PostId PostId
        +ChannelId ChannelId
        +string Summary
        +Url Url
        +DateTime PublishedAt
        +ImportanceModel Importance
    }

    class DigestModel {
        <<record>>
        +DigestId DigestId
        +List~PostSummaryModel~ Posts
        +DigestSummaryModel DigestSummary
    }

    class DigestSummaryModel {
        <<record>>
        +DigestId DigestId
        +string Title
        +string PostsSummary
        +int PostsCount
        +int AverageImportance
        +DateTime CreatedAt
        +DateTime DateFrom
        +DateTime DateTo
        +Url ImageUrl
    }

    class SettingsModel {
        <<record>>
        +string EmailRecipient
        +TimeOnly DigestTime
        +SmtpSettingsModel SmtpSettings
        +OpenAiSettingsModel OpenAiSettings
    }

    MainService --> SettingsManager
    MainService --> Scheduler
    MainService --> EmailSender
    MainService --> DigestsService
    MainService --> ChannelsService
    PublicFacade --> MainService
    DigestsService --> DigestRepository
    DigestsService --> SummaryGenerator
    DigestsService --> ChannelReader
    DigestsService --> ChannelsRepository
    ChannelsService --> ChannelsRepository
```

### Project structure

```plaintext
TelegramDigest/
├── .github/
│   └── workflows/
│       └── ci.yml
├── TelegramDigest.Application/
│   ├── Models.cs
│   ├── IdStructs.cs
│   ├── Dtos.cs
│   ├── Database/
│   │   ├── ChannelConfiguration.cs
│   │   ├── DigestConfiguration.cs
│   │   └── DbContext.cs
│   ├── ChannelReader.cs
│   ├── ChannelsService.cs
│   ├── ChannelsRepository.cs
│   ├── DigestsService.cs
│   ├── SummaryGenerator.cs
│   ├── DigestRepository.cs
│   ├── EmailSender.cs
│   ├── SettingsManager.cs
│   ├── PublicFacade.cs
│   ├── MainService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── TelegramDigest.Application.csproj
├── TelegramDigest.Web/
│   └── ...
├── TelegramDigest.Tests/
│   └── ...
├── .gitignore
├── README.md
├── Directory.Build.props
└── TelegramDigest.sln
```
