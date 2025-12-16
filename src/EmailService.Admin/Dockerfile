FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EmailService.Admin/EmailService.Admin.csproj", "src/EmailService.Admin/"]
COPY ["src/EmailService.Application/EmailService.Application.csproj", "src/EmailService.Application/"]
COPY ["src/EmailService.Domain/EmailService.Domain.csproj", "src/EmailService.Domain/"]
COPY ["src/EmailService.Infrastructure/EmailService.Infrastructure.csproj", "src/EmailService.Infrastructure/"]
RUN dotnet restore "src/EmailService.Admin/EmailService.Admin.csproj"
COPY . .
WORKDIR "/src/src/EmailService.Admin"
RUN dotnet build "EmailService.Admin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailService.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailService.Admin.dll"]
