using UnityEngine;

public class EnemyControl : MonoBehaviour
{
    WorldControl worldControl;
    LanePoint lanePoint;
    AudioSource AudioSource;
    Vector3 startPosition;
    double startTime, endTime;
    public void Init(WorldControl world, LanePoint lane, AudioSource audio, double start, double end)
    {
        startPosition = transform.position;
        worldControl = world;
        lanePoint = lane;
        AudioSource = audio;
        startTime = start;
        endTime = end;

    }

    private void Update()
    {
        transform.position = Vector3.Lerp(startPosition, lanePoint.transform.position, (float)((AudioSource.time - startTime) / (endTime - startTime)));
    }
}
