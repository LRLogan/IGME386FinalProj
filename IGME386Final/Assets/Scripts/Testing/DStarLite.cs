using System.Collections;
using UnityEngine;


public static class DStarLite
{
    public static IEnumerator Run(GraphManager graph, GraphNode start, GraphNode goal)
    {
        PriorityQueue<GraphNode> open = new();


        goal.rhs = 0;
        open.Enqueue(goal, Heuristic(start, goal));


        while (open.Count > 0)
        {
            GraphNode current = open.Dequeue();


            if (current == start)
                break;


            current.g = current.rhs;

            foreach (GraphEdge edge in current.edges)
            {
                GraphNode neighbor = edge.to;
                float tentative = current.g + edge.cost;


                if (tentative < neighbor.rhs)
                {
                    neighbor.rhs = tentative;
                    if (!open.Contains(neighbor))
                        open.Enqueue(neighbor, tentative + Heuristic(neighbor, start));
                }
            }
        }

        // Reconstruct path visually
        GraphNode pathNode = start;
        while (pathNode != goal)
        {
            GraphEdge bestEdge = null;
            float bestCost = Mathf.Infinity;


            foreach (GraphEdge edge in pathNode.edges)
            {
                float cost = edge.cost + edge.to.g;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestEdge = edge;
                }
            }
            if (bestEdge == null)
                yield break;


            bestEdge.line.SetColors(Color.red, Color.red);
            pathNode = bestEdge.to;
            pathNode.gameObject.GetComponent<MeshRenderer>().material = graph.pathLineMat;


            yield return new WaitForSeconds(0.3f);
        }
    }


    static float Heuristic(GraphNode a, GraphNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }
}