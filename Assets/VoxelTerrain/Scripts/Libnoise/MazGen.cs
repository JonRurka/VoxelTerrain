using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;

namespace LibNoise
{
    public class MazeGen : IModule
    {
        public class Maze
        {
            public enum Direction
            {
                N = 1,
                W = 2
            }

            Stack s_stack;
            Random rand;

            public int MSIZEX;
            public int MSIZEY;
            public int MSIZEZ;
            public int[] maze_base;
            public byte[,] maze_data;

            private int iSmooth;

            #region Generating
            public void GenerateMaze(int sizeX, int sizeY, int sizeZ, int seed, int smoothness)
            {
                iSmooth = smoothness;
                MSIZEX = sizeX;
                MSIZEY = sizeY;
                MSIZEZ = sizeZ;
                maze_base = new int[MSIZEX * MSIZEZ];
                maze_data = new Byte[MSIZEX, MSIZEZ];

                s_stack = new Stack();
                rand = new Random(seed);

                MazeInit(rand);

                cMazeState state = new cMazeState(rand.Next() % MSIZEX, rand.Next() % MSIZEZ, 0);
                analyze_cell(state, rand);
            }

            void analyze_cell(cMazeState s, Random r)
            {
                bool bEnd = false, found;
                int indexSrc, indexDest, tDir = 0, prevDir = 0;

                while (true)
                {
                    if (s.dir == 15)
                    {
                        while (s.dir == 15)
                        {
                            s = (cMazeState)s_stack.pop();
                            if (s == null)
                            {
                                bEnd = true;
                                break;
                            }
                        }
                        if (bEnd == true) break;
                    }
                    else
                    {
                        do
                        {
                            prevDir = tDir;
                            tDir = (int)System.Math.Pow(2, r.Next() % 4);

                            if ((r.Next() % 32) < iSmooth)
                                if ((s.dir & prevDir) == 0)
                                    tDir = prevDir;

                            if ((s.dir & tDir) != 0)
                                found = true;
                            else
                                found = false;
                        } while (found == true && s.dir != 15);

                        s.dir |= tDir;

                        indexSrc = cell_index(s.x, s.y);

                        // direction W
                        if (tDir == 1 && s.x > 0)
                        {
                            indexDest = cell_index(s.x - 1, s.y);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y] |= (byte)Direction.W;

                                s_stack.push(new cMazeState(s));
                                s.x -= 1; s.dir = 0;
                            }
                        }

                        // direction E
                        if (tDir == 2 && s.x < MSIZEX - 1)
                        {
                            indexDest = cell_index(s.x + 1, s.y);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x + 1, s.y] |= (byte)Direction.W;

                                s_stack.push(new cMazeState(s));
                                s.x += 1; s.dir = 0;
                            }
                        }

                        // direction N
                        if (tDir == 4 && s.y > 0)
                        {
                            indexDest = cell_index(s.x, s.y - 1);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y] |= (byte)Direction.N;

