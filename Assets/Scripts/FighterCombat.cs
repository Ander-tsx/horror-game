using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FighterCombat : MonoBehaviour
{
    public int damage = 10;
    public float attackCooldown = 0.55f;
    public float attackReach = 1.05f;
    public float hitRadius = 0.72f;
    public float knockbackForce = 4.5f;
    public float knockbackUpward = 1.2f;
    public AudioClip attackClip;
    public Health health;
    public SpriteFrameAnimator animator;

    private AudioSource audioSource;
    private int facingDirection = 1;
    private float nextAttackTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (health == null)
            health = GetComponent<Health>();
        if (animator == null)
            animator = GetComponent<SpriteFrameAnimator>();
    }

    public void SetFacingDirection(int direction)
    {
        if (direction == 0)
            return;

        facingDirection = direction > 0 ? 1 : -1;
        if (animator != null)
            animator.SetFacingDirection(facingDirection);
    }

    public bool TryAttack(Health targetOverride = null)
    {
        if (health == null || health.IsDead || Time.time < nextAttackTime)
            return false;

        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
            animator.PlayAttack();
        if (audioSource != null && attackClip != null)
            audioSource.PlayOneShot(attackClip);

        if (targetOverride != null)
        {
            if (targetOverride != health && !targetOverride.IsDead)
            {
                Vector2 knockback = new Vector2(facingDirection * knockbackForce, knockbackUpward);
                targetOverride.TakeDamage(damage, knockback);
            }
            return true;
        }

        // Un único solapamiento por ataque impide aplicar daño varias veces al mismo golpe.
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(facingDirection * attackReach, 0.9f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitRadius);
        foreach (Collider2D hit in hits)
        {
            Health target = hit.GetComponentInParent<Health>();
            if (target == null || target == health || target.IsDead)
                continue;

            Vector2 knockback = new Vector2(facingDirection * knockbackForce, knockbackUpward);
            target.TakeDamage(damage, knockback);
            break;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(facingDirection * attackReach, 0.9f, 0f), hitRadius);
    }
}