using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Events;

public class EnemyActions : MonoBehaviour
{
	public NoteData noteData;
	public GameObject hitModel;
	public Transform root;
	public float reactTime = 2.1f;
	private static List<EnemyActions> allActions = new List<EnemyActions>();


	public int lane = 0;// 0 = centrer, 1 = left, 2 = right

	// Awake is called only once before Start
	void Awake()
	{
		//Add any initial animation code here!!(NVM DONT DO THAT)
		allActions.Add(this);


	}

	private void OnDestroy()
	{
		allActions.Remove(this);
	}

	private int last;
	//public DateTime timer = DateTime.MinValue;
	public AudioSource clip;
	void NoteUpdate()
	{
		var timings = noteData?.hitTimings;
		if(timings == null) return;


		if(last >= timings.Count) return;

		for(int count = last; count < timings.Count; ++count)
		{
			var time = timings[count] - reactTime;

			if(clip.time - time >= .5f)
				break;

			if(clip.time >= time)
			{
				/*PLACE NOTE LOGIC HERE!!!*/

				switch(noteData.hitTypes[count])
				{
				case HitType.TEST1://melee target
					/*	print("Test1 Triggered");
						StartCoroutine(AnimateMeleeTarget(time + reactTime, reactTime));
					*/
					StartCoroutine(AnimateNote1(time, time + reactTime, transform.localPosition));
					break;
				case HitType.TEST2://dodge target
					/*print("Test2 Triggered");
					StartCoroutine(AnimateDodgeEnemy(reactTime + time, reactTime));
					*/
					StartCoroutine(AnimateNote2(time, time + reactTime, transform.localPosition));
					break;
				case HitType.TEST3://dodge target
					/*print("Test2 Triggered");
					StartCoroutine(AnimateDodgeEnemy(reactTime + time, reactTime));
					*/
					StartCoroutine(AnimateNote3(time, time + reactTime, transform.localPosition));
					break;
				case HitType.TEST4://dodge target
					/*print("Test2 Triggered");
					StartCoroutine(AnimateDodgeEnemy(reactTime + time, reactTime));
					*/
					StartCoroutine(AnimateNote4(time, time + reactTime, transform.localPosition));
					break;

				default:
					break;
				}

				last = count + 1;
			}
		}

		if(last >= timings.Count)//automatic cleanup
			StartCoroutine(OnEnemyEnd(reactTime * 3f));
	}

	IEnumerator AnimateMeleeTarget(float timing, float react, Vector3 location = new Vector3())
	{
		var obj = Instantiate(hitModel, transform);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.GetChild(0).localPosition += location;
		obj.GetComponentInChildren<Renderer>().material.color = new Color(1, 0, 0);

		yield return new WaitUntil(() =>
		{
			//Animation Logic Here!!
			if(clip.time >= timing)//on completion
			{
				obj.GetComponentInChildren<Renderer>().material.color = new Color(0, 1, 0);
				IEnumerator Cleanup()
				{
					yield return new WaitForSeconds(react);
					Destroy(obj);
				}
				StartCoroutine(Cleanup());
				return true;
			}
			// Any other logic

			return false;
		});

		yield break;
	}

	IEnumerator AnimateDodgeEnemy(float timing, float react)
	{
		var tmp = GetComponentInChildren<Renderer>().material.color;
		GetComponentInChildren<Renderer>().material.color = new Color(1, 1, 0);

		yield return new WaitUntil(() =>
		{
			//print("Why this no work?");
			//Animation Logic Here!!
			if(clip.time >= timing)//on player can be hit
			{
				GetComponentInChildren<Renderer>().material.color = new Color(0, 0, 1);
				return true;
			}
			if(clip.time >= timing - react * .5)//Player not in danger
			{
				GetComponentInChildren<Renderer>().material.color = new Color(0, 1, 0);
				return false;
			}
			return false;
		});

		yield return new WaitUntil(() =>
		{
			//print("Why this no work?");
			//Animation Logic Here!!
			if(clip.time >= timing + react * .5)//Player not in danger
				return true;

			return false;
		});



		GetComponentInChildren<Renderer>().material.color = tmp;
		yield break;
	}


