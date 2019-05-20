using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : GameObjectWithHealth
{
    [HideInInspector]
    public Transform player;

    public float timeBetweenAttacks;

    public float speed;

    public int damage;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

}
