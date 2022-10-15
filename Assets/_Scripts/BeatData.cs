using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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



public enum HitType
{
	NONE,
	TEST1,
	TEST2,
	TEST3,
}

[Serializable]
public class NoteData
{
	//note stuff

	/// <summary>
	/// places the enemy can be hit (actual locations will be objects on enemy)
	/// </summary>
	public List<uint> hitLocations;

	/// <summary>
	/// the timing of each hit location
	/// </summary>
	public List<float> hitTimings;

	/// <summary>
	/// how the target should be hit (will make it a single value later)
	/// </summary>
	public List<HitType> hitTypes;

}


[CreateAssetMenu(menuName = "ScriptableObjects/BeatData")]
public class BeatData : ScriptableObject
{
	public static bool pausing = false;
	public static bool hallogram = false;

	public NoteData noteData = null;


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
	/// <para set>this</para>
	/// </summary>
	public float spawnTimeOffset
	{
		set => _spawnTimeOffset = value;
		get => (noteData?.hitTimings?[0] ?? float.PositiveInfinity) - Mathf.Abs(_spawnTimeOffset);
	}
	private float _spawnTimeOffset = 2.5f;
}

