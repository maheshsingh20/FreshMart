# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["services/ReviewService/ReviewService.csproj", "services/ReviewService/"]
COPY ["shared/SharedKernel/SharedKernel.csproj", "shared/SharedKernel/"]
RUN dotnet restore "services/ReviewService/ReviewService.csproj"
COPY . .
WORKDIR "/src/services/ReviewService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReviewService.dll"]
