using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class EjikAcademy : Academy
{
    GameObject enemyManager;
    GameObject player;

    public override void InitializeAcademy()
    {
        enemyManager = GameObject.Find("EnemyManager");
        player = GameObject.Find("Ejik");

        var managers = enemyManager.GetComponents<EnemyManager>();
        foreach (var manager in managers)
        {
            manager.resumePause.min = (int)resetParameters[$"min{manager.enemy.name}"];
            manager.resumePause.max = (int)resetParameters[$"max{manager.enemy.name}"];
        }

        var ejik = player.GetComponent<Player>();
        ejik.damageCoeff = resetParameters["damageCoeff"];
        ejik.health = resetParameters["health"];
    }

}
