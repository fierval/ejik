using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerSafeRadiusEditor : Editor
{
    public void OnSceneGUI()
    {
        Player t = (target as Player);
        Handles.color = Color.red;

        EditorGUI.BeginChangeCheck();
        float areaOfEffect = Handles.RadiusHandle(Quaternion.identity, t.transform.position, t.enemyRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed Area Of Effect");
            t.enemyRadius = areaOfEffect;
        }
    }

}
