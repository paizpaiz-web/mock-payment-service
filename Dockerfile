FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj and restore dependencies
COPY MockPaymentServiceLambda/src/MockPaymentServiceLambda/*.csproj ./MockPaymentServiceLambda/src/MockPaymentServiceLambda/
RUN dotnet restore ./MockPaymentServiceLambda/src/MockPaymentServiceLambda/MockPaymentServiceLambda.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./MockPaymentServiceLambda/src/MockPaymentServiceLambda -c Release -r linux-x64 --self-contained false -o /app/publish

# Runtime stage
FROM public.ecr.aws/lambda/dotnet:8

WORKDIR /var/task

# Copy the published application from build stage
COPY --from=build /app/publish .

# Set the handler
CMD ["MockPaymentServiceLambda"]