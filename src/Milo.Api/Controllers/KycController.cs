using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milo.Application.Kyc.Commands.VerifyKyc;
using Milo.Application.Kyc.Queries.GetKycStatus;

namespace Milo.Api.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize(Roles = "Guest")]
public sealed class KycController(ISender sender) : ControllerBase
{
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyKycRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyKycCommand(body.ImageUrl), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status409Conflict);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await sender.Send(new GetKycStatusQuery(), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }
}

public record VerifyKycRequest(string ImageUrl);
