using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Day16Demo.Auth;
using Day16Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "CampusHub";
})
// The cookie handler owns BOTH failure outcomes, and they are different:
//   not signed in       -> CHALLENGE  -> redirect to the provider (DefaultChallengeScheme)
//   signed in, denied   -> FORBID     -> redirect to AccessDeniedPath, below.
// Without this line that second redirect goes to the framework default
// "/Account/AccessDenied", which this app never maps -- so a forbidden user gets a
// blank 404 instead of an explanation. Authorization worked; only the telling failed.
.AddCookie(options =>
{
    options.AccessDeniedPath = "/access-denied";
})
.AddOpenIdConnect("CampusHub", options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = "code";

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.CallbackPath = "/callback";
    options.ClaimsIssuer = "CampusHub";
    options.SaveTokens = true;
    // Keep the provider's claim names (sub, email) instead of renaming them to long
    // Microsoft URIs -- without this line, User.FindFirst("email") returns null.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        // RoleClaimType is the linchpin of role-based auth. Our provider emits roles under a
        // custom namespaced claim ("https://sdev2351.example.com/roles"); this line tells
        // ASP.NET Core to treat that claim AS the user's roles. After it,
        // [Authorize(Roles="Admin")] and <AuthorizeView Roles="..."> just work. Omit it
        // and the framework looks for its own ClaimTypes.Role, finds nothing, and every
        // role check silently denies — no error, just an empty/forbidden page.
        //
        // You can READ the code that mints it. In the AuthServer:
        //   Seed/Clients.cs      -- LessonRolesClaim declares the namespace, once
        //   Identity/ClaimsFactory.cs:54 -- identity.AddClaim(new Claim(profile.RolesClaimType, role))
        // The string below and the string there must agree. That is the entire contract,
        // and it is the one both sides of an OIDC handshake have to be told about
        // separately — which is precisely why getting it wrong fails silently.
        RoleClaimType = "https://sdev2351.example.com/roles"
    };
});

builder.Services.AddCascadingAuthenticationState();

// Day-14 called AddAuthorization() with no arguments — it only needed [Authorize].
// Today we register a named POLICY. "CanManageInventory" is satisfied when a
// DepartmentRequirement("Inventory") holds. A policy is the seam between a page that
// says [Authorize(Policy="...")] and the handler that decides — the page references
// only the name, never the rule, so the rule can change without touching the page.
builder.Services.AddAuthorization(options =>
{
    // The name pages use in [Authorize(Policy = "CanManageInventory")]
    options.AddPolicy("CanManageInventory", policy =>
        // Passes only when the user's department claim is Inventory
        policy.Requirements.Add(new DepartmentRequirement("Inventory")));
});

// The requirement only NAMES what to check; the handler does the checking. Register it
// or the policy can never be satisfied (no handler runs → access is always denied).
// Handlers are stateless, so a singleton is the right lifetime.
builder.Services.AddSingleton<IAuthorizationHandler, DepartmentHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
{
    // Open-redirect guard: only follow a relative returnUrl like "/profile".
    // A returnUrl such as https://evil.com would otherwise send the user to an
    // attacker-controlled site after login.
    // NOTE: this simple check still allows "//evil.com"; production code should use
    // Url.IsLocalUrl(returnUrl), which also rejects protocol-relative URLs.
    if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";
    }

    await httpContext.ChallengeAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = returnUrl
    });
});

app.MapGet("/Account/Logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignOutAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.Run();
