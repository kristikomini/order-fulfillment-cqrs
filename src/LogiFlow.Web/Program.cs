using System.Globalization;
using LogiFlow.Web.Components;
using LogiFlow.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

// Pin a fixed culture so currency/number formatting is deterministic regardless
// of the host's locale. Containers default to the invariant culture, which
// renders the generic "¤" currency symbol; this makes prices render as euro.
var culture = CultureInfo.GetCultureInfo("it-IT");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Behind Coolify's Traefik proxy, TLS is terminated upstream and plain HTTP is
// forwarded. Honour X-Forwarded-Proto/For so the app sees the real scheme and
// client IP (and HTTPS redirection doesn't loop).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed client pointed at the LogiFlow API.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/";
builder.Services.AddHttpClient<LogiFlowApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
