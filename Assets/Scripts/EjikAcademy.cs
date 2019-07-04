using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class EjikAcademy : Academy
{
    GameObject enemyManager;
    Player ejik;

    public override void InitializeAcademy()
    {
        enemyManager = GameObject.Find("EnemyManager");
        ejik = PlayerManager.Instance.player.GetComponent<Player>();
        ejik.health = resetParameters["health"];
        ejik.damageCoeff = resetParameters["damageCoeff"];
    }

    public override void AcademyReset()
    {
        base.AcademyReset();
        var managers = enemyManager.GetComponents<EnemyManager>();

        ejik.health = resetParameters["health"];
        ejik.damageCoeff = resetParameters["damageCoeff"];

        foreach (var manager in managers)
        {
            manager.resumePause.min = (int)resetParameters[$"min{manager.enemy.name}"];
            manager.resumePause.max = (int)resetParameters[$"max{manager.enemy.name}"];
        }
    }
}
