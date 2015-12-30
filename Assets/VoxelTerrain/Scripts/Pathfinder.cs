using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class Pathfinder {
    public Vector3Int start { get; private set; }
    public int added { get; private set; }
    public int iterations { get; private set; }
    public int frontNodesProccessed { get; private set; }
    public int cameFromCount { get; private set; }
    public string threadName { get; private set; }
    public float testIso = 0;

	// Use this for initialization
	public Pathfinder(string threadName) {
        this.threadName = threadName;
        Loom.AddAsyncThread(threadName);
    }

    public void FindPath(Vector3Int start, Vector3Int goal, Action<Vector3Int[], bool> onFinish) {
        this.start = start;
        Loom.QueueAsyncTask(threadName, () => {
            bool pathBrock;
            Vector3Int[] result = Pathfind(start, goal,  out pathBrock);
            Loom.QueueOnMainThread(() => onFinish(result, pathBrock));
        });
    }

    private Vector3Int[] Pathfind(Vector3Int start, Vector3Int goal, out bool pathBrocken) {
        try {
            List<Vector3Int> frontier = new List<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            cameFrom.Clear();
            frontier.Add(start);
            cameFrom.Add(start, start);
            int maxDistance = (VoxelSettings.radius * VoxelSettings.ChunkSizeX * 2);
            double maxBlocks = (4d / 3d) * Math.PI * Math.Pow(maxDistance, 3);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            System.Threading.ManualResetEvent reset = new System.Threading.ManualResetEvent(false);
            bool goalFound = false;
            while (frontier.Count != 0 && !goalFound) {
                Vector3Int current = frontier[0];
                frontier.RemoveAt(0);
                if (Vector3.Distance(current, Vector3.zero) < maxDistance) {
                    Vector3Int[] neighbors = GetNeighbors(current);
                    for (int nIndex = 0; nIndex < neighbors.Length; nIndex++) {
                        if (!cameFrom.ContainsKey(neighbors[nIndex]) && TerrainController.Instance.GetBlock(neighbors[nIndex]).iso < testIso) {
                            frontier.Add(neighbors[nIndex]);
                            cameFrom.Add(neighbors[nIndex], current);
                            if (neighbors[nIndex] == goal)
                                goalFound = false;
                            added++;
                        }
                    }
                }
                else
                    break;
                iterations++;
            }
            frontier.Clear();
            watch.Stop();
            return Trace(cameFrom, goal, out pathBrocken);
            //SafeDebug.Log("time: " + watch.Elapsed);
        }
        catch(Exception e) {
            SafeDebug.LogException(e);
        }
        pathBrocken = true;
        return new Vector3Int[0];
    }

    private Vector3Int[] Trace(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int goal, out bool pathBrocken) {
        Vector3Int current = goal;
        pathBrocken = false;
        List<Vector3Int> path = new List<Vector3Int>();
        try {
            for (int i = 0; i < 10000; i++) {
                if (cameFrom.ContainsKey(current)) {
                    current = cameFrom[current];
                    if (!path.Contains(current))
                        path.Add(current);
                    if (current == start)
                        break;
                }
                else {
                    SafeDebug.Log("Path brocken");
                    pathBrocken = true;
                    break;
                }
            }
            path.Reverse();
        }
        catch(Exception e) {
            SafeDebug.LogException(e);
            SafeDebug.LogError("length: " + path.Count);
            throw new Exception(":(");
        }
        return path.ToArray();
    }

    private Vector3Int[] GetNeighbors(Vector3Int point) {
        return new Vector3Int[] {
            new Vector3Int(point.x - 1, point.y, point.z),
            new Vector3Int(point.x + 1, point.y, point.z),
            new Vector3Int(point.x, point.y - 1, point.z),
            new Vector3Int(point.x, point.y + 1, point.z),
            new Vector3Int(point.x, point.y, point.z - 1),
            new Vector3Int(point.x, point.y, point.z + 1)
        };
    }
        
}
