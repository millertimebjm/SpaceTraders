#!/bin/bash

# git pull

# sh build_1_delete_docker_artifacts.sh

# sh build_2_create_docker_artifacts.sh $1 


# 1. Get latest code
git pull

# 2. Rebuild and restart (Docker handles the cleanup)
# We pass the connection string as an environment variable for the build/run
export APP_CONFIG_CONNECTION_STRING=$1
docker compose up -d --build

# 3. Optional: Cleanup old, unused images
docker image prune -f