using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityGameServer
{
    public class AsyncTask
    {
        private ManualResetEvent _resetEvent;
        private List<Action> _actions;
        private List<Action> _currentActions;
        private bool _run;
        private TaskQueue _queue;

        public Thread thread { get; private set; }
        public string threadName { get; private set; }

        public AsyncTask(string name, TaskQueue queue)
        {
            threadName = name;
            _queue = queue;
            _run = true;
            _resetEvent = new ManualResetEvent(false);
            _actions = new List<Action>();
            _currentActions = new List<Action>();
            thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Start();
        }

        public void AddTask(Action e)
        {
            lock (_actions)
            {
                _actions.Add(e);
                _resetEvent.Set();
            }
        }

        public void Close()
        {
            _run = false;
            //thread.Abort();
        }

        public void Update()
        {
            if (_actions.Count > 0)
                _resetEvent.Set();
        }

        private void Run()
        {
            try
            {
                while (_run)
                {
                    _resetEvent.WaitOne();
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
                                Logger.LogError("{0} queue: {1}\n{2}", threadName, e.Message, e.StackTrace);
                                _currentActions = null;
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                Logger.Log("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.Message);
            }
            Logger.Log("Process finished: " + threadName);
            _queue.CloseRemove(threadName);
        }
    }
}