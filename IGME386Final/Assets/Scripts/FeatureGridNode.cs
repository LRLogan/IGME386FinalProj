using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureGridNode
{
    public Vector2 latLong; // Geographic coords (deg)
    public Vector2 meters; // Projected threshold used for connectivity
    public int vertexIndex; // Index in roads polyline
    public string roadName;
    public int id;
    public List<FeatureGridNode> adjList = new List<FeatureGridNode>();
    public RoadData ownerRoad;

}
