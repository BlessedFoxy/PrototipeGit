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
    public float approachSpeed = 1.2f;
    public float slowDownDistance = 10f;
    public float slowDownTargetSpeed = 0.5f;

    [Header("Настройки дороги")]
    public float campDistanceOnRoad = 0f;

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

    // Сохранение
    private const string SAVE_KEY = "LastCampfireDistance";
    private const string SAVE_KEY_HAS_DATA = "HasCampfireSaveData";

    void Start()
    {
        FindPlayer();
        if (fireLight != null) fireLight.enabled = false;
        if (fireParticles != null) fireParticles.Stop();

        // Загрузка сохранения
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

        // Плавное замедление
        if (!isResting && !isApproaching && !hasBeenUsed)
        {
            if (distance < slowDownDistance && distance > activationDistance)
            {
                float t = (slowDownDistance - distance) / (slowDownDistance - activationDistance);
                float targetSpeed = Mathf.Lerp(trailWalker.defaultWalkSpeed, slowDownTargetSpeed, t);
                trailWalker.SetSpeed(Mathf.Max(targetSpeed, 0.3f));
            }
            else if (distance < activationDistance)
            {
                trailWalker.SetSpeed(0f);
                EnterCampfire();
            }
        }

        // Выход
        if (isResting)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                ExitCampfire();
                return;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ExitCampfire();
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                ExitCampfire();
                return;
            }
        }

        // Анимация подхода
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

        if (trailWalker != null)
        {
            // 1. Останавливаем скорость
            trailWalker.SetSpeed(0f);
            // 2. Включаем режим сидения в TrailWalker (это отключит притяжение к сплайну!)
            trailWalker.SetSitting(true);
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

        // Сохранение дистанции
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
            // 1. Говорим шагоходу, что внутренняя дистанция теперь равна дистанции костра
            trailWalker.SetDistance(campDistanceOnRoad);

            // 2. Запускаем плавный возврат на дорогу!
            // Персонаж физически дойдет от точки костра до сплайна, 
            // и только потом продолжит путь по тропе.
            trailWalker.StartReturningToRoad();

            Debug.Log($"[Campfire] ➡️ Возврат на дорогу с дистанции: {campDistanceOnRoad:F1}m");
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

    public void Initialize(RoadGeneratorEditor generator, float distance)
    {
        campDistanceOnRoad = distance;
        Debug.Log($"[Campfire] Инициализирован с дистанцией: {distance:F1}m");
    }

    public bool IsResting()
    {
        return isResting;
    }

    [ContextMenu("Clear Save Data")]
    public void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(SAVE_KEY_HAS_DATA);
        PlayerPrefs.Save();
        Debug.Log("🗑️ Данные сохранения очищены");
    }
}