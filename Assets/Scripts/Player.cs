using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : GameObjectWithHealth
{
    public float speed = 5;
    Rigidbody2D rb;
    Vector2 moveAmount;
    Animator anim;

    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite blackHeart;

    Slider healthSlider;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        healthSlider = GameObject.FindGameObjectWithTag("Player Health Slider").GetComponent<Slider>();
        healthSlider.maxValue = health;
        healthSlider.value = health;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveAmount = moveInput.normalized * speed;

        anim.SetBool("isRunning", moveInput != Vector2.zero);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveAmount * Time.fixedDeltaTime);
    }

    public override void TakeDamage(int damageAmount)
    {
        base.TakeDamage(damageAmount);
        UpdateHealthUI();
        if (health <= 0)
        {
            Destroy(healthSlider.gameObject, 1f);
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider == null) { return; }
        healthSlider.value = health;
    }
}
