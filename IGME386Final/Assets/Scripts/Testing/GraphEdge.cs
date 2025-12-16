using UnityEngine;


[System.Serializable]
public class GraphEdge
{
    public GraphNode from;
    public GraphNode to;
    public float cost;
    public LineRenderer line;
}