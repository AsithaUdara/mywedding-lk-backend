using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWedding.API.Middleware;
using MyWedding.Application.Features.Users.Commands.SyncUser;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Authentication;
using MyWedding.Infrastructure.Persistence;
using MyWedding.Infrastructure.Persistence.Repositories;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- Define the CORS policy name ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- Add services to the container. ---

// 1. Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000") // Your frontend's address
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 2. Add MediatR for Application layer
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SyncUserCommand).Assembly));

// 3. Add DbContext for Infrastructure layer
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 4. Register Repositories and Unit of Work
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWeddingEventRepository, WeddingEventRepository>();
builder.Services.AddScoped<IEventOrganizerRepository, EventOrganizerRepository>();
builder.Services.AddScoped<IEventTaskRepository, EventTaskRepository>();
builder.Services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IVendorServiceRepository, VendorServiceRepository>();
builder.Services.AddScoped<IVendorBookingRepository, VendorBookingRepository>();
builder.Services.AddScoped<IActivityFeedRepository, ActivityFeedRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();
builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

// 5. Initialize Firebase Admin SDK
builder.Services.InitializeFirebase(builder.Configuration);

// 6. Configure JWT Bearer Authentication from Firebase
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var firebaseProjectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrEmpty(firebaseProjectId))
        {
            throw new InvalidOperationException("Firebase ProjectId is not configured in appsettings.json.");
        }

        options.Authority = "https://securetoken.google.com/" + firebaseProjectId;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/" + firebaseProjectId,
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Accept both camelCase and PascalCase from clients
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MyWedding LK API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// --- HTTP Request Pipeline ---
app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 7. Use the CORS policy - IMPORTANT: This goes before Authentication/Authorization
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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