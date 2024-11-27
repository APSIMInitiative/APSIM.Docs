# # Learn about building .NET container images:
# # https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG TARGETARCH
RUN echo ${TARGETARCH}

# Get ApsimX dependencies.
WORKDIR /
RUN git clone https://github.com/APSIMInitiative/ApsimX.git

COPY . /apsimdocs
WORKDIR /apsimdocs
# RUN dotnet publish APSIM.Docs.csproj -a amd64 --output ../app
RUN dotnet publish APSIM.Docs.csproj -a amd64 -c Release -o ../app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble
EXPOSE 8080
# WORKDIR /app
WORKDIR /
COPY --link --from=build /app /app
COPY --link --from=build /ApsimX /ApsimX

WORKDIR /app
USER $APP_UID
ENTRYPOINT ["dotnet","APSIM.Docs.dll"]
