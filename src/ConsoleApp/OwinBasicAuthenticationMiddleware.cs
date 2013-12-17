using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace Common
{
    public class BasicAuthenticationOptions : AuthenticationOptions
    {
        public Func<string, string, Task<AuthenticationTicket>> ValidateCredentials { get; private set; }
        public string Realm { get; private set; }

        public BasicAuthenticationOptions(string realm, Func<string, string, Task<AuthenticationTicket>> validateCredentials)
            : base("Basic")
        {
            Realm = realm;
            ValidateCredentials = validateCredentials;
        }
    }

    class BasicAuthnMiddleware : AuthenticationMiddleware<BasicAuthenticationOptions>
    {
        public BasicAuthnMiddleware(OwinMiddleware next, BasicAuthenticationOptions options)
            : base(next, options)
        { }

        protected override AuthenticationHandler<BasicAuthenticationOptions> CreateHandler()
        {
            return new BasicAuthenticationHandler(Options);
        }
    }

    class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        private readonly string _challenge;

        public BasicAuthenticationHandler(BasicAuthenticationOptions options)
        {
            _challenge = "Basic realm=" + options.Realm;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var authzValue = Request.Headers.Get("Authorization");
            if (string.IsNullOrEmpty(authzValue) || !authzValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var token = authzValue.Substring("Basic ".Length).Trim();
            return await token.TryGetPrincipalFromBasicCredentialsUsing(Options.ValidateCredentials);
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);
                if (challenge != null)
                {
                    Response.Headers.AppendValues("WWW-Authenticate", _challenge);
                }
            }
            return Task.FromResult<object>(null);
        }
    }

    public static class BasicAuthnMiddlewareExtensions
    {
        public static IAppBuilder UseBasicAuthentication(this IAppBuilder app, BasicAuthenticationOptions options)
        {
            return app.Use(typeof(BasicAuthnMiddleware), options);
        }
    }

    public static class BasicAuthenticationExtensions
    {
        public static bool HasAuthorizationHeaderWithBasicScheme(this HttpRequestMessage req)
        {
            return req.Headers.Authorization != null
                   && req.Headers.Authorization.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<TPrinc> TryGetPrincipalFromBasicCredentialsUsing<TPrinc>(this string credentials,
            Func<string, string, Task<TPrinc>> validate) where TPrinc : class
        {
            string pair;
            try
            {
                pair = Encoding.UTF8.GetString(
                    Convert.FromBase64String(credentials));
            }
            catch (FormatException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            var ix = pair.IndexOf(':');
            if (ix == -1) return null;
            var username = pair.Substring(0, ix);
            var pw = pair.Substring(ix + 1);
            return await validate(username, pw);
        }

        public static Task<ClaimsPrincipal> TryGetPrincipalFromBasicCredentialsUsing(
            this HttpRequestMessage req,
            Func<string, string, Task<ClaimsPrincipal>> validate)
        {
            return req.Headers.Authorization.Parameter.TryGetPrincipalFromBasicCredentialsUsing(validate);
        }
    }
}
