﻿using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    public float maxJumpHeight = 4f;
    public float minJumpHeight = 1f;
    public float timeToJumpApex = .4f;
    private float accelerationTimeAirborne = .2f;
    private float accelerationTimeGrounded = .1f;
    private float moveSpeed = 6f;
    private bool facingRight = true;
    private float moveInput;

    private bool isSwimming = false;

    private string downButton = "down";

    public bool canDoubleJump;
    private bool isDoubleJumping = false;

    private bool isSuperJumping = false;


    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector3 velocity;
    private float velocityXSmoothing;
    public LayerMask groundLayer;

    private Controller2D controller;

    private Vector2 directionalInput;

    // ***** particles when land in water *****
    public GameObject splash;

    private int wallDirX;
    // ***** KEY COMBO *****
    private KeyCode[] sequence = new KeyCode[]{
    KeyCode.DownArrow,
    KeyCode.UpArrow,
    KeyCode.Space};
    private int sequenceIndex;
    // ***** Super Jump *****
    private float timeLeft = 1.5f;
    private void HandleSuperJump()
    {
        if (sequenceIndex != 0 && timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            // Debug.Log(timeLeft);
        }
        if (isSuperJumping)
        {
            maxJumpVelocity /= 6;
            isSuperJumping = false;
        }
        if (Input.GetKeyDown(sequence[sequenceIndex]))
        {
            if (++sequenceIndex == sequence.Length && timeLeft > 0)
            {
                sequenceIndex = 0;
                timeLeft = 1.5f;
                isSuperJumping = true;
                maxJumpVelocity *= 6;
            }
        }
        else if (Input.anyKeyDown || timeLeft <= 0)
        {
            sequenceIndex = 0;
            timeLeft = 1.5f;
        }
    }

    Animator anim;

    SpriteRenderer m_SpriteRenderer;

    TrailRenderer tr;

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.layer == 4)
        {
            isSwimming = true;
            gravity /= 10;
            moveSpeed /= 2;
            maxJumpVelocity /= 2;
            velocity.y /= 3;
            // m_SpriteRenderer.color = new Color (143F/255,114F/255,209F/255,1);
            Instantiate(splash, transform.position, Quaternion.identity);
        }

    }
    void OnCollisionExit2D(Collision2D coll)
    {
        if (coll.gameObject.layer == 4)
        {
            isSwimming = false;
            gravity *= 10;
            moveSpeed *= 2;
            maxJumpVelocity *= 2;
            Instantiate(splash, transform.position, Quaternion.identity);
        }

    }



    private void Start()
    {
        controller = GetComponent<Controller2D>();
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        anim = GetComponent<Animator>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        tr = GetComponent<TrailRenderer>();
        tr.sortingLayerName = "Character";
    }

    private void Update()
    {
        CalculateVelocity();

        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0f;
        }



        moveInput = Input.GetAxis("Horizontal");

        HandleFlipSprite();
        HandleAnimationStates();
        HandleSuperJump();
    }


    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        if (Input.GetKey(downButton))
        {
            return;
        }
        if (controller.collisions.below)
        {
            velocity.y = maxJumpVelocity;
            isDoubleJumping = false;
        }
        if (canDoubleJump && !controller.collisions.below && !isDoubleJumping)
        {
            velocity.y = maxJumpVelocity;
            isDoubleJumping = true;
        }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    void HandleAnimationStates()
    {
        if (!controller.collisions.below)
        {
            if (velocity.y > 0 && !isDoubleJumping)
            {
                anim.SetInteger("State", 3);

            }
            if (velocity.y > 0 && isDoubleJumping)
            {
                anim.SetInteger("State", 4);
            }
            if (velocity.y <= 0 && !IsGrounded())
            {
                anim.SetInteger("State", 1);
                this.tr.enabled = false;
            }
        }
        if (controller.collisions.below)
        {
            if (moveInput != 0)
            {
                anim.SetInteger("State", 2);
            }
            if (moveInput == 0 && !Input.GetKey(downButton))
            {
                anim.SetInteger("State", 0);
            }

        }
        if (Input.GetKeyDown(downButton))
        {
            if (IsGrounded())
            {
                anim.SetInteger("State", 5);
                moveSpeed *= 0;
            }
        }
        else if (Input.GetKeyUp(downButton))
        {
            anim.SetInteger("State", 6);
            moveSpeed = isSwimming ? 3f : 6f;
        }
        if (isSuperJumping)
        {
            this.tr.enabled = true;
        }

    }
    void FlipSprite()
    {
        facingRight = !facingRight;
        Vector3 Scaler = transform.localScale;
        Scaler.x *= -1;
        transform.localScale = Scaler;
    }
    void HandleFlipSprite()
    {
        if (facingRight == false && moveInput > 0)
        {
            FlipSprite();
        }
        else if (facingRight == true && moveInput < 0)
        {
            FlipSprite();
        }
    }

    bool IsGrounded()
    {
        Vector2 position = transform.position;
        Vector2 direction = Vector2.down;
        float distance = 0.5f;

        RaycastHit2D hit = Physics2D.Raycast(position, direction, distance, groundLayer);
        if (hit.collider != null)
        {
            return true;
        }

        return false;
    }

    private void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        velocity.y += gravity * Time.deltaTime;

    }
}
