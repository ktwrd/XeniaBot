import os, sys, datetime, tarfile, os.path
from pymongo import MongoClient
from bson.json_util import dumps

"""
Required packages;
  - pymongo
  - bson
  
Run with the following environment variables
- MONGO_URL
    - MongoDB Connection URL
- MONGO_DBNAME
    - Name of the database to backup
- MONGO_BACKUP_DIR (optional)
    - Backup Directory
"""

class MongoBackup:
    def __init__(self):
        self.backup_directory = os.path.abspath('./backups')
        self.mongo_url = ''
        self.db_name = ''
    def create_folder_backup(self):
        print('[*] target directory: %s' % self.backup_directory)
        dt = datetime.datetime.now()
        directory = os.path.join(
            self.backup_directory,
            'bk_%s_%s-%s-%s__%s_%s' % (self.db_name,dt.month,dt.day,dt.year, dt.hour, dt.minute)
        )
        if not os.path.exists(directory):
            os.makedirs(directory)
        return directory
    def run_backup(self):
        client = MongoClient(self.mongo_url)
        db = client[self.db_name]
        collections = db.list_collection_names()
        files_to_compress = []
        directory = self.create_folder_backup()
        for collection in collections:
            db_collection = db[collection]
            cursor = db_collection.find({})
            filename = ('%s/%s.json' %(directory,collection))
            files_to_compress.append(filename)
            with open(filename, 'w') as file:
                file.write('[')
                for document in cursor:
                    file.write(dumps(document))
                    file.write(',')
                file.write(']')
        tar_file = ('%s.tar.gz' % (directory)) 
        self.make_tarfile(tar_file,files_to_compress)
    def make_tarfile(self, output_filename, source_dir):
        tar = tarfile.open(output_filename, "w:gz")
        for filename in source_dir:
            tar.add(filename)
        tar.close()
    def from_env(self):
        self.mongo_url = os.environ['MONGO_URL']
        self.db_name = os.environ['MONGO_DBNAME']
        self.backup_directory = os.environ.get('MONGO_BACKUP_DIR', './backups')

if __name__ == '__main__':
    instance = MongoBackup()
    instance.from_env()
    try:
        instance.run_backup()
        print('[*] Successfully performed backup')
    except Exception as e:
        print('[-] An unexpected error has occurred')
        print('[-] '+ str(e) )
        print('[-] EXIT')