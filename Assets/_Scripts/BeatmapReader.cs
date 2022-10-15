using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiParser;
using System.IO;




public class BeatmapReader : MonoBehaviour
{
	[SerializeField]string fileName;


	MidiFile file;
	//List<MidiTrack> tracks;

	public List<GameObject> enemytypes;

	private SongTrack song;

	void Awake()
	{

		var path = Path.Combine(Application.dataPath + "/", fileName);
		if(!File.Exists(path)) return;

		file = new MidiFile(path);


		foreach(var tracks in file.Tracks)
		{
			foreach(var ev in tracks.MidiEvents)
			{
				switch(ev.MetaEventType)
				{
				case MetaEventType.KeySignature:

					break;
				case MetaEventType.Tempo:

					break;
				}

				switch(ev.MidiEventType)
				{
				case MidiEventType.NoteOn://used for character stand placement
					
					break;
				case MidiEventType.NoteOff://used for exit options

					break;
				}

				int position = ev.Note % 12;
				int EnemyType = ev.Note / 12;



			}
		
			foreach(var ev in tracks.TextEvents)
			{
				switch(ev.TextEventType)
				{
				case TextEventType.Text:
					break;
				case TextEventType.Lyric:
					break;
				}
				
			}
		}
	}
}