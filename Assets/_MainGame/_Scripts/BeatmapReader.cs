/*
 File: BeatMapReader.cs
 Brief: Loads and inspects a MIDI file to extract events useful for building beat maps and extracting metadata.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiParser;
using System.IO;




/// <summary>
/// Reads and parses a MIDI file to extract beat, tempo, and text events for map creation.
/// </summary>
public class BeatMapReader : MonoBehaviour
{
    /// <summary>Configured MIDI filename, relative to the application's data directory.</summary>
    [SerializeField] string fileName;


    /// <summary>Parsed MIDI file loaded by this component.</summary>
    MidiFile file;
    //List<MidiTrack> tracks;

    /// <summary>Enemy prefabs available for mapping MIDI note data.</summary>
    public List<GameObject> enemytypes;

    /// <summary>Song data instance associated with this MIDI reader.</summary>
    private SongTrack song;

    /// <summary>
    /// Unity Awake callback. Loads and parses the configured MIDI file (if present) and inspects its events.
    /// </summary>
    void Awake()
    {
         

        var path = Path.Combine(Application.dataPath + "/", fileName);
        if(!File.Exists(path)) return;

        file = new MidiFile(path);


        foreach(var tracks in file.Tracks)
        {
            foreach(var ev in tracks.MidiEvents)
            {
                // Metadata and channel events are classified separately before deriving note-based mapping values.
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
                // Distinguish general text from lyrics for later map metadata handling.
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