FROM public.ecr.aws/lambda/dotnet:8

WORKDIR /var/task

# Copy csproj and restore dependencies
COPY MockPaymentServiceLambda/src/MockPaymentServiceLambda/*.csproj ./MockPaymentServiceLambda/src/MockPaymentServiceLambda/
RUN dotnet restore ./MockPaymentServiceLambda/src/MockPaymentServiceLambda/MockPaymentServiceLambda.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./MockPaymentServiceLambda/src/MockPaymentServiceLambda -c Release -r linux-x64 --self-contained false -o /var/task

# Set the handler
CMD ["MockPaymentServiceLambda"]