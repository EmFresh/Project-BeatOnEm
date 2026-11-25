/*
 File: MapEditManager.cs
 Brief: Provides editor-side map management including loading/saving map packages, managing the timeline UI
 and exposing an ISongManager interface for other components to interact with song data.
*/

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

    /// <summary>
    /// Save the current MapTrack and associated audio/prefabs into a packaged file (zip) at the specified location.
    /// </summary>
    /// <param name="saveDir">Directory in which the package will be saved.</param>
    /// <param name="audioPath">Path to the audio file to include in the package.</param>
    /// <param name="name">Filename for the saved package.</param>
    /// <param name="MPOptions">Optional MessagePack serializer options used to serialize the SongTrack.</param>
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
            // Convert the MessagePack model to readable JSON for the package entry.
            var ser =
                JToken.Parse(MessagePackSerializer.SerializeToJson(MapTrack, MPOptions)).ToString(Formatting.Indented);

            List<byte[]> serializedEnemis = new List<byte[]>();
            List<byte[]> serializedNotes = new List<byte[]>();


            // Validate the audio asset and ensure the package destination exists before writing entries.
            if(!File.Exists(audioPath))
                throw new FileNotFoundException("Audio file not found.");

            if(!Directory.Exists(Path.GetDirectoryName(saveDir)))
                Directory.CreateDirectory(Path.GetDirectoryName(saveDir));

            using(var fileStream = new FileStream(Path.Combine(saveDir, name), FileMode.OpenOrCreate))
            {
                // Store map data and its audio asset together in the map archive.
                using(var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
                {
                    using(var entryStream = archive.CreateEntry("data.json", CompressionLevel.Optimal).Open())
                        entryStream.Write(Encoding.Unicode.GetBytes(ser));

                    archive.CreateEntryFromFile(audioPath, Path.GetFileName(audioPath), CompressionLevel.Optimal);
                }
            }


        }
        catch(Exception e)
        {
            Toast.SendToast($"{e}", Toast.ToastType.Error);
        }
    }

    /// <summary>
    /// Load a packaged map file (zip) and populate the MapTrack and audio clip from its contents.
    /// </summary>
    /// <param name="path">Full path to the package file to load.</param>
    /// <param name="excludePrefabs">If true, prefab contents will not be applied to the MapTrack (used for editor workflows).</param>
    /// <param name="MPOptions">Optional MessagePack serializer options used to deserialize the SongTrack.</param>
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
            // Open the archive and process each map or audio entry by file type.
            using(var archive = ZipFile.OpenRead(path))
            {
                foreach(var entry in archive.Entries)
                {
                    if(entry.Name.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase))
                    {
                        using(var reader = new StreamReader(entry.Open(), Encoding.Unicode))
                        {
                            //  Toast.SendToast($"Loading JSON file: {entry.FullName}");
                            json = reader.ReadToEnd();
                            // Convert the JSON payload back into the serialized track model and copy it into the active map.
                            var track = MessagePackSerializer.Deserialize<SongTrack>(
                                    MessagePackSerializer.ConvertFromJson(json, MPOptions), MPOptions);

                            MapTrack?.Copy(track, excludePrefabs: excludePrefabs);
                            //  Toast.SendToast($"Loaded JSON file: {entry.FullName}");
                        }
                    }

                    if(entry.Name.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase) ||
                       entry.Name.EndsWith(".wav", StringComparison.CurrentCultureIgnoreCase) ||
                       entry.Name.EndsWith(".ogg", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Extract the audio entry to a temporary file so Unity can decode it asynchronously.
                        var tmpPath = Path.Combine(Application.temporaryCachePath, "music" + Path.GetExtension(entry.Name));
                        entry.ExtractToFile(tmpPath, true);
                        using(var request = UnityWebRequestMultimedia.GetAudioClip(tmpPath, AudioType.UNKNOWN))
                        {
                            // Toast.SendToast($"Loading audio file: {tmpPath}");

                            // Wait for decoding, report failures, and assign the resulting clip when available.
                            await request.SendWebRequest();

                            if(request.result != UnityWebRequest.Result.Success)
                                throw new Exception($"Error loading audio file: {request.error}");

                            var clip = DownloadHandlerAudioClip.GetContent(request);
                            if(clip != null)
                                AudioSource.clip = clip; // set the AudioSource clip if desired

                            //Toast.SendToast(clip != null
                            //    ? $"Loaded audio file: {tmpPath}"
                            //    : $"Failed to load audio file: {tmpPath}");
                        }
                        // The clip is loaded into memory, so remove the temporary extracted file.
                        File.Delete(tmpPath);
                    }
                }
            }
        }
        catch(Exception e)
        {
            Toast.SendToast($"{e}", Toast.ToastType.Error);
        }

    }

}

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// Central manager for map editing functionality. Handles loading/saving maps, editing operations,
/// audio playback integration, and orchestration of editor UI components.
/// </summary>
public class MapEditManager : ActionHistory, ISongManager
{
    /// <summary>The song map currently being edited.</summary>
    [SerializeField] SongTrack mapTrack;
    /// <summary>Audio source used to preview the current song.</summary>
    [SerializeField] AudioSource audioSource;
    /// <summary>Editor settings containing map and audio file paths.</summary>
    [SerializeField] EditorSettings editorSettings;
    /// <summary>Input actions used by editor controls.</summary>
    [SerializeField] PlayerInput playerInput;
    /// <summary>Creates waveform and timeline image UI.</summary>
    public TimelineImageCreator timelineImageCreator;
    /// <summary>Manages tempo and time-signature markers on the timeline.</summary>
    public TempoMarkManager tempoMarkManager;
    /// <summary>Displays the current timeline time.</summary>
    public TimeBar timeBar;
    /// <summary>Controls seeking through the song timeline.</summary>
    public SeekBar seekBar;


