using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RavenEnemy : Enemy
{
    bool facingRight = true;

    protected override void Start()
    {
        base.Start();
        FaceTowardsPlayer();
    }
    // Start is called before the first frame update
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        FaceTowardsPlayer();
    }

    void FaceTowardsPlayer()
    {
        if (player == null) { return; }
        var xDir = player.position.x - transform.position.x;

        if (xDir > 0 && !facingRight || xDir < 0 && facingRight)
        {
            transform.eulerAngles = 
                new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + 180f, transform.eulerAngles.z);
            facingRight = !facingRight;
        }
    }

}
