### Done

| Done | Task                                                                        | Time  |
|------|-----------------------------------------------------------------------------|-------|
| [x]  | Verify SettingsManager work via swagger                                     | 30m   |
| [x]  | Fix found errors                                                            | 1h?   |
| [x]  | Verify ChannelReader logic via swagger                                      | 30m   |
| [x]  | Learn Syndication and implement it                                          | 1h    |
| [x]  | Check db schema and/or fix it                                               | 30m   |
| [x]  | Implement repositories                                                      | 2h    |
| [x]  | Fix db initialization                                                       | 1h    |
| [x]  | Add openai docs to CodeGPT                                                  | 30min |
| [x]  | Implement basic summariser                                                  | 30min |
| [x]  | Add Razor Pages UI draft                                                    | 1h    |
| [x]  | Fix compile issues in UI                                                    | 1h    |
| [x]  | Learn Razor by TODOs                                                        | 1h    |
| [x]  | Fix error handling                                                          | 1h    |
| [x]  | Fix Digests UI Page                                                         | 1h    |
| [x]  | Fix Digest UI Page                                                          | 1h    |
| [x]  | Fix Channels UI Page                                                        | 1h    |
| [x]  | Fix Settings UI Page                                                        | 1h    |
| [x]  | Learn Razor binding                                                         | 30min |
| [x]  | Move prompt to settings                                                     | 30min |
| [x]  | Add all settings fields in UI                                               | 30min |
| [x]  | Use Backend types in VMs                                                    | 1h    |
| [x]  | Digest publishing to RSS, RSS Validation via https://validator.w3.org/feed/ | 1h    |
| [x]  | Error handling: redirect to base page from handlers (eg add)                | 1h    |
| [x]  | Bug: channel deletion                                                       | 30min |

### High priority

| Done | Task                                                      | Time  |
|------|-----------------------------------------------------------|-------|
| [ ]  | Bug: newlines in UI                                       | 30min |
| [ ]  | Bug: add new chanel: backend error not shown until reload | 30min |
| [ ]  | digest deletion                                           | 30min |
| [ ]  | Async digest generation                                   | 1h    |
| [ ]  | Reimplement and test email sending?                       | 1h    |
| [ ]  | Configure deploy                                          | 2h    |
| [ ]  | Add BaseUrl                                               | 2h    |

### Low priority

| Done | Task                                              | Time  |
|------|---------------------------------------------------|-------|
| [ ]  | Parametrized digest generation                    | 1h    |
| [ ]  | Any RSS: Add any RSS feed in UI                   | 1h    |
| [ ]  | Any RSS: Handle any rss feed in SummaryGenerator  | 1h    |
| [ ]  | Any RSS: rename digest UI items                   | 1h    |
| [ ]  | Add more parameters and jinja template to prompts | 1h    |
| [ ]  | Obtain API key and pay                            | 30min |
| [ ]  | Error handling: exception stacktrace in UI        | 30min |
| [ ]  | Error handling: 404 page                          | 30min |
| [ ]  | Error handling: use Result in frontend            | 1h    |
| [ ]  | Tests: unit                                       | 2h    |
| [ ]  | Tests: integration                                | 2h    |
| [ ]  | Tests: UI                                         | 2h    |

### Future

- change line length in csharpier
- improve openapi/swagger support in controller
    - fix error codes
        - 404 / NotFoundError, OneOf?
- handle 00:00 in GenerateDailyDigest
- Check code quality
- use Optional instead of nulls
- use arrays instead of Lists if data is immutable
- add users and auth
- improve error handling by adding error "location" to UI
- settings migration support
