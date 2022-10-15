using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "ScriptableObjects/SongTrack")]
public class SongTrack : ScriptableObject
{

	public List<GameObject> enemyPrefabs = new List<GameObject>();
	public List<GameObject> notePrefabs = new List<GameObject>();

	public List<BeatData> beats = new List<BeatData>();
	public TempoMap tempo { get; private set; } = new TempoMap();

	void init()
	{
		//tempo.init(beats);
	}


	void clear()
	{
		enemyPrefabs.Clear();
		notePrefabs.Clear();
		beats.Clear();
		tempo.Clear() ;
	}
}
