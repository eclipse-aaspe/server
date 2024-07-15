# Use a multi-stage build to build and publish the .NET application
# Specify the initial base architecture (amd64 in this case)
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /repo/src

# Copy everything else and build
COPY ./src/ /repo/src/
COPY ./LICENSE.TXT /repo/LICENSE.txt

RUN dotnet clean
RUN dotnet restore
RUN dotnet build -o /out/AasxServerBlazor AasxServerBlazor -v diag
RUN dotnet publish -c Release -v diag --no-restore

# Use a runtime image to run the application
# Specify the initial base architecture (amd64 in this case)
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0 as base
RUN apt update && apt upgrade --yes
RUN apt install -y curl nano libgdiplus
EXPOSE 5001
COPY --from=build-env /out/AasxServerBlazor/ /AasxServerBlazor/
COPY ./content-for-demo/ /AasxServerBlazor/
WORKDIR /AasxServerBlazor
ENTRYPOINT ["/bin/bash", "-c", "./startForDemo.sh"]
