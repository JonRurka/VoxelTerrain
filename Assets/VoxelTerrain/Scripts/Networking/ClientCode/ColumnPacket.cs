using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class ColumnPacket
{
    public Vector2Int Location { get; private set; }
    public float[] Heightmap { get; private set; }

    private int[][] data;

    public ColumnPacket(Vector2Int location, float[] heightmap, int[][] data)
    {
        Location = location;
        Heightmap = heightmap;
        this.data = data;
    }

    public int[] GetBlocks(int y)
    {
        if (y > 0 && y < data.Length)
        {
            return data[y];
        }
        return new int[0];
    }
}
