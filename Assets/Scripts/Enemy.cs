using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Enemy : GameObjectWithHealth
{
    [HideInInspector]
    public Transform player;

    float attackTime = 0;
    public float attackSpeed;

    float stopDistance;

    public float timeBetweenAttacks;

    public int damage;
    bool facedPlayer = false;
    protected AIPath aiPath;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        player = PlayerManager.Instance.player.transform;

        try
        {
            GetComponent<AIDestinationSetter>().target = player.transform;
            aiPath = GetComponent<AIPath>();
            stopDistance = aiPath.endReachedDistance;
        }
        catch (Exception)
        {
            aiPath = null;
        }
    }

    protected virtual void Update()
    {
        if (player == null || aiPath == null) { return; }

        if (Vector2.Distance(transform.position, player.position) <= stopDistance)
        {
            aiPath.enabled = false;
            if (Time.time >= attackTime)
            {
                StartCoroutine(Attack());
                attackTime = Time.time + timeBetweenAttacks;
            }

        }
        else
        {
            aiPath.enabled = true;
        }
    }

    IEnumerator Attack()
    {
        if (player == null)
        {
            yield break;
        }
        else
        {
            player.GetComponent<Player>().TakeDamage(damage);
        }

        Vector2 originalPosition = transform.position;
        Vector2 targetPosition = player.position;


        for (float fraction = 0; fraction < 1; fraction += Time.deltaTime * attackSpeed)
        {
            float amount = fraction * (1 - fraction) * 4;

            transform.position = Vector2.Lerp(originalPosition, targetPosition, amount);
            yield return null;
        }
    }

    protected void FacePlayer()
    {
        if (player == null) { return; }

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
