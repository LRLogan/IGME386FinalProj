using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FinalProjManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingPannel;

    [SerializeField] private RoadFeatureQuery roadFeatureQuery;
    [SerializeField] private FeatureGrid roadGrid;
    [SerializeField] private FeatureGridNode roadGridNode;
    private List<GameObject> roads = new List<GameObject>();


    private Dictionary<FeatureGridNode, Vector3> nodeWorldPositions;
    [SerializeField] private Material pathMaterial;
    [SerializeField] private float pathWidth = 4f;

    private Dictionary<string, RoadData> roadsToData = new Dictionary<string, RoadData>();

    //D*    
    [SerializeField] private float iterationDelay = 0.05f;

    //[SerializeField] private Vector3Int startNode;
    //[SerializeField] private Vector3Int goalNode;
    private FeatureGridNode startNode;
    private FeatureGridNode goalNode;

    Dictionary<FeatureGridNode, float> g;
    Dictionary<FeatureGridNode, float> rhs;
    List<FeatureGridNode> open;
    Dictionary<FeatureGridNode, Vector2> keys;
    private float km; 
    private FeatureGridNode lastStart;

    // Start is called before the first frame update
    void Start()
    {
        StartSimulation();
    }

    /// <summary>
    /// Using Start as a wrapper function to start
    /// </summary>
    private void StartSimulation()
    {
        loadingPannel = loadingPannel.GetComponentInChildren<TextMeshProUGUI>();

        // Same thing as above just without the comments
        StartCoroutine(roadFeatureQuery.QueryFeatureService(()=>
        {
            roads = roadFeatureQuery.lineArray;
            roadGrid = new FeatureGrid();
            List<RoadData> allRoadData = new List<RoadData>();

            // Extracts only the data to send to the graph
            foreach (GameObject roadGO in roads)
            {
                allRoadData.Add(roadGO.GetComponent<RoadData>());
                roadsToData[roadGO.name] = roadGO.GetComponent<RoadData>();
            }

            roadGrid.BuildRoadGraph(allRoadData);

            //roadsDone = true;
            List<FeatureGridNode> nodes = roadGrid.GetAllNodes();

            startNode = roadsToData["Beaver St"].backendNodes[0];                     
            goalNode = roadsToData["Water St"].backendNodes[0];        

            DStar();

            nodeWorldPositions = new Dictionary<FeatureGridNode, Vector3>();

            foreach (GameObject roadGO in roads)
            {
                RoadData rd = roadGO.GetComponent<RoadData>();
                if (rd == null) continue;

                // Each RoadSegment child has a LineRenderer
                foreach (Transform child in roadGO.transform)
                {
                    LineRenderer lr = child.GetComponent<LineRenderer>();
                    if (lr == null) continue;

                    int i = 0;
                    foreach (FeatureGridNode n in rd.backendNodes)
                    {
                        if (i < lr.positionCount)
                            nodeWorldPositions[n] = lr.GetPosition(i);
                        i++;
                    }
                }
            }


        }, loadingPannel));

        //while (!roadsDone)
        //    yield return null;

        //List<FeatureGridNode> nodes = roadGrid.GetAllNodes();
        //
        //startNode = nodes[0];                     // example
        //goalNode = nodes[nodes.Count - 1];        // example
        //
        //DStar();



    }




    // Update is called once per frame
    void Update()
    {
        
    }


    private List<FeatureGridNode> ExtractPath()
    {
        List<FeatureGridNode> path = new List<FeatureGridNode>();

        FeatureGridNode current = startNode;
        path.Add(current);

        // If even the start has infinite g, nothing is reachable
        if (float.IsInfinity(GetG(startNode)))
        {
            Debug.LogWarning("Start node cannot reach any goal-related component.");
            return path;
        }

        int safety = 0;

        while (current != goalNode && safety < 10000)
        {
            FeatureGridNode best = null;
            float bestScore = Mathf.Infinity;

            foreach (FeatureGridNode s in GetSuccessors(current))
            {
                float c = Cost(current, s);
                float gS = GetG(s);

                if (float.IsInfinity(gS))
                    continue;

                float score = gS + c;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }

            // No improving successor, we are at the frontier of reachability
            if (best == null)
            {
                Debug.LogWarning(
                    "Goal unreachable. Path terminated at closest reachable node."
                );
                break;
            }

            // Prevent loops
            if (path.Contains(best))
                break;

            current = best;
            path.Add(current);
            safety++;
        }

        return path;
    }

    public void DStar()
    {
        Debug.Log("DSTARING ALL OVER THE PLACE");
        //Delay before each iteration        
        //Make sure the algo accurately finds the path on a temp weighted grid that also is able to change 

        g = new Dictionary<FeatureGridNode, float>();
        rhs = new Dictionary<FeatureGridNode, float>();

        open = new List<FeatureGridNode>();
        keys = new Dictionary<FeatureGridNode, Vector2>();

        km = 0f;
        lastStart = startNode;

        rhs[goalNode] = 0f;
        g[goalNode] = Mathf.Infinity;

        open.Add(goalNode);
        keys[goalNode] = CalculateKey(goalNode);

        //D* loop
        StartCoroutine(ComputeShortestPath());
    }

    private IEnumerator ComputeShortestPath()
    {
        while (open.Count > 0 &&
              (KeyLess(TopKey(), CalculateKey(startNode)) ||
               !Mathf.Approximately(GetG(startNode), GetRHS(startNode))))
        {

            FeatureGridNode u = PopMin(out Vector2 kOld);
            Vector2 kNew = CalculateKey(u);

            //Vector2 kOld = keys[u];
            //Vector2 kNew = CalculateKey(u);



            if (KeyLess(kOld, kNew))
            {
                open.Add(u);
                keys[u] = kNew;
            }
            else if (GetG(u) > GetRHS(u))
            {
                g[u] = GetRHS(u);

                foreach (FeatureGridNode s in GetPredecessors(u))
                    UpdateVertex(s);
            }
            else
            {
                g[u] = Mathf.Infinity;
                UpdateVertex(u);

                foreach (FeatureGridNode s in GetPredecessors(u))
                    UpdateVertex(s);
            }

            //delay before each iteration
            yield return new WaitForSeconds(iterationDelay);
        }

        List<FeatureGridNode> path = ExtractPath();
        Debug.Log($"Path length: {path.Count}");
        HighlightPath(path);


    }

    private void UpdateVertex(FeatureGridNode u)
    {
        if (u != goalNode)
        {
            float min = Mathf.Infinity;

            foreach (FeatureGridNode s in GetSuccessors(u))
            {
                float c = Cost(u, s);
                if (c < Mathf.Infinity)
                    min = Mathf.Min(min, GetG(s) + c);
            }

            rhs[u] = min;
        }

        open.Remove(u);
        keys.Remove(u);

        if (!Mathf.Approximately(GetG(u), GetRHS(u)))
        {
            open.Add(u);
            keys[u] = CalculateKey(u);
        }
    }

    private FeatureGridNode PopMin(out Vector2 kOld)
    {
        FeatureGridNode best = open[0];
        Vector2 bestKey = keys[best];

        foreach (FeatureGridNode n in open)
        {
            Vector2 k = keys[n];
            if (KeyLess(k, bestKey))
            {
                best = n;
                bestKey = k;
            }
        }

        if(open.Contains(best))
            open.Remove(best);
        keys.Remove(best);

        kOld = bestKey;
        return best;
    }


    private Vector2 TopKey()
    {
        Vector2 best = new Vector2(Mathf.Infinity, Mathf.Infinity);

        foreach (FeatureGridNode n in open)
        {
            Vector2 k = keys[n];
            if (KeyLess(k, best))
                best = k;
        }

        return best;
    }

    private bool KeyLess(Vector2 a, Vector2 b)
    {
        return a.x < b.x || (Mathf.Approximately(a.x, b.x) && a.y < b.y);
    }


    //--------------Cost and grid queries--------------
    private float Cost(FeatureGridNode from, FeatureGridNode to)
    {
        return Vector2.Distance(from.meters, to.meters);
    }


    private IEnumerable<FeatureGridNode> GetSuccessors(FeatureGridNode u)
    {
        return u.adjList;
    }

    private IEnumerable<FeatureGridNode> GetPredecessors(FeatureGridNode u)
    {
        return u.adjList;
    }

    //--------------Keys and heuristics-----------------
    private Vector2 CalculateKey(FeatureGridNode s)
    {
        float min = Mathf.Min(GetG(s), GetRHS(s));
        return new Vector2(
            min + Heuristic(startNode, s) + km,
            min
        );
    }

    private float GetG(FeatureGridNode n)
    {
        return g.TryGetValue(n, out float v) ? v : Mathf.Infinity;
    }

    private float GetRHS(FeatureGridNode n)
    {
        return rhs.TryGetValue(n, out float v) ? v : Mathf.Infinity;
    }

    private float Heuristic(FeatureGridNode a, FeatureGridNode b)
    {
        return Vector2.Distance(a.meters, b.meters);
    }

    private void HighlightPath(List<FeatureGridNode> path)
    {
        HashSet<RoadData> roadsOnPath = new HashSet<RoadData>();

        foreach (var node in path)
        {
            if (node.ownerRoad != null)
                roadsOnPath.Add(node.ownerRoad);
        }

        foreach (var rd in roadsOnPath)
        {
            rd.UpdateCValAndGrad(500f); // force red
        }
        Debug.Log($"Roads highlighted: {roadsOnPath.Count}");

    }


}
