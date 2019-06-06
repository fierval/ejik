using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class RavenEnemy : Enemy
{
    protected override void Start()
    {
        base.Start();
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

        if(aiPath.desiredVelocity.x >= 0.01f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if(aiPath.desiredVelocity.x <= - 0.01f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

}
