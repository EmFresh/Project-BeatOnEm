using UnityEngine;

using System;
using UnityEngine.UI;
using UnityEngine.UIElements;

[RequireComponent(typeof(RectTransform))]
public class TimeBar : MonoBehaviour
{

    public MapEditManager mapEditManager;
    [SerializeField] RectTransform m_timeBarTrans;
    [SerializeField] float m_thickness = 5;
    [SerializeField] bool m_fallow = true;
    [SerializeField] bool m_vertical = true;

    bool vertical { get => m_vertical; set { SetBarOrientation(m_vertical = value, m_thickness); } }
    float thickness { get => m_thickness; set { SetBarOrientation(m_vertical, m_thickness = value); } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
         SetBarOrientation(m_vertical, m_thickness);
    }

    public void SetBarOrientation(bool vertical, float barThickness)
    {
        m_timeBarTrans.anchorMin = new Vector2(vertical ? 0 : .5f, vertical ? .5f : 0);
        m_timeBarTrans.anchorMax = new Vector2(vertical ? 1 : .5f, vertical ? .5f : 1);
        m_timeBarTrans.offsetMax = m_timeBarTrans.offsetMax = Vector2.zero;

        m_timeBarTrans.sizeDelta = new Vector2(vertical ? m_timeBarTrans.sizeDelta.x : barThickness, vertical ? barThickness : m_timeBarTrans.sizeDelta.y);
        m_timeBarTrans.pivot = new Vector2(.5f, .5f);
        var rect = GetComponent<RectTransform>().rect;
        m_timeBarTrans.localPosition = new Vector2(vertical ? rect.center.x : rect.xMin, vertical ? rect.yMin : rect.center.y);
    }

    // Update is called once per frame
    void Update()
    {
        if(!mapEditManager.timelineImageCreator) throw new NullReferenceException("Timeline Creater was not set");
        if(!m_timeBarTrans) throw new NullReferenceException("Time Bar was not set");
        //  timeBar. = Vector3.Lerp();

        SetLocation(mapEditManager.audioSource.time, mapEditManager.audioSource.isPlaying ? m_fallow : false);


        m_timeBarTrans.transform.SetAsLastSibling();
    }

    public void Fallow(float time)

    {
        var clip = mapEditManager.audioSource.clip;
        var timebarPos = time / clip.length;

        var sr = mapEditManager.timelineImageCreator.GetComponent<ScrollRect>();
        var rt = mapEditManager.timelineImageCreator.GetComponent<RectTransform>();


        var scrollPos = sr.normalizedPosition;
        var viewSizeNorm = rt.sizeDelta / sr.content.sizeDelta;//view size in relation to content size


        var tmpPos = (m_vertical ? scrollPos.y : scrollPos.x);
        bool InRange(float val, float min, float max) => val >= min && val <= max;

        float fallowPoint = .5f;
        float min = .0f, max = .5f;
        if(!InRange(timebarPos + Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, timebarPos),
            tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * min, tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * max))
        {
            tmpPos = Mathf.Clamp01((timebarPos +
                Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, timebarPos)) -
                (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * fallowPoint);
        }


        sr.normalizedPosition = m_vertical ? new Vector2(0, tmpPos) : new Vector2(tmpPos, 0);


    }

    public void SetLocation(float time,bool fallow =false)
    {
        if(!mapEditManager.timelineImageCreator) throw new NullReferenceException("Timeline Creater was not set");
        if(!m_timeBarTrans) throw new NullReferenceException("Time Bar was not set");

        var source = mapEditManager.audioSource;
        var clip = source.clip;
        var timebarPos = time / clip.length;

        var rect = GetComponent<RectTransform>().rect;
        var pos = Vector2.Lerp(rect.min, rect.max, timebarPos);

        m_timeBarTrans.localPosition = new Vector2(
            m_vertical ? m_timeBarTrans.localPosition.x : pos.x,
            m_vertical ? pos.y : m_timeBarTrans.localPosition.y);

        if(fallow)
            Fallow(time);

        m_timeBarTrans.transform.SetAsLastSibling();

       // currentTime = time;
    }
}
