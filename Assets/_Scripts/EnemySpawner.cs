using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
	public SongTrack track;

	int last = 0;
	private void SpawnUpdate()
	{
		if(!track) return;

		var timings = new List<Tuple<float, NoteData>>();
		foreach(var beat in track.beats)
			if(beat.spawnTimeOffset != float.PositiveInfinity)
				timings.Add(new Tuple<float, NoteData>(beat.spawnTimeOffset, beat.noteData));


		float reactTime=2.1f;
		for(int count = last; count < timings.Count; ++count)
		{
			var time = timings[count];

			if(clip.time/*replacing song time*/ - time.Item1 >= reactTime) // top bound for enemy
				break;

			if(clip.time/*replacing song time*/ -time.Item1 >= -.05) // within bound
			{
				print("created Object!!");

				var obj = Instantiate(track.enemyPrefabs[0], new Vector3(0, 0, time.Item1), Quaternion.Euler(0, 180, 0));
				obj.transform.localPosition -= (Vector3)(Vector2)obj.GetComponentInChildren<MeshFilter>().mesh.bounds.min;

				var act = obj.AddComponent<EnemyActions>();
				act.noteData = time.Item2;
				act.reactTime = reactTime;
				act.hitModel = track.notePrefabs[0];
				act.clip = clip;

				last = count + 1;
			}
		}
	}

	//DateTime timer = DateTime.MinValue;
	public AudioSource clip = null;
	void Update()
	{
		if(!clip?.isPlaying ?? false)
			clip?.Play();

		SpawnUpdate();
	}
}
