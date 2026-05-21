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
using MyWedding.API.Middleware;
using MyWedding.SharedKernel.Behaviors;
using MyWedding.Infrastructure.Persistence.Repositories;
using System.Text.Encodings.Web;

// Enable TLS 1.2 and 1.3 explicitly for Google Auth connectivity
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
IdentityModelEventSource.ShowPII = true; 

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 1. Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000")
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

// MediatR Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

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
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

// 6. Initialize Firebase Admin SDK
builder.Services.InitializeFirebase(builder.Configuration);

// 7. Configure Custom Firebase Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Firebase";
    options.DefaultChallengeScheme = "Firebase";
})
.AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", options => { });

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

app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CollaborationHub>("/hubs/collaboration");

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

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
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.NoResult();

        string authHeader = Request.Headers["Authorization"]!;
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid Authorization Header");

        string token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            Console.WriteLine("🔑 Custom Auth: Verifying Firebase token...");
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

            Console.WriteLine("✅ Custom Auth: Token Validated Successfully");
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Custom Auth Failed: {ex.Message}");
            return AuthenticateResult.Fail(ex);
        }
    }
}
