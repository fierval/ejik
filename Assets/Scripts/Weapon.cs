using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    public float timeBetweenShots;

    public int swingDamage;
    float shotTime = 0;
    public GameObject explosion;

    // Update is called once per frame
    void Update()
    {
        var direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        if ((Input.GetMouseButton(0) || Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space)) && Time.time >= shotTime)
        {
            Instantiate(projectile, shotPoint.position, transform.rotation);
            shotTime = Time.time + timeBetweenShots;
        }
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
