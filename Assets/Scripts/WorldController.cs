using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public Vector2Int playerPositionPolarTile;
    public Transform playerView;
    public Transform camera;

    public SpriteMask ringParentPrefab;

    public int numSegments = 16;
    public float radius;

    public GameObject tilePrefab;
    public float halfTileSize = 0.5f;

    public Color tempTileColor;

    public List<GameObject> tempTileDump;
    public Transform[] instantiatedMap;

    public int mapDepth = 16; // How many rings in can we go.

    private void OnValidate()
    {
        radius = (numSegments / Mathf.PI) / 2.0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        tempTileDump = new List<GameObject>();
        instantiatedMap = new Transform[mapDepth];

        for (int i = 0; i < mapDepth; i++)
        {
            var ring = GenerateRing(i);
            instantiatedMap[i] = ring;
            ring.transform.parent = transform;
        }

        UpdateRingScales();
        UpdatePositions();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateRingScales();
        }

        // TODO WT: need collision checks
        if (Input.GetButtonDown("Up"))
        {
            playerPositionPolarTile.y++;
            UpdatePositions();
            UpdateRingScales();
        }

        if (Input.GetButtonDown("Down"))
        {
            playerPositionPolarTile.y--;
            UpdatePositions();
            UpdateRingScales();
        }
    }

    void UpdatePositions()
    {
        playerView.position = new Vector2(0.0f, -radius);
        camera.position = new Vector3(playerView.position.x, playerView.position.y, camera.position.z);
    }

    void UpdateRingScales()
    {
        for(int i = 0; i < mapDepth; i++)
        {
            var playerDist = i - playerPositionPolarTile.y;
            var scale = 1.0f - ((float)playerDist / (float)mapDepth);

            var ring = instantiatedMap[i];
            ring.localScale = new Vector3(scale, scale, scale);

            var mask = ring.GetComponent<SpriteMask>();
            mask.alphaCutoff = (Mathf.Abs(playerDist) / 3.0f);

            Debug.Log(scale);
        }
    }

    Color getColorVariant()
    {
        float h, s, v;
        Color.RGBToHSV(tempTileColor, out h, out s, out v);
        h += UnityEngine.Random.value / 10.0f;
        s += UnityEngine.Random.value / 10.0f;
        v += UnityEngine.Random.value / 10.0f;

        return Color.HSVToRGB(h, s, v);
    }

    Transform GenerateRing(int ringDepthId)
    {
        var parent = Instantiate(ringParentPrefab).transform;
        parent.gameObject.name = "Ring_" + ringDepthId;

        for(int i = 0; i < numSegments; i++)
        {
            var theta = ((float)i /  (float)numSegments) * (Mathf.PI * 2.0f);
            var x = Mathf.Cos(theta);
            var y = Mathf.Sin(theta);

            var tile = Instantiate(tilePrefab, new Vector3(x, y, 0) * (radius - halfTileSize), Quaternion.Euler(new Vector3(0.0f, 0.0f, theta * Mathf.Rad2Deg)), parent);
            tile.GetComponent<SpriteRenderer>().color = getColorVariant();
            // UV color for ease.
            tile.GetComponent<SpriteRenderer>().color = new Color(i / (float)numSegments, ringDepthId / (float)mapDepth, 0.0f);

            tempTileDump.Add(tile);
        }

        return parent;
    }
}
