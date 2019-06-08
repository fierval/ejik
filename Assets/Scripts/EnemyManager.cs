using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemy;
    // we'll keep track of the enemies
    List<GameObject> enemies;

    public float spawnTime = 5f;

    [Tooltip("At which point to pause spawning.")]
    [Range(0, 20)]
    public int pause;

    [Tooltip("Resume spawning when we reach this value")]
    [Range(0, 20)]
    public int resume;

    [Tooltip("Are we allowed to re-spawn again")]
    public bool respawn = true;
    bool hasSpawned;

    Player player;
    private bool spawningPaused;

    // Start is called before the first frame update
    void Start()
    {
        player = PlayerManager.Instance.player.GetComponent<Player>();
        enemies = new List<GameObject>();
        hasSpawned = false;

        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        InvokeRepeating("Spawn", spawnTime, spawnTime);
    }

    private Vector2 GenerateRandomSpawnPoint()
    {
        // different ranges
        var minLeftX = player.transform.position.x - 3 * player.enemyRadius;
        var maxLeftX = player.transform.position.x - player.enemyRadius;

        var minRightX = player.transform.position.x + player.enemyRadius;
        var maxRightX = player.transform.position.x + 3 * player.enemyRadius;

        var minBottomY = player.transform.position.y - 1.5f * player.enemyRadius;
        var maxBottomY = player.transform.position.y - player.enemyRadius;

        var minTopY = player.transform.position.y + player.enemyRadius;
        var maxTopY = player.transform.position.y + 1.5f * player.enemyRadius;

        // store x and y ranges in their lists so we can easily pick random ranges
        //... at random!
        var choicesX = new List<float>() { minLeftX, maxLeftX, minRightX, maxRightX };
        var choicesY = new List<float>() { minBottomY, maxBottomY, minTopY, maxTopY };

        var center = new Vector2(player.transform.position.x, player.transform.position.y);

        var squareRadius = player.enemyRadius * player.enemyRadius;

        var xDirection = UnityEngine.Random.Range(0, 2) * 2;
        var yDirection = UnityEngine.Random.Range(0, 2) * 2;

        Vector2 v = new Vector2(UnityEngine.Random.Range(choicesX[xDirection], choicesX[xDirection + 1]),
            UnityEngine.Random.Range(choicesY[yDirection], choicesY[yDirection + 1]));

        return v;
    }

    void Spawn()
    {
        enemies = enemies.Where(e => e != null).ToList();

        // don't spawn if the player is dead or we have reached our limit
        if (player.health <= 0f) { return; }

        if (!respawn && hasSpawned) { return; }

        if (enemies.Count >= pause)
        {
            // this will ensure we'll never re-spawn again
            // if this is a non-respawnable object
            hasSpawned = true;

            spawningPaused = true;
            return;
        }

        // when we recover after having saturated the scene with enemies
        // wait until their number drops down to "resume" value
        // set resume >= pause to never re-spawn again
        if(spawningPaused && enemies.Count >= resume)
        {
            spawningPaused = false;
            return;
        }

        var spawnPoint = GenerateRandomSpawnPoint();

        var enemyPos = enemy.GetComponent<Transform>();

        // start with the enemy rotation
        enemies.Add(Instantiate(enemy, spawnPoint, enemyPos.rotation));
    }
}
