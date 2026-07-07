using AerodyneCompressors.Areas.Identity.Data;
using AerodyneCompressors.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
var isRenderDeployment = !string.IsNullOrWhiteSpace(port);
if (isRenderDeployment)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");
var hasDatabase = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

if (hasDatabase)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

    builder.Services
        .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultUI();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
}

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Text("healthy", "text/plain")).AllowAnonymous();
app.MapGet("/health/", () => Results.Text("healthy", "text/plain")).AllowAnonymous();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (hasDatabase)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            if (await dbContext.Database.CanConnectAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Application database initialized successfully.");
            }
            else
            {
                logger.LogWarning("Database unreachable at startup. Identity is unavailable; site pages and SMTP remain active.");
            }
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database initialization skipped. Site pages and SMTP remain active.");
        }
    });
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("No database connection string configured. Identity is disabled; site pages and SMTP remain active.");
}

app.Logger.LogInformation("AERODYNE Compressors started on {Urls}", isRenderDeployment ? $"http://0.0.0.0:{port}" : "local URLs");
app.Run();
