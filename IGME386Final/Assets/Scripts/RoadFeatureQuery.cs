using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Modular road builder using lines Written by Logan Larrondo
/// </summary>
public class RoadFeatureQuery : MonoBehaviour
{
    [Header("ArcGIS / Query")]
    public ArcGISMapComponent mapComponent;
    [Tooltip("ArcGIS FeatureServer query URL (without trailing parameters)")]
    public string featureServiceUrl =
        "https://services2.arcgis.com/FiaPA4ga0iQKduv3/arcgis/rest/services/Transportation_v1/FeatureServer/7/query";

    [Header("Bounding Box (if querying web) - use 4326 or 3857 depending on service")]
    // Default to NYC (In unity coords)
    [SerializeField] private double xMin = -8265194.95;
    [SerializeField] private double xMax = -8211773.97;
    [SerializeField] private double yMin = 4934181.47;
    [SerializeField] private double yMax = 4978649.49;

    [Header("Rendering Settings")]
    public Material roadMaterial;
    public float lineWidth = 1.5f;
    public float heightOffset = 5f;

    public bool groupByName = false;            // set true to parent segments by NAME

    public List<GameObject> lineArray;          // Holds all parent objs

    private void Start()
    {
        if (mapComponent == null)
            Debug.LogError("ArcGISMapComponent not assigned. Assign it in inspector.");

        if (roadMaterial == null)
            Debug.LogWarning("roadMaterial not assigned — assign a visible Unlit/Color material.");

        lineArray = new List<GameObject>();
    }

    public IEnumerator QueryFeatureService(Action onComplete, TextMeshProUGUI progressDisplay)
    {
        string geometry;
        string outSR;
        geometry = $"{{\"xmin\":-74.2591,\"ymin\":40.4774,\"xmax\":-73.7004,\"ymax\":40.9176,\"spatialReference\":{{\"wkid\":4326}}}}";
        outSR = "4326";

        int resultOffset = 0;
        int maxRecordCount = 2000;
        bool hasMore = true;

        List<JToken> allFeatures = new List<JToken>();

        // Makes sure to get all data from the feature layer (gotten in chunks to optimize program / bypass limit)
        while (hasMore)
        {
            string url =
                $"{featureServiceUrl}?f=geojson&where=1%3D1" +
                $"&geometry={UnityWebRequest.EscapeURL(geometry)}" +
                "&geometryType=esriGeometryEnvelope" +
                "&spatialRel=esriSpatialRelIntersects" +
                "&outFields=NAME,OBJECTID" +
                "&returnGeometry=true" +
                $"&outSR={outSR}" +
                $"&resultOffset={resultOffset}" +
                $"&resultRecordCount={maxRecordCount}";

            progressDisplay.text = "Traffic visualizer simulation now loading: \nFetching road data. This will take a moment." +
                $"\nRequesting records {resultOffset}–{resultOffset + maxRecordCount}";
            Debug.Log($"Requesting records {resultOffset}–{resultOffset + maxRecordCount}");

            // In the end this code gets the data in geoJSON format while catching exceptions
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Feature query failed at offset {resultOffset}: {request.error}");
                    yield break;
                }

                string json = request.downloadHandler.text;
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError("Empty response from FeatureServer.");
                    yield break;
                }

                JObject root;
                try
                {
                    root = JObject.Parse(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse GeoJSON at offset {resultOffset}: {e}");
                    yield break;
                }

                JArray features = (JArray)root["features"];
                if (features == null || features.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                allFeatures.AddRange(features);

                Debug.Log($"Fetched {features.Count} features at offset {resultOffset}");

                if (features.Count < maxRecordCount)
                    hasMore = false;
                else
                    resultOffset += maxRecordCount;
            }
        }

        if (allFeatures.Count == 0)
        {
            Debug.LogWarning("No features returned after all pages.");
            yield break;
        }

