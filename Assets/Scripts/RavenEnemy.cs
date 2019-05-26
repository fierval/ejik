﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RavenEnemy : Enemy
{
    bool facingRight = true;

    protected override void Start()
    {
        base.Start();
        FlipTowardsPlayer();
    }
    // Start is called before the first frame update
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        FlipTowardsPlayer();
    }

    void FlipTowardsPlayer()
    {
        if (player == null) { return; }
        var xDir = player.position.x - transform.position.x;

        if (xDir > 0 && !facingRight || xDir < 0 && facingRight)
        {
            transform.Rotate(Vector3.up, 180);
                
            facingRight = !facingRight;
        }
    }

}
