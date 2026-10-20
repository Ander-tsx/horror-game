using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public UIManager uiManager;
    public GameManager gameManager;
    public SpriteRenderer visual;
    public ParticleSystem bloodParticles;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D body;
    private Color originalColor = Color.white;
    private Vector3 originalVisualScale;
    private Quaternion originalVisualRotation;
    private Coroutine hitRoutine;
    private Material bloodMaterial;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        body = GetComponent<Rigidbody2D>();
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            originalColor = visual.color;
            originalVisualScale = visual.transform.localScale;
            originalVisualRotation = visual.transform.localRotation;
        }

        if (bloodParticles == null)
            CreateBloodParticles();
    }

    private void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (uiManager != null)
            uiManager.RefreshHealth(this);
    }

    public void TakeDamage(int amount, Vector2 knockback)
    {
        if (IsDead)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (uiManager != null)
            uiManager.RefreshHealth(this);

        if (body != null)
            body.AddForce(knockback, ForceMode2D.Impulse);

        EmitBlood(knockback);
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(PlayHitReaction(knockback));

        if (CurrentHealth == 0)
        {
            IsDead = true;
            SpriteFrameAnimator spriteAnimator = GetComponent<SpriteFrameAnimator>();
            if (spriteAnimator != null)
                spriteAnimator.PlayDeath();

            PlayerController player = GetComponent<PlayerController>();
            EnemyAI enemy = GetComponent<EnemyAI>();
            if (player != null) player.enabled = false;
            if (enemy != null) enemy.enabled = false;
            if (body != null) body.linearVelocity = Vector2.zero;
            if (gameManager != null)
                gameManager.FinishGame(this);
        }
    }

    private IEnumerator PlayHitReaction(Vector2 knockback)
    {
        if (visual == null)
            yield break;

        float leanDirection = knockback.x >= 0f ? -1f : 1f;
        for (int blink = 0; blink < 2; blink++)
        {
            visual.color = Color.white;
            visual.transform.localScale = Vector3.Scale(originalVisualScale, new Vector3(1.1f, 0.86f, 1f));
            visual.transform.localRotation = originalVisualRotation * Quaternion.Euler(0f, 0f, leanDirection * 9f);
            yield return new WaitForSeconds(0.055f);
            visual.color = originalColor;
            visual.transform.localScale = originalVisualScale;
            visual.transform.localRotation = originalVisualRotation;
            yield return new WaitForSeconds(0.055f);
        }

        hitRoutine = null;
    }

    private void CreateBloodParticles()
    {
        GameObject particlesObject = new GameObject("Blood Particles");
        particlesObject.transform.SetParent(transform, false);
        particlesObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        bloodParticles = particlesObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = bloodParticles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.015f, 0.025f), new Color(0.95f, 0.06f, 0.07f));
        main.gravityModifier = 0.65f;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = bloodParticles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = bloodParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 38f;
        shape.radius = 0.045f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = bloodParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(new Color(0.8f, 0.02f, 0.035f), 0f), new GradientColorKey(new Color(0.35f, 0f, 0.015f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer particleRenderer = bloodParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = visual != null ? visual.sortingOrder + 1 : 3;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            bloodMaterial = new Material(shader);
            particleRenderer.sharedMaterial = bloodMaterial;
        }
    }

    private void EmitBlood(Vector2 knockback)
    {
        if (bloodParticles == null)
            return;

        ParticleSystem.ShapeModule shape = bloodParticles.shape;
        shape.rotation = new Vector3(0f, knockback.x >= 0f ? 90f : -90f, 0f);
        bloodParticles.Emit(8);
    }

    private void OnDestroy()
    {
        if (bloodMaterial != null)
            Destroy(bloodMaterial);
    }
}