	IEnumerator AnimateNote1(float timing, float react, Vector3 startLocation = new Vector3())
	{
		transform.localPosition = startLocation;

		bool hit = false;
		UnityAction<HitType> act;
		NoteHitting.onNotePressed.AddListener(act = (nh) => { if(nh == HitType.TEST1) hit = true; });

		yield return new WaitUntil(() =>
		{
			//Animation Logic Here!!
			if(hit && Mathf.Abs(transform.localPosition.z) < GetComponentInChildren<Collider>().bounds.extents.z * 3)//on completion
			{
				HappyBar.onNoteHit.Invoke(.02f);
				StartCoroutine(OnEnemyEnd(0));
				return true;
			}
			else if((transform.localPosition.z) < -GetComponentInChildren<Collider>().bounds.extents.z * 2)
			{
				HappyBar.onNoteMiss.Invoke(.1f);
				StartCoroutine(OnEnemyEnd(.01f));
				return true;
			}

			// Any other logic
			hit = false;


			//move note location
			float tmp;
			transform.localPosition = ((Vector3)(Vector2)transform.localPosition) + new Vector3(0, 0, tmp = Mathf.LerpUnclamped(startLocation.z, 0, 1 - (react - clip.time) / (react - timing)));

			return false;
		});


		NoteHitting.onNotePressed.RemoveListener(act);
		yield break;
	}
	IEnumerator AnimateNote2(float timing, float react, Vector3 startLocation = new Vector3())
	{
		transform.localPosition = startLocation;

		bool hit = false;
		UnityAction<HitType> act;
		NoteHitting.onNotePressed.AddListener(act = (nh) => { if(nh == HitType.TEST2) hit = true; });

		yield return new WaitUntil(() =>
		{
			//Animation Logic Here!!
			if(hit && Mathf.Abs(transform.localPosition.z) < GetComponentInChildren<Collider>().bounds.extents.z * 4)//on completion
			{
				HappyBar.onNoteHit.Invoke(.02f);
				StartCoroutine(OnEnemyEnd(0));
				return hit;
			}
			else if((transform.localPosition.z) < -GetComponentInChildren<Collider>().bounds.extents.z * 2)
			{
				HappyBar.onNoteMiss.Invoke(.1f);
				StartCoroutine(OnEnemyEnd(.01f));
				return true;
			}


			// Any other logic
			hit = false;


			//move note location
			float tmp;
			transform.localPosition = ((Vector3)(Vector2)transform.localPosition) + new Vector3(0, 0, tmp = Mathf.LerpUnclamped(startLocation.z, 0, 1 - (react - clip.time) / (react - timing)));

			return false;
		});


		NoteHitting.onNotePressed.RemoveListener(act);
		yield break;
	}
	IEnumerator AnimateNote3(float timing, float react, Vector3 startLocation = new Vector3())
	{
		transform.localPosition = startLocation;

		bool hit = false;
		UnityAction<HitType> act;
		NoteHitting.onNotePressed.AddListener(act = (nh) => { if(nh == HitType.TEST3) hit = true; });

		yield return new WaitUntil(() =>
		{
			//Animation Logic Here!!
			if(hit && Mathf.Abs(transform.localPosition.z) < GetComponentInChildren<Collider>().bounds.extents.z * 4)//on completion
			{
				HappyBar.onNoteHit.Invoke(.02f);
				StartCoroutine(OnEnemyEnd(0));
				return hit;
			}
			else if((transform.localPosition.z) < -GetComponentInChildren<Collider>().bounds.extents.z * 2)
			{
				HappyBar.onNoteMiss.Invoke(.1f);
				StartCoroutine(OnEnemyEnd(.01f));
				return true;
			}


			// Any other logic
			hit = false;


			//move note location
			float tmp;
			transform.localPosition = ((Vector3)(Vector2)transform.localPosition) + new Vector3(0, 0, tmp = Mathf.LerpUnclamped(startLocation.z, 0, 1 - (react - clip.time) / (react - timing)));

			return false;
		});


		NoteHitting.onNotePressed.RemoveListener(act);
		yield break;
	}
	IEnumerator AnimateNote4(float timing, float react, Vector3 startLocation = new Vector3())
	{
		transform.localPosition = startLocation;

		bool hit = false;
		UnityAction<HitType> act;
		NoteHitting.onNotePressed.AddListener(act = (nh) => { if(nh == HitType.TEST4) hit = true; });

		yield return new WaitUntil(() =>
		{
			//Animation Logic Here!!
			if(hit && Mathf.Abs(transform.localPosition.z) < GetComponentInChildren<Collider>().bounds.extents.z * 4)//on completion
			{
				HappyBar.onNoteHit.Invoke(.02f);
				StartCoroutine(OnEnemyEnd(0));
				return hit;
			}
			else if((transform.localPosition.z) < -GetComponentInChildren<Collider>().bounds.extents.z * 2)
			{
				HappyBar.onNoteMiss.Invoke(.1f);
				StartCoroutine(OnEnemyEnd(.01f));
				return true;
			}


			// Any other logic
			hit = false;


			//move note location
			float tmp;
			transform.localPosition = ((Vector3)(Vector2)transform.localPosition) + new Vector3(0, 0, tmp = Mathf.LerpUnclamped(startLocation.z, 0, 1 - (react - clip.time) / (react - timing)));

			return false;
		});


		NoteHitting.onNotePressed.RemoveListener(act);
		yield break;
	}


	private IEnumerator OnEnemyEnd(float react)
	{
		yield return new WaitForSeconds(react);
		Destroy(gameObject);
		yield break;
	}

	// Update is called once per frame
	void Update()
	{
		//if(timer == DateTime.MinValue)
		//	timer = DateTime.Now;

		NoteUpdate();
	}
}
