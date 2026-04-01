#!/bin/bash

# Delete docker container FORCED if it exists
# Your image name
PROJECT_NAME="spacetraders-console"
IMAGE_NAME="spacetraders-console:latest"


dotnet publish --os linux --arch x64 /t:PublishContainer -c Release

docker run -d --restart=no --name "$PROJECT_NAME" -e AppConfigConnectionString="$1" "$IMAGE_NAME"
rm -rf /tmp/Containers

# Check if there are any dangling images
if [ -n "$(docker images -f dangling=true -q)" ]; then
    # Remove dangling images
    docker rmi --force $(docker images -f "dangling=true" -q)
else
    echo "No dangling images found."
fi
