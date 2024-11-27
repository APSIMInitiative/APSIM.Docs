docker build -t apsimdocs .
docker rm apsimdocs
docker run -p 8080:8080 -e ASPNETCORE_HTTP_PORTS=8080 --name apsimdocs apsimdocs