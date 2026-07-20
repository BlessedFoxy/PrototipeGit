using UnityEngine;

public class Campfire : MonoBehaviour
{
    [Header("Настройки")]
    public float activationDistance = 5f;
    public GameObject interactPrompt;

    [Header("Эффекты")]
    public ParticleSystem fireParticles;
    public Light fireLight;

    private Transform player;
    private bool isPlayerNear = false;
    private bool isResting = false;
    private TrailWalker trailWalker;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            trailWalker = player.GetComponent<TrailWalker>();
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (fireLight != null)
            fireLight.enabled = false;

        if (fireParticles != null)
            fireParticles.Stop();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerNear = distance < activationDistance;

        if (interactPrompt != null)
            interactPrompt.SetActive(isPlayerNear && !isResting);

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            EnterCampfire();
        }

        if (isResting && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitCampfire();
        }
    }

    void EnterCampfire()
    {
        if (isResting) return;
        isResting = true;

        if (fireParticles != null)
            fireParticles.Play();
        if (fireLight != null)
            fireLight.enabled = true;

        if (trailWalker != null)
        {
            trailWalker.SetSpeed(0f);
        }

        Debug.Log("🏕️ Entered campfire! Press Escape to exit.");
    }

    public void ExitCampfire()
    {
        if (!isResting) return;
        isResting = false;

        if (fireParticles != null)
            fireParticles.Stop();
        if (fireLight != null)
            fireLight.enabled = false;

        if (trailWalker != null)
        {
            trailWalker.SetSpeed(2f);
        }

        Debug.Log("🚶 Exited campfire!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}