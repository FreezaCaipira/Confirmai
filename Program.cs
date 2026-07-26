using Confirmai;
using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.EventPayments;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Crypto;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Services.Groups;
using Confirmai.Services.Notification;
using Confirmai.Config;
using Confirmai.Configuration;
using Confirmai.Hubs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog.Sinks.OpenTelemetry;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

// When EfiBank mTLS webhook validation is configured, tell Kestrel to request (not require)
// client certificates so that the webhook handler can validate EfiBank's cert.
// NOTE: in nginx/Caddy deployments, configure ssl_client_certificate + ssl_verify_client optional
// and pass the cert via the X-SSL-Client-Cert header instead of relying on Kestrel.
var efiBankCfg = builder.Configuration.GetSection(EfiBankOptions.Section).Get<EfiBankOptions>();
if (!string.IsNullOrWhiteSpace(efiBankCfg?.WebhookClientCertSubject))
{
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.ConfigureHttpsDefaults(https =>
        {
            https.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate;
        });
    });
}

// -- OpenTelemetry / Serilog OTLP -------------------------------------------
// Config is read first so the OTLP sink can be included in the single logger.
// When Endpoint is empty, OTLP is silently skipped.
var otelSection = builder.Configuration.GetSection(OtelOptions.Section);
var otelOptions = otelSection.Get<OtelOptions>() ?? new OtelOptions();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/Confirmai-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteSerilogOtlpSinkIfEnabled(otelOptions)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<BitcoinQuoteService>();
builder.Services.AddSingleton<CryptoQuoteService>();
builder.Services.AddSingleton<PaymentEventBus>();
builder.Services.AddSingleton<PaymentDomainMetrics>();

builder.Services.AddScoped<IBitcoinPaymentService, BtcPayServerPaymentService>();
builder.Services.AddScoped<IBitcoinPaymentService, TestnetBitcoinPaymentService>();
builder.Services.AddScoped<IBitcoinPaymentService, AbacatePayPixService>();
builder.Services.AddScoped<AbacatePayPixService>();
builder.Services.AddScoped<IEventPaymentGateway, EfiBankEventPaymentGateway>();
builder.Services.AddScoped<IEventPaymentGateway, AbacatePayEventPaymentGateway>();
builder.Services.AddScoped<IEventPaymentGateway, AppmaxEventPaymentGateway>();
builder.Services.AddScoped<BitcoinPaymentFactory>();
builder.Services.AddScoped<EventPaymentGatewayFactory>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<GatewayService>();
builder.Services.AddScoped<PaymentConfirmationService>();
builder.Services.AddScoped<EventPaymentReconciliationService>();
    builder.Services.AddScoped<EventPaymentService>();
builder.Services.AddScoped<EventPaymentChargeCalculator>();
builder.Services.AddScoped<EventConfirmationPaymentStatusService>();
builder.Services.AddScoped<AdminConfirmationService>();
    builder.Services.AddScoped<AdminPaymentsQueryService>();
    builder.Services.AddScoped<AdminPaymentsSummaryService>();
builder.Services.AddScoped<Confirmai.Services.Utility.MailboxQueryService>();
builder.Services.AddScoped<Confirmai.Services.Payment.PaymentInitializationService>();
builder.Services.AddScoped<Confirmai.Services.Payment.PaymentCommandService>();
builder.Services.AddScoped<ReconciliationSeverityEvaluator>();
builder.Services.AddScoped<DelinquencyService>();
builder.Services.AddScoped<PixProofUploadService>();
builder.Services.AddScoped<AppInitializationService>();
builder.Services.AddScoped<Confirmai.Services.Payment.Shared.WebhookPaymentMarker>(sp => 
    new Confirmai.Services.Payment.Shared.WebhookPaymentMarker(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<LogService>(),
        sp.GetRequiredService<PaymentEventBus>(),
        sp.GetService<PayoutService>()));
