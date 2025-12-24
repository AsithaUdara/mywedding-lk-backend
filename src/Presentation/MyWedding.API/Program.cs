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

// --- Add services to the container. ---

// 1. Add MediatR for Application layer - NEW CORRECT SYNTAX
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SyncUserCommand).Assembly));

// 2. Add DbContext for Infrastructure layer
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 3. Register Repositories and Unit of Work
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, ApplicationDbContext>();

// 4. Initialize Firebase Admin SDK
// We still need to create the FirebaseAdminSetup file in Infrastructure
// and then we can uncomment this.
// builder.Services.InitializeFirebase(); 

// 5. Configure JWT Bearer Authentication from Firebase
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();