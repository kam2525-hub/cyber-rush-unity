using System.Collections;
using UnityEngine;

namespace CyberRush
{
    /// <summary>
    /// Procedural 3D neon katana crescent arcs, voxel shatter explosions,
    /// dash wind streaks, and hit-stop freeze frames.
    /// </summary>
    public static class CyberVFX
    {
        private static Shader UnlitVfxShader =>
            Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");

        private static Coroutine _hitStopRoutine;

        public static void TriggerHitStop(MonoBehaviour host, float duration = 0.045f, float scale = 0.08f)
        {
            if (host == null) return;
            if (_hitStopRoutine != null) host.StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = host.StartCoroutine(HitStopCoroutine(duration, scale));
        }

        private static IEnumerator HitStopCoroutine(float dur, float scale)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(dur);
            // Only restore normal flow if slow-mo isn't currently overriding
            if (Mathf.Approximately(Time.timeScale, scale))
                Time.timeScale = prev > 0.4f ? 1f : prev;
            _hitStopRoutine = null;
        }

        public static void SpawnCrescentSlash(
            MonoBehaviour host,
            Vector3 origin,
            Quaternion facingRot,
            float rollDeg,
            float pitchDeg,
            float sweepDeg,
            float outerRadius,
            float innerRadius,
            Color coreColor,
            Color outerColor,
            float duration = 0.18f,
            bool reverseSweep = false)
        {
            if (host == null) return;
            GameObject go = new GameObject("CyberCrescentSlash");
            go.transform.position = origin;
            go.transform.rotation = facingRot * Quaternion.Euler(pitchDeg, 0f, rollDeg);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Mesh mesh = BuildCrescentMesh(sweepDeg, outerRadius, innerRadius, coreColor, outerColor);
            mf.sharedMesh = mesh;

            Material mat = new Material(UnlitVfxShader);
            mat.color = Color.white;
            mr.sharedMaterial = mat;

            host.StartCoroutine(AnimateCrescent(go, mesh, mat, duration, reverseSweep ? -70f : 70f));
        }

