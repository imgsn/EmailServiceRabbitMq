FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EmailService.Gateway/EmailService.Gateway.csproj", "src/EmailService.Gateway/"]
RUN dotnet restore "src/EmailService.Gateway/EmailService.Gateway.csproj"
COPY . .
WORKDIR "/src/src/EmailService.Gateway"
RUN dotnet build "EmailService.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailService.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailService.Gateway.dll"]
