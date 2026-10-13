using UnityEngine;

/// <summary>
/// Represents a lane position where enemies or objects can be targeted or spawned.
/// </summary>
public class LanePoint : MonoBehaviour
{
   /// <summary>
   /// Whether this lane point is currently occupied or reserved.
   /// </summary>
   public bool inUse = false;
}
