using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class HitPointVisualizer : MonoBehaviour
{
    public HitPoints hitPoints;
    public GameObject point;
    List<GameObject> pointInstances = new List<GameObject>();

    private void Awake()
    {
        foreach(var point in hitPoints.points)
        {
            var pos = hitPoints.GetOffsetPoint(point);

            var obj = Instantiate(this.point, transform, false);

            obj.transform.localPosition = pos;
        }

    }

    // Update is called once per frame
    void Update()
    {
        int count = 0;
        foreach(var point in hitPoints.points)
        {
            var pos = hitPoints.GetOffsetPoint(point);
            if(count >= pointInstances.Count)
                pointInstances.Add(Instantiate(this.point, transform, false));

            pointInstances[count].transform.localPosition = pos;
        }
    }
}