builder.Services.AddScoped<PayoutService>();
builder.Services.AddScoped<BtcPayWebhookService>();
builder.Services.AddScoped<AbacatePayWebhookService>();
builder.Services.AddScoped<EfiBankPixService>();
builder.Services.AddScoped<IPixPayoutService, EfiBankPixPayoutService>();
builder.Services.AddScoped<AppmaxPixService>();
builder.Services.AddScoped<EfiBankWebhookService>();
builder.Services.AddScoped<CurrencyPreferenceService>();
builder.Services.AddScoped<LanguagePreferenceService>();
builder.Services.AddScoped<UiTextService>(sp => new UiTextService(sp.GetRequiredService<LanguagePreferenceService>()));
builder.Services.AddScoped<DashboardMetricsService>();
builder.Services.AddScoped<GroupMetricsService>();
builder.Services.AddScoped<WhatsAppNotificationService>();
builder.Services.AddScoped<AdminSettingsService>();
builder.Services.AddScoped<AdminRevenueReportService>();
builder.Services.AddScoped<OperationFeeCalculatorService>();
builder.Services.AddScoped<EventNotificationService>();
builder.Services.AddScoped<AdminLogsQueryService>();
builder.Services.AddScoped<AdminLogsExportService>();
builder.Services.AddScoped<AdminLogsFilterStateService>();
builder.Services.AddScoped<AdminUsersFilterStateService>();
 builder.Services.AddScoped<AdminPaymentsFilterStateService>();
builder.Services.AddScoped<EventCollisionService>();
builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider>();
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddHostedService<RachaSchedulerService>();
builder.Services.AddHostedService<EventNotificationSchedulerService>();
builder.Services.AddHostedService<EventPaymentReconciliationWorker>();
builder.Services.AddHostedService<PendingWebhooksAlertService>();
builder.Services.AddHostedService<PayoutRetryService>();
builder.Services.AddHostedService<CertificateHealthCheckService>();
builder.Services.AddScoped<IEmailSender, IdentityEmailSender>();
builder.Services.AddScoped<AdminSecurityPolicyService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!builder.Environment.IsEnvironment("Testing") &&
    (string.IsNullOrWhiteSpace(defaultConnection) || defaultConnection.Contains("__SET_VIA_USER_SECRETS__")))
{
    throw new InvalidOperationException(
        "DefaultConnection n�o configurada. Defina em User Secrets com: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=Confirmai;Username=freeza;Password=...\" --project .\\Confirmai.csproj"
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection), ServiceLifetime.Scoped);

// Configure Data Protection to persist antiforgery tokens in PostgreSQL
// This ensures tokens remain valid across container restarts in production
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

var isDevelopment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
var securityPolicy = SecurityPolicyDefaults.Create(isDevelopment);
var emailEnabled = builder.Configuration.GetSection("Email").GetValue<bool>("Enabled");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = securityPolicy.RequireConfirmedEmail && emailEnabled;
        options.SignIn.RequireConfirmedAccount = true;

        options.Password.RequiredLength = securityPolicy.PasswordRequiredLength;
        options.Password.RequireDigit = securityPolicy.PasswordRequireDigit;
        options.Password.RequireLowercase = securityPolicy.PasswordRequireLowercase;
        options.Password.RequireUppercase = securityPolicy.PasswordRequireUppercase;
        options.Password.RequireNonAlphanumeric = securityPolicy.PasswordRequireNonAlphanumeric;
        options.Password.RequiredUniqueChars = securityPolicy.PasswordRequiredUniqueChars;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = securityPolicy.LockoutMaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(securityPolicy.LockoutMinutes);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ServerAdmin", policy =>
        policy.RequireClaim("server_admin", "true"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Confirmai.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(securityPolicy.SessionTimeoutMinutes);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/Login";
});

// Validate security stamp every 30 seconds so that sessions invalidated via
// UpdateSecurityStampAsync (e.g. game-login flow) are kicked out quickly.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(30);
});

