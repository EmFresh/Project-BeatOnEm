using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SimpleFileBrowser;

using UnityEditor;
using UnityEditor.Rendering;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

using static EditorSettings;

using CompressionLevel = System.IO.Compression.CompressionLevel;

/// <summary>
/// Interface implemented by classes that provide access to a SongTrack, AudioSource and lane GameObjects.
/// Used by systems that need to interact with a song manager without depending on a concrete implementation.
/// </summary>
public interface ISongManager
{


    /// <summary>
    /// The currently loaded song track containing beats, tempo map and prefab references.
    /// </summary>
    public SongTrack MapTrack { get; }
    /// <summary>
    /// AudioSource used for playback of the song associated with the map.
    /// </summary>
    public AudioSource AudioSource { get; }
    /// <summary>
    /// List of lane GameObjects used by the song/map for spawning and positioning.
    /// </summary>
    public List<GameObject> Lanes { get; }

    public void SaveFile(string saveDir, string audioPath, string name, MessagePackSerializerOptions MPOptions = null)
    {
        MPOptions ??=
            new MessagePackSerializerOptions(
                CompositeResolver.Create(
                    UnityResolver.Instance,
                    StandardResolver.Instance,
                    BuiltinResolver.Instance,
                    //default resolver
                    ContractlessStandardResolver.Instance
                )
            );

        try
        {

            var ser =
                JToken.Parse(MessagePackSerializer.SerializeToJson(MapTrack, MPOptions)).ToString(Formatting.Indented);

            List<byte[]> serializedEnemis = new List<byte[]>();
            List<byte[]> serializedNotes = new List<byte[]>();

            MapTrack.enemyPrefabs.ForEach((prefab) =>
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                if(string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(
                        PrefabUtility.SaveAsPrefabAsset(prefab, Path.Combine(Application.temporaryCachePath, "enemy.prefab")));
                }

                serializedEnemis.Add(Encoding.Unicode.GetBytes(File.ReadAllText(prefabPath)));
            });
            MapTrack.notePrefabs.ForEach((prefab) =>
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                if(string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(
                        PrefabUtility.SaveAsPrefabAsset(prefab, Path.Combine(Application.temporaryCachePath, "note.prefab")));
                }
                serializedNotes.Add(Encoding.Unicode.GetBytes(File.ReadAllText(prefabPath)));

            });

            //  ser = JsonUtility.ToJson(ser, true);
            if(!File.Exists(audioPath))
                throw new FileNotFoundException("Audio file not found.");

            if(!Directory.Exists(Path.GetDirectoryName(saveDir)))
                Directory.CreateDirectory(Path.GetDirectoryName(saveDir));

            using(var fileStream = new FileStream(Path.Combine(saveDir, name), FileMode.OpenOrCreate))
            {
                using(var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
                {
                    using(var entryStream = archive.CreateEntry("data.json", CompressionLevel.Optimal).Open())
                        entryStream.Write(Encoding.Unicode.GetBytes(ser));

                    archive.CreateEntryFromFile(audioPath, Path.GetFileName(audioPath), CompressionLevel.Optimal);
                }
            }


            //  print($"Saved file [{phase.ToString()}]");
        }
        catch(Exception e)
        {
            Debug.LogError($"{e}");
        }
        //  print(JsonUtility.ToJson(pile.ocupado,true));

