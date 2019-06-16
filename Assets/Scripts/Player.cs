using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAgents;

public class Player : GameObjectWithHealth
{
    public float speed = 5;

    [Tooltip("Radius within which no enemy initially appears")]
    public float enemyRadius = 1;

    Rigidbody2D rb;

    Vector2 moveAmount;
    Animator anim;

    public AudioClip deathSound;

    Slider healthSlider;

    EjikAcademy academy;
    [HideInInspector]
    public bool isMLRun;
    [HideInInspector]
    public float damageCoeff = 1f;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        academy = FindObjectOfType<EjikAcademy>();
        isMLRun = academy != null && academy.isActiveAndEnabled;

        healthSlider = GameObject.FindGameObjectWithTag("Player Health Slider").GetComponent<Slider>();
        healthSlider.maxValue = health;
        healthSlider.value = health;

    }

    // Update is called once per frame
    void Update()
    {
        if (!isMLRun)
        {
            SetMoveAmount(CaptureInput());
        }
        anim.SetBool("isRunning", moveAmount != Vector2.zero);
    }

    Vector2 CaptureInput()
    {
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return moveInput;
    }


    public void SetMoveAmount(Vector2 moveInput)
    {
        moveAmount = moveInput.normalized * speed;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveAmount * Time.fixedDeltaTime);
    }

    public override void TakeDamage(int damageAmount, float damageMult = 1f)
    {
        base.TakeDamage(damageAmount, damageCoeff);
        UpdateHealthUI();

        // no theatrics if this is not an ML run
        if (health <= 0 && !isMLRun)
        {
            takeDamageSource.clip = deathSound;
            anim.SetTrigger("isDying");
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider == null) { return; }
        healthSlider.value = health;
    }

    public void OnDeath()
    {
        // we don't just die if this is an ml run
        if (isMLRun) { return; }

        gameObject.GetComponentInChildren<Weapon>().gameObject.SetActive(false);
        takeDamageSource.Play();
        Destroy(gameObject, 3f);
        Destroy(healthSlider.gameObject, 1f);
    }
}
