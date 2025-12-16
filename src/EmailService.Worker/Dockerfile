FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EmailService.Worker/EmailService.Worker.csproj", "src/EmailService.Worker/"]
COPY ["src/EmailService.Application/EmailService.Application.csproj", "src/EmailService.Application/"]
COPY ["src/EmailService.Domain/EmailService.Domain.csproj", "src/EmailService.Domain/"]
COPY ["src/EmailService.Infrastructure/EmailService.Infrastructure.csproj", "src/EmailService.Infrastructure/"]
RUN dotnet restore "src/EmailService.Worker/EmailService.Worker.csproj"
COPY . .
WORKDIR "/src/src/EmailService.Worker"
RUN dotnet build "EmailService.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailService.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailService.Worker.dll"]
