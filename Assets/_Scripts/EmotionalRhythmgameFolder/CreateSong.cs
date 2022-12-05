using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Jobs;
//using UnityEngine.Jobs;
using System;
using System.Linq;

public class CreateSong : MonoBehaviour
{
	public SongTrack track;

	public new AudioSource audio;


	struct CreateAssetJob : IJob
	{

		public int num;

		public void Execute()
		{


			
		}
	}
	// Start is called before the first frame update
	void Awake()
	{

		track.beats.Clear();
		int num = 1;
		CreateAssetJob job = new CreateAssetJob();
		JobHandle createAssetHandle = new JobHandle();

		
		NoteHitting.onNotePressed.AddListener((ht) =>
		{
			createAssetHandle.Complete();
			var tmp = ScriptableObject.CreateInstance<BeatData>();



			var note = new[] { HitType.TEST1, HitType.TEST2, HitType.TEST3, HitType.TEST4 };
			System.Random ran = new System.Random();
			tmp.noteData = new NoteData();
			tmp.noteData.hitTimings = new List<float> { audio.time };

			tmp.noteData.hitTypes = new List<HitType> { note[ran.Next(0, 4)] };
			track.beats.Add(tmp);

			string path = $"Assets/_scripts/SongNotes/Note{num++}.asset";//realized that number is now a static variable even out of scope

			AssetDatabase.CreateAsset(tmp, path);
		});
	}

	// Update is called once per frame
	void Update()
	{

	}
}
