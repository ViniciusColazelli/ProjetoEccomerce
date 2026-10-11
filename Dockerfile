FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BackEnd/ src/BackEnd/

RUN dotnet restore src/BackEnd/API/API.csproj
RUN dotnet publish src/BackEnd/API/API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "API.dll"]