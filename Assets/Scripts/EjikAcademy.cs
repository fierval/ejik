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
        try
        {
            ejik = PlayerManager.Instance.player.GetComponent<Player>();
        }
        catch
        {
            ejik = null;
        }
    }

    public override void AcademyReset()
    {
        base.AcademyReset();
        var managers = enemyManager.GetComponents<EnemyManager>();

        ejik.health = resetParameters["health"];
        ejik.initialHealth = ejik.health;
        ejik.playerDeadReward = resetParameters["playerDeadReward"];
        
        //mute all sounds if we are running the environment
        AudioListener.volume = 0;

        foreach (var manager in managers)
        {
            manager.resumePause.min = (int)resetParameters[$"min{manager.enemy.name}"];
            manager.resumePause.max = (int)resetParameters[$"max{manager.enemy.name}"];
        }

        Monitor.SetActive(true);
    }
}
