using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/SongTrack")]
public class SongTrack : ScriptableObject
{

    public List<GameObject> enemyPrefabs;
    public List<GameObject> notePrefabs;
    public List<HitPoints> hitPoints; //hit points for the enemies

    public List<BeatData> beats;
    public TempoMap tempoMap;

    private void OnEnable()
    {
        if(enemyPrefabs == null)
            enemyPrefabs = new List<GameObject>();
        if(notePrefabs == null)
            notePrefabs = new List<GameObject>();
        if(beats == null)
            beats = new List<BeatData>();
       // if(tempoMap == null)
       //     tempoMap = CreateInstance<TempoMap>();
    }

    void init()
    {
        //tempo.init(beats);
    }


    void clear()
    {
        enemyPrefabs?.Clear();
        notePrefabs?.Clear();
        beats?.Clear();
        tempoMap?.Clear();
    }
}
