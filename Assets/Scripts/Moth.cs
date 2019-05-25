using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moth : Enemy
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        FacePlayer();
    }

    protected override void Update()
    {
        base.Update();
        FacePlayer();
    }
}
