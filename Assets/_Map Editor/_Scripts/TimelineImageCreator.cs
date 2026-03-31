using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

using static UnityEngine.Rendering.DebugUI.Table;

using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(ScrollRect))]
public class TimelineImageCreator : MonoBehaviour
{
    //public AudioClip clip;

    public MapEditManager editManager;
    public List<RectTransform> lineRenderers = new List<RectTransform>();

    private ScrollRect scrollRect;

    List<GameObject> UIImages = new List<GameObject>();
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        UpdateImageUIAsync(false);
    }

    async void UpdateImageUIAsync(bool async = true)
    {
        scrollRect.normalizedPosition = Vector2.zero;
        // AddTextureToUI(await CreateImagesAsync(w: 100, h: 500, vertical: true, editManager.AudioSource.clip.length / 80, async), parent: scrollRect.content);
        AddLineToUI(await CreateLineAsync(w: 1, h: editManager.AudioSource.clip.samples / editManager.AudioSource.clip.channels / 100, steps: .3f, vertical: true, async: async), maxPoints: 4000, parents: lineRenderers);
    }



    public List<int> GetLowFactors(int numberToCheck)
    {
        var factors = new List<int>();

        int sqrt = (int)Math.Ceiling(Math.Sqrt(numberToCheck));

        for(int a = 1; a < sqrt; ++a)
            if(numberToCheck % a == 0)
                factors.Add(a);


        return factors;
    }

    public List<int> GetHighFactors(int numberToCheck)
    {
        var low = GetLowFactors(numberToCheck);
        var factors = new List<int>();
        low.Reverse();

        foreach(var factor in low)
            factors.Add(numberToCheck / factor);

        return factors;
    }

    public List<int> GetAllFactors(int numberToCheck)
    {
        var low = GetLowFactors(numberToCheck);
        var factors = new List<int>(low);
        low.Reverse();

        foreach(var factor in low)
            factors.Add(numberToCheck / factor);


        return factors;
    }



    void AddLineToUI(IEnumerable<Vector2>[] points, List<RectTransform> parents, int maxPoints = 4000)
    {
        int a = 0;
        Color[] colors = new Color[] { Color.darkOliveGreen, Color.rebeccaPurple, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        //set parent of images
        foreach(var parent in parents)
        {
            var pointlist = points[a].ToList();
            var factor = GetAllFactors(pointlist.Count).Last(p => a <= maxPoints);
            var div = pointlist.Count / factor;

            while(pointlist.Count / div > maxPoints)
                div *= 2;

            var divAmount = Mathf.CeilToInt((float)pointlist.Count / div);

            for(int divCount = 0; divCount < pointlist.Count; divCount += divAmount)
            {
                var rendererObj = new GameObject($"Renderer {divCount / divAmount + 1}", typeof(UILineRenderer));
                var renderer = rendererObj.GetComponent<UILineRenderer>();
                rendererObj.transform.SetParent(parent, false);

                int b = 0;
                if(points.Count() > 0)
                {
                    renderer.color = colors[a % colors.Length];
                    renderer.Points = new Vector2[Mathf.Clamp(pointlist.Count - divCount, 0, divAmount) + (divCount > 0 ? 1 : 0)];
                    renderer.RelativeSize = true;
                    renderer.BezierSegmentsPerCurve = 1;
                    renderer.BezierMode = UILineRenderer.BezierType.None;
                    renderer.LineJoins = UILineRenderer.JoinType.Miter;
                    renderer.raycastTarget = false;
                    renderer.maskable = true;
                    renderer.canvasRenderer.cull = false;
                    // renderer.canvasRenderer.EnableRectClipping(GetComponentInParent<ScrollRect>().GetComponent<RectTransform>().rect);

                    for(b = 0; b < renderer.Points.Length; ++b)
                    {
                        var refPoint = pointlist[b + divCount - (divCount > 0 ? 1 : 0)];
                        renderer.Points[b] = refPoint;

                        renderer.Points[b].y = Mathf.InverseLerp(pointlist[divCount - (divCount > 0 ? 1 : 0)].y,
                            pointlist[divCount + renderer.Points.Length - (divCount > 0 ? 2 : 1)].y,
                            renderer.Points[b].y);
                    }
                }
                else
                    renderer.Points = null;

                //  print($"completed {b} of {pointlist.Count} objects");
            }
            ++a;
        }
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
    async public Task<Texture2D[]> CreateImagesAsync(int w, int h, bool vertical = true, float audioLengthPerImage = 0, bool async = true)
    {

        print("Creating images...");

        if(!editManager.AudioSource)
            throw new NullReferenceException("No AudioSource was spesified");
        if(!editManager.AudioSource.clip)
            throw new NullReferenceException("No clip was spesified in AudioSource");

        if(audioLengthPerImage <= 0)
            audioLengthPerImage = editManager.AudioSource.clip.length;

        //get amout of images based on clip length
        var imageAmount = editManager.AudioSource.clip.length / audioLengthPerImage;

        var images = new Texture2D[(int)Mathf.Ceil(imageAmount)];


        var songData = new float[editManager.AudioSource.clip.samples * editManager.AudioSource.clip.channels - 1];
        //= (float[])Array.CreateInstance(typeof(float), AudioSource.clip.samples * AudioSource.clip.channels);
        float samplesPerImage = (songData.Length) / imageAmount;
        editManager.AudioSource.clip.GetData(songData, 0);

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
                sampleIndex -= (sampleIndex + channel) % editManager.AudioSource.clip.channels;

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
                              songData.Length - (editManager.AudioSource.clip.channels - channel)],
                              songData[hasNext ? nextSampleIndex : lastSampleIndex],
                              (pos / pixelsPerStep) % 1);

            }

            for(int y = 0; y < (vertical ? tex.height : tex.width); ++y)
            {
                for(int channel = 0; channel < editManager.AudioSource.clip.channels; ++channel)
                {
                    var value = GetSongDataValue(y, channel, steps);
                    value = (value * .5f + .5f);
                    value *= width / editManager.AudioSource.clip.channels;

                    var nextValue = GetSongDataValue(y + 1, channel, steps);
                    nextValue = (nextValue * .5f + .5f);
                    nextValue *= width / editManager.AudioSource.clip.channels;

                    //colour image
                    for(int x = 0; x < (!vertical ? tex.height : tex.width); ++x)
                    {
                        if(channel == 0)
                            tex.SetPixel((!vertical ? y : x), (vertical ? y : x), Color.black);//background colour

                        //set colour
                        int pixels = 1;
                        if(Math.Clamp(x,
                            Mathf.Min(value, nextValue) + width / editManager.AudioSource.clip.channels * channel - pixels,
                            Mathf.Max(value, nextValue) + width / editManager.AudioSource.clip.channels * channel + pixels) == x)
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


    async public Task<HashSet<Vector2>[]> CreateLineAsync(int w, int h, bool vertical = true, float steps = .5f, bool async = true)
    {

        print("Creating Waveform...");

        if(!editManager.AudioSource)
            throw new NullReferenceException("No AudioSource was spesified");
        if(!editManager.AudioSource.clip)
            throw new NullReferenceException("No clip was spesified in AudioSource");

        var positions = new HashSet<Vector2>[editManager.AudioSource.clip.channels];

        var songData = new float[editManager.AudioSource.clip.samples * editManager.AudioSource.clip.channels - 1];
        //= (float[])Array.CreateInstance(typeof(float), AudioSource.clip.samples * AudioSource.clip.channels);
        //float samplesPerImage = (songData.Length) / imageAmount;
        editManager.AudioSource.clip.GetData(songData, 0);



        if(async)
            await Task.Yield(); //yield to allow the UI to update

        //for(int a = 0; a < images.Length; ++a)
        {

            //  Texture2D tex = images[a] = new Texture2D(vertical ? w : length, vertical ? length : h, TextureFormat.RGB24, false);
            //   tex.filterMode = FilterMode.Bilinear;


            var length = (float)(vertical ? h : w);
            var width = (float)(!vertical ? h : w);


            // print($"Creating image {a + 1} of {images.Length} with length {length} and width {width})");

            //get the sample index based on the image position
            int GetSampleIndex(float pos, int channel)
            {
                // float stepDiv = (float)length / steps;
                var lengthPos = ((pos) / length);
                var sampleIndex = (int)(lengthPos * songData.Length) + channel;
                sampleIndex -= (sampleIndex + channel) % editManager.AudioSource.clip.channels;

                return sampleIndex;
            }

            //get the audio value at the given position
            float GetSongDataValue(float pos, int channel, float steps)
            {
                var pixelsPerStep = (length / (editManager.AudioSource.clip.frequency * steps));
                var stepPos = (int)(pos / pixelsPerStep) * pixelsPerStep;

                int lastSampleIndex = GetSampleIndex((int)stepPos, channel);
                int nextSampleIndex = GetSampleIndex((int)(stepPos + pixelsPerStep), channel);
                bool haslast = lastSampleIndex < songData.Length;
                bool hasNext = nextSampleIndex < songData.Length;


                return Mathf.Lerp(
                              songData[lastSampleIndex = haslast ? lastSampleIndex :
                              songData.Length - (editManager.AudioSource.clip.channels - channel)],
                              songData[hasNext ? nextSampleIndex : lastSampleIndex],
                              (pos / pixelsPerStep) % 1);

            }


            var space = 1f / editManager.AudioSource.clip.channels;
            //int step = length / 1000;
            for(int y = 0; y < length; ++y)
            {
                for(int channel = 0; channel < editManager.AudioSource.clip.channels; ++channel)
                {
                    var value = GetSongDataValue(y, channel, steps);
                    value = (value * space + (1 - space));



                    if(positions[channel] == null)
                        positions[channel] = new HashSet<Vector2>();

                    if(vertical)
                        positions[channel].Add(new Vector2(value, y / length));
                    else
                        positions[channel].Add(new Vector2(y / length, value));

                }

                //if(async)
                //    await Task.Yield(); //yield to allow the UI to update
            }
        }

        print("Created " + positions[0].Count + " points");
        return positions;
    }
}
