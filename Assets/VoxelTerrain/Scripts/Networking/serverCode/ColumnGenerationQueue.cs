﻿using System;
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
            public Column.LOD_Mode Mode { get; set; } 
            public User Requester { get; private set; }
            public ConcurrentQueue<User> Subscribers { get; private set; }
            public Action<QueueEntry> Callback { get; private set; }
            public object Meta { get; private set; }
            public bool Processed { get; private set; }

            private readonly object _padLock = new object();

            public QueueEntry(Vector3Int location, Region region, Column.LOD_Mode mode, User requester, Action<QueueEntry> callback, object meta)
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

            public void Subscribe(User user, Column.LOD_Mode new_mode)
            {
                lock (_padLock)
                {
                    Mode = (Column.LOD_Mode)Math.Max((int)Mode, (int)new_mode);
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

                    Processed = true;
                    TaskQueue.QueueMain(() =>
                    {
                        User usr;
                        while (Subscribers.TryDequeue(out usr))
                        {
                            usr.RequestedColumnGenerated(column, Meta);
                        }
                        Callback?.Invoke(this);
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

        public void Start()
        {
            Run = true;
            for (int i = 0; i < NumThreads; i++)
            {
                SpawnThread(i);
            }
        }

        public void QueueGeneration(Vector3Int location, Region region, Column.LOD_Mode mode, User requester, Action<QueueEntry> callback, object Meta)
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

        public void Subscribe(Vector3Int location, User user, Column.LOD_Mode mode)
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

        private void SpawnThread(int num)
        {
            TaskQueue.QeueAsync("Gen_" + num, () =>
            {
                while (Run)
                {
                    Vector3Int loc;
                    if (genQueue.TryDequeue(out loc))
                    {
                        Columns[loc].Process();

                        QueueEntry ent;
                        Columns.TryRemove(loc, out ent);
                    }

                    System.Threading.Thread.Sleep(1);
                }
            });
        }
    }
}