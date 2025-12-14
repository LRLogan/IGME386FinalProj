using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Endpoint
{
    public Vector2 position; // Meters
    public string roadName;
    public int vertexIndex;
    public bool merged;

    // Index / ref to graph node if needed
    public FeatureGridNode backendRef;
}

public struct CellIndex
{
    public int x, y;

    public CellIndex(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // Required for dictionary usage
    public override bool Equals(object obj)
    {
        if (!(obj is CellIndex)) return false;

        CellIndex other = (CellIndex)obj;
        return x == other.x && y == other.y;
    }

    public override int GetHashCode()
    {
        // good enough hash for spatial grids
        return (x * 73856093) ^ (y * 19349663);
    }
}


public class FeatureGrid
{
    // The spatial hash grid itself
    Dictionary<CellIndex, List<Endpoint>> grid;

    float cellSize;         // adjust based on snapping tolerance
    float snapTolerance;    // endpoints closer than this will be merged

    public FeatureGrid(float cellSize = 10.0f, float snapTolerance = 3.0f)
    {
        this.cellSize = cellSize;
        this.snapTolerance = snapTolerance;
        grid = new Dictionary<CellIndex, List<Endpoint>>();
    }

    /// <summary>
    /// Returns the position in the grid given world position
    /// </summary>
    /// <param name="meters">world pos in meters</param>
    /// <returns></returns>
    private CellIndex GetCellIndex(Vector2 meters)
    {
        return new CellIndex(
            Mathf.FloorToInt(meters.x / cellSize),
            Mathf.FloorToInt(meters.y / cellSize)
        );
    }


    /// <summary>
    /// Inserts an endpoint into a cells bucket
    /// </summary>
    /// <param name="ep"></param>
    private void InsertEndpoint(Endpoint ep)
    {
        CellIndex cell = GetCellIndex(ep.position);

        if (!grid.TryGetValue(cell, out List<Endpoint> bucket))
        {
            bucket = new List<Endpoint>();
            grid[cell] = bucket;
        }

        bucket.Add(ep);
    }

    private List<Endpoint> GetNearbyEndpoints(Vector2 pos)
    {
        List<Endpoint> results = new List<Endpoint>();
        CellIndex baseCell = GetCellIndex(pos);

        // Looping through the cells neighbors 
        for(int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                CellIndex neighborCell = new CellIndex(baseCell.x + i, baseCell.y + j);

                if (grid.TryGetValue(neighborCell, out List<Endpoint> bucket))
                    results.AddRange(bucket);
            }
        }

        return results;
    }

    private void ProcessEndpoint(Endpoint ep)
    {
        if (ep.merged) return;

        List<Endpoint> neighbors = GetNearbyEndpoints(ep.position);

        Endpoint otherEp;
        for (int i = 0; i < neighbors.Count; i++)
        {
            otherEp = neighbors[i];
            // No need to check the same ep or if it is merged
            if (ReferenceEquals(ep, otherEp) || otherEp.merged)
            {
                continue;
            }

            float dist = UnityEngine.Vector2.Distance(ep.position, otherEp.position);

            // This is where if the two roads should be connected, connect them here
            if(dist <= snapTolerance)
            {
                ep.backendRef.adjList.Add(otherEp.backendRef);
                otherEp.backendRef.adjList.Add(ep.backendRef);
                ep.merged = true;
                otherEp.merged = true;
            }
        }
    }

    public void BuildRoadGraph(List<RoadData> allRoads)
    {
        List<Endpoint> allEndpoints = new List<Endpoint>();

        // Get all endpoints from roads then insert them
        foreach (RoadData rd in allRoads)
        {
            foreach (FeatureGridNode node in rd.backendNodes)
            {
                Endpoint ep = new Endpoint();
                ep.position = node.meters;   
                ep.vertexIndex = node.vertexIndex;
                ep.roadName = rd.roadName;
                ep.backendRef = node;

                Debug.Log("Building endpoint: " + ep.roadName);
                allEndpoints.Add(ep);
                InsertEndpoint(ep);
            }
        }

        // Process them for connections
        foreach (Endpoint ep in allEndpoints)
        {
            ProcessEndpoint(ep);
        }
    }
}
