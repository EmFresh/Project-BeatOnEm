using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.AppUI.UI;

using UnityEditor.Experimental.GraphView;

using UnityEngine;

using static Unity.Burst.Intrinsics.X86.Avx;

public static class AudioExtentions
{

    public static double AccurateTime(this AudioSource source) =>
        ((double)source.timeSamples / source.clip.frequency);
    public static int TimeMilli(this AudioSource source) =>
        (int)(source.AccurateTime() * 1000);
    public static int SetAccurateTime(this AudioSource source, double time) =>
        source.timeSamples = (int)(time * source.clip.frequency);

    public static double AccurateLength(this AudioClip clip) =>
        ((double)clip.samples / clip.frequency);
    public static int LengthMilli(this AudioClip clip) =>
       (int)(clip.AccurateLength() * 1000);
}

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
    public double spb
    {
        get
        {
            if(_bpm != bpm)
                try { _divider = 1d / bpm; }
                catch { _divider = 0; }

            _bpm = bpm;
            return 60d * _divider;
        }
    }

    //milliseconds per beat
    public int mspb
    {
        get => (int)(spb * 1000);
    }


    double _divider = 0;
    float _bpm = 0;
}

[Serializable]
public class Tempo
{
    //beats per min 
    public BPM bpm = 80;


    //song time stamp
    public double timing = 0;
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }


    public double GetNextBeat(double noteTime)
    {
        var currentTime = noteTime - timing;
        var amount = (int)Math.Floor(currentTime / bpm.spb);
        var val = ((amount + 1) * bpm.spb) + timing;
        var check = ((val) == (noteTime));//needed
        val = check ? (((amount + 2) * bpm.spb) + timing) : val;

        return val;
    }
    public int GetNextBeatMilli(int noteTimeMilli)
    {
        var currentTime = noteTimeMilli - TimingMilli;
        var amount = (currentTime / (bpm.mspb));
        var val = ((amount + 1) * (int)(bpm.mspb)) + TimingMilli;
        var check = ((val / (bpm.mspb)) == (noteTimeMilli / (bpm.mspb)));//needed
        val = check ? (((amount + 2) * (int)(bpm.mspb)) + TimingMilli) : val;

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
    public double timing;
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }

    public static TimeSig HalfTime { get => new TimeSig() { beats = 2, note = 4, timing = 0 }; }
    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    public int GetNextBeat(double noteTime, Tempo tempo)
    {
        var currentTime = noteTime - timing;
        var amount = (int)Math.Floor(currentTime / tempo.bpm.spb);
        var beatsCalc = beats * (note / 4f);
        var check = ((amount + 1) * tempo.bpm.spb > noteTime);
        return Mathf.FloorToInt((check ? (amount + 1) : (amount + 2)) % beatsCalc);

    }
    public int GetNextBeatMilli(int noteTimeMilli, Tempo tempo)
    {
        var currentTime = noteTimeMilli - TimingMilli;
        var amount = (int)Math.Floor(currentTime / (tempo.bpm.spb * 1000));
        var beatsCalc = beats * (note / 4f);
        var check = ((amount + 1) * (tempo.bpm.spb * 1000) > noteTimeMilli);
        return Mathf.FloorToInt((check ? (amount + 1) : (amount + 2)) % beatsCalc);

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

    public void AddTempo(float bpm, double timing) =>
      AddTempo(new Tempo() { bpm = bpm, timing = timing });
    public void AddTempo(Tempo tempo)
    {
        tempos.Add(tempo);
        Sort();
    }
    public void RemoveTempo(double time) =>
        RemoveTempo(GetTempo(time));
    public void RemoveTempo(Tempo tempo)
    {
        tempos.Remove(tempo);
        Sort();
    }

    public void AddTimeSig(int beats, int note, double timing) =>
        AddTimeSig(new TimeSig() { beats = beats, note = note, timing = timing });
    public void AddTimeSig(TimeSig timeSig)
    {
        timeSigs.Add(timeSig);
        Sort();
    }
    public void RemoveTimeSig(double time) =>
       RemoveTimeSig(GetTimeSig(time));
    public void RemoveTimeSig(TimeSig tempo)
    {
        timeSigs.Remove(tempo);
        Sort();
    }

    public Tempo GetTempo(double noteTime)
    {
        if(tempos.Count == 0) return null;

        try { return tempos.Last(t => t.timing <= noteTime); }
        catch { return tempos.First(); }
    }

    public TimeSig GetTimeSig(double noteTime)
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
