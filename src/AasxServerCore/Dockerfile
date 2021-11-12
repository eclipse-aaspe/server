#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app
EXPOSE 51710

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["AasxServerCore/AasxServerCore.csproj", "AasxServerCore/"]
COPY ["AasxServerStandardBib/AasxServerStandardBib.csproj", "AasxServerStandardBib/"]
RUN dotnet restore "AasxServerCore/AasxServerCore.csproj"
COPY . .
WORKDIR "/src/AasxServerCore"
RUN dotnet build "AasxServerCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AasxServerCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AasxServerCore.dll"]