using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem.Interactions;

 

[Serializable]
public class Tempo
{
    //beats per min
    public uint bpm = 0;
    //seconds per beat
    public float spb { get { try { return 60f / bpm; } catch { return 0; } } }
    //song time stamp
    public float timing = 0;


    public float GetNextBeat(float noteTime)
    {
        var currentTime = noteTime - timing;

        return ((MathF.Floor(currentTime / spb) + 1) * spb) + timing;
    }

    public override string ToString()
    {
        return 
            $"bpm:{bpm}\n" +
            $"timing:{timing}";
    }
}

[Serializable]
public struct TimeSig
{
    //numerator
    public int beats;
    //denominator
    public int note;
    //where in the song ?
    public int timing;

    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    public float GetNextBeat(float noteTime, Tempo tempo)
    {
        var currentTime = noteTime - timing;
        return (MathF.Floor(currentTime / tempo.spb) + 1) % beats;
    }

    public override string ToString()
    {
        return
            $"beats:{beats}\n" +
            $"note:{note}\n" +
            $"timing:{timing}";
    }
}

[CreateAssetMenu(menuName = "ScriptableObject/TempoMap")]
public class TempoMap : ScriptableObject
{
    public List<Tempo> tempos { get; private set; }
    public List<TimeSig> timeSigs { get; private set; }

    private void OnEnable()
    {
        tempos = new List<Tempo>();
        timeSigs = new List<TimeSig>();

        tempos.Add(new Tempo() { bpm = 80 });
        timeSigs.Add(TimeSig.CommonTime);

        tempos.Sort((a, b) => { return a.timing.CompareTo(b.timing); });
        timeSigs.Sort((a, b) => { return a.timing.CompareTo(b.timing); });
    }
     
    public Tempo GetTempo(float noteTime)
    {
        if(tempos.Count == 0) return null;

        tempos.Sort((a, b) => { return a.timing.CompareTo(b.timing); });

        foreach(var tmpo in tempos)
            if(tmpo.timing >= noteTime)
                return tmpo;

        return tempos.Last() ;
    }

    public TimeSig GetTimeSig(float noteTime)
    {
        if(timeSigs.Count == 0) return TimeSig.CommonTime;

        timeSigs.Sort((a, b) => { return a.timing.CompareTo(b.timing); });

        foreach(var tmpo in timeSigs)
            if(tmpo.timing >= noteTime)
                return tmpo;

        return timeSigs.Last();
    }

    public void Clear()
    {
        tempos.Clear();
        timeSigs.Clear();
    }
}
