using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
using MLAgents.CommunicatorObjects;

public class EjikAgent : Agent
{
    Player ejik;
    Weapon weapon;
    Camera renderCamera;

    public override void InitializeAgent()
    {
        ejik = GetComponent<Player>();
        renderCamera = gameObject.GetComponentInChildren<Camera>();
    }

    public override void AgentReset()
    {
        weapon = PlayerManager.Instance.weapon.GetComponent<Weapon>();

        // restore health
        ejik.health = ejik.initialHealth;
        ejik.transform.position = new Vector3(0f, 0f, 0f);
        RenderTexture();
        Display();
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // if a default action is coming in we should not listen
        if (string.IsNullOrEmpty(textAction))
        {
            return;
        }

        // retrieve raw actions
        (float x, float y, float swingAction, float shootAction) =
            (vectorAction[0], vectorAction[1], vectorAction[2], vectorAction[3]);

        // prepare Ejik to move its rigid body
        // actions are normalized -1 to 1, need to be -180 to 180 degrees
        ejik.SetMoveAmount(new Vector3(x, y));

        var directionSwing = -swingAction * 180;
        weapon.transform.rotation = Quaternion.AngleAxis(directionSwing, Vector3.forward);

        // shootAction is [-1, 1]
        // simply cut it down the middle to decide to shoot
        if(shootAction > 0)
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

        return  Mathf.RoundToInt(mapMin + (val - origMin) * (mapMax - mapMin) / (origMax - origMin));
    }

    /// <summary>
    /// Since we are rendering to texture we need to do it ourselves
    /// </summary>
    public void FixedUpdate()
    {
        RenderTexture();
        if(IsDone() && !ejik.IsDead())
        {
            ejik.OnDone();
        }
    }

    void RenderTexture()
    {
        if (renderCamera != null)
        {
            renderCamera.Render();
        }

    }

    public void Display()
    {
        Monitor.Log("Reward", GetCumulativeReward().ToString("F3"), transform);
    }
}
