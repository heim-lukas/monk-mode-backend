﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using monk_mode_backend.Application;
using monk_mode_backend.Domain;
using monk_mode_backend.Hubs;
using monk_mode_backend.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(MappingProfile));

var connectionString = builder.Configuration.GetConnectionString("postgresql");

builder.Services.AddDbContext<MonkModeDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
}).AddEntityFrameworkStores<MonkModeDbContext>();

builder.Services.AddAuthentication(a => {
    a.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    a.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt => {
    opt.TokenValidationParameters = new TokenValidationParameters​ {
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(
                builder.Configuration.GetSection("JwtSettings")["Secret"]!
                )),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = false,
        ValidateLifetime = true
    };

    // JWT validation für SignalR
    opt.Events = new JwtBearerEvents {
        OnMessageReceived = context => {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
                context.Token = accessToken;
            }

            return System.Threading.Tasks.Task.CompletedTask; // Fully qualify Task
        }
    };
});

builder.Services.AddSwaggerGen(opt => {
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme() {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below."
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
          {
          {
             new OpenApiSecurityScheme
              {
                Reference = new OpenApiReference
                {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
                }
              }, new string[] {}
          }
        });
});

builder.Services.AddScoped<ITokenService, JWTService>();

// Enable CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowReactApp",
        policy => policy
            .WithOrigins("http://localhost:5173") // Your React app's origin
            .AllowAnyMethod() // Allows all HTTP methods (GET, POST, etc.)
            .AllowAnyHeader() // Allows all headers
            .AllowCredentials()); // Allows credentials (cookies, authorization headers)
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.UseHttpsRedirection();
app.Run();