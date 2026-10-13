/*
 File: BeatData.cs
 Brief: Data container types used to describe song notes and grouped beat sections. Includes serializable
 representations used by MessagePack for saving/loading.
*/

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
/// <summary>Describes the category of hit associated with a note.</summary>
public enum HitType : int
{
    /// <summary>No specific hit type.</summary>
    NONE,
    /// <summary>First placeholder hit type.</summary>
    TEST1,
    /// <summary>Second placeholder hit type.</summary>
    TEST2,
    /// <summary>Third placeholder hit type.</summary>
    TEST3,
    /// <summary>Fourth placeholder hit type.</summary>
    TEST4,
    /// <summary>Hit type represented by an image.</summary>
    IMAGE,
}

/// <summary>
/// Represents a single note/enemy hit entry including location, timing and hit type.
/// </summary>

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


    /// <summary>
    /// Create a shallow clone of this NoteData value.
    /// </summary>
    /// <returns>A copy of the struct with the same values.</returns>
    public NoteData Clone() => new NoteData() { hitLocation = hitLocation, hitTiming = hitTiming, hitType = hitType };
}


[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Represents a grouped set of NoteData that defines a section of the song (a beat/enemy spawn group).
/// Contains spawn metadata, hit data list and timing information used by spawning and gameplay systems.
/// </summary>
public partial class BeatData
{

    //public SongTrack track = null;

    /// <summary>
    /// The list of notes contained in this beat section.
    /// </summary>
    public List<NoteData> noteDataList = new List<NoteData>();

    /// <summary>
    /// Index into a HitLocations table used by the spawner.
    /// </summary>
    public int hitLocationsIndex = -1; //locations where you can hit the enemies

    /// <summary>Index of the lane containing this beat section.</summary>
    public int laneIndex = -1;
    /// <summary>Index of the enemy prefab associated with this beat section.</summary>
    public int enemyIndex = -1;
    /// <summary>Start and end times of this beat section in seconds.</summary>
    public double startTime = 0, endTime = 0;

    /// <summary>
    /// Add a note to this beat group, avoiding duplicates within the provided time pad window.
    /// </summary>
    /// <param name="hitTiming">Timing of the note in seconds.</param>
    /// <param name="hitLocation">Index of the hit location.</param>
    /// <param name="hitType">Type of hit to record.</param>
    /// <param name="timePad">Tolerance for duplicate detection in seconds.</param>
    public void AddNote(double hitTiming, int hitLocation, HitType hitType, float timePad = 0)
    {
        if(noteDataList.Any((note) =>
         InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad) &&
        note.hitLocation == hitLocation &&
        note.hitType == hitType)) return;

        noteDataList.Add(new NoteData() { hitLocation = hitLocation, hitTiming = hitTiming, hitType = hitType });
    }

    /// <summary>
    /// Remove notes that match the specified parameters within the tolerance window.
    /// </summary>
    /// <param name="hitTiming">Timing to match.</param>
    /// <param name="hitLocation">Hit location index to match.</param>
    /// <param name="hitType">Hit type to match.</param>
    /// <param name="timePad">Tolerance window in seconds.</param>
    public void RemoveNote(double hitTiming, int hitLocation, HitType hitType, float timePad = 0)
    {
        noteDataList.RemoveAll((note) => InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad) && note.hitLocation == hitLocation && note.hitType == hitType);
    }

    /// <summary>
    /// Retrieve a single note that matches the specified timing and optional filters.
    /// </summary>
    /// <param name="hitTiming">Target timing to search for.</param>
    /// <param name="TimePad">Tolerance in seconds.</param>
    /// <param name="hitLocation">Optional hit location filter; -1 to ignore.</param>
    /// <param name="hitType">Optional hit type filter; -1 to ignore.</param>
    /// <returns>The first matching NoteData or default if none found.</returns>
    public NoteData GetNote(double hitTiming, float TimePad = 0, int hitLocation = -1, HitType hitType = (HitType)(-1)) =>
        noteDataList.FirstOrDefault((note) =>
        InRange(hitTiming, note.hitTiming - TimePad, note.hitTiming + TimePad) &&
        note.hitLocation == (hitLocation == -1 ? note.hitLocation : hitLocation) &&
        note.hitType == (hitType == (hitType) - 1 ? note.hitType : hitType));


    /// <summary>
    /// Get all notes within the provided timing window.
    /// </summary>
    /// <param name="hitTiming">Center timing.</param>
    /// <param name="timePad">Tolerance window.</param>
    /// <returns>List of matching NoteData.</returns>
    public List<NoteData> GetNotes(double hitTiming, float timePad) =>
         noteDataList?.Where((note) => InRange(hitTiming, note.hitTiming - timePad, note.hitTiming + timePad))?.ToList() ?? new List<NoteData>();

    /// <summary>
    /// Create a deep-ish clone of this BeatData instance.
    /// </summary>
    /// <returns>A new BeatData instance with copied fields.</returns>
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

    /// <summary>Determines whether a value falls within an inclusive numeric interval.</summary>
    /// <param name="val">Value to test.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <returns><see langword="true"/> when the value is within the bounds; otherwise, <see langword="false"/>.</returns>
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
    /// <summary>Gets or sets the time offset used to calculate the enemy spawn time from the section start.</summary>
    public double spawnTime
    {
        set => _spawnTimeOffset = value;
        get => (startTime) - Math.Abs(_spawnTimeOffset);
    }
    /// <summary>Backing offset used by <see cref="spawnTime"/>.</summary>
    private double _spawnTimeOffset = 2.5d;
}

