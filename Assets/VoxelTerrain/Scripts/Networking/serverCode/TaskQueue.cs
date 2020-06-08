using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace UnityGameServer
{
    public class TaskQueue
    {
        private List<Action> _actions = new List<Action>();
        private List<Action> _currentActions = new List<Action>();
        private SafeDictionary<string, AsyncTask> _asyncTasks = new SafeDictionary<string, AsyncTask>();
        private static TaskQueue _instance;
        private ManualResetEvent _closeWait = new ManualResetEvent(false);

        public TaskQueue()
        {
            _instance = this;
        }

        public void Update()
        {
            foreach (AsyncTask task in _asyncTasks.Values.ToArray())
            {
                task.Update();
            }

            if (_actions.Count > 0)
            {
                lock (_actions)
                {
                    _currentActions.Clear();
                    _currentActions.AddRange(_actions);
                    _actions.Clear();
                }
                for (int i = 0; i < _currentActions.Count; i++)
                {
                    try
                    {
                        _currentActions[i]();
                        _currentActions[i] = null;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Queue: Message: {0}\n {1}", e.Message, e.StackTrace);
                        Logger.LogError(e.StackTrace);
                        _currentActions[i] = null;
                    }
                }
            }
        }

        public static void Close()
        {
            if (_instance != null && _instance._asyncTasks != null)
            {
                int taskCount = _instance._asyncTasks.Count;
                foreach (AsyncTask task in new List<AsyncTask>(_instance._asyncTasks.Values))
                {
                    task.Close();
                }
                if (taskCount > 0)
                    _instance._closeWait.WaitOne();
            }
        }

        public static void Close(string thread)
        {
            if (_instance != null && _instance._asyncTasks != null)
            {
                lock (_instance._asyncTasks)
                {
                    if (_instance._asyncTasks.ContainsKey(thread))
                    {
                        _instance._asyncTasks[thread].Close();
                        _instance._asyncTasks.Remove(thread);
                    }
                }
            }

        }

        public static void QueueMain(Action action)
        {
            if (_instance != null && _instance._actions != null)
            {
                lock (_instance._actions)
                {
                    _instance._actions.Add(action);
                }
            }
        }

        public static void QeueAsync(string thread, Action e)
        {
            if (_instance != null && _instance._asyncTasks != null)
            {
                lock (_instance._asyncTasks)
                {
                    if (_instance._asyncTasks.ContainsKey(thread))
                    {
                        _instance._asyncTasks[thread].AddTask(e);
                    }
                    else
                    {
                        AddAsyncQueue(thread);
                        QeueAsync(thread, e);
                    }
                }
            }
        }

        public static void AddAsyncQueue(string thread)
        {
            if (_instance != null && _instance._asyncTasks != null)
            {
                lock (_instance._asyncTasks)
                {
                    if (!_instance._asyncTasks.ContainsKey(thread))
                    {
                        AsyncTask task = new AsyncTask(thread, _instance);
                        _instance._asyncTasks.Add(thread, task);
                    }
                }
            }
        }

        public static bool ThreadExists(string thread)
        {
            return _instance._asyncTasks.ContainsKey(thread);
        }

        public static string[] GetProcessNames()
        {
            return _instance._asyncTasks.Keys.ToArray();
        }

        public static Thread GetThreadRef(string thread)
        {


            if (_instance != null && _instance._asyncTasks != null)
            {
                lock (_instance._asyncTasks)
                {
                    if (_instance._asyncTasks.ContainsKey(thread))
                    {
                        return _instance._asyncTasks[thread].thread;
                    }
                    else return null;
                }
            }
            return null;
        }

        public void CloseRemove(string name)
        {
            if (_asyncTasks.ContainsKey(name))
                _asyncTasks.Remove(name);

            if (_asyncTasks.Count == 0)
            {
                _closeWait.Set();
            }
        }
    }
}