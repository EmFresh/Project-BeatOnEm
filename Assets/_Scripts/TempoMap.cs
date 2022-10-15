using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[Serializable]
public class BeatMeasure
{
	public int measure = 0;
	public float beat = 0;
	public float time = 0;
	public bool end = false;
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
	public List<Tempo> tempos { get; private set; } = new List<Tempo>();
	public List<TimeSig> timeSigs { get; private set; } = new List<TimeSig>();


	public BeatMeasure GetBeatMeasure(float noteTime)
	{
		if(tempos.Count == 0 || timeSigs.Count == 0) return null;

		int last = 0;
		for(int a = 0; a < tempos.Count; ++a)
			if(tempos[a].timing < noteTime)
				last = a;

		BeatMeasure measure = new BeatMeasure();
		var tmp = new Tempo();
		float time = 0;
		for(int a = 0; a < timeSigs.Count; ++a)
		{
			float GetSegmentTime(ref BeatMeasure measure, float currTime, float lastTime, float endTime)
			{
				if(measure == null) return -1;

				TimeSig sig = timeSigs[a];

				Tempo tempo = tempos.Find((val) => val.timing >= currTime);
				float spb = tempo.spb;

				currTime = currTime < endTime ? currTime : endTime;
				Tempo nexttempo = tempos.Find((val) => val.timing > tempo.timing);
				try
				{

					measure.measure += (int)Mathf.Floor((currTime - lastTime) / (spb * timeSigs[a].beats));
					measure.beat += ((currTime - lastTime) % (spb * timeSigs[a].beats));

				}
				catch(Exception e)
				{
					measure.measure = 0;
					measure.beat = 0;
					Debug.LogException(e);
					return -1000000;
				}

				if(currTime >= endTime) return spb * ;

				return spb * sig.beats * measure.measure + GetSegmentTime(ref measure, currTime + spb * sig.beats, lastTime = currTime, endTime);
			}



			time += GetSegmentTime(ref measure, time, time,
				tempos[a].timing < noteTime ? tempos[a].timing : noteTime);
		}


		return measure;
	}



	public void Clear()
	{
		tempos.Clear();
		timeSigs.Clear();
	}
}
