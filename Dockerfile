FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env
WORKDIR /app

# Copy everything else and build
COPY ./AasxServerCore ./AasxServerCore
COPY ./AasxServerStandardBib ./AasxServerStandardBib
RUN dotnet publish ./AasxServerCore/AasxServerCore.csproj -c Release -o out

# Build runtime image, copy XML config files
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
EXPOSE 51210
EXPOSE 51310
WORKDIR /app
COPY --from=build-env /app/out .
COPY ./AasxServerStandardBib/*.xml ./
COPY ./dockerize/startServer.sh ./
ENTRYPOINT ["/bin/bash", "-c", "./startServer.sh"]
