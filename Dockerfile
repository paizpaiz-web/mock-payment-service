FROM public.ecr.aws/lambda/dotnet:8

# Copy the published application
COPY MockPaymentServiceLambda/src/MockPaymentServiceLambda/bin/Release/net8.0/linux-x64/publish/ ${LAMBDA_TASK_ROOT}

# Set the handler
CMD ["MockPaymentServiceLambda"]