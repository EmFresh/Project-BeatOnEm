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
			float timeReset = Time.time;
			IEnumerator CoDynamicMovement()
			{
				Rect tmp = image.uvRect;
				float xdist = 1 - tmp.width;
				float ydist = 1 - tmp.height;
				System.Random ran = new();
				float[] speeds = { 0, 0.25f, 0.5f, 0.75f, 1 };

				timeReset = Time.time;

				float speed1 = 0;
				float speed2 = 0;
				while(speed1 + speed2 == 0)
				{
					speed1 = speeds[ran.Next(0, 5)];
					speed2 = speeds[ran.Next(0, 5)];
				}
				yield return new WaitWhile(() =>
				{
					float x = Mathf.Lerp(0, xdist, Mathf.Cos((Time.time - timeReset) * speed1 * speedScale));
					float y = Mathf.Lerp(0, ydist, Mathf.Cos((Time.time - timeReset) * speed2 * speedScale));

					image.uvRect = new(x, y, tmp.width, tmp.height);

					return true;
				});

				yield break;
			}

			co = StartCoroutine(CoDynamicMovement());
		});

	}



}
