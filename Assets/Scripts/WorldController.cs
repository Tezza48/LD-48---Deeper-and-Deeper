using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public PlayerController player;
    new public Camera camera;

    public int mapWidth = 16;
    public int mapDepth = 16;

    public GameObject tilePrefab;
    public SpriteRenderer fogLayerPrefab;

    public Gradient groundColorGradient;
    public Gradient fogColourGradient;
    public SpriteRenderer[] fogLayers;

    public SpriteRenderer[,] mainMap;
    public Transform mapContainer;

    public SpriteRenderer[,] cloneMap;
    public Transform cloneMapContainer;

    public List<GameObject> Entities;
    //public SpriteRenderer[,] wrapMap; // Map used when player is close to the edge, to fill in the gaps


    private void OnValidate()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
        player.onPlayerMoved.AddListener(handlePlayerMoved);
        handlePlayerMoved();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void handlePlayerMoved()
    {
        // Clamp vertically
        player.position.y = Mathf.Clamp(player.position.y, -mapDepth + 1, 0);

        // Wrap horizontally
        player.position.x = (mapWidth + player.position.x) % mapWidth;

        player.transform.position = (Vector2)player.position;

        camera.transform.position = new Vector3(player.position.x, player.position.y, camera.transform.position.z);

        cloneMapContainer.position = new Vector2((player.position.x < mapWidth / 2.0f) ? -mapWidth : mapWidth, 0.0f);

        for(int i = 0; i < 5; i++)
        {
            var layerDistToPlayer = Mathf.Abs(i - 2);
            var color = fogColourGradient.Evaluate(-(float)player.position.y / (float)mapDepth);
            color.a = ((float)layerDistToPlayer / 2.0f);
            Debug.Log(color);
            fogLayers[i].color = color;
        }

        camera.backgroundColor = fogColourGradient.Evaluate(-(float)player.position.y / (float)mapDepth);
    }

    Color getColorVariant(int y)
    {
        float h, s, v;
        Color.RGBToHSV(groundColorGradient.Evaluate((float)y / (float)mapDepth), out h, out s, out v);
        h += UnityEngine.Random.value / 10.0f;
        s += UnityEngine.Random.value / 10.0f;
        v += UnityEngine.Random.value / 10.0f;

        return Color.HSVToRGB(h, s, v);
    }

    public void GenerateMap()
    {
        mapContainer = new GameObject("MapContainer").transform;
        mainMap = new SpriteRenderer[mapWidth, mapDepth];

        for (int y = 0; y < mapDepth; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                var tile = Instantiate(tilePrefab, new Vector3(x, -y, 0.0f), Quaternion.identity, mapContainer);
                var renderer = tile.GetComponent<SpriteRenderer>();
                renderer.color = getColorVariant(y);
                // UV color for ease.
                //renderer.color = new Color(x / (float)mapWidth, y/ (float)mapDepth, 0.0f);

                mainMap[x, y] = renderer;
            }
        }

        GenerateMapClone();
        GenerateFogLayers();
    }

    public void GenerateMapClone()
    {
        cloneMapContainer = new GameObject("CloneMapContainer").transform;
        cloneMap = new SpriteRenderer[mapWidth, mapDepth];

        for (int y = 0; y < mapDepth; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                cloneMap[x, y] = Instantiate(mainMap[x, y], cloneMapContainer);
            }
        }
    }

    public void GenerateFogLayers()
    {
        fogLayers = new SpriteRenderer[5];

        fogLayers[0] = Instantiate(fogLayerPrefab, player.position + new Vector2(0.0f, -4.0f), Quaternion.identity, player.transform);
        fogLayers[4] = Instantiate(fogLayerPrefab, player.position + new Vector2(0.0f, 4.0f), Quaternion.identity, player.transform);

        fogLayers[0].transform.localScale = fogLayers[4].transform.localScale = new Vector3(fogLayers[4].transform.localScale.x, 5.0f);

        for(int i = 1; i < 4; i++)
        {
            fogLayers[i] = Instantiate(fogLayerPrefab, player.position + new Vector2(0.0f, i -2.0f), Quaternion.identity, player.transform);
        }
    }
}
