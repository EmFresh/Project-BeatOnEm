using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.TextCore.Text;
using UnityEngine.UI.Extensions;

using static EditorSettings;
/// <summary>
/// Spawns enemy GameObjects based on the SongTrack beat data and manages their initialization.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] SongManager songManager;
    [SerializeField] EditorSettings editorSettings;

    /// <summary>
    /// Reference to the SongManager that provides song data and audio.
    /// </summary>
    public Transform parentObj;
    /// <summary>
    /// List of lane points used for spawning enemies into specific lanes.
    /// </summary>
    public List<LanePoint> lanePoint;
    /// <summary>
    /// How early (seconds) enemies appear before their target time.
    /// </summary>
    public float reactTime = 2.1f;

    [HideInInspector]
    public WorldControl mapMover;

    /// <summary>
    /// Load the current song track (excluding prefabs) and start audio playback if present.
    /// Runs once when the component awakens.
    /// </summary>
    private async void Awake()
    {

        await ((ISongManager)songManager).LoadFile(Path.Combine(editorSettings.FileDir, editorSettings.FileName + EXTENSION), excludePrefabs: true);
        //songManager.MapTrack.Copy(loadedTrack, excludePrefabs: true);

        var check = songManager.AudioSource?.isPlaying ?? false;
        mapMover = FindAnyObjectByType<WorldControl>();

       // if(check)
       //     songManager.AudioSource?.Pause();
       // if(check)
            songManager.AudioSource?.Play();


        last = 0;
    }

    /// <summary>
    /// Performs startup initialization and begins helper coroutines.
    /// </summary>
    private void Start()
    {
        StartCoroutine(printer());
        IEnumerator printer()
        {
            for(int a = 0; a < 50; ++a)
                yield return null;

            FindAnyObjectByType<WorldScale>().Rescale(FindAnyObjectByType<Camera>().GetComponent<TrackedPoseDriver>()?.positionAction.ReadValue<Vector3>().y ?? 1, 1);

            print($"controls: {string.Join(", ", FindAnyObjectByType<TrackedPoseDriver>(FindObjectsInactive.Include).positionAction.activeControl?.device.allControls.Select(ctrl => ctrl.name) ?? new List<string>())}");
        }   
    }

    int last = 0, texNum = 0;
    bool init = true;
    List<(double, BeatData)> timings = new List<(double, BeatData)>();
    /// <summary>
    /// Internal spawn update loop that instantiates enemies when their spawn window is reached.
    /// Called from Update().
    /// </summary>
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
                act.SongManager = songManager;

                act.visualizer.point = songManager.MapTrack.notePrefabs[0];
                act.visualizer.hitLocations = songManager.MapTrack.hitLocationsList[time.Item2.hitLocationsIndex];
                act.visualizer.SetColour3D(new Color(1, 1, 1, 0.2f));

                act.noteData = time.Item2.noteDataList;
                act.hitWindow = reactTime * .33f;


                last = count + 1;
            }
        }
    }

    //DateTime timer = DateTime.MinValue; 
    /// <summary>
    /// Unity update method invoked every frame. Drives the SpawnUpdate logic.
    /// </summary>
    void Update()
    {
        //if(!clip?.isPlaying ?? false)
        //	clip?.Play();

        //	var tmp = track.tempo.GetBeatMeasure(clip.time);
        //	print(tmp);

        SpawnUpdate();
    }
}
