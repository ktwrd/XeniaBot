@echo off
echo Creating database with name of "mongodb-xenia"
echo Username: user
echo Password: password
docker run -v mongoData:/data/db -p 27020:27017 --name mongodb-xenia -d -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=password mongo