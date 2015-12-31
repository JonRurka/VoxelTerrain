using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;

class AsyncRunner : IDisposable
{
    public ManualResetEvent resetEvent;
    public List<Action> actions;
    public List<Action> Actions {
        get {
            lock (actions) {
                return actions;
            }
        }
    }
    public bool run;
    public string threadName;
    public List<Action> _currentActions;
    public Thread thread;
    public bool functionRunning = false;
    public System.Diagnostics.Stopwatch FuctionWorkTime;
    int x = 0;

    public AsyncRunner(string _name) {
        threadName = _name;
        resetEvent = new ManualResetEvent(false);
        actions = new List<Action>();
        _currentActions = new List<Action>();
        run = true;
        FuctionWorkTime = new System.Diagnostics.Stopwatch();
        thread = new Thread(new ThreadStart(Run));
        thread.Start();
    }

    public void AddAsyncTask(Action e) {
        lock (actions) {
            actions.Add(e);
            //resetEvent.Set();
        }
    }

    public void Run()
    {
        while (run)
        {
            if (actions.Count > 0)
                resetEvent.WaitOne(1);
            else
                resetEvent.WaitOne(10);
            try
            {
                lock (actions) {
                    _currentActions.Clear();
                    _currentActions.AddRange(actions);
                    actions.Clear();
                }
                if (_currentActions.Count > 0)
                {
                    for (int i = 0; i < _currentActions.Count; i++)
                    {
                        try
                        {
                            if (run) {
                                functionRunning = true;
                                FuctionWorkTime.Reset();
                                FuctionWorkTime.Start();
                                _currentActions[i]();
                                _currentActions[i] = null;
                                FuctionWorkTime.Stop();
                                functionRunning = false;
                            }
                            //ConsoleWpr.LogDebug(threadName + ": function Called.");
                        }
                        catch (Exception e)
                        {
                            SafeDebug.LogError("message: " + e.Message + ", thread: " + threadName, e);
                            _currentActions[i] = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SafeDebug.LogError("\nMessage: " + e.Message + "\nFunction: Run\nThread: " + threadName);
            }
            resetEvent.Reset();
        }
    }

    public void Dispose() {
        //ConsoleWpr.LogDebug("Dispose called in thread " + threadName);
        run = false;
        thread.Abort();
        for (int i = 0; i < actions.Count; i++) {
            Actions[i] = null;
        }
        Actions.Clear();
        actions = null;
    }
}

