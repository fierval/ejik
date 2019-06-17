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
    float playerDeadReward;
    float enemyDeadReward;
    protected EjikAgent agent;
    protected EjikAcademy academy;
    protected bool isMLRun;

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;

        var academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;

        playerDeadReward = academy.resetParameters["playerDeadReward"];
        enemyDeadReward = academy.resetParameters["enemyDeadReward"];
        agent = isMLRun ? PlayerManager.Instance.player.GetComponent<EjikAgent>() : null;
    }

    public virtual void TakeDamage(int damageAmount, float damageCoeff = 1f)
    {
        bool isEnemy = gameObject.tag == "Enemy";
        
        health -= damageAmount * damageCoeff;
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
            else if(agent != null)
            {
                agent.AddReward(playerDeadReward);
                agent.Done();
            }

        }
        else
        {
            takeDamageSource.Play();
            // if this is a player - add negative reward
            if(!isEnemy && agent != null)
            {
                agent.AddReward(damageCoeff);
            }
        }
    }

}
