using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Experimental.GraphView;

using UnityEngine;

using static Unity.Burst.Intrinsics.X86.Avx;

[Serializable]
public class BPM
{
    public BPM(float bpm = 60) => this.bpm = bpm;

    public static implicit operator float(BPM bpm) => bpm.bpm;
    public static implicit operator BPM(float bpm) => new BPM(bpm);

    public override string ToString() => bpm.ToString();

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

        var amount = Mathf.FloorToInt(currentTime / bpm.spb);
        var val = ((amount + 1) * bpm.spb) + timing;
        var check = (Mathf.FloorToInt(val / bpm.spb) == Mathf.FloorToInt(noteTime / bpm.spb));//needed
        val = check ? (((amount + 2) * bpm.spb) + timing) : val;

        return val;
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
public class TimeSig
{
    //numerator
    public int beats;
    //denominator
    public int note;
    //where in the song ?
    public float timing;

    public static TimeSig HalfTime { get => new TimeSig() { beats = 2, note = 4, timing = 0 }; }
    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    public float GetNextBeat(float noteTime, Tempo tempo)
    {
        var currentTime = noteTime - timing;
        return (Mathf.FloorToInt(currentTime / tempo.bpm.spb) + 1) % beats;

    }

    public override string ToString()
    {
        return
            $"beats:{beats}\n" +
            $"note:{note}\n" +
            $"timing:{timing}";
    }

    public TimeSig Clone() => new TimeSig() { beats = beats, note = note, timing = timing };
}

[Serializable]
public class TempoMap
{
    public List<Tempo> tempos;
    public List<TimeSig> timeSigs;

    public TempoMap() { Init(); }

    public void Init()
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
        timeSigs.Sort((a, b) => { return a?.timing.CompareTo(b.timing) ?? 1; });
    }

    public void AddTempo(float bpm, float timing) =>
      AddTempo(new Tempo() { bpm = bpm, timing = timing });
    public void AddTempo(Tempo tempo)
    {
        tempos.Add(tempo);
        Sort();
    }
    public void RemoveTempo(float time) =>
        RemoveTempo(GetTempo(time));
    public void RemoveTempo(Tempo tempo)
    {
        tempos.Remove(tempo);
        Sort();
    }

    public void AddTimeSig(int beats, int note, float timing) =>
        AddTimeSig(new TimeSig() { beats = beats, note = note, timing = timing });
    public void AddTimeSig(TimeSig timeSig)
    {
        timeSigs.Add(timeSig);
        Sort();
    }
    public void RemoveTimeSig(float time) =>
       RemoveTimeSig(GetTimeSig(time));
    public void RemoveTimeSig(TimeSig tempo)
    {
        timeSigs.Remove(tempo);
        Sort();
    }

    public Tempo GetTempo(float noteTime)
    {
        if(tempos.Count == 0) return null;

        try { return tempos.Last(t => t.timing <= noteTime); }
        catch { return tempos.First(); }
    }

    public TimeSig GetTimeSig(float noteTime)
    {
        if(timeSigs.Count == 0) return null;

        try { return timeSigs.Last(t => t.timing <= noteTime); }
        catch { return timeSigs.First(); }

    }

    public void Clear()
    {
        tempos.Clear();
        timeSigs.Clear();
    }
}
