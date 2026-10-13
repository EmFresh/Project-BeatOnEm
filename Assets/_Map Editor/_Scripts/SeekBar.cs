/*
 File: SeekBar.cs
 Brief: Interactive seek bar control used by the map editor timeline. Supports pointer interactions,
 snapping to beats, and notifying listeners when the playhead position changes.
*/

using System;
using System.Collections;
using System.Linq;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
[RequireComponent(typeof(RectTransform), typeof(EventTrigger))]
/// <summary>
/// Interactive seek bar used to set and follow playback time inside the map editor timeline.
/// Supports clicking to set time, snapping to beats, and notifying listeners when the bar moves.
/// </summary>
public class SeekBar : MonoBehaviour
{

    /*
     Beginner
     Easy
     Advanced
     Trojan
    */


    /// <summary>
    /// Reference to the MapEditManager for audio and timeline data.
    /// </summary>
    public MapEditManager mapEditManager;

    /// <summary>
    /// RectTransform representing the visual seek bar element.
    /// </summary>
    [SerializeField] RectTransform m_seekBarTrans;
    /// <summary>
    /// Thickness (in pixels) of the seek bar visual.
    /// </summary>
    [SerializeField] float m_thickness = 5;
    /// <summary>
    /// Whether the seek bar is vertical (true) or horizontal (false).
    /// </summary>
    [SerializeField] bool m_vertical = true;
    /// <summary>
    /// When true, the seek position snaps to nearest beat values.
    /// </summary>
    [field: SerializeField] public bool snap { get; set; } = true;
    /// <summary>
    /// When true, the timeline view will follow the seek bar when playback moves.
    /// </summary>
    [field: SerializeField] public bool fallow { get; set; } = true;
    /// <summary>
    /// Current time represented by the seek bar in seconds.
    /// </summary>
    public double currentTime = -1;
    /// <summary>
    /// Event invoked when the seek bar position changes. Provides the new time value (seconds).
    /// </summary>
    public UnityEvent<double> onSeekBarMoved = new UnityEvent<double>();
    bool vertical { get => m_vertical; set { SetBarOrientation(m_vertical = value, m_thickness); } }
    float thickness { get => m_thickness; set { SetBarOrientation(m_vertical, m_thickness = value); } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /// <summary>
    /// Awake lifecycle callback. Sets initial orientation and hooks pointer click events for seeking.
    /// </summary>
    void Awake()
    {


        SetBarOrientation(m_vertical, m_thickness);

        var eventTriggers = GetComponent<EventTrigger>();
        eventTriggers.triggers.Add(new EventTrigger.Entry() { eventID = EventTriggerType.PointerClick });
        eventTriggers.triggers.Last().callback.AddListener(
            new UnityAction<BaseEventData>((data) =>
            {
                PointerEventData ped = (PointerEventData)data;

                if(ped.button == PointerEventData.InputButton.Left)
                {

                    var pointPressed =// ped.pointerPressRaycast.worldPosition;
                    Mouse.current.position.value;
                    //print($"Pressed at: {pointPressed}");
                    pointPressed = GetComponent<RectTransform>().InverseTransformPoint(pointPressed);
                    var rect = GetComponent<RectTransform>().rect;
                    float timeNorm = m_vertical ?
                        (pointPressed.y - rect.yMin) / rect.height :
                        (pointPressed.x - rect.xMin) / rect.width;
                    var clip = mapEditManager.AudioSource.clip;
                    var time = timeNorm * clip.AccurateLength();

                  //  print($"timeline rect: {rect}, point pressed: {pointPressed}, time norm: {timeNorm}, time: {time}");

                    //  m_creator.AudioSource.time = time;
                    SetLocation(time, fallow, snap);
                    //StartCoroutine(UpdateVisualsCoroutine());
                    //IEnumerator UpdateVisualsCoroutine()
                    //{
                    //    yield return null;
                    //    mapEditManager.UpdateVisuals();
                    //}

                }
            })
        );

        //bool tmpFallow = true;
        var rect = mapEditManager.timelineImageCreator.GetComponent<ScrollRect>();

        rect.onValueChanged.AddListener((pos) => { SetLocation(currentTime); });
        SetLocation(0);
    }


    /// <summary>
    /// Unity Update callback. Ensures the seek bar remains on top of other UI elements.
    /// </summary>
    private void Update()
    {
        m_seekBarTrans.SetAsLastSibling();
    }

    /// <summary>
    /// Configure anchors, size and pivot of the internal seek bar RectTransform.
    /// </summary>
    /// <param name="vertical">True if the bar should be vertical.</param>
    /// <param name="barThickness">Pixel thickness for the bar.</param>
    public void SetBarOrientation(bool vertical, float barThickness)
    {
        m_seekBarTrans.anchorMin = new Vector2(vertical ? 0 : .5f, vertical ? .5f : 0);
        m_seekBarTrans.anchorMax = new Vector2(vertical ? 1 : .5f, vertical ? .5f : 1);
        m_seekBarTrans.offsetMax = m_seekBarTrans.offsetMax = Vector2.zero;

        m_seekBarTrans.sizeDelta = new Vector2(vertical ? m_seekBarTrans.sizeDelta.x : barThickness, vertical ? barThickness : m_seekBarTrans.sizeDelta.y);
        m_seekBarTrans.pivot = new Vector2(.5f, .5f);
        var rect = GetComponent<RectTransform>().rect;
        m_seekBarTrans.localPosition = new Vector2(vertical ? rect.center.x : rect.xMin, vertical ? rect.yMin : rect.center.y);
    }

    /// <summary>Determines whether a value falls within an inclusive numeric interval.</summary>
    /// <param name="val">Value to test.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <returns><see langword="true"/> when the value is within the bounds; otherwise, <see langword="false"/>.</returns>
    bool InRange(double val, double min, double max) => val >= min && val <= max;

    /// <summary>
    /// Adjust the timeline scroll position so the provided time remains visible (follows the bar).
    /// </summary>
    /// <param name="time">Time in seconds to make visible.</param>
    public void Fallow(double time)

    {
        var clip = mapEditManager.AudioSource.clip;
        var timebarPos = time / clip.AccurateLength();

        var sr = mapEditManager.timelineImageCreator.GetComponent<ScrollRect>();
        var rt = mapEditManager.timelineImageCreator.GetComponent<RectTransform>();


        var scrollPos = sr.normalizedPosition;
        var viewSizeNorm = rt.rect.size / sr.content.rect.size;//view size in relation to content size


        var tmpPos = (m_vertical ? scrollPos.y : scrollPos.x);

        float fallowPoint = .5f;
        float min = .10f, max = .90f;
        if(!InRange(timebarPos + Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, (float)timebarPos),
            tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * min, tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * max))
        {
            tmpPos = Mathf.Clamp01(((float)timebarPos +
                Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, (float)timebarPos)) -
                (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * fallowPoint);
        }


        sr.normalizedPosition = m_vertical ? new Vector2(0, tmpPos) : new Vector2(tmpPos, 0);


    }

