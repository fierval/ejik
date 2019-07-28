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

        academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;

        agent = isMLRun ? PlayerManager.Instance.player.GetComponent<EjikAgent>() : null;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        bool isEnemy = IsEnemy();
        float reward = isEnemy? 0 : -damageAmount;

        health -= damageAmount;
        if (IsDead())
        {
            if (isEnemy)
            {
                Destroy(gameObject);
                Instantiate(PlayerManager.Instance.enemyDeathEffect, transform.position, transform.rotation);

                ejik.health += enemyDeadReward;
                reward = enemyDeadReward;
            }
            else
            {
                reward = playerDeadReward;
            }
        }
        else
        {
            takeDamageSource.Play();
        }

        if(agent != null)
        {
            agent.AddReward(reward);
            if(!isEnemy && IsDead())
            {
                agent.Done();
            }
            agent.Display();
        }
    }

    public bool IsDead()
    {
        return health < 0;
    }

    protected bool IsEnemy()
    {
        return gameObject.tag == "Enemy";
    }

}
