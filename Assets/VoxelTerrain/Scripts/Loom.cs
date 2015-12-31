using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

public class Loom : MonoBehaviour {
    public enum messageType
    {
        Log,
        Warning,
        Error,
    }
    public struct Message
    {
        public messageType type;
        public string message;
        public Message(messageType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }

    public static bool DebugMode = false;

    public static int maxThreads = 8;
    public string time;
    static int numThreads;

    private static Loom _current;
    private int _count;
    public static Loom Current {
        get {
            Initialize();
            return _current;
        }
    }

    void Awake() {
        _current = this;
        initialized = true;
        DontDestroyOnLoad(this);
    }

    static bool initialized;

    static void Initialize() {
        if (!initialized) {

            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();
            SafeDebug.Log("Loom object created.");
        }

    }

    private List<Action> _actions = new List<Action>();
    private List<Action> _spread = new List<Action>();
    private List<Message> _messages = new List<Message>();
    private Dictionary<string, AsyncRunner> _AsynAction = new Dictionary<string, AsyncRunner>();

    public struct DelayedQueueItem {
        public float time;
        public Action action;
    }

    private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

    List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

    public static void QueueOnMainThread(Action action, bool spreadOut = false) {
        QueueOnMainThread(action, 0f, spreadOut);
    }

    public static void QueueOnMainThread(Action action, float time, bool spreadOut = false) {
        if (time != 0) {
            if (Current._delayed != null) {
                lock (Current._delayed) {
                    Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
                }
            }
        }
        else {
            if (spreadOut) {
                lock (Current._spread) {
                    Current._spread.Add(action);
                }
            }
            else {
                if (Current._actions != null) {
                    lock (Current._actions) {
                        Current._actions.Add(action);
                    }
                }
            }
        }
    }

    public static void AddAsyncThread(string thread) {
        if (Current._AsynAction != null){
            lock (Current._AsynAction) {
                try {
                    if (!Current._AsynAction.ContainsKey(thread)) {
                        AsyncRunner _runner = new AsyncRunner(thread);
                        Current._AsynAction.Add(thread, _runner);
                    }
                }
                catch (Exception e) {
                    SafeDebug.LogError("\nMessage: " + e.Message + "\nFunction: AddAsyncThread\nThread: " + thread, e);
                }
            }
        }
    }

    public static void QueueAsyncTask(string thread, Action e) {
        lock (Current._AsynAction) {
            try {
                if (Current._AsynAction.ContainsKey(thread)) {
                    Current._AsynAction[thread].AddAsyncTask(e);
                }
                else {
                    AddAsyncThread(thread);
                    QueueAsyncTask(thread, e);
                }
            }
            catch (Exception ex) {
                SafeDebug.LogError("\nMessage: " + ex.Message + "\nFunction: QueueAsyncTask\nThread: " + thread);
            }
        }
    }

    public static void QueueMessage(messageType type, string message)
    {
        lock(Current._messages)
        {
            try
            {
                Current._messages.Add(new Message(type, message));
            }
            catch (Exception ex)
            {
                SafeDebug.LogError("\nMessage: " + ex.Message + "\nFunction: QueueMessage");
            }
        }
    }

    public static Thread GetThreadRef(string thread) {
        lock (Current._AsynAction) {
            if (Current._AsynAction.ContainsKey(thread)) {
                return Current._AsynAction[thread].thread;
            }
            else
                return null;
        }
    }

    public static string GetThreadName(Thread thread) {
        foreach (string runner in Current._AsynAction.Keys) {
            if (Current._AsynAction[runner].thread.Equals(thread)) {
                return runner;
            }
        }
        return null;
    }

    public static bool ThreadExists(string thread) {
        return Current._AsynAction.ContainsKey(thread);
    }

    public static Thread RunAsync(Action a) {
        Initialize();
        while (numThreads >= maxThreads) {
            Thread.Sleep(1);
        }
        Interlocked.Increment(ref numThreads);
        ThreadPool.QueueUserWorkItem(RunAction, a);
        a = null;
        return null;
    }

    private static void RunAction(object action) {
        try {
            ((Action)action)();
        }
        catch {
        }
        finally {
            Interlocked.Decrement(ref numThreads);
        }

    }

    void OnDisable() {
        if (_current == this) {

            _current = null;
        }
    }

    List<Action> _currentActions = new List<Action>();

    List<Action> _spreadOutActions = new List<Action>();

    List<Message> _currentMessages = new List<Message>();

