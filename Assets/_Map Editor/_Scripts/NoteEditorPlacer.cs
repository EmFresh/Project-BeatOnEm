/*
 File: NoteEditorPlacer.cs
 Brief: Editor helper component to place notes into the active SongTrack at the current seek time.
*/
using UnityEngine;

/// <summary>Provides editor controls for placing note-related data at the current playhead time.</summary>
public class NoteEditorPlacer : MonoBehaviour
{

    /// <summary>
    /// Reference to the MapEditManager used to query the current time and map data.
    /// </summary>
    public MapEditManager mapEditManager;


    /// <summary>
    /// Instantiate and place a note prefab at the current seek time in the map editor.
    /// </summary>
    /// <param name="notePrefab">Prefab GameObject used for the note representation.</param>
    public void PlaceNoteAtCurrentTime(GameObject notePrefab)
    {
        if(mapEditManager.seekBar == null || mapEditManager.MapTrack == null || notePrefab == null)
            throw new System.Exception("Missing references in NoteEditorPlacer");

        // Prepare a new BeatData placeholder for future implementation. Documentation added; no runtime change.
        BeatData noteData = new BeatData();

    }

}
