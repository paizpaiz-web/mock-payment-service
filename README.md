# Mock Payment Service

[![CI/CD](https://github.com/paizpaiz-web/mock-payment-service/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/paizpaiz-web/mock-payment-service/actions/workflows/ci-cd.yml)
[![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=flat&logo=docker&logoColor=white)](https://docker.com)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET 8 Web API service that simulates payment processing with JWT authentication, structured logging, and AWS ECS deployment configuration.

## Features

- **JWT Authentication**: Secure API endpoints with Bearer token authentication, including user registration, login, and token refresh
- **Database Authentication**: User management with secure password hashing using BCrypt
- **Token Refresh Mechanism**: Automatic token renewal with refresh tokens stored in database
- **HTTPS Enforcement**: HSTS enabled in production environments
- **Payment Processing**: Mock charge and refund operations with realistic success/failure rates
- **Structured Logging**: Serilog integration with console and file logging
- **Containerization**: Docker support with multi-stage build
- **AWS ECS Deployment**: Ready-to-use AWS ECS task definitions and CI/CD pipeline

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new user account
  - Body: `{ "email": "user@example.com", "password": "securepassword123" }`
- `POST /api/auth/login` - Authenticate and get JWT tokens
  - Body: `{ "email": "user@example.com", "password": "securepassword123" }`
## Database Setup

Before running the application, set up the database:

```bash
cd MockPaymentService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This will create the necessary database tables for user authentication and refresh tokens.
  - Returns: `{ "accessToken": "...", "refreshToken": "..." }`
- `POST /api/auth/refresh` - Refresh access token using refresh token
  - Body: `{ "refreshToken": "your_refresh_token_here" }`
  - Returns: `{ "accessToken": "...", "refreshToken": "..." }`

### Payment Operations (Requires Authentication)
- `POST /api/payment/charge` - Process a payment charge
- `POST /api/payment/refund` - Process a refund
- `GET /health` - Health check endpoint

## Running Locally

### Prerequisites
- .NET 8.0 SDK
- Local SQL Server instance or SQL Server LocalDB (automatically available with Visual Studio)
- Docker & Docker Compose (optional but recommended)

### Option 1: Using .NET CLI
```bash
cd MockPaymentService
dotnet restore
dotnet build
dotnet run
```

### Option 2: Using Docker Compose (Recommended)
```bash
# Build and run the service
docker-compose up --build

# Run in background
docker-compose up -d

# Stop the service
docker-compose down
```

### Option 3: Using Docker directly
```bash
# Build the image
docker build -t mock-payment-service .

# Run the container
docker run -p 8080:8080 -v $(pwd)/logs:/app/logs mock-payment-service
```

## Docker Configuration

- Multi-stage Dockerfile optimized for production
- Non-root user execution
- Health checks included
- Port 8080 exposed

## Deployment Options

### GitHub Actions CI/CD

The project includes GitHub Actions workflows for automated CI/CD:

1. **Automatic Testing**: Runs on every push and PR
2. **Docker Image Build**: Builds and pushes to Docker Hub on main branch
3. **AWS ECS Deployment**: Deploys to AWS ECS via ECR

#### Required GitHub Secrets
Set these in your repository settings under "Secrets and variables" > "Actions":

```
DOCKERHUB_USERNAME     # Your Docker Hub username
DOCKERHUB_PASSWORD     # Your Docker Hub password/token
AWS_ACCESS_KEY_ID      # AWS access key for ECR/ECS access
AWS_SECRET_ACCESS_KEY  # AWS secret key
AWS_REGION            # AWS region (e.g., us-east-1)
ECS_CLUSTER_NAME      # Your ECS cluster name
ECS_SERVICE_NAME      # Your ECS service name
```

#### GitHub Workflow Features
- **Test Stage**: Runs unit tests and builds
- **Build Stage**: Creates Docker images with proper tagging
- **Deploy Stage**: Pushes to ECR and updates ECS service
- **Caching**: Uses GitHub Actions cache for faster builds

### AWS ECS Deployment (Manual)

#### Prerequisites
- AWS CLI configured
- ECR repository created
- ECS cluster set up
- VPC, subnets, and security groups configured

#### Manual Deployment Steps

1. **Build and push Docker image**
   ```bash
   aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com
   docker build -t mock-payment-service .
   docker tag mock-payment-service:latest YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/mock-payment-service:latest
   docker push YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/mock-payment-service:latest
   ```

2. **Register task definition**
   ```bash
   aws ecs register-task-definition --cli-input-json file://aws/ecs-task-definition.json
   ```

3. **Create/update service**
   ```bash
   aws ecs create-service --cli-input-json file://aws/ecs-service.json
   ```

### AWS CodeBuild CI/CD

For AWS-native CI/CD, use the included `buildspec.yml` with AWS CodeBuild.

## Configuration

Update `appsettings.json` for:
- JWT settings (key, issuer, audience, token expiry times)
- Database connection string
- Serilog configuration (log levels, sinks)
- Kestrel endpoints

## Logging

- **Console**: Structured output with timestamps and log levels
- **File**: Daily rolling log files in `/app/logs/`
- **AWS CloudWatch**: Integrated with ECS task definition

## Security

- JWT access tokens expire after 15 minutes, refresh tokens after 7 days
- Secure password hashing using BCrypt with salt
- Database-backed user authentication with proper validation
- HTTPS enforced in production with HSTS headers
- All payment endpoints require valid JWT authentication
- Container runs as non-root user
- Sensitive configuration via environment variables

## Quick Start with GitHub

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/yourusername/mock-payment-service.git
   cd mock-payment-service
   ```

2. **Run locally with Docker Compose**
   ```bash
   docker-compose up --build
   ```

3. **Test the API**
    ```bash
    # Register a new user
    curl -X POST http://localhost:8080/api/auth/register \
      -H "Content-Type: application/json" \
      -d '{"email":"test@example.com","password":"securepassword123"}'

    # Login to get tokens
    curl -X POST http://localhost:8080/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"email":"test@example.com","password":"securepassword123"}'

    # Extract tokens from login response and use for payment operations
    export ACCESS_TOKEN="your_access_token_here"
    export REFRESH_TOKEN="your_refresh_token_here"

    # Use access token for payment operations
    curl -X POST http://localhost:8080/api/payment/charge \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -H "Content-Type: application/json" \
      -d '{"amount":100.50,"cardNumber":"4111111111111111","expirationDate":"12/25","cvv":"123","cardholderName":"John Doe"}'

    # When access token expires, refresh it
    curl -X POST http://localhost:8080/api/auth/refresh \
      -H "Content-Type: application/json" \
      -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
    ```

## GitHub Repository Setup

1. **Configure GitHub Secrets** (in repository Settings > Secrets and variables > Actions):
   - `DOCKERHUB_USERNAME` - Your Docker Hub username
   - `DOCKERHUB_PASSWORD` - Docker Hub access token
   - `AWS_ACCESS_KEY_ID` - AWS access key
   - `AWS_SECRET_ACCESS_KEY` - AWS secret key
   - `AWS_REGION` - AWS region (e.g., us-east-1)
   - `ECS_CLUSTER_NAME` - Your ECS cluster name
   - `ECS_SERVICE_NAME` - Your ECS service name

2. **Push to main branch** to trigger automated deployment

3. **Monitor deployment** through GitHub Actions tab

## Project Structure

```
mock-payment-service/
├── MockPaymentService/           # Main application
│   ├── Controllers/
│   │   ├── AuthController.cs     # JWT authentication, registration, login, token refresh
│   │   └── PaymentController.cs  # Payment operations
│   ├── Models/
│   │   ├── User.cs               # User entity with secure password handling
│   │   └── AppDbContext.cs       # Entity Framework database context
│   ├── Program.cs                # Application entry point
│   ├── appsettings.json          # Configuration (JWT, database, logging)
│   └── MockPaymentService.csproj # Project file with dependencies
├── aws/                         # AWS deployment configs
│   ├── ecs-task-definition.json
│   ├── ecs-service.json
│   └── buildspec.yml            # AWS CodeBuild
├── .github/workflows/           # GitHub Actions
│   └── ci-cd.yml
├── Dockerfile                   # Container definition
├── docker-compose.yml          # Local development
├── .dockerignore               # Docker ignore rules
└── README.md                   # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Monitoring

- Health endpoint: `GET /health`
- Container health checks configured
- Structured logging for observability
- AWS CloudWatch integration for ECS
- GitHub Actions deployment status
