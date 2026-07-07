using System;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

using UnityEngine;

//Ill organize this later
/*
 what the song data needs to handle (*.beatz format): 
TOD:
* spawning will take into account the note timing (for the first note occurrence per enemy) and an initial buffer time for each enemy (how early the enemy shows up).
* enemies can show up in 3 - 5 preset areas that are identified either in a script or an extra text file.
* notes can show up in predefined areas on the body and should be adjustable by model (can be easily added or removed from a model)
* enemies can either stop the player from moving or can pass by the player and only need to be dodged / hit once
* background objects can also be spawned on a different midi track (most likely an index to a list of objects)
 Done:
*/



[Serializable]
public enum HitType : int
{
    NONE,
    TEST1,
    TEST2,
    TEST3,
    TEST4,
    IMAGE,
}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
public struct NoteData
{
    //note stuff

    /// <summary>
    /// places the enemy can be hit (actual locations will be objects on enemy)
    /// </summary>
    public int hitLocation;

    /// <summary>
    /// the timing of each hit location
    /// </summary>
    public double hitTiming;

    /// <summary>
    /// how the target should be hit (will make it a single value later)
    /// </summary>
    public HitType hitType;


    public NoteData Clone() => new NoteData() { hitLocation = hitLocation, hitTiming = hitTiming, hitType = hitType };
}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
public partial class BeatData
{

    //public SongTrack track = null;

    public List<NoteData> noteDataList = new List<NoteData>();
    public int hitLocationsIndex = -1; //locations where you can hit the enemies

    public int laneIndex = -1;
    public int enemyIndex = -1;
    public double startTime = 0, endTime = 0;

    public void AddNote(double hitTiming, int hitLocation, HitType hitType, float timePad = 0)
    {
        if(noteDataList.Any((note) =>
         InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad) &&
        note.hitLocation == hitLocation &&
        note.hitType == hitType)) return;

        noteDataList.Add(new NoteData() { hitLocation = hitLocation, hitTiming = hitTiming, hitType = hitType });
    }

    public void RemoveNote(double hitTiming, int hitLocation, HitType hitType, float timePad = 0)
    {
        noteDataList.RemoveAll((note) => InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad) && note.hitLocation == hitLocation && note.hitType == hitType);
    }

    public NoteData GetNote(double hitTiming, float TimePad = 0, int hitLocation = -1, HitType hitType = (HitType)(-1)) =>
        noteDataList.FirstOrDefault((note) =>
        InRange(hitTiming, note.hitTiming - TimePad, note.hitTiming + TimePad) &&
        note.hitLocation == (hitLocation == -1 ? note.hitLocation : hitLocation) &&
        note.hitType == (hitType == (hitType) - 1 ? note.hitType : hitType));


    public List<NoteData> GetNotes(double hitTiming, float timePad) =>
         noteDataList?.Where((note) => InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad))?.ToList() ?? new List<NoteData>();

    public BeatData Clone() => new BeatData()
    {
        noteDataList = noteDataList.Select(note => note.Clone()).ToList(),
        hitLocationsIndex = hitLocationsIndex,
        laneIndex = laneIndex,
        startTime = startTime,
        endTime = endTime,
        SectionName = SectionName,
        spawnLocation = spawnLocation + Vector3.zero,
        _spawnTimeOffset = _spawnTimeOffset,
        //track = track,
    };

    bool InRange(double val, double min, double max) => val >= min && val <= max;


    //spawn stuff

    /// <summary>
    /// what a song section name is called. can be used for some sort of 
    /// results screen later (and organization, practice, UI, etc.)
    /// </summary>
    public string SectionName;

    /// <summary>
    /// the location the enemy spawns
    /// </summary>
    public Vector3 spawnLocation;

    /// <summary>
    /// the spawn time based on the first enemy hitTiming 
    /// </summary>
    [IgnoreMember]
    public double spawnTime
    {
        set => _spawnTimeOffset = value;
        get => (startTime) - Math.Abs(_spawnTimeOffset);
    }
    private double _spawnTimeOffset = 2.5d;
}

