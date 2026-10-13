using UnityEngine;

/// <summary>
/// Keeps a GameObject positioned at a computed hit location point inside a HitLocations instance.
/// Useful for automatically updating instantiated hitpoint objects when offsets change.
/// </summary>
public class AutoPointLocation : MonoBehaviour
{
    /// <summary>
    /// Local-space point value used to compute the offset position.
    /// </summary>
    public Vector3 point;
    /// <summary>
    /// Source HitLocations instance used to compute the actual local position.
    /// </summary>
    public HitLocations hitPoints;
    // Update is called once per frame
    void Update()
    {
        var pos = hitPoints.GetOffsetPoint(point);

        if(transform.localPosition != pos)
            transform.localPosition = pos;
    }
}
