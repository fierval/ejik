using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horse : Enemy
{
    public float minX, maxX, minY, maxY;
    public float speed;

    Vector2 targetPosition;
    Vector2 curPosition;
    Animator anim;

    public float timeBetweenSummons;
    float summonTime = 0;

    public Enemy enemyToSummon;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        targetPosition = new Vector2(randomX, randomY);

        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (player == null) { return; }
        
        if(Vector2.Distance(transform.position, targetPosition) > .5f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
        else if(Time.time >= summonTime)
        {
            summonTime = Time.time + timeBetweenSummons;
            anim.SetTrigger("summon");
        }
    }

    public void Summon()
    {
        if (player == null) { return; }
        Instantiate(enemyToSummon, transform.position, transform.rotation);   
    }
}
