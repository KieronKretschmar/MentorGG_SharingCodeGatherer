# ===============
# BUILD IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers

WORKDIR /app/Entities
COPY ./Entities/*.csproj ./
RUN dotnet restore

WORKDIR /app/Database
COPY ./Database/*.csproj ./
RUN dotnet restore

WORKDIR /app/RabbitCommunicationLib
COPY ./RabbitCommunicationLib/*.csproj ./
RUN dotnet restore

WORKDIR /app/SharingCodeGatherer
COPY ./SharingCodeGatherer/*.csproj ./
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY ./SharingCodeGatherer/ ./SharingCodeGatherer
COPY ./Database/ ./Database
COPY ./Entities ./Entities
COPY ./RabbitCommunicationLib ./RabbitCommunicationLib

RUN dotnet publish SharingCodeGatherer/ -c Release -o out

# ===============
# RUNTIME IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "SharingCodeGatherer.dll"]
