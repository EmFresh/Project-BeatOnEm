using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public interface ISongManager
{
    public SongTrack MapTrack { get; }
    public AudioSource AudioSource { get; }
    public List<GameObject> Lanes { get; }

    public void SaveFile(string path, MessagePackSerializerOptions MPOptions = null)
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


            //  ser = JsonUtility.ToJson(ser, true);

            if(!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ser);

            //  print($"Saved file [{phase.ToString()}]");
        }
        catch(Exception e)
        {
            Debug.LogError($"{e}");
        }
        //  print(JsonUtility.ToJson(pile.ocupado,true));

        // return true;
    }

    public SongTrack LoadFile(string path, MessagePackSerializerOptions MPOptions = null)
    {
        if(!File.Exists(path)) return null;

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

            var json = File.ReadAllText(path);
            var track =
            MessagePackSerializer.Deserialize<SongTrack>(
                MessagePackSerializer.ConvertFromJson(json, MPOptions), MPOptions);

            return track;
            //JsonConvert.DeserializeObject<SongTrack>(json);      
        }
        catch(Exception e)
        {
            Debug.LogError($"{e}");
        }

        return null;
    }

}

[RequireComponent(typeof(AudioSource))]
public class MapEditManager : ActionHistory, ISongManager
{
    [SerializeField] SongTrack mapTrack;
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<GameObject> lanes;
    [SerializeField] string filePath;
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
    public List<GameObject> Lanes => lanes;

    private void Start()
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

        if(!string.IsNullOrWhiteSpace(filePath))
            LoadFile(filePath);

        UpdateVisuals();
    }

    private void Update()
    {
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if(!AudioSource.isPlaying)
                AudioSource.SetAccurateTime(seekBar.currentTime);

            if(AudioSource.isPlaying) AudioSource.Pause();
            else AudioSource.Play();

        }

        if(AudioSource.isPlaying)
            UpdateVisuals();

    }

    public void SaveFile(string path)
    {
        try
        {
            var phase = playerInput?.actions?.FindAction("Save")?.phase;
            if((int)phase > 1 && phase != InputActionPhase.Performed) return;

            ((ISongManager)this).SaveFile(path, MPOptions);

            print($"Saved file [{phase.ToString()}]");
        }
        catch(Exception e)
        {
            Debug.LogError($"{e}");
        }
        //  print(JsonUtility.ToJson(pile.ocupado,true));

        // return true;
    }

    public bool LoadFile(string path)
    {
        if(!File.Exists(path)) return false;


        mapTrack = ((ISongManager)this).LoadFile(path, MPOptions);

        tempoMarkManager?.MarkerUpdate();
        UpdateVisuals();

        print("Loaded file");

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
                break;
            case ActionType.Redo:
                AddTempo(BPM, time, nested: true);
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
                break;
            case ActionType.Redo:
                AddTimeSig(beats, note, time, nested: true);
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
                break;
            case ActionType.Redo:
                EditTempo(tempo, time, nested: true);
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
                break;
            case ActionType.Redo:
                EditTimeSig(beats, note, time, nested: true);
                break;
            }
        }, nested);
    }

    public void RemoveTempo(double time, bool nested = false)
    {
        var oldTempo = MapTrack.tempoMap.GetTempo(time).Clone();
        MapTrack.tempoMap.RemoveTempo(time);
        tempoMarkManager.MarkerUpdate();

        AddToActionHistory(t =>
        {
            switch(t)
            {
            case ActionType.Undo:
                AddTempo(oldTempo.bpm, oldTempo.timing, nested: true);
                break;
            case ActionType.Redo:
                RemoveTempo(time, nested: true);
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
                break;
            case ActionType.Redo:
                RemoveTimeSig(time, nested: true);
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
                break;
            case ActionType.Redo:
                AddEnemy(lane, hitLocationsIndex, duration, spawnTime, nested: true);
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
                break;
            case ActionType.Redo:
                RemoveEnemy(lane, nested: true);
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
                break;
            case ActionType.Redo:
                AddNote(lane, hitLocation, hitType, nested: true);
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
                break;
            case ActionType.Redo:
                RemoveNote(lane, time, hitLocation, hitType, timePad, nested: true);
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
                    visualizer.scale = Vector3.one * 100;

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
                //visualizer.SetColour2D(note.hitLocation, Color.green);
                visualizer.SetColour2D(note.hitLocation, Color.Lerp(Color.green, Color.white, Mathf.InverseLerp((float)note.hitTiming - timePad, (float)note.hitTiming + timePad, (float)time) - .5f));



            //  print("Completed updating visuals");
        }
    }

    bool InRange(double val, double min, double max) => val >= min && val <= max;
}
