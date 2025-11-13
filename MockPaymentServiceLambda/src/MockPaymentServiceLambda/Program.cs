using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MockPaymentServiceLambda.Models;
using MockPaymentServiceLambda.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddControllers(); // Remove MVC controllers for minimal APIs

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-secret-key"))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

// Auth endpoints
app.MapPost("/api/auth/register", async (IAuthService authService, RegisterRequest request) =>
{
    var result = await authService.RegisterAsync(request.Email, request.Password);
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { message = result.Message });
})
.WithName("Register");

app.MapPost("/api/auth/login", async (IAuthService authService, LoginRequest request) =>
{
    var result = await authService.LoginAsync(request.Email, request.Password);
    return result.Success ? Results.Ok(result.Data) : Results.Unauthorized();
})
.WithName("Login");

app.MapPost("/api/auth/refresh", async (IAuthService authService, RefreshRequest request) =>
{
    var result = await authService.RefreshTokenAsync(request.RefreshToken);
    return result.Success ? Results.Ok(result.Data) : Results.Unauthorized();
})
.WithName("RefreshToken");

// Payment endpoints (require authorization)
app.MapPost("/api/payment/charge", async (IPaymentService paymentService, ChargeRequest request) =>
{
    var result = await paymentService.ChargeAsync(request.Amount, request.CardNumber, request.ExpirationDate, request.CVV, request.CardholderName);
    return result.Success ? Results.Ok(result.Data) : Results.BadRequest(result.Data);
})
.WithName("Charge")
.RequireAuthorization();

app.MapPost("/api/payment/refund", async (IPaymentService paymentService, RefundRequest request) =>
{
    var result = await paymentService.RefundAsync(request.TransactionId, request.Amount, request.Reason);
    return result.Success ? Results.Ok(result.Data) : Results.BadRequest(result.Data);
})
.WithName("Refund")
.RequireAuthorization();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

app.Run();

// Request/Response DTOs
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ChargeRequest(decimal Amount, string? CardNumber, string? ExpirationDate, string? CVV, string? CardholderName);
public record RefundRequest(string TransactionId, decimal Amount, string? Reason);
