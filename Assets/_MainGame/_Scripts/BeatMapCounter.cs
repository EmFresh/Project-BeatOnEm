using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events; 


public class BeatMapCounter : MonoBehaviour
{
    public AudioClip tickSound;
    public AudioClip tockSound;

    public UnityEvent<Tempo, TimeSig> onBeat { get; } = new UnityEvent<Tempo, TimeSig>();
    public UnityEvent<Tempo, TimeSig> onTick { get; } = new UnityEvent<Tempo, TimeSig>();
    public UnityEvent<Tempo, TimeSig> onTock { get; } = new UnityEvent<Tempo, TimeSig>();

    [SerializeField] ISongManager manager;
    [SerializeField] MapEditManager editManager;
    [SerializeField] SongManager songManager;
    [SerializeField] AudioSource source;

    public int audioOffsetMilli = 0; 
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





    int nextBeatTime = 0;
    Tempo currentTempo = null;
    TimeSig currentTimeSig = TimeSig.CommonTime;

    // Update is called once per frame
    void Update()
    { 

        var time = manager.AudioSource.TimeMilli() + audioOffsetMilli;

        if(time >= (nextBeatTime) && currentTempo != null)
        {

            if(currentTimeSig.GetNextBeatMilli(time, currentTempo) - 1 == 0)
                onTick?.Invoke(currentTempo, currentTimeSig);
            else
                onTock?.Invoke(currentTempo, currentTimeSig);

            onBeat?.Invoke(currentTempo, currentTimeSig);

        }

        if(time < nextBeatTime && Mathf.Abs(time - nextBeatTime) < (currentTempo?.bpm.mspb ?? 0)) return;
        // print("change tempo");


        currentTempo = manager.MapTrack.tempoMap.GetTempo((time + currentTempo?.bpm.mspb ?? 0) / 1000d).Clone();
        currentTimeSig = manager.MapTrack.tempoMap.GetTimeSig((time + currentTempo?.bpm.mspb ?? 0) / 1000d).Clone();

        currentTempo.bpm *= currentTimeSig.note / 4f;//adjust tempo to fit the beat divisions of the time signature

        nextBeatTime = currentTempo.GetNextBeatMilli(time);
    }
}


