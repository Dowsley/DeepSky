using System;
using System.IO;
using System.Text.RegularExpressions;
using DeepSky.Rendering.Atmosphere;
using DeepSky.Rendering.CameraEffects;
using DeepSky.Rendering.LightShafts;
using DeepSky.Rendering.Particles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepSky.Editor
{
    public static class GameCapture
    {
        /// <summary>Renders the running player camera and its custom effects to a new ignored PNG capture.</summary>
        /// <param name="name">Unique filename stem beginning with deepsky-, followed by lowercase letters, digits or hyphens.</param>
        /// <param name="width">Output width in pixels, between 640 and 3840 inclusive.</param>
        /// <param name="height">Output height in pixels, between 480 and 2160 inclusive.</param>
        /// <returns>The capture path and camera pose, projection and simulation-time metadata.</returns>
        /// <exception cref="InvalidOperationException">Play mode, the player camera or prepared effect state is missing.</exception>
        /// <exception cref="ArgumentException">The filename stem is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The requested image dimensions are unsupported.</exception>
        /// <exception cref="IOException">The destination already exists or the capture cannot be written.</exception>
        /// <exception cref="NotSupportedException">The active render pipeline cannot fulfill the camera render request.</exception>
        public static string Capture(string name,int width=1920,int height=1080)
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Capture the running game in Play mode.");
            if(!Regex.IsMatch(name,@"\Adeepsky-[a-z0-9-]+\z")) throw new ArgumentException("Use a deepsky- filename with lowercase letters, digits and hyphens.");
            if(width<640 || width>3840 || height<480 || height>2160) throw new ArgumentOutOfRangeException("Capture dimensions are outside the supported range.");
            string path=Path.GetFullPath("Captures/"+name+".png");
            if(File.Exists(path)) throw new IOException("Capture already exists: "+path);
            var cameraObject=GameObject.Find("Player Camera");
            var source=cameraObject ? cameraObject.GetComponent<Camera>() : null;
            if(source is null || !source) throw new InvalidOperationException("Player Camera is required.");
            var shafts = source.GetComponent<LightShaftManager>();
            if (shafts && shafts.enabled && !shafts.CameraStateCurrent)
                throw new InvalidOperationException("Wait for the light shafts to update after positioning the camera before capturing.");

            foreach (var volume in source.GetComponentsInChildren<ParticleVolume>())
            {
                if (volume.enabled && !volume.CameraStateCurrent)
                    throw new InvalidOperationException("Wait for the particle volumes to update after positioning the camera before capturing.");
            }
            var node=new GameObject("Reference capture camera") {hideFlags=HideFlags.HideAndDontSave};
            // Required effect references must be assigned before OnEnable runs.
            node.SetActive(false);
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                var camera=node.AddComponent<Camera>();
                camera.enabled=false;
                camera.CopyFrom(source);
                camera.enabled=false;
                camera.targetTexture=target;
                camera.aspect=(float)width/height;
                camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                var bloom=source.GetComponent<UnderwaterBloom>();
                if (bloom && bloom.enabled)
                {
                    EditorUtility.CopySerialized(bloom, node.AddComponent<UnderwaterBloom>());
                }
                var water=source.GetComponent<WaterComposite>();
                if (water && water.enabled)
                {
                    EditorUtility.CopySerialized(water, node.AddComponent<WaterComposite>());
                }
                var atmosphere = source.GetComponent<DepthAtmosphere>();
                if (atmosphere && atmosphere.enabled)
                {
                    EditorUtility.CopySerialized(atmosphere, node.AddComponent<DepthAtmosphere>());
                }
                var presentation = source.GetComponent<RetroPresentation>();
                if (presentation && presentation.enabled)
                {
                    EditorUtility.CopySerialized(presentation, node.AddComponent<RetroPresentation>());
                }
                node.SetActive(true);
                var request=new RenderPipeline.StandardRequest {destination=target};
                if(!RenderPipeline.SupportsRenderRequest(camera,request)) throw new NotSupportedException("The active renderer does not support camera captures.");
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);
                pixels.Apply();
                byte[] png=pixels.EncodeToPNG();
                Directory.CreateDirectory(Path.GetDirectoryName(path)
                    ?? throw new InvalidOperationException("Capture path requires a parent directory."));
                using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write)) output.Write(png,0,png.Length);
                return path+" | eye="+camera.transform.position+" angles="+camera.transform.eulerAngles
                    +" FOV="+camera.fieldOfView+" size="+width+"x"+height+" time="+Time.time;
            }
            finally
            {
                RenderTexture.active=previous;
                UnityEngine.Object.DestroyImmediate(node);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
