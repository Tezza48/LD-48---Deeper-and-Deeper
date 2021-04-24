using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class WorldController : MonoBehaviour
{
    public PlayerController player;
    public int hitPoints = 3;

    public GameObject UI_HitpointContainer;
    public GameObject UI_HitpointPrefab;

    public List<GameObject> hitpointDisplays;

    new public Camera camera;

    public const int MAP_WIDTH = 16;
    public const int MAP_HEIGHT = 32;

    public GameObject tilePrefab;
    public SpriteRenderer fogLayerPrefab;

    public Gradient groundColorGradient;
    public Gradient fogColourGradient;
    public SpriteRenderer[] fogLayers;

    public SpriteRenderer[,] mainMap;
    public Transform mapContainer;

    public SpriteRenderer[,] cloneMap;
    public Transform cloneMapContainer;

    //public List<GameObject> Entities;

    private List<EnemyState> enemies;
    //public SpriteRenderer[,] wrapMap; // Map used when player is close to the edge, to fill in the gaps
    private List<Plant> plants;

    public float tickLength = 1.0f;
    public float lastTickTime;


    private void OnValidate()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        lastTickTime = Time.time;

        plants = FindObjectsOfType<Plant>().ToList();
        enemies = FindObjectsOfType<EnemyState>().ToList();

        GenerateMap();
        player.onPlayerMoved.AddListener(handlePlayerMoved);
        handlePlayerMoved();

        Tick();

        hitpointDisplays = new List<GameObject>();
        for (int i = 0; i < hitPoints; i++)
        {
            hitpointDisplays.Add(Instantiate(UI_HitpointPrefab, UI_HitpointContainer.transform));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTickTime + tickLength < Time.time)
        {
            lastTickTime = Time.time;

            Tick();
        }
    }

    void Tick()
    {
        foreach (var plant in plants)
        {
            var toRemove = new List<EnemyState>();

            enemies.ForEach((enemy) =>
            {
                var toEnemy = getWrappedDirectionTo(plant, enemy);
                if (toEnemy.sqrMagnitude < 1.5)
                {
                    plant.Eat();
                    toRemove.Add(enemy);
                }
            });

            toRemove.ForEach((enemy) =>
            {
                Destroy(enemy.gameObject);
                enemies.Remove(enemy);
            });

            plant.transform.position = (Vector2)plant.position; // Shouldn't need to do this but doing it anyway
        }

        foreach (var enemy in enemies)
        {
            var toPlayer = getWrappedDirectionTo(enemy, player);

            if (Mathf.Abs(toPlayer.y) <= 2 && Mathf.Abs(toPlayer.x) < 5)
            {
                bool canHurt = toPlayer.sqrMagnitude < 1.5f;
                if (canHurt)
                {
                    hurtPlayer();
                }

                if (Mathf.Abs(toPlayer.x) > 1.0)
                {
                    float xDir = Mathf.Clamp(toPlayer.x, -1, 1);

                    enemy.position.x += (int)xDir;
                    enemy.position.x = (MAP_WIDTH + enemy.position.x) % MAP_WIDTH;
                }

                if (!enemy.isLayerLocked)
                {
                    enemy.position.y += Mathf.Clamp(toPlayer.y, -1, 1);
                }
            }

            enemy.transform.position = (Vector2)enemy.position;
        }
    }

    void hurtPlayer()
    {
        hitPoints--;

        for (int i = 0; i < hitpointDisplays.Count; i++)
        {
            hitpointDisplays[i].SetActive(i <= hitPoints);
        }

        if (hitPoints == 0)
        {
            // TODO WT: Add a transition.
            SceneManager.LoadScene("MenuScene");
        }
    }

    private Vector2Int getWrappedDirectionTo(GridEntity from, GridEntity to)
    {
        Vector2Int result = new Vector2Int();
        result.y = to.position.y - from.position.y;


        int reg = to.position.x - from.position.x;
        int leftWrap = (to.position.x - MAP_WIDTH) - from.position.x;
        int rightWrap = (to.position.x + MAP_WIDTH) - from.position.x;

        int absReg = Mathf.Abs(reg);
        int absLeft = Mathf.Abs(leftWrap);
        int absRight = Mathf.Abs(rightWrap);

        int minX = Mathf.Min(absReg, absLeft, absRight);

        if (minX == absReg)
        {
            result.x = reg;
        } else if (minX == absLeft)
        {
            result.x = leftWrap;
        } else
        {
            result.x = rightWrap;
        }

        return result;
    }

    private void handlePlayerMoved()
    {
        // Clamp vertically
        player.position.y = Mathf.Clamp(player.position.y, -MAP_HEIGHT + 1, 0);

        // Wrap horizontally
        player.position.x = (MAP_WIDTH + player.position.x) % MAP_WIDTH;

        // TODO WT: Figure if i can lerp AND have it nicelywrap on the edges.
        player.transform.position = (Vector2)player.position;
        camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camera.transform.position.z);

        cloneMapContainer.position = new Vector2((player.position.x < MAP_WIDTH / 2.0f) ? -MAP_WIDTH : MAP_WIDTH, 0.0f);

        for(int i = 0; i < 5; i++)
        {
            var layerDistToPlayer = Mathf.Abs(i - 2);
            var color = fogColourGradient.Evaluate(-(float)player.position.y / (float)MAP_HEIGHT);
            color.a = ((float)layerDistToPlayer / 2.0f);
            fogLayers[i].color = color;
        }

        camera.backgroundColor = fogColourGradient.Evaluate(-(float)player.position.y / (float)MAP_HEIGHT);
    }

    Color getColorVariant(int y)
    {
        float h, s, v;
        Color.RGBToHSV(groundColorGradient.Evaluate((float)y / (float)MAP_HEIGHT), out h, out s, out v);
        h += UnityEngine.Random.value / 10.0f;
        s += UnityEngine.Random.value / 10.0f;
        v += UnityEngine.Random.value / 10.0f;

        return Color.HSVToRGB(h, s, v);
    }

    public void GenerateMap()
    {
        mapContainer = new GameObject("MapContainer").transform;
        mainMap = new SpriteRenderer[MAP_WIDTH, MAP_HEIGHT];

        for (int y = 0; y < MAP_HEIGHT; y++)
        {
            for (int x = 0; x < MAP_WIDTH; x++)
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
        cloneMap = new SpriteRenderer[MAP_WIDTH, MAP_HEIGHT];

        for (int y = 0; y < MAP_HEIGHT; y++)
        {
            for (int x = 0; x < MAP_WIDTH; x++)
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

    private void OnDrawGizmos()
    {

        Gizmos.DrawWireCube(new Vector3(MAP_WIDTH / 2.0f, -MAP_HEIGHT / 2.0f) - new Vector3(0.5f, -0.5f), new Vector3(MAP_WIDTH, MAP_HEIGHT));
    }
}
