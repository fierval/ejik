using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed;
    public float lifetime;
    public float damage;

    public GameObject explosion;
    public GameObject emittedSound;
    EjikAcademy academy;
    bool isMLRun;

    // Start is called before the first frame update
    void Start()
    {
        academy = PlayerManager.Instance.academy.GetComponent<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;
        if (isMLRun)
        {
            damage *= -academy.resetParameters["playerDamageReward"];
        }

        Invoke("DestroyProjectile", lifetime);
        Instantiate(emittedSound, transform.position, transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    void DestroyProjectile()
    {
        Destroy(gameObject);
        Instantiate(explosion, transform.position, Quaternion.identity);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            collision.gameObject.GetComponent<Enemy>().TakeDamage(damage);
            DestroyProjectile();
        }
    }
}
