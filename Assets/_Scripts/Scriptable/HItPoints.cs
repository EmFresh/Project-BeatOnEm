using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(fileName = "HitPoints", menuName = "Scriptable Objects/HitPoints")]
public class HitPoints : ScriptableObject
{
    public List<Vector3> points = new List<Vector3>();
    public Vector3 offsetPos = Vector3.zero;
    public Vector3 offsetScale = Vector3.one;


    public Vector3 GetOffsetPoint(uint index)
    {
        if(index >= points.Count) return Vector3.zero;
        return GetOffsetPoint(points[(int)index]);
    }
    public Vector3 GetOffsetPoint(Vector3 point) => Vector3.Scale(point, offsetScale) + offsetPos;


    public GameObject CreateHitpointObject(GameObject prefab, uint index, Transform parent = null)
    {
        if(index >= points.Count) return null;


        var obj = Instantiate(prefab, parent);
        obj.transform.localPosition = GetOffsetPoint(index);
        var auto = obj.AddComponent<AutoPointLocation>();
        auto.point = points[(int)index];
        auto.hitPoints = this;

        return obj;
    }

    public GameObject CreateHitpointObject(GameObject prefab, Vector3 point, Transform parent = null)
    {

        var obj = Instantiate(prefab, parent);
        obj.transform.localPosition = GetOffsetPoint(point);
        var auto = obj.AddComponent<AutoPointLocation>();
        auto.point = point;
        auto.hitPoints = this;

        return obj;
    }
}