    int currentSetSize = 0;
    int currentSelection = 0;
    int tickCounter = 0;

    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    // Update is called once per frame
    void Update() {
        lock (_actions) {
            _currentActions.Clear();
            _currentActions.AddRange(_actions);
            _actions.Clear();
        }
        for (int i = 0; i < _currentActions.Count; i++) {
            watch.Stop();
            time = watch.Elapsed.ToString();
            watch.Reset();
            _currentActions[i]();
            _currentActions[i] = null;
            watch.Start();
        }

        if (Input.GetKey(KeyCode.Alpha5)) {
            foreach (string thread in _AsynAction.Keys) {
                if (_AsynAction[thread].Actions.Count > 0) {
                    //Console.LogDebug(_AsynAction[thread].threadName + ": functions detected: " + _AsynAction[thread].Actions.Count + ", " + _AsynAction[thread]._currentActions.Count);
                }
            }
        }

        lock (_messages)
        {
            _currentMessages.Clear();
            _currentMessages.AddRange(_messages);
            _messages.Clear();
        }
        for (int i = 0; i < _currentMessages.Count; i++)
        {
            switch(_currentMessages[i].type)
            {
                case messageType.Log:
                    Debug.Log(_currentMessages[i].message);
                    break;
                case messageType.Warning:
                    Debug.LogWarning(_currentMessages[i].message);
                    break;
                case messageType.Error:
                    Debug.LogError(_currentMessages[i].message);
                    break;
            }
        }

        /*if (_spreadOutActions.Count == 0 && _spread.Count > 0) {
            lock (_spread) {
                _spreadOutActions.Clear();
                _spreadOutActions.AddRange(_spread);
                _spread.Clear();
                StartCoroutine(SpreadOut());
            }
        }*/

        if (_spreadOutActions.Count == 0 && _spread.Count > 0)
        {
            lock (_spread)
            {
                tickCounter = 0;
                currentSelection = 0;
                currentSetSize = _spread.Count;
                _spreadOutActions.Clear();
                _spreadOutActions.AddRange(_spread);
                _spread.Clear();
            }
        }
        else if (_spreadOutActions.Count > 0)
        {
            tickCounter++;
            if (currentSelection < currentSetSize && tickCounter % 2 == 0)
            {
                _spreadOutActions[currentSelection]();
                _spreadOutActions[currentSelection] = null;
                currentSelection++;
            }
        }

        lock (_delayed)
        {
            _currentDelayed.Clear();
            _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
            foreach (var item in _currentDelayed)
                _delayed.Remove(item);
        }
        foreach (var delayed in _currentDelayed)
        {
            delayed.action();
        }
    }

    /*void OnGUI()
    {
        if (DebugMode)
        {
            int baseYpos = 20 + 10 + 30 + 20 + 20;
            int i = 0;
            GUI.Label(new Rect(10, baseYpos, 100, 20), "-- THREADS --");
            foreach(string threadName in _AsynAction.Keys)
            {
                bool threadRunning = _AsynAction[threadName].functionRunning;
                int tasks = _AsynAction[threadName]._currentActions.Count;
                TimeSpan time = _AsynAction[threadName].FuctionWorkTime.Elapsed;
                string content = string.Format("{0} ({1}) - running: {2}, Time: {3}",
                    threadName, tasks, threadRunning, time.Seconds.ToString() + ":" + time.Milliseconds.ToString());
                GUI.Label(new Rect(10 + 20, baseYpos + 20 + (i * 20), Screen.width - 30, 20), content);
                i++;
            }
        }
    }*/

    void OnApplicationQuit() {
        Close();
    }

    IEnumerator SpreadOut() {
        for (int i = 0; i < _spreadOutActions.Count; i++) {
            _spreadOutActions[i]();
            _spreadOutActions[i] = null;
            yield return new WaitForEndOfFrame();
        }
        _spreadOutActions.Clear();
    }

    public void Close() {
        if (_actions != null) {
            _actions.Clear();
            _actions = null;
        }
        if (_AsynAction != null) {
            foreach (AsyncRunner runner in _AsynAction.Values) {
                runner.Dispose();
            }
            _AsynAction.Clear();
            _AsynAction = null;
        }
        if (_currentActions != null) {
            _currentActions.Clear();
            _currentActions = null;
        }
        if (_delayed != null) {
            _delayed.Clear();
            _delayed = null;
        }
        if (_currentDelayed != null) {
            _currentDelayed.Clear();
            _currentDelayed = null;
        }
        Debug.Log("Loom closed");
    }
}
