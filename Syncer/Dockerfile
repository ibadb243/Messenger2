FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
# Каталог для файловых логов Serilog: пользователь приложения не root и не сможет создать его сам.
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Syncer/Syncer.csproj", "Syncer/"]
RUN dotnet restore "./Syncer/Syncer.csproj"
COPY . .
WORKDIR "/src/Syncer"
RUN dotnet publish "./Syncer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Messenger2.Syncer.dll"]
