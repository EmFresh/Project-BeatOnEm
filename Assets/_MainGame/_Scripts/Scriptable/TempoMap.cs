using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

using UnityEngine;

public static class AudioExtentions
{
    /// <summary>
    /// Helper extensions to obtain accurate time and length values (in seconds or milliseconds)
    /// from Unity AudioSource/AudioClip using sample counts and frequency.
    /// </summary>
    public static double AccurateTime(this AudioSource source) =>
        source.clip == null ? 0 : ((double)source.timeSamples / source.clip.frequency);
    public static int AccurateTimeMilli(this AudioSource source) =>
        (int)(source.AccurateTime() * 1000);

    public static int SetAccurateTime(this AudioSource source, double time) =>
        source.timeSamples = source.clip == null ? 0 : (int)(time * source.clip.frequency);
    public static int SetAccurateTimeMilli(this AudioSource source, int time) =>
        source.timeSamples = source.clip == null ? 0 : (int)(time * source.clip.frequency / 1000d);

    public static double AccurateLength(this AudioClip clip) =>
        clip == null ? 0 : ((double)clip.samples / clip.frequency);
    public static int AccurateLengthMilli(this AudioClip clip) =>
       (int)(clip.AccurateLength() * 1000);
}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Simple wrapper for beats-per-minute values that provides helpers for seconds and milliseconds per beat.
/// </summary>
public partial class BPM
{
    public BPM(float bpm = 60) => this.bpm = bpm;

    public static implicit operator float(BPM bpm) => bpm.bpm;
    public static implicit operator BPM(float bpm) => new BPM(bpm);

    public override string ToString() => bpm.ToString();

    /// <summary>
    /// Raw beats-per-minute value.
    /// </summary>
    public float bpm;
    //1/60 = 0.0166667f

    //seconds per beat
    [IgnoreMember]
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
    [IgnoreMember]
    public int mspb
    {
        get => (int)(spb * 1000);
    }

    [IgnoreMember]
    double _divider = 0;
    [IgnoreMember]
    float _bpm = 0;
}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Represents a tempo change at a specific time in the song and provides helpers to compute next beat timestamps.
/// </summary>
public class Tempo
{
    //beats per min 
    public BPM bpm = 80;


