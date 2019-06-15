using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.CommunicatorObjects;

public class EjikAgent : Agent
{
    Player ejik;

    // Start is called before the first frame update
    void Start()
    {
        ejik = GetComponent<Player>();      
    }

    public override void AgentReset()
    {
        if (ejik.health <= 0)
        {
            ejik.gameObject.transform.position = Vector3.zero;
            ejik.health = 1f;
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction, CustomAction customAction)
    {
        base.AgentAction(vectorAction, textAction, customAction);
    }
}
