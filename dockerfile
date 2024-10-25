# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG TARGETARCH
WORKDIR /source
RUN echo ${TARGETARCH}

# # Copy project file and restore as distinct layers
# COPY --link APSIM.Docs.csproj .
# RUN dotnet restore -a $TARGETARCH

# Copy source code and publish app
RUN git clone https://github.com/APSIMInitiative/ApsimX.git

RUN mkdir /app
COPY . ./apsimdocs

RUN dotnet publish -a $TARGETARCH -o /app


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./app"]
