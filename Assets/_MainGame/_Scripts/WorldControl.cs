/*
 File: WorldControl.cs
 Brief: Controls world movement and playback state for scrolling gameplay scenes.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls world movement and playback state for scrolling gameplay scenes.
/// Adjust the speed to change how fast the world translates along the Z axis.
/// </summary>
public class WorldControl : MonoBehaviour
{
	[SerializeField]
	/// <summary>Backing value for the world movement speed.</summary>
	private float m_speed = 10;
	[SerializeField]
	/// <summary>Backing value indicating whether world movement is paused.</summary>
	private bool m_pause = false;

	/// <summary>
	/// Movement speed of the world. Setting invokes onSpeedChange.
	/// </summary>
	public float speed
	{
		get => m_speed;
		set
		{
			m_speed = value;
			onSpeedChange.Invoke(m_speed);
		}
	}
	/// <summary>
	/// Pause or resume world movement; triggers onPause/onPlay events.
	/// </summary>
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


	/// <summary>Event invoked when world movement resumes.</summary>
	public UnityEvent onPlay;
	/// <summary>Event invoked when world movement is paused.</summary>
	public UnityEvent onPause;
	/// <summary>Event invoked with the new speed when world movement speed changes.</summary>
	public UnityEvent<float> onSpeedChange;



	/// <summary>Moves the world along the negative Z axis while it is not paused.</summary>
	void Update()
	{
		if(!m_pause)
			gameObject.transform.position += new Vector3(0, 0, Time.deltaTime * -m_speed);
	}
}
