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
    public GameObject UI_HitpointBossContainer;
    public GameObject UI_HitpointPrefab;
    public GameObject UI_HitpointBossPrefab;

    public List<GameObject> hitpointDisplays;
    public List<GameObject> hitpointBossDisplays;

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

    public AudioClip playerMoveSound;
    public AudioClip playerHurtSound;
    public AudioClip plantEatSound;
    public AudioClip bossActivateSound;
    public AudioClip bossMoveSound;

    public AudioSource audioSource;

    public GameObject tryAgainText;
    public GameObject youWinText;

    //public List<GameObject> Entities;

    private List<EnemyState> enemies;
    //public SpriteRenderer[,] wrapMap; // Map used when player is close to the edge, to fill in the gaps
    private List<Plant> plants;
    public Boss boss;

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

        hitpointBossDisplays = new List<GameObject>();
        for (int i = 0; i < boss.hitpoints; i++)
        {
            hitpointBossDisplays.Add(Instantiate(UI_HitpointBossPrefab, UI_HitpointBossContainer.transform));
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

        camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camera.transform.position.z);
    }

    IEnumerator TweenEntityPosition(GridEntity entity, Vector2Int direction)
    {
        var startPos = (Vector2)(entity.position - direction);
        var endPos = (Vector2)(entity.position);

        float startTime = Time.time;
        float duration = 0.25f;
        float endTime = startTime + duration;

        while(Time.time < endTime)
        {
            entity.transform.position = Vector3.Lerp(startPos, endPos, (Time.time - startTime) / duration);
            yield return new WaitForSecondsRealtime(0);
        }

        entity.moveTween = null;
    }

    void Tick()
    {
        foreach (var plant in plants)
        {
            if (plant.isExploded) continue;

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

            if (toRemove.Count != 0)
            {
                audioSource.PlayOneShot(plantEatSound);
            }

            if (boss != null && boss.isActive && plant.mature)
            {
                var toBoss = getWrappedDirectionTo(plant, boss);

                if (toBoss.sqrMagnitude < 1.5)
                {
                    plant.Explode();
                    boss.hitpoints--;
                    // Play explode sound.
                    audioSource.PlayOneShot(plantEatSound);

                    for (int i = 0; i < hitpointBossDisplays.Count; i++)
                    {
                        hitpointBossDisplays[i].SetActive(i < boss.hitpoints);
                    }

                    if (boss.hitpoints <= 0)
                    {
                        // Display win screen.
                        PlayWinSequence();

                        Destroy(boss.gameObject);
                        boss = null;
                    }
                }

            }

            plant.transform.position = (Vector2)plant.position; // Shouldn't need to do this but doing it anyway
        }

        foreach (var enemy in enemies)
        {
            var toPlayer = getWrappedDirectionTo(enemy, player);

            var moveDirection = Vector2Int.zero;

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

                    moveDirection.x = (int)xDir;

                    enemy.position.x += (int)xDir;
                    enemy.position.x = (MAP_WIDTH + enemy.position.x) % MAP_WIDTH;
                }

                if (!enemy.isLayerLocked)
                {
                    moveDirection.y = Mathf.Clamp(toPlayer.y, -1, 1);
                    enemy.position.y += moveDirection.y;
                }
            }

            if (enemy.moveTween != null)
            {
                StopCoroutine(enemy.moveTween);
                enemy.transform.position = (Vector2)enemy.position;
            }

            enemy.moveTween = StartCoroutine(TweenEntityPosition(enemy, moveDirection));

            //enemy.transform.position = (Vector2)enemy.position;
        }

        if (boss != null && boss.isActive)
        {
            var toPlayer = getWrappedDirectionTo(boss, player);

            var moveDirection = Vector2Int.zero;

            bool canHurt = toPlayer.sqrMagnitude < 1.5f;
            if (canHurt)
            {
                hurtPlayer();
            }

            if (Mathf.Abs(toPlayer.x) > 1.0)
            {
                float xDir = Mathf.Clamp(toPlayer.x, -1, 1);

                moveDirection.x = (int)xDir;

                boss.position.x += (int)xDir;
                boss.position.x = (MAP_WIDTH + boss.position.x) % MAP_WIDTH;
            }
            audioSource.PlayOneShot(bossMoveSound);

            if (boss.moveTween != null)
            {
                StopCoroutine(boss.moveTween);
                boss.transform.position = (Vector2)boss.position;
            }

            moveDirection.y = Mathf.Clamp(toPlayer.y, -1, 1);
            boss.position.y += moveDirection.y;

            boss.moveTween = StartCoroutine(TweenEntityPosition(boss, moveDirection));
        } else
        {
            if (player.position.y < -25)
            {
                boss.isActive = true;
                tickLength /= 2.0f;
                // Play boss actifation sound.
                audioSource.PlayOneShot(bossActivateSound);

                // Activate boss health display
                UI_HitpointBossContainer.SetActive(true);
            }
        }
    }

    void hurtPlayer()
    {
        hitPoints--;

        audioSource.PlayOneShot(playerHurtSound);

        for (int i = 0; i < hitpointDisplays.Count; i++)
        {
            hitpointDisplays[i].SetActive(i < hitPoints);
        }

        if (hitPoints == 0)
        {
            PlayDeathSequence();
        }
    }

    void PlayDeathSequence()
    {
        tryAgainText.SetActive(true);
        // Show Fail notification.
        StartCoroutine(DeferredLoadMenu(2));
    }

    IEnumerator DeferredLoadMenu(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        SceneManager.LoadScene("MenuScene");
    }

    void PlayWinSequence()
    {
        youWinText.SetActive(true);
        audioSource.PlayOneShot(bossActivateSound);
        // Show Success notification
        StartCoroutine(DeferredLoadMenu(2));
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
        audioSource.PlayOneShot(playerMoveSound);

        if (player.moveTween != null)
        {
            StopCoroutine(player.moveTween);
        }

        player.moveTween = StartCoroutine(TweenEntityPosition(player, player.lastMoveDirection));

        cloneMapContainer.position = new Vector2((player.position.x < MAP_WIDTH / 2.0f) ? -MAP_WIDTH : MAP_WIDTH, 0.0f);

        //for(int i = 0; i < 5; i++)
        //{
        //    var layerDistToPlayer = Mathf.Abs(i - 2);
        //    var color = fogColourGradient.Evaluate(-(float)player.position.y / (float)MAP_HEIGHT);
        //    color.a = ((float)layerDistToPlayer / 2.0f);
        //    fogLayers[i].color = color;
        //}

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
                renderer.color = getColorVariant(y);
                mainMap[x, y] = renderer;
            }
        }

        GenerateMapClone();
        //GenerateFogLayers();
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
