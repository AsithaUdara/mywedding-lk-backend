// File: src/Presentation/MyWedding.API/Program.cs

using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWedding.Application.Features.Users.Commands.SyncUser;
using MyWedding.Domain.Interfaces;
using MyWedding.Infrastructure.Authentication;
using MyWedding.Infrastructure.Persistence;
using MyWedding.Infrastructure.Persistence.Repositories;

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
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>()); // <-- FIXED LINE

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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Configure the HTTP request pipeline. ---
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

app.Run();