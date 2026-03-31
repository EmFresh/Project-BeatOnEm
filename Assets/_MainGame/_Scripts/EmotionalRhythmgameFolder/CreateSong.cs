using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

public class CreateSong : MonoBehaviour
{
    public SongTrack track;

    public new AudioSource audio;



    // Start is called before the first frame update
    void OnEnable()
    {
        if(Application.isEditor)
        {


            //var note = new List<HitType> { HitType.TEST1, HitType.TEST2, HitType.TEST3, HitType.TEST4 };
            //track.beats.RemoveAll((a) => { return note.FindIndex((b) => b == (a?.noteData?.hitTypes[0])) >= 0; });

            //int num = 1;

            NoteHitting.onNotePressed.AddListener((ht) =>
            {

                var tmp = new BeatData();

                System.Random ran = new System.Random();

                //tmp.noteData = new NoteData();
                //tmp.noteData.hitTimings = new List<float> { audio.time };
                //tmp.noteData.hitTypes = new List<HitType> { note[ran.Next(0, 4)] };
                
                track.beats.Add(tmp);

                //string path = $"Assets/_scripts/SongNotes/Note{num++}.asset";//realized that number is now a static variable even out of scope

                //AssetDatabase.CreateAsset(tmp, path);//uncomment wen creating later 
            });
        }
    }

    private void OnApplicationQuit()
    {
        if(!enabled) return;

        int num = 1;
        var note = new List<HitType> { HitType.TEST1, HitType.TEST2, HitType.TEST3, HitType.TEST4 };
        //var notes = track.beats.FindAll((a) => { return note.FindIndex((b) => b == (a?.noteData?.hitTypes[0])) >= 0; });
        //foreach(var tmp in notes)
        //{
        //    string path = $"Assets/_scripts/SongNotes/Note{num++}.asset";
        //    AssetDatabase.CreateAsset(tmp, path);//uncomment wen creating later 
        //}
    }

}
