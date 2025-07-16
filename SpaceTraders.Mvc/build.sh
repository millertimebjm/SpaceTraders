#!/bin/bash

git pull

sh build_1_delete_docker_artifacts.sh

sh build_2_create_docker_artifacts.sh $1 
