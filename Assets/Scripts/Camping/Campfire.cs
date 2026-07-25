using UnityEngine;
using System.Collections;

public class Campfire : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform sitPoint;
    public ParticleSystem fireParticles;
    public Light fireLight;

    [Header("Настройки")]
    public float activationDistance = 5f;
    public float approachSpeed = 2f;

    private Transform player;
    private TrailWalker trailWalker;
    private Animator playerAnimator;
    private bool isResting = false;
    private bool isApproaching = false;
    private bool hasBeenUsed = false;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float moveProgress = 0f;

    private Vector3 savedSitPosition;
    private Quaternion savedSitRotation;

    // ============================================
    // 🔥 КЛЮЧИ ДЛЯ СОХРАНЕНИЯ
    // ============================================
    private const string SAVE_KEY = "LastCampfireDistance";
    private const string SAVE_KEY_HAS_DATA = "HasCampfireSaveData";

    void Start()
    {
        FindPlayer();
        if (fireLight != null) fireLight.enabled = false;
        if (fireParticles != null) fireParticles.Stop();

        // ============================================
        // 🔥 ЗАГРУЗКА СОХРАНЁННОЙ ДИСТАНЦИИ
        // ============================================
        if (PlayerPrefs.HasKey(SAVE_KEY_HAS_DATA) && PlayerPrefs.GetInt(SAVE_KEY_HAS_DATA) == 1)
        {
            float savedDistance = PlayerPrefs.GetFloat(SAVE_KEY, 0f);
            if (trailWalker != null && savedDistance > 0f)
            {
                trailWalker.SetDistance(savedDistance);
                Debug.Log($"📂 Загружена дистанция: {savedDistance:F1}m");
            }
        }
    }

    void Update()
    {
        if (player == null || trailWalker == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ============================================
        // АВТОМАТИЧЕСКИЙ ВХОД
        // ============================================
        if (!isResting && !isApproaching && !hasBeenUsed && distance < activationDistance)
        {
            Debug.Log("[Campfire] 🔥 ВХОД В ЛАГЕРЬ!");
            EnterCampfire();
        }

        // ============================================
        // ВЫХОД ПО W
        // ============================================
        if (isResting && Input.GetKeyDown(KeyCode.W))
        {
            ExitCampfire();
        }

        // ============================================
        // АНИМАЦИЯ ПОДХОДА
        // ============================================
        if (isApproaching)
        {
            moveProgress += Time.deltaTime * approachSpeed;
            if (moveProgress >= 1f)
            {
                moveProgress = 1f;
                isApproaching = false;
                OnReachedCampfire();
            }

            float t = Mathf.SmoothStep(0f, 1f, moveProgress);
            player.position = Vector3.Lerp(startPosition, targetPosition, t);
            player.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
        }
    }

    void EnterCampfire()
    {
        if (isApproaching || isResting) return;

        // Останавливаем движение
        if (trailWalker != null)
        {
            trailWalker.SetSpeed(0f);
            trailWalker.enabled = false;
        }

        startPosition = player.position;
        startRotation = player.rotation;

        if (sitPoint != null)
        {
            targetPosition = sitPoint.position;
            targetRotation = sitPoint.rotation;
        }
        else
        {
            targetPosition = transform.position + transform.forward * 1.5f;
            targetRotation = Quaternion.LookRotation(-transform.forward);
        }

        moveProgress = 0f;
        isApproaching = true;
    }

    void OnReachedCampfire()
    {
        savedSitPosition = targetPosition;
        savedSitRotation = targetRotation;

        player.position = savedSitPosition;
        player.rotation = savedSitRotation;

        if (fireParticles != null) fireParticles.Play();
        if (fireLight != null) fireLight.enabled = true;

        if (playerAnimator != null)
            playerAnimator.SetBool("IsSitting", true);

        isResting = true;
        hasBeenUsed = true;

        // ============================================
        // 🔥 СОХРАНЯЕМ ДИСТАНЦИЮ
        // ============================================
        if (trailWalker != null)
        {
            float currentDistance = trailWalker.GetCurrentDistance();
            PlayerPrefs.SetFloat(SAVE_KEY, currentDistance);
            PlayerPrefs.SetInt(SAVE_KEY_HAS_DATA, 1);
            PlayerPrefs.Save();
            Debug.Log($"💾 СОХРАНЕНО: Дистанция {currentDistance:F1}m");
        }

        Debug.Log("[Campfire] 🏕️ Персонаж сел у костра!");
    }

    void ExitCampfire()
    {
        if (!isResting) return;

        if (playerAnimator != null)
            playerAnimator.SetBool("IsSitting", false);

        if (fireParticles != null) fireParticles.Stop();
        if (fireLight != null) fireLight.enabled = false;

        if (trailWalker != null)
        {
            trailWalker.enabled = true;
            trailWalker.SetSpeed(trailWalker.defaultWalkSpeed);
        }

        isResting = false;
        Debug.Log("[Campfire] 🚶 Выход из лагеря!");
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            trailWalker = playerObj.GetComponent<TrailWalker>();
            playerAnimator = playerObj.GetComponentInChildren<Animator>();
        }
    }

    public void Initialize(RoadGeneratorEditor generator, float distance) { }

    // ============================================
    // 🔥 ОЧИСТКА СОХРАНЕНИЯ (ДЛЯ ОТЛАДКИ)
    // ============================================
    [ContextMenu("Clear Save Data")]
    public void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(SAVE_KEY_HAS_DATA);
        PlayerPrefs.Save();
        Debug.Log("🗑️ Данные сохранения очищены");
    }
}