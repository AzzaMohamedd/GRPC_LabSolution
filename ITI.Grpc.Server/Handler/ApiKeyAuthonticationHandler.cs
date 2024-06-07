using ITI.Grpc.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ITI.Grpc.Server.Handler
{
    public class ApiKeyAuthonticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IApiKeyAuthenticationService _apiKeyAuthenticationService;
        public ApiKeyAuthonticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IApiKeyAuthenticationService apiKeyAuthenticationService) : base(options, logger, encoder, clock)
        {
            _apiKeyAuthenticationService = apiKeyAuthenticationService;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var isAuthenticated = _apiKeyAuthenticationService.Authenticate();

            if (!isAuthenticated)
                return Task.FromResult(AuthenticateResult.Fail("API Key is Invalid"));

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "API UName"),
                new Claim(ClaimTypes.Role, "API User")
            };

            var id = new ClaimsIdentity(claims, Scheme.Name);

            var principal = new ClaimsPrincipal(id);

            var tick = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(tick));

        }
    }
}
