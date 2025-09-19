using UnityEngine;

// Adapted from https://gist.github.com/MatthewMaker/5293052
public class DrawArrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [Tooltip("Even when true, the arrow will not be drawn in the game view.")]
    public bool alwaysDrawArrow = false;
    public Color color = Color.green;

    public float length = 1f;
    public float originOffset = 0f;
    public Vector3 direction = Vector3.zero;
    
    [Min(0f)]
    public float headLength = 0.2f;
    
    [Range(0f, 180f)]
    public float headAngle = 20f;
    
    private void OnDrawGizmosSelected()
    {
        if (!alwaysDrawArrow)
        {
            DrawArrowFunc();
        }
    }
    
    private void OnDrawGizmos()
    {
        if (alwaysDrawArrow)
        {
            DrawArrowFunc();
        }
    }

    private void DrawArrowFunc()
    {
        Gizmos.color = color;
        
        Vector3 arrowOrigin = transform.position + (transform.forward.normalized * originOffset);
        Vector3 arrowDirection = (transform.forward.normalized + direction) * length;
        
        // Draw arrow shaft
        Gizmos.DrawRay(arrowOrigin, arrowDirection);
        
        // Draw arrow head
        Vector3 right = Quaternion.LookRotation(arrowDirection) * Quaternion.Euler(0, 180 + headAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(arrowDirection) * Quaternion.Euler(0, 180 - headAngle, 0) * Vector3.forward;

        Gizmos.DrawRay(arrowOrigin + arrowDirection, right * headLength);
        Gizmos.DrawRay(arrowOrigin + arrowDirection, left * headLength);
    }
}