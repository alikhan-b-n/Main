using Lama.Api.Integrations.TelegramBot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lama.Tests.LeadManagement;

public class TelegramBotApiKeyFilterTests
{
    private static IActionResult? Authorize(string configuredKey, string? header, string? value)
    {
        var httpContext = new DefaultHttpContext();
        if (header != null)
            httpContext.Request.Headers[header] = value;

        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        var filter = new TelegramBotApiKeyFilter(
            Options.Create(new TelegramBotOptions { ApiKey = configuredKey }),
            NullLogger<TelegramBotApiKeyFilter>.Instance);

        filter.OnAuthorization(context);
        return context.Result;
    }

    [Fact]
    public void UnconfiguredKey_DisablesEndpoint()
    {
        var result = Assert.IsType<ObjectResult>(Authorize("", "Authorization", "Bearer "));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Authorization", "Bearer wrong")]
    [InlineData("Authorization", "secret")]
    [InlineData("X-Api-Key", "wrong")]
    public void MissingOrWrongKey_IsRejected(string? header, string? value)
    {
        Assert.IsType<UnauthorizedObjectResult>(Authorize("secret", header, value));
    }

    [Theory]
    [InlineData("Authorization", "Bearer secret")]
    [InlineData("Authorization", "bearer  secret ")]
    [InlineData("X-Api-Key", "secret")]
    public void CorrectKey_PassesThrough(string header, string value)
    {
        Assert.Null(Authorize("secret", header, value));
    }
}
