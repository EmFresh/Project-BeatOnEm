using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[Serializable]
public class BPM
{
    public BPM(float bpm = 60) => this.bpm = bpm;

    public static implicit operator float(BPM bpm) => bpm.bpm;
    public static implicit operator BPM(float bpm) => new BPM(bpm);
    
    public string toString() => bpm.ToString();

    public float bpm;
    //1/60 = 0.0166667f

    //seconds per beat
    public float spb
    {
        get
        {
            if(_bpm != bpm)
                try { diviser = 1f / bpm; }
                catch { diviser = 0; }

            _bpm = bpm;
            return 60f * diviser;
        }
    }

    [HideInInspector]
    public float diviser = 0;

    [HideInInspector]
    public float _bpm = 0;
}

[Serializable]
public class Tempo
{
    //beats per min 
    public BPM bpm = 80;


    //song time stamp
    public float timing = 0;


    public float GetNextBeat(float noteTime)
    {
        var currentTime = noteTime - timing;

        return (float)((Math.Floor(currentTime / (double)bpm.spb) + 1) * bpm.spb) + timing;
    }

    public override string ToString()
    {
        return
            $"bpm:{bpm}\n" +
            $"timing:{timing}";
    }

    public Tempo Clone() => new Tempo() { bpm = bpm, timing = timing };

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

    public static TimeSig HalfTime { get => new TimeSig() { beats = 2, note = 4, timing = 0 }; }
    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    public float GetNextBeat(float noteTime, Tempo tempo)
    {
        var currentTime = noteTime - timing;
        return (MathF.Floor(currentTime / tempo.bpm.spb) + 1) % beats;
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
    public List<Tempo> tempos;
    public List<TimeSig> timeSigs;

    private void OnEnable()
    {
        if(tempos == null)
            tempos = new List<Tempo>();
        if(timeSigs == null)
            timeSigs = new List<TimeSig>();

        if(tempos.Count == 0)
            tempos.Add(new Tempo() { bpm = 80 });
        if(timeSigs.Count == 0)
            timeSigs.Add(TimeSig.CommonTime);

        Sort();
    }

    public void Sort()
    {
        tempos.Sort((a, b) => { return a?.timing.CompareTo(b?.timing) ?? 1; });
        timeSigs.Sort((a, b) => { return a.timing.CompareTo(b.timing); });
    }

    public Tempo GetTempo(float noteTime)
    {
        if(tempos.Count == 0) return null;

        foreach(var tmpo in tempos)
            if(tmpo.timing >= noteTime)
                return tmpo;

        return tempos.Last();
    }

    public TimeSig GetTimeSig(float noteTime)
    {
        if(timeSigs.Count == 0) return TimeSig.CommonTime;

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
