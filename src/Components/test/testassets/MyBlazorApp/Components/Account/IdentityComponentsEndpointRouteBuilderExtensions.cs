using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MyBlazorApp.Components.Account.Pages;
using MyBlazorApp.Components.Account.Pages.Manage;
using MyBlazorApp.Data;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query = [
                new("ReturnUrl", returnUrl),
                new("Action", ExternalLogin.LoginCallbackAction)];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        accountGroup.MapPost("/MakeCredentialCreationOptions", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] IOptions<JsonOptions> jsonOptions) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var options = new CredentialCreationOptions
            {
                Mediation = CredentialMediationRequirement.Required,
                PublicKey = new PublicKeyCredentialCreationOptions
                {
                    Rp = new PublicKeyCredentialRpEntity
                    {
                        Id = "MyBlazorApp",
                        Name = "My Blazor App",
                    },
                    User = new PublicKeyCredentialUserEntity
                    {
                        Id = user.Id,
                        Name = user.UserName,
                        DisplayName = user.UserName,
                    },
                    Challenge = [117, 61, 252, 231, 191, 241], // TODO: Generate a real challenge value
                    PubKeyCredParams =
                    [
                        new PublicKeyCredentialParameters
                        {
                            Type = "public-key",
                            Alg = -7,
                        },
                    ],
                },
            };

            // TODO: Figure out how to configure session storage.
            // But should we be using session storage anyway? Shouldn't this stay completely server-side?
            // Yes, it should. Putting the challenge in session storage is a security risk.
            // Okay, but session storage _is_ stored on the server. They key is stored in a cookie on the client.
            //
            context.Session.SetString("CredentialCreationOptions", JsonSerializer.Serialize(options, jsonOptions.Value.JsonSerializerOptions));

            // TODO: Handle the case where the user already has a passwordless login enabled.
            return Results.Ok(options);
        });

        accountGroup.MapPost("/RegisterCredential", (
            HttpContext context,
            [FromBody] AuthenticatorAttestationResponse attestationResponse,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] IOptions<JsonOptions> jsonOptions) =>
        {
            // 7.1.6. Let 'C' be the client data claimed as collected during the credential creation,
            //        be the result of running an implementation-specific JSON parser on JSONtext.
            var jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
            var clientData = JsonSerializer.Deserialize<CollectedClientData>(attestationResponse.ClientDataJson, jsonSerializerOptions);

            // 7.1.7. Verify that the value of C.type is 'webauthn.create'
            if (!string.Equals(clientData.Type, "webauthn.create", StringComparison.Ordinal))
            {
                return Results.BadRequest("Invalid client data type.");
            }

            // 7.1.8. Verify that the value of 'C.challenge' equals the base64url encoding of 'pkOptions.challenge'.
            var originalOptionsJson = context.Session.GetString("CredentialCreationOptions");
            var originalOptions = JsonSerializer.Deserialize<CredentialCreationOptions>(originalOptionsJson, jsonSerializerOptions);
            if (originalOptions is null)
            {
                return Results.BadRequest("Invalid session state.");
            }

            var challengeBytes = System.Buffers.Text.Base64Url.DecodeFromChars(clientData.Challenge);
            if (!challengeBytes.SequenceEqual(originalOptions.PublicKey.Challenge))
            {
                return Results.BadRequest("Invalid challenge value.");
            }

            // TODO: Verify the origin of the request, etc.

            // TODO (YAH): Obtain the attestation object, perform CBOR decoding, get the authenticator data,
            // and then get the public key from that authenticator data.
            // For now, just add the public key to the user's account and skip validation so that we can test the rest of the flow.
            // Also, we'll want to perform the deserialization of the client JSON here, because we eventually need to compute a hash of it anyway.

            var reader = new System.Formats.Cbor.CborReader(attestationResponse.AttestationObjectCbor);

            // var user = await userManager.GetUserAsync(context.User);
            // await userManager.SetPublicKeyAsync(user, );

            return Results.Ok();
        });

        var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        manageGroup.MapPost("/LinkExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider) =>
        {
            // Clear the existing external cookie to ensure a clean login process
            await context.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/Manage/ExternalLogins",
                QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
            return TypedResults.Challenge(properties, [provider]);
        });

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

        manageGroup.MapPost("/DownloadPersonalData", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
            var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

            context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
            return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
        });

        return accountGroup;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    private enum CredentialMediationRequirement
    {
        Silent,
        Optional,
        Conditional,
        Required,
    }

    // 2.4. The CredentialCreationOptions dictionary
    private sealed class CredentialCreationOptions
    {
        public CredentialMediationRequirement Mediation { get; set; }

        // 5.1.1. CredentialCreationOptions Dictionary Extension
        public PublicKeyCredentialCreationOptions PublicKey { get; set; }
    }

    // 5.4. Options for Credential Creation
    private sealed class PublicKeyCredentialCreationOptions
    {
        public required PublicKeyCredentialRpEntity Rp { get; set; }

        public required PublicKeyCredentialUserEntity User { get; set; }

        public required byte[] Challenge { get; set; }

        public required PublicKeyCredentialParameters[] PubKeyCredParams { get; set; }
    }

    // 5.4.1. Public Key Entity Description
    private class PublicKeyCredentialEntity
    {
        public required string Name { get; set; }
    }

    // 5.4.2. Relying Party Parameters for Credential Generation
    private sealed class PublicKeyCredentialRpEntity : PublicKeyCredentialEntity
    {
        public required string Id { get; set; }
    }

    // 5.4.3. User Account Parameters for Credential Generation
    private sealed class PublicKeyCredentialUserEntity : PublicKeyCredentialEntity
    {
        public required string Id { get; set; }

        public required string DisplayName { get; set; }
    }

    // 5.3. Parameters for Credential Generation
    private sealed class PublicKeyCredentialParameters
    {
        public required string Type { get; set; }

        public required long Alg { get; set; }
    }

    // 5.8.1. Client Data Used in WebAuthn Signatures
    private sealed class CollectedClientData
    {
        public required string Type { get; set; }

        public required string Challenge { get; set; }

        public required string Origin { get; set; }

        public bool CrossOrigin { get; set; }

        public string? TopOrigin { get; set; }
    }

    private sealed class AuthenticatorAttestationResponse
    {
        // public required CollectedClientData ClientData { get; set; }
        public required string ClientDataJson { get; set; }

        public required byte[] AttestationObjectCbor { get; set; }
    }

    private sealed class AttestationRawResponse
    {
        public required byte[] Id { get; set; }

        public required byte[] RawId { get; set; }

        public AttestationResponse Response { get; set; }

        public sealed class AttestationResponse
        {
            [JsonPropertyName("clientDataJSON")]
            public byte[] ClientDataJson { get; set; }

            [JsonPropertyName("attestationObject")]
            public byte[] AttestationObject { get; set; }
        }
    }
}
