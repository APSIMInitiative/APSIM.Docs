docker build -t apsim-docs
docker run -p 8000:8080 -e ASPNETCORE_HTTP_PORTS=8080 apsim-docs