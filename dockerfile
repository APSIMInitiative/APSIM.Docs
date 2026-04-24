# # Learn about building .NET container images:
# # https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG TARGETARCH
RUN echo ${TARGETARCH}
COPY . /apsimdocs
WORKDIR /apsimdocs
RUN dotnet publish APSIM.Docs.csproj -a amd64 -c Debug -o ../app
EXPOSE 8080
WORKDIR /app
ENTRYPOINT ["dotnet","APSIM.Docs.dll"]
