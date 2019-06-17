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
    EjikAgent agent;
    EjikAcademy academy;

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;

        var academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        bool isMLRun = academy != null && academy.isActiveAndEnabled;

        playerDeadReward = academy.resetParameters["playerDeadReward"];
        enemyDeadReward = academy.resetParameters["enemyDeadReward"];
        agent = isMLRun ? PlayerManager.Instance.player.GetComponent<EjikAgent>() : null;
    }

    public virtual void TakeDamage(int damageAmount, float damageCoeff = 1f)
    {
        bool deathEffect = gameObject.tag == "Enemy";
        
        health -= damageAmount * damageCoeff;
        if (health <= 0)
        {
            if (deathEffect)
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
        }
    }

}
