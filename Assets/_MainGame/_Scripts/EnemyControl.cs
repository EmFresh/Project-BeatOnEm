using UnityEngine;

public class EnemyControl : MonoBehaviour
{
    WorldControl worldControl;
    LanePoint lanePoint;
    AudioSource AudioSource;
    Vector3 startPosition;
    Quaternion startRotation;
    double startTime, endTime;
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

    private void Update()
    {
        var t = (float)((AudioSource.AccurateTime() - startTime) / (endTime - startTime));
        transform.position = Vector3.Lerp(startPosition, lanePoint.transform.position, t);
        transform.rotation = Quaternion.Lerp(startRotation, lanePoint.transform.rotation, t);
    }
}
