using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameServer
{
    public class ColumnGenerationQueue : IDisposable
    {
        public class QueueEntry
        {
            public Vector3Int Column { get; private set; }
            public Region Region { get; private set; }
            public LOD_Mode Mode { get; set; } 
            public User Requester { get; private set; }
            public ConcurrentQueue<User> Subscribers { get; private set; }
            public Action<QueueEntry, Column> Callback { get; private set; }
            public object Meta { get; private set; }
            public bool Processed { get; private set; }

            private readonly object _padLock = new object();

            public QueueEntry(Vector3Int location, Region region, LOD_Mode mode, User requester, Action<QueueEntry, Column> callback, object meta)
            {
                Column = location;
                Region = region;
                Mode = mode;
                Requester = requester;
                Callback = callback;
                Meta = meta;

                Subscribers = new ConcurrentQueue<User>();
                Subscribers.Enqueue(requester);
            }

            public void Subscribe(User user, LOD_Mode new_mode)
            {
                lock (_padLock)
                {
                    Mode = (LOD_Mode)Math.Max((int)Mode, (int)new_mode);
                    if (!Subscribers.Contains(user))
                    {
                        Subscribers.Enqueue(user);
                    }
                }
            }

            public void Process()
            {
                lock (_padLock)
                {
                    Column column = null;
                    if (Region.ChunkExists(Column))
                    {
                        column = Region.RegenerateColumn(Requester, Column, Mode);
                    }
                    else
                    {
                        column = Region.CreateColumn(Requester, Column, Mode);
                    }

                    Logger.Log("ColumnGenerationQueue finished Gen: {0}", DebugTimer.Elapsed());
                    Processed = true;
                    TaskQueue.QueueMain(() =>
                    {
                        Logger.Log("ColumnGenerationQueue finished Gen QueueMain: {0}", DebugTimer.Elapsed());
                        User usr;
                        while (Subscribers.TryDequeue(out usr))
                        {
                            usr.RequestedColumnGenerated(column, Meta);
                        }
                        Callback?.Invoke(this, column);
                    });
                }
            }

            public override bool Equals(object obj)
            {
                QueueEntry other = (QueueEntry)obj;
                return other.Column == Column;
            }

            public override int GetHashCode()
            {
                return Column.GetHashCode();
            }
        }

        public int NumThreads { get; private set; }

        private ConcurrentQueue<Vector3Int> genQueue;
        private ConcurrentDictionary<Vector3Int, QueueEntry> Columns;
        private bool Run;

        public ColumnGenerationQueue(int numThreads)
        {
            NumThreads = numThreads;
            genQueue = new ConcurrentQueue<Vector3Int>();
            Columns = new ConcurrentDictionary<Vector3Int, QueueEntry>();
        }

        public void Start(bool run_main = false)
        {
            Run = true;
            for (int i = 0; i < NumThreads; i++)
            {
                SpawnThread(i, run_main);
            }
        }

        public void QueueGeneration(Vector3Int location, Region region, LOD_Mode mode, User requester, Action<QueueEntry, Column> callback, object Meta)
        {
            if (Columns.ContainsKey(location))
            {
                Subscribe(location, requester, mode);
                return;
            }

            QueueEntry ent = new QueueEntry(location, region, mode, requester, callback, Meta);
            Columns[location] = ent;
            genQueue.Enqueue(location);
        }

        public void Subscribe(Vector3Int location, User user, LOD_Mode mode)
        {
            if (Columns.ContainsKey(location))
            {
                Columns[location].Subscribe(user, mode);
            }
        }

        public void Dispose()
        {
            Run = false;
            for (int i = 0; i < NumThreads; i++)
                TaskQueue.Close("Gen_" + i);
        }

        private void SpawnThread(int num, bool run_main = false)
        {
            TaskQueue.QeueAsync("Gen_" + num, () =>
            {

                while (Run)
                {
                    Action ac = () => { 

                        Vector3Int loc;
                        if (genQueue.TryDequeue(out loc))
                        {
                            Columns[loc].Process();

                            QueueEntry ent;
                            Columns.TryRemove(loc, out ent);
                        }

                    };

                    if (run_main)
                    {
                        Loom.QueueOnMainThread(ac);
                    }
                    else
                    {
                        ac();
                    }
                    
                    System.Threading.Thread.Sleep(1);
                }
            });
        }
    }
}
