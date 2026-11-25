/*
 File: TimelineImageCreator.cs
 Brief: Generates timeline images and line renderers used by the map editor timeline UI. This file contains
 helpers to create textures and line point data from audio clips and to populate UI elements.
*/

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
/// <summary>
/// Generates timeline images and line renderers from an audio source for the map editor timeline.
/// The class creates textures and point arrays that represent the audio waveform and populates
/// UI elements (RawImage and UILineRenderer) so the timeline can be visualized.
/// </summary>
public class TimelineImageCreator : MonoBehaviour
{
    /// <summary>
    /// Reference to the MapEditManager providing the audio source and related data.
    /// </summary>
    public MapEditManager editManager;

    /// <summary>
    /// RectTransforms that will receive generated line renderers for waveform visualization.
    /// </summary>
    public List<RectTransform> lineRenderers = new List<RectTransform>();

    /// <summary>
    /// Cached ScrollRect component used to place generated UI elements.
    /// </summary>
    private ScrollRect scrollRect;

    /// <summary>
    /// Pool of GameObjects used to host generated RawImage components for textures.
    /// </summary>
    private List<GameObject> UIImages = new List<GameObject>();

    /// <summary>
    /// Awake is called when the script instance is being loaded. Caches the ScrollRect
    /// component required to add generated UI elements into the hierarchy.
    /// </summary>
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    /// <summary>
    /// Rebuilds the image UI for the timeline asynchronously and assigns textures to the ScrollRect content.
    /// </summary>
    /// <param name="async">Whether to perform work asynchronously. If true the image creation may yield to
    /// the Unity scheduler during long-running work.</param>
    public async void UpdateImageUIAsync(bool async = true)
    {
        scrollRect.normalizedPosition = Vector2.zero;
        AddTextureToUI(await CreateImagesAsync(w: 100, h: 500, vertical: true, editManager.AudioSource.clip.length / 80, async), parent: scrollRect.content);
    }

    /// <summary>
    /// Rebuilds the line renderer UI elements asynchronously and places them into the configured parents.
    /// </summary>
    /// <param name="async">Whether to perform work asynchronously.</param>
    public async void UpdateLineUIAsync(bool async = true)
    {
        scrollRect = GetComponent<ScrollRect>();
        scrollRect.normalizedPosition = Vector2.zero;
        if(editManager.AudioSource.clip == null)
            return;
        AddLineToUI(await CreateLineAsync(w: 1, h: editManager.AudioSource.clip.samples / editManager.AudioSource.clip.channels / 100, steps: .3f, vertical: true, async: async), maxPoints: 4000, parents: lineRenderers);
    }

    /// <summary>
    /// Returns a list of low factors (divisors) for the provided integer value.
    /// Only factors less than the square root of the input are returned.
    /// </summary>
    /// <param name="value">Value to factorize.</param>
    /// <returns>List of integer factors less than the square root of the value.</returns>
    public List<int> GetLowFactors(int value)
    {
        var factors = new List<int>();

        int sqrt = (int)Math.Ceiling(Math.Sqrt(value));

        for (int a = 1; a < sqrt; ++a)
            if (value % a == 0)
                factors.Add(a);

        return factors;
    }

    /// <summary>
    /// Returns factors greater than or equal to the square root by inverting the low factors.
    /// </summary>
    /// <param name="value">Value to factorize.</param>
    /// <returns>List of integer factors greater than or equal to the square root of the value.</returns>
    public List<int> GetHighFactors(int value)
    {
        var low = GetLowFactors(value);
        var factors = new List<int>();
        low.Reverse();

        foreach (var factor in low)
            factors.Add(value / factor);

        return factors;
    }

    /// <summary>
    /// Returns all factors of the specified value by combining low and high factors.
    /// </summary>
    /// <param name="value">Value to factorize.</param>
    /// <returns>List of all integer factors for the value.</returns>
    public List<int> GetAllFactors(int value)
    {
        var low = GetLowFactors(value);
        var factors = new List<int>(low);
        low.Reverse();

        foreach (var factor in low)
            factors.Add(value / factor);

        return factors;
    }

