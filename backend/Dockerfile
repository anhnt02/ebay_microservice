FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PaymentShipping.slnx", "."]
COPY ["PaymentShipping.Domain/PaymentShipping.Domain.csproj", "PaymentShipping.Domain/"]
COPY ["PaymentShipping.Contracts/PaymentShipping.Contracts.csproj", "PaymentShipping.Contracts/"]
COPY ["PaymentShipping.Application/PaymentShipping.Application.csproj", "PaymentShipping.Application/"]
COPY ["PaymentShipping.Infrastructure/PaymentShipping.Infrastructure.csproj", "PaymentShipping.Infrastructure/"]
COPY ["PaymentShipping.Api/PaymentShipping.Api.csproj", "PaymentShipping.Api/"]

# Restore dependencies
RUN dotnet restore "PaymentShipping.Api/PaymentShipping.Api.csproj"

# Copy the remaining files and build
COPY . .
WORKDIR "/src/PaymentShipping.Api"
RUN dotnet build "PaymentShipping.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentShipping.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentShipping.Api.dll"]
