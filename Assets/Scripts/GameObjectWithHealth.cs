﻿using System;
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

    protected virtual void Start()
    {
        takeDamageSource = GetComponent<AudioSource>();
        takeDamageSource.clip = takeDamageSound;
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
            }

        }
        else
        {
            takeDamageSource.Play();
        }
    }

}
