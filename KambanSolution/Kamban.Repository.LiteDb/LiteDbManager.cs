using System;
using System.Threading.Tasks;
using LiteDB;

namespace Kamban.Repository.LiteDb
{
    public class LiteDbManager
    {
        private readonly ConnectionString connStr;
        private LiteDatabase dba;
        private int counter;

        public LiteDbManager(string uri)
        {
            dba = null;
            counter = 0;

            connStr = new ConnectionString()
            {
                Filename = uri,
                Upgrade = true
            };

            // prepare db
            try
            {
                var db = LockDb();

                db.CheckpointSize = 0;
                db.Checkpoint();
                db.Rebuild();
            }
            finally
            {
                FreeDb();
            }
        }

        public LiteDatabase LockDb()
        {
            lock(this)
            {
                if (dba == null)
                    dba = new LiteDatabase(connStr);

                counter++;

                return dba;
            }
        }

        public void FreeDb()
        {
            lock (this)
            {
                if (counter > 0)
                    counter--;

                // set an killers timer
                if (counter == 0)
                    Task.Delay(2000)
                        .ContinueWith(_ => CloseDb());

                if (counter < 0)
                    throw new Exception("Abnormal counter behavior");
            }
        }

        public void CloseDb() 
        {
            lock(this)
            {
                if (counter == 0 && dba != null)
                {
                    dba.Checkpoint();
                    dba.Dispose();
                    dba = null;
                }
            }
        }

    }//end of class
}
