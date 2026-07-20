using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Splines.Examples
{
    [RequireComponent(typeof(LineRenderer))]
    public class ShowNearestPoint : MonoBehaviour
    {
        Vector3 m_Center = Vector3.zero;
        float m_Size = 50f;

        private SplineContainer m_SplineContainer;
        LineRenderer m_LineRenderer;

        [SerializeField]
        Transform m_NearestPoint;

        void Start()
        {
            m_SplineContainer = FindAnyObjectByType<SplineContainer>();

            if (m_SplineContainer == null)
            {
                Debug.LogError("SplineContainer не найден в сцене!");
                return;
            }

            Debug.Log($"SplineContainer найден: {m_SplineContainer.gameObject.name}");
        }

        void Update()
        {
            var position = CalculatePosition();
            var nearest = new float4(0, 0, 0, float.PositiveInfinity);

            // Исправление: работаем напрямую со сплайном
            if (m_SplineContainer != null && m_SplineContainer.Spline != null)
            {
                using var native = new NativeSpline(m_SplineContainer.Spline, m_SplineContainer.transform.localToWorldMatrix);
                float d = SplineUtility.GetNearestPoint(native, transform.position, out float3 p, out float t);
                if (d < nearest.w)
                    nearest = new float4(p, d);
            }

            m_LineRenderer.SetPosition(0, position);
            m_LineRenderer.SetPosition(1, nearest.xyz);
            m_NearestPoint.position = nearest.xyz;
            transform.position = position;
        }

        Vector3 CalculatePosition()
        {
            float time = Time.time * .2f, time1 = time + 1;
            float half = m_Size * .5f;

            return m_Center + new Vector3(
                Mathf.PerlinNoise(time, time) * m_Size - half,
                0,
                Mathf.PerlinNoise(time1, time1) * m_Size - half
            );
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(m_Center, new Vector3(m_Size, .1f, m_Size));
        }
    }
}