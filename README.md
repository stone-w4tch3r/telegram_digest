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
- openai-dotnet package
- EF Core (sqlite)
- Swagger
- RSSHub as a way to get RSS feed from Telegram channels
- Built-in JSON tools
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
            DS[Digests Servise]
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

    class IPublicFacade {
        <<interface>>
        +GetChannels() Result~List~Channel~~
        +AddChannel(Channel) Result~Unit~
        +RemoveChannel(string channelId) Result~Unit~
        +GetDigests() Result~List~Digest~~
        +GetDigest(string digestId) Result~Digest~
        +GetSettings() Result~Settings~
        +UpdateSettings(Settings) Result~Unit~
    }

    class IMainService {
        <<interface>>
        +ProcessDailyDigest() Result~Digest~
        +ManageChannels(ChannelOperation) Result~Unit~
        +UpdateSettings(Settings) Result~Unit~
    }

    class IChannelReader {
        <<interface>>
        +FetchPosts(string channelUrl) Result~List~Post~~
        +ValidateChannel(string channelUrl) Result~bool~
    }

    class ISummaryGenerator {
        <<interface>>
        +GenerateSummary(List~Post~ posts) Result~Summary~
        +EvaluatePostImportance(Post) Result~int~
    }

    class ISettingsManager {
        <<interface>>
        +LoadSettings() Result~Settings~
        +SaveSettings(Settings) Result~Unit~
    }

    class IScheduler {
        <<interface>>
        +ScheduleDigestGeneration(DateTime time)
        +CancelScheduledTasks()
    }

    class IEmailSender {
        <<interface>>
        +SendDigest(Digest digest, string emailTo) Result~Unit~
    }

    class IDigestsService {
        <<interface>>
        +CreateDigest(List~Post~ posts) Result~Digest~
        +GetDigest(string digestId) Result~Digest~
        +GetAllDigests() Result~List~Digest~~
    }

    class IRepository~T~ {
        <<interface>>
        +GetById(string id) Result~T~
        +GetAll() Result~List~T~~
        +Add(T entity) Result~Unit~
        +Update(T entity) Result~Unit~
        +Delete(string id) Result~Unit~
    }

    class Channel {
        <<record>>
        +string Id
        +string Name
        +string RssUrl
        +DateTime LastFetched
    }

    class Post {
        <<record>>
        +string Id
        +string ChannelId
        +string Content
        +DateTime PublishedAt
        +int Importance
    }

    class Digest {
        <<record>>
        +string Id
        +DateTime CreatedAt
        +List~Post~ Posts
        +Summary Summary
    }

    class Summary {
        <<record>>
        +string Id
        +string Content
        +int QualityScore
    }

    class Settings {
        <<record>>
        +string EmailRecipient
        +TimeSpan DigestTime
        +SmtpSettings SmtpSettings
        +OpenAiSettings OpenAiSettings
    }

    class Result~T~ {
        <<record>>
        +bool IsSuccess
        +T Value
        +string Error
    }

    IMainService --> IChannelReader
    IMainService --> ISummaryGenerator
    IMainService --> ISettingsManager
    IMainService --> IScheduler
    IMainService --> IEmailSender
    IMainService --> IDigestsService
    IPublicFacade --> IMainService
    IDigestsService --> IRepository

```

### Project structure