    /// <summary>Template image used to display enemy hit locations.</summary>
    [SerializeField] RawImage enemyImg;
    /// <summary>Template image used to display note hit locations.</summary>
    [SerializeField] RawImage noteImg;
    /// <summary>Time tolerance, in seconds, used when matching notes for visualization.</summary>
    [SerializeField] float timePad = 0.1f;
    /// <summary>Serializer options configured for map package persistence.</summary>
    private MessagePackSerializerOptions MPOptions;

    /// <summary>Gets the current song map.</summary>
    public SongTrack MapTrack => mapTrack;
    /// <summary>Gets the audio source associated with the current map.</summary>
    public AudioSource AudioSource => audioSource;
    /// <summary>Gets the lane GameObjects managed by the shared body-pile collection.</summary>
    public List<GameObject> Lanes => AddToTheBodyPile.pile.Select(o => o.gameObject).ToList();


    /// <summary>Initializes serializer settings, seek callbacks, and the saved editor map.</summary>
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

        /// <summary>Plays a brief audio segment from the current playhead position.</summary>
        /// <param name="duration">Playback duration in seconds.</param>
        void ShortAudioPlay(float duration)
        {
            var clip = GetComponent<AudioSource>(); clip.clip = AudioSource.clip;
            clip.SetAccurateTime(seekBar.currentTime);
            StartCoroutine(StartStop());
            /// <summary>Plays the clip and pauses it after the requested short interval.</summary>
            /// <returns>A coroutine enumerator for short playback.</returns>
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

    /// <summary>Processes playback input and refreshes map visuals while audio is playing.</summary>
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


    /// <summary>Saves the current map using the configured editor paths and filename.</summary>
    public void SaveFile() => SaveFile(editorSettings.FileDir, editorSettings.AudioDir, editorSettings.FileName + EXTENSION);

    /// <summary>Saves the current map and audio into a packaged map file.</summary>
    /// <param name="saveDir">Directory in which to save the package.</param>
    /// <param name="audioPath">Path to the audio file included in the package.</param>
    /// <param name="name">Package filename.</param>
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
    }

