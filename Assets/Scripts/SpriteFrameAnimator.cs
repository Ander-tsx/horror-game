using UnityEngine;

public enum SpriteAnimationState
{
    Idle,
    Walk,
    Attack,
    Death
}

public class SpriteFrameAnimator : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] deathFrames;

    [Header("Ajustes")]
    [Min(0.02f)] public float frameDuration = 0.11f;
    public SpriteRenderer targetRenderer;

    private SpriteAnimationState currentState = SpriteAnimationState.Idle;
    private float frameTimer;
    private float attackTimer;
    private int frameIndex;
    private bool facingRight = true;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyFrame();
    }

    private void Update()
    {
        if (currentState == SpriteAnimationState.Attack)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
                SetState(SpriteAnimationState.Idle, true);
        }

        Sprite[] frames = GetFrames(currentState);
        if (frames == null || frames.Length == 0)
            return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;
            if (currentState == SpriteAnimationState.Death)
                frameIndex = Mathf.Min(frameIndex + 1, frames.Length - 1);
            else
                frameIndex = (frameIndex + 1) % frames.Length;

            ApplyFrame();
        }
    }

    public void SetMoving(bool moving)
    {
        if (currentState == SpriteAnimationState.Death || currentState == SpriteAnimationState.Attack)
            return;

        SetState(moving ? SpriteAnimationState.Walk : SpriteAnimationState.Idle);
    }

    public void PlayAttack()
    {
        if (currentState == SpriteAnimationState.Death)
            return;

        SetState(SpriteAnimationState.Attack, true);
        Sprite[] frames = GetFrames(SpriteAnimationState.Attack);
        attackTimer = Mathf.Max(0.24f, (frames == null ? 0 : frames.Length) * frameDuration);
    }

    public void PlayDeath()
    {
        SetState(SpriteAnimationState.Death, true);
    }

    public void SetFacingDirection(int direction)
    {
        if (direction == 0)
            return;

        facingRight = direction > 0;
        if (targetRenderer != null)
            targetRenderer.flipX = !facingRight;
    }

    private void SetState(SpriteAnimationState newState, bool restart = false)
    {
        if (!restart && currentState == newState)
            return;

        currentState = newState;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyFrame();
    }

    private Sprite[] GetFrames(SpriteAnimationState state)
    {
        switch (state)
        {
            case SpriteAnimationState.Walk: return walkFrames;
            case SpriteAnimationState.Attack: return attackFrames;
            case SpriteAnimationState.Death: return deathFrames;
            default: return idleFrames;
        }
    }

    private void ApplyFrame()
    {
        if (targetRenderer == null)
            return;

        Sprite[] frames = GetFrames(currentState);
        if (frames != null && frames.Length > 0 && frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)] != null)
        {
            targetRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            // El origen del personaje permanece en los pies aunque varíe el ancho del recorte.
            Vector3 position = targetRenderer.transform.localPosition;
            targetRenderer.transform.localPosition = new Vector3(-targetRenderer.sprite.bounds.center.x, position.y, position.z);
        }

        targetRenderer.flipX = !facingRight;
    }
}