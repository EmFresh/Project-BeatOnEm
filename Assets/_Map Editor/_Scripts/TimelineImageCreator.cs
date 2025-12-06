using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

[RequireComponent(typeof(ScrollRect))]
public class TimelineImageCreator : MonoBehaviour
{
    //public AudioClip clip;

    public MapEditManager editManager;

    private ScrollRect scrollRect;

    List<GameObject> UIImages = new List<GameObject>();
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        UpdateUIAsync(false);
    }

    async void UpdateUIAsync(bool async = true)
    {
        scrollRect.normalizedPosition = Vector2.zero;

        AddTextureToUI(await CreateImagesAsync(w: 100, h: 500, vertical: true, editManager.audioSource.clip.length / 80, async),parent: scrollRect.content);

    }


    void AddTextureToUI<T>(IEnumerable<T> images, RectTransform parent) where T : Texture
    {
        int a = 0;
        //set parent of images
        foreach(var image in images)
        {
            if(a >= UIImages.Count)
                UIImages.Add(new GameObject($"Image {a + 1}", typeof(RawImage)));

            UIImages[a].transform.SetParent(scrollRect.content, false);
            UIImages[a].GetComponent<RawImage>().texture = image;

            ++a;
        }

        //remove unused images
        for(int b = a; b < UIImages.Count; ++b)
            Destroy(UIImages[b]);

    }



    /// <summary>
    /// CReate an array of RawImage GameObjects that contain sections of audio
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="vertical"></param>
    /// <param name="audioLengthPerImage"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    async public Task<Texture2D[]> CreateImagesAsync(int w, int h, bool vertical, float audioLengthPerImage = 0, bool async = true)
    { 
         
        print("Creating images...");

        if(!editManager. audioSource)
            throw new NullReferenceException("No AudioSource was spesified");
        if(!editManager.audioSource.clip)
            throw new NullReferenceException("No clip was spesified in AudioSource");

        if(audioLengthPerImage == 0)
            audioLengthPerImage = editManager.audioSource.clip.length;

        //get amout of images based on clip length
        var imageAmount = editManager.audioSource.clip.length / audioLengthPerImage;

        var images = new Texture2D[(int)Mathf.Ceil(imageAmount)];


        var songData = new float[editManager.audioSource.clip.samples * editManager.audioSource.clip.channels-1];
        //= (float[])Array.CreateInstance(typeof(float), audioSource.clip.samples * audioSource.clip.channels);
        float samplesPerImage = (songData.Length) / imageAmount;
        editManager.audioSource.clip.GetData(songData, 0);

        if(async)
            await Task.Yield(); //yield to allow the UI to update

        for(int a = 0; a < images.Length; ++a)
        {
            var length = (int)((imageAmount - a) >= 1 ? (vertical ? h : w) : (vertical ? h : w) * (imageAmount - a));

            Texture2D tex = images[a] = new Texture2D(vertical ? w : length, vertical ? length : h, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;


            int steps = 1000;
            int width = (!vertical ? tex.height : tex.width);
            print($"Creating image {a + 1} of {images.Length} with length {length} and width {width})");

            //get the sample index based on the image position
            int GetSampleIndex(int pos, int channel)
            {
                // float stepDiv = (float)length / steps;
                var lengthPos = ((float)(pos) / length);
                var sampleIndex = (int)(a * samplesPerImage + lengthPos * samplesPerImage) + channel;
                sampleIndex -= (sampleIndex + channel) % editManager.audioSource.clip.channels;

                return sampleIndex;
            }

            //get the audio value at the given position
            float GetSongDataValue(int pos, int channel, int steps)
            {
                var pixelsPerStep = ((float)length / steps);
                var stepPos = (int)(pos / pixelsPerStep) * pixelsPerStep;

                int lastSampleIndex = GetSampleIndex((int)stepPos, channel);
                int nextSampleIndex = GetSampleIndex((int)(stepPos + pixelsPerStep), channel);
                bool haslast = lastSampleIndex < songData.Length;
                bool hasNext = nextSampleIndex < songData.Length;


                return Mathf.Lerp(
                              songData[lastSampleIndex = haslast ? lastSampleIndex :
                              songData.Length - (editManager.audioSource.clip.channels - channel)],
                              songData[hasNext ? nextSampleIndex : lastSampleIndex],
                              (pos / pixelsPerStep) % 1);

            }

            for(int y = 0; y < (vertical ? tex.height : tex.width); ++y)
            {
                for(int channel = 0; channel < editManager.audioSource.clip.channels; ++channel)
                {
                    var value = GetSongDataValue(y, channel, steps);
                    value = (value * .5f + .5f);
                    value *= width / editManager.audioSource.clip.channels;

                    var nextValue = GetSongDataValue(y + 1, channel, steps);
                    nextValue = (nextValue * .5f + .5f);
                    nextValue *= width / editManager.audioSource.clip.channels;

                    //colour image
                    for(int x = 0; x < (!vertical ? tex.height : tex.width); ++x)
                    {
                        if(channel == 0)
                            tex.SetPixel((!vertical ? y : x), (vertical ? y : x), Color.black);//background colour

                        //set colour
                        int pixels = 1;
                        if(Math.Clamp(x,
                            Mathf.Min(value, nextValue) + width / editManager.audioSource.clip.channels * channel - pixels,
                            Mathf.Max(value, nextValue) + width / editManager.audioSource.clip.channels * channel + pixels) == x)
                            tex.SetPixel((!vertical ? y : x), (vertical ? y : x), channel % 2 == 0 ? Color.darkOliveGreen : Color.rebeccaPurple);

                        //if((y < 10 || y == tex.height - 1) && x == 0)
                        //{
                        //    print("Last Value: " + value);
                        //    print("Next Value: " + nextValue);
                        //    //print("Lerp Value: " + ((vertical ? y : x) / samplesPerPixel) % samplesPerPixel);
                        //    //print("Last Data: " + songData[lastSampleIndex]);
                        //    //print("Next Data: " + songData[nextSampleIndex]);
                        //    //print("Value Data: " + value);
                        //}
                    }
                    //if(async)
                    //    await Task.Yield(); //yield to allow the UI to update

                }

                //if(async)
                //    await Task.Yield(); //yield to allow the UI to update
            }

            tex.Apply();

        }
        print("Created " + images.Length + " images");
        return images;
    }
}
