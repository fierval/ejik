using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectile;
    public Transform shotPoint;
    public float timeBetweenShots;

    float shotTime;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        if (Input.GetMouseButton(0) && Time.time >= shotTime)
        {
            Instantiate(projectile, shotPoint.position, transform.rotation);
            shotTime = Time.time + timeBetweenShots;
        }
    }
}
