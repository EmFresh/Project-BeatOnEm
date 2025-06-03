using UnityEngine;

public class AutoPointLocation : MonoBehaviour
{
    public Vector3 point;
    public HitPoints hitPoints;
    // Update is called once per frame
    void Update()
    {
        var pos = hitPoints.GetOffsetPoint(point);

        if(transform.localPosition != pos)
            transform.localPosition = pos;
    }
}
