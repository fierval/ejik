using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemy;
    public float spawnTime = 5f;
    public Transform[] spawnPoints;
    [Tooltip("Negative value means infinite instantiations")]
    public int maxInstantiations = -1;
    int curInstance;

    Player player;

    // Start is called before the first frame update
    void Start()
    {
        player = PlayerManager.Instance.player.GetComponent<Player>();
        curInstance = 0;
        InvokeRepeating("Spawn", spawnTime, spawnTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Spawn()
    {

        // don't spawn if the player is dead or we have reached our limit
        if(player.health <= 0f 
            || (maxInstantiations > 0 && curInstance >= maxInstantiations)) { return; }

        curInstance++;
        int spawnPointIdx = Random.Range(0, spawnPoints.Length);
        var spawnPoint = spawnPoints[spawnPointIdx];
        var enemyPos = enemy.GetComponent<Transform>();

        // start with the enemy rotation
        Instantiate(enemy, spawnPoint.position, enemyPos.rotation);
    }
}
