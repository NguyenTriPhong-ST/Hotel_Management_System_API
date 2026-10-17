using System.IdentityModel.Tokens.Jwt;
using System.Text;
using HotelManagementSystem.Data;
using HotelManagementSystem.Mappers;
using HotelManagementSystem.Repositories.Implementations;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Implementations;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    ));

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AccountMappingProfile>());

// Dependency Injection - Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<IPasswordHasherService, BcryptPasswordHasherService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();

// JWT Authentication Configuration
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? "HotelManagementSystem_SuperSecretKey_For_JWT_Authentication_2026_PRN232";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "HotelManagementSystem";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "HotelManagementClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };

    // Kiểm tra và làm sạch token nếu người dùng lỡ copy thừa ký tự hoặc không có chữ Bearer
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                var raw = authHeader.Trim();
                if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    raw = raw.Substring(7).Trim();
                }
                raw = raw.Trim('"', '\'', ' ');
                // Tự động trích xuất chuỗi JWT chuẩn (ey...) nếu người dùng lỡ copy dính cả JSON hoặc expiresAt
                var match = System.Text.RegularExpressions.Regex.Match(raw, @"(ey[a-zA-Z0-9_\-\.]+)");
                if (match.Success)
                {
                    raw = match.Groups[1].Value;
                }
                context.Token = raw;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var sessionIdClaim = context.Principal?.FindFirst("SessionId")?.Value 
                ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                context.Fail("Token không chứa SessionId hợp lệ.");
                return;
            }

            var sessionRepo = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionRepository>();
            var activeSession = await sessionRepo.GetActiveSessionAsync(sessionId);

            if (activeSession == null || activeSession.Account == null || !activeSession.Account.IsActive)
            {
                context.Fail("Phiên đăng nhập không tồn tại, đã hết hạn hoặc tài khoản đã bị vô hiệu hóa.");
            }
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Hotel Management System API", 
        Version = "v1",
        Description = "API Quản lý Khách sạn - Phân hệ Authentication (F01) và Accounts (F02)"
    });

    // Cấu hình JWT Bearer trong Swagger UI chuẩn HTTP Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Chỉ cần dán mã token (không cần gõ thêm chữ Bearer):",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Khởi tạo dữ liệu mẫu cho toàn bộ database nếu chưa có
await DbSeeder.SeedAsync(app.Services);

app.Run();
