using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyWedding.Domain.Interfaces;
using MyWedding.API.Hubs;
using MyWedding.API.Services;
using System.Reflection;
using System.Text.Json.Serialization;
using MyWedding.Identity.Infrastructure;
using MyWedding.Events.Infrastructure;
using System.Net;
using Microsoft.IdentityModel.Logging;
using MyWedding.Tasks.Infrastructure;
using MyWedding.Budget.Infrastructure;
using MyWedding.Vendors.Infrastructure;
using MyWedding.Collaboration.Infrastructure;
using MyWedding.Identity.Application;
using MyWedding.Events.Application;
using MyWedding.Tasks.Application;
using MyWedding.Budget.Application;
using MyWedding.Vendors.Application;
using MyWedding.Collaboration.Application;
using MyWedding.Planner.Application;
using MyWedding.API.Auth.Testing;
using MyWedding.API.Middleware;
using MyWedding.API.Workers;
using MyWedding.SharedKernel.Behaviors;
using MyWedding.SharedKernel.Interfaces;
using MyWedding.Infrastructure.Persistence.Repositories;
using MyWedding.Infrastructure.DependencyInjection;
using MyWedding.Infrastructure.Services;
using System.Text.Encodings.Web;

// Enable TLS 1.2 and 1.3 explicitly for Google Auth connectivity
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { configuration["Frontend:BaseUrl"] ?? "http://localhost:3000" };
var corsPolicyName = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName,
        policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// 2. Add SignalR services
builder.Services.AddSignalR();

// 3. Add Modular Application Services
builder.Services.AddIdentityApplication();
builder.Services.AddEventsApplication();
builder.Services.AddTasksApplication();
builder.Services.AddBudgetApplication();
builder.Services.AddVendorsApplication();
builder.Services.AddCollaborationApplication();
builder.Services.AddPlannerApplication();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// MediatR Pipeline Behaviors (outermost registered last)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PlannerSubscriptionBehavior<,>));

// 4. Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 5. Register Modular Infrastructure
builder.Services.AddIdentityModule();
builder.Services.AddEventsModule();
builder.Services.AddTasksModule();
builder.Services.AddBudgetModule();
builder.Services.AddVendorsModule();
builder.Services.AddCollaborationModule();

builder.Services.AddScoped<ICollaborationService, CollaborationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentPlannerAccessor, CurrentPlannerAccessor>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ICommissionSettlementRepository, CommissionSettlementRepository>();
builder.Services.AddScoped<IWeddingPlannerRepository, WeddingPlannerRepository>();
builder.Services.AddScoped<IPlannerClientEventRepository, PlannerClientEventRepository>();
builder.Services.AddScoped<IPlannerTaskTemplateRepository, PlannerTaskTemplateRepository>();
builder.Services.AddScoped<IPlannerSubscriptionRepository, PlannerSubscriptionRepository>();
builder.Services.AddScoped<IPlannerSubscriptionGate, PlannerSubscriptionGate>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<SubscriptionExpiryWorker>();
}
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

// --- External integrations (SMTP, OpenAI, PayHere) — see docs/REAL_API_SETUP.md ---
builder.Services.AddExternalIntegrations(builder.Configuration);
IntegrationServiceExtensions.LogIntegrationStatus(builder.Configuration);
builder.Services.AddHttpClient<IAiCopilotService, OpenAiCopilotService>();
builder.Services.AddScoped<IPaymentGatewayService, PayHerePaymentGatewayService>();
builder.Services.AddScoped<IPaymentWorkflowService, PaymentWorkflowService>();
builder.Services.AddScoped<IEventAiService, EventAiService>();
builder.Services.AddScoped<IPlannerVendorSuggestionService, PlannerVendorSuggestionService>();
builder.Services.AddScoped<IPlannerWorkspaceService, PlannerWorkspaceService>();
builder.Services.AddScoped<IVendorDashboardService, VendorDashboardService>();

// 6. Initialize Firebase Admin SDK
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.InitializeFirebase(builder.Configuration, builder.Environment.ContentRootPath);
}

// 7. Configure authentication (Test scheme in Testing environment)
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Firebase";
        options.DefaultChallengeScheme = "Firebase";
    })
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", options => { });
}

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MyWedding LK API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Firebase ID token. Example: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// --- HTTP Request Pipeline ---
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseMiddleware<PlannerSubscriptionLockoutMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CollaborationHub>("/hubs/collaboration");
app.MapHub<NotificationHub>("/hubs/notifications");

// Apply migrations and seed reference data (development / first-run)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var applyMigrations = configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
        var seedData = configuration.GetValue("Database:SeedOnStartup", app.Environment.IsDevelopment());
        await DbInitializer.InitializeAsync(context, applyMigrations, seedData);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();

public partial class Program { }

// Custom Authentication Handler using Firebase Admin SDK
public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // SignalR negotiate / SSE / WebSocket pass the Firebase token as access_token on the query string.
        var token = ExtractBearerToken(Request.Headers.Authorization.ToString());
        if (string.IsNullOrEmpty(token) && Request.Query.TryGetValue("access_token", out var queryToken))
            token = queryToken.ToString();

        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        try
        {
            var firebaseToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, firebaseToken.Uid),
                new(System.Security.Claims.ClaimTypes.Email, firebaseToken.Claims.GetValueOrDefault("email")?.ToString() ?? ""),
                new("user_id", firebaseToken.Uid),
                new("firebase_uid", firebaseToken.Uid)
            };

            // Forward Firebase custom claims (e.g. role = "vendor" | "admin")
            if (firebaseToken.Claims.TryGetValue("role", out var roleClaim) && roleClaim is string roleValue)
            {
                claims.Add(new System.Security.Claims.Claim("role", roleValue));
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleValue));
            }

            var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Firebase token validation failed.");
            return AuthenticateResult.Fail(ex);
        }
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader["Bearer ".Length..].Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }
}
