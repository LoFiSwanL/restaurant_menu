# 1. Базовий образ для запуску (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Етап збірки проєкту (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ВИПРАВЛЕНО: з великої літери Restaurant_menu, як на GitHub
COPY ["Restaurant_menu/Restaurant_menu.csproj", "Restaurant_menu/"]
RUN dotnet restore "./Restaurant_menu/Restaurant_menu.csproj"
COPY . .
WORKDIR "/src/Restaurant_menu"
RUN dotnet build "./Restaurant_menu.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Етап публікації
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Restaurant_menu.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Фінальний чистий образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restaurant_menu.dll"]