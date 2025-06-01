using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static MyInput;


public class NoteHitting : MonoBehaviour, IKeyboardActionsActions
{
	MyInput inputs;
	KeyboardActionsActions acts;
	public HitType type;

	// Update is called once per frame
	void Update()
	{

	}

	public static UnityEvent<HitType> onNotePressed = new UnityEvent<HitType>();
	public static UnityEvent<HitType> onNoteReleased = new UnityEvent<HitType>();

	private void OnEnable()
	{
		if(inputs == null)
		{
			inputs = new MyInput();
			acts = inputs.KeyboardActions;
			acts.SetCallbacks(this);
		}
		acts.Enable();
	}

	private void OnDisable()
	{
		acts.Disable();
	}

	void hitButton(InputAction.CallbackContext context)
	{
		var col = GetComponentInChildren<Collider>();
		var mat = GetComponentInChildren<Renderer>()?.material;
		//print($"Started: {context.started}");
		//print($"Cancelled: {context.canceled}");

		float size = .35f;
		if(context.started)
		{
			//	print("gets performed");
			transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x * size, transform.localScale.z);
			//print(transform.localScale);

			if(mat)
			{
				Color.RGBToHSV(mat.color, out float h, out float s, out float v);
				mat.color = Color.HSVToRGB(h, s, 1);
			}

		}
		else if(context.canceled)
		{
			//	print("gets cancelled");
			transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x / (size / size), transform.localScale.z);
			//print(transform.localScale);
			if(mat)
			{
				Color.RGBToHSV(mat.color, out float h, out float s, out float v);
				mat.color = Color.HSVToRGB(h, s, 0.75f);
			}
		}
	}

	public void OnNote1(InputAction.CallbackContext context)
	{
		if(type != HitType.TEST1) return;

		hitButton(context);
		if(context.started) onNotePressed.Invoke(HitType.TEST1);
		if(context.canceled) onNoteReleased.Invoke(HitType.TEST1);
	}

	public void OnNote2(InputAction.CallbackContext context)
	{
		if(type != HitType.TEST2) return;

		hitButton(context);
		if(context.started) onNotePressed.Invoke(HitType.TEST2);
		if(context.canceled) onNoteReleased.Invoke(HitType.TEST2);
	}

	public void OnNote3(InputAction.CallbackContext context)
	{
		if(type != HitType.TEST3) return;

		hitButton(context);
		if(context.started) onNotePressed.Invoke(HitType.TEST3);
		if(context.canceled) onNoteReleased.Invoke(HitType.TEST3);
	}

	public void OnNote4(InputAction.CallbackContext context)
	{
		if(type != HitType.TEST4) return;

		hitButton(context);
		if(context.started) onNotePressed.Invoke(HitType.TEST4);
		if(context.canceled) onNoteReleased.Invoke(HitType.TEST4);
	}
}