    /// <summary>Opens a file picker and loads the selected map package.</summary>
    public void LoadFile() =>

        FileBrowser.ShowLoadDialog(async paths =>
        {
            await LoadFile(paths[0]);
        }, null, FileBrowser.PickMode.Files, allowMultiSelection: false, title: "Load Map File");

    /// <summary>Loads a map package and refreshes timeline markers and visuals.</summary>
    /// <param name="path">Full path to the package file.</param>
    /// <returns><see langword="true"/> when the file exists and the load operation is invoked; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> LoadFile(string path)
    {
        // Stop immediately when the selected path does not exist.
        if(!File.Exists(path))
        {
            Toast.SendToast($"File not found: {path}");
            return false;
        }


        // Load map data/audio first, then refresh editor markers and visuals from the new state.
        await ((ISongManager)this).LoadFile(path, MPOptions: MPOptions);

        tempoMarkManager?.MarkerUpdate();
        UpdateVisuals();

        Toast.SendToast($"File Loaded from ({path})");

        return true;
    }

    public void AutoCreateTempoList()
    {
        var tempoList = audioSource.clip.GetTempoList();
        var oldList = MapTrack.tempoMap.tempos.ToList();

        MapTrack.tempoMap.tempos.Clear();
        foreach(var tempo in tempoList)
            MapTrack.tempoMap.AddTempo(tempo.bpm, tempo.timing);

        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                MapTrack.tempoMap.tempos.Clear();
                foreach(var tempo in oldList)
                    MapTrack.tempoMap.AddTempo(tempo.bpm, tempo.timing);
                tempoMarkManager.MarkerUpdate();
                Toast.SendToast($"Undo: Undid Auto Tempo");
                break;
            case ActionType.Redo:
                MapTrack.tempoMap.tempos.Clear();
                foreach(var tempo in tempoList)
                    MapTrack.tempoMap.AddTempo(tempo.bpm, tempo.timing);
                tempoMarkManager.MarkerUpdate();
                Toast.SendToast($"Redo: Restored Auto Tempo");
                break;
            }
        }, nested: false);
    }

    /// <summary>Adds a tempo change and records the operation for undo and redo.</summary>
    /// <param name="BPM">Tempo in beats per minute.</param>
    /// <param name="time">Time in seconds at which the tempo takes effect.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void AddTempo(float BPM, double time, bool nested = false)
    {
        // Update the data model and visible markers before registering the inverse operation.
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

    /// <summary>Adds a time-signature change and records the operation for undo and redo.</summary>
    /// <param name="beats">Numerator of the time signature.</param>
    /// <param name="note">Denominator of the time signature.</param>
    /// <param name="time">Time in seconds at which the signature takes effect.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
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

    /// <summary>Changes the effective tempo and records the operation for undo and redo.</summary>
    /// <param name="tempo">New tempo in beats per minute.</param>
    /// <param name="time">Time in seconds of the tempo entry to edit.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void EditTempo(float tempo, double time, bool nested = false)
    {
        // Preserve the previous entry so undo can restore its exact value and effective time.
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

    /// <summary>Changes the effective time signature and records the operation for undo and redo.</summary>
    /// <param name="beats">New time-signature numerator.</param>
    /// <param name="note">New time-signature denominator.</param>
    /// <param name="time">Time in seconds of the signature entry to edit.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void EditTimeSig(int beats, int note, double time, bool nested = false)
    {
        // Snapshot the old signature, mutate the active entry, then refresh markers and history.
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

    /// <summary>Removes the tempo effective at the specified time and records undo and redo actions.</summary>
    /// <param name="time">Time in seconds used to locate the tempo entry.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void RemoveTempo(double time, bool nested = false)
    {
        // Save the removed entry and list size because undo may need to restore the default entry as well.
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

    /// <summary>Removes the time signature effective at the specified time and records undo and redo actions.</summary>
    /// <param name="time">Time in seconds used to locate the time-signature entry.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void RemoveTimeSig(double time, bool nested = false)
    {
        // Snapshot the effective signature before removal so the history callback can restore it.
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

    /// <summary>Adds or updates an enemy section in a lane and records the operation for undo and redo.</summary>
    /// <param name="lane">Lane index receiving the enemy.</param>
    /// <param name="hitLocationsIndex">Index of the hit-location definition.</param>
    /// <param name="duration">Duration of the enemy section in seconds.</param>
    /// <param name="spawnTime">Enemy spawn time in seconds.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void AddEnemy(int lane, int hitLocationsIndex, double duration, double spawnTime, bool nested = false)
    {
        // Find the lane's editor slot, create a beat section if needed, and update its timing/metadata.
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

        // Capture the resulting section for history and use its start time when restoring the playhead.
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

    /// <summary>Removes the enemy section in the specified lane and records undo and redo actions.</summary>
    /// <param name="lane">Lane index from which to remove the enemy.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void RemoveEnemy(int lane, bool nested = false)
    {
        // Preserve the section before clearing both the map entry and the lane's active editor slot.
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

    /// <summary>Adds a note at the current playhead position and records the operation for undo and redo.</summary>
    /// <param name="lane">Lane containing the beat section.</param>
    /// <param name="hitLocation">Hit-location index for the note.</param>
    /// <param name="hitType">Type of hit represented by the note.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void AddNote(int lane, int hitLocation, HitType hitType, bool nested = false)
    {
        // Resolve the selected lane's beat section and add a note at the current playhead time.
        var pile = AddToTheBodyPile.pile.Find((pile) => pile.laneNum == lane);
        var beat = pile.ocupado;


        beat?.AddNote(seekBar.currentTime, hitLocation, hitType);


        // Snapshot the section so undo/redo can seek back to the same context.
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

    /// <summary>Removes a matching note from the specified lane and records undo and redo actions.</summary>
    /// <param name="lane">Lane containing the beat section.</param>
    /// <param name="time">Note timing in seconds.</param>
    /// <param name="hitLocation">Hit-location index for the note.</param>
    /// <param name="hitType">Type of hit represented by the note.</param>
    /// <param name="timePad">Timing tolerance in seconds.</param>
    /// <param name="nested">Whether this operation is part of another history action.</param>
    public void RemoveNote(int lane, double time, int hitLocation, HitType hitType, float timePad, bool nested = false)
    {
        // Find the lane section and retain a copy before removing the matching note.
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

    /// <summary>Updates lane hit-location visuals and note colors for the current song time.</summary>
    public void UpdateVisuals()
    {
        // Use live audio time during playback and the editor playhead while paused.
        var time = AudioSource.isPlaying ? AudioSource.time : seekBar.currentTime;

        var liljon = AddToTheBodyPile.pile;
        var enemies = MapTrack.GetEnemies(time);

        // Clear lane previews that are no longer associated with a beat active at this time.
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


        // Create or refresh each active lane preview, then color its hit points from nearby notes.
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
                    // Add one non-interactive preview image and wire each hit point to editor mouse actions.
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
                }
            }

            var notes = enemy.GetNotes(time, timePad);
            for(int a = 0; a < MapTrack.hitLocationsList[enemy.hitLocationsIndex].points.Count; ++a)
                visualizer.SetColour2D(a, Color.white);

            foreach(var note in notes)
                visualizer.SetColour2D(note.hitLocation, Color.Lerp(Color.green, Color.white, Mathf.InverseLerp((float)note.hitTiming - timePad, (float)note.hitTiming + timePad, (float)time) - .5f));

        }
    }

    /// <summary>Determines whether a value falls within an inclusive numeric interval.</summary>
    /// <param name="val">Value to test.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <returns><see langword="true"/> when the value is within the bounds; otherwise, <see langword="false"/>.</returns>
    bool InRange(double val, double min, double max) => val >= min && val <= max;
}
