using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.CommunicatorObjects;

public class EjikAgent : Agent
{
    Player ejik;
    Weapon weapon;

    public override void InitializeAgent()
    {
        ejik = GetComponent<Player>();
        weapon = PlayerManager.Instance.weapon.GetComponent<Weapon>();
    }

    public override void AgentReset()
    {
        ejik.transform.position = Vector3.zero;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // retrieve raw actions
        (float moveAction, float swingAction, float shootAction) =
            (vectorAction[0], vectorAction[1], vectorAction[2]);

        // prepare Ejik to move its rigid body
        // actions are normalized -1 to 1, need to be -180 to 180 degrees
        ejik.SetMoveAmount(PositionFromAngle(moveAction * 180));

        var directionSwing = PositionFromAngle(swingAction * 180) - weapon.transform.position;
        weapon.SetShotDirection(directionSwing);

        var isShooting = MapDiscreteRange(shootAction, 0, 2) > 0;
        if(isShooting)
        {
            weapon.Fire();
        }
    }

    Vector3 PositionFromAngle(float angle)
    {
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    int MapDiscreteRange(float val, float mapMin, float mapMax, float origMin = -1, float origMax = 1)
    {
        if (val == origMax)
        {
            val -= (origMax - origMin) / Mathf.Pow((mapMax - mapMin), 2f);
        }

        return  (int)(mapMin + (val - origMin) * (mapMax - mapMin) / (origMax - origMin));
    }
}
