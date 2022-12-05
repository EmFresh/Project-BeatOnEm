using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HappyBar : MonoBehaviour
{
	public Slider slider;
	public static UnityEvent<float> onNoteHit;
	public static UnityEvent<float> onNoteMiss ;

	// Start is called before the first frame update
	void Awake()
	{
		if(onNoteMiss == null)
		{
			onNoteMiss = new UnityEvent<float>();
			onNoteMiss.AddListener((val) =>
			{
				slider.value -= val;

			});
		}

		if(onNoteHit == null)
		{
			onNoteHit = new UnityEvent<float>();
			onNoteHit.AddListener((val) =>
			{

				slider.value += val;

			});
		}


	}


	// Update is called once per frame
	void Update()
	{

		if(slider.value == 0)
		{

		}


	}
}
