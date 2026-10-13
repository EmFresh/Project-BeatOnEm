/*
 File: BeatMapCounter.cs
 Brief: Manages counting and display of beats within the active beatmap; utility used by the editor or runtime to report statistics.
*/

using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Tracks beats based on the tempo map and raises tick/tock/beat events for other systems to react to.
/// </summary>
public class BeatMapCounter : MonoBehaviour
{
    /// <summary>Audio clip played for beats classified as ticks.</summary>
    public AudioClip tickSound;
    /// <summary>Audio clip played for beats classified as tocks.</summary>
    public AudioClip tockSound;

    /// <summary>Event raised whenever the counter reaches a beat.</summary>
    public UnityEvent<Tempo, TimeSig> onBeat { get; } = new UnityEvent<Tempo, TimeSig>();
    /// <summary>Event raised for the first beat position in a measure.</summary>
    public UnityEvent<Tempo, TimeSig> onTick { get; } = new UnityEvent<Tempo, TimeSig>();
    /// <summary>Event raised for subsequent beat positions in a measure.</summary>
    public UnityEvent<Tempo, TimeSig> onTock { get; } = new UnityEvent<Tempo, TimeSig>();

    /// <summary>Song manager supplying the map and audio source.</summary>
    [SerializeField] ISongManager manager;
    /// <summary>Optional map editor manager used as the song manager.</summary>
    [SerializeField] MapEditManager editManager;
    /// <summary>Optional runtime song manager used as the song manager.</summary>
    [SerializeField] SongManager songManager;
    /// <summary>Audio source field retained for inspector configuration.</summary>
    [SerializeField] AudioSource source;

    /// <summary>Millisecond offset applied to the audio time before beat evaluation.</summary>
    public int audioOffsetMilli = 0;
    /// <summary>
    /// Initialize the beat counter and hook default audio feedback listeners.
    /// Runs once when the component awakens.
    /// </summary>
    private async void Awake()
    {
        manager = editManager ? editManager : songManager ? songManager : null;

        manager.MapTrack.tempoMap.Sort();

        onTick.AddListener((tempo, timeSig) =>
        {
            if(tickSound != null)
            {
                (manager.AudioSource)?.PlayOneShot(tickSound, 2);
                // print("Tick");
            }
        });
        onTock.AddListener((tempo, timeSig) =>
        {
            if(tockSound != null)
            {
                (manager.AudioSource)?.PlayOneShot(tockSound, 2);
                //    print("tock");
            }
        });
        onBeat.AddListener((tempo, timeSig) =>
        {
            // print(timeSig.GetNextBeat(clip.time, tempo));
            // print($"Time Samples: {(manager.AudioSource)?.timeSamples}" +
            //     $"Time Samples/Frequency: {(manager.AudioSource)?.timeSamples * 1000 / manager.AudioSource.clip.frequency}");
        });

    }





    /// <summary>Time in milliseconds at which the next beat is expected.</summary>
    int nextBeatTime = 0;
    /// <summary>Tempo currently used to evaluate the beat sequence.</summary>
    Tempo currentTempo = null;
    /// <summary>Time signature currently used to classify beats in the measure.</summary>
    TimeSig currentTimeSig = TimeSig.CommonTime;

    /// <summary>
    /// Polls the audio playback position and fires tick/tock/beat events when beats are reached.
    /// </summary>
    // Update is called once per frame
    void Update()
    {

        var time = manager.AudioSource.AccurateTimeMilli() + audioOffsetMilli;

        if(time >= (nextBeatTime) && currentTempo != null)
        {

            if(currentTimeSig.GetNextBeatMilli(time, currentTempo) - 1 == 0)
                onTick?.Invoke(currentTempo, currentTimeSig);
            else
                onTock?.Invoke(currentTempo, currentTimeSig);

            onBeat?.Invoke(currentTempo, currentTimeSig);

        }

        if(time < nextBeatTime && Mathf.Abs(nextBeatTime - time) < (currentTempo?.bpm?.mspb ?? 0)) return;
        // print("change tempo");


        currentTempo = manager.MapTrack.tempoMap.GetTempo((time + currentTempo?.bpm.mspb ?? 0) / 1000d).Clone();
        currentTimeSig = manager.MapTrack.tempoMap.GetTimeSig((time + currentTempo?.bpm.mspb ?? 0) / 1000d).Clone();

        currentTempo.bpm *= currentTimeSig.note / (float)currentTimeSig.beats;//adjust tempo to fit the beat divisions of the time signature

        nextBeatTime = currentTempo.GetNextBeatMilli(time);
    }
}