        Debug.Log($"Total features collected: {allFeatures.Count}");
        CreateRoadLines(new JArray(allFeatures));
        onComplete.Invoke();
    }


    private void CreateRoadLines(JArray features)
    {
        // Parent segments by street name
        Dictionary<string, Transform> groupParents = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        int drawn = 0;
        foreach (JToken feature in features)
        {
            try
            {
                var geom = feature["geometry"];
                if (geom == null) continue;

                string geomType = geom["type"]?.ToString();
                if (geomType != "LineString" && geomType != "MultiLineString")
                    continue;

                string name = feature["properties"]?["NAME"]?.ToString() ?? "Unnamed";

                // Ensure a parent Group if requested
                Transform parent = transform;
                if (groupByName)
                {
                    if (!groupParents.TryGetValue(name, out Transform p))
                    {
                        GameObject go = new GameObject(name);
                        go.transform.parent = transform;
                        groupParents[name] = go.transform;
                        parent = go.transform;
                        lineArray.Add(go);

                        // Add RoadData and set its name
                        RoadData data = go.AddComponent<RoadData>();
                        data.roadName = name;
                    }
                    else parent = p;
                }

                JArray coords = geom["coordinates"] as JArray;
                if (coords == null) continue;
                DrawLineString(coords, parent.transform);
                drawn++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error processing feature: {e.Message}");
            }
        }

        Debug.Log($"Finished drawing {drawn} segments.");
    }

    private void DrawLineString(JArray coords, Transform parent)
    {
        // Checks for missing data
        if (coords == null || coords.Count < 2) return;

        RoadData roadData = parent.GetComponent<RoadData>();
        if (roadData == null)
        {
            Debug.LogError("RoadData missing on road parent!");
            return;
        }

        List<Vector3> unityPositions = new List<Vector3>(coords.Count);
        bool hasInvalid = false;
        FeatureGridNode prevNode = null;
        int vertexIndex = 0;

        foreach (JToken token in coords)
        {
            // Expect [lon, lat] (or x/y depending on outSR)
            if (!(token is JArray pair) || pair.Count < 2)
            {
                hasInvalid = true;
                continue;
            }

            double lon = pair[0].Value<double>();
            double lat = pair[1].Value<double>();

            // Creating the feature gird node
            FeatureGridNode node = new FeatureGridNode
            {
                latLong = new Vector2((float)lon, (float)lat),
                vertexIndex = vertexIndex,
                roadName = roadData.roadName,
                id = roadData.backendNodes.Count
            };

            // Setting adjancey for lines in the same road
            if (prevNode != null)
            {
                prevNode.adjList.Add(node);
                node.adjList.Add(prevNode);
            }

            roadData.backendNodes.Add(node);
            prevNode = node;
            vertexIndex++;

            // Use 4326 geographic points for SDK to project properly
            ArcGISPoint gisPoint;
            gisPoint = new ArcGISPoint(lon, lat, heightOffset, new ArcGISSpatialReference(4326));

            Vector3 enginePos;
            try
            {
                enginePos = mapComponent.GeographicToEngine(gisPoint);
                if (float.IsNaN(enginePos.x) || float.IsNaN(enginePos.y) || float.IsNaN(enginePos.z) ||
                    float.IsInfinity(enginePos.x) || float.IsInfinity(enginePos.y))
                {
                    hasInvalid = true;
                    continue;
                }
            }
            catch (Exception)
            {
                hasInvalid = true;
                continue;
            }

            unityPositions.Add(enginePos);
        }

        // Create line object and LineRenderer
        GameObject lineObj = new GameObject("RoadSegment");
        lineObj.transform.parent = parent;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.positionCount = unityPositions.Count;
        lr.SetPositions(unityPositions.ToArray());

        // Ensure a material is present
        if (roadMaterial == null)
        {
            Debug.LogError("roadMaterial is NULL Assign in inspector!");
        }
        else
        {
            lr.material = roadMaterial;
        }

        // Set width in a backward-compatible way
        lr.widthMultiplier = lineWidth;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}