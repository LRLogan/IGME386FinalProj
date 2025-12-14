using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FinalProjManager : MonoBehaviour
{
    [SerializeField] private RoadFeatureQuery roadFeatureQuery;
    [SerializeField] private FeatureGrid roadGrid;
    private List<GameObject> roads = new List<GameObject>();


    //D*    
    [SerializeField] private float iterationDelay = 0.05f;

    [SerializeField] private Vector3Int startNode;
    [SerializeField] private Vector3Int goalNode;

    private Dictionary<Vector3Int, float> g;
    private Dictionary<Vector3Int, float> rhs;

    private float km;
    private Vector3Int lastStart;
    List<Vector3Int> open;
    Dictionary<Vector3Int, Vector2> keys;


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
        /*
         * Leving this here for now if we decide to use it
        StartCoroutine(roadFeatureQuery.QueryFeatureService(() =>
        {
            lineArray = lineBuilder.lineArray;
            AssignStartingData();
        }, loadingPannel.GetComponentInChildren<TextMeshProUGUI>()));
        */

        // Same thing as above just without the comments
        StartCoroutine(roadFeatureQuery.QueryFeatureService(()=>
        {
            roads = roadFeatureQuery.lineArray;
            roadGrid = new FeatureGrid();
            List<RoadData> allRoadData = new List<RoadData>();

            // Extracts only the data to send to the graph
            foreach(GameObject roadGO in roads)
            {
                allRoadData.Add(roadGO.GetComponent<RoadData>());
            }

            roadGrid.BuildRoadGraph(allRoadData);
            
        }));
    }




    // Update is called once per frame
    void Update()
    {
        
    }


    void DStar()
    {
        //Delay before each iteration        
        //Make sure the algo accurately finds the path on a temp weighted grid that also is able to change 

        g = new Dictionary<Vector3Int, float>();
        rhs = new Dictionary<Vector3Int, float>();

        open = new List<Vector3Int>();
        keys = new Dictionary<Vector3Int, Vector2>();

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
            Vector3Int u = PopMin();
            Vector2 kOld = keys[u];
            Vector2 kNew = CalculateKey(u);

            if (KeyLess(kOld, kNew))
            {
                open.Add(u);
                keys[u] = kNew;
            }
            else if (GetG(u) > GetRHS(u))
            {
                g[u] = GetRHS(u);

                foreach (Vector3Int s in GetPredecessors(u))
                    UpdateVertex(s);
            }
            else
            {
                g[u] = Mathf.Infinity;
                UpdateVertex(u);

                foreach (Vector3Int s in GetPredecessors(u))
                    UpdateVertex(s);
            }

            // Delay before each iteration (requirement)
            yield return new WaitForSeconds(iterationDelay);
        }
    }

    private void UpdateVertex(Vector3Int u)
    {
        if (u != goalNode)
        {
            float min = Mathf.Infinity;

            foreach (Vector3Int s in GetSuccessors(u))
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

    private Vector3Int PopMin()
    {
        Vector3Int best = open[0];
        Vector2 bestKey = keys[best];

        foreach (Vector3Int n in open)
        {
            Vector2 k = keys[n];
            if (KeyLess(k, bestKey))
            {
                best = n;
                bestKey = k;
            }
        }

        open.Remove(best);
        keys.Remove(best);
        return best;
    }

    private Vector2 TopKey()
    {
        Vector2 best = new Vector2(Mathf.Infinity, Mathf.Infinity);

        foreach (Vector3Int n in open)
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
    private float Cost(Vector3Int from, Vector3Int to)
    {
        // TEMP weighted grid (dynamic)
        //return roadFeatureQuery.GetTraversalCost(to);
        return 0;
    }

    private IEnumerable<Vector3Int> GetSuccessors(Vector3Int u)
    {
        //return roadGrid.GetNeighbors(u);
        //Temporary
        List<Vector3Int> temp = new List<Vector3Int>();
        return temp;
    }

    private IEnumerable<Vector3Int> GetPredecessors(Vector3Int u)
    {
        //return roadGrid.GetNeighbors(u);

        //Temporary
        List<Vector3Int> temp = new List<Vector3Int>();
        return temp;
    }

    //--------------Keys and heuristics-----------------
    private Vector2 CalculateKey(Vector3Int s)
    {
        float min = Mathf.Min(GetG(s), GetRHS(s));
        return new Vector2(
            min + Heuristic(startNode, s) + km,
            min
        );
    }

    private float Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private float GetG(Vector3Int n)
    {
        return g.ContainsKey(n) ? g[n] : Mathf.Infinity;
    }

    private float GetRHS(Vector3Int n)
    {
        return rhs.ContainsKey(n) ? rhs[n] : Mathf.Infinity;
    }
}
