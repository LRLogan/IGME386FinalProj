using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureGridNode : MonoBehaviour
{
    public Vector2 latLong; // Geographic coords (deg)
    public Vector2 meters; // Projected threshold used for connectivity
    public int vertexIndex; // Index in roads polyline
    public string roadName;
    public int id;
    public List<FeatureGridNode> adjList = new List<FeatureGridNode>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
