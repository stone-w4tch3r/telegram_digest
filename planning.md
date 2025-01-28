### 23 jan 2025

| Done | Task                                    | Time  |
|------|-----------------------------------------|-------|
| [x]  | Verify SettingsManager work via swagger | 30m   |
| [x]  | Fix found errors                        | 1h?   |
| [x]  | Verify ChannelReader logic via swagger  | 30m   |
| [x]  | Learn Syndication and implement it      | 1h    |
| [x]  | Check db schema and/or fix it           | 30m   |
| [ ]  | Implement repositories                  | 2h    |
| [ ]  | Add openai docs to CodeGPT              | 30min |
| [ ]  | Implement basic summariser              | 30min |
| [ ]  | Obtain API key and pay                  | 30min |

### TODO

- change line length in csharpier
- improve openapi/swagger support in controller
    - fix error codes
- fix runtime/ folder location
- handle 00:00 in GenerateDailyDigest
- questions about NavProperties:
    - q1: Is it safe/fast/reliable/good sql generated compared to classic approach without navigation properties
    - q2: how I can mark such property so it will be apparent for users that it is navigational
    - q3: in some big EF based projects I have never seen usage of navigational properties. Why? 
- Tests (and interfaces?)
- Check code quality
- implement proper channel deletion