    /// <summary>
    /// Adds line renderer GameObjects to the provided parent RectTransforms using the provided
    /// point arrays. Each element in <paramref name="points"/> corresponds to a parent in <paramref name="parents"/>.
    /// </summary>
    /// <param name="points">Array of enumerable point collections representing line points for each parent.</param>
    /// <param name="parents">List of RectTransform parents to attach generated UILineRenderer objects to.</param>
    /// <param name="maxPoints">Maximum number of points allowed per generated renderer; reduces detail if necessary.</param>
    private void AddLineToUI(IEnumerable<Vector2>[] points, List<RectTransform> parents, int maxPoints = 4000)
    {
        int a = 0;
        Color[] colors = new Color[] { Color.darkOliveGreen, Color.rebeccaPurple, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        foreach (var parent in parents)
        {
            var pointlist = points[a].ToList();
            var factor = GetAllFactors(pointlist.Count).Last(p => a <= maxPoints);
            var div = pointlist.Count / factor;

            while (pointlist.Count / div > maxPoints)
                div *= 2;

            var divAmount = Mathf.CeilToInt((float)pointlist.Count / div);

            for (int divCount = 0; divCount < pointlist.Count; divCount += divAmount)
            {
                var rendererObj = new GameObject($"Renderer {divCount / divAmount + 1}", typeof(UILineRenderer));
                var renderer = rendererObj.GetComponent<UILineRenderer>();
                rendererObj.transform.SetParent(parent, false);

                int b = 0;
                if (points.Count() > 0)
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
                    for (b = 0; b < renderer.Points.Length; ++b)
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

            }
            ++a;
        }
    }

    /// <summary>
    /// Adds textures as RawImage children of the configured ScrollRect content.
    /// </summary>
    /// <typeparam name="T">Texture type (e.g., Texture2D).</typeparam>
    /// <param name="images">Enumerable of textures to add to the UI.</param>
    /// <param name="parent">Parent RectTransform under which new or pooled images will be placed.</param>
    private void AddTextureToUI<T>(IEnumerable<T> images, RectTransform parent) where T : Texture
    {
        int a = 0;
        foreach (var image in images)
        {
            if (a >= UIImages.Count)
                UIImages.Add(new GameObject($"Image {a + 1}", typeof(RawImage)));

            UIImages[a].transform.SetParent(parent, false);
            UIImages[a].GetComponent<RawImage>().texture = image;

            ++a;
        }

        for(int b = a; b < UIImages.Count; ++b)
            Destroy(UIImages[b]);

    }

    /// <summary>Creates waveform textures for successive portions of the attached audio clip.</summary>
    /// <param name="w">Texture width, or the waveform width when generated horizontally.</param>
    /// <param name="h">Texture height, or the waveform height when generated horizontally.</param>
    /// <param name="vertical">Whether the waveform is laid out vertically.</param>
    /// <param name="audioLengthPerImage">Audio duration represented by each texture; nonpositive values use the whole clip.</param>
    /// <param name="async">Whether to yield once to the Unity scheduler before generating textures.</param>
    /// <returns>The generated textures in timeline order.</returns>
    /// <exception cref="NullReferenceException">The manager, audio source, or audio clip is unavailable.</exception>
    async public Task<Texture2D[]> CreateImagesAsync(int w, int h, bool vertical = true, float audioLengthPerImage = 0, bool async = true)
    {

        print("Creating images...");

        if(!editManager.AudioSource)
            throw new NullReferenceException("No AudioSource was spesified");
        if(!editManager.AudioSource.clip)
            throw new NullReferenceException("No clip was spesified in AudioSource");

        if(audioLengthPerImage <= 0)
            audioLengthPerImage = editManager.AudioSource.clip.length;

        // Divide the clip into equal-duration image segments.
        var imageAmount = editManager.AudioSource.clip.length / audioLengthPerImage;

        var images = new Texture2D[(int)Mathf.Ceil(imageAmount)];

        // Read the interleaved channel samples once and divide their indices across the output images.
        var songData = new float[editManager.AudioSource.clip.samples * editManager.AudioSource.clip.channels];
        float samplesPerImage = (songData.Length) / imageAmount;
        editManager.AudioSource.clip.GetData(songData, 0);

        if(async)
            await Task.Yield(); //yield to allow the UI to update

        for(int a = 0; a < images.Length; ++a)
        {
            // Create a full-size texture except for the final partial audio segment.
            var length = (int)((imageAmount - a) >= 1 ? (vertical ? h : w) : (vertical ? h : w) * (imageAmount - a));

            Texture2D tex = images[a] = new Texture2D(vertical ? w : length, vertical ? length : h, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;


            int steps = 1000;
            int width = (!vertical ? tex.height : tex.width);
            print($"Creating image {a + 1} of {images.Length} with length {length} and width {width})");

            // Map a pixel along this segment to its channel-aligned position in the audio buffer.
            int GetSampleIndex(int pos, int channel)
            {
                var lengthPos = ((float)(pos) / length);
                var sampleIndex = (int)(a * samplesPerImage + lengthPos * samplesPerImage) + channel;
                sampleIndex -= (sampleIndex + channel) % editManager.AudioSource.clip.channels;

                return sampleIndex;
            }

            float GetSongDataValue(int pos, int channel, int steps)
            {
                // Locate adjacent sampled positions, clamp at the buffer end, and interpolate between them.
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

            // Convert each channel's samples to waveform coordinates and rasterize the connecting trace.
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

                    for(int x = 0; x < (!vertical ? tex.height : tex.width); ++x)
                    {
                        if(channel == 0)
                            tex.SetPixel((!vertical ? y : x), (vertical ? y : x), Color.black);//background colour

                        int pixels = 1;
                        if(Math.Clamp(x,
                            Mathf.Min(value, nextValue) + width / editManager.AudioSource.clip.channels * channel - pixels,
                            Mathf.Max(value, nextValue) + width / editManager.AudioSource.clip.channels * channel + pixels) == x)
                            tex.SetPixel((!vertical ? y : x), (vertical ? y : x), channel % 2 == 0 ? Color.darkOliveGreen : Color.rebeccaPurple);

                    }

                }
            }

            tex.Apply();

        }
        print("Created " + images.Length + " images");
        return images;
    }


    /// <summary>Creates normalized waveform point collections, one for each audio channel.</summary>
    /// <param name="w">Waveform width when laid out horizontally.</param>
    /// <param name="h">Waveform height when laid out vertically.</param>
    /// <param name="vertical">Whether the waveform points are laid out vertically.</param>
    /// <param name="steps">Sampling interval multiplier used to select waveform values.</param>
    /// <param name="async">Whether to yield once to the Unity scheduler before generating points.</param>
    /// <returns>Point collections for each audio channel.</returns>
    /// <exception cref="NullReferenceException">The manager, audio source, or audio clip is unavailable.</exception>
    async public Task<HashSet<Vector2>[]> CreateLineAsync(int w, int h, bool vertical = true, float steps = .5f, bool async = true)
    {

        print("Creating Waveform...");

        if(!editManager.AudioSource)
            throw new NullReferenceException("No AudioSource was spesified");
        if(!editManager.AudioSource.clip)
            throw new NullReferenceException("No clip was spesified in AudioSource");

        var positions = new HashSet<Vector2>[editManager.AudioSource.clip.channels];

        var songData = new float[editManager.AudioSource.clip.samples * editManager.AudioSource.clip.channels];
        editManager.AudioSource.clip.GetData(songData, 0);

        // Let Unity render the current frame before processing the sample buffer when requested.
        if(async)
            await Task.Yield(); //yield to allow the UI to update

        {
            var length = (float)(vertical ? h : w);
            var width = (float)(!vertical ? h : w);

            // Map a normalized waveform position to the interleaved audio sample buffer.
            int GetSampleIndex(float pos, int channel)
            {
                var lengthPos = ((pos) / length);
                var sampleIndex = (int)(lengthPos * songData.Length) + channel;
                sampleIndex -= (sampleIndex + channel) % editManager.AudioSource.clip.channels;

                return sampleIndex;
            }

            // Interpolate between neighboring samples for the requested channel.
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
            // Normalize each channel's samples and store points in the requested orientation.
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

            }
        }

        print("Created " + positions[0].Count + " points");
        return positions;
    }
}
