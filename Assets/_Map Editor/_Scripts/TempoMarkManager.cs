using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using UnityEngine; 
using UnityEngine.UI;


/// <summary>
/// Manages visual tempo/beat markers inside the map editor UI.
/// Responsible for creating, positioning and updating beat markers based on the audio clip, tempo map and scroll position.
/// </summary>
public class TempoMarkManager : MonoBehaviour
{
    /// <summary>
    /// Reference to the map edit manager which provides access to the audio source and tempo map.
    /// Must be set in the inspector or by another behaviour before using marker creation/update methods.
    /// </summary>
    public MapEditManager mapEditManager;
    //public TimeSig timeSig = new TimeSig() { beats = 10, note = 4 };

    /// <summary>
    /// ScrollRect that contains the timeline/content used to calculate visible time range.
    /// </summary>
    [SerializeField] private ScrollRect rect;
    /// <summary>
    /// Parent RectTransform that holds instantiated marker GameObjects. If null, rect.content is used when placing markers.
    /// </summary>
    [SerializeField] private RectTransform markerParent;
    /// <summary>
    /// Prefab used for generating individual beat marker GameObjects.
    /// </summary>
    [SerializeField] private GameObject tmpMarker;
    /// <summary>
    /// Collection of currently active marker instances. Cleared and re-populated when MarkerUpdate runs.
    /// </summary>
    [SerializeField] private List<GameObject> beatMarkers = new List<GameObject>();



    /// <summary>
    /// Whether the timeline is vertical. Affects marker orientation and placement logic.
    /// </summary>
    public bool vertical = true;
    
    /// <summary>
    /// Configure the anchor, size and pivot of a marker RectTransform based on orientation and thickness.
    /// </summary>
    /// <param name="marker">RectTransform of the marker to configure.</param>
    /// <param name="vertical">True for vertical timeline, false for horizontal.</param>
    /// <param name="barThickness">Pixel thickness of the visual bar when applied to the marker.</param>
    /// <param name="barWidthP">Normalized width multiplier (0..1) for the marker relative to parent.</param>
    public void SetMarkerOrientation(RectTransform marker, bool vertical, float barThickness, float barWidthP)
    {
        var halfWidth = barWidthP / 2f;
        // Set anchors so the marker sits centered along the cross axis and spans on the primary axis according to barWidthP
        marker.anchorMin = new Vector2(vertical ? 0.5f - halfWidth : .5f, vertical ? .5f : 0.5f - halfWidth);
        marker.anchorMax = new Vector2(vertical ? 0.5f + halfWidth : .5f, vertical ? .5f : 0.5f + halfWidth);
        // Reset offsets to zero so anchors fully control the rect
        marker.offsetMax = marker.offsetMax = Vector2.zero;

        // Determine sizeDelta depending on orientation: primary axis uses barThickness
        marker.sizeDelta = new Vector2(vertical ? marker.sizeDelta.x : barThickness, vertical ? barThickness : marker.sizeDelta.y * barWidthP);
        marker.pivot = new Vector2(.5f, .5f);
        var rect = GetComponent<RectTransform>().rect;
        // Align the marker to the edge or center depending on orientation
        marker.localPosition = new Vector2(vertical ? rect.center.x : rect.xMin, vertical ? rect.yMin : rect.center.y);
    }

    /// <summary>
    /// Place a marker in the content RectTransform corresponding to a time position inside an audio duration.
    /// </summary>
    /// <param name="marker">The marker RectTransform to move.</param>
    /// <param name="time">Time in seconds where the marker should be placed.</param>
    /// <param name="duration">Total duration of the audio (seconds) used to normalize position.</param>
    /// <param name="vertical">Whether the timeline is vertical; controls which axis to update.</param>
    /// <param name="content">The content RectTransform representing the timeline area. If the content rect is empty, the parent chain is used.</param>
    public void PlaceMarker(RectTransform marker, double time, float duration, bool vertical, RectTransform content)
    {

        // Compute normalized position (0..1) along the timeline based on time and duration
        var timebarPos = (time / duration);

        // If the passed content rect has zero size, walk up the parent chain until a valid rect is found
        while(content.rect.size == Vector2.zero)
            content = content.parent as RectTransform;

        var contentRect = content;
        // Lerp between min and max corners to get the local position inside the content rect
        var pos = Vector2.Lerp(contentRect.rect.min, contentRect.rect.max, (float)timebarPos);

        // Update only the axis appropriate for the orientation
        marker.localPosition = new Vector2(
            vertical ? marker.localPosition.x : pos.x,
            vertical ? pos.y : marker.localPosition.y);

        // Ensure this marker renders above others (last sibling)
        marker.SetAsLastSibling();
    }

