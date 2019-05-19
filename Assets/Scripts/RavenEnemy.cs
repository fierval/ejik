﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RavenEnemy : Enemy
{
    public float stopDistance;

    float attackTime = 0;
    public float attackSpeed;

    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if(player == null) { return; }

        if(Vector2.Distance(transform.position, player.position) > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }
        else if (Time.time >= attackTime)
        {
            StartCoroutine(Attack());
            attackTime = Time.time + timeBetweenAttacks;
        }
    }

    IEnumerator Attack ()
    {
        player.GetComponent<Player>().TakeDamage(damage);

        Vector2 originalPosition = transform.position;
        Vector2 targetPosition = player.position;

        float fraction = 0;
        while(fraction < 1)
        {
            fraction += Mathf.Clamp(Time.deltaTime * attackSpeed, 0.0001f, 1f);
            float amount = fraction * (1 - fraction) * 4;

            transform.position = Vector2.Lerp(originalPosition, targetPosition, amount);
            yield return null;
        }
    }
}
