/* File: EnemyControl.cs
   Brief: Controls enemy movement interpolation from spawn to target lane over a time window and provides initialization hooks used by the spawner.
*/

using UnityEngine;

/// <summary>
/// Controls enemy movement interpolation from spawn to target lane over a time window.
/// </summary>
public class EnemyControl : MonoBehaviour
{
    /// <summary>World movement controller associated with the enemy.</summary>
    WorldControl worldControl;
    /// <summary>Target lane point toward which the enemy moves.</summary>
    LanePoint lanePoint;
    /// <summary>Audio source whose time drives enemy movement.</summary>
    AudioSource AudioSource;
    /// <summary>World position captured when movement is initialized.</summary>
    Vector3 startPosition;
    /// <summary>World rotation captured when movement is initialized.</summary>
    Quaternion startRotation;
    /// <summary>Audio time interval over which the enemy moves.</summary>
    double startTime, endTime;

    /// <summary>Initializes the enemy's movement path and timing.</summary>
    /// <param name="world">World movement controller.</param>
    /// <param name="lane">Target lane point.</param>
    /// <param name="audio">Audio source used as the movement clock.</param>
    /// <param name="start">Movement start time in seconds.</param>
    /// <param name="end">Movement end time in seconds.</param>
    public void Init(WorldControl world, LanePoint lane, AudioSource audio, double start, double end)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        worldControl = world;
        lanePoint = lane;
        AudioSource = audio;
        startTime = start;
        endTime = end;

    }

    /// <summary>Interpolates the enemy's position and rotation toward its target lane based on audio time.</summary>
    private void Update()
    {
        var t = (float)((AudioSource.AccurateTime() - startTime) / (endTime - startTime));
        transform.position = Vector3.Lerp(startPosition, lanePoint.transform.position, t);
        transform.rotation = Quaternion.Lerp(startRotation, lanePoint.transform.rotation, t);
    }
}
