using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

using UnityEngine;

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
[CreateAssetMenu(menuName = "ScriptableObjects/SongTrack")]
/// <summary>
/// ScriptableObject that stores song metadata, beats, tempo map and prefab references for a level.
/// </summary>
public partial class SongTrack : ScriptableObject
{
    /// <summary>
    /// Human-readable song name.
    /// </summary>
    [SerializeField]private string _songName;
    [SerializeField]private string _artist;
    [SerializeField]private string _mapper;
    public string songName { get => _songName; set => _songName = value; }
    public string artist { get => _artist; set => _artist = value; }
    public string mapper { get => _mapper; set => _mapper = value; }

    public Version version { get; } = new Version(1, 0);

    [IgnoreMember]
    public List<GameObject> enemyPrefabs;
    [IgnoreMember]
    public List<GameObject> notePrefabs;

    public List<HitLocations> hitLocationsList;
    public List<BeatData> beats;

    public TempoMap tempoMap;

    /// <summary>
    /// Ensures internal collections and the tempo map are initialized when the asset is enabled.
    /// </summary>

    private void OnEnable()
    {
        Init();
    }

    public BeatData GetEnemy(int lane, double time)
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




    public void Init()
    {
        if(enemyPrefabs == null)
            enemyPrefabs = new List<GameObject>();
        if(notePrefabs == null)
            notePrefabs = new List<GameObject>();
        if(hitLocationsList == null)
            hitLocationsList = new List<HitLocations>();
        if(beats == null)
            beats = new List<BeatData>();

        if(tempoMap == null)
            tempoMap = new TempoMap();

        tempoMap.Init();
    }


    public void clear()
    {
        enemyPrefabs?.Clear();
        notePrefabs?.Clear();
        hitLocationsList?.Clear();
        beats?.Clear();
        tempoMap?.Clear();
    }

    public void Copy(SongTrack other, bool excludePrefabs = false)
    {
        if(!excludePrefabs)
        {
            enemyPrefabs = other.enemyPrefabs.ToList();
            notePrefabs = other.notePrefabs.ToList();
        }
        hitLocationsList = other.hitLocationsList.ToList();
        beats = other.beats.ToList();
        tempoMap = other.tempoMap.Clone();
    }
}
