using UnityEngine;

public class EnemyControl : MonoBehaviour
{
    WorldControl worldControl;
    LanePoint lanePoint;
    AudioSource audioSource;
    Vector3 startPosition;
    float startTime,endTime;
    public void Init(WorldControl world, LanePoint lane, AudioSource audio, float start, float end)
    {
        startPosition = transform.position;
        worldControl = world;
        lanePoint = lane;
        audioSource = audio;
        startTime = start;
        endTime = end;

    }

    private void Update()
    {
        transform.position = Vector3.Lerp(startPosition, lanePoint.transform.position  , (audioSource.time - startTime) / (endTime - startTime));
    }
}
