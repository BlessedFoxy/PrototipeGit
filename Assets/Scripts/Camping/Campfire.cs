using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;

public class Campfire : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform sitPoint;
    public ParticleSystem fireParticles;
    public Light fireLight;

    [Header("Настройки лагеря")]
    public float activationDistance = 8f;
    public float slowDownDistance = 15f;
    public float approachSpeed = 2.5f;
    public float exitSpeed = 2f;
    public float slowDownTargetSpeed = 1f;

    [Header("Настройки дороги")]
    public float campDistanceOnRoad = 0f;

    private Transform player;
    private TrailWalker trailWalker;
    private Animator playerAnimator;
    private SplineContainer splineContainer;

    private bool isResting = false;
    private bool isApproaching = false;
    private bool isExiting = false;
    private bool hasBeenUsed = false;
    private bool isSlowingDown = false;

    private Vector3 entryPosition;
    private Quaternion entryRotation;
    private Vector3 sitPosition;
    private Quaternion sitRotation;
    private Vector3 exitPosition;
    private Quaternion exitRotation;

    private float moveProgress = 0f;

    // ✅ ДЛЯ ПЛАВНОГО ВЫХОДА
    private float exitDistance = 0f;

    void Start()
    {
        FindPlayer();

        if (splineContainer == null)
        {
            splineContainer = FindAnyObjectByType<SplineContainer>();
        }

        if (fireLight != null)
            fireLight.enabled = false;
        if (fireParticles != null)
            fireParticles.Stop();
    }

    void Update()
    {
        if (player == null || trailWalker == null)
        {
            FindPlayer();
            return;
        }

        // ============================================
        // 1. ПОДХОД К ЛАГЕРЮ
        // ============================================
        if (!isResting && !isApproaching && !isExiting && !hasBeenUsed)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float playerRoadDistance = trailWalker.GetCurrentDistance();
            float roadDiff = Mathf.Abs(playerRoadDistance - campDistanceOnRoad);

            if (distance < slowDownDistance && distance > activationDistance && roadDiff < 20f)
            {
                float t = (slowDownDistance - distance) / (slowDownDistance - activationDistance);
                float targetSpeed = Mathf.Lerp(trailWalker.defaultWalkSpeed, slowDownTargetSpeed, t);
                trailWalker.SetSpeed(Mathf.Max(targetSpeed, 0.3f));
                isSlowingDown = true;
            }
            else if (distance < activationDistance && roadDiff < 15f && !isApproaching && !isResting)
            {
                Debug.Log($"🔥 Вход в лагерь!");
                trailWalker.SetSpeed(0f);
                StartApproach();
            }
            else if (isSlowingDown && distance > slowDownDistance)
            {
                isSlowingDown = false;
                trailWalker.SetSpeed(trailWalker.defaultWalkSpeed);
            }
        }

        // ============================================
        // 2. ОЖИДАНИЕ ВЫХОДА
        // ============================================
        if (isResting && !isExiting)
        {
            player.position = sitPosition;
            player.rotation = sitRotation;

            bool wPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            bool leftClick = Input.GetMouseButtonDown(0);

            bool touchTap = false;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended)
                {
                    touchTap = true;
                }
            }

            if (wPressed || leftClick || touchTap)
            {
                Debug.Log("🚶 Выход из лагеря!");
                StartExit();
            }
        }

        // ============================================
        // 3. АНИМАЦИЯ ПОДХОДА
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
            player.position = Vector3.Lerp(entryPosition, sitPosition, t);
            player.rotation = Quaternion.Lerp(entryRotation, sitRotation, t);
        }

        // ============================================
        // 4. АНИМАЦИЯ ВЫХОДА
        // ============================================
        if (isExiting)
        {
            moveProgress += Time.deltaTime * exitSpeed;

            if (moveProgress >= 1f)
            {
                moveProgress = 1f;
                isExiting = false;
                OnExitedCampfire();
            }

            float t = Mathf.SmoothStep(0f, 1f, moveProgress);
            player.position = Vector3.Lerp(sitPosition, exitPosition, t);
            player.rotation = Quaternion.Lerp(sitRotation, exitRotation, t);
        }
    }

    // ============================================
    // НАЧАЛО ПОДХОДА
    // ============================================

    void StartApproach()
    {
        if (isApproaching || isResting || isExiting) return;

        Debug.Log("🚶 Плавный подход к лагерю...");

        if (trailWalker != null)
        {
            trailWalker.SetSpeed(0f);
            trailWalker.enabled = false;
        }

        entryPosition = player.position;
        entryRotation = player.rotation;

        if (sitPoint != null)
        {
            sitPosition = sitPoint.position;
            sitRotation = sitPoint.rotation;
        }
        else
        {
            sitPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            sitRotation = Quaternion.LookRotation(-transform.forward);
        }

        // ============================================
        // ВЫХОДИМ ВПЕРЁД ОТ ЛАГЕРЯ
        // ============================================
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.magnitude < 0.1f) forward = Vector3.forward;
        forward.Normalize();

        exitPosition = transform.position + forward * 3f + Vector3.up * 0.5f;
        exitRotation = Quaternion.LookRotation(forward);

        // ✅ Вычисляем дистанцию на сплайне для точки выхода
        exitDistance = GetDistanceOnSpline(exitPosition);

        Debug.Log($"📍 Точка выхода: {exitPosition}");
        Debug.Log($"📍 Дистанция выхода: {exitDistance:F1}m");

        moveProgress = 0f;
        isApproaching = true;
        isSlowingDown = false;
    }

    // ============================================
    // ПОЛУЧАЕМ ДИСТАНЦИЮ НА СПЛАЙНЕ
    // ============================================
    private float GetDistanceOnSpline(Vector3 worldPosition)
    {
        if (splineContainer == null || splineContainer.Spline == null)
        {
            return trailWalker.GetCurrentDistance() + 3f;
        }

        var spline = splineContainer.Spline;
        int segments = 200;
        float minDistance = float.MaxValue;
        float bestT = 0f;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float3 pos = spline.EvaluatePosition(t);
            Vector3 worldPos = splineContainer.transform.TransformPoint(new Vector3(pos.x, pos.y, pos.z));

            float dist = Vector3.Distance(worldPos, worldPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestT = t;
            }
        }

        float totalLength = GetSplineLength();
        return bestT * totalLength;
    }

    private float GetSplineLength()
    {
        if (splineContainer == null || splineContainer.Spline == null) return 100f;

        var spline = splineContainer.Spline;
        int segments = 200;
        float total = 0f;

        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            float3 p1 = spline.EvaluatePosition(t1);
            float3 p2 = spline.EvaluatePosition(t2);
            total += math.distance(p1, p2);
        }

        return total > 1f ? total : 100f;
    }

    void OnReachedCampfire()
    {
        Debug.Log("🏕️ Персонаж сел у костра!");

        player.position = sitPosition;
        player.rotation = sitRotation;

        if (fireParticles != null)
            fireParticles.Play();
        if (fireLight != null)
            fireLight.enabled = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", true);
        }

        isResting = true;
        hasBeenUsed = true;

        Debug.Log("🟢 Нажмите W, ЛКМ или тапните, чтобы встать");
    }

    void StartExit()
    {
        if (isExiting || !isResting) return;

        Debug.Log("🚶 Выход из лагеря...");

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
        }

        if (fireParticles != null)
            fireParticles.Stop();
        if (fireLight != null)
            fireLight.enabled = false;

        moveProgress = 0f;
        isExiting = true;
        isResting = false;
    }

    // ============================================
    // ✅ ПЛАВНЫЙ ВЫХОД
    // ============================================
    void OnExitedCampfire()
    {
        Debug.Log("✅ Персонаж вышел, продолжает путь!");

        if (trailWalker != null)
        {
            // ✅ Устанавливаем дистанцию на сплайне
            trailWalker.SetDistance(exitDistance);

            // ✅ ВКЛЮЧАЕМ ДВИЖЕНИЕ
            trailWalker.enabled = true;
            trailWalker.SetSpeed(trailWalker.defaultWalkSpeed);

            Debug.Log($"✅ Дистанция установлена: {exitDistance:F1}m");
        }

        hasBeenUsed = true;
    }

    // ============================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            trailWalker = playerObj.GetComponent<TrailWalker>();
            playerAnimator = playerObj.GetComponentInChildren<Animator>();

            if (trailWalker != null && trailWalker.trailSpline != null)
            {
                splineContainer = trailWalker.trailSpline;
            }
        }
    }

    public void Initialize(RoadGeneratorEditor generator, float distance)
    {
        campDistanceOnRoad = distance;

        if (sitPoint == null)
        {
            GameObject sit = new GameObject("SitPoint");
            sit.transform.SetParent(transform);
            sit.transform.localPosition = new Vector3(0f, 0.5f, 1.5f);
            sitPoint = sit.transform;
        }
    }

    public bool IsResting => isResting;
    public bool IsActive => !hasBeenUsed;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, slowDownDistance);

        if (sitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(sitPoint.position, 0.3f);
        }

        if (exitPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(exitPosition, 0.5f);
            Gizmos.DrawLine(sitPosition, exitPosition);
        }
    }
}