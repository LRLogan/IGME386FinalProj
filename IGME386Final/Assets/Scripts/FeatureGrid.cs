using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureGrid : MonoBehaviour
{
    public FeatureGridNode[,] roadGrid; 
    // Start is called before the first frame update
    void Start()
    {
        roadGrid = new FeatureGridNode[10, 10];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
