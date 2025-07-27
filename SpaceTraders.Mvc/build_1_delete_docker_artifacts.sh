#!/bin/bash

# Delete docker container FORCED if it exists
# Your image name
IMAGE_NAME="spacetraders-mvc"

# Find the container ID by filtering containers based on the image name
CONTAINER_ID=$(docker ps -q --filter "ancestor=$IMAGE_NAME:latest")

# Check if the container ID is empty, indicating that no container is running from the image
if [ -z "$CONTAINER_ID" ]; then
  echo "No container running from the image $IMAGE_NAME."
else
  # Stop the container if it's running
  docker stop "$CONTAINER_ID"

  # Delete the container
  docker rm "$CONTAINER_ID"
fi



# Check if the image exists in local Docker images
if docker images --format "{{.Repository}}" | grep -q "^$IMAGE_NAME$"; then
  # Delete the image
  docker rmi "$IMAGE_NAME:latest"
  echo "Image $IMAGE_NAME deleted successfully."
else
  echo "Image $IMAGE_NAME not found. No action needed."
fi