        public static void SpawnShatterExplosion(MonoBehaviour host, Vector3 pos, Color accentColor, int shardCount = 14)
        {
            if (host == null) return;
            Shader hardShader = Shader.Find("Custom/CyberHardSurface") ?? UnlitVfxShader;
            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(shard.GetComponent<Collider>());
                shard.name = "CyberShard";
                shard.transform.position = pos + Random.insideUnitSphere * 0.35f;
                shard.transform.rotation = Random.rotation;
                float s = Random.Range(0.10f, 0.28f);
                shard.transform.localScale = new Vector3(s, s * Random.Range(0.5f, 1.8f), s);

                var mr = shard.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Material m = new Material(hardShader);
                bool isGlow = i % 2 == 0;
                Color col = isGlow ? accentColor : new Color(0.08f, 0.09f, 0.14f);
                m.SetColor("_Color", col);
                if (m.HasProperty("_EmissionColor"))
                    m.SetColor("_EmissionColor", isGlow ? accentColor * 3.8f : Color.black);
                mr.sharedMaterial = m;

                Vector3 vel = Random.onUnitSphere * Random.Range(6f, 15f) + Vector3.up * Random.Range(2f, 6f);
                Vector3 spin = Random.onUnitSphere * Random.Range(180f, 540f);
                host.StartCoroutine(AnimateShard(shard, m, vel, spin, Random.Range(0.45f, 0.75f)));
            }
        }

        public static void SpawnDashStreaks(MonoBehaviour host, Vector3 pos, Quaternion dirRot, Color color, int count = 7)
        {
            if (host == null) return;
            Shader hardShader = Shader.Find("Custom/CyberHardSurface") ?? UnlitVfxShader;
            for (int i = 0; i < count; i++)
            {
                GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(streak.GetComponent<Collider>());
                float ang = i * (Mathf.PI * 2f / count);
                Vector3 offset = dirRot * new Vector3(Mathf.Cos(ang) * 0.55f, Mathf.Sin(ang) * 0.35f, 0.45f);
                streak.transform.position = pos + offset;
                streak.transform.rotation = dirRot;
                streak.transform.localScale = new Vector3(0.03f, 0.03f, 1.3f);

                var mr = streak.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Material m = new Material(hardShader);
                m.SetColor("_Color", color);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 3.5f);
                mr.sharedMaterial = m;

                Vector3 moveDir = -(dirRot * Vector3.forward);
                host.StartCoroutine(AnimateStreak(streak, m, moveDir * 14f, 0.18f));
            }
        }

        public static void SpawnRingPulse(MonoBehaviour host, Vector3 pos, Color color, float maxScale = 4.5f, float dur = 0.30f)
        {
            if (host == null) return;
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position = pos;
            ring.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);

            var mr = ring.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material m = new Material(Shader.Find("Custom/CyberHardSurface") ?? UnlitVfxShader);
            m.SetColor("_Color", color);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 3.5f);
            mr.sharedMaterial = m;

            host.StartCoroutine(AnimateRing(ring, m, maxScale, dur));
        }

        private static Mesh BuildCrescentMesh(float sweepDeg, float outerR, float innerR, Color coreCol, Color outerCol)
        {
            const int segs = 24;
            int vertCount = (segs + 1) * 2;
            Vector3[] verts = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];
            int[] tris = new int[segs * 12];
            float halfRad = sweepDeg * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float ang = Mathf.Lerp(-halfRad, halfRad, t);
                float taper = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.72f);
                float rIn = Mathf.Lerp(outerR, innerR, taper);

                float sx = Mathf.Sin(ang);
                float sz = Mathf.Cos(ang);
                int vIdx = i * 2;
                verts[vIdx] = new Vector3(sx * rIn, 0f, sz * rIn);
                verts[vIdx + 1] = new Vector3(sx * outerR, 0f, sz * outerR);

                float alpha = Mathf.Clamp01(t * 1.4f) * Mathf.Sin(t * Mathf.PI);
                colors[vIdx] = new Color(outerCol.r, outerCol.g, outerCol.b, 0.14f * alpha);
                colors[vIdx + 1] = new Color(coreCol.r, coreCol.g, coreCol.b, alpha);
            }

            int ti = 0;
            for (int i = 0; i < segs; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = c;
                tris[ti++] = c; tris[ti++] = b; tris[ti++] = d;
                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = c; tris[ti++] = d; tris[ti++] = b;
            }

            Mesh m = new Mesh { name = "CyberCrescentMesh" };
            m.vertices = verts;
            m.colors = colors;
            m.triangles = tris;
            m.RecalculateNormals();
            return m;
        }

        private static IEnumerator AnimateCrescent(GameObject go, Mesh mesh, Material mat, float dur, float spinDeg)
        {
            float t = 0f;
            Quaternion startRot = go.transform.rotation;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                go.transform.rotation = startRot * Quaternion.Euler(0f, Mathf.Lerp(-spinDeg * 0.45f, spinDeg * 0.55f, p), 0f);
                float sc = Mathf.Lerp(0.84f, 1.25f, p);
                go.transform.localScale = new Vector3(sc, 1f, sc);
                Color c = Color.white;
                c.a = Mathf.Pow(1f - p, 1.3f);
                mat.color = c;
                yield return null;
            }
            Object.Destroy(mesh);
            Object.Destroy(mat);
            Object.Destroy(go);
        }

        private static IEnumerator AnimateShard(GameObject go, Material m, Vector3 vel, Vector3 spin, float dur)
        {
            float t = 0f;
            Vector3 startScale = go.transform.localScale;
            while (t < dur)
            {
                float dt = Time.deltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / dur);
                vel.y -= 18f * dt;
                go.transform.position += vel * dt;
                go.transform.Rotate(spin * dt);
                go.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p * p);
                yield return null;
            }
            Object.Destroy(m);
            Object.Destroy(go);
        }

        private static IEnumerator AnimateStreak(GameObject go, Material m, Vector3 vel, float dur)
        {
            float t = 0f;
            Vector3 s0 = go.transform.localScale;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                go.transform.position += vel * Time.unscaledDeltaTime;
                go.transform.localScale = Vector3.Lerp(s0, new Vector3(0.004f, 0.004f, 0.35f), p);
                yield return null;
            }
            Object.Destroy(m);
            Object.Destroy(go);
        }

        private static IEnumerator AnimateRing(GameObject go, Material m, float maxScale, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.5f, maxScale, p);
                go.transform.localScale = new Vector3(s, 0.04f * (1f - p * 0.8f), s);
                yield return null;
            }
            Object.Destroy(m);
            Object.Destroy(go);
        }
    }
}
