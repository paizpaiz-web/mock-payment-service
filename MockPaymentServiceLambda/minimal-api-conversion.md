# Minimal API Conversion Plan

## Current Controller Structure vs Minimal API

### AuthController.cs (142 lines) → Minimal API Endpoints

**Current:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("register")] public async Task<IActionResult> Register() { ... }
    [HttpPost("login")] public async Task<IActionResult> Login() { ... }
    [HttpPost("refresh")] public async Task<IActionResult> Refresh() { ... }
}
```

**Lambda Minimal API:**
```csharp
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return result.Success ? Results.Ok(new { message = "User registered" }) : Results.BadRequest(result.Errors);
});

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result.Tokens) : Results.Unauthorized();
});

app.MapPost("/api/auth/refresh", async (RefreshRequest request, IAuthService authService) =>
{
    var result = await authService.RefreshTokenAsync(request);
    return result.Success ? Results.Ok(result.Tokens) : Results.Unauthorized();
});
```

### PaymentController.cs (125 lines) → Minimal API Endpoints

**Current:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    [HttpPost("charge")] public async Task<IActionResult> Charge() { ... }
    [HttpPost("refund")] public async Task<IActionResult> Refund() { ... }
}
```

**Lambda Minimal API:**
```csharp
app.MapPost("/api/payment/charge", async (ChargeRequest request, IPaymentService paymentService, HttpContext context) =>
{
    // Extract JWT token from Authorization header for user identification
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    var result = await paymentService.ChargeAsync(request, token);
    return result.Success ? Results.Ok(result.Response) : Results.BadRequest(result.Response);
})
.RequireAuthorization();

app.MapPost("/api/payment/refund", async (RefundRequest request, IPaymentService paymentService, HttpContext context) =>
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    var result = await paymentService.RefundAsync(request, token);
    return result.Success ? Results.Ok(result.Response) : Results.BadRequest(result.Response);
})
.RequireAuthorization();
```

## Service Layer Extraction

### Current: Logic embedded in controllers
- Password hashing/verification in User model
- Token generation in AuthController
- Payment logic in PaymentController
- Database operations scattered

### Proposed: Clean service layer
```csharp
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(RefreshRequest request);
}

public interface IPaymentService
{
    Task<PaymentResult> ChargeAsync(ChargeRequest request, string jwtToken);
    Task<PaymentResult> RefundAsync(RefundRequest request, string jwtToken);
}

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
```

## Benefits of Minimal API Conversion

1. **Reduced Boilerplate**: No controller classes, attributes, or IActionResult
2. **Better Performance**: Direct request/response mapping
3. **Easier Testing**: Pure functions instead of controller methods
4. **Serverless Optimized**: Better cold start performance
5. **Cleaner Code**: Logic separated into services, presentation in endpoints

## Migration Strategy

1. Extract business logic into service classes
2. Create DTOs for requests/responses
3. Convert controllers to minimal API endpoints
4. Update dependency injection configuration
5. Add Lambda-specific configuration (Function.cs, cloudformation template)
6. Update logging to use CloudWatch
7. Configure VPC and security groups for RDS access