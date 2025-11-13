# MockPaymentServiceLambda - Serverless Architecture

## Project Structure

```
MockPaymentServiceLambda/
├── src/
│   └── MockPaymentServiceLambda/
│       ├── Models/
│       │   ├── User.cs                    # User entity with BCrypt password handling
│       │   ├── AppDbContext.cs            # EF Core database context
│       │   └── DTOs/                      # Request/Response DTOs
│       │       ├── Auth/
│       │       │   ├── RegisterRequest.cs
│       │       │   ├── LoginRequest.cs
│       │       │   ├── RefreshRequest.cs
│       │       │   └── TokenResponse.cs
│       │       └── Payment/
│       │           ├── ChargeRequest.cs
│       │           ├── ChargeResponse.cs
│       │           ├── RefundRequest.cs
│       │           └── RefundResponse.cs
│       ├── Services/
│       │   ├── IJwtService.cs             # JWT token generation interface
│       │   ├── JwtService.cs              # JWT implementation
│       │   ├── IAuthService.cs            # Authentication business logic
│       │   ├── AuthService.cs             # Auth service implementation
│       │   ├── IPaymentService.cs         # Payment processing interface
│       │   └── PaymentService.cs          # Mock payment processing
│       ├── Function.cs                    # Lambda function entry point
│       ├── Program.cs                     # ASP.NET Core minimal API setup
│       ├── appsettings.json               # Configuration
│       └── MockPaymentServiceLambda.csproj # Project file
├── test/
│   └── MockPaymentServiceLambda.Tests/
│       ├── AuthTests.cs
│       ├── PaymentTests.cs
│       └── MockPaymentServiceLambda.Tests.csproj
├── infrastructure/
│   ├── template.yaml                      # SAM template for deployment
│   ├── buildspec.yml                     # CodeBuild CI/CD
│   └── parameters.json                   # Environment parameters
└── README.md
```

## Key Architecture Changes from ECS Version

### 1. Entry Point
- **ECS**: `Program.cs` with `WebApplication.CreateBuilder()` and Kestrel hosting
- **Lambda**: `Function.cs` extending `Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction`

### 2. API Structure
- **ECS**: Controller classes with `[ApiController]` and `[Route]` attributes
- **Lambda**: Minimal API endpoints defined in `Program.cs` using `app.MapPost()`, etc.

### 3. Dependency Injection
- **ECS**: Services registered in `Program.cs` builder
- **Lambda**: Same DI setup, but Lambda function handles HTTP context routing

### 4. Configuration
- **ECS**: `appsettings.json` loaded automatically
- **Lambda**: Same, but environment variables override for AWS deployment

### 5. Database Connection
- **ECS**: Direct SQL Server connection
- **Lambda**: Same, but requires VPC configuration and security groups for RDS access

### 6. Logging
- **ECS**: Serilog with console and file sinks
- **Lambda**: CloudWatch integration via `Amazon.Lambda.Logging.AspNetCore`

### 7. Deployment
- **ECS**: Docker container in Fargate
- **Lambda**: Zip deployment package or container image via SAM/CDK