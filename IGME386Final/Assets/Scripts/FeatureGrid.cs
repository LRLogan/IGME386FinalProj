using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Endpoint
{
    public Vector2 position; // Meters
    public string roadName;
    public int vertexIndex;

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
    private struct RoadSegment
    {
        public FeatureGridNode a;
        public FeatureGridNode b;
        public RoadData road;
    }

    Dictionary<CellIndex, List<RoadSegment>> segmentGrid;

    // The spatial hash grid itself
    Dictionary<CellIndex, List<Endpoint>> grid;

    float cellSize;         // adjust based on snapping tolerance
    float snapTolerance;    // endpoints closer than this will be merged

    public FeatureGrid(float cellSize = 15.0f, float snapTolerance = 6.0f)
    {
        this.cellSize = cellSize;
        this.snapTolerance = snapTolerance;
        grid = new Dictionary<CellIndex, List<Endpoint>>();
        segmentGrid = new Dictionary<CellIndex, List<RoadSegment>>();

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
        List<Endpoint> neighbors = GetNearbyEndpoints(ep.position);

        Endpoint otherEp;
        for (int i = 0; i < neighbors.Count; i++)
        {
            otherEp = neighbors[i];
            if (ReferenceEquals(ep, otherEp))
                continue;

            if (ep.backendRef.ownerRoad == otherEp.backendRef.ownerRoad)
                continue;

            float dist = Vector2.Distance(ep.position, otherEp.position);

            // Must be VERY close (tighten tolerance)
            if (dist > 4f)
                continue;


            // Direction sanity check (prevents parallel roads)
            Vector2 d1 = ep.backendRef.direction;
            Vector2 d2 = otherEp.backendRef.direction;

            if (Vector2.Dot(d1.normalized, d2.normalized) > 0.85f)
                continue;

            if (dist <= snapTolerance)
            {
                ep.backendRef.adjList.Add(otherEp.backendRef);
                otherEp.backendRef.adjList.Add(ep.backendRef);
            }

        }
    }

    //public void BuildRoadGraph(List<RoadData> allRoads)
    //{
    //    List<Endpoint> allEndpoints = new List<Endpoint>();
    //
    //    // Get all endpoints from roads then insert them
    //    //foreach (RoadData rd in allRoads)
    //    //{
    //    //    foreach (FeatureGridNode node in rd.backendNodes)
    //    //    {
    //    //        if (node.vertexIndex == 0 || node.vertexIndex == rd.backendNodes.Count - 1)
    //    //        {
    //    //            AddEndpoint(node, rd, allEndpoints);
    //    //        }
    //    //
    //    //    }
    //    //}
    //    foreach (RoadData rd in allRoads)
    //    {
    //        int stride = 5; // adjust if needed
    //
    //        for (int i = 0; i < rd.backendNodes.Count; i += stride)
    //        {
    //            AddEndpoint(rd.backendNodes[i], rd, allEndpoints);
    //        }
    //
    //        // Always include true endpoints
    //        AddEndpoint(rd.backendNodes[0], rd, allEndpoints);
    //        AddEndpoint(rd.backendNodes[^1], rd, allEndpoints);
    //    }
    //
    //
    //
    //
    //    // Process them for connections
    //    foreach (Endpoint ep in allEndpoints)
    //    {
    //        ProcessEndpoint(ep);
    //
    //    }
    //
    //}

    public void BuildRoadGraph(List<RoadData> allRoads)
    {
        List<Endpoint> allEndpoints = new List<Endpoint>();

        foreach (RoadData rd in allRoads)
        {
            foreach (FeatureGridNode node in rd.backendNodes)
            {
                AddEndpoint(node, rd, allEndpoints);
            }
        }


        // Existing endpoint snapping
        foreach (Endpoint ep in allEndpoints)
        {
            ProcessEndpoint(ep);
        }

        // >>> INSERTED FIX <<<
        List<RoadSegment> segments = CollectSegments(allRoads);
        ConnectIntersectingSegmentsSpatial();

    }


    /// <summary>
    /// Helper method for adding in an endpoint to the graph
    /// </summary>
    /// <param name="node"></param>
    /// <param name="rd"></param>
    /// <param name="allEndpoints"></param>
    private void AddEndpoint(
    FeatureGridNode node,
    RoadData rd,
    List<Endpoint> allEndpoints)
    {
        Endpoint ep = new Endpoint
        {
            position = node.meters,
            vertexIndex = node.vertexIndex,
            roadName = rd.roadName,
            backendRef = node
        };

        InsertEndpoint(ep);
        allEndpoints.Add(ep);
    }

    public List<FeatureGridNode> GetAllNodes()
    {
        HashSet<FeatureGridNode> nodes = new HashSet<FeatureGridNode>();

        foreach (var bucket in grid.Values)
        {
            foreach (Endpoint ep in bucket)
            {
                nodes.Add(ep.backendRef);
            }
        }

        return new List<FeatureGridNode>(nodes);
    }

    private bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float d1 = Direction(p3, p4, p1);
        float d2 = Direction(p3, p4, p2);
        float d3 = Direction(p1, p2, p3);
        float d4 = Direction(p1, p2, p4);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    private float Direction(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
    }

    private List<RoadSegment> CollectSegments(List<RoadData> roads)
    {
        segmentGrid.Clear();
        List<RoadSegment> segments = new List<RoadSegment>();

        foreach (RoadData rd in roads)
        {
            for (int i = 0; i < rd.backendNodes.Count - 1; i++)
            {
                RoadSegment s = new RoadSegment
                {
                    a = rd.backendNodes[i],
                    b = rd.backendNodes[i + 1],
                    road = rd
                };

                segments.Add(s);
                InsertSegment(s);
            }
        }

        return segments;
    }


    private void ConnectIntersectingSegmentsSpatial()
    {
        foreach (var kvp in segmentGrid)
        {
            List<RoadSegment> bucket = kvp.Value;

            for (int i = 0; i < bucket.Count; i++)
            {
                RoadSegment s1 = bucket[i];

                for (int j = i + 1; j < bucket.Count; j++)
                {
                    RoadSegment s2 = bucket[j];

                    if (s1.road == s2.road)
                        continue;

                    if (SegmentsIntersect(
                        s1.a.meters, s1.b.meters,
                        s2.a.meters, s2.b.meters))
                    {
                        s1.a.adjList.Add(s2.a);
                        s2.a.adjList.Add(s1.a);
                    }
                }
            }
        }
    }


    private void InsertSegment(RoadSegment s)
    {
        Vector2 mid = (s.a.meters + s.b.meters) * 0.5f;
        CellIndex cell = GetCellIndex(mid);

        if (!segmentGrid.TryGetValue(cell, out var bucket))
        {
            bucket = new List<RoadSegment>();
            segmentGrid[cell] = bucket;
        }

        bucket.Add(s);
    }


}