    //song time stamp
    public double timing = 0;
    [IgnoreMember]
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }


    /// <summary>
    /// Compute the time (seconds) of the next beat after the provided note time.
    /// </summary>
    public double GetNextBeat(double noteTime)
    {
        var currentTime = noteTime - timing;
        var amount = (int)Math.Floor(currentTime / bpm.spb);
        var val = ((amount + 1) * bpm.spb) + timing;
        var check = ((val) == (noteTime));//needed

        return check ? (((amount + 2) * bpm.spb) + timing) : val;
    }
    /// <summary>
    /// Compute the time (milliseconds) of the next beat after the provided note time in milliseconds.
    /// </summary>
    public int GetNextBeatMilli(int noteTimeMilli)
    {
        var currentTime = noteTimeMilli - TimingMilli;
        var amount = (currentTime / (bpm.mspb));

        var val = ((amount + 1) * (int)(bpm.mspb)) + TimingMilli;
        var check = ((val) == (noteTimeMilli));//needed

        return check ? (((amount + 2) * (int)(bpm.mspb)) + TimingMilli) : val;
    }

    public override string ToString()
    {
        return
            $"bpm:{bpm}\n" +
            $"timing:{timing}";
    }

    public Tempo Clone() => new Tempo() { bpm = bpm.bpm, timing = timing };

}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Represents a time signature (beats per measure and note value) and provides beat-index helpers.
/// </summary>
public class TimeSig
{
    //numerator
    public int beats;
    //denominator
    public int note;
    //where in the song ?
    public double timing;
    [IgnoreMember]
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }

    public static TimeSig HalfTime { get => new TimeSig() { beats = 2, note = 4, timing = 0 }; }
    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    /// <summary>
    /// Returns the beat index (0-based) within the measure for the provided note time using the given tempo.
    /// </summary>
    public int GetNextBeat(double noteTime, Tempo tempo)
    {
        var currentTime = noteTime - tempo.timing;
        var amount = (int)Math.Floor(currentTime / tempo.bpm.spb);
        var beatsCalc = beats * (note / 4f);

        var val = ((amount + 1) * tempo.bpm.spb) + tempo.timing;
        var check = (val == noteTime);

        return Mathf.FloorToInt((check ? (amount + 2) : (amount + 1)) % beatsCalc);

    }
    /// <summary>
    /// Returns the beat index (0-based) within the measure for the provided note time in milliseconds using the given tempo.
    /// </summary>
    public int GetNextBeatMilli(int noteTimeMilli, Tempo tempo)
    {
        var currentTime = noteTimeMilli - tempo.TimingMilli;
        var amount = (int)Math.Floor((double)currentTime / (tempo.bpm.mspb));
        var beatsCalc = beats * (note / 4f);

        var val = ((amount + 1) * tempo.bpm.mspb) + tempo.TimingMilli;
        var check = (val == noteTimeMilli);

        return Mathf.FloorToInt((check ? (amount + 2) : (amount + 1)) % beatsCalc);
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
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Container for tempo and time signature changes across a song. Provides helpers to manage and query tempo/time signature events.
/// </summary>
public class TempoMap
{
    /// <summary>
    /// List of tempo change entries ordered by timing.
    /// </summary>
    public List<Tempo> tempos;
    /// <summary>
    /// List of time signature changes ordered by timing.
    /// </summary>
    public List<TimeSig> timeSigs;

    public TempoMap() { Init(); }

    /// <summary>
    /// Initialize collections with sensible defaults if empty.
    /// </summary>
    public void Init()
    {
        if(tempos == null)
            tempos = new List<Tempo>();
        if(timeSigs == null)
            timeSigs = new List<TimeSig>();

        if(tempos.Count == 0)
            tempos.Add(new Tempo());
        if(timeSigs.Count == 0)
            timeSigs.Add(TimeSig.CommonTime);

        Sort();
    }

    /// <summary>
    /// Sort the internal lists by timing values.
    /// </summary>
    public void Sort()
    {
        tempos.Sort((a, b) => { return a?.timing.CompareTo(b?.timing) ?? 1; });
        timeSigs.Sort((a, b) => { return a?.timing.CompareTo(b?.timing) ?? 1; });
    }

    public void AddTempo(float bpm, double timing) =>
      AddTempo(new Tempo() { bpm = bpm, timing = timing });
    public void AddTempo(Tempo tempo)
    {
        tempos.Add(tempo);
        Init();
    }
    public void RemoveTempo(double time) =>
        RemoveTempo(GetTempo(time));
    public void RemoveTempo(Tempo tempo)
    {
        tempos.Remove(tempo);
        Init();
    }

    public void AddTimeSig(int beats, int note, double timing) =>
        AddTimeSig(new TimeSig() { beats = beats, note = note, timing = timing });
    public void AddTimeSig(TimeSig timeSig)
    {
        timeSigs.Add(timeSig);
        Init();
    }
    public void RemoveTimeSig(double time) =>
       RemoveTimeSig(GetTimeSigs(time)?[0]);
    public void RemoveTimeSig(TimeSig tempo)
    {
        timeSigs.Remove(tempo);
        Init();
    }

    /// <summary>
    /// Get the most-recent tempo effective at the specified time.
    /// </summary>
    public Tempo GetTempo(double noteTime)
    {
        if(tempos.Count == 0) return null;

        try { return tempos.Last(t => t.timing <= noteTime); }
        catch { return tempos[0]; }
    }
    public List<Tempo> GetTempos(double noteTime)
    {
        if(tempos.Count == 0) return new List<Tempo>() { };

        noteTime = GetTempo(noteTime).timing;

        try { return tempos.Where(t => t.timing == noteTime).ToList(); }
        catch { return new List<Tempo>() { }; }
    }

    public TimeSig GetTimeSig(double noteTime)
    {
        if(timeSigs.Count == 0) return null;

        try { return timeSigs.Last(t => t.timing <= noteTime); }
        catch { return timeSigs[0]; }

    }
    public List<TimeSig> GetTimeSigs(double noteTime)
    {
        if(timeSigs.Count == 0) return null;
        noteTime = GetTimeSig(noteTime).timing;

        try { return timeSigs.Where(t => t.timing == noteTime).ToList(); }
        catch { return null; }
    }

    public void Clear()
    {
        tempos.Clear();
        timeSigs.Clear();
        Init();
    }
    public TempoMap Clone() =>
        new TempoMap()
        {
            tempos = tempos.Select(t => t?.Clone()).ToList(),
            timeSigs = timeSigs.Select(t => t?.Clone()).ToList()
        };


}
