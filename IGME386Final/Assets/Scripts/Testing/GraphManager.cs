using System.Collections.Generic;
using UnityEngine;


public class GraphManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public int nodeCount = 100;
    public Vector3 spawnArea = new Vector3(10, 0, 10);
    public float connectionRadius = 6f;


    [Header("Prefabs")]
    public GameObject nodePrefab;
    public Material defaultLineMat;
    public Material pathLineMat;


    public List<GraphNode> nodes = new List<GraphNode>();


    public GraphNode startNode;
    public GraphNode goalNode;

    void Start()
    {
        SpawnNodes();
        ConnectNodes();


        startNode = nodes[0];
        goalNode = nodes[^1];


        StartCoroutine(DStarLite.Run(this, startNode, goalNode));
    }

    void SpawnNodes()
    {
        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            Random.Range(-spawnArea.y, spawnArea.y),
            Random.Range(-spawnArea.z, spawnArea.z)
            );


            GameObject obj = Instantiate(nodePrefab, pos, Quaternion.identity);
            obj.name = $"Node_{i}";


            GraphNode node = obj.AddComponent<GraphNode>();
            node.id = i;
            nodes.Add(node);
        }
    }

    void ConnectNodes()
    {
        foreach (GraphNode a in nodes)
        {
            foreach (GraphNode b in nodes)
            {
                if (a == b) continue;


                float dist = Vector3.Distance(a.Position, b.Position);
                if (dist <= connectionRadius)
                {
                    LineRenderer lr = new GameObject("Edge").AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.SetPosition(0, a.Position);
                    lr.SetPosition(1, b.Position);
                    lr.material = defaultLineMat;
                    lr.widthMultiplier = 0.1f;
                    GraphEdge edge = new GraphEdge
                    {
                        from = a,
                        to = b,
                        cost = dist,
                        line = lr
                    };


                    a.edges.Add(edge);
                }
            }
        }
    }
}