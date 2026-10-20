using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float backstepSpeed = 7.5f;
    public float backstepDuration = 0.18f;
    public Transform opponent;
    public FighterCombat combat;
    public SpriteFrameAnimator animator;
    public Health health;

    private Rigidbody2D body;
    private bool grounded;
    private int facingDirection = -1;
    private float backstepUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (combat == null) combat = GetComponent<FighterCombat>();
        if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
        if (health == null) health = GetComponent<Health>();
    }

    private void Start()
    {
        if (opponent == null)
        {
            GameObject ghost = GameObject.Find("Ghost");
            if (ghost != null) opponent = ghost.transform;
        }

        if (opponent != null)
            facingDirection = opponent.position.x >= transform.position.x ? 1 : -1;
        if (combat != null)
            combat.SetFacingDirection(facingDirection);
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        float horizontal = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;

        if (horizontal != 0f)
        {
            facingDirection = horizontal > 0f ? 1 : -1;
            combat.SetFacingDirection(facingDirection);
        }

        bool backstepPressed = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
        if (backstepPressed)
            backstepUntil = Time.time + backstepDuration;

        bool backstepping = Time.time < backstepUntil;
        float movement = backstepping ? -facingDirection * backstepSpeed : horizontal * moveSpeed;
        body.linearVelocity = new Vector2(movement, body.linearVelocity.y);

        if (animator != null)
            animator.SetMoving(Mathf.Abs(movement) > 0.01f);

        bool jumpPressed = keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        if (jumpPressed && grounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            grounded = false;
        }

        Mouse mouse = Mouse.current;
        bool attackPressed = keyboard.jKey.wasPressedThisFrame ||
                             (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (attackPressed)
            combat.TryAttack();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        for (int index = 0; index < collision.contactCount; index++)
        {
            if (collision.GetContact(index).normal.y > 0.5f)
            {
                grounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        grounded = false;
    }
}