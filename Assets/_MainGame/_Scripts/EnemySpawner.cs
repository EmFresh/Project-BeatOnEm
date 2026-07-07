using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI.Extensions;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] SongManager songManager;
    [SerializeField] string dataFile;
    public Transform parentObj;
    public List<LanePoint> lanePoint;
    public float reactTime = 2.1f;

    [HideInInspector]
    public WorldControl mapMover;

    private void Awake()
    {
        songManager.MapTrack.Copy(((ISongManager)songManager).LoadFile(dataFile), excludePrefabs: true);
        var check = songManager.AudioSource?.isPlaying ?? false;
        mapMover = FindAnyObjectByType<WorldControl>();

        if(check)
            songManager.AudioSource?.Pause();
        if(check)
            songManager.AudioSource?.Play();


        last = 0;
    }

    int last = 0, texNum = 0;
    bool init = true;
    List<(double, BeatData)> timings = new List<(double, BeatData)>();
    private void SpawnUpdate()
    {
        if(!songManager.MapTrack) return;

        if(init)
        {
            songManager.MapTrack.beats.Sort((a, b) => { return a?.spawnTime.CompareTo(b?.spawnTime) ?? 1; });
            foreach(var beat in songManager.MapTrack.beats)
                if((beat?.spawnTime ?? float.PositiveInfinity) != float.PositiveInfinity)
                    timings.Add((beat.startTime, beat));

            init = false;
        }

        GameObject createEnemy(int index)
        {
            if(index < 0) index = 0;
            if(index >= songManager.MapTrack.enemyPrefabs.Count) return null;

            GameObject obj = null;
            obj = Instantiate(songManager.MapTrack.enemyPrefabs[index], parentObj, false);
            obj.transform.localPosition = new(-1.15f + 1.15f * index, 0, 5/*start location*/ + reactTime * (mapMover.speed));
            obj.transform.localRotation *= Quaternion.Euler(0, 180, 0);
            return obj;
        }

        for(int count = last; count < timings.Count; ++count)
        {
            var time = timings[count];

            //if((time?.Item2 ?? null) == null) continue;

            if(songManager.AudioSource.AccurateTime() - time.Item1 >= reactTime) // top bound for enemy
                break;


            if(songManager.AudioSource.AccurateTime() - time.Item1 >= -reactTime) // within bound
            {
                //	print("created Object!!");

                //var obj = Instantiate(track.enemyPrefabs[0], new Vector3(0, 0, time.Item1), Quaternion.Euler(0, 180, 0), parentObj);

                GameObject obj = null;
                var notespace = parentObj.transform.GetChild(0).GetComponent<Collider>().bounds.extents.x / 2;


                obj = createEnemy(time.Item2.enemyIndex);


                if(obj == null) continue;
                //	obj.transform.localPosition -= (Vector3)(Vector2)obj.GetComponentInChildren<MeshFilter>().mesh.bounds.min;

                var ctrl = obj.GetOrAddComponent<EnemyControl>();

                float quarterTimeBias = 0.05f;//a short time before the notes get to the enemy
                ctrl.Init(mapMover, lanePoint[time.Item2.laneIndex], songManager.AudioSource, Math.Min(songManager.AudioSource.AccurateTime(), time.Item1) + .05f, time.Item1 - quarterTimeBias);

                var act = obj.GetOrAddComponent<EnemyActions>();
                act.visualizer = obj.GetOrAddComponent<HitLocationVisualizer>();

                act.visualizer.point = songManager.MapTrack.notePrefabs[0];
                act.visualizer.hitLocations = songManager.MapTrack.hitLocationsList[time.Item2.hitLocationsIndex];
                act.visualizer.SetColour3D(new Color(1, 1, 1, 0.2f));

                act.noteData = time.Item2.noteDataList;
                act.hitWindow = reactTime * .33f;
                act.clip = songManager.AudioSource;



                last = count + 1;
            }
        }
    }

    //DateTime timer = DateTime.MinValue; 
    void Update()
    {
        //if(!clip?.isPlaying ?? false)
        //	clip?.Play();

        //	var tmp = track.tempo.GetBeatMeasure(clip.time);
        //	print(tmp);

        SpawnUpdate();
    }
}
