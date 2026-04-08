using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using Unity.Mathematics;
/*
loot boxes
multiple mele weapons
last hit cyoty time
 */

public class EnemyActions : MonoBehaviour
{
    public List<NoteData> noteData;
    public GameObject hitModel;
    // public Transform root;
    public float hitWindow = 2.1f;
    private static List<EnemyActions> allActions = new List<EnemyActions>();
    public  HitLocations points = new HitLocations();

    // Awake is called only once before Start
    void Awake()
    {
        //Add any initial animation code here!!(NVM DONT DO THAT)
        allActions.Add(this);


    }

    private void OnDestroy()
    {
        allActions.Remove(this);
    }


    // * *
    //* * *
    // * *

    private int last = 0;
    //public DateTime timer = DateTime.MinValue;
    public AudioSource clip;
    void NoteUpdate()
    {
        var timings = noteData.Select(v=>v.hitTiming).ToList();
        var cTime = clip.time;
        if(timings == null) return;


        if(last >= timings.Count()) return;

        bool check = false;
        for(int count = last; count < timings.Count(); ++count)
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
                switch(noteData.ElementAt(count).hitType)
                {
                case HitType.TEST1://melee target
                    print("Test1 Triggered");

                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, points, location: noteData.ElementAt(count).hitLocation));
                    //	StartCoroutine(AnimateMeleeTarget(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST2://dodge target
                    print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, points, location: noteData.ElementAt(count).hitLocation));
                    //  StartCoroutine(AnimateMeleeTarget(timing, hitWindow));
                    //	StartCoroutine(AnimateNote2(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST3://dodge target
                    print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, points, location: noteData.ElementAt(count).hitLocation));
                    //  StartCoroutine(AnimateMeleeTarget(timing, hitWindow));
                    //	StartCoroutine(AnimateNote3(time, time + reactTime, transform.localPosition));

                    break;
                case HitType.TEST4://dodge target
                    print("Test2 Triggered");
                    StartCoroutine(AnimateMeleeTarget(timing, hitWindow, points, location: noteData.ElementAt(count).hitLocation));
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

        if(last >= timings.Count())//automatic cleanup
            StartCoroutine(OnEnemyEnd(hitWindow * 3f));
    }

    IEnumerator AnimateMeleeTarget(double timing, float window, HitLocations points, int location)
    {
        var obj = points.CreateHitpointObject(hitModel, location, this.transform);
        obj.transform.localScale = Vector3.one*1.8f ;


        //obj.transform.GetChild(0).localPosition += Vector3.zero;
        obj.GetComponentInChildren<Renderer>().material.color = new Color(1, 0, 0);
        IEnumerator Cleanup()
        {
            yield return new WaitForSeconds(window);
            Destroy(obj);
        }

        var callback = obj.AddComponent<HayYouSlappedSomthing>();

        callback.onObjectHit.AddListener(() => { Destroy(obj); });

        var targetScales = new List<Vector3>();
        for(int a = 0; a < obj.transform.childCount; ++a)
            targetScales.Add(obj.transform.GetChild(a).localScale);

        yield return new WaitUntil(() =>
        {
            float i = 0;
            obj.GetComponentInChildren<Renderer>().material.color =
            Color.Lerp(Color.red, Color.green,
            Mathf.Clamp((i =
            Mathf.InverseLerp((float)(clip.time < timing ?
            timing - (window * .5f) : timing + (window * .5f)),
            (float)timing, clip.time)) * i, 0, 1));//testing

            for(int a = 0; a < obj.transform.childCount; ++a)
                obj.transform.GetChild(a).localScale
                = Vector3.Lerp(targetScales[a], targetScales[a] * 0.7f, Mathf.Clamp(i * i, 0, 1));


            //Animation Logic Here!!
            if(clip.time >= timing)//on note timing 
            {
                //obj.GetComponentInChildren<Renderer>().material.color = new Color(0, 1, 0);
                StartCoroutine(Cleanup());
            }
            // Any other logic

            if(clip.time >= timing + window * .5f)//on completion
                return true;

            return false;


        });

        yield break;
    }

    IEnumerator AnimateEnemyAttack(float timing, float window)
    {
        var tmp = GetComponentInChildren<Renderer>().material.color;
        GetComponentInChildren<Renderer>().material.color = new Color(1, 1, 0);



        yield return new WaitUntil(() =>
        {
            //print("Why this no work?");
            //Animation Logic Here!!
            if(clip.time >= timing + window * .5)//Player not in danger
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