        // return true;
    }

    public async Task LoadFile(string path, bool excludePrefabs = true, MessagePackSerializerOptions MPOptions = null)
    {
        Debug.Log($"Loading file: {path}");
        if(!File.Exists(path)) return;

        MPOptions ??=
            new MessagePackSerializerOptions(
                CompositeResolver.Create(
                    UnityResolver.Instance,
                    StandardResolver.Instance,
                    BuiltinResolver.Instance,
                    //default resolver
                    ContractlessStandardResolver.Instance
                )
            );

        try
        {
            string json = null;
            //SongTrack track = null;


            using(var archive = ZipFile.OpenRead(path))
            {
                foreach(var entry in archive.Entries)
                {
                    if(entry.Name.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase))
                    {
                        using(var reader = new StreamReader(entry.Open(), Encoding.Unicode))
                        {
                            Toast.SendToast($"Loading JSON file: {entry.FullName}");
                            json = reader.ReadToEnd();
                            //json = Encoding.Unicode.GetString(Encoding.Unicode.GetBytes(json));
                            var track = MessagePackSerializer.Deserialize<SongTrack>(
                                    MessagePackSerializer.ConvertFromJson(json, MPOptions), MPOptions);

                            MapTrack?.Copy(track, excludePrefabs: excludePrefabs);
                            Toast.SendToast($"Loaded JSON file: {entry.FullName}");
                        }
                    }

                    if(entry.Name.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase) ||
                       entry.Name.EndsWith(".wav", StringComparison.CurrentCultureIgnoreCase) ||
                       entry.Name.EndsWith(".ogg", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var tmpPath = Path.Combine(Application.temporaryCachePath, "music" + Path.GetExtension(entry.Name));
                        entry.ExtractToFile(tmpPath, true);
                        using(var request = UnityWebRequestMultimedia.GetAudioClip(tmpPath, AudioType.UNKNOWN))
                        {
                            Toast.SendToast($"Loading audio file: {tmpPath}");

                            // Wait for completion  
                            await request.SendWebRequest();

                            if(request.result != UnityWebRequest.Result.Success)
                                throw new Exception($"Error loading audio file: {request.error}");

                            var clip = DownloadHandlerAudioClip.GetContent(request);
                            if(clip != null)
                                AudioSource.clip = clip; // set the AudioSource clip if desired

                            Toast.SendToast(clip != null
                                ? $"Loaded audio file: {tmpPath}"
                                : $"Failed to load audio file: {tmpPath}");
                        }
                        File.Delete(tmpPath);
                    }
                }
            }



            // var json = File.ReadAllText(path);
            // var track =
            // MessagePackSerializer.Deserialize<SongTrack>(
            //     MessagePackSerializer.ConvertFromJson(json, MPOptions), MPOptions);

            //return track;
            //JsonConvert.DeserializeObject<SongTrack>(json);      
        }
        catch(Exception e)
        {
            Toast.SendToast($"{e}", Toast.ToastType.Error);
        }

        //  return null;
    }

}

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// Central manager for map editing functionality. Handles loading/saving maps, editing operations,
/// audio playback integration, and orchestration of editor UI components.
/// </summary>
public class MapEditManager : ActionHistory, ISongManager
{
    [SerializeField] SongTrack mapTrack;
    [SerializeField] AudioSource audioSource;
    [SerializeField] EditorSettings editorSettings;
    [SerializeField] PlayerInput playerInput;
    public TimelineImageCreator timelineImageCreator;
    public TempoMarkManager tempoMarkManager;
    public TimeBar timeBar;
    public SeekBar seekBar;


    [SerializeField] RawImage enemyImg;
    [SerializeField] RawImage noteImg;
    [SerializeField] float timePad = 0.1f;
    private MessagePackSerializerOptions MPOptions;

    public SongTrack MapTrack => mapTrack;
    public AudioSource AudioSource => audioSource;
    public List<GameObject> Lanes => AddToTheBodyPile.pile.Select(o => o.gameObject).ToList();


    private async void Start()
    {
        MPOptions =
        new MessagePackSerializerOptions(
        CompositeResolver.Create(
                UnityResolver.Instance,
                StandardResolver.Instance,
                BuiltinResolver.Instance,
                //default resolver
                ContractlessStandardResolver.Instance));

        //JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
        //{
        //    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        //};

        void ShortAudioPlay(float duration)
        {
            var clip = GetComponent<AudioSource>(); clip.clip = AudioSource.clip;
            clip.SetAccurateTime(seekBar.currentTime);
            StartCoroutine(StartStop());
            IEnumerator StartStop()
            {
                clip.Play();
                yield return new WaitForSeconds(duration);
                clip.Pause();

            }
        }

        double lastTime = -1;
        seekBar.onSeekBarMoved.AddListener((time) =>
        {
            if(time == lastTime) return;

            if(!AudioSource.isPlaying) ShortAudioPlay(.1f);
            UpdateVisuals();

            lastTime = time;
        });

        var file = Path.Combine(editorSettings.FileDir, editorSettings.FileName + EXTENSION);
        var loaded = await LoadFile(file);


        if(loaded)
        {
            timelineImageCreator.UpdateLineUIAsync(false);
            UpdateVisuals();
        }
    }

    private void Update()
    {
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if(seekBar.currentTime < 0)
                seekBar.SetLocation(0);

            if(!AudioSource.isPlaying)
                AudioSource.SetAccurateTime(seekBar.currentTime);

            if(AudioSource.isPlaying) AudioSource.Pause();
            else AudioSource.Play();

        }

