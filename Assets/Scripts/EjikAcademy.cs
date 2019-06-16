using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class EjikAcademy : Academy
{
    GameObject enemyManager;

    public override void InitializeAcademy()
    {
        enemyManager = GameObject.Find("EnemyManager");
    }

    public override void AcademyReset()
    {
        base.AcademyReset();
        var managers = enemyManager.GetComponents<EnemyManager>();
        foreach (var manager in managers)
        {
            manager.resumePause.min = (int)resetParameters[$"min{manager.enemy.name}"];
            manager.resumePause.max = (int)resetParameters[$"max{manager.enemy.name}"];
        }
    }
}
