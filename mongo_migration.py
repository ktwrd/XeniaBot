import bson, os, argparse
from pymongo import MongoClient

class MongoMigrationTool:
    def __init__(self):
        print('[*] mongodb migration tool')
        parser = argparse.ArgumentParser(
            prog='Mongo Migration Tool',
            description='Tool used to backup/resotre mongodb databases'
        )
        parser.add_argument('-a', '--action', choices=['b', 'r', 'backup', 'restore'], required=True)
        parser.add_argument('-u', '--url', help='MongoDB connection URL', required=True)
        parser.add_argument('-d', '--database-name', help='Name of the database to backup/restore', required=True)
        parser.add_argument('-f', '--folder', help='Directory to backup/restore database', required=True)
        self.args = parser.parse_args()
        
        if not os.path.exists(self.args.folder):
            os.mkdir(self.args.folder)
        
        client = MongoClient(self.args.url)
        if self.args.action == 'b' or self.args.action == 'backup':
            print('[*] performing backup')
            self.dump(self.args.folder, client, self.args.database_name)
        else:
            print('[*] performing restore')
            self.restore(self.args.folder, client, self.args.db_name)
            

    def dump(self, path,conn,db_name):
        """
        MongoDB Dump
        :param collections: Database collections name
        :param conn: MongoDB client connection
        :param db_name: Database name
        :param path:
        :return:
        
        >>> DB_BACKUP_DIR = '/path/backups/'
        >>> conn = MongoClient("mongodb://admin:admin@127.0.0.1:27017", authSource="admin")
        >>> db_name = 'my_db'
        >>> collections = ['collection_name', 'collection_name1', 'collection_name2']
        >>> dump(collections, conn, db_name, DB_BACKUP_DIR)
        """

        db = conn[db_name]
        count = 0
        for coll in db.list_collection_names():
            with open(os.path.join(path, f'{coll}.bson'), 'wb+') as f:
                for doc in db[coll].find():
                    f.write(bson.BSON.encode(doc))
            print('[*] done: %s' % coll)
            count += 1
        print('[*] backup complete (%s collections)' % count)


    def restore(self, path, conn, db_name):
        """
        MongoDB Restore
        :param path: Database dumped path
        :param conn: MongoDB client connection
        :param db_name: Database name
        :return:
        
        >>> DB_BACKUP_DIR = '/path/backups/'
        >>> conn = MongoClient("mongodb://admin:admin@127.0.0.1:27017", authSource="admin")
        >>> db_name = 'my_db'
        >>> restore(DB_BACKUP_DIR, conn, db_name)
        
        """
        
        db = conn[db_name]
        for coll in os.listdir(path):
            if coll.endswith('.bson'):
                with open(os.path.join(path, coll), 'rb+') as f:
                    c = coll.split('.')[0]
                    db[c].insert_many(bson.decode_all(f.read()))
                    print('[*] done: %s' % c)
    print('[*] restore complete')

MongoMigrationTool()