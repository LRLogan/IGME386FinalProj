using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct Endpoint
{
    public Vector2 position;
    public string roadName;
    public int vertexIndex;
    public bool merged;
}

public struct CellIndex
{
    public int x, y;

    public CellIndex(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class FeatureGrid : MonoBehaviour
{
    // The spatial hash grid itself
    Dictionary<CellIndex, List<Endpoint>> grid;

    float cellSize = 5.0f;             // adjust based on snapping tolerance
    float snapTolerance = 3.0f;        // endpoints closer than this will be merged

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Returns the position in the grid given world position
    /// </summary>
    /// <param name="worldPos">world pos</param>
    /// <returns></returns>
    private CellIndex GetCellIndex(Vector2 worldPos)
    {
        return new CellIndex((int)Mathf.Floor(worldPos.x / cellSize),
            (int)Mathf.Floor(worldPos.y / cellSize));
    }

    /// <summary>
    /// Inserts an endpoint into a cells bucket
    /// </summary>
    /// <param name="ep"></param>
    private void InsertEndpoint(Endpoint ep)
    {
        CellIndex cell = GetCellIndex(ep.position);

        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<Endpoint>();
        }
        
        grid[cell].Add(ep);
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

                if (grid.ContainsKey(neighborCell))
                    results.AddRange(grid[neighborCell]);
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
                ep.merged = true;
                otherEp.merged = true;
            }
        }
    }

    public void BuildRoadGraph()
    {
        grid.Clear();

        // Get all endpoints from roads

        // Insert all endpoints into grid

        // Process them for connections
    }
}
