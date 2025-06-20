using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WorldControl : MonoBehaviour
{
	[SerializeField]
	private float m_speed = 10;
	[SerializeField]
	private bool m_pause = false;

	public float speed
	{
		get => m_speed;
		set
		{
			m_speed = value;
			onSpeedChange.Invoke(m_speed);
		}
	}
	public bool pause
	{
		get => m_pause;
		set
		{
			m_pause = value;
			if(m_pause) onPause.Invoke();
			else onPlay.Invoke();
		}
	}


	public UnityEvent onPlay;
	public UnityEvent onPause;
	public UnityEvent<float> onSpeedChange;



	// Update is called once per frame
	void Update()
	{
		if(!m_pause)
			gameObject.transform.position += new Vector3(0, 0, Time.deltaTime * -m_speed);
	}
}
