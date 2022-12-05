using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "ScriptableObjects/SongTrack")]
public class SongTrack : ScriptableObject
{

	public List<GameObject> enemyPrefabs;
	public List<GameObject> notePrefabs;

	public List<BeatData> beats;
	public TempoMap tempo;

	private void OnEnable()
	{
		if(enemyPrefabs == null)
			enemyPrefabs = new List<GameObject>();
		if(notePrefabs == null)
			notePrefabs = new List<GameObject>();
		if(beats == null)
			beats = new List<BeatData>();
		if(tempo == null)
			tempo = new TempoMap();
	}

	void init()
	{
		//tempo.init(beats);
	}


	void clear()
	{
		enemyPrefabs.Clear();
		notePrefabs.Clear();
		beats.Clear();
		tempo.Clear();
	}
}
