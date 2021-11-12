#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY . .
WORKDIR AasxServerBlazor
RUN dotnet restore "AasxServerBlazor.csproj"
RUN dotnet build "AasxServerBlazor.csproj" -c Release -o /app/build
RUN dotnet publish "AasxServerBlazor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AasxServerBlazor.dll"]