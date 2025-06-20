using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.Android;

[RequireComponent(typeof(EnemySpawner))]
public class BeatMapCounter : MonoBehaviour
{
    public AudioClip tickSound;
    public AudioClip tockSound;

    public UnityEvent<Tempo, TimeSig> onBeat { get; private set; } = new UnityEvent<Tempo, TimeSig>();
    public UnityEvent<Tempo, TimeSig> onTick { get; private set; } = new UnityEvent<Tempo, TimeSig>();
    public UnityEvent<Tempo, TimeSig> onTock { get; private set; } = new UnityEvent<Tempo, TimeSig>();

    TempoMap tempoMap;
    AudioSource clip;

    private void Awake()
    {
        clip = GetComponent<EnemySpawner>().clip;
        tempoMap = GetComponent<EnemySpawner>().track.tempo;
        tempoMap.Sort();

        onTick.AddListener((tempo, timeSig) =>
        {
            if(tickSound != null)
            {
                clip?.PlayOneShot(tickSound);
                // print("Tick");
            }
        });
        onTock.AddListener((tempo, timeSig) =>
        {
            if(tockSound != null)
            {
                clip?.PlayOneShot(tockSound);
                //    print("tock");
            }
        });
        onBeat.AddListener((tempo, timeSig) =>
        {
            // print(timeSig.GetNextBeat(clip.time, tempo));
        });
    }

    float nextBeatTime = 0f;
    Tempo currentTempo = null;
    TimeSig currentTimeSig = TimeSig.CommonTime;
    // Update is called once per frame
    void Update()
    {
        var time = clip.time;

        if(time >= nextBeatTime && currentTempo != null)
        {

            if(currentTimeSig.GetNextBeat(time, currentTempo)-1 == 0)
                onTick.Invoke(currentTempo, currentTimeSig);
            else
                onTock.Invoke(currentTempo, currentTimeSig);

            onBeat?.Invoke(currentTempo, currentTimeSig);

        }

        if(time < nextBeatTime && Mathf.Abs(time - nextBeatTime) < (currentTempo?.spb ?? 0)) return;

        print("change tempo");

        currentTempo = tempoMap.GetTempo((time + currentTempo?.spb ?? 0));

        nextBeatTime = currentTempo.GetNextBeat(time);

        currentTimeSig = tempoMap.GetTimeSig(nextBeatTime);


    }
}
