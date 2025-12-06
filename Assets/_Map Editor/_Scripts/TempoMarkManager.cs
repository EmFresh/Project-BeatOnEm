using System.Collections.Generic;
using System.Data;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class TempoMarkManager : MonoBehaviour
{
    public MapEditManager mapEditManager; 
    public TimeSig timeSig = new TimeSig() { beats = 10, note = 4 };

    [SerializeField] private ScrollRect rect;
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

    public void PlaceMarker(RectTransform marker, float time, float diration, bool vertical, RectTransform content)
    {

        // var clip = m_creator.audioSource.clip;
        // var source = m_creator.audioSource;
        var timebarPos = (time / diration);

        var contentRect = rect.content;
        var pos = Vector2.Lerp(contentRect.rect.min, contentRect.rect.max, timebarPos);

        marker.localPosition = new Vector2(
            vertical ? marker.localPosition.x : pos.x,
            vertical ? pos.y : marker.localPosition.y);

        marker.SetAsLastSibling();
    }

    public void CreateMarker(float time, float widthP = 1)
    {
        var tempo = mapEditManager.mapTrack.tempoMap.GetTempo(time);

        var beat = tempo.GetNextBeat(time);

        beatMarkers.Add(Instantiate(tmpMarker, rect.content, false));
        var marker = beatMarkers.Last();
        var markerTrans = marker.GetComponent<RectTransform>();
        var tempoMark = marker.AddComponent<TempoMark>();
        tempoMark.time = time; 
    

        SetMarkerOrientation(marker.GetComponent<RectTransform>(), vertical, 5, widthP);

        PlaceMarker(markerTrans, time, mapEditManager.audioSource.clip.length, vertical, rect.content);
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

        var songPosPercent = scrollPos.y - scrollPos.y * viewSizeNorm.y;



        var startTime = Mathf.Max(0, songPosPercent) * mapEditManager.audioSource.clip.length;
        var endTime = startTime + viewSizeNorm.y * mapEditManager.audioSource.clip.length;


        
        var divisions = timeSig.beats / timeSig.note;
        while(startTime < endTime)
        {
            var tempo = mapEditManager.mapTrack.tempoMap.GetTempo(startTime);
            var timeSigneture = mapEditManager.mapTrack.tempoMap.GetTimeSig(startTime);
            var beats = timeSigneture.beats / timeSigneture.note * timeSigneture.beats;

            tempo = tempo.Clone();
            tempo.bpm *= divisions;
            var currentBeat = (int)((startTime - tempo.timing) / tempo.bpm.spb);

            float widthP = .8f;
            //int period = 6;
            var beatOfBar = (currentBeat) % (timeSig.beats);

            if(beatOfBar != 0)
            {
                int divCount = 0;

                int pow = 2;

                // if(timesig % 5 == 0)
                // {
                //     float check = 0;
                //     while(-(check = Mathf.Pow(pow, divCount)) + check * .5f - (divCount - 1) < timesig) ;
                // }
                // else
                if(timeSig.beats % 3 == 0)
                {
                    float check = 0;
                    float val = 0;

                    while((check = Mathf.Pow(pow, ++divCount)) - check * .5 + check * .25 < (timeSig.beats / 2)) ;
                    //divCount--;
                }
                else
                    while(Mathf.Pow(pow, ++divCount) < (timeSig.beats / 2)) ;

                // divCount -= pow == 3 ? 1 : 0;

                for(int a = divCount; a > 0; --a)
                {
                    var check = Mathf.Pow(pow, a);//=
                    float calc =

                        (timeSig.beats % 3) == 0 ?
                        beatOfBar % (check = (float)(check - check * .5 + check * .25)) :
                        beatOfBar % check;


                    if(check > (timeSig.beats / 2))
                        continue;


                    if(calc == 0)
                    {
                        widthP /= (divCount - a) + 1;
                        break;
                    }
                    else if(a == 1)
                        widthP /= divCount + 2;
                }

            }

            CreateMarker(startTime, widthP: widthP);
            var beat = tempo.GetNextBeat(startTime);
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
