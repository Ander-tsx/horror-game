using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3.25f;
    public float attackDistance = 2f;
    public float jumpForce = 8.5f;
    public FighterCombat combat;
    public SpriteFrameAnimator animator;
    public Health health;

    private Rigidbody2D body;
    private bool grounded;
    private float nextAttackTime;
    private float nextDecisionTime;
    private float retreatUntil;
    private int retreatDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (combat == null) combat = GetComponent<FighterCombat>();
        if (animator == null) animator = GetComponent<SpriteFrameAnimator>();
        if (health == null) health = GetComponent<Health>();
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void Update()
    {
        if (target == null)
            ResolveTarget();
        if (target == null || combat == null || (health != null && health.IsDead))
            return;

        float deltaX = target.position.x - transform.position.x;
        int towardTarget = deltaX >= 0f ? 1 : -1;
        combat.SetFacingDirection(towardTarget);

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + Random.Range(0.55f, 0.95f);
            // Una retirada o salto ocasional hace que el CPU sea menos predecible.
            if (Mathf.Abs(deltaX) < 3.5f && Random.value < 0.32f)
            {
                retreatDirection = -towardTarget;
                retreatUntil = Time.time + Random.Range(0.35f, 0.65f);
            }

            if (grounded && Mathf.Abs(deltaX) < 5f && Random.value < 0.22f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
                grounded = false;
            }
        }

        float distance = Mathf.Abs(deltaX);
        bool retreating = Time.time < retreatUntil;
        if (distance <= attackDistance && !retreating)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            Health targetHealth = target.GetComponentInParent<Health>();
            if (Time.time >= nextAttackTime && combat.TryAttack(targetHealth))
                nextAttackTime = Time.time + Random.Range(0.8f, 1.5f);
            if (animator != null)
                animator.SetMoving(false);
            return;
        }

        int moveDirection = retreating ? retreatDirection : towardTarget;
        body.linearVelocity = new Vector2(moveDirection * moveSpeed, body.linearVelocity.y);
        if (animator != null)
            animator.SetMoving(true);
    }

    private void ResolveTarget()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.gameObject != gameObject)
        {
            target = player.transform;
            return;
        }

        GameObject jason = GameObject.Find("Jason");
        if (jason != null)
            target = jason.transform;
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