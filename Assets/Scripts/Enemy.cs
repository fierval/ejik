using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : GameObjectWithHealth
{
    [HideInInspector]
    public Transform player;

    float attackTime = 0;
    public float attackSpeed;

    public float stopDistance;

    public float timeBetweenAttacks;

    public float speed;

    public int damage;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    protected virtual void Update()
    {
        if (player == null) { return; }

        if (Vector2.Distance(transform.position, player.position) > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }
        else if (Time.time >= attackTime)
        {
            StartCoroutine(Attack());
            attackTime = Time.time + timeBetweenAttacks;
        }
    }

    IEnumerator Attack()
    {

        Vector2 originalPosition = transform.position;
        Vector2 targetPosition = player.position;


        for (float fraction = 0; fraction < 1; fraction += Time.deltaTime * attackSpeed)
        {
            float amount = fraction * (1 - fraction) * 4;

            transform.position = Vector2.Lerp(originalPosition, targetPosition, amount);
            yield return null;
        }

        player.GetComponent<Player>().TakeDamage(damage);
    }


}
