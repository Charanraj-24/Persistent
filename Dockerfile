# Reflect Provider Dashboard (demo) - container image.
# Build:  docker build -t reflect-dashboard .
# Run:    docker run -p 8080:8080 -e Demo__Password=choose-one reflect-dashboard
# AI (optional): add -e Assistant__ApiKey=... (never bake keys into the image)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ReflectProviderDashboard.slnx ./
COPY src/Reflect.Domain/Reflect.Domain.csproj src/Reflect.Domain/
COPY src/Reflect.Web/Reflect.Web.csproj src/Reflect.Web/
RUN dotnet restore src/Reflect.Web/Reflect.Web.csproj -p:RazorRuntimeCompilation=false
COPY src/ src/
RUN dotnet publish src/Reflect.Web/Reflect.Web.csproj -c Release -o /app -p:RazorRuntimeCompilation=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Csp__StrictScripts=true \
    Hosting__BehindProxy=true
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Reflect.Web.dll"]
