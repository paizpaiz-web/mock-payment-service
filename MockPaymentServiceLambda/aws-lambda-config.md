# AWS Lambda Configuration Design

## Lambda Function Configuration

### Runtime & Architecture
- **Runtime**: .NET 8 (`dotnet8`)
- **Architecture**: x86_64 (or arm64 for better performance/cost)
- **Memory**: 512 MB (start low, scale based on needs)
- **Timeout**: 30 seconds (sufficient for API operations)
- **Reserved Concurrency**: 0 (unlimited scaling)

### Environment Variables
```json
{
  "RDS_ENDPOINT": "mock-payment-service-db.xxxxxxx.us-east-1.rds.amazonaws.com",
  "RDS_USER": "admin",
  "RDS_PASSWORD": "/aws/reference/secretsmanager/mock-payment-service/rds-credentials",
  "RDS_DATABASE": "MockPaymentServiceDb",
  "JWT_KEY": "/aws/reference/secretsmanager/mock-payment-service/jwt-key",
  "JWT_ISSUER": "MockPaymentServiceLambda",
  "JWT_AUDIENCE": "MockPaymentServiceLambda",
  "JWT_ACCESS_TOKEN_EXPIRY_MINUTES": "15",
  "JWT_REFRESH_TOKEN_EXPIRY_DAYS": "7",
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

### VPC Configuration (Critical for RDS Access)
```yaml
VpcConfig:
  SecurityGroupIds:
    - sg-xxxxxxxxx  # Security group allowing Lambda to RDS
  SubnetIds:
    - subnet-xxxxxxxxx  # Private subnet in same VPC as RDS
    - subnet-xxxxxxxxx  # Second AZ for HA
```

### IAM Role & Permissions
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:*:*:*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue"
      ],
      "Resource": [
        "arn:aws:secretsmanager:region:account:secret:mock-payment-service/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "ec2:CreateNetworkInterface",
        "ec2:DescribeNetworkInterfaces",
        "ec2:DeleteNetworkInterface"
      ],
      "Resource": "*"
    }
  ]
}
```

## API Gateway Configuration

### API Gateway Type: HTTP API (REST API alternative)
- **Protocol**: HTTP/HTTPS
- **Integration**: Lambda Proxy
- **CORS**: Enabled for all origins (configure appropriately for production)

### Routes & Methods
```
/api/auth/register     POST
/api/auth/login        POST
/api/auth/refresh      POST
/api/payment/charge    POST  (requires JWT auth)
/api/payment/refund    POST  (requires JWT auth)
/health               GET
```

### Custom Domain & SSL
- Use API Gateway custom domain with SSL certificate
- Route53 integration for DNS

### Authentication (API Gateway Level)
- JWT Authorizer for protected routes
- Custom Lambda Authorizer if needed

## Database Connectivity Strategy

### Option 1: Direct RDS Connection (Recommended for simplicity)
- Lambda connects directly to RDS SQL Server
- Requires VPC configuration and security groups
- Connection pooling handled by EF Core

### Option 2: RDS Proxy (For high-scale, connection pooling)
```yaml
RDSDBProxy:
  Type: AWS::RDS::DBProxy
  Properties:
    DBProxyName: mock-payment-service-proxy
    EngineFamily: SQLSERVER
    VpcSubnetIds:
      - subnet-xxxxxxxxx
    VpcSecurityGroupIds:
      - sg-xxxxxxxxx
    Auth:
      - AuthScheme: SECRETS
        SecretArn: !Ref RDSSecret
        IAMAuth: DISABLED
```

## CloudWatch Monitoring

### Logs
- Lambda function logs automatically sent to CloudWatch
- Structured logging with Serilog → CloudWatch format

### Metrics
- Invocation count, duration, error rate
- Custom metrics for business logic (payment success/failure rates)

### Alarms
- High error rate (>5%)
- High duration (>20s)
- High concurrent executions

## Cost Optimization

### Lambda
- Use ARM64 architecture (20% cost reduction)
- Right-size memory allocation
- Implement provisioned concurrency if predictable traffic

### API Gateway
- Use HTTP API instead of REST API (cheaper)
- Enable caching for frequently accessed data

### RDS
- Use appropriate instance size
- Consider Aurora Serverless v2 for variable workloads