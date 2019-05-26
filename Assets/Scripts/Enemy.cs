using System;
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
    bool facedPlayer = false;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        player = PlayerManager.Instance.player.transform;
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
        if (player == null)
        {
            yield break;
        }

        Vector2 originalPosition = transform.position;
        Vector2 targetPosition = player.position;


        for (float fraction = 0; fraction < 1; fraction += Time.deltaTime * attackSpeed)
        {
            float amount = fraction * (1 - fraction) * 4;

            transform.position = Vector2.Lerp(originalPosition, targetPosition, amount);
            yield return null;
        }

        // avoid taking damage for the dead player
        try
        {
            player.GetComponent<Player>().TakeDamage(damage);
        }
        catch (Exception)
        {

        }


    }

    protected void FacePlayer()
    {
        if(player == null) { return; }

        var direction = transform.position - player.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // if this is the first time we are doing this
        // add to the current rotation in the direction of the player
        if (!facedPlayer)
        {
            transform.Rotate(new Vector3(0f, 0f, 180 + angle));
        }
        // we need to adjust our angle, not do a full rotation
        else
        {
            transform.rotation = Quaternion.AngleAxis(180 + angle, Vector3.forward);
        }

        facedPlayer = true;
    }
}