                                s_stack.push(new cMazeState(s));
                                s.y -= 1; s.dir = 0;
                            }
                        }

                        // direction S
                        if (tDir == 8 && s.y < MSIZEZ - 1)
                        {
                            indexDest = cell_index(s.x, s.y + 1);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y + 1] |= (byte)Direction.N;

                                s_stack.push(new cMazeState(s));
                                s.y += 1; s.dir = 0;
                            }
                        }
                    } // else
                } // while 
            } // function
            #endregion
            #region getData
            public bool[,,] GetMaze(int xS, int zS, int cellSize)
            {
                int i, j;

                xS *= cellSize;
                zS *= cellSize;

                int xSize = xS / MSIZEX;
                int zSize = zS / MSIZEZ;

                bool[, ,] VoxelMaze = new bool[xS + 1, MSIZEZ, zS + 1];
                for (i = 0; i < MSIZEZ; i++)
                {
                    for (j = 0; j < MSIZEZ; j++)
                    {
                        if ((maze_data[i,j] & (int)Direction.N) == 0)
                        {
                            // draw voxel vertical line.
                            int startingPointX = xSize * i;
                            int EndingPointX = xSize * (i + 1);
                            int zPos = zSize * j;

                            for (int _x = startingPointX; _x < EndingPointX; _x++)
                            {
                                for (int _y = 0; _y < MSIZEY; _y++)
                                {
                                    VoxelMaze[_x, _y, zPos] = true;
                                }
                            }

                        }

                        if ((maze_data[i,j] & (int)Direction.W) == 0)
                        {
                            // draw voxel horizontal line.
                            int startingPointZ = zSize * j;
                            int endingPointZ = zSize * (j + 1);
                            int xPos = xSize * i;

                            for (int _y = 0; _y < MSIZEY; _y++)
                            {
                                for (int _z = startingPointZ; _z < endingPointZ; _z++)
                                {
                                    VoxelMaze[xPos, _y, _z] = true;
                                }
                            }
                        }
                    }
                }

                return VoxelMaze;
            }
            #endregion
            #region Cell functions
            int cell_index(int x, int y)
            {
                return MSIZEX * y + x;
            }
            int base_cell(int tIndex)
            {
                int index = tIndex;
                while (maze_base[index] >= 0)
                {
                    index = maze_base[index];
                }
                return index;
            }
            void merge(int index1, int index2)
            {
                // merge both lists
                int base1 = base_cell(index1);
                int base2 = base_cell(index2);
                maze_base[base2] = base1;
            }
            #endregion
            #region MazeInit
            void MazeInit(Random r)
            {
                int i, j;

                // maze data
                for (i = 0; i < MSIZEX; i++)
                    for (j = 0; j < MSIZEZ; j++)
                    {
                        maze_base[cell_index(i, j)] = -1;
                        maze_data[i, j] = 0;
                    }
            }


            #endregion
        }

        Maze myMaze;
        bool[, ,] Voxels;

        public MazeGen() : this(2, 2, 2, 0, 2, 1) { }

        public MazeGen(int mazeSizeX, int mazeSizeY, int mazeSizeZ, int seed, int cellSize, int smooth)
        {
            try
            {
                myMaze = new Maze();
                myMaze.GenerateMaze(mazeSizeX, mazeSizeY, mazeSizeZ, seed, smooth);
                Voxels = myMaze.GetMaze(mazeSizeX * 2, mazeSizeY * 2, cellSize);
            }
            catch (Exception e)
            {
                SafeDebug.LogException(e);
            }
        }

        public double GetValue(double x, double y, double z)
        {
            if (Voxels != null)
            {
                try
                {
                    if (IsInBounds((int)x, (int)y, (int)z))
                        return Voxels[(int)x, (int)z, (int)z] ? 1 : 0;
                    else return 0;
                }
                catch (Exception e)
                {
                    SafeDebug.LogError(string.Format("Message: {0}, \nfunction: GetValue, \nStacktrace: {1}, \nValues: x={2}/{3}, z={4}/{5}.",
                        e.Message, e.StackTrace, x, Voxels.GetLength(0), z, Voxels.GetLength(1)), e);
                }
            }
            return 0;
        }

        private bool IsInBounds(int x,int y, int z)
        {
            return ((x <= Voxels.GetLength(0) - 1) && x >= 0) && ((y <= Voxels.GetLength(1) - 1) && x >= y) && ((z <= Voxels.GetLength(2) - 1) && z >= 0);
        }
    }

    public class Stack
    {
        ArrayList tStack;

        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)tStack.GetEnumerator();
        }

        public int Count
        {
            get { return tStack.Count; }
        }

        public object push(object o)
        {
            tStack.Add(o);
            return o;
        }

        public object pop()
        {
            if (tStack.Count > 0)
            {
                object val = tStack[tStack.Count - 1];
                tStack.RemoveAt(tStack.Count - 1);
                return val;
            }
            else
                return null;
        }

        public object top()
        {
            if (tStack.Count > 0)
                return tStack[tStack.Count - 1];
            else
                return null;
        }

        public bool empty()
        {
            return (tStack.Count == 0);
        }

        public Stack() { tStack = new ArrayList(); }
    }

    public class cMazeState
    {
        public int x, y, dir;
        public cMazeState(int tx, int ty, int td) { x = tx; y = ty; dir = td; }
        public cMazeState(cMazeState s) { x = s.x; y = s.y; dir = s.dir; }
    }

    public class cCellPosition
    {
        public int x, y;
        public cCellPosition() { }
        public cCellPosition(int xp, int yp) { x = xp; y = yp; }
    }
}
