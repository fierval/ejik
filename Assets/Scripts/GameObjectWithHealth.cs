using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GameObjectWithHealth : MonoBehaviour
{
    public float health;
    public AudioClip takeDamageSound;
    protected AudioSource takeDamageSource;
    protected float playerDeadReward = 0f;
    protected float enemyDeadReward = 0f;
    protected float playerDamageReward = 0f;

    protected EjikAgent agent;
    protected EjikAcademy academy;
    protected bool isMLRun;

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;

        var academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;
        if (academy != null)
        {
            playerDeadReward = academy.resetParameters["playerDeadReward"];
            enemyDeadReward = academy.resetParameters["enemyDeadReward"];
            playerDamageReward = academy.resetParameters["playerDamageReward"];

            // everything gets scaled by this reward for all actors
            health *= -playerDamageReward;
        }
        agent = isMLRun ? PlayerManager.Instance.player.GetComponent<EjikAgent>() : null;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        bool isEnemy = gameObject.tag == "Enemy";

        health -= damageAmount;
        if (health <= 0)
        {
            if (isEnemy)
            {
                Destroy(gameObject);
                Instantiate(PlayerManager.Instance.enemyDeathEffect, transform.position, transform.rotation);

                if (agent != null)
                {
                    agent.AddReward(enemyDeadReward);
                }
            }
            else if (agent != null)
            {
                agent.AddReward(playerDeadReward);
                agent.Done();
            }

        }
        else
        {
            takeDamageSource.Play();
            // if this is a player - add negative reward
            if (!isEnemy && agent != null)
            {
                agent.AddReward(playerDamageReward);
            }
        }
    }

}
