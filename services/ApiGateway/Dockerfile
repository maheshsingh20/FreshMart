# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["services/ApiGateway/ApiGateway.csproj", "services/ApiGateway/"]
RUN dotnet restore "services/ApiGateway/ApiGateway.csproj"
COPY . .
WORKDIR "/src/services/ApiGateway"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
