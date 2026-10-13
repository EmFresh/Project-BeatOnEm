/*
 File: AutoPointLocation.cs
 Brief: Positions a GameObject at a computed offset from a HitLocations instance; useful for auto-updating hitpoint visuals.
*/

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
    /// <summary>Updates the attached object's position from its configured hit-location data.</summary>
    void Update()
    {
        var pos = hitPoints.GetOffsetPoint(point);

        if(transform.localPosition != pos)
            transform.localPosition = pos;
    }
}
