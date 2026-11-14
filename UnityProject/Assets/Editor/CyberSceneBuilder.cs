using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CyberRush.Editor
{
    public static class CyberSceneBuilder
    {
        private static Material _gridCyan;
        private static Material _gridMagenta;
        private static Material _gridGold;
        private static Material _cityTowerMat;

        [MenuItem("CyberRush/Build CyberRush & Executable")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Synthwave Night Atmosphere
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.14f, 0.22f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.08f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.14f);
            RenderSettings.fogDensity = 0.0085f;

            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
                sun.color = new Color(0.75f, 0.90f, 1.0f);
                sun.intensity = 1.05f;
            }

            CreateGridMaterials();

            // 1. FPS Players (P1 + P2 Split-Screen) with Katana, Blaster & 3P Cyber Avatar
            BuildPlayerRig("CyberPlayer", 0, new Vector3(0f, 2.0f, 0f), Quaternion.identity, true, selfBodyLayer: 8);
            var p2 = BuildPlayerRig("CyberPlayer2", 1, new Vector3(360f, 2.2f, 20f), Quaternion.Euler(0f, 180f, 0f), false, selfBodyLayer: 9);
            p2.SetActive(false);

            // 2. Build 15 High-Speed Parkour & Slow-Mo Action Stages
            BuildStage1(0f);
            BuildStage2(120f);
            BuildStage3(240f);
            BuildBattleArena(360f);
            BuildStage4(480f);
            BuildStage5(600f);
            BuildStage6(720f);
            BuildStage7(840f);
            BuildStage8(960f);
            BuildStage9(1080f);
            BuildStage10(1200f);
            BuildStage11(1320f);
            BuildStage12(1440f);
            BuildStage13(1560f);
            BuildStage14(1680f);
            BuildStage15(1800f);
            BuildNeonSkylineBackdrop();

            // 3. Stage Manager, Audio, LAN & HUD
            var gm = new GameObject("CyberManagers");
            gm.AddComponent<CyberAudio>();
            gm.AddComponent<LanPvpManager>();
            gm.AddComponent<StageManager>();
            gm.AddComponent<CyberHUD>();

            // 4. Save Scene & Build Standalone Executable
            Directory.CreateDirectory("Assets/Scenes");
            string scenePath = "Assets/Scenes/CyberRush.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));
            Directory.CreateDirectory(buildDir);
            string exePath = Path.Combine(buildDir, "CyberRush.exe");

            var report = BuildPipeline.BuildPlayer(
                new[] { scenePath },
                exePath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None
            );
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Build failed: {report.summary.result}");
            }
            Debug.Log($"[CyberSceneBuilder] CYBER RUSH BUILD SUCCEEDED: {exePath}");
        }

        [MenuItem("CyberRush/Build WebGL (docs/ for GitHub Pages)")]
        public static void BuildWebGL()
        {
            string scenePath = "Assets/Scenes/CyberRush.unity";
            if (!File.Exists(scenePath))
                throw new System.Exception("CyberRush.unity does not exist yet. Run BuildAll first.");

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.productName = "CYBER RUSH";
            PlayerSettings.companyName = "kam2525-hub";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            string docsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs"));
            if (Directory.Exists(docsDir)) Directory.Delete(docsDir, true);
            Directory.CreateDirectory(docsDir);

            var report = BuildPipeline.BuildPlayer(
                new[] { scenePath },
                docsDir,
                BuildTarget.WebGL,
                BuildOptions.None
            );
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception($"WebGL build failed: {report.summary.result}");

            File.WriteAllText(Path.Combine(docsDir, ".nojekyll"), "");
            Debug.Log($"[CyberSceneBuilder] CYBER RUSH WEBGL BUILD SUCCEEDED: {docsDir}");
        }

        private static GameObject BuildPlayerRig(string name, int playerId, Vector3 pos, Quaternion rot, bool isMainCamera, int selfBodyLayer)
        {
            var playerGo = new GameObject(name);
            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.radius = 0.38f;
            playerGo.transform.position = pos;
            playerGo.transform.rotation = rot;

            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(playerGo.transform, false);
            camHolder.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            GameObject camObj;
            if (isMainCamera && Camera.main != null)
            {
                camObj = Camera.main.gameObject;
                var listener = camObj.GetComponent<AudioListener>();
                if (listener != null) Object.DestroyImmediate(listener);
            }
            else
            {
                camObj = new GameObject(playerId == 0 ? "Main Camera" : "Player2 Camera");
            }
            if (isMainCamera) camObj.tag = "MainCamera";
            camObj.transform.SetParent(camHolder.transform, false);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;

            var cam = camObj.GetComponent<Camera>();
            if (cam == null) cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.10f);
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 320f;
            cam.cullingMask &= ~(1 << selfBodyLayer);

            // FPS Katana Viewmodel
            var katanaPivot = new GameObject("KatanaPivot");
            katanaPivot.transform.SetParent(camObj.transform, false);
            katanaPivot.transform.localPosition = new Vector3(0.42f, -0.34f, 0.56f);
            katanaPivot.transform.localRotation = Quaternion.Euler(-12f, -14f, 8f);
            var katanaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cyber_Katana.fbx");
            if (katanaPrefab != null)
            {
                var k = (GameObject)PrefabUtility.InstantiatePrefab(katanaPrefab, katanaPivot.transform);
                k.name = "Cyber_Katana";
                k.transform.localPosition = Vector3.zero;
                k.transform.localRotation = Quaternion.identity;
                k.transform.localScale = Vector3.one * 0.95f;
                ApplyHardSurfaceMaterials(k, outline: 0.0018f);
                foreach (var c in k.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }

            // FPS Cyber Blaster Viewmodel
            var blasterPivot = new GameObject("BlasterPivot");
            blasterPivot.transform.SetParent(camObj.transform, false);
            blasterPivot.transform.localPosition = new Vector3(0.34f, -0.26f, 0.52f);
            blasterPivot.transform.localRotation = Quaternion.Euler(2f, -4f, 0f);
            var blasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cyber_Blaster.fbx");
            if (blasterPrefab != null)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(blasterPrefab, blasterPivot.transform);
                b.name = "Cyber_Blaster";
                b.transform.localPosition = Vector3.zero;
                b.transform.localRotation = Quaternion.identity;
                b.transform.localScale = Vector3.one * 0.85f;
                ApplyHardSurfaceMaterials(b, outline: 0.0016f);
                foreach (var c in b.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            blasterPivot.SetActive(false);

            // Third-person Body Mesh (visible to opposing player in Split-Screen, culled from own FPS cam)
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cyber_NinjaBot.fbx");
            if (bodyPrefab != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, playerGo.transform);
                body.name = "Player3PBody";
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                ApplyHardSurfaceMaterials(body, outline: 0.0022f);
                SetLayerRecursively(body, selfBodyLayer);
                foreach (var c in body.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }

            var pc = playerGo.AddComponent<CyberParkourController>();
            pc.playerId = playerId;
            pc.cameraHolder = camHolder.transform;
            pc.playerCamera = cam;
            pc.katanaPivot = katanaPivot.transform;
            pc.blasterPivot = blasterPivot.transform;
            return playerGo;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform t in obj.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        private static void CreateGridMaterials()
        {
            Shader gridShader = Shader.Find("Custom/CyberNeonGrid") ?? Shader.Find("Standard");
            _gridCyan = new Material(gridShader) { name = "Grid_Cyan" };
            _gridCyan.SetColor("_BaseColor", new Color(0.03f, 0.05f, 0.12f));
            _gridCyan.SetColor("_GridColor", new Color(0.12f, 0.92f, 1.0f) * 1.4f);
            _gridCyan.SetFloat("_GridScale", 0.5f);

            _gridMagenta = new Material(gridShader) { name = "Grid_Magenta" };
            _gridMagenta.SetColor("_BaseColor", new Color(0.08f, 0.03f, 0.12f));
            _gridMagenta.SetColor("_GridColor", new Color(1.0f, 0.16f, 0.72f) * 1.6f);
            _gridMagenta.SetFloat("_GridScale", 0.5f);

            _gridGold = new Material(gridShader) { name = "Grid_Gold" };
            _gridGold.SetColor("_BaseColor", new Color(0.07f, 0.05f, 0.04f));
            _gridGold.SetColor("_GridColor", new Color(1.0f, 0.78f, 0.22f) * 1.5f);
            _gridGold.SetFloat("_GridScale", 0.5f);

            _cityTowerMat = new Material(gridShader) { name = "Grid_Tower" };
            _cityTowerMat.SetColor("_BaseColor", new Color(0.02f, 0.025f, 0.07f));
            _cityTowerMat.SetColor("_GridColor", new Color(0.18f, 0.45f, 0.95f) * 0.9f);
            _cityTowerMat.SetFloat("_GridScale", 0.25f);
            _cityTowerMat.SetFloat("_MajorDiv", 3.0f);
        }

        private static GameObject CreateBlock(string name, Vector3 pos, Vector3 scale, Material mat, bool isWallRun = false, Vector3 rotEuler = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (rotEuler != default) go.transform.rotation = Quaternion.Euler(rotEuler);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (isWallRun) go.AddComponent<WallRunSurface>();
            return go;
        }

        private static void SpawnProp(string fbxPath, string name, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            ApplyHardSurfaceMaterials(go, outline: 0.0025f);
        }

        private static void SpawnJumpPad(Vector3 pos, float boost = 19.5f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Jump_Pad.fbx");
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "JumpPad";
            go.transform.position = pos;
            ApplyHardSurfaceMaterials(go, 0.002f);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.6f;
            col.center = new Vector3(0f, 0.6f, 0f);
            var jp = go.AddComponent<JumpPad>();
            jp.launchSpeed = boost;
        }

        private static void SpawnGrapple(Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Grapple_Anchor.fbx");
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GrappleAnchor";
            go.transform.position = pos;
            ApplyHardSurfaceMaterials(go, 0.002f);
            go.AddComponent<GrappleAnchor>();
        }

        private static void SpawnGoalPortal(int stageIdx, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Goal_Portal.fbx");
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"GoalPortal_S{stageIdx}";
            go.transform.position = pos;
            ApplyHardSurfaceMaterials(go, 0.0025f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(6.5f, 7.5f, 2.8f);
            col.center = new Vector3(0f, 3.8f, 0f);
            var gp = go.AddComponent<GoalPortal>();
            gp.stageIndex = stageIdx;
        }

        private static void SpawnCyberEnemy(string fbxPath, string name, int stageIdx, EnemyKind kind, Vector3 pos, Quaternion rot, int hp = 1)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            ApplyHardSurfaceMaterials(go, 0.0025f);

            var col = go.AddComponent<SphereCollider>();
            col.radius = kind == EnemyKind.CoreBoss ? 2.4f : (kind == EnemyKind.ShieldPylon ? 1.5f : 1.05f);
            col.center = new Vector3(0f, kind == EnemyKind.CoreBoss ? 2.4f : 1.25f, 0f);

            var ai = go.AddComponent<CyberEnemy>();
            ai.stageIndex = stageIdx;
            ai.kind = kind;
            ai.maxHp = hp;
        }

        // =====================================================================
        // STAGE 1: NEON SKYLINE
        // =====================================================================
        private static void BuildStage1(float ox)
        {
            CreateBlock("S1_StartRoof", new Vector3(ox, 0f, 6f), new Vector3(9f, 2f, 18f), _gridCyan);
            CreateBlock("S1_BridgeRamp", new Vector3(ox, 1.2f, 21f), new Vector3(5.5f, 1f, 10f), _gridCyan, rotEuler: new Vector3(-12f, 0f, 0f));

            CreateBlock("S1_MidRoof", new Vector3(ox, 2.2f, 38f), new Vector3(11f, 2f, 22f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S1_Drone_1", 1, EnemyKind.Drone, new Vector3(ox, 3.3f, 35f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S1_Trooper_1", 1, EnemyKind.Trooper, new Vector3(ox + 2.2f, 3.2f, 44f), Quaternion.Euler(0, 180f, 0));

            // Dual Wall-Run Corridor (Glowing Magenta Billboards!)
            CreateBlock("S1_WallL", new Vector3(ox - 5.2f, 6.2f, 64f), new Vector3(1.2f, 8f, 26f), _gridMagenta, isWallRun: true);
            CreateBlock("S1_WallR", new Vector3(ox + 5.2f, 6.2f, 70f), new Vector3(1.2f, 8f, 24f), _gridMagenta, isWallRun: true);
            SpawnJumpPad(new Vector3(ox, 3.25f, 47.5f), 14.5f);

            CreateBlock("S1_UpperRoof", new Vector3(ox, 5.5f, 88f), new Vector3(12f, 2f, 18f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S1_Drone_2", 1, EnemyKind.Drone, new Vector3(ox - 2f, 7.0f, 87f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S1_Trooper_2", 1, EnemyKind.Trooper, new Vector3(ox + 2f, 6.5f, 92f), Quaternion.Euler(0, 180f, 0));

            // Grapple Abyss crossing to Goal Apex
            SpawnGrapple(new Vector3(ox, 12.5f, 108f));
            CreateBlock("S1_GoalRoof", new Vector3(ox, 8.5f, 126f), new Vector3(14f, 2.4f, 22f), _gridGold);
            SpawnGoalPortal(1, new Vector3(ox, 9.7f, 132f));
        }

        // =====================================================================
        // STAGE 2: CYBER CANYON
        // =====================================================================
        private static void BuildStage2(float ox)
        {
            CreateBlock("S2_Start", new Vector3(ox, 0f, 5f), new Vector3(9f, 2f, 16f), _gridCyan);
            CreateBlock("S2_Wall_1L", new Vector3(ox - 5.5f, 4.5f, 25f), new Vector3(1.2f, 8.5f, 24f), _gridMagenta, isWallRun: true);
            CreateBlock("S2_Wall_2R", new Vector3(ox + 5.5f, 6.5f, 48f), new Vector3(1.2f, 8.5f, 24f), _gridMagenta, isWallRun: true);

            CreateBlock("S2_IslandMid", new Vector3(ox, 5.0f, 69f), new Vector3(12f, 2f, 18f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S2_Trooper_1", 2, EnemyKind.Trooper, new Vector3(ox - 2.5f, 6.0f, 69f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S2_Trooper_2", 2, EnemyKind.Trooper, new Vector3(ox + 2.5f, 6.0f, 73f), Quaternion.Euler(0, 180f, 0));

            // Double Grapple Swing across wide Neon Canyon
            SpawnGrapple(new Vector3(ox - 3.5f, 11.5f, 89f));
            SpawnGrapple(new Vector3(ox + 3.5f, 14.5f, 108f));

            CreateBlock("S2_PreGoal", new Vector3(ox, 8.5f, 125f), new Vector3(11f, 2f, 18f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S2_Drone_1", 2, EnemyKind.Drone, new Vector3(ox, 10.5f, 123f), Quaternion.Euler(0, 180f, 0));
            SpawnJumpPad(new Vector3(ox, 9.5f, 131f), 20.5f);

            CreateBlock("S2_Apex", new Vector3(ox, 17.5f, 148f), new Vector3(14f, 2.4f, 20f), _gridGold);
            SpawnGoalPortal(2, new Vector3(ox, 18.7f, 154f));
        }

        // =====================================================================
        // STAGE 3: ARCHON SPIRE (FINAL BOSS + 3 SHIELD PYLONS)
        // =====================================================================
        private static void BuildStage3(float ox)
        {
            CreateBlock("S3_LaunchPad", new Vector3(ox, 0f, 5f), new Vector3(10f, 2f, 16f), _gridCyan);
            SpawnJumpPad(new Vector3(ox, 1.05f, 10f), 18.5f);
            CreateBlock("S3_WallEntryL", new Vector3(ox - 5.5f, 8.0f, 26f), new Vector3(1.2f, 9f, 22f), _gridMagenta, isWallRun: true);
            CreateBlock("S3_WallEntryR", new Vector3(ox + 5.5f, 8.0f, 26f), new Vector3(1.2f, 9f, 22f), _gridMagenta, isWallRun: true);

            // Massive Archon Spire Arena
            CreateBlock("S3_BossArena", new Vector3(ox, 7.0f, 58f), new Vector3(34f, 2.6f, 34f), _gridMagenta);
            SpawnCyberEnemy("Assets/Models/Cyber_CoreBoss.fbx", "S3_ArchonCoreBoss", 3, EnemyKind.CoreBoss, new Vector3(ox, 8.8f, 60f), Quaternion.Euler(0, 180f, 0), hp: 8);

            // 3 Shield Pylons mounted around elevated flanks
            Vector3[] pylonPlatforms =
            {
                new Vector3(ox - 15f, 10.5f, 54f),
                new Vector3(ox + 15f, 10.5f, 54f),
                new Vector3(ox, 12.0f, 75f),
            };
            for (int i = 0; i < pylonPlatforms.Length; i++)
            {
                Vector3 p = pylonPlatforms[i];
                CreateBlock($"S3_PylonPlat_{i}", p, new Vector3(7f, 1.6f, 7f), _gridGold);
                SpawnCyberEnemy("Assets/Models/Grapple_Anchor.fbx", $"S3_ShieldPylon_{i}", 3, EnemyKind.ShieldPylon, p + new Vector3(0f, 2.2f, 0f), Quaternion.identity);
                SpawnGrapple(p + new Vector3(0f, 5.2f, -3f));
            }

            // Defending Drones around Core
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S3_Drone_L", 3, EnemyKind.Drone, new Vector3(ox - 7f, 9.5f, 52f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S3_Drone_R", 3, EnemyKind.Drone, new Vector3(ox + 7f, 9.5f, 52f), Quaternion.Euler(0, 180f, 0));
        }

        // =====================================================================
        // STAGE 4: KILL-CHAIN GAUNTLET (HIGH-DENSITY SLOW-MO TAKEDOWN CORRIDOR)
        // =====================================================================
        private static void BuildStage4(float ox)
        {
            // Entry Deck + First Combo Pair
            CreateBlock("S4_Start", new Vector3(ox, 0f, 6f), new Vector3(11f, 2f, 20f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_Trooper_1", 4, EnemyKind.Trooper, new Vector3(ox - 2.5f, 1.0f, 11f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_Trooper_2", 4, EnemyKind.Trooper, new Vector3(ox + 2.5f, 1.0f, 14f), Quaternion.Euler(0, 180f, 0));
            SpawnJumpPad(new Vector3(ox, 1.05f, 15.5f), 17.5f);

            // Aerial Wall-Run Gallery with 4 Hovering Drones for Mid-Air Slow-Mo Chains
            CreateBlock("S4_WallL1", new Vector3(ox - 6.0f, 6.5f, 34f), new Vector3(1.2f, 9f, 26f), _gridMagenta, isWallRun: true);
            CreateBlock("S4_WallR1", new Vector3(ox + 6.0f, 6.5f, 50f), new Vector3(1.2f, 9f, 26f), _gridMagenta, isWallRun: true);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_1", 4, EnemyKind.Drone, new Vector3(ox - 2.2f, 7.2f, 27f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_2", 4, EnemyKind.Drone, new Vector3(ox + 2.2f, 7.8f, 36f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_3", 4, EnemyKind.Drone, new Vector3(ox - 1.5f, 8.2f, 46f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_4", 4, EnemyKind.Drone, new Vector3(ox + 1.8f, 8.5f, 56f), Quaternion.Euler(0, 180f, 0));

            // Mid Gauntlet Plaza: 3 Troopers + 2 Flanking Drones (Ideal for Cyclone Spin & Sword Wave!)
            CreateBlock("S4_MidPlaza", new Vector3(ox, 5.5f, 76f), new Vector3(18f, 2f, 26f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_Trooper_3", 4, EnemyKind.Trooper, new Vector3(ox - 4.5f, 6.5f, 72f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_Trooper_4", 4, EnemyKind.Trooper, new Vector3(ox, 6.5f, 77f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_Trooper_5", 4, EnemyKind.Trooper, new Vector3(ox + 4.5f, 6.5f, 82f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_5", 4, EnemyKind.Drone, new Vector3(ox - 6.0f, 9.2f, 76f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_6", 4, EnemyKind.Drone, new Vector3(ox + 6.0f, 9.2f, 80f), Quaternion.Euler(0, 180f, 0));

            // Grapple Strike Stretch -> Apex Goal
            SpawnGrapple(new Vector3(ox, 13.0f, 100f));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S4_Drone_7", 4, EnemyKind.Drone, new Vector3(ox, 11.2f, 111f), Quaternion.Euler(0, 180f, 0));
            SpawnGrapple(new Vector3(ox, 14.5f, 122f));

            CreateBlock("S4_GoalRoof", new Vector3(ox, 9.0f, 140f), new Vector3(16f, 2.4f, 24f), _gridGold);
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S4_GuardLast", 4, EnemyKind.Trooper, new Vector3(ox, 10.2f, 134f), Quaternion.Euler(0, 180f, 0));
            SpawnGoalPortal(4, new Vector3(ox, 10.2f, 146f));
        }

        // =====================================================================
        // STAGE 5: SKY FORTRESS (MULTI-WAVE ENEMIES + CITADEL CORE BOSS)
        // =====================================================================
        private static void BuildStage5(float ox)
        {
            CreateBlock("S5_Start", new Vector3(ox, 0f, 5f), new Vector3(12f, 2f, 18f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_IntroDrone", 5, EnemyKind.Drone, new Vector3(ox, 2.4f, 11f), Quaternion.Euler(0, 180f, 0));
            SpawnJumpPad(new Vector3(ox, 1.05f, 12.5f), 20.0f);

            // Skyway Bridge Deck 1
            CreateBlock("S5_SkyBridge", new Vector3(ox, 7.5f, 36f), new Vector3(14f, 2f, 28f), _gridCyan);
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S5_T1", 5, EnemyKind.Trooper, new Vector3(ox - 3.5f, 8.5f, 30f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S5_T2", 5, EnemyKind.Trooper, new Vector3(ox + 3.5f, 8.5f, 34f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_D1", 5, EnemyKind.Drone, new Vector3(ox, 10.8f, 38f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S5_T3", 5, EnemyKind.Trooper, new Vector3(ox, 8.5f, 44f), Quaternion.Euler(0, 180f, 0));

            // Dual Wall-Run Ascent with Mid-Air Targets
            CreateBlock("S5_WallL", new Vector3(ox - 6.5f, 12.5f, 64f), new Vector3(1.2f, 9f, 26f), _gridMagenta, isWallRun: true);
            CreateBlock("S5_WallR", new Vector3(ox + 6.5f, 12.5f, 64f), new Vector3(1.2f, 9f, 26f), _gridMagenta, isWallRun: true);
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_D2", 5, EnemyKind.Drone, new Vector3(ox - 2.5f, 13.5f, 60f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_D3", 5, EnemyKind.Drone, new Vector3(ox + 2.5f, 14.0f, 69f), Quaternion.Euler(0, 180f, 0));
            SpawnGrapple(new Vector3(ox, 18.5f, 80f));

            // Grand Sky Fortress Citadel
            CreateBlock("S5_FortressDeck", new Vector3(ox, 11.0f, 108f), new Vector3(38f, 2.6f, 38f), _gridMagenta);
            SpawnCyberEnemy("Assets/Models/Cyber_CoreBoss.fbx", "S5_FortressArchonBoss", 5, EnemyKind.CoreBoss, new Vector3(ox, 12.8f, 112f), Quaternion.Euler(0, 180f, 0), hp: 10);

            // 2 Flanking Shield Pylons + Citadel Escort Squad
            Vector3[] s5Pylons = { new Vector3(ox - 16f, 14.2f, 108f), new Vector3(ox + 16f, 14.2f, 108f) };
            for (int i = 0; i < s5Pylons.Length; i++)
            {
                CreateBlock($"S5_PylonPlat_{i}", s5Pylons[i], new Vector3(7f, 1.6f, 7f), _gridGold);
                SpawnCyberEnemy("Assets/Models/Grapple_Anchor.fbx", $"S5_ShieldPylon_{i}", 5, EnemyKind.ShieldPylon, s5Pylons[i] + new Vector3(0f, 2.2f, 0f), Quaternion.identity);
                SpawnGrapple(s5Pylons[i] + new Vector3(0f, 5.4f, -3f));
            }
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S5_CitadelT1", 5, EnemyKind.Trooper, new Vector3(ox - 7f, 12.3f, 98f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Trooper.fbx", "S5_CitadelT2", 5, EnemyKind.Trooper, new Vector3(ox + 7f, 12.3f, 98f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_CitadelD1", 5, EnemyKind.Drone, new Vector3(ox - 8f, 15.2f, 105f), Quaternion.Euler(0, 180f, 0));
            SpawnCyberEnemy("Assets/Models/Cyber_Drone.fbx", "S5_CitadelD2", 5, EnemyKind.Drone, new Vector3(ox + 8f, 15.2f, 105f), Quaternion.Euler(0, 180f, 0));
        }

        private static void SpawnSquadEnemy(int stageIdx, string prefix, int idx, EnemyKind kind, Vector3 pos, int hp = 1)
        {
            string fbx = kind == EnemyKind.Drone ? "Assets/Models/Cyber_Drone.fbx"
                : (kind == EnemyKind.CoreBoss ? "Assets/Models/Cyber_CoreBoss.fbx"
                : (kind == EnemyKind.ShieldPylon ? "Assets/Models/Grapple_Anchor.fbx" : "Assets/Models/Cyber_Trooper.fbx"));
            SpawnCyberEnemy(fbx, $"S{stageIdx}_{prefix}_{idx}", stageIdx, kind, pos, Quaternion.Euler(0, 180f, 0), hp);
        }

        // STAGE 6: SKYLINE ALTERNATE NIGHT RAID (UPPER GRAPPLE vs LOWER TROOPER GAUNTLET)
        private static void BuildStage6(float ox)
        {
            CreateBlock("S6_Start", new Vector3(ox, 0f, 6f), new Vector3(11f, 2f, 20f), _gridCyan);
            SpawnSquadEnemy(6, "T", 1, EnemyKind.Trooper, new Vector3(ox - 2f, 1.0f, 10f));
            SpawnSquadEnemy(6, "D", 1, EnemyKind.Drone, new Vector3(ox + 2f, 3.4f, 14f));
            SpawnJumpPad(new Vector3(ox - 2f, 1.05f, 14f), 17.5f);

            // Upper Grapple Skyline Route
            SpawnGrapple(new Vector3(ox - 4f, 11.5f, 32f));
            SpawnSquadEnemy(6, "D", 2, EnemyKind.Drone, new Vector3(ox - 4f, 9.8f, 43f));
            SpawnGrapple(new Vector3(ox - 4f, 12.5f, 54f));

            // Lower Street Gauntlet Route
            CreateBlock("S6_LowBridge", new Vector3(ox + 6.5f, 1.5f, 40f), new Vector3(8.5f, 1.8f, 36f), _gridMagenta);
            SpawnSquadEnemy(6, "T", 2, EnemyKind.Trooper, new Vector3(ox + 5f, 2.5f, 30f));
            SpawnSquadEnemy(6, "T", 3, EnemyKind.Trooper, new Vector3(ox + 8f, 2.5f, 39f));
            SpawnSquadEnemy(6, "T", 4, EnemyKind.Trooper, new Vector3(ox + 6f, 2.5f, 49f));
            SpawnJumpPad(new Vector3(ox + 6.5f, 2.5f, 55f), 14.5f);

            // Converge Plaza + Dual Wall-Run Slow-Mo
            CreateBlock("S6_MidPlaza", new Vector3(ox, 4.5f, 74f), new Vector3(16f, 2f, 20f), _gridCyan);
            SpawnSquadEnemy(6, "T", 5, EnemyKind.Trooper, new Vector3(ox - 3.5f, 5.5f, 72f));
            SpawnSquadEnemy(6, "T", 6, EnemyKind.Trooper, new Vector3(ox + 3.5f, 5.5f, 76f));
            SpawnSquadEnemy(6, "D", 3, EnemyKind.Drone, new Vector3(ox, 8.2f, 75f));

            CreateBlock("S6_WallL", new Vector3(ox - 5.6f, 8.5f, 102f), new Vector3(1.2f, 9f, 28f), _gridMagenta, isWallRun: true);
            CreateBlock("S6_WallR", new Vector3(ox + 5.6f, 8.5f, 104f), new Vector3(1.2f, 9f, 28f), _gridMagenta, isWallRun: true);
            SpawnSquadEnemy(6, "D", 4, EnemyKind.Drone, new Vector3(ox - 1.5f, 9.2f, 96f));
            SpawnSquadEnemy(6, "D", 5, EnemyKind.Drone, new Vector3(ox + 1.5f, 9.6f, 106f));
            SpawnSquadEnemy(6, "D", 6, EnemyKind.Drone, new Vector3(ox, 9.8f, 115f));

            CreateBlock("S6_GoalRoof", new Vector3(ox, 7.5f, 134f), new Vector3(15f, 2.4f, 22f), _gridGold);
            SpawnSquadEnemy(6, "T", 7, EnemyKind.Trooper, new Vector3(ox, 8.8f, 129f));
            SpawnGoalPortal(6, new Vector3(ox, 8.7f, 140f));
        }

        // STAGE 7: TWIN-WALL SLOW-MO CANYON (4 ZIG-ZAG WALLS + AIR DRONE TAKEDOWNS)
        private static void BuildStage7(float ox)
        {
            CreateBlock("S7_Start", new Vector3(ox, 0f, 5f), new Vector3(10f, 2f, 16f), _gridCyan);
            SpawnJumpPad(new Vector3(ox, 1.05f, 11f), 17.0f);

            float[] wallX = { -5.4f, 5.4f, -5.4f, 5.4f };
            float[] wallZ = { 28f, 51f, 74f, 97f };
            for (int i = 0; i < 4; i++)
            {
                float wy = 5.5f + i * 1.2f;
                CreateBlock($"S7_Wall_{i}", new Vector3(ox + wallX[i], wy, wallZ[i]), new Vector3(1.2f, 9.5f, 24f), _gridMagenta, isWallRun: true);
                SpawnSquadEnemy(7, "D", i + 1, EnemyKind.Drone, new Vector3(ox + wallX[i] * 0.35f, wy + 1.2f, wallZ[i] + 4f));
            }

            CreateBlock("S7_Deck", new Vector3(ox, 7.0f, 120f), new Vector3(15f, 2f, 22f), _gridCyan);
            SpawnSquadEnemy(7, "T", 1, EnemyKind.Trooper, new Vector3(ox - 3.5f, 8.0f, 116f));
            SpawnSquadEnemy(7, "T", 2, EnemyKind.Trooper, new Vector3(ox, 8.0f, 121f));
            SpawnSquadEnemy(7, "T", 3, EnemyKind.Trooper, new Vector3(ox + 3.5f, 8.0f, 126f));

            SpawnGrapple(new Vector3(ox, 15.0f, 140f));
            CreateBlock("S7_Goal", new Vector3(ox, 9.5f, 158f), new Vector3(15f, 2.4f, 20f), _gridGold);
            SpawnGoalPortal(7, new Vector3(ox, 10.7f, 164f));
        }

        // STAGE 8: AERIAL TRAMPOLINE CHAIN (HIGH-ALTITUDE JUMP PAD SLOW-MO)
        private static void BuildStage8(float ox)
        {
            CreateBlock("S8_Start", new Vector3(ox, 0f, 6f), new Vector3(11f, 2f, 18f), _gridCyan);
            SpawnJumpPad(new Vector3(ox, 1.05f, 13f), 19.5f);

            for (int i = 0; i < 3; i++)
            {
                float iy = 6.5f + i * 6.5f;
                float iz = 36f + i * 32f;
                SpawnSquadEnemy(8, "DA", i * 2 + 1, EnemyKind.Drone, new Vector3(ox - 2.2f, iy + 2.5f, iz - 11f));
                SpawnSquadEnemy(8, "DA", i * 2 + 2, EnemyKind.Drone, new Vector3(ox + 2.2f, iy + 2.8f, iz - 8f));

                CreateBlock($"S8_Island_{i}", new Vector3(ox, iy, iz), new Vector3(14f, 2f, 18f), i == 2 ? _gridMagenta : _gridCyan);
                SpawnSquadEnemy(8, "T", i * 2 + 1, EnemyKind.Trooper, new Vector3(ox - 3f, iy + 1.0f, iz - 2f));
                SpawnSquadEnemy(8, "T", i * 2 + 2, EnemyKind.Trooper, new Vector3(ox + 3f, iy + 1.0f, iz + 2f));
                SpawnJumpPad(new Vector3(ox, iy + 1.05f, iz + 6.5f), 20.5f);
            }

            CreateBlock("S8_Goal", new Vector3(ox, 25.0f, 132f), new Vector3(16f, 2.4f, 22f), _gridGold);
            SpawnSquadEnemy(8, "DFinal", 1, EnemyKind.Drone, new Vector3(ox, 27.6f, 126f));
            SpawnGoalPortal(8, new Vector3(ox, 26.2f, 138f));
        }

        // STAGE 9: RAILGUN SLALOM HIGHWAY (HIGH-SPEED LINEUP MULTI-KILLS)
        private static void BuildStage9(float ox)
        {
            for (int s = 0; s < 3; s++)
            {
                float sz = 22f + s * 42f;
                float sy = s * 1.8f;
                CreateBlock($"S9_Hwy_{s}", new Vector3(ox, sy, sz), new Vector3(14f, 2f, 38f), s == 1 ? _gridMagenta : _gridCyan);
                CreateBlock($"S9_SideWall_{s}", new Vector3(ox + (s % 2 == 0 ? -7.2f : 7.2f), sy + 4.5f, sz), new Vector3(1.2f, 7.5f, 34f), _gridMagenta, isWallRun: true);

                // Line up 3 Troopers + 2 Drones in a satisfying Sword-Wave / Railgun row!
                for (int e = 0; e < 3; e++)
                    SpawnSquadEnemy(9, $"S{s}T", e + 1, EnemyKind.Trooper, new Vector3(ox + (e - 1) * 1.8f, sy + 1.0f, sz + e * 5f));
                SpawnSquadEnemy(9, $"S{s}D", 1, EnemyKind.Drone, new Vector3(ox - 3.5f, sy + 3.8f, sz + 6f));
                SpawnSquadEnemy(9, $"S{s}D", 2, EnemyKind.Drone, new Vector3(ox + 3.5f, sy + 3.8f, sz + 11f));

                if (s < 2)
                    CreateBlock($"S9_Ramp_{s}", new Vector3(ox, sy + 0.9f, sz + 21f), new Vector3(7f, 1f, 8f), _gridGold, rotEuler: new Vector3(-13f, 0f, 0f));
            }

            SpawnJumpPad(new Vector3(ox, 4.7f, 124f), 17.5f);
            CreateBlock("S9_Goal", new Vector3(ox, 9.5f, 146f), new Vector3(16f, 2.4f, 22f), _gridGold);
            SpawnGoalPortal(9, new Vector3(ox, 10.7f, 152f));
        }

        // STAGE 10: INFERNO CLIMB TOWER (VERTICAL SPIRAL + MINI-BOSS CITADEL)
        private static void BuildStage10(float ox)
        {
            CreateBlock("S10_Base", new Vector3(ox, 0f, 5f), new Vector3(12f, 2f, 16f), _gridCyan);
            SpawnJumpPad(new Vector3(ox + 2.5f, 1.05f, 10f), 17.5f);

            CreateBlock("S10_Tier1", new Vector3(ox + 9f, 6.0f, 24f), new Vector3(13f, 2f, 16f), _gridCyan);
            SpawnSquadEnemy(10, "T", 1, EnemyKind.Trooper, new Vector3(ox + 7f, 7.0f, 23f));
            SpawnSquadEnemy(10, "T", 2, EnemyKind.Trooper, new Vector3(ox + 11f, 7.0f, 26f));
            SpawnSquadEnemy(10, "D", 1, EnemyKind.Drone, new Vector3(ox + 9f, 9.4f, 28f));
            SpawnJumpPad(new Vector3(ox + 5f, 7.05f, 29f), 18.0f);

            CreateBlock("S10_Tier2", new Vector3(ox - 2f, 12.0f, 43f), new Vector3(14f, 2f, 16f), _gridMagenta);
            SpawnSquadEnemy(10, "T", 3, EnemyKind.Trooper, new Vector3(ox - 4f, 13.0f, 43f));
            SpawnSquadEnemy(10, "D", 2, EnemyKind.Drone, new Vector3(ox + 1f, 15.2f, 46f));
            SpawnGrapple(new Vector3(ox, 19.5f, 58f));

            CreateBlock("S10_MiniBossArena", new Vector3(ox, 15.0f, 78f), new Vector3(32f, 2.4f, 32f), _gridMagenta);
            SpawnSquadEnemy(10, "MiniArchonBoss", 1, EnemyKind.CoreBoss, new Vector3(ox, 16.8f, 81f), hp: 6);
            Vector3[] p10 = { new Vector3(ox - 13f, 18.0f, 78f), new Vector3(ox + 13f, 18.0f, 78f) };
            for (int i = 0; i < p10.Length; i++)
            {
                CreateBlock($"S10_PylonPlat_{i}", p10[i], new Vector3(6.5f, 1.5f, 6.5f), _gridGold);
                SpawnSquadEnemy(10, "Pylon", i + 1, EnemyKind.ShieldPylon, p10[i] + new Vector3(0f, 2.1f, 0f));
                SpawnGrapple(p10[i] + new Vector3(0f, 5.2f, -3f));
            }
            SpawnSquadEnemy(10, "DG", 1, EnemyKind.Drone, new Vector3(ox - 6f, 18.0f, 72f));
            SpawnSquadEnemy(10, "DG", 2, EnemyKind.Drone, new Vector3(ox + 6f, 18.0f, 72f));
        }

        // STAGE 11: CYBER CANYON HARD REMIX (TRIPLE GRAPPLE ABYSS + SNIPER DRONES)
        private static void BuildStage11(float ox)
        {
            CreateBlock("S11_Start", new Vector3(ox, 0f, 6f), new Vector3(9f, 2f, 18f), _gridCyan);
            CreateBlock("S11_W1", new Vector3(ox - 5.4f, 4.8f, 25f), new Vector3(1.2f, 8.5f, 22f), _gridMagenta, isWallRun: true);
            SpawnSquadEnemy(11, "D", 1, EnemyKind.Drone, new Vector3(ox - 1.5f, 6.2f, 30f));
            CreateBlock("S11_W2", new Vector3(ox + 5.4f, 6.8f, 47f), new Vector3(1.2f, 8.5f, 22f), _gridMagenta, isWallRun: true);

            // Triple Grapple Swing with Aerial Drones
            SpawnGrapple(new Vector3(ox, 12.5f, 68f));
            SpawnSquadEnemy(11, "D", 2, EnemyKind.Drone, new Vector3(ox - 2.5f, 10.5f, 78f));
            SpawnGrapple(new Vector3(ox - 3.5f, 14.0f, 88f));
            SpawnSquadEnemy(11, "D", 3, EnemyKind.Drone, new Vector3(ox + 2.5f, 11.5f, 98f));
            SpawnGrapple(new Vector3(ox + 3.5f, 15.5f, 108f));

            CreateBlock("S11_FortDeck", new Vector3(ox, 8.5f, 127f), new Vector3(14f, 2f, 20f), _gridCyan);
            for (int i = 0; i < 4; i++)
                SpawnSquadEnemy(11, "T", i + 1, EnemyKind.Trooper, new Vector3(ox + (i - 1.5f) * 2.8f, 9.5f, 124f + (i % 2) * 4f));
            SpawnJumpPad(new Vector3(ox, 9.55f, 134f), 20.0f);

            CreateBlock("S11_WallFinalL", new Vector3(ox - 5.5f, 15.5f, 152f), new Vector3(1.2f, 9f, 22f), _gridMagenta, isWallRun: true);
            CreateBlock("S11_Goal", new Vector3(ox, 17.5f, 173f), new Vector3(15f, 2.4f, 20f), _gridGold);
            SpawnGoalPortal(11, new Vector3(ox, 18.7f, 179f));
        }

        // STAGE 12: KILL-CHAIN EXTREME (15+ DENSE ENEMIES FOR NONSTOP SLOW-MO RUSH)
        private static void BuildStage12(float ox)
        {
            CreateBlock("S12_Entry", new Vector3(ox, 0f, 8f), new Vector3(13f, 2f, 24f), _gridCyan);
            SpawnSquadEnemy(12, "T", 1, EnemyKind.Trooper, new Vector3(ox - 3f, 1.0f, 10f));
            SpawnSquadEnemy(12, "T", 2, EnemyKind.Trooper, new Vector3(ox + 3f, 1.0f, 12f));
            SpawnSquadEnemy(12, "D", 1, EnemyKind.Drone, new Vector3(ox, 3.6f, 15f));
            SpawnJumpPad(new Vector3(ox, 1.05f, 17.5f), 17.5f);

            CreateBlock("S12_WallL", new Vector3(ox - 6.0f, 6.8f, 36f), new Vector3(1.2f, 9.5f, 26f), _gridMagenta, isWallRun: true);
            CreateBlock("S12_WallR", new Vector3(ox + 6.0f, 6.8f, 44f), new Vector3(1.2f, 9.5f, 26f), _gridMagenta, isWallRun: true);
            for (int i = 0; i < 4; i++)
                SpawnSquadEnemy(12, "D", i + 2, EnemyKind.Drone, new Vector3(ox + (i % 2 == 0 ? -2.2f : 2.2f), 7.6f + i * 0.35f, 28f + i * 8f));

            CreateBlock("S12_RushBridge", new Vector3(ox, 5.5f, 80f), new Vector3(16f, 2f, 42f), _gridCyan);
            for (int i = 0; i < 6; i++)
            {
                float z = 64f + i * 6f;
                float x = ox + ((i % 3) - 1) * 3.8f;
                SpawnSquadEnemy(12, "R", i + 1, i % 2 == 0 ? EnemyKind.Trooper : EnemyKind.Drone, new Vector3(x, i % 2 == 0 ? 6.5f : 8.8f, z));
            }

            SpawnGrapple(new Vector3(ox, 13.5f, 110f));
            SpawnSquadEnemy(12, "DF", 1, EnemyKind.Drone, new Vector3(ox - 2f, 11.2f, 118f));
            SpawnSquadEnemy(12, "DF", 2, EnemyKind.Drone, new Vector3(ox + 2f, 11.2f, 122f));
            SpawnGrapple(new Vector3(ox, 14.5f, 130f));

            CreateBlock("S12_Goal", new Vector3(ox, 9.0f, 148f), new Vector3(16f, 2.4f, 22f), _gridGold);
            SpawnSquadEnemy(12, "TL", 1, EnemyKind.Trooper, new Vector3(ox - 2.5f, 10.2f, 142f));
            SpawnSquadEnemy(12, "TL", 2, EnemyKind.Trooper, new Vector3(ox + 2.5f, 10.2f, 142f));
            SpawnGoalPortal(12, new Vector3(ox, 10.2f, 154f));
        }

        // STAGE 13: GRAPPLE SKYBRIDGE (5 SEQUENTIAL AIR WIRE SWINGS)
        private static void BuildStage13(float ox)
        {
            CreateBlock("S13_Start", new Vector3(ox, 2.0f, 5f), new Vector3(11f, 2f, 16f), _gridCyan);
            SpawnJumpPad(new Vector3(ox, 3.05f, 11f), 17.5f);

            for (int i = 0; i < 5; i++)
            {
                float gz = 28f + i * 22f;
                float gx = ox + (i % 2 == 0 ? -3.0f : 3.0f);
                SpawnGrapple(new Vector3(gx, 14.5f + (i % 2) * 1.5f, gz));
                SpawnSquadEnemy(13, "D", i + 1, EnemyKind.Drone, new Vector3(ox, 11.5f, gz + 8f));
                if (i == 1 || i == 3)
                {
                    CreateBlock($"S13_Pad_{i}", new Vector3(ox, 6.5f, gz + 6f), new Vector3(9f, 1.8f, 10f), _gridMagenta);
                    SpawnSquadEnemy(13, "T", i + 1, EnemyKind.Trooper, new Vector3(ox, 7.6f, gz + 6f));
                }
            }

            CreateBlock("S13_Goal", new Vector3(ox, 10.0f, 144f), new Vector3(16f, 2.4f, 22f), _gridGold);
            SpawnSquadEnemy(13, "T", 9, EnemyKind.Trooper, new Vector3(ox, 11.2f, 139f));
            SpawnGoalPortal(13, new Vector3(ox, 11.2f, 150f));
        }

        // STAGE 14: TWIN GATE DUAL ROUTE (LEFT KATANA MELEE STREET vs RIGHT SKY GRAPPLE)
        private static void BuildStage14(float ox)
        {
            CreateBlock("S14_Start", new Vector3(ox, 0f, 6f), new Vector3(16f, 2f, 18f), _gridCyan);

            // Left Route: Katana Melee Street
            CreateBlock("S14_LeftDeck", new Vector3(ox - 10f, 2.0f, 48f), new Vector3(9f, 2f, 64f), _gridMagenta);
            CreateBlock("S14_LeftWall", new Vector3(ox - 15f, 6.0f, 52f), new Vector3(1.2f, 8f, 44f), _gridCyan, isWallRun: true);
            for (int i = 0; i < 5; i++)
                SpawnSquadEnemy(14, "LT", i + 1, EnemyKind.Trooper, new Vector3(ox - 10f + (i % 2 == 0 ? -1.8f : 1.8f), 3.0f, 26f + i * 11f));
            SpawnJumpPad(new Vector3(ox - 10f, 3.05f, 76f), 17.0f);

            // Right Route: Sky Grapple & Aerial Blaster
            SpawnJumpPad(new Vector3(ox + 5.5f, 1.05f, 12f), 18.0f);
            SpawnGrapple(new Vector3(ox + 10f, 13.5f, 34f));
            SpawnSquadEnemy(14, "RD", 1, EnemyKind.Drone, new Vector3(ox + 10f, 11.2f, 45f));
            CreateBlock("S14_RightIsland", new Vector3(ox + 10f, 6.5f, 58f), new Vector3(9f, 1.8f, 16f), _gridCyan);
            SpawnSquadEnemy(14, "RD", 2, EnemyKind.Drone, new Vector3(ox + 10f, 9.5f, 60f));
            SpawnGrapple(new Vector3(ox + 10f, 14.0f, 78f));

            // Convergence Arena + Final Stretch
            CreateBlock("S14_Converge", new Vector3(ox, 6.0f, 102f), new Vector3(24f, 2.2f, 26f), _gridCyan);
            for (int i = 0; i < 4; i++)
                SpawnSquadEnemy(14, "CT", i + 1, EnemyKind.Trooper, new Vector3(ox + (i - 1.5f) * 4f, 7.2f, 102f));
            SpawnSquadEnemy(14, "CD", 1, EnemyKind.Drone, new Vector3(ox - 5f, 10.2f, 106f));
            SpawnSquadEnemy(14, "CD", 2, EnemyKind.Drone, new Vector3(ox + 5f, 10.2f, 106f));

            SpawnGrapple(new Vector3(ox, 14.0f, 124f));
            CreateBlock("S14_Goal", new Vector3(ox, 9.0f, 144f), new Vector3(16f, 2.4f, 22f), _gridGold);
            SpawnGoalPortal(14, new Vector3(ox, 10.2f, 150f));
        }

        // STAGE 15: OMEGA CITADEL (FINAL 4-PYLON FORTRESS & OMEGA ARCHON CORE)
        private static void BuildStage15(float ox)
        {
            CreateBlock("S15_Beachhead", new Vector3(ox, 0f, 8f), new Vector3(14f, 2f, 22f), _gridCyan);
            SpawnSquadEnemy(15, "T", 1, EnemyKind.Trooper, new Vector3(ox - 3.5f, 1.0f, 10f));
            SpawnSquadEnemy(15, "T", 2, EnemyKind.Trooper, new Vector3(ox + 3.5f, 1.0f, 10f));
            SpawnSquadEnemy(15, "D", 1, EnemyKind.Drone, new Vector3(ox, 3.6f, 14f));
            SpawnJumpPad(new Vector3(ox, 1.05f, 16f), 19.5f);

            // Outer Battlement
            CreateBlock("S15_OuterDeck", new Vector3(ox, 6.5f, 40f), new Vector3(16f, 2f, 26f), _gridCyan);
            for (int i = 0; i < 4; i++)
                SpawnSquadEnemy(15, "OT", i + 1, EnemyKind.Trooper, new Vector3(ox + (i % 2 == 0 ? -4f : 4f), 7.5f, 32f + i * 5f));
            SpawnSquadEnemy(15, "OD", 1, EnemyKind.Drone, new Vector3(ox, 10.4f, 41f));

            // Dual Wall Approach & Grapple Leap
            CreateBlock("S15_WallL", new Vector3(ox - 6.2f, 12.0f, 66f), new Vector3(1.2f, 9.5f, 24f), _gridMagenta, isWallRun: true);
            CreateBlock("S15_WallR", new Vector3(ox + 6.2f, 12.0f, 66f), new Vector3(1.2f, 9.5f, 24f), _gridMagenta, isWallRun: true);
            SpawnGrapple(new Vector3(ox, 17.5f, 85f));

            // Grand Omega Citadel Arena + 4 Shield Pylons + Omega CoreBoss
            CreateBlock("S15_OmegaArena", new Vector3(ox, 10.0f, 118f), new Vector3(42f, 2.8f, 42f), _gridMagenta);
            SpawnSquadEnemy(15, "OmegaArchonBoss", 1, EnemyKind.CoreBoss, new Vector3(ox, 11.8f, 122f), hp: 12);

            Vector3[] p15 =
            {
                new Vector3(ox - 17f, 13.5f, 106f),
                new Vector3(ox + 17f, 13.5f, 106f),
                new Vector3(ox - 17f, 14.5f, 132f),
                new Vector3(ox + 17f, 14.5f, 132f),
            };
            for (int i = 0; i < p15.Length; i++)
            {
                CreateBlock($"S15_PylonPlat_{i}", p15[i], new Vector3(7f, 1.6f, 7f), _gridGold);
                SpawnSquadEnemy(15, "Pylon", i + 1, EnemyKind.ShieldPylon, p15[i] + new Vector3(0f, 2.2f, 0f));
                SpawnGrapple(p15[i] + new Vector3(0f, 5.4f, -3f));
            }
            SpawnSquadEnemy(15, "CD", 1, EnemyKind.Drone, new Vector3(ox - 9f, 14.2f, 114f));
            SpawnSquadEnemy(15, "CD", 2, EnemyKind.Drone, new Vector3(ox + 9f, 14.2f, 114f));
            SpawnSquadEnemy(15, "CT", 1, EnemyKind.Trooper, new Vector3(ox - 7f, 11.5f, 104f));
            SpawnSquadEnemy(15, "CT", 2, EnemyKind.Trooper, new Vector3(ox + 7f, 11.5f, 104f));
        }

        // =====================================================================
        // STAGE 4: CYBER COMBAT ARENA (VS CPU, LOCAL 2P SPLIT-SCREEN & LAN PVP)
        // =====================================================================
        private static void BuildBattleArena(float ox)
        {
            // Main 54m x 54m Arena Deck
            CreateBlock("Arena_MainFloor", new Vector3(ox, 0f, 0f), new Vector3(54f, 2f, 54f), _gridCyan);

            // Central Raised Dais & Ramps
            CreateBlock("Arena_CenterDais", new Vector3(ox, 1.8f, 0f), new Vector3(12f, 1.8f, 12f), _gridGold);
            CreateBlock("Arena_RampN", new Vector3(ox, 1.2f, 8.5f), new Vector3(5f, 1.0f, 6f), _gridGold, rotEuler: new Vector3(16f, 0f, 0f));
            CreateBlock("Arena_RampS", new Vector3(ox, 1.2f, -8.5f), new Vector3(5f, 1.0f, 6f), _gridGold, rotEuler: new Vector3(-16f, 0f, 0f));

            // 4 Side Catwalks + Wall-Run Billboards
            CreateBlock("Arena_WallW", new Vector3(ox - 26f, 5.5f, 0f), new Vector3(1.5f, 9f, 44f), _gridMagenta, isWallRun: true);
            CreateBlock("Arena_WallE", new Vector3(ox + 26f, 5.5f, 0f), new Vector3(1.5f, 9f, 44f), _gridMagenta, isWallRun: true);
            CreateBlock("Arena_CatwalkW", new Vector3(ox - 20f, 4.5f, 0f), new Vector3(5f, 1.2f, 28f), _gridCyan);
            CreateBlock("Arena_CatwalkE", new Vector3(ox + 20f, 4.5f, 0f), new Vector3(5f, 1.2f, 28f), _gridCyan);

            // Cover Pillars
            Vector3[] pillars =
            {
                new Vector3(ox - 12f, 3.2f, -12f),
                new Vector3(ox + 12f, 3.2f, -12f),
                new Vector3(ox - 12f, 3.2f, 12f),
                new Vector3(ox + 12f, 3.2f, 12f),
            };
            for (int i = 0; i < pillars.Length; i++)
                CreateBlock($"Arena_CoverPillar_{i}", pillars[i], new Vector3(3.2f, 4.6f, 3.2f), _gridMagenta);

            // Corner Jump Pads + Overhead Grapple Anchors
            SpawnJumpPad(new Vector3(ox - 20f, 1.05f, -20f), 19.5f);
            SpawnJumpPad(new Vector3(ox + 20f, 1.05f, -20f), 19.5f);
            SpawnJumpPad(new Vector3(ox - 20f, 1.05f, 20f), 19.5f);
            SpawnJumpPad(new Vector3(ox + 20f, 1.05f, 20f), 19.5f);
            SpawnGrapple(new Vector3(ox, 12.5f, 0f));
            SpawnGrapple(new Vector3(ox - 11f, 10.5f, 0f));
            SpawnGrapple(new Vector3(ox + 11f, 10.5f, 0f));

            // Spawn 2 AI Cyber Ninja Bots for VS CPU Arena
            SpawnArenaBot("Arena_Bot_Alpha", 0, "CYBER NINJA ALPHA", new Vector3(ox - 8f, 2.2f, 14f), new Color(1.0f, 0.22f, 0.72f));
            SpawnArenaBot("Arena_Bot_Viper", 1, "CYBER NINJA VIPER", new Vector3(ox + 8f, 2.2f, 14f), new Color(1.0f, 0.58f, 0.14f));

            // Spawn Remote LAN PvP Opponent Avatar
            var botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cyber_NinjaBot.fbx");
            var remoteGo = new GameObject("Arena_RemoteOpponent");
            remoteGo.transform.position = new Vector3(ox, 2.0f, 20f);
            remoteGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var rc = remoteGo.AddComponent<CapsuleCollider>();
            rc.height = 1.85f;
            rc.center = new Vector3(0f, 0.92f, 0f);
            rc.radius = 0.48f;
            if (botPrefab != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(botPrefab, remoteGo.transform);
                vis.name = "RemoteBotMesh";
                ApplyHardSurfaceMaterials(vis, outline: 0.0024f);
                foreach (var c in vis.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            remoteGo.AddComponent<RemotePvpAvatar>();
            remoteGo.SetActive(false);
        }

        private static void SpawnArenaBot(string name, int botIdx, string displayName, Vector3 pos, Color accent)
        {
            var botGo = new GameObject(name);
            botGo.transform.position = pos;
            botGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cc = botGo.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.radius = 0.42f;

            var botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cyber_NinjaBot.fbx");
            if (botPrefab != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(botPrefab, botGo.transform);
                vis.name = "NinjaBotMesh";
                ApplyHardSurfaceMaterials(vis, outline: 0.0024f);
                foreach (var c in vis.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }

            var ai = botGo.AddComponent<CyberBotAI>();
            ai.botIndex = botIdx;
            ai.botName = displayName;
            ai.accentColor = accent;
            botGo.SetActive(false);
        }

        private static void BuildNeonSkylineBackdrop()
        {
            Random.InitState(2077);
            for (int slot = 0; slot <= 15; slot++)
            {
                float sx = slot * 120f;
                bool isArena = Mathf.Approximately(sx, 360f);
                for (int i = 0; i < 16; i++)
                {
                    float side = (i % 2 == 0) ? -1f : 1f;
                    float tx = sx + side * Random.Range(32f, 68f);
                    float tz = isArena ? Random.Range(-45f, 45f) : Random.Range(-20f, 195f);
                    float h = Random.Range(48f, 120f);
                    float w = Random.Range(9f, 20f);
                    CreateBlock($"CityTower_{sx}_{i}", new Vector3(tx, h * 0.35f - 28f, tz), new Vector3(w, h, w), _cityTowerMat);
                }
            }
        }

        private static void ApplyHardSurfaceMaterials(GameObject root, float outline)
        {
            Shader s = Shader.Find("Custom/CyberHardSurface") ?? Shader.Find("Standard");
            var cache = new Dictionary<Material, Material>();
            foreach (var mr in root.GetComponentsInChildren<Renderer>())
            {
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var orig = mats[i];
                    if (orig == null) continue;
                    if (!cache.TryGetValue(orig, out var m))
                    {
                        m = new Material(s) { name = orig.name + "_Cyber" };
                        Color baseCol = orig.HasProperty("_Color") ? orig.GetColor("_Color") : Color.white;
                        m.SetColor("_Color", baseCol);
                        if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);

                        if (orig.name.Contains("Glow_Cyan"))
                            m.SetColor("_EmissionColor", new Color(0.12f, 0.95f, 1.0f) * 3.4f);
                        else if (orig.name.Contains("Glow_Magenta"))
                            m.SetColor("_EmissionColor", new Color(1.0f, 0.16f, 0.72f) * 3.4f);
                        else if (orig.name.Contains("Glow_Crimson"))
                            m.SetColor("_EmissionColor", new Color(1.0f, 0.14f, 0.20f) * 3.6f);
                        else if (orig.name.Contains("Glow_Gold"))
                            m.SetColor("_EmissionColor", new Color(1.0f, 0.78f, 0.20f) * 3.2f);

                        cache[orig] = m;
                    }
                    mats[i] = m;
                }
                mr.sharedMaterials = mats;
            }
        }
    }
}
