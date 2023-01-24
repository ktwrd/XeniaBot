@echo off
echo Creating database with name of "mongodb-shortcake"
echo Username: user
echo Password: password
docker run -p 27020:27017 --name mongodb-shortcake -d -e MONGO_INITDB_ROOT_USERNAME=user -e MONGO_INITDB_ROOT_PASSWORD=password mongo