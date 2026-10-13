using UnityEngine;

using System;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Events;

/// <summary>
/// Visual time bar that indicates the current audio time and can move the timeline view.
/// Raises an event when the time bar moves so listeners can update displays.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TimeBar : MonoBehaviour
{

    /// <summary>
    /// Link to the MapEditManager to query the audio source and timeline info.
    /// </summary>
    public MapEditManager mapEditManager;
    /// <summary>
    /// RectTransform representing the time bar visual element.
    /// </summary>
    [SerializeField] RectTransform m_timeBarTrans;
    /// <summary>
    /// Pixel thickness of the time bar.
    /// </summary>
    [SerializeField] float m_thickness = 5;
    /// <summary>
    /// Whether the timeline should follow playback while playing.
    /// </summary>
    [SerializeField] bool m_fallow = true;
    /// <summary>
    /// Whether the time bar is vertical.
    /// </summary>
    [SerializeField] bool m_vertical = true;
    /// <summary>
    /// Current time represented by the time bar.
    /// </summary>
    public double currentTime = -1;
    /// <summary>
    /// Event invoked when the time bar is moved. Provides the new time in seconds.
    /// </summary>
    public UnityEvent<double> onTimeBarMoved = new UnityEvent<double>();

    bool Vertical { get => m_vertical; set { SetBarOrientation(m_vertical = value, m_thickness); } }
    float Thickness { get => m_thickness; set { SetBarOrientation(m_vertical, m_thickness = value); } }

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

        SetLocation(mapEditManager.AudioSource.AccurateTime(), mapEditManager.AudioSource.isPlaying ? m_fallow : false);


        m_timeBarTrans.transform.SetAsLastSibling();
    }

    public void Fallow(double time)

    {
        var clip = mapEditManager.AudioSource.clip;
        var timebarPos = time / clip.AccurateLength();

        var sr = mapEditManager.timelineImageCreator.GetComponent<ScrollRect>();
        var rt = mapEditManager.timelineImageCreator.GetComponent<RectTransform>();


        var scrollPos = sr.normalizedPosition;
        var viewSizeNorm = rt.rect.size / sr.content.rect.size;//view size in relation to content size


        var tmpPos = (m_vertical ? scrollPos.y : scrollPos.x);
        bool InRange(double val, double min, double max) => val >= min && val <= max;

        float fallowPoint = .5f;
        float min = .0f, max = .5f;
        if(!InRange(timebarPos + Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, (float)timebarPos),
            tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * min, tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * max))
        {
            tmpPos = Mathf.Clamp01(((float)timebarPos +
                Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, (float)timebarPos)) -
                (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * fallowPoint);
        }


        sr.normalizedPosition = m_vertical ? new Vector2(0, tmpPos) : new Vector2(tmpPos, 0);


    }

    public void SetLocation(double time, bool fallow = false)
    {
        var source = mapEditManager.AudioSource;
        var clip = source.clip;

        if(clip.AccurateLength() <= 0) return;
        if(!mapEditManager.timelineImageCreator) throw new NullReferenceException("Timeline Creater was not set");
        if(!m_timeBarTrans) throw new NullReferenceException("Time Bar was not set");

        var timebarPos = time / clip.AccurateLength();

        var rect = GetComponent<RectTransform>().rect;
        var pos = Vector2.Lerp(rect.min, rect.max, (float)timebarPos);

        m_timeBarTrans.localPosition = new Vector2(
            m_vertical ? m_timeBarTrans.localPosition.x : pos.x,
            m_vertical ? pos.y : m_timeBarTrans.localPosition.y);

        if(fallow)
            Fallow(time);

        onTimeBarMoved.Invoke(currentTime = time);

        m_timeBarTrans.transform.SetAsLastSibling();
    }
}