        if(AudioSource.isPlaying)
            UpdateVisuals();

    }


    public void SaveFile() => SaveFile(editorSettings.FileDir, editorSettings.AudioDir, editorSettings.FileName + EXTENSION);
    public void SaveFile(string saveDir, string audioPath, string name)
    {
        try
        {
            var phase = playerInput?.actions?.FindAction("Save")?.phase;
            if((int)phase > 1 && phase != InputActionPhase.Performed) return;

            ((ISongManager)this).SaveFile(saveDir, audioPath, name, MPOptions);

        }
        catch(Exception e)
        {
            Toast.SendToast($"Error saving file: {e.Message}");
        }
        Toast.SendToast($"File saved to ({Path.Combine(saveDir, name)})");
        //  print(JsonUtility.ToJson(pile.ocupado,true));

        // return true;
    }


    public void LoadFile() =>

        FileBrowser.ShowLoadDialog(async paths =>
        {
            await LoadFile(paths[0]);
        }, null, FileBrowser.PickMode.Files, allowMultiSelection: false, title: "Load Map File");

    public async Task<bool> LoadFile(string path)
    {
        //print($"Path to file: {path}");
        if(!File.Exists(path))
        {
            Toast.SendToast($"File not found: {path}");
            return false;
        }


        await ((ISongManager)this).LoadFile(path, MPOptions: MPOptions);

        tempoMarkManager?.MarkerUpdate();
        UpdateVisuals();

        Toast.SendToast($"File Loaded from ({path})");

        return true;
    }

    public void AddTempo(float BPM, double time, bool nested = false)
    {
        MapTrack.tempoMap.AddTempo(BPM, time);
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                RemoveTempo(time, nested: true);
                Toast.SendToast($"Undo: Removed Tempo at [{time}] seconds");
                break;
            case ActionType.Redo:
                AddTempo(BPM, time, nested: true);
                Toast.SendToast($"Redo: Added Tempo at [{time}] seconds");
                break;
            }
        }, nested);
    }
    public void AddTimeSig(int beats, int note, double time, bool nested = false)
    {
        MapTrack.tempoMap.AddTimeSig(beats, note, time);
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                RemoveTimeSig(time, nested: true);
                Toast.SendToast($"Undo: Removed Time Signature at [{time}] seconds");
                break;
            case ActionType.Redo:
                AddTimeSig(beats, note, time, nested: true);
                Toast.SendToast($"Redo: Added Time Signature at [{time}] seconds");
                break;
            }
        }, nested);
    }

    public void EditTempo(float tempo, double time, bool nested = false)
    {
        var oldTempo = MapTrack.tempoMap.GetTempo(time).Clone();

        MapTrack.tempoMap.GetTempo(time).bpm = tempo;
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                EditTempo(oldTempo.bpm, oldTempo.timing, nested: true);
                Toast.SendToast($"Undo: Edited Tempo at [{oldTempo.timing}] seconds to {oldTempo.bpm} BPM");
                break;
            case ActionType.Redo:
                EditTempo(tempo, time, nested: true);
                Toast.SendToast($"Redo: Edited Tempo at [{time}] seconds to {tempo} BPM");
                break;
            }
        }, nested);
    }
    public void EditTimeSig(int beats, int note, double time, bool nested = false)
    {
        var oldSig = MapTrack.tempoMap.GetTimeSig(time).Clone();
        var sig = MapTrack.tempoMap.GetTimeSig(time);
        sig.note = note;
        sig.beats = beats;

        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                EditTimeSig(oldSig.beats, oldSig.note, oldSig.timing, nested: true);
                Toast.SendToast($"Undo: Edited Time Signature at [{oldSig.timing}] seconds to {oldSig.beats}/{oldSig.note}");
                break;
            case ActionType.Redo:
                EditTimeSig(beats, note, time, nested: true);
                Toast.SendToast($"Redo: Edited Time Signature at [{time}] seconds to {beats}/{note}");
                break;
            }
        }, nested);
    }

    public void RemoveTempo(double time, bool nested = false)
    {
        var oldTempo = MapTrack.tempoMap.GetTempo(time).Clone();
        int count = MapTrack.tempoMap.tempos.Count;

        MapTrack.tempoMap.RemoveTempo(time);
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:

                AddTempo(oldTempo.bpm, oldTempo.timing, nested: true);
                if(count < 2)
                    RemoveTempo(0, nested: true);

                Toast.SendToast($"Undo: Added Tempo at [{oldTempo.timing}] seconds");
                break;
            case ActionType.Redo:
                RemoveTempo(time, nested: true);
                Toast.SendToast($"Redo: Removed Tempo at [{time}] seconds");
                break;
            }
        }, nested);
    }
    public void RemoveTimeSig(double time, bool nested = false)
    {
        var oldTimSig = MapTrack.tempoMap.GetTimeSig(time).Clone();
        MapTrack.tempoMap.RemoveTimeSig(time);
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                AddTimeSig(oldTimSig.beats, oldTimSig.note, oldTimSig.timing, nested: true);
                Toast.SendToast($"Undo: Added Time Signature at [{oldTimSig.timing}] seconds");
                break;
            case ActionType.Redo:
                RemoveTimeSig(time, nested: true);
                Toast.SendToast($"Redo: Removed Time Signature at [{time}] seconds");
                break;
            }
        }, nested);
    }

    public void AddEnemy(int lane, int hitLocationsIndex, double duration, double spawnTime, bool nested = false)
    {

        var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);

        if(pile.ocupado == null)
            MapTrack.beats.Add(pile.ocupado = new BeatData() { startTime = seekBar.currentTime, endTime = seekBar.currentTime + duration });
        {
            pile.ocupado.laneIndex = lane;
            pile.ocupado.hitLocationsIndex = hitLocationsIndex;
            //pile.ocupado.track = MapTrack;
            pile.ocupado.spawnTime = spawnTime;
            // pile.ocupado.startTime =   pile.ocupado.startTime;
            pile.ocupado.endTime = pile.ocupado.startTime + duration;
        }

        var beat = pile.ocupado?.Clone();
        AddToActionHistory(t =>
        {
            seekBar.SetLocation(beat.startTime);
            switch(t)
            {
            case ActionType.Undo:
                RemoveEnemy(lane, nested: true);
                Toast.SendToast($"Undo: Removed Enemy at [{beat.startTime}] seconds");
                break;
            case ActionType.Redo:
                AddEnemy(lane, hitLocationsIndex, duration, spawnTime, nested: true);
                Toast.SendToast($"Redo: Added Enemy at [{beat.startTime}] seconds");
                break;
            }
        }, nested);

        if(pile.ocupado != null)
            UpdateVisuals();

    }
    public void RemoveEnemy(int lane, bool nested = false)
    {
        var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);

        if(pile.ocupado == null) return;
        var beat = pile.ocupado.Clone();

        MapTrack.beats.Remove(pile.ocupado);
        pile.ocupado = null;

        AddToActionHistory(t =>
        {
            seekBar.SetLocation(beat.startTime);
            switch(t)
            {
            case ActionType.Undo:
                AddEnemy(lane, beat.hitLocationsIndex, beat.endTime - beat.startTime, beat.spawnTime, nested: true);
                Toast.SendToast($"Undo: Added Enemy at [{beat.startTime}] seconds");
                break;
            case ActionType.Redo:
                RemoveEnemy(lane, nested: true);
                Toast.SendToast($"Redo: Removed Enemy at [{beat.startTime}] seconds");
                break;
            }
        }, nested);

        UpdateVisuals();
    }

    //public void ToggleEnemy(int lane)
    //{
    //    var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);
    //    if(pile.ocupado)
    //        removeEnemy(lane);
    //    else
    //        AddEnemy(lane);
    //}

    public void AddNote(int lane, int hitLocation, HitType hitType, bool nested = false)
    {
        var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);
        var beat = pile.ocupado;


        beat?.AddNote(seekBar.currentTime, hitLocation, hitType);


        var beatClone = beat?.Clone();

        AddToActionHistory(t =>
        {

            seekBar.SetLocation(beatClone.startTime);
            switch(t)
            {
            case ActionType.Undo:
                RemoveNote(lane, seekBar.currentTime, hitLocation, hitType, timePad, nested: true);
                Toast.SendToast($"Undo: Removed Note at [{seekBar.currentTime}] seconds");
                break;
            case ActionType.Redo:
                AddNote(lane, hitLocation, hitType, nested: true);
                Toast.SendToast($"Redo: Added Note at [{seekBar.currentTime}] seconds");
                break;
            }
        }, nested);

        UpdateVisuals();
    }
    public void RemoveNote(int lane, double time, int hitLocation, HitType hitType, float timePad, bool nested = false)
    {

        var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);
        var beat = pile.ocupado;
        var oldBeat = beat?.Clone();
        beat.RemoveNote(time, hitLocation, hitType, timePad);
        pile.ocupado = null;


        AddToActionHistory(t =>
        {
            seekBar.SetLocation(oldBeat.startTime);
            switch(t)
            {
            case ActionType.Undo:
                AddNote(lane, hitLocation, hitType, nested: true);
                Toast.SendToast($"Undo: Added Note at [{seekBar.currentTime}] seconds");
                break;
            case ActionType.Redo:
                RemoveNote(lane, time, hitLocation, hitType, timePad, nested: true);
                Toast.SendToast($"Redo: Removed Note at [{seekBar.currentTime}] seconds");
                break;
            }
        }, nested);

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        var time = AudioSource.isPlaying ? AudioSource.time : seekBar.currentTime;

        var liljon = AddToTheBodyPile.pile;
        var enemies = MapTrack.GetEnemies(time);

        //remove all unused images
        foreach(var lane in liljon)
            //if(!enemies.Any(p => p.laneIndex == lane.laneNum) && lane.ocupado)
            if((lane.ocupado != null && !enemies.Any(p => p.laneIndex == lane.laneNum)) ||
               (lane.ocupado == null && lane.GetComponentInParent<HitLocationVisualizer>().enabled))
            {
                Array.ForEach(lane.GetComponentsInChildren<RawImage>(),
                    (img) =>
                    {
                        if(img.gameObject != lane.gameObject)
                            Destroy(img.gameObject);
                    });

                lane.GetComponentInParent<HitLocationVisualizer>().enabled = false;
                lane.ocupado = null;
            }


        //add images for all enemies in range
        foreach(var enemy in enemies)
        {
            var pile = liljon.Find((pile) => pile.laneNum == enemy.laneIndex);
            var visualizer = pile.GetComponentInParent<HitLocationVisualizer>();
            if(visualizer)
            {
                visualizer.hitLocations = MapTrack.hitLocationsList[enemy.hitLocationsIndex];

                //add if not already present
                if(!visualizer.enabled)
                {
                    pile.ocupado = enemy;
                    //visualizer.scale = Vector3.one * 100 * 3;

                    var obj = Instantiate(enemyImg.gameObject, pile.transform, false);
                    obj.transform.SetAsFirstSibling();
                    obj.GetComponent<RawImage>().raycastTarget = false;

                    visualizer.enableAction.RemoveAllListeners();
                    visualizer.enableAction.AddListener(() =>
                    {
                        int count = 0;
                        foreach(var point in visualizer.points)
                        {
                            if(point.GetComponent<EventTrigger>())
                                Destroy(point.GetComponent<EventTrigger>());
                            var location = count;//Needed for the lambda to work, otherwise it will always be the last value of count

                            var trigger = point.AddComponent<EventTrigger>();
                            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown, };
                            entry.callback.AddListener((data) =>
                            {
                                var pointerData = (PointerEventData)data;
                                if(pointerData.button == PointerEventData.InputButton.Left)
                                    AddNote(enemy.laneIndex, location, HitType.NONE);
                                else if(pointerData.button == PointerEventData.InputButton.Right)
                                    RemoveNote(enemy.laneIndex, time, location, HitType.NONE, timePad);
                            });
                            trigger.triggers.Add(entry);
                            ++count;
                        }
                    });
                    visualizer.enabled = true;
                    // visualizer.VisualUpdate();
                }
            }

            var notes = enemy.GetNotes(time, timePad);
            for(int a = 0; a < MapTrack.hitLocationsList[enemy.hitLocationsIndex].points.Count; ++a)
                visualizer.SetColour2D(a, Color.white);

            foreach(var note in notes)
                //visualizer.SetColour2D(note.hitLocation, Color.green);
                visualizer.SetColour2D(note.hitLocation, Color.Lerp(Color.green, Color.white, Mathf.InverseLerp((float)note.hitTiming - timePad, (float)note.hitTiming + timePad, (float)time) - .5f));



            //  print("Completed updating visuals");
        }
    }

    bool InRange(double val, double min, double max) => val >= min && val <= max;
}
