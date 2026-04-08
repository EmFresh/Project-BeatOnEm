using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "ScriptableObjects/SongTrack")]
public class SongTrack : ScriptableObject
{
    public List<GameObject> enemyPrefabs;
    public List<GameObject> notePrefabs;

    public List<HitLocations> hitLocationsList;
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

        if(tempoMap == null)
            tempoMap = new TempoMap();
        tempoMap.Init();
    }

    public BeatData  GetEnemy(int lane, double time)
    {
        var enemies = GetEnemies(time);

        foreach(var enemy in enemies)
            if(enemy.laneIndex == lane)
                return enemy;


        return null;
    }

    public List<BeatData> GetEnemies(double time)
    {
        return beats?.FindAll(beat => time >= beat.startTime && time <= beat.endTime);
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
