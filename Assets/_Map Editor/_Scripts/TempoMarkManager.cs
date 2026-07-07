using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using UnityEngine; 
using UnityEngine.UI;
 

public class TempoMarkManager : MonoBehaviour
{
    public MapEditManager mapEditManager;
    //public TimeSig timeSig = new TimeSig() { beats = 10, note = 4 };

    [SerializeField] private ScrollRect rect;
    [SerializeField] private RectTransform markerParent;
    [SerializeField] private GameObject tmpMarker;
    [SerializeField] private List<GameObject> beatMarkers = new List<GameObject>();



    public bool vertical = true;
    public void SetMarkerOrientation(RectTransform marker, bool vertical, float barThickness, float barWidthP)
    {
        var halfWidth = barWidthP / 2f;
        marker.anchorMin = new Vector2(vertical ? 0.5f - halfWidth : .5f, vertical ? .5f : 0.5f - halfWidth);
        marker.anchorMax = new Vector2(vertical ? 0.5f + halfWidth : .5f, vertical ? .5f : 0.5f + halfWidth);
        marker.offsetMax = marker.offsetMax = Vector2.zero;

        marker.sizeDelta = new Vector2(vertical ? marker.sizeDelta.x : barThickness, vertical ? barThickness : marker.sizeDelta.y * barWidthP);
        marker.pivot = new Vector2(.5f, .5f);
        var rect = GetComponent<RectTransform>().rect;
        marker.localPosition = new Vector2(vertical ? rect.center.x : rect.xMin, vertical ? rect.yMin : rect.center.y);
    }

    public void PlaceMarker(RectTransform marker, double time, float duration, bool vertical, RectTransform content)
    {

        // var clip = m_creator.AudioSource.clip;
        // var source = m_creator.AudioSource;
        var timebarPos = (time / duration);

        while(content.rect.size == Vector2.zero)
            content = content.parent as RectTransform;

        var contentRect = content;
        var pos = Vector2.Lerp(contentRect.rect.min, contentRect.rect.max, (float)timebarPos);

        marker.localPosition = new Vector2(
            vertical ? marker.localPosition.x : pos.x,
            vertical ? pos.y : marker.localPosition.y);

        marker.SetAsLastSibling();
    }

    public void CreateMarker(double time, float widthP = 1)
    {
        //var tempo = mapEditManager.MapTrack.tempoMap.GetTempo(time);
        //
        //var beat = tempo.GetNextBeat(time);

        beatMarkers.Add(Instantiate(tmpMarker, markerParent, false));
        var marker = beatMarkers.Last();
        var markerTrans = marker.GetComponent<RectTransform>();
        var tempoMark = marker.AddComponent<TempoMark>();
        tempoMark.time = time;


        SetMarkerOrientation(marker.GetComponent<RectTransform>(), vertical, 5, widthP);

        PlaceMarker(markerTrans, time, mapEditManager.AudioSource.clip.length, vertical, markerParent != null ? markerParent : rect.content);
    }

    public void MarkerUpdate()
    {
        foreach(var mark in beatMarkers)
            Destroy(mark);
        beatMarkers.Clear();

        var sr = rect.GetComponent<ScrollRect>();
        var rt = rect.GetComponent<RectTransform>();

        var scrollPos = rect.normalizedPosition;

        var viewSizeNorm = rt.sizeDelta / sr.content.sizeDelta;//view size in relation to content size

        var songPosPercent = vertical ?
            scrollPos.y - scrollPos.y * viewSizeNorm.y :
            scrollPos.x - scrollPos.x * viewSizeNorm.x;


        int startTime = (int)(Mathf.Max(0, songPosPercent) * mapEditManager.AudioSource.clip.LengthMilli());
        int endTime = (int)(startTime + viewSizeNorm.y * mapEditManager.AudioSource.clip.LengthMilli());

        while(startTime < endTime)
        {
            var tempo = mapEditManager.MapTrack.tempoMap.GetTempo(startTime / 1000d).Clone();
            var timeSig = mapEditManager.MapTrack.tempoMap.GetTimeSig(startTime / 1000d).Clone();

            var beatsCalc = timeSig.beats * (timeSig.note / 4d);
            tempo.bpm *= timeSig.note / 4f;//adjust tempo to fit the beat divisions of the time signature

            startTime = tempo.GetNextBeatMilli(startTime) - tempo.bpm.mspb;
            var currentBeat = (((tempo.GetNextBeatMilli(startTime) - tempo.bpm.mspb - tempo.TimingMilli) / tempo.bpm.mspb));//needs to be done this way

            float widthP = .8f;
            var beatOfBar = (currentBeat) % (beatsCalc);

            //I'm calling this solved don't look inside
            if(beatOfBar != 0)
            {
                //beatOfBar--;

                int divCount = 0;

                int pow = 2;


                if(beatsCalc % 3 == 0)
                {
                    float check = 0;

                    while((check = Mathf.Pow(pow, ++divCount)) - check * .5 + check * .25 < (beatsCalc)) ;
                }
                else
                {
                    while(Mathf.Pow(pow, ++divCount) < (beatsCalc)) ;
                }

                for(int a = divCount; a > 0; --a)
                {
                    var check = Math.Pow(pow, a);//=2^a 

                    if(check > (beatsCalc / 2))//never change this (stops un-needed bar line division sizes)
                        continue;

                    var checkOfBar = beatOfBar;//not needed but I'll keep it.

                    var calc =
                       (beatsCalc % 3) == 0 ?
                       checkOfBar % (check = (check - check * .5 + check * .25)) :
                       checkOfBar % check;

                    if(calc == 0)
                    {
                        widthP /= Mathf.Clamp((divCount - a) + 1, 2, divCount + 1);
                        break;
                    }
                    else if(a == 1)
                        widthP /= divCount + 2;
                }
            }

            CreateMarker(startTime / 1000d, widthP: widthP);
            var beat = tempo.GetNextBeatMilli(startTime);

            if(beat <= startTime)
                throw new Exception("Beats are repeating");

            startTime = beat;
            //if(currentBeat > beatOfBar) return;
        }

    }


    private void Start()
    {
        rect.onValueChanged.AddListener((v) => MarkerUpdate());
        MarkerUpdate();
    }
}
