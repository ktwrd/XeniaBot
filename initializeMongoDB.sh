#!/bin/bash
echo "Creating database with name of mongodb-shortcake"
echo "Username: user"
echo "Password: password"
echo "Port: 27020"
docker run \
	-v mongoData:/data/db \
	-p 27020:27017 \
	--name mongodb-shortcake \
	-d \
	-e MONGO_INITDB_ROOT_USERNAME=user \
	-e MONGO_INITDB_ROOT_PASSWORD=password mongo
