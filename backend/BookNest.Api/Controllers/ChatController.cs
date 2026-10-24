using BookNest.Api.DTOs;
using BookNest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookNest.Api.Controllers;
[ApiController, Route("api/chat"), EnableRateLimiting("chat")]
public class ChatController(ChatService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask(ChatDto dto, CancellationToken ct) => Ok(await service.Ask(dto.Message, ct));
}