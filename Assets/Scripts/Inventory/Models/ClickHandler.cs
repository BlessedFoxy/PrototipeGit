using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    public System.Action OnClick;
    public System.Action OnDrag;
    public System.Action OnRelease;

    private bool isDragging = false;

    void OnMouseDown()
    {
        isDragging = true;
        OnClick?.Invoke();
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            OnDrag?.Invoke();
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            OnRelease?.Invoke();
        }
    }
}