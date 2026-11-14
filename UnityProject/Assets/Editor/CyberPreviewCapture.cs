using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CyberRush.Editor
{
    public static class CyberPreviewCapture
    {
        public static void CaptureAll()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CyberRush.unity", OpenSceneMode.Single);
            string outDir = @"C:\Users\akeer.AKERU\.gemini\antigravity\brain\38f7717d-e75e-4d54-a1af-288b471485bd";

            // 1. Live FPS Main Camera View (Katana Viewmodel + Stage 1 Skyline)
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                RenderToPng(mainCam, Path.Combine(outDir, "cyber_fps_preview.png"));

                // 1b. Switch to Cyber Blaster FPS Viewmodel Preview
                var pc = Object.FindFirstObjectByType<CyberParkourController>();
                if (pc != null && pc.katanaPivot != null && pc.blasterPivot != null)
                {
                    pc.katanaPivot.gameObject.SetActive(false);
                    pc.blasterPivot.gameObject.SetActive(true);
                    RenderToPng(mainCam, Path.Combine(outDir, "cyber_blaster_preview.png"));
                    pc.blasterPivot.gameObject.SetActive(false);
                    pc.katanaPivot.gameObject.SetActive(true);
                }
            }

            // 2. Stage 1 Overview (Wall-Run Corridor + Rooftops)
            CaptureFrom(new Vector3(-18f, 18f, 18f), new Vector3(0f, 5f, 62f), Path.Combine(outDir, "cyber_stage1_preview.png"));

            // 3. Stage 2 Cyber Canyon Overview (Dual Wall-Runs & Grapple Swing)
            CaptureFrom(new Vector3(102f, 20f, 35f), new Vector3(120f, 8f, 82f), Path.Combine(outDir, "cyber_stage2_preview.png"));

            // 4. Stage 3 Archon Spire Boss Arena
            CaptureFrom(new Vector3(222f, 24f, 34f), new Vector3(240f, 10f, 60f), Path.Combine(outDir, "cyber_stage3_boss_preview.png"));

            // 5. Cyber Combat Arena Overview (Bots temporarily visible for preview)
            var arenaBots = Object.FindObjectsByType<CyberBotAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bot in arenaBots)
                bot.gameObject.SetActive(true);
            CaptureFrom(new Vector3(336f, 20f, -24f), new Vector3(360f, 3f, 4f), Path.Combine(outDir, "cyber_arena_preview.png"));
            foreach (var bot in arenaBots)
                bot.gameObject.SetActive(false);
        }

        private static void CaptureFrom(Vector3 pos, Vector3 lookAt, string outPath)
        {
            var go = new GameObject("TempCyberPreviewCam");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.10f);
            cam.fieldOfView = 62f;
            cam.transform.position = pos;
            cam.transform.LookAt(lookAt);

            RenderToPng(cam, outPath);
            Object.DestroyImmediate(go);
        }

        private static void RenderToPng(Camera cam, string outPath)
        {
            int w = 1280, h = 720;
            var prevRt = cam.targetTexture;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[CyberPreviewCapture] Saved: {outPath}");
        }
    }
}
