using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Application.Public;

namespace TelegramDigest.API;

[ApiController]
[Route("api")]
public class Controller(PublicFacade facade) : ControllerBase
{
    [HttpGet("digest-summaries")]
    public async Task<ActionResult<List<DigestSummaryDto>>> GetDigestSummaries()
    {
        var result = await facade.GetDigestSummaries();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("digests/{digestId:guid}")]
    public async Task<ActionResult<DigestDto>> GetDigest(Guid digestId)
    {
        var result = await facade.GetDigest(digestId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("generate-digest")]
    public async Task<ActionResult<DigestSummaryDto>> GenerateDigest()
    {
        var result = await facade.GenerateDigest();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SettingsDto>> GetSettings()
    {
        var result = await facade.GetSettings();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsDto settings)
    {
        var result = await facade.UpdateSettings(settings);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    [HttpGet("channels")]
    public async Task<ActionResult<List<ChannelDto>>> GetChannels()
    {
        var result = await facade.GetChannels();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("channels/{channelName}")]
    public async Task<IActionResult> AddChannel(string channelName)
    {
        var result = await facade.AddChannel(channelName);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    [HttpDelete("channel/{channelName}")]
    public async Task<IActionResult> RemoveChannel(string channelName)
    {
        var result = await facade.RemoveChannel(channelName);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }
}
