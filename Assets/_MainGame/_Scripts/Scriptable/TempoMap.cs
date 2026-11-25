/*
 File: TempoMap.cs
 Brief: Defines BPM, Tempo, TimeSig and TempoMap helper types used to represent tempo and time signature changes
 across a song. Also includes Audio extension helpers for accurate timing calculations.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using MessagePack;

using UnityEngine;

/// <summary>Provides sample-accurate time and length helpers for Unity audio sources and clips.</summary>
public static class AudioExtentions
{
    /// <summary>
    /// Helper extensions to obtain accurate time and length values (in seconds or milliseconds)
    /// from Unity AudioSource/AudioClip using sample counts and frequency.
    /// </summary>
    /// <summary>Gets playback time in seconds from the audio sample position.</summary>
    /// <param name="source">Audio source whose playback position is queried.</param>
    /// <returns>Playback time in seconds, or zero when the source has no clip.</returns>
    public static double AccurateTime(this AudioSource source) =>
        source.clip == null ? 0 : ((double)source.timeSamples / source.clip.frequency);
    /// <summary>
    /// Get accurate playback time in seconds using sample counts to avoid Unity's sample rounding.
    /// </summary>
    /// <summary>Gets playback time in milliseconds from the audio sample position.</summary>
    /// <param name="source">Audio source whose playback position is queried.</param>
    /// <returns>Playback time in milliseconds.</returns>
    public static int AccurateTimeMilli(this AudioSource source) =>
        (int)(source.AccurateTime() * 1000);

    /// <summary>
    /// <summary>Sets playback time in seconds by converting the time to an audio sample position.</summary>
    /// <param name="source">Audio source whose playback position is changed.</param>
    /// <param name="time">Playback time in seconds.</param>
    /// <returns>The assigned sample position.</returns>
    public static int SetAccurateTime(this AudioSource source, double time) =>
        source.timeSamples = source.clip == null ? 0 : (int)(time * source.clip.frequency);
    /// <summary>
    /// Set the audio playback time precisely (seconds) using samples.
    /// </summary>
    /// <summary>Sets playback time in milliseconds by converting the time to an audio sample position.</summary>
    /// <param name="source">Audio source whose playback position is changed.</param>
    /// <param name="time">Playback time in milliseconds.</param>
    /// <returns>The assigned sample position.</returns>
    public static int SetAccurateTimeMilli(this AudioSource source, int time) =>
        source.timeSamples = source.clip == null ? 0 : (int)(time * source.clip.frequency / 1000d);
    /// <summary>
    /// <summary>Gets the audio clip length in seconds from its sample count.</summary>
    /// <param name="clip">Audio clip whose length is queried.</param>
    /// <returns>Clip length in seconds, or zero when the clip is null.</returns>
    public static double AccurateLength(this AudioClip clip) =>
        clip == null ? 0 : ((double)clip.samples / clip.frequency);
    /// <summary>
    /// Get accurate clip length in seconds using sample counts.
    /// </summary>
    /// <summary>Gets the audio clip length in milliseconds from its sample count.</summary>
    /// <param name="clip">Audio clip whose length is queried.</param>
    /// <returns>Clip length in milliseconds.</returns>
    public static int AccurateLengthMilli(this AudioClip clip) =>
       (int)(clip.AccurateLength() * 1000);
    /// <summary>
    /// Get accurate clip length in milliseconds.
    /// </summary>
}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Simple wrapper for beats-per-minute values that provides helpers for seconds and milliseconds per beat.
/// </summary>
public partial class BPM
{
    /// <summary>Creates a beats-per-minute value.</summary>
    /// <param name="bpm">Initial tempo in beats per minute; defaults to 60.</param>
    public BPM(float bpm = 60) => this.bpm = bpm;

    /// <summary>Converts a BPM wrapper to its raw floating-point value.</summary>
    /// <param name="bpm">Value to convert.</param>
    /// <returns>The raw beats-per-minute value.</returns>
    public static implicit operator float(BPM bpm) => bpm.bpm;
    /// <summary>Creates a BPM wrapper from a floating-point value.</summary>
    /// <param name="bpm">Beats-per-minute value.</param>
    /// <returns>A new BPM wrapper.</returns>
    public static implicit operator BPM(float bpm) => new BPM(bpm);

    /// <summary>Returns the beats-per-minute value as text.</summary>
    /// <returns>The raw BPM value formatted as a string.</returns>
    public override string ToString() => bpm.ToString();

    /// <summary>
    /// Raw beats-per-minute value.
    /// </summary>
    public float bpm;
    //1/60 = 0.0166667f

    /// <summary>Gets the duration of one beat in seconds.</summary>
    [IgnoreMember]
    public double spb
    {
        get
        {
            // Recompute the reciprocal only when the underlying BPM value changes.
            if(_bpm != bpm)
                try { _divider = 1d / bpm; }
                catch { _divider = 0; }

            _bpm = bpm;
            // Convert beats per minute to seconds per beat.
            return 60d * _divider;
        }
    }

    /// <summary>Gets the duration of one beat in milliseconds.</summary>
    [IgnoreMember]
    public int mspb
    {
        get => (int)(spb * 1000);
    }

    /// <summary>Cached reciprocal BPM value used to calculate beat duration.</summary>
    [IgnoreMember]
    double _divider = 0;
    /// <summary>Cached BPM value associated with <see cref="_divider"/>.</summary>
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
    /// <summary>Beats-per-minute value for this tempo entry.</summary>
    public BPM bpm = 80;


    /// <summary>Song time in seconds at which this tempo entry takes effect.</summary>
    public double timing = 0;
    [IgnoreMember]
    /// <summary>Gets or sets the tempo entry time in milliseconds.</summary>
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }


    /// <summary>
    /// Compute the time (seconds) of the next beat after the provided note time.
    /// </summary>
    /// <param name="noteTime">Note time in seconds.</param>
    /// <returns>The next beat time in seconds.</returns>
    public double GetNextBeat(double noteTime)
    {
        // Shift to the tempo entry's origin, then count complete beat intervals.
        var currentTime = noteTime - timing;
        var amount = (int)Math.Floor(currentTime / bpm.spb);
        // Advance to the next boundary; an exact-boundary input advances one additional beat.
        var val = ((amount + 1) * bpm.spb) + timing;
        var check = ((val) == (noteTime));//needed

        return check ? (((amount + 2) * bpm.spb) + timing) : val;
    }
    /// <summary>
    /// Compute the time (milliseconds) of the next beat after the provided note time in milliseconds.
    /// </summary>
    /// <param name="noteTimeMilli">Note time in milliseconds.</param>
    /// <returns>The next beat time in milliseconds.</returns>
    public int GetNextBeatMilli(int noteTimeMilli)
    {
        // Work in integer milliseconds relative to this tempo entry's start time.
        var currentTime = noteTimeMilli - TimingMilli;
        var amount = (currentTime / (bpm.mspb));

        // Advance past the current beat boundary, including when input is already on it.
        var val = ((amount + 1) * (int)(bpm.mspb)) + TimingMilli;
        var check = ((val) == (noteTimeMilli));//needed

        return check ? (((amount + 2) * (int)(bpm.mspb)) + TimingMilli) : val;
    }

    /// <summary>Returns the tempo and effective time as text.</summary>
    /// <returns>A string containing this tempo entry's BPM and time.</returns>
    public override string ToString()
    {
        return
            $"bpm:{bpm}\n" +
            $"timing:{timing}";
    }

    /// <summary>Creates a copy of this tempo entry.</summary>
    /// <returns>A new tempo entry with the same BPM and timing.</returns>
    public Tempo Clone() => new Tempo() { bpm = bpm.bpm, timing = timing };

}

[Serializable]
[MessagePackObject(true, AllowPrivate = true)]
/// <summary>
/// Represents a time signature (beats per measure and note value) and provides beat-index helpers.
/// </summary>
public class TimeSig
{
    /// <summary>Number of beats in each measure.</summary>
    public int beats;
    /// <summary>Note value represented by each beat.</summary>
    public int note;
    /// <summary>Song time in seconds at which this time signature takes effect.</summary>
    public double timing;
    [IgnoreMember]
    /// <summary>Gets or sets the time-signature entry time in milliseconds.</summary>
    public int TimingMilli { get => (int)(timing * 1000); set => timing = value / 1000; }

    /// <summary>Creates a 2/4 time signature effective at the start of the song.</summary>
    public static TimeSig HalfTime { get => new TimeSig() { beats = 2, note = 4, timing = 0 }; }
    /// <summary>Creates a 4/4 time signature effective at the start of the song.</summary>
    public static TimeSig CommonTime { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }


    /// <summary>
    /// Returns the beat index (0-based) within the measure for the provided note time using the given tempo.
    /// </summary>
    /// <param name="noteTime">Note time in seconds.</param>
    /// <param name="tempo">Tempo used to calculate the beat position.</param>
    /// <returns>The zero-based beat index within the measure.</returns>
    public int GetNextBeat(double noteTime, Tempo tempo)
    {
        // Count completed beat intervals since the active tempo began.
        var currentTime = noteTime - tempo.timing;
        var amount = (int)Math.Floor(currentTime / tempo.bpm.spb);
        // Convert the signature to its effective number of quarter-note beat positions.
        var beatsCalc = beats * (note / 4f);

        // Select the upcoming beat position, then wrap it to the current measure.
        var val = ((amount + 1) * tempo.bpm.spb) + tempo.timing;
        var check = (val == noteTime);

        return Mathf.FloorToInt((check ? (amount + 2) : (amount + 1)) % beatsCalc);

    }
    /// <summary>
    /// Returns the beat index (0-based) within the measure for the provided note time in milliseconds using the given tempo.
    /// </summary>
    /// <param name="noteTimeMilli">Note time in milliseconds.</param>
    /// <param name="tempo">Tempo used to calculate the beat position.</param>
    /// <returns>The zero-based beat index within the measure.</returns>
    public int GetNextBeatMilli(int noteTimeMilli, Tempo tempo)
    {
        // Compute completed beat intervals using the millisecond representation.
        var currentTime = noteTimeMilli - tempo.TimingMilli;
        var amount = (int)Math.Floor((double)currentTime / (tempo.bpm.mspb));
        var beatsCalc = beats * (note / 4f);

        // Select the next beat index and wrap it at the measure length.
        var val = ((amount + 1) * tempo.bpm.mspb) + tempo.TimingMilli;
        var check = (val == noteTimeMilli);

        return Mathf.FloorToInt((check ? (amount + 2) : (amount + 1)) % beatsCalc);
    }

    /// <summary>Returns the time-signature values and effective time as text.</summary>
    /// <returns>A string containing the numerator, denominator, and timing.</returns>
    public override string ToString()
    {
        return
            $"beats:{beats}\n" +
            $"note:{note}\n" +
            $"timing:{timing}";
    }

    /// <summary>Creates a copy of this time-signature entry.</summary>
    /// <returns>A new entry with the same beats, note value, and timing.</returns>
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

    /// <summary>Creates a tempo map and initializes its collections and default entries.</summary>
    public TempoMap() { Init(); }

    /// <summary>
    /// Initialize collections with sensible defaults if empty.
    /// </summary>
    public void Init()
    {
        // Ensure both collections exist before adding their required defaults.
        if(tempos == null)
            tempos = new List<Tempo>();
        if(timeSigs == null)
            timeSigs = new List<TimeSig>();

        if(tempos.Count == 0)
            tempos.Add(new Tempo());
        if(timeSigs.Count == 0)
            timeSigs.Add(TimeSig.CommonTime);

        // Keep effective-entry lookups ordered by their start times.
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

    /// <summary>
    /// Add a tempo entry by bpm and timing values.
    /// </summary>
    /// <param name="bpm">Beats per minute to add.</param>
    /// <param name="timing">Timing (seconds) at which the tempo becomes effective.</param>
    public void AddTempo(float bpm, double timing) =>
      AddTempo(new Tempo() { bpm = bpm, timing = timing });

    /// <summary>
    /// Add a Tempo object to the tempo map and reinitialize internal state.
    /// </summary>
    /// <param name="tempo">Tempo instance to add.</param>
    public void AddTempo(Tempo tempo)
    {
        // Add the change, then restore defaults/order through shared initialization.
        tempos.Add(tempo);
        Init();
    }

    /// <summary>
    /// Remove the tempo effective at the specified time.
    /// </summary>
    /// <param name="time">Time in seconds used to locate the tempo to remove.</param>
    public void RemoveTempo(double time) =>
        RemoveTempo(GetTempo(time));

    /// <summary>
    /// Remove the specified Tempo instance from the map and reinitialize.
    /// </summary>
    /// <param name="tempo">Tempo instance to remove.</param>
    public void RemoveTempo(Tempo tempo)
    {
        // Remove the selected entry and reinitialize in case the list became empty.
        tempos.Remove(tempo);
        Init();
    }

    /// <summary>
    /// Add a time signature entry using numeric values.
    /// </summary>
    /// <param name="beats">Numerator (beats per measure).</param>
    /// <param name="note">Denominator note value (e.g., 4 for quarter notes).</param>
    /// <param name="timing">Timing (seconds) at which the time signature becomes effective.</param>
    public void AddTimeSig(int beats, int note, double timing) =>
        AddTimeSig(new TimeSig() { beats = beats, note = note, timing = timing });

    /// <summary>
    /// Add a TimeSig instance to the map and reinitialize internal state.
    /// </summary>
    /// <param name="timeSig">TimeSig instance to add.</param>
    public void AddTimeSig(TimeSig timeSig)
    {
        // Add the change, then restore defaults/order through shared initialization.
        timeSigs.Add(timeSig);
        Init();
    }

    /// <summary>
    /// Remove the time signature effective at the given time.
    /// </summary>
    /// <param name="time">Time in seconds used to locate the time signature to remove.</param>
    public void RemoveTimeSig(double time) =>
       RemoveTimeSig(GetTimeSigs(time)?[0]);

    /// <summary>
    /// Remove the specified TimeSig instance and reinitialize.
    /// </summary>
    /// <param name="tempo">TimeSig instance to remove.</param>
    public void RemoveTimeSig(TimeSig tempo)
    {
        // Remove the selected entry and reinitialize in case the list became empty.
        timeSigs.Remove(tempo);
        Init();
    }

    /// <summary>
    /// Get the most-recent tempo effective at the specified time.
    /// </summary>
    /// <param name="noteTime">Time in seconds to query.</param>
    /// <returns>The effective tempo entry, or null when the tempo map is empty.</returns>
    public Tempo GetTempo(double noteTime)
    {
        if(tempos.Count == 0) return null;

        // Use the latest entry already in effect; before the first entry, use the first one.
        try { return tempos.Last(t => t.timing <= noteTime); }
        catch { return tempos[0]; }
    }
    /// <summary>
    /// Get all tempo entries that have the same timing as the tempo effective at the specified time.
    /// </summary>
    /// <param name="noteTime">Time in seconds to query.</param>
    /// <returns>List of Tempo entries matching the effective timing.</returns>
    public List<Tempo> GetTempos(double noteTime)
    {
        if(tempos.Count == 0) return new List<Tempo>() { };

        // Resolve the active change time, then return every entry sharing that timestamp.
        noteTime = GetTempo(noteTime).timing;

        try { return tempos.Where(t => t.timing == noteTime).ToList(); }
        catch { return new List<Tempo>() { }; }
    }

    /// <summary>
    /// Get the most recent TimeSig effective at the specified time.
    /// </summary>
    /// <param name="noteTime">Time in seconds to query.</param>
    /// <returns>The TimeSig effective at the time, or null if none exist.</returns>
    public TimeSig GetTimeSig(double noteTime)
    {
        if(timeSigs.Count == 0) return null;

        // Use the latest entry already in effect; before the first entry, use the first one.
        try { return timeSigs.Last(t => t.timing <= noteTime); }
        catch { return timeSigs[0]; }

    }

    /// <summary>
    /// Get all TimeSig entries that share the same timing as the effective TimeSig at the specified time.
    /// </summary>
    /// <param name="noteTime">Time in seconds to query.</param>
    /// <returns>List of TimeSig entries matching the effective timing.</returns>
    public List<TimeSig> GetTimeSigs(double noteTime)
    {
        if(timeSigs.Count == 0) return null;
        // Resolve the active signature time, then return all entries at that time.
        noteTime = GetTimeSig(noteTime).timing;

        try { return timeSigs.Where(t => t.timing == noteTime).ToList(); }
        catch { return null; }
    }

    /// <summary>
    /// Clear all tempo and time signature entries and reinitialize the map.
    /// </summary>
    public void Clear()
    {
        // Empty both collections; initialization repopulates the default tempo and meter.
        tempos.Clear();
        timeSigs.Clear();
        Init();
    }

    /// <summary>
    /// Create a deep-ish clone of the TempoMap including cloned Tempo and TimeSig entries.
    /// </summary>
    /// <returns>A cloned TempoMap instance.</returns>
    public TempoMap Clone() =>
        new TempoMap()
        {
            // Clone entries so edits to the new map do not mutate the original entries.
            tempos = tempos.Select(t => t?.Clone()).ToList(),
            timeSigs = timeSigs.Select(t => t?.Clone()).ToList()
        };


}
