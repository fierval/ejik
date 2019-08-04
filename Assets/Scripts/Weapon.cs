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
        try
        {
            academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
            isMLRun = academy != null && academy.isActiveAndEnabled;
        }
        catch
        {
            isMLRun = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // shooting and swinging is handled by the Agent in case of ML
        if(isMLRun) { return; }
        
        SetShotDirection(GetShotDirection());

        if (Input.GetMouseButton(0) || Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    public void Fire()
    {
        if(!CanShoot()) { return; }

        Instantiate(projectile, shotPoint.position, transform.rotation);
        shotTime = Time.time + timeBetweenShots;
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

    public bool CanShoot()
    {
        return Time.time >= shotTime;
    }

}
