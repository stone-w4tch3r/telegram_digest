
---

# 1. Initial prompt

### Brief description

I need to create a simple application that will create a summary digest from multiple Telegram channels and send this digest daily to my email.

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

- Python 13
- FastAPI
- Jinja2
- OpenAI API
- Pydantic
- RSSHub as a way to get RSS feed from Telegram channels
- Docker Compose

### Code style and practices

- Type hints
- Pydantic models
- Static analysis (mypy)
- Linting (ruff)
- Formatting (black)
- Testing (pytest)
- Logging
- Error handling (global exception handlers)
- Readme and documentation
- Feature-based directory structure

### Task

Now let's focus on high-level architecture of project. Project should be simple and time for implementation is one week. List components and their responsibilities and relations, use mermaid diagrams to visualize them.

---

# 2. Contracts

//previous response goes here

Based on our architecture, let's create contracts for each component. Use mermaid diagrams. Remember about code style and practices:

- Type hints
- Pydantic models
- Error handling (global exception handlers)

---

# 3. Contracts restart

Look at the description of an application that I am building:

//readme and architecture goes here

Based on our architecture, let's create contracts for each component. Use mermaid class diagrams syntax. Put all into one diagram. Remember about code style and practices:

- Type hints
- Pydantic models
- Error handling (global exception handlers)

---

# 4. Contracts improvement

Look at the description of an application that I am building:

//readme and architecture goes here

I have created a draft of contracts of an application.

//draft contracts goes here

Finish the contracts. If you have significant improvements, add them.

---

# 5. Implementation - project structure

Look at the description of an application that I am building:

//readme and architecture goes here

Create a project structure for the application. Do not create any files, just structure.

---

# 6. Implementation - files creation

Look at the description of an application that I am building:

//readme and architecture goes here

Create api.py, web_ui.py, models.py, summary_generator.py Use separate code blocks for each file.

Do not be lazy, write all the required code, do not leave any TODOs.