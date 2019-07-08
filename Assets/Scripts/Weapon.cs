using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    public float timeBetweenShots;

    public float swingDamage;
    float shotTime = 0;
    public GameObject explosion;

    EjikAcademy academy;
    bool isMLRun = false;

    private void Start()
    {
        academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;
        if(isMLRun)
        {
            swingDamage *= -academy.resetParameters["playerDamageReward"];
        }
    }

    // Update is called once per frame
    void Update()
    {
        // shooting and swinging is handled by the Agent in case of ML
        if(isMLRun) { return; }
        
        SetShotDirection(GetShotDirection());

        if ((Input.GetMouseButton(0) || Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space)) && Time.time >= shotTime)
        {
            Fire();
            shotTime = Time.time + timeBetweenShots;
        }
    }

    public void Fire()
    {
        Instantiate(projectile, shotPoint.position, transform.rotation);
    }

    Vector3 GetShotDirection()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
    }

    public void SetShotDirection(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            collision.gameObject.GetComponent<Enemy>().TakeDamage(swingDamage);
            Instantiate(explosion, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
        }
    }

}
