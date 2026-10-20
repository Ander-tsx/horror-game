using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Health ghostHealth;
    public Health jasonHealth;
    public UIManager uiManager;
    public AudioSource musicSource;
    public AudioSource effectsSource;
    public AudioClip deathClip;

    private bool gameFinished;

    private void Start()
    {
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }

        if (uiManager != null)
        {
            if (ghostHealth != null) uiManager.RefreshHealth(ghostHealth);
            if (jasonHealth != null) uiManager.RefreshHealth(jasonHealth);
        }
    }

    public void FinishGame(Health defeatedFighter)
    {
        if (gameFinished)
            return;

        gameFinished = true;
        if (effectsSource != null && deathClip != null)
            effectsSource.PlayOneShot(deathClip);

        StopFighter(ghostHealth);
        StopFighter(jasonHealth);
        string winner = defeatedFighter == ghostHealth ? "JASON GANA" : "GHOST GANA";
        StartCoroutine(ShowResultAfterDeath(winner));
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ShowResultAfterDeath(string winner)
    {
        yield return new WaitForSeconds(1.1f);
        if (uiManager != null)
            uiManager.ShowResult(winner);
    }

    private static void StopFighter(Health fighterHealth)
    {
        if (fighterHealth == null)
            return;

        PlayerController player = fighterHealth.GetComponent<PlayerController>();
        EnemyAI enemy = fighterHealth.GetComponent<EnemyAI>();
        Rigidbody2D body = fighterHealth.GetComponent<Rigidbody2D>();
        if (player != null) player.enabled = false;
        if (enemy != null) enemy.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}