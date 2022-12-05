using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[Serializable]
public class BeatMeasure
{
	public float beat = 0;
	public float time = 0;
	public int measure = 0;

	override
	public string ToString()
	{
		return $"{beat} : {measure}";
	}
}

[Serializable]
public class Tempo
{
	//beats per min
	public uint bpm = 0;
	//seconds per beat
	public float spb { get { try { return 60 / bpm; } catch { return 0; } } }
	//song time stamp
	public float timing = 0;

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

	public static TimeSig defaultSig { get => new TimeSig() { beats = 4, note = 4, timing = 0 }; }
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
		timeSigs.Add(TimeSig.defaultSig);
	}


	public BeatMeasure GetBeatMeasure(float noteTime)
	{
		if(tempos.Count == 0 || timeSigs.Count == 0) return null;
		BeatMeasure measure = new BeatMeasure();

		tempos.Sort((a, b) => { return a.timing.CompareTo(b.timing); });
		timeSigs.Sort((a, b) => { return a.timing.CompareTo(b.timing); });

		Tempo lastTmpo = null;

		List<TimeSig> timeSigsTmp = new List<TimeSig>();
		BeatMeasure AmIInsane_Yes(Tempo tmpo, BeatMeasure measure)
		{
			timeSigsTmp = new List<TimeSig>();
			for(int i = 0; (i < timeSigs.Count) &&
				(timeSigs.ElementAt(i).timing < tmpo.timing); ++i)
				timeSigsTmp.Add(timeSigs.ElementAt(i));

			if(lastTmpo != null)
			{
				TimeSig lastTimeSig = new TimeSig { beats = 0, note = 0, timing = 0 };
				int count = 0;
				foreach(var tmp in timeSigsTmp)
				{
					if(count + 1 >= timeSigsTmp.Count)
					{
						float secondsperbeat = tmpo.spb;
						float time = (noteTime);
						measure.measure += (int)Math.Floor(time / secondsperbeat);
						measure.beat += time % secondsperbeat;
						measure.time = time;

						if(tmp.timing >= noteTime)
							return measure;
					}
					else
					if(lastTimeSig.note != 0)
					{
						float secondsperbeat = lastTmpo.spb;
						float time = (tmp.timing < noteTime ? tmp.timing : noteTime);
						measure.measure += (int)Math.Floor(time / secondsperbeat);
						measure.beat += time % secondsperbeat;
						measure.time = time;

						if(tmp.timing >= noteTime)
							return measure;
					}
					lastTimeSig = tmp;
					++count;
				}
			}
			lastTmpo = tmpo;
			return measure;
		}

		foreach(var tmpo in tempos)
			measure = AmIInsane_Yes(tmpo, measure);

		return measure;
	}



	public void Clear()
	{
		tempos.Clear();
		timeSigs.Clear();
	}
}