builder.Services.Configure<BtcPayOptions>(builder.Configuration.GetSection("BtcPay"));
builder.Services.Configure<AbacatePayOptions>(builder.Configuration.GetSection(AbacatePayOptions.Section));
builder.Services.Configure<EfiBankOptions>(builder.Configuration.GetSection(EfiBankOptions.Section));
builder.Services.Configure<AppmaxOptions>(builder.Configuration.GetSection(AppmaxOptions.Section));
builder.Services.Configure<FeeOptions>(builder.Configuration.GetSection(FeeOptions.Section));
builder.Services.AddHttpClient("AbacatePay", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AbacatePayOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl)
        ? "https://api.abacatepay.com/v2"
        : opts.BaseUrl);
    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);
});

builder.Services.AddHttpClient("Appmax", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AppmaxOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.BaseUrl)
        ? "https://api.appmax.com.br"
        : opts.BaseUrl);
});

builder.Services.AddHttpClient("AppmaxAuth", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<AppmaxOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(opts.AuthBaseUrl)
        ? "https://auth.appmax.com.br"
        : opts.AuthBaseUrl);
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(ApiKeyAuthDefaults.AuthenticationScheme, _ => { });

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

// S-7: Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("webhook", o =>
    {
        o.PermitLimit = 30;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 20;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("integration", o =>
    {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// -- OpenTelemetry traces + metrics -------------------------------------------
// Only registered when an OTLP endpoint is configured; otherwise the app runs
// normally with console/file logging only (no performance overhead).
builder.Services.Configure<OtelOptions>(otelSection);
if (otelOptions.IsEnabled && Uri.IsWellFormedUriString(otelOptions.Endpoint, UriKind.Absolute))
{
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: otelOptions.ServiceName,
            serviceVersion: otelOptions.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = otelOptions.Environment
        });

    var headers = otelOptions.Headers;

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                // Skip health check and static file spans
                o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                    && !ctx.Request.Path.StartsWithSegments("/_framework")
                    && !ctx.Request.Path.StartsWithSegments("/_blazor");
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otelOptions.Endpoint);
                if (!string.IsNullOrWhiteSpace(headers))
                    o.Headers = headers;
            }))
        .WithMetrics(metrics => metrics
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(PaymentDomainMetrics.MeterName)
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otelOptions.Endpoint);
                if (!string.IsNullOrWhiteSpace(headers))
                    o.Headers = headers;
            }));
}

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// P-1: Response compression (Brotli preferred, Gzip fallback) for text payloads.
// EnableForHttps=true is acceptable here because we don't echo user-controlled
// secret content into compressed responses (BREACH mitigation: no auth tokens
// or per-user secrets are reflected into compressible HTML/JSON bodies).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "text/css",
        "application/json",
        "image/svg+xml"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

var app = builder.Build();

// Guard: EfiBank sandbox must never be enabled in Production.
// Fail fast at startup so a misconfiguration does not silently route real payments to sandbox.
if (!isDevelopment)
{
    var efiBankOpts = app.Services.GetRequiredService<IOptions<EfiBankOptions>>().Value;
    if (efiBankOpts.Sandbox)
        throw new InvalidOperationException(
            "EfiBank:Sandbox=true está ativo em um ambiente não-Development. " +
            "Defina EfiBank:Sandbox=false (ou remova a chave) antes de publicar em produção.");

    // Guard: AbacatePay dev API key should not be used in Production.
    var abacateOpts = app.Services.GetRequiredService<IOptions<AbacatePayOptions>>().Value;
    if (abacateOpts.IsEnabled &&
        abacateOpts.ApiKey!.StartsWith("abc_dev_", StringComparison.OrdinalIgnoreCase))
    {
        Log.Warning(
            "AbacatePay: ApiKey começa com 'abc_dev_' em um ambiente não-Development. " +
            "Substitua pela ApiKey de produção (abc_live_...) antes de processar pagamentos reais.");
    }
}

// S-1: Error handler + HTTPS redirect + HSTS
if (!isDevelopment)
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// S-8: Forward headers from reverse proxy (nginx / Caddy) so the app sees
// real client IPs and the original scheme. Must come before HTTPS redirect.
if (!isDevelopment)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

