using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement; 

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

/*
loot boxes
multiple mele weapons
last hit cyoty time
 */


/// <summary>
/// Encapsulates enemy behaviour and hit logic driven by timed NoteData entries.
/// Handles animation, hit detection, and cleanup for enemies.
/// </summary>
public partial class EnemyActions : MonoBehaviour
{
    /// <summary>
    /// Reference to the SongManager used to query audio time and guidance systems.
    /// </summary>
    public SongManager SongManager;
    /// <summary>
    /// The sequence of note hit data that drives this enemy's behavior.
    /// </summary>
    public List<NoteData> noteData;
    /// <summary>
    /// Optional model used to visualize hits.
    /// </summary>
    public GameObject hitModel;
    // public Transform root;
    /// <summary>
    /// Time window (seconds) used to consider hits valid around note timings.
    /// </summary>
    public float hitWindow = 2.1f;
    /// <summary>
    /// Visualizer used to display hit locations and feedback.
    /// </summary>
    public HitLocationVisualizer visualizer;

    [AutoStaticsCleanup] private static List<EnemyActions> AllActionsList { get; } = new List<EnemyActions>();


    public static List<EnemyActions> GetAllActions() => AllActionsList;

    // Awake is called only once before Start

    void Awake()
    {
        AllActionsList.Add(this);
    }

    private void OnDestroy()
    {
        AllActionsList.Remove(this);
    }


    // 0 1
    //2 3 4
    // 5 6

    private int last = 0;
    //public DateTime timer = DateTime.MinValue;
    //public AudioSource clip;
    void NoteUpdate()
    {
        var timings = noteData.Select(v => v.hitTiming).ToList();
        var cTime = SongManager.AudioSource.AccurateTime();
        if(timings == null) return;


        if(last >= timings.Count) return;

        bool check = false;
        for(int count = last; count < timings.Count; ++count)
        {
            var timing = timings[count];

            if(cTime - timing >= hitWindow * 0.5f)
            {
                last = count + 1;
                continue;
            }
            check = true;
            if(cTime - timing >= -hitWindow * 0.5f && cTime < timings[count])
            {
                /*PLACE NOTE LOGIC HERE!!!*/
                switch(noteData[count].hitType)
                {
                case HitType.NONE:
                case HitType.TEST1://melee target
                                   // print("Test1 Triggered");

                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, visualizer, location: noteData[count].hitLocation));
                    //	StartCoroutine(AnimateMeleeTarget(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST2://dodge target
                    //print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, visualizer, location: noteData[count].hitLocation));
                    //  StartCoroutine(AnimateMeleeTarget(timing, hitWindow));
                    //	StartCoroutine(AnimateNote2(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST3://dodge target
                    //print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, visualizer, location: noteData[count].hitLocation));
                    //  StartCoroutine(AnimateMeleeTarget(timing, hitWindow));
                    //	StartCoroutine(AnimateNote3(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST4://dodge target
                    //print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, visualizer, location: noteData[count].hitLocation));
                    //  StartCoroutine(AnimateMeleeTarget(timing, hitWindow));
                    //	StartCoroutine(AnimateNote4(time, time + reactTime, transform.localPosition));

                    break;
                default:
                    break;

                }


                last = count + 1;
            }

        }

        if(!check)
            hitWindow = 0;

        if(last >= timings.Count)//automatic cleanup
            StartCoroutine(OnEnemyEnd(hitWindow * 3f));
    }

    IEnumerator AnimateMeleeTarget(double timing, float window, HitLocationVisualizer visualizer, int location)
    {
        var parent = visualizer.parent ? visualizer.parent : visualizer.transform;
        var obj = Instantiate(visualizer.points[location], parent, false);
        var rend = obj.GetComponentInChildren<Renderer>();

        obj.transform.localScale = Vector3.one * 1.8f;
        rend.material.color = Color.red;

        var lastNotePosition = SongManager.guideTrail.GetLastNotePosition();
        SongManager.guideTrail.onGuideTrailNext.Invoke(lastNotePosition, SongManager.guideTrail.SetLastNotePosition(visualizer.points[location].transform.position), SongManager.AudioSource.AccurateTime(), timing);

        var callback = visualizer.points[location].GetOrAddComponent<HayYouSlappedSomething>();
        callback.enabled = true;
        callback.SongManager = SongManager;
        callback.onObjectHit.AddListener((c) => { StartCoroutine(Cleanup()); });

        var targetScales = new List<Vector3>() { obj.transform.localScale };

        yield return new WaitUntil(() =>
        {
            if(obj == null)
                return true;

            float i = 0;
            var cTime = SongManager.AudioSource.AccurateTime();
            var objRenderer = obj.GetComponentInChildren<Renderer>();

            objRenderer.material.color =
            Color.Lerp(Color.red, Color.green,
            Mathf.Clamp01((i =
            Mathf.InverseLerp((float)(cTime < timing ?
            timing - (window * .5f) : timing + (window * .5f)),
            (float)timing, (float)cTime)) * i));//testing

            // for(int a = 0; a < obj.transform.childCount; ++a)
            obj.transform.localScale
            = Vector3.Lerp(targetScales[0], targetScales[0] * 0.7f, Mathf.Clamp01(i * i));


            //Animation Logic Here!!
            if(cTime >= timing)//on note timing 
            {
                //obj.GetComponentInChildren<Renderer>().material.color = new Color(0, 1, 0);
                StartCoroutine(Cleanup());
            }

            // Any other logic
            if(cTime >= timing + window * .5f)//on completion
                return true;

            return false;
        });

        IEnumerator Cleanup(float time = 0)
        {
            yield return new WaitForSeconds(time);
            Destroy(obj);
            if(lastNotePosition == SongManager.guideTrail.GetLastNotePosition())
                callback.enabled = false;
        }

        yield break;
    }

    IEnumerator AnimateEnemyAttack(float timing, float window)
    {
        var tmp = GetComponentInChildren<Renderer>().material.color;
        GetComponentInChildren<Renderer>().material.color = new Color(1, 1, 0);



        yield return new WaitUntil(() =>
        {
            var cTime = SongManager.AudioSource.AccurateTime();
            //print("Why this no work?");
            //Animation Logic Here!!
            if(cTime >= timing + window * .5)//Player not in danger
                return true;

            return false;
        });



        GetComponentInChildren<Renderer>().material.color = tmp;
        yield break;
    }


    private IEnumerator OnEnemyEnd(float react)
    {
        yield return new WaitForSeconds(react);
        Destroy(gameObject);
        yield break;
    }

    // Update is called once per frame
    void Update()
    {
        //if(timer == DateTime.MinValue)
        //	timer = DateTime.Now;

        NoteUpdate();
    }
}
