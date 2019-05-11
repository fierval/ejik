using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform ejikTransform;
    public float speed = 0.1f;

    public float minX, minY, maxX, maxY;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = ejikTransform.position;    
    }

    // Update is called once per frame
    void Update()
    {
        if (ejikTransform != null)
        {
            var floatX = Mathf.Clamp(ejikTransform.position.x, minX, maxX);
            var floatY = Mathf.Clamp(ejikTransform.position.y, minY, maxY);
            var clampedPos = new Vector2(floatX, floatY);
            transform.position = Vector2.Lerp(transform.position, clampedPos, speed);
        }
    }
}