if (!isDevelopment)
{
    app.UseHttpsRedirection();
}

// P-1: Compress text responses BEFORE static files / endpoints handle them.
// Disabled in Development so dotnet watch can inject its browser-refresh script.
if (!isDevelopment)
{
    app.UseResponseCompression();
}

// S-2: Security response headers + per-request CSP nonce
app.Use(async (context, next) =>
{
    // Generate a 128-bit nonce for inline <script> tags so we can drop
    // 'unsafe-inline' from script-src in our CSP.
    var nonceBytes = RandomNumberGenerator.GetBytes(16);
    var nonce = Convert.ToBase64String(nonceBytes);
    context.Items["csp-nonce"] = nonce;

    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' https://maps.googleapis.com https://maps.gstatic.com; " +
        // style-src keeps 'unsafe-inline' because Blazor injects inline error
        // styles and component styles cannot easily be nonced at this layer.
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: blob: https://maps.gstatic.com https://maps.googleapis.com; " +
        "connect-src 'self' wss: ws: https://maps.googleapis.com; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider(
        new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider().Mappings)
    {
        Mappings = { [".lua"] = "text/plain" }
    },
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            // Always force the canonical filename regardless of the URL used
            var downloadName = ctx.File.Name;
            ctx.Context.Response.Headers["Content-Disposition"] =
                $"attachment; filename=\"{downloadName}\"";
        }
        else if (!isDevelopment)
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000";
        }
    }
});
app.UseRateLimiter();
app.UseRouting();

app.Use(async (context, next) =>
{
    var languagePreference = context.RequestServices.GetRequiredService<LanguagePreferenceService>();
    context.Request.Cookies.TryGetValue("Confirmai.uiLanguage", out var languageFromCookie);
    languagePreference.SetLanguage(languageFromCookie);
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/set-language/{languageCode}", (HttpContext context, string languageCode, string? returnUrl) =>
{
    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "pt-BR",
        "en-US",
        "es-ES"
    };

    var normalized = string.IsNullOrWhiteSpace(languageCode) ? LanguagePreferenceService.DefaultLanguage : languageCode.Trim();
    if (!supported.Contains(normalized))
    {
        normalized = LanguagePreferenceService.DefaultLanguage;
    }

    context.Response.Cookies.Append("Confirmai.uiLanguage", normalized, new CookieOptions
    {
        Path = "/",
        HttpOnly = false,
        IsEssential = true,
        Secure = !isDevelopment,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(365)
    });

    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    if (!Uri.TryCreate(target, UriKind.Relative, out _) || target.StartsWith("//", StringComparison.Ordinal))
    {
        target = "/";
    }
    else if (!target.StartsWith('/'))
    {
        target = "/" + target;
    }

    return Results.LocalRedirect(target);
});

app.MapControllers();
app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.MapPost("/api/btcpay/webhook", async (HttpContext context, BtcPayWebhookService webhookService) =>
{
    return await webhookService.HandleAsync(context);
}).RequireRateLimiting("webhook");

app.MapPost("/api/abacatepay/webhook", async (HttpContext context, AbacatePayWebhookService webhookService) =>
{
    return await webhookService.HandleAsync(context);
}).RequireRateLimiting("webhook");

app.MapPost("/api/webhooks/efibank/pix", async (HttpContext context, EfiBankWebhookService webhookService) =>
{
    return await webhookService.HandleAsync(context);
}).RequireRateLimiting("webhook");

