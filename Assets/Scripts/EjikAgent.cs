using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using MLAgents.CommunicatorObjects;

public class EjikAgent : Agent
{
    Player ejik;
    EjikAcademy academy;
    // Start is called before the first frame update
    void Start()
    {
        academy = FindObjectOfType<EjikAcademy>();
        ejik = GetComponent<Player>();      
    }

    public override void AgentReset()
    {
        ejik.gameObject.transform.position = Vector3.zero;
        academy.AcademyReset();
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        (float moveAction, float swingAction, float shootAction) =
            (vectorAction[0], vectorAction[1], vectorAction[2]);

        ejik.SetMoveAmount(new Vector2(Mathf.Cos(moveAction), Mathf.Sin(moveAction)));

        var swingDegree = swingAction * 180;
        var isShooting = MapDiscreteRange(shootAction, 0, 2);
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
