using UnityEngine;

public class InventoryCamera : MonoBehaviour
{
    public RenderTexture targetTexture;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null && targetTexture != null)
        {
            cam.targetTexture = targetTexture;
        }
    }
}