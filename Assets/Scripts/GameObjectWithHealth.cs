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
    public float enemyDeadReward;

    protected AudioSource takeDamageSource;

    [HideInInspector]
    public float playerDeadReward = 0f;

    protected EjikAgent agent;
    protected EjikAcademy academy;
    protected bool isMLRun;
    Player ejik;

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;
        ejik = PlayerManager.Instance.player.GetComponent<Player>();

        var academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;

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
                ejik.health += enemyDeadReward;

                if (agent != null)
                {
                    agent.AddReward(ejik.enemyDeadReward);
                }
            }
            else if (agent != null)
            {
                agent.AddReward(ejik.playerDeadReward);
                agent.Done();
            }

        }
        else
        {
            takeDamageSource.Play();
            // if this is a player - add negative reward
            if (!isEnemy && agent != null)
            {
                agent.AddReward(-damageAmount);
            }
        }
    }

}
