using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FinalProjManager : MonoBehaviour
{
    [SerializeField] private RoadFeatureQuery roadFeatureQuery;

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
        StartCoroutine(roadFeatureQuery.QueryFeatureService(() =>
        {
            lineArray = lineBuilder.lineArray;
            AssignStartingData();
        }, loadingPannel.GetComponentInChildren<TextMeshProUGUI>()));
        */
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
