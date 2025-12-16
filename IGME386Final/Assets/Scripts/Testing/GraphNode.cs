using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to the nodes in the scene
/// </summary>
public class GraphNode : MonoBehaviour
{
    public int id;
    public List<GraphEdge> edges = new List<GraphEdge>();


    // D* Lite values
    public float g = Mathf.Infinity;
    public float rhs = Mathf.Infinity;


    public Vector3 Position => transform.position;
}