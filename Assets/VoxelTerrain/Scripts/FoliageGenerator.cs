using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageGeneratorBase
{
    protected ISampler heightSampler;
    protected int sizeX;
    protected int sizeZ;
    protected int perBlock;


    public FoliageGeneratorBase(int sizeX, int sizeZ, int perBlock, ISampler sampler)
    {
        this.sizeX = sizeX;
        this.sizeZ = sizeZ;
        this.perBlock = perBlock;
        this.heightSampler = sampler;
    }

    public void Generate()
    {

    }

    public virtual uint GetType(int x, int z)
    {
        return 0;
    }
}
