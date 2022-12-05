using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChangeBGImage : MonoBehaviour
{
	public RawImage image;
	public float speedScale = .2f;
	public static UnityEvent<Texture> onChangeImage = new UnityEvent<Texture>();

	// Start is called before the first frame update
	void Awake()
	{
		Coroutine co = null;
		onChangeImage.AddListener((img) =>
		{
			image.texture = img;
			if(co != null)
				StopCoroutine(co);
			IEnumerator CoDynamicMovement()
			{
				Rect tmp = image.uvRect;
				float xdist = 1 - tmp.width;
				float ydist = 1 - tmp.height;
				System.Random ran = new();
				float[] speeds = { 0, 0.25f, 0.5f, 0.75f, 1 };
				float timeReset = Time.time;
				yield return new WaitWhile(() =>
				{

					float speed1 = 0;
					float speed2 = 0;
					while(speed1 + speed2 == 0)
					{
						speed1 = speeds[ran.Next(0, 5)];
						speed2 = speeds[ran.Next(0, 5)];
					}

					float x = Mathf.Lerp(0, xdist, Mathf.Cos((Time.time -timeReset)* speed1));
					float y = Mathf.Lerp(0, ydist, Mathf.Cos((Time.time -timeReset)* speed2));

					image.uvRect = new(x, y, tmp.width, tmp.height);

					return true;
				});

				yield break;
			}

			co = StartCoroutine(CoDynamicMovement());
		});
		
		Rect tmp = image.uvRect;
		xdist = 1 - tmp.width;
		ydist = 1 - tmp.height;
		float[] speeds = { 0, 0.25f, 0.5f, 0.75f, 1 };
		while(speed1 + speed2 == 0)
		{
			speed1 = speeds[ran.Next(0, 5)];
			speed2 = speeds[ran.Next(0, 5)];
		}
	}
	float xdist;
	float ydist;
	float speed1 = 0;
	float speed2 = 0;
	System.Random ran = new();
	private void Update()
	{
		Rect tmp = image.uvRect;
		float x = Mathf.Lerp(0, xdist, Mathf.Abs(Mathf.Cos(Time.time * speed1 * speedScale)));
		float y = Mathf.Lerp(0, ydist, Mathf.Abs(Mathf.Cos(Time.time * speed2 * speedScale)));

		image.uvRect = new(x, y, .75f, .75f);

	}

}
