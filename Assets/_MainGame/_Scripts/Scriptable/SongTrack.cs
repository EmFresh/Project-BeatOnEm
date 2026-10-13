/* File: SongTrack.cs
   Brief: Data structure representing a song track used by the editor and runtime.
   Holds metadata and per-track note lists. */
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
    /// <summary>Backing value for <see cref="artist"/>.</summary>
    [SerializeField]private string _artist;
    /// <summary>Backing value for <see cref="mapper"/>.</summary>
    [SerializeField]private string _mapper;
    /// <summary>Gets or sets the song's display name.</summary>
    public string songName { get => _songName; set => _songName = value; }
    /// <summary>Gets or sets the artist name.</summary>
    public string artist { get => _artist; set => _artist = value; }
    /// <summary>Gets or sets the mapper name.</summary>
    public string mapper { get => _mapper; set => _mapper = value; }

    /// <summary>Gets the data format version for this track.</summary>
    public Version version { get; } = new Version(1, 0);

    [IgnoreMember]
    /// <summary>Enemy prefabs available to this song track.</summary>
    public List<GameObject> enemyPrefabs;
    [IgnoreMember]
    /// <summary>Note prefabs available to this song track.</summary>
    public List<GameObject> notePrefabs;

    /// <summary>Hit-location definitions referenced by beat sections.</summary>
    public List<HitLocations> hitLocationsList;
    /// <summary>Beat and enemy sections contained in the song.</summary>
    public List<BeatData> beats;

    /// <summary>Tempo and time-signature changes for the song.</summary>
    public TempoMap tempoMap;

    /// <summary>
    /// Unity callback invoked when the ScriptableObject is enabled. Ensures internal collections are initialized.
    /// </summary>
    private void OnEnable()
    {
        Init();
    }

    /// <summary>
    /// Find an enemy BeatData within the specified time window that matches the given lane index.
    /// </summary>
    /// <param name="lane">Lane index to match.</param>
    /// <param name="time">Time in seconds to search within.</param>
    /// <returns>The matching BeatData or null if none found.</returns>
    public BeatData GetEnemy(int lane, double time)
    {
        var enemies = GetEnemies(time);

        foreach(var enemy in enemies)
            if(enemy.laneIndex == lane)
                return enemy;


        return null;
    }

    /// <summary>
    /// Get all BeatData entries that are active at the specified time.
    /// </summary>
    /// <param name="time">Time in seconds to query.</param>
    /// <returns>List of BeatData active at the given time.</returns>
    public List<BeatData> GetEnemies(double time)
    {
        return beats?.FindAll(beat => time >= beat.startTime && time <= beat.endTime);
    }




    /// <summary>
    /// Initialize internal collections and ensure the tempo map exists.
    /// </summary>
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


    /// <summary>
    /// Clear all collections and tempo map data from this SongTrack.
    /// </summary>
    public void clear()
    {
        enemyPrefabs?.Clear();
        notePrefabs?.Clear();
        hitLocationsList?.Clear();
        beats?.Clear();
        tempoMap?.Clear();
    }

    /// <summary>
    /// Copy data from another SongTrack instance into this one.
    /// </summary>
    /// <param name="other">Source SongTrack to copy from.</param>
    /// <param name="excludePrefabs">If true, prefab lists are not copied.</param>
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