// Serve Pix payment proof images — only to the payer or a group admin
app.MapGet("/api/pix-proof/{id:int}", async (
    int id,
    HttpContext ctx,
    IDbContextFactory<AppDbContext> dbFactory,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.GetUserAsync(ctx.User);
    if (user is null) return Results.Unauthorized();

    await using var db = await dbFactory.CreateDbContextAsync();
    var conf = await db.EventConfirmations
        .Include(c => c.Event)
            .ThenInclude(e => e.Group)
                .ThenInclude(g => g.Members)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (conf is null || conf.PixProofImageData is null || conf.PixProofImageData.Length == 0)
        return Results.NotFound();

    bool isOwner = conf.UserId == user.Id;
    bool isAdmin = conf.Event.Group.Members
        .Any(m => m.UserId == user.Id && m.Role == GroupMemberRole.Admin);
    if (!isOwner && !isAdmin)
        return Results.Forbid();

    var contentType = conf.PixProofContentType ?? "image/jpeg";
    return Results.File(conf.PixProofImageData, contentType);
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    var initializer = scope.ServiceProvider.GetRequiredService<AppInitializationService>();
    await initializer.SeedAsync();

    // Register EfiBank webhook URL so EfiBank knows where to POST Pix notifications.
    // No-op when EfiBank:WebhookUrl is not configured or the service is disabled.
    var efiBankPix = scope.ServiceProvider.GetRequiredService<EfiBankPixService>();
    await efiBankPix.RegisterWebhookAsync();
}

// PaymentHub is a server-to-client notification channel; [Authorize] on the hub ensures only authenticated users connect
app.MapHub<PaymentHub>("/paymentHub");
app.MapHealthChecks("/health");

// ── Dev-only seed endpoints (only registered in Development / Testing) ──────
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    // POST /api/test/seed-event-confirmations?eventId=1
    // Fills the event with all jogadorXX@teste.com + goleiroXX@teste.com seed
    // users, respecting MaxPlayers and MaxGoalkeepers. Idempotent.
    app.MapPost("/api/test/seed-event-confirmations", async (
        int eventId,
        AppDbContext db,
        UserManager<ApplicationUser> userManager) =>
    {
        var ev = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null)
            return Results.NotFound(new { error = $"Evento {eventId} não encontrado." });

        var maxOutfield = ev.MaxPlayers;
        var maxGk = ev.MaxGoalkeepers ?? 0;

        // Collect existing confirmations so we don't double-insert
        var existing = await db.EventConfirmations
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.UserId, c.Position })
            .ToListAsync();

        var existingOutfield = existing.Count(c => c.Position == Confirmai.Models.FutsalPosition.Outfield || c.Position == null);
        var existingGk = existing.Count(c => c.Position == Confirmai.Models.FutsalPosition.Goalkeeper);
        var existingUserIds = existing.Select(c => c.UserId).ToHashSet();

        var added = new List<string>();

        // Fill goalkeepers
        for (int i = 1; i <= 10 && existingGk < maxGk; i++)
        {
            var email = $"goleiro{i:D2}@teste.com";
            var user = await userManager.FindByEmailAsync(email);
            if (user is null || existingUserIds.Contains(user.Id)) continue;
            db.EventConfirmations.Add(new Confirmai.Models.EventConfirmation
            {
                EventId = eventId,
                UserId = user.Id,
                Position = Confirmai.Models.FutsalPosition.Goalkeeper,
                ConfirmedAt = DateTime.UtcNow
            });
            existingGk++;
            existingUserIds.Add(user.Id);
            added.Add(email);
        }

        // Fill outfield
        for (int i = 1; i <= 30 && existingOutfield < maxOutfield; i++)
        {
            var email = $"jogador{i:D2}@teste.com";
            var user = await userManager.FindByEmailAsync(email);
            if (user is null || existingUserIds.Contains(user.Id)) continue;
            db.EventConfirmations.Add(new Confirmai.Models.EventConfirmation
            {
                EventId = eventId,
                UserId = user.Id,
                Position = Confirmai.Models.FutsalPosition.Outfield,
                ConfirmedAt = DateTime.UtcNow
            });
            existingOutfield++;
            existingUserIds.Add(user.Id);
            added.Add(email);
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            eventId,
            added = added.Count,
            users = added,
            totalOutfield = existingOutfield,
            totalGoalkeepers = existingGk
        });
    });
}

app.Run();

public partial class Program { }

