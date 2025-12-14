using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FinalProjManager : MonoBehaviour
{
    [SerializeField] private RoadFeatureQuery roadFeatureQuery;
    [SerializeField] private FeatureGrid roadGrid;
    private List<RoadData> roads = new List<RoadData>();

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
            /*
            roadGrid = new FeatureGrid();

            foreach(GameObject roadGO in roadFeatureQuery.lineArray)
            {
                RoadData roadData = roadGO.GetComponent<RoadData>();
            }

            roadGrid.BuildRoadGraph();
            */
        }));
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
