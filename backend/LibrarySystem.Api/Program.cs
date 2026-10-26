using System.Text;
using LibrarySystem.Api.Auth;
using LibrarySystem.Api.BackgroundJobs;
using LibrarySystem.Api.Common;
using LibrarySystem.Api.Data;
using LibrarySystem.Api.Hubs;
using LibrarySystem.Api.Messaging;
using LibrarySystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration binding ----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<DueDateReminderSettings>(builder.Configuration.GetSection("DueDateReminder"));

// ---- EF Core / SQL Server ----
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- App services ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IBorrowingRequestService, BorrowingRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ---- RabbitMQ publisher (singleton: owns a long-lived connection) + consumer hosted service ----
builder.Services.AddSingleton<IBorrowRequestPublisher, BorrowRequestPublisher>();
builder.Services.AddHostedService<BorrowRequestConsumerService>();
builder.Services.AddHostedService<DueDateReminderJob>();

// ---- SignalR ----
builder.Services.AddSignalR();

// ---- JWT Authentication ----
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // Allow the SignalR JS client to authenticate via ?access_token=
    // (browsers can't set Authorization headers on WebSocket upgrades).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR: browsers can't set custom headers on the WebSocket
            // handshake, so accept the app's JWT via query string there.
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            // Standard requests: the app's own JWT now travels in a custom
            // header, since "Authorization" is reserved for the WSO2
            // gateway token coming from the integration layer.
            var appToken = context.Request.Headers["X-Auth-Token"].ToString();
            if (!string.IsNullOrEmpty(appToken))
            {
                context.Token = appToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? appToken["Bearer ".Length..].Trim()
                    : appToken.Trim();
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

// ---- CORS (for the Angular frontend) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

// ---- Controllers + Swagger ----
builder.Services.AddControllers().ConfigureApiBehaviorOptions(o =>
{
    // Shape default [ApiController] model-validation failures into the
    // same error envelope used everywhere else in this API.
    o.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new LibrarySystem.Api.DTOs.ErrorDetail
            {
                Field = kvp.Key,
                Message = e.ErrorMessage
            }))
            .ToList();

        var body = new LibrarySystem.Api.DTOs.ErrorResponse
        {
            Code = "VALIDATION_ERROR",
            Message = "One or more fields are invalid.",
            Details = details,
            TraceId = context.HttpContext.TraceIdentifier
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(body);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Library System API", Version = "1.0.0" });
    options.AddSecurityDefinition("AppAuth", new()
    {
        Name = "X-Auth-Token",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Library System's own JWT (from POST /api/auth/login). Enter: Bearer {your JWT}"
    });
    options.AddSecurityDefinition("Wso2Auth", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "WSO2 gateway token from the integration layer. Enter: Bearer {token}"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "AppAuth" }
            },
            Array.Empty<string>()
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Wso2Auth" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---- Apply migrations + seed on startup ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    db.Database.Migrate();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    SeedData.Seed(db, hasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();