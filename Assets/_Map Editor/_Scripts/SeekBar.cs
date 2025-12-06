using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Linq;
[RequireComponent(typeof(RectTransform), typeof(EventTrigger))]
public class SeekBar : MonoBehaviour
{

    /*
     Beginner
     Easy
     Advanced
     Trojan
    */


    public MapEditManager mapEditManager;

    [SerializeField] RectTransform m_seekBarTrans;
    [SerializeField] float m_thickness = 5;
    [SerializeField] bool m_vertical = true;
    [field: SerializeField] public bool snap { get; set; } = true;
    [field: SerializeField] public bool fallow { get; set; } = true;
    public float currentTime = -1;

    bool vertical { get => m_vertical; set { SetBarOrientation(m_vertical = value, m_thickness); } }
    float thickness { get => m_thickness; set { SetBarOrientation(m_vertical, m_thickness = value); } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
                    var pointPressed = ped.pointerPressRaycast.worldPosition;
                    pointPressed = GetComponent<RectTransform>().InverseTransformPoint(pointPressed);
                    var rect = GetComponent<RectTransform>().rect;
                    float timeNorm = m_vertical ?
                        (pointPressed.y - rect.yMin) / rect.height :
                        (pointPressed.x - rect.xMin) / rect.width;
                    var clip = mapEditManager.audioSource.clip;
                    float time = timeNorm * clip.length;

                    if(snap)
                    {
                        var tempo = mapEditManager.mapTrack.tempoMap.GetTempo(time).Clone();
                        var timeSig = mapEditManager.tempoMarkManager.timeSig;
                        tempo.bpm *= timeSig.beats / timeSig.note;

                        var beat = tempo.GetNextBeat(time);
                        time = beat - time < time - beat - tempo.bpm.spb ? beat : beat - tempo.bpm.spb;
                    }

                    //  m_creator.audioSource.time = time;
                    SetLocation(time, fallow);
                }
            })
        );

        bool tmpFallow = true;
        var rect = mapEditManager.timelineImageCreator.GetComponent<ScrollRect>();
        
       rect.onValueChanged.AddListener((pos) => { SetLocation(currentTime, false); });

    }


    private void Update()
    {
        m_seekBarTrans.SetAsLastSibling();
    }
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
        float min = .25f, max = .75f;
        if(!InRange(timebarPos + Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, timebarPos),
            tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * min, tmpPos + (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * max))
        {
            tmpPos = Mathf.Clamp01((timebarPos +
                Mathf.Lerp(0, m_vertical ? viewSizeNorm.y : viewSizeNorm.x, timebarPos)) -
                (m_vertical ? viewSizeNorm.y : viewSizeNorm.x) * fallowPoint);
        }


        sr.normalizedPosition = m_vertical ? new Vector2(0, tmpPos) : new Vector2(tmpPos, 0);


    }

    public void SetLocation(float time, bool fallow = false)
    {
        if(!mapEditManager.timelineImageCreator) throw new NullReferenceException("Timeline Creater was not set");
        if(!m_seekBarTrans) throw new NullReferenceException("Time Bar was not set");

        var source = mapEditManager.audioSource;
        var clip = source.clip;
        var timebarPos = time / clip.length;

        var rect = GetComponent<RectTransform>().rect;
        var pos = Vector2.Lerp(rect.min, rect.max, timebarPos);

        m_seekBarTrans.localPosition = new Vector2(
            m_vertical ? m_seekBarTrans.localPosition.x : pos.x,
            m_vertical ? pos.y : m_seekBarTrans.localPosition.y);

        if(fallow)
            Fallow(time);

        m_seekBarTrans.transform.SetAsLastSibling();

        currentTime = time;
    }
}
