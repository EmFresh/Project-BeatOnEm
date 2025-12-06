using UnityEngine;

public class NoteEditorPlacer : MonoBehaviour
{

    public MapEditManager mapEditManager;


    public void PlaceNoteAtCurrentTime(GameObject notePrefab)
    {
        if(mapEditManager.seekBar == null || mapEditManager.mapTrack == null || notePrefab == null)
            throw new System.Exception("Missing references in NoteEditorPlacer");



        BeatData noteData = ScriptableObject.CreateInstance<BeatData>();


    }

}
