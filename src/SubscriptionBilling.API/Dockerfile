# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY src/SubscriptionBilling.Domain/*.csproj src/SubscriptionBilling.Domain/
COPY src/SubscriptionBilling.Application/*.csproj src/SubscriptionBilling.Application/
COPY src/SubscriptionBilling.Infrastructure/*.csproj src/SubscriptionBilling.Infrastructure/
COPY src/SubscriptionBilling.API/*.csproj src/SubscriptionBilling.API/
RUN dotnet restore src/SubscriptionBilling.API/SubscriptionBilling.API.csproj

# Copy remaining source code
COPY src/SubscriptionBilling.Domain/ src/SubscriptionBilling.Domain/
COPY src/SubscriptionBilling.Application/ src/SubscriptionBilling.Application/
COPY src/SubscriptionBilling.Infrastructure/ src/SubscriptionBilling.Infrastructure/
COPY src/SubscriptionBilling.API/ src/SubscriptionBilling.API/

WORKDIR /app/src/SubscriptionBilling.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "SubscriptionBilling.API.dll"]