    /// <summary>
    /// Move the seek bar to the specified time and optionally snap or follow the timeline.
    /// </summary>
    /// <param name="time">Target time in seconds.</param>
    /// <param name="fallow">If true, adjust the timeline view to follow the new position.</param>
    /// <param name="snap">If true, snap the time to the nearest beat according to the tempo map.</param>
    public void SetLocation(double time, bool fallow = false, bool snap = false)
    {
        //print($"set location Current Time: {time}");


        var source = mapEditManager.AudioSource;
        var clip = source.clip;

        if(clip.AccurateLength() <= 0) return;
        if(!mapEditManager.timelineImageCreator) throw new NullReferenceException("Timeline Creater was not set");
        if(!m_seekBarTrans) throw new NullReferenceException("Time Bar was not set");

        if(snap)
        {
            var tempo = mapEditManager.MapTrack.tempoMap.GetTempo(time).Clone();
            var timeSig = mapEditManager.MapTrack.tempoMap.GetTimeSig(time).Clone();


            tempo.bpm *= timeSig.note / 4f;//adjust tempo to fit the beat divisions of the time signature
            time = (int)(time * 1000);
            // var offset = (tempo.bpm.spb * .5f);
            var beat = tempo.GetNextBeatMilli((int)time);
            time = (beat - time < time - beat - tempo.bpm.mspb ? beat : (beat - tempo.bpm.mspb));

            time /= 1000;
        }

        var timebarPos = (time) / clip.AccurateLength();

        var rect = GetComponent<RectTransform>().rect;
        var pos = Vector2.Lerp(rect.min, rect.max, (float)timebarPos);

        m_seekBarTrans.localPosition = new Vector2(
            m_vertical ? m_seekBarTrans.localPosition.x : pos.x,
            m_vertical ? pos.y : m_seekBarTrans.localPosition.y);


        if(fallow)
            Fallow(time);

        onSeekBarMoved.Invoke(currentTime = time);

        m_seekBarTrans.transform.SetAsLastSibling();
    }


}