    /// <summary>
    /// Instantiate a marker GameObject and set its tempo/time data and transform.
    /// </summary>
    /// <param name="time">Time in seconds for the marker's tempo event.</param>
    /// <param name="widthP">Optional normalized width multiplier for the marker visual (defaults to 1).</param>
    public void CreateMarker(double time, float widthP = 1)
    {
        // Instantiate marker prefab under markerParent and track the instance
        beatMarkers.Add(Instantiate(tmpMarker, markerParent, false));
        var marker = beatMarkers.Last();
        var markerTrans = marker.GetComponent<RectTransform>();
        var tempoMark = marker.AddComponent<TempoMark>();
        tempoMark.time = time;


        // Configure visual orientation/size and place the marker in the timeline
        SetMarkerOrientation(marker.GetComponent<RectTransform>(), vertical, 5, widthP);

        PlaceMarker(markerTrans, time, mapEditManager.AudioSource.clip.length, vertical, markerParent != null ? markerParent : rect.content);
    }

    /// <summary>
    /// Rebuilds the visible marker set for the current scroll position. This clears existing markers and
    /// creates markers for every beat (and subdivisions) that appear within the current view window.
    /// </summary>
    public void MarkerUpdate()
    {
        // Remove existing markers
        foreach(var mark in beatMarkers)
            Destroy(mark);
        beatMarkers.Clear();

        var sr = rect.GetComponent<ScrollRect>();
        var rt = rect.GetComponent<RectTransform>();

        var scrollPos = rect.normalizedPosition;

        // View size relative to the content size (normalized)
        var viewSizeNorm = rt.rect.size / sr.content.rect.size;//view size in relation to content size
        viewSizeNorm.y =MathF.Abs(viewSizeNorm.y);

        // Calculate the normalized start percent of the visible range depending on orientation
        var songPosPercent = vertical ?
            scrollPos.y - scrollPos.y * viewSizeNorm.y :
            scrollPos.x - scrollPos.x * viewSizeNorm.x;


        int startTime = (int)(Mathf.Max(0, songPosPercent) * mapEditManager.AudioSource.clip.AccurateLengthMilli());
        int endTime = (int)(startTime + viewSizeNorm.y * mapEditManager.AudioSource.clip.AccurateLengthMilli());


        //Toast.SendToast($"Start Time: {startTime} End Time: {endTime}");
        //Toast.SendToast($"View Size Norm: {viewSizeNorm} Scroll Pos: {scrollPos} RT Size: {rt.rect.size} Content Size: {sr.content.sizeDelta}");

        // Iterate through beats in the visible millisecond range and create markers
        while(startTime < endTime)
        {
            var tempo = mapEditManager.MapTrack.tempoMap.GetTempo(startTime / 1000d).Clone();
            var timeSig = mapEditManager.MapTrack.tempoMap.GetTimeSig(startTime / 1000d).Clone();

            var beatsCalc = timeSig.beats * (timeSig.note / 4d);
            // Adjust BPM to match the division used for beat subdivision calculations
            tempo.bpm *= timeSig.note / 4f;//adjust tempo to fit the beat divisions of the time signature

            // Position startTime to just before the next beat so calculations below resolve the current beat index
            startTime = tempo.GetNextBeatMilli(startTime) - tempo.bpm.mspb;
            var currentBeat = (((tempo.GetNextBeatMilli(startTime) - tempo.bpm.mspb - tempo.TimingMilli) / tempo.bpm.mspb));//needs to be done this way

            float widthP = .8f;
            var beatOfBar = (currentBeat) % (beatsCalc);

            // Determine visual subdivision size based on beats per bar
            if(beatOfBar != 0)
            {
                int divCount = 0;

                int pow = 2;

                if(beatsCalc % 3 == 0)
                {
                    float check = 0;

                    // For compound meters (divisible by 3) use modified check progression.
                    while((check = Mathf.Pow(pow, ++divCount)) - check * .5 + check * .25 < (beatsCalc)) ;
                }
                else
                {
                    // For simple meters use binary subdivision progression.
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

            // Defensive check: ensure GetNextBeat advanced in time, otherwise throw a descriptive exception.
            if(beat <= startTime)
                throw new DataException("Tempo/GetNextBeat produced non-advancing time");

            // Advance to next beat to avoid infinite loops.
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
