#!/bin/bash

# Delete docker container FORCED if it exists
# Your image name
PROJECT_NAME="spacetrader-mvc"
IMAGE_NAME="spacetrader-mvc:latest"


dotnet publish --os linux --arch x64 /t:PublishContainer -c Release

docker run -d -p 9050:9050 --restart=always --name "$PROJECT_NAME" -e AppConfigConnectionString="$1" "$IMAGE_NAME"
rm -rf /tmp/Containers

# Check if there are any dangling images
if [ -n "$(docker images -f dangling=true -q)" ]; then
    # Remove dangling images
    docker rmi --force $(docker images -f "dangling=true" -q)
else
    echo "No dangling images found."
fi
