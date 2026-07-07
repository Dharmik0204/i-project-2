using AerodyneCompressors.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Render always sets PORT (default 10000). Must bind to that exact port for health checks.
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
var isRenderDeployment = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT"));
if (isRenderDeployment)
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.PostConfigure<SmtpSettings>(settings =>
{
    settings.Server = string.IsNullOrWhiteSpace(settings.Server) ? "smtp.gmail.com" : settings.Server.Trim();
    settings.SenderEmail = settings.SenderEmail?.Trim() ?? string.Empty;
    settings.ReceiverEmail = settings.ReceiverEmail?.Trim() ?? string.Empty;
    settings.SenderName = settings.SenderName?.Trim() ?? string.Empty;
    settings.Password = settings.Password?.Replace(" ", string.Empty) ?? string.Empty;
});
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = true;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Respond to Render health checks immediately, before any other middleware.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path == "/health" || path == "/health/")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("healthy");
        return;
    }

    await next();
});

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

if (!isRenderDeployment)
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Logger.LogInformation("Listening on http://+:{Port}", port);
app.Run();
