using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PaymentShipping.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(sub, out var userId)) return userId;
            return 1;
        }
    }

    protected string CurrentCorrelationId =>
        HttpContext.Items["X-Correlation-Id"]?.ToString() ?? HttpContext.TraceIdentifier;

    protected string CurrentTransactionId =>
        HttpContext.Items["X-Transaction-Id"]?.ToString() ?? CurrentCorrelationId;
}
