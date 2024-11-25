# # Learn about building .NET container images:
# # https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
# FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
# ARG TARGETARCH
# WORKDIR /source
# RUN echo ${TARGETARCH}

# # # Copy project file and restore as distinct layers
# # COPY --link APSIM.Docs.csproj .
# # RUN dotnet restore -a $TARGETARCH

# # Copy source code and publish app
# WORKDIR /
# # TODO: Turn this back on later once local ApsimX copy works.
FROM apsimx-html-docs AS build

RUN mkdir /app
COPY . /apsimdocs
WORKDIR /apsimdocs
RUN dotnet publish APSIM.Docs.csproj -a amd64 --output ../app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app ./
USER $APP_UID
ENTRYPOINT ["dotnet","APSIM.Docs.dll"]
