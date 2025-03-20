FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore "TelegramDigest.Web/TelegramDigest.Web.csproj"
RUN dotnet build "TelegramDigest.Web/TelegramDigest.Web.csproj" -c $BUILD_CONFIGURATION
RUN dotnet publish "TelegramDigest.Web/TelegramDigest.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
USER root
RUN mkdir -p /app/runtime
RUN chown -R $APP_UID:$APP_UID /app/runtime
RUN chmod 755 /app/runtime
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelegramDigest.Web.dll"]
