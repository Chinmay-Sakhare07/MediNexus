# MediNexus .NET API — container image for Render / Cloud Run / any Docker host.
# Build context = repo root.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Backend/HospitalManagement.API.csproj Backend/
RUN dotnet restore Backend/HospitalManagement.API.csproj
COPY Backend/ Backend/
RUN dotnet publish Backend/HospitalManagement.API.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# The host injects $PORT; Program.cs binds Kestrel to it. Documented default:
EXPOSE 8080
ENTRYPOINT ["dotnet", "HospitalManagement.API.dll"]
