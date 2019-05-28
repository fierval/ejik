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
    AudioSource takeDamageSource;

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;
    }

    public virtual void TakeDamage(int damageAmount)
    {
        bool deathEffect = gameObject.tag == "Enemy";
        
        health -= damageAmount;
        if (health <= 0)
        {
            Destroy(gameObject);
            if (deathEffect)
            {
                Instantiate(PlayerManager.Instance.enemyDeathEffect, transform.position, transform.rotation);
            }

        }
        else
        {
            takeDamageSource.Play();
        }
    }

}
