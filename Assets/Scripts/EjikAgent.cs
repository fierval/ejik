using System.Collections;
using System.Collections.Generic;
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
        ejik.transform.position = Vector3.zero;
        weapon = PlayerManager.Instance.weapon.GetComponent<Weapon>();
        RenderTexture();
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // don't take any action if textAction is set to something
        // this is done so we can accumulate visual observations
        // before performing any action
        if (!string.IsNullOrEmpty(textAction))
        {
            return;
        }

        // retrieve raw actions
        (float x, float y, float swingAction, float shootAction) =
            (vectorAction[0], vectorAction[1], vectorAction[2], vectorAction[3]);

        // prepare Ejik to move its rigid body
        // actions are normalized -1 to 1, need to be -180 to 180 degrees
        ejik.SetMoveAmount(new Vector2(x, y));

        var directionSwing = swingAction * 180;
        weapon.transform.rotation = Quaternion.AngleAxis(swingAction, Vector3.forward);

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

    /// <summary>
    /// Since we are rendering to texture we need to do it ourselves
    /// </summary>
    public void FixedUpdate()
    {
        RenderTexture();
    }

    void RenderTexture()
    {
        if (renderCamera != null)
        {
            renderCamera.Render();
        }

    }
}
