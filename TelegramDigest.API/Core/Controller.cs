using Microsoft.AspNetCore.Mvc;

namespace TelegramDigest.API.Core;

[ApiController]
[Route("api")]
public class Controller(IApplicationFacade applicationFacade) : ControllerBase
{
    [HttpGet("digest-summaries")]
    public async Task<ActionResult<List<DigestSummaryDto>>> GetDigestSummaries()
    {
        var result = await applicationFacade.GetDigestSummaries();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("digests/{digestId:guid}")]
    public async Task<ActionResult<DigestDto>> GetDigest(Guid digestId)
    {
        var result = await applicationFacade.GetDigest(digestId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("generate-digest")]
    public async Task<ActionResult<DigestGenerationDto>> GenerateDigest()
    {
        var result = await applicationFacade.GenerateDigest();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SettingsDto>> GetSettings()
    {
        var result = await applicationFacade.GetSettings();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsDto settings)
    {
        var result = await applicationFacade.UpdateSettings(settings);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    [HttpGet("channels")]
    public async Task<ActionResult<List<ChannelDto>>> GetChannels()
    {
        var result = await applicationFacade.GetChannels();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("channels/{channelName}")]
    public async Task<IActionResult> AddChannel(string channelName)
    {
        var result = await applicationFacade.AddOrUpdateChannel(channelName);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    [HttpDelete("channel/{channelName}")]
    public async Task<IActionResult> RemoveChannel(string channelName)
    {
        var result = await applicationFacade.RemoveChannel(channelName);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }
}
