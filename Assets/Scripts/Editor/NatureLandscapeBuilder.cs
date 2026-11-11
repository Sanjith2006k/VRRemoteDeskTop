using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRRemoteDesktop.Editor
{
    /// <summary>
    /// Constructs a fully modelled 3D natural landscape outside the observation window:
    /// 1. 3D Mountain Peaks: Towering faceted alpine peaks with jagged rock crags and snowcaps.
    /// 2. 3D Winding River: Curved water surface with specular sheen, riverbed pebbles, and boulders.
    /// 3. 3D Pine & Evergreen Forest: 50+ detailed 3D trees (trunk + 4 tiered foliage cones) across foreground, riverbanks, and hillsides.
    /// 4. 3D Alpine Cottages: Cozy rustic cabins with timber logs, pitched roofs, stone chimneys, and warm glowing windows.
    /// 5. Rolling Valley Terrain: Lush green alpine grass extending from under the window sill to the foothills.
    /// 6. Warm Natural Sunlight: Casting soft shadows across trees and peaks into the room.
    /// </summary>
    public static class NatureLandscapeBuilder
    {
        // Reusable procedural meshes
        private static Mesh _coneMesh12;
        private static Mesh _coneMesh8;
        private static Mesh _mountainPeakMesh;
        private static Mesh _snowCapMesh;
        private static Mesh _prismRoofMesh;

        public static void Build3DLandscape(Transform parent,
                                            Material matRock, Material matSnow, Material matGrass,
                                            Material matBark, Material matFoliageDark, Material matFoliageLight,
                                            Material matWater, Material matStone,
                                            Material matCabinWood, Material matCabinRoof, Material matWindowGlow,
                                            Material matCosmicVista)
        {
            EnsureProceduralMeshes();

            GameObject landscapeRoot = new GameObject("Nature3DLandscape");
            landscapeRoot.transform.SetParent(parent, false);

            // 1. Rolling Valley Floor & Foothills
            BuildValleyTerrain(landscapeRoot.transform, matGrass);

            // 2. Winding River & Riverbanks
            BuildWindingRiver(landscapeRoot.transform, matWater, matStone);

            // 3. Majestic 3D Mountain Range
            BuildMountainRange(landscapeRoot.transform, matRock, matSnow);

            // 4. Lush 3D Pine Forest
            BuildForest(landscapeRoot.transform, matBark, matFoliageDark, matFoliageLight);

            // 5. Cozy 3D Alpine Cottages (Nestled in between the trees)
            BuildCottages(landscapeRoot.transform, matCabinWood, matCabinRoof, matWindowGlow, matStone);

            // 6. Natural Sunlight & Distant Horizon Atmosphere
            BuildSunlightAndAtmosphere(landscapeRoot.transform, matCosmicVista);

            // 7. Interactive Weather Particle Systems (Rain, Snow, Autumn Leaves)
            BuildWeatherParticles(landscapeRoot.transform);
        }

        private static void EnsureProceduralMeshes()
        {
            if (_coneMesh12 == null) _coneMesh12 = CreateConeMesh(1.0f, 1.0f, 12, "Proc_Cone12");
            if (_coneMesh8 == null) _coneMesh8 = CreateConeMesh(1.0f, 1.0f, 8, "Proc_Cone8");
            if (_mountainPeakMesh == null) _mountainPeakMesh = CreateMountainMesh(1.0f, 1.0f, 10, "Proc_MountainPeak");
            if (_snowCapMesh == null) _snowCapMesh = CreateSnowCapMesh(0.45f, 0.40f, 10, "Proc_SnowCap");
            if (_prismRoofMesh == null) _prismRoofMesh = CreatePrismMesh(1.0f, 1.0f, 1.0f, "Proc_PrismRoof");
        }

        #region 1. Valley Terrain
        private static void BuildValleyTerrain(Transform parent, Material matGrass)
        {
            GameObject terrainRoot = new GameObject("ValleyTerrain");
            terrainRoot.transform.SetParent(parent, false);

            // Main broad valley floor
            GameObject mainValley = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainValley.name = "ValleyFloor_Main";
            mainValley.transform.SetParent(terrainRoot.transform, false);
            mainValley.transform.position = new Vector3(38.0f, -0.45f, -0.5f);
            mainValley.transform.localScale = new Vector3(68.0f, 0.8f, 75.0f);
            if (matGrass != null) mainValley.GetComponent<Renderer>().sharedMaterial = matGrass;
            Object.DestroyImmediate(mainValley.GetComponent<Collider>());

            // Foreground rolling slope connecting from beneath window sill (x = 5.8m to 12m)
            GameObject foreSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreSlope.name = "ValleyFloor_ForeSlope";
            foreSlope.transform.SetParent(terrainRoot.transform, false);
            foreSlope.transform.position = new Vector3(8.5f, -0.25f, -0.5f);
            foreSlope.transform.rotation = Quaternion.Euler(0f, 0f, -3.5f);
            foreSlope.transform.localScale = new Vector3(6.5f, 0.7f, 18.0f);
            if (matGrass != null) foreSlope.GetComponent<Renderer>().sharedMaterial = matGrass;
            Object.DestroyImmediate(foreSlope.GetComponent<Collider>());

            // Rolling Hill Mounds (Left & Right framing foothills)
            CreateHillMound(terrainRoot.transform, "Hill_North_1", new Vector3(22.0f, 0.8f, 16.0f), new Vector3(18.0f, 3.2f, 14.0f), matGrass);
            CreateHillMound(terrainRoot.transform, "Hill_North_2", new Vector3(34.0f, 1.5f, 24.0f), new Vector3(22.0f, 4.8f, 18.0f), matGrass);
            CreateHillMound(terrainRoot.transform, "Hill_South_1", new Vector3(20.0f, 0.6f, -15.0f), new Vector3(16.0f, 2.8f, 13.0f), matGrass);
            CreateHillMound(terrainRoot.transform, "Hill_South_2", new Vector3(32.0f, 1.4f, -22.0f), new Vector3(20.0f, 4.4f, 16.0f), matGrass);
            CreateHillMound(terrainRoot.transform, "Hill_Mid_Gentle", new Vector3(26.0f, 0.4f, 4.0f), new Vector3(12.0f, 1.8f, 10.0f), matGrass);
        }

        private static void CreateHillMound(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = name;
            hill.transform.SetParent(parent, false);
            hill.transform.position = pos;
            hill.transform.localScale = scale;
            if (mat != null) hill.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(hill.GetComponent<Collider>());
        }
        #endregion

        #region 2. Winding River & Riverbanks
        private static void BuildWindingRiver(Transform parent, Material matWater, Material matStone)
        {
            GameObject riverRoot = new GameObject("WindingRiver");
            riverRoot.transform.SetParent(parent, false);

            // River water segments flowing from distant mountains towards the foreground
            CreateRiverSegment(riverRoot.transform, "River_Seg_Far", new Vector3(42.0f, -0.12f, -14.0f), 32f, new Vector3(16.0f, 0.10f, 7.5f), matWater);
            CreateRiverSegment(riverRoot.transform, "River_Seg_MidFar", new Vector3(28.0f, -0.12f, -5.5f), -18f, new Vector3(15.0f, 0.10f, 6.8f), matWater);
            CreateRiverSegment(riverRoot.transform, "River_Seg_Bend", new Vector3(17.5f, -0.12f, 1.5f), 24f, new Vector3(14.0f, 0.10f, 6.2f), matWater);
            CreateRiverSegment(riverRoot.transform, "River_Seg_Fore", new Vector3(10.5f, -0.12f, -2.0f), -26f, new Vector3(12.0f, 0.10f, 5.5f), matWater);

            // Riverbed boulders & stones scattered along the water edge
            Vector3[] stonePositions = new Vector3[]
            {
                new Vector3(8.8f, 0.05f, 0.6f),
                new Vector3(9.4f, 0.08f, -4.5f),
                new Vector3(11.2f, 0.12f, 1.8f),
                new Vector3(11.2f, 0.08f, -3.2f),
                new Vector3(15.0f, 0.15f, 5.2f),
                new Vector3(16.8f, 0.20f, -2.4f),
                new Vector3(19.2f, 0.18f, 4.8f),
                new Vector3(21.5f, 0.22f, -7.5f),
                new Vector3(24.0f, 0.15f, -2.2f),
                new Vector3(26.5f, 0.25f, -8.8f),
                new Vector3(32.0f, 0.28f, -9.2f),
                new Vector3(36.0f, 0.35f, -18.0f)
            };

            for (int i = 0; i < stonePositions.Length; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"RiverStone_{i + 1}";
                stone.transform.SetParent(riverRoot.transform, false);
                stone.transform.position = stonePositions[i];
                float s = 0.55f + (i % 4) * 0.25f;
                stone.transform.localScale = new Vector3(s * 1.3f, s * 0.65f, s * 1.1f);
                stone.transform.rotation = Quaternion.Euler((i * 17) % 360, (i * 43) % 360, 0f);
                if (matStone != null) stone.GetComponent<Renderer>().sharedMaterial = matStone;
                Object.DestroyImmediate(stone.GetComponent<Collider>());
            }
        }

        private static void CreateRiverSegment(Transform parent, string name, Vector3 pos, float yRot, Vector3 scale, Material mat)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = name;
            seg.transform.SetParent(parent, false);
            seg.transform.position = pos;
            seg.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            seg.transform.localScale = scale;
            if (mat != null) seg.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(seg.GetComponent<Collider>());
        }
        #endregion

        #region 3. Majestic 3D Mountain Range
        private static void BuildMountainRange(Transform parent, Material matRock, Material matSnow)
        {
            GameObject mountainsRoot = new GameObject("AlpineMountainRange");
            mountainsRoot.transform.SetParent(parent, false);

            // Mountain definitions (pos, radius, height, yRot)
            var peaks = new[]
            {
                // Grand Central Towering Peak (Centerpiece of the window view)
                new { name = "Peak_Grand_Center", pos = new Vector3(56.0f, 0.0f, -2.0f), r = 24.0f, h = 32.0f, rot = 18f },
                // High Left Flank Peak
                new { name = "Peak_High_Left", pos = new Vector3(46.0f, 0.0f, 15.0f), r = 18.0f, h = 25.0f, rot = -25f },
                // High Right Flank Peak
                new { name = "Peak_High_Right", pos = new Vector3(48.0f, 0.0f, -22.0f), r = 19.0f, h = 26.0f, rot = 42f },
                // Colossus Horizon Peak (Massive backdrop peak)
                new { name = "Peak_Colossus_Back", pos = new Vector3(76.0f, 2.0f, 8.0f), r = 32.0f, h = 42.0f, rot = -10f },
                // Northern Horizon Ridge Peak
                new { name = "Peak_North_Horizon", pos = new Vector3(68.0f, 2.0f, 34.0f), r = 26.0f, h = 34.0f, rot = 30f },
                // Southern Horizon Ridge Peak
                new { name = "Peak_South_Horizon", pos = new Vector3(68.0f, 2.0f, -38.0f), r = 27.0f, h = 33.0f, rot = -35f },
                // Intermediate Rugged Crag (Left)
                new { name = "Peak_Crag_Left", pos = new Vector3(38.0f, 0.0f, 28.0f), r = 14.0f, h = 18.0f, rot = 55f },
                // Intermediate Rugged Crag (Right)
                new { name = "Peak_Crag_Right", pos = new Vector3(40.0f, 0.0f, -32.0f), r = 15.0f, h = 19.0f, rot = -48f }
            };

            foreach (var p in peaks)
            {
                CreateMountainPeak(mountainsRoot.transform, p.name, p.pos, p.r, p.h, p.rot, matRock, matSnow);
            }
        }

        private static void CreateMountainPeak(Transform parent, string name, Vector3 pos, float radius, float height, float yRot,
                                               Material matRock, Material matSnow)
        {
            GameObject mtn = new GameObject(name);
            mtn.transform.SetParent(parent, false);
            mtn.transform.position = pos;
            mtn.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Rocky Mountain Body
            GameObject body = new GameObject("RockBody");
            body.transform.SetParent(mtn.transform, false);
            body.transform.localScale = new Vector3(radius, height, radius);
            var mf = body.AddComponent<MeshFilter>();
            mf.sharedMesh = _mountainPeakMesh;
            var mr = body.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matRock;

            // 2. Alpine Snow Cap on top 38% of peak
            GameObject snow = new GameObject("SnowCap");
            snow.transform.SetParent(mtn.transform, false);
            snow.transform.localPosition = new Vector3(0f, height * 0.62f, 0f);
            snow.transform.localScale = new Vector3(radius * 0.44f, height * 0.39f, radius * 0.44f);
            var mfSnow = snow.AddComponent<MeshFilter>();
            mfSnow.sharedMesh = _snowCapMesh;
            var mrSnow = snow.AddComponent<MeshRenderer>();
            mrSnow.sharedMaterial = matSnow;

            // 3. Secondary Rugged Shoulder Crag for natural asymmetry
            GameObject shoulder = new GameObject("ShoulderCrag");
            shoulder.transform.SetParent(mtn.transform, false);
            shoulder.transform.localPosition = new Vector3(radius * 0.35f, 0f, radius * 0.20f);
            shoulder.transform.localScale = new Vector3(radius * 0.60f, height * 0.65f, radius * 0.60f);
            var mfS = shoulder.AddComponent<MeshFilter>();
            mfS.sharedMesh = _coneMesh8;
            var mrS = shoulder.AddComponent<MeshRenderer>();
            mrS.sharedMaterial = matRock;
        }
        #endregion

        #region 4. Detailed 3D Pine & Evergreen Forest
        private static void BuildForest(Transform parent, Material matBark, Material matFoliageDark, Material matFoliageLight)
        {
            GameObject forestRoot = new GameObject("PineForest");
            forestRoot.transform.SetParent(parent, false);

            // Handcrafted natural coordinates for rich stereoscopic depth through the window:
            // Group A: Foreground Window Edge Trees (Immediate depth & stereoscopic framing at x = 7.5m to 11.5m)
            var foreTrees = new[]
            {
                new { pos = new Vector3(7.6f, 0.1f, 3.4f), scale = 1.35f, rot = 15f, vibrant = true },
                new { pos = new Vector3(8.4f, 0.0f, 4.6f), scale = 1.65f, rot = -40f, vibrant = false },
                new { pos = new Vector3(9.2f, 0.0f, 3.0f), scale = 1.20f, rot = 75f, vibrant = true },
                new { pos = new Vector3(7.8f, 0.1f, -4.2f), scale = 1.45f, rot = -25f, vibrant = false },
                new { pos = new Vector3(8.6f, 0.0f, -5.4f), scale = 1.70f, rot = 50f, vibrant = true },
                new { pos = new Vector3(9.4f, 0.0f, -3.8f), scale = 1.25f, rot = -80f, vibrant = false },
                new { pos = new Vector3(10.5f, 0.0f, 2.2f), scale = 1.15f, rot = 30f, vibrant = true },
                new { pos = new Vector3(10.8f, 0.0f, -2.4f), scale = 1.20f, rot = -15f, vibrant = false }
            };

            foreach (var t in foreTrees)
            {
                CreatePineTree(forestRoot.transform, t.pos, t.scale, t.rot, t.vibrant ? matFoliageLight : matFoliageDark, matBark);
            }

            // Group B: Riverside Grove & Valley Meadows (x = 12m to 25m)
            var midTrees = new[]
            {
                // Left bank grove
                new { pos = new Vector3(13.5f, 0.0f, 6.2f), scale = 1.8f, rot = 12f, vibrant = false },
                new { pos = new Vector3(15.2f, 0.0f, 8.4f), scale = 2.1f, rot = 64f, vibrant = true },
                new { pos = new Vector3(16.8f, 0.0f, 5.0f), scale = 1.6f, rot = -32f, vibrant = false },
                new { pos = new Vector3(19.0f, 0.0f, 9.8f), scale = 2.3f, rot = 88f, vibrant = true },
                new { pos = new Vector3(21.4f, 0.2f, 7.5f), scale = 1.9f, rot = -45f, vibrant = false },
                new { pos = new Vector3(23.2f, 0.3f, 11.2f), scale = 2.4f, rot = 20f, vibrant = true },

                // Right bank grove
                new { pos = new Vector3(13.0f, 0.0f, -6.8f), scale = 1.7f, rot = -55f, vibrant = true },
                new { pos = new Vector3(14.8f, 0.0f, -8.5f), scale = 2.0f, rot = 35f, vibrant = false },
                new { pos = new Vector3(16.5f, 0.0f, -5.2f), scale = 1.5f, rot = -18f, vibrant = true },
                new { pos = new Vector3(18.8f, 0.1f, -10.5f), scale = 2.2f, rot = 70f, vibrant = false },
                new { pos = new Vector3(21.0f, 0.2f, -8.0f), scale = 1.8f, rot = -42f, vibrant = true },
                new { pos = new Vector3(23.5f, 0.3f, -12.5f), scale = 2.3f, rot = 15f, vibrant = false },

                // Center meadow clusters
                new { pos = new Vector3(15.8f, 0.0f, -1.2f), scale = 1.4f, rot = 45f, vibrant = true },
                new { pos = new Vector3(18.2f, 0.0f, -3.2f), scale = 1.7f, rot = -70f, vibrant = false },
                new { pos = new Vector3(22.0f, 0.1f, 1.8f), scale = 1.9f, rot = 25f, vibrant = true },
                new { pos = new Vector3(25.5f, 0.2f, -2.5f), scale = 2.2f, rot = -10f, vibrant = false }
            };

            foreach (var t in midTrees)
            {
                CreatePineTree(forestRoot.transform, t.pos, t.scale, t.rot, t.vibrant ? matFoliageLight : matFoliageDark, matBark);
            }

            // Group C: Foothill & Mountain Slope Forest (x = 26m to 48m, tall dense alpine pines)
            var hillTrees = new[]
            {
                new { pos = new Vector3(28.0f, 0.5f, 15.0f), scale = 2.6f, rot = 40f, vibrant = false },
                new { pos = new Vector3(30.5f, 0.8f, 18.5f), scale = 2.9f, rot = -15f, vibrant = true },
                new { pos = new Vector3(33.0f, 1.2f, 14.2f), scale = 2.5f, rot = 80f, vibrant = false },
                new { pos = new Vector3(36.0f, 1.6f, 21.0f), scale = 3.2f, rot = -60f, vibrant = true },
                new { pos = new Vector3(39.5f, 2.0f, 17.5f), scale = 2.8f, rot = 25f, vibrant = false },
                new { pos = new Vector3(42.0f, 2.5f, 23.0f), scale = 3.4f, rot = -35f, vibrant = true },

                new { pos = new Vector3(27.5f, 0.5f, -16.0f), scale = 2.5f, rot = -20f, vibrant = true },
                new { pos = new Vector3(30.0f, 0.8f, -19.5f), scale = 2.8f, rot = 65f, vibrant = false },
                new { pos = new Vector3(32.5f, 1.1f, -15.5f), scale = 2.4f, rot = -75f, vibrant = true },
                new { pos = new Vector3(35.5f, 1.5f, -22.5f), scale = 3.1f, rot = 30f, vibrant = false },
                new { pos = new Vector3(38.5f, 1.9f, -18.0f), scale = 2.7f, rot = -40f, vibrant = true },
                new { pos = new Vector3(41.5f, 2.4f, -25.0f), scale = 3.3f, rot = 55f, vibrant = false },

                // Deep central valley tree stands
                new { pos = new Vector3(32.0f, 0.3f, 5.5f), scale = 2.7f, rot = 15f, vibrant = false },
                new { pos = new Vector3(35.0f, 0.4f, -6.5f), scale = 2.8f, rot = -45f, vibrant = true },
                new { pos = new Vector3(38.0f, 0.6f, 2.0f), scale = 3.0f, rot = 70f, vibrant = false },
                new { pos = new Vector3(42.0f, 0.9f, -4.0f), scale = 3.2f, rot = -20f, vibrant = true }
            };

            foreach (var t in hillTrees)
            {
                CreatePineTree(forestRoot.transform, t.pos, t.scale, t.rot, t.vibrant ? matFoliageLight : matFoliageDark, matBark);
            }
        }

        private static void CreatePineTree(Transform parent, Vector3 pos, float scale, float yRot, Material matFoliage, Material matBark)
        {
            GameObject tree = new GameObject("PineTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Trunk (Tapered wood cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.1f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.24f * scale, 1.1f * scale, 0.24f * scale);
            if (matBark != null) trunk.GetComponent<Renderer>().sharedMaterial = matBark;
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            // 2. 4-Tiered Conical Foliage Layers (Stacked and rotated for lush pine branch silhouette)
            var tiers = new[]
            {
                new { y = 1.0f * scale, r = 1.35f * scale, h = 1.6f * scale, rot = 0f },
                new { y = 1.9f * scale, r = 1.10f * scale, h = 1.4f * scale, rot = 32f },
                new { y = 2.7f * scale, r = 0.85f * scale, h = 1.2f * scale, rot = -28f },
                new { y = 3.4f * scale, r = 0.55f * scale, h = 1.1f * scale, rot = 15f }
            };

            for (int i = 0; i < tiers.Length; i++)
            {
                var t = tiers[i];
                GameObject tier = new GameObject($"Foliage_Tier_{i + 1}");
                tier.transform.SetParent(tree.transform, false);
                tier.transform.localPosition = new Vector3(0f, t.y, 0f);
                tier.transform.localRotation = Quaternion.Euler(0f, t.rot, 0f);
                tier.transform.localScale = new Vector3(t.r, t.h, t.r);

                var mf = tier.AddComponent<MeshFilter>();
                mf.sharedMesh = _coneMesh12;
                var mr = tier.AddComponent<MeshRenderer>();
                mr.sharedMaterial = matFoliage;
            }
        }
        #endregion

        #region 5. Cozy 3D Alpine Cottages
        private static void BuildCottages(Transform parent, Material matWood, Material matRoof, Material matWindowGlow, Material matStone)
        {
            GameObject cottagesRoot = new GameObject("AlpineCottages");
            cottagesRoot.transform.SetParent(parent, false);

            // Cottage 1: Village Center Cabin (Directly visible in center view outside the observation window at x = 13.2m, z = -0.3m)
            CreateCabin(cottagesRoot.transform, "Cottage_VillageCenter", new Vector3(13.2f, 0.05f, -0.3f), -12f,
                        new Vector3(3.8f, 2.3f, 3.0f), matWood, matRoof, matWindowGlow, matStone, true);

            // Cottage 2: Riverside Homestead (Framed clearly in mid-left view at x = 19.5f, z = 6.8m)
            CreateCabin(cottagesRoot.transform, "Cottage_Riverside", new Vector3(19.5f, 0.35f, 6.8f), -22f,
                        new Vector3(4.2f, 2.4f, 3.2f), matWood, matRoof, matWindowGlow, matStone, true);

            // Cottage 3: River Bend Glade Cabin (Nestled between riverbank trees at x = 17.5m, z = 2.0m)
            CreateCabin(cottagesRoot.transform, "Cottage_GladeCabin", new Vector3(17.5f, 0.20f, 2.0f), 24f,
                        new Vector3(3.5f, 2.2f, 2.8f), matWood, matRoof, matWindowGlow, matStone, false);

            // Cottage 4: Distant Valley Farmhouse (Nestled in right valley at x = 25.0m, z = -6.5m)
            CreateCabin(cottagesRoot.transform, "Cottage_ValleyFarm", new Vector3(25.0f, 0.40f, -6.5f), 35f,
                        new Vector3(3.8f, 2.3f, 3.0f), matWood, matRoof, matWindowGlow, matStone, false);
        }

        private static void CreateCabin(Transform parent, string name, Vector3 pos, float yRot, Vector3 size,
                                        Material matWood, Material matRoof, Material matWindowGlow, Material matStone, bool hasPorch)
        {
            GameObject cabin = new GameObject(name);
            cabin.transform.SetParent(parent, false);
            cabin.transform.position = pos;
            cabin.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Sturdy Stone Foundation Plinth (Elevates log walls and grounds house firmly on terrain)
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name = "Foundation_Stone";
            foundation.transform.SetParent(cabin.transform, false);
            foundation.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            foundation.transform.localScale = new Vector3(size.x * 1.05f, 0.32f, size.z * 1.05f);
            if (matStone != null) foundation.GetComponent<Renderer>().sharedMaterial = matStone;
            Object.DestroyImmediate(foundation.GetComponent<Collider>());

            // 2. Cabin Log Body (Walls)
            float wallH = size.y - 0.28f;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CabinWalls";
            body.transform.SetParent(cabin.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.28f + (wallH * 0.5f), 0f);
            body.transform.localScale = new Vector3(size.x, wallH, size.z);
            if (matWood != null) body.GetComponent<Renderer>().sharedMaterial = matWood;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // 3. Steep Pitched A-Frame Roof (Prism with solid gables, overhangs, and soffit)
            GameObject roof = new GameObject("Roof_Pitched");
            roof.transform.SetParent(cabin.transform, false);
            roof.transform.localPosition = new Vector3(0f, size.y, 0f);
            roof.transform.localScale = new Vector3(size.x * 1.20f, size.y * 0.74f, size.z * 1.20f);
            var mfR = roof.AddComponent<MeshFilter>();
            mfR.sharedMesh = _prismRoofMesh;
            var mrR = roof.AddComponent<MeshRenderer>();
            mrR.sharedMaterial = matRoof;

            // Ridge Beam Cap
            GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ridge.name = "Roof_RidgeBeam";
            ridge.transform.SetParent(cabin.transform, false);
            ridge.transform.localPosition = new Vector3(0f, size.y + (size.y * 0.74f) + 0.04f, 0f);
            ridge.transform.localScale = new Vector3(0.20f, 0.12f, size.z * 1.24f);
            if (matWood != null) ridge.GetComponent<Renderer>().sharedMaterial = matWood;
            Object.DestroyImmediate(ridge.GetComponent<Collider>());

            // 4. Fieldstone Chimney with Cap
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(cabin.transform, false);
            chimney.transform.localPosition = new Vector3(size.x * 0.30f, size.y * 1.15f, size.z * 0.20f);
            chimney.transform.localScale = new Vector3(0.60f, size.y * 1.20f, 0.60f);
            if (matStone != null) chimney.GetComponent<Renderer>().sharedMaterial = matStone;
            Object.DestroyImmediate(chimney.GetComponent<Collider>());

            GameObject chimneyCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimneyCap.name = "ChimneyCap";
            chimneyCap.transform.SetParent(chimney.transform, false);
            chimneyCap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            chimneyCap.transform.localScale = new Vector3(1.25f, 0.10f, 1.25f);
            if (matStone != null) chimneyCap.GetComponent<Renderer>().sharedMaterial = matStone;
            Object.DestroyImmediate(chimneyCap.GetComponent<Collider>());

            // 5. Rustic Front Door with Timber Trim Frame
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "FrontDoor_Frame";
            doorFrame.transform.SetParent(cabin.transform, false);
            doorFrame.transform.localPosition = new Vector3(0f, 0.28f + 0.85f, -size.z * 0.505f);
            doorFrame.transform.localScale = new Vector3(0.85f, 1.70f, 0.08f);
            if (matWood != null) doorFrame.GetComponent<Renderer>().sharedMaterial = matWood;
            Object.DestroyImmediate(doorFrame.GetComponent<Collider>());

            GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "FrontDoor_Panel";
            doorPanel.transform.SetParent(doorFrame.transform, false);
            doorPanel.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            doorPanel.transform.localScale = new Vector3(0.82f, 0.92f, 0.60f);
            if (matRoof != null) doorPanel.GetComponent<Renderer>().sharedMaterial = matRoof;
            Object.DestroyImmediate(doorPanel.GetComponent<Collider>());

            // 6. Warm Glowing Windows with Decorative Wooden Frames & Sills
            CreateWindowWithFrame(cabin.transform, "Window_Front_L", new Vector3(-size.x * 0.28f, size.y * 0.55f, -size.z * 0.505f), new Vector2(0.70f, 0.85f), matWindowGlow, matWood, 0f);
            CreateWindowWithFrame(cabin.transform, "Window_Front_R", new Vector3(size.x * 0.28f, size.y * 0.55f, -size.z * 0.505f), new Vector2(0.70f, 0.85f), matWindowGlow, matWood, 0f);
            CreateWindowWithFrame(cabin.transform, "Window_Side_L", new Vector3(-size.x * 0.505f, size.y * 0.55f, 0f), new Vector2(0.75f, 0.85f), matWindowGlow, matWood, 90f);

            // 7. Covered Wooden Front Porch with Sloping Awning Roof & Railings
            if (hasPorch)
            {
                float porchDepth = size.z * 0.55f;
                float porchCenterZ = -size.z * 0.50f - (porchDepth * 0.50f);

                // Porch Deck
                GameObject porch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                porch.name = "PorchDeck";
                porch.transform.SetParent(cabin.transform, false);
                porch.transform.localPosition = new Vector3(0f, 0.14f, porchCenterZ);
                porch.transform.localScale = new Vector3(size.x * 1.10f, 0.26f, porchDepth);
                if (matWood != null) porch.GetComponent<Renderer>().sharedMaterial = matWood;
                Object.DestroyImmediate(porch.GetComponent<Collider>());

                // Sloping Porch Roof Awning
                GameObject porchRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                porchRoof.name = "PorchRoof";
                porchRoof.transform.SetParent(cabin.transform, false);
                porchRoof.transform.localPosition = new Vector3(0f, size.y * 0.86f, porchCenterZ);
                porchRoof.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
                porchRoof.transform.localScale = new Vector3(size.x * 1.18f, 0.10f, porchDepth * 1.15f);
                if (matRoof != null) porchRoof.GetComponent<Renderer>().sharedMaterial = matRoof;
                Object.DestroyImmediate(porchRoof.GetComponent<Collider>());

                // Support Posts connecting deck to porch roof
                float postPosZ = porchCenterZ - (porchDepth * 0.44f);
                float postHeight = size.y * 0.74f;
                for (int p = -1; p <= 1; p += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = $"PorchPost_{(p < 0 ? "L" : "R")}";
                    post.transform.SetParent(cabin.transform, false);
                    post.transform.localPosition = new Vector3(p * size.x * 0.48f, 0.26f + (postHeight * 0.5f), postPosZ);
                    post.transform.localScale = new Vector3(0.14f, postHeight, 0.14f);
                    if (matWood != null) post.GetComponent<Renderer>().sharedMaterial = matWood;
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }

                // Porch Side Railings
                for (int r = -1; r <= 1; r += 2)
                {
                    GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail.name = $"PorchRail_{(r < 0 ? "L" : "R")}";
                    rail.transform.SetParent(cabin.transform, false);
                    rail.transform.localPosition = new Vector3(r * size.x * 0.51f, 0.65f, porchCenterZ);
                    rail.transform.localScale = new Vector3(0.08f, 0.08f, porchDepth * 0.88f);
                    if (matWood != null) rail.GetComponent<Renderer>().sharedMaterial = matWood;
                    Object.DestroyImmediate(rail.GetComponent<Collider>());
                }
            }
        }

        private static void CreateWindowWithFrame(Transform parent, string name, Vector3 pos, Vector2 size, Material matGlow, Material matFrame, float yRot)
        {
            GameObject winRoot = new GameObject(name);
            winRoot.transform.SetParent(parent, false);
            winRoot.transform.localPosition = pos;
            winRoot.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // Glowing glass
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "GlassGlow";
            glass.transform.SetParent(winRoot.transform, false);
            glass.transform.localScale = new Vector3(size.x, size.y, 0.04f);
            if (matGlow != null) glass.GetComponent<Renderer>().sharedMaterial = matGlow;
            Object.DestroyImmediate(glass.GetComponent<Collider>());

            // Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame_Top";
            frame.transform.SetParent(winRoot.transform, false);
            frame.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.04f, -0.01f);
            frame.transform.localScale = new Vector3(size.x + 0.12f, 0.08f, 0.06f);
            if (matFrame != null) frame.GetComponent<Renderer>().sharedMaterial = matFrame;
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Frame_Sill";
            sill.transform.SetParent(winRoot.transform, false);
            sill.transform.localPosition = new Vector3(0f, -size.y * 0.5f - 0.04f, -0.02f);
            sill.transform.localScale = new Vector3(size.x + 0.18f, 0.08f, 0.09f);
            if (matFrame != null) sill.GetComponent<Renderer>().sharedMaterial = matFrame;
            Object.DestroyImmediate(sill.GetComponent<Collider>());
        }

        private static void CreateWindowGlow(Transform parent, string name, Vector3 pos, Vector3 scale, Material matGlow)
        {
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = name;
            win.transform.SetParent(parent, false);
            win.transform.localPosition = pos;
            win.transform.localScale = scale;
            if (matGlow != null) win.GetComponent<Renderer>().sharedMaterial = matGlow;
            Object.DestroyImmediate(win.GetComponent<Collider>());
        }

#if UNITY_EDITOR
        [MenuItem("VR Remote Desktop/Rebuild Alpine Cottages Only")]
        public static void RebuildAlpineCottages()
        {
            var oldCottages = GameObject.Find("AlpineCottages");
            Transform parent = null;
            if (oldCottages != null)
            {
                parent = oldCottages.transform.parent;
                Object.DestroyImmediate(oldCottages);
            }
            else
            {
                var landscape = GameObject.Find("Nature3DLandscape");
                if (landscape != null) parent = landscape.transform;
            }

            if (parent == null)
            {
                Debug.LogWarning("[NatureLandscapeBuilder] Cannot find parent for AlpineCottages.");
                return;
            }

            _prismRoofMesh = null;
            EnsureProceduralMeshes();

            Material matCabinWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CabinWood.mat");
            Material matCabinRoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CabinRoof.mat");
            Material matWindowGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WindowGlowWarm.mat");
            Material matStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_RiverStone.mat");

            BuildCottages(parent, matCabinWood, matCabinRoof, matWindowGlow, matStone);

            // Also ensure RiverStone_4 does not clip into Cottage_VillageCenter
            var stone4 = GameObject.Find("RiverStone_4");
            if (stone4 != null)
            {
                stone4.transform.position = new Vector3(11.2f, 0.08f, -3.2f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[NatureLandscapeBuilder] Alpine Cottages successfully rebuilt and saved to scene!");
        }

        [InitializeOnLoadMethod]
        private static void AutoFixBrokenCottages()
        {
            EditorApplication.update += CheckAndFix;
        }

        private static void CheckAndFix()
        {
            EditorApplication.update -= CheckAndFix;
            var cottages = GameObject.Find("AlpineCottages");
            if (cottages != null)
            {
                var roof = cottages.transform.Find("Cottage_VillageCenter/Roof_Pitched");
                var mf = roof != null ? roof.GetComponent<MeshFilter>() : null;
                if (roof == null || mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount < 40)
                {
                    Debug.Log("[NatureLandscapeBuilder] Auto-fixing broken alpine cottage rooftop mesh...");
                    RebuildAlpineCottages();
                }
                else
                {
                    // Ensure scene file on disk is also saved
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cottages.scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[NatureLandscapeBuilder] Alpine Cottages confirmed intact and saved to scene file.");
                }
            }
        }
#endif
        #endregion

        #region 6. Natural Sunlight & Atmosphere
        private static void BuildSunlightAndAtmosphere(Transform parent, Material matCosmicVista)
        {
            // Warm Alpine Sunlight streaming through the window and illuminating 3D trees and peaks
            GameObject sunObj = new GameObject("NaturalSunlight");
            sunObj.transform.SetParent(parent, false);
            sunObj.transform.position = new Vector3(16.0f, 22.0f, 14.0f);
            sunObj.transform.rotation = Quaternion.Euler(34f, -76f, 0f);
            var light = sunObj.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 80f;
            light.spotAngle = 100f;
            light.innerSpotAngle = 55f;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 18.0f;
            light.shadows = LightShadows.Soft;

            // Distant soft atmospheric panoramic backdrop far beyond the mountains (x = 88m)
            // Gently blends mountain silhouettes into the sky horizon
            if (matCosmicVista != null)
            {
                GameObject skyBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyBackdrop.name = "DistantHorizonBackdrop";
                skyBackdrop.transform.SetParent(parent, false);
                skyBackdrop.transform.position = new Vector3(88.0f, 14.0f, -0.5f);
                skyBackdrop.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                skyBackdrop.transform.localScale = new Vector3(140.0f, 48.0f, 1f);
                skyBackdrop.GetComponent<Renderer>().sharedMaterial = matCosmicVista;
                Object.DestroyImmediate(skyBackdrop.GetComponent<Collider>());
            }
        }
        #endregion

        #region 7. Interactive Weather Particle Systems
        private static void BuildWeatherParticles(Transform parent)
        {
            GameObject particlesRoot = new GameObject("WeatherParticles");
            particlesRoot.transform.SetParent(parent, false);

            CreateWeatherParticle(particlesRoot.transform, "WeatherParticles_Rain",
                                  new Vector3(16.0f, 11.0f, -0.5f),
                                  new Vector3(26.0f, 1.0f, 32.0f),
                                  new Vector3(0.4f, -15.0f, 0.2f),
                                  new Color(0.70f, 0.85f, 1.0f, 0.70f),
                                  0.05f, 350f, 1.1f);

            CreateWeatherParticle(particlesRoot.transform, "WeatherParticles_Snow",
                                  new Vector3(16.0f, 11.0f, -0.5f),
                                  new Vector3(26.0f, 1.0f, 32.0f),
                                  new Vector3(0.5f, -2.0f, 0.3f),
                                  new Color(0.96f, 0.98f, 1.0f, 0.90f),
                                  0.10f, 160f, 5.5f);

            CreateWeatherParticle(particlesRoot.transform, "WeatherParticles_Autumn",
                                  new Vector3(16.0f, 9.5f, -0.5f),
                                  new Vector3(24.0f, 1.0f, 28.0f),
                                  new Vector3(1.2f, -1.5f, 0.7f),
                                  new Color(0.94f, 0.52f, 0.12f, 0.85f),
                                  0.14f, 45f, 6.0f);
        }

        private static void CreateWeatherParticle(Transform parent, string name, Vector3 pos, Vector3 boxSize,
                                                  Vector3 velocity, Color color, float size, float rate, float lifetime)
        {
            GameObject pObj = new GameObject(name);
            pObj.transform.SetParent(parent, false);
            pObj.transform.position = pos;

            var ps = pObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = velocity.magnitude;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2500;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(velocity.x * 0.7f, velocity.x * 1.3f);
            vel.y = new ParticleSystem.MinMaxCurve(velocity.y * 0.85f, velocity.y * 1.15f);
            vel.z = new ParticleSystem.MinMaxCurve(velocity.z * 0.7f, velocity.z * 1.3f);

            var renderer = pObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Material pMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("e823cd5b5d27c0f4b8256e7c12ee3e6d"));
                if (pMat == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                    if (s != null) pMat = new Material(s);
                }
                if (pMat != null) renderer.sharedMaterial = pMat;
            }

            ps.Stop();
        }
        #endregion

        #region Mesh Generation Helpers
        /// <summary>
        /// Generates a clean 3D cone mesh (used for pine tree tiered foliage and cone shapes).
        /// </summary>
        public static Mesh CreateConeMesh(float radius, float height, int segments, string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            Vector3 tip = new Vector3(0f, height, 0f);

            for (int i = 0; i < segments; i++)
            {
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                Vector3 p2 = new Vector3(Mathf.Cos(a2) * radius, 0f, Mathf.Sin(a2) * radius);

                // Side triangle (tip, p2, p1 for clockwise winding)
                int start = verts.Count;
                verts.Add(tip);
                verts.Add(p2);
                verts.Add(p1);

                Vector3 n = Vector3.Cross(p2 - tip, p1 - tip).normalized;
                norms.Add(n); norms.Add(n); norms.Add(n);
                uvs.Add(new Vector2(0.5f, 1f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0f));
                uvs.Add(new Vector2((float)i / segments, 0f));

                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);

                // Base triangle (center, p1, p2)
                int bStart = verts.Count;
                verts.Add(Vector3.zero);
                verts.Add(p1);
                verts.Add(p2);

                norms.Add(Vector3.down); norms.Add(Vector3.down); norms.Add(Vector3.down);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(a2) * 0.5f, 0.5f + Mathf.Sin(a2) * 0.5f));

                tris.Add(bStart); tris.Add(bStart + 1); tris.Add(bStart + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a multifaceted mountain peak mesh with organic craggy ridges.
        /// </summary>
        public static Mesh CreateMountainMesh(float radius, float height, int segments, string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            Vector3 tip = new Vector3(0f, height, 0f);

            // Perturb radii per segment for realistic natural rocky crags
            float[] rMods = new float[segments];
            for (int i = 0; i < segments; i++)
            {
                rMods[i] = 1.0f + 0.18f * Mathf.Sin(i * 3.4f) + 0.12f * Mathf.Cos(i * 5.1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius * rMods[i], 0f, Mathf.Sin(a1) * radius * rMods[i]);
                Vector3 p2 = new Vector3(Mathf.Cos(a2) * radius * rMods[next], 0f, Mathf.Sin(a2) * radius * rMods[next]);

                // Mid-slope ridge break point for cragged faceted mountains
                float midH = height * 0.45f;
                float midR = radius * 0.58f;
                Vector3 m1 = new Vector3(Mathf.Cos(a1) * midR * rMods[i], midH, Mathf.Sin(a1) * midR * rMods[i]);
                Vector3 m2 = new Vector3(Mathf.Cos(a2) * midR * rMods[next], midH, Mathf.Sin(a2) * midR * rMods[next]);

                // Upper quad -> 2 triangles (tip, m2, m1)
                int start = verts.Count;
                verts.Add(tip); verts.Add(m2); verts.Add(m1);
                Vector3 nUpper = Vector3.Cross(m2 - tip, m1 - tip).normalized;
                norms.Add(nUpper); norms.Add(nUpper); norms.Add(nUpper);
                uvs.Add(new Vector2(0.5f, 1f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0.45f));
                uvs.Add(new Vector2((float)i / segments, 0.45f));
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);

                // Lower quad (m1, m2, p2, p1) -> 2 triangles
                int lStart = verts.Count;
                verts.Add(m1); verts.Add(m2); verts.Add(p2);
                verts.Add(m1); verts.Add(p2); verts.Add(p1);
                Vector3 nLower = Vector3.Cross(m2 - m1, p1 - m1).normalized;
                for (int k = 0; k < 6; k++) norms.Add(nLower);
                uvs.Add(new Vector2((float)i / segments, 0.45f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0.45f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0f));
                uvs.Add(new Vector2((float)i / segments, 0.45f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0f));
                uvs.Add(new Vector2((float)i / segments, 0f));
                for (int k = 0; k < 6; k++) tris.Add(lStart + k);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates a snow cap mesh fitted atop the mountain peak.
        /// </summary>
        public static Mesh CreateSnowCapMesh(float radius, float height, int segments, string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            Vector3 tip = new Vector3(0f, height, 0f);

            for (int i = 0; i < segments; i++)
            {
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                // Wavy bottom rim simulating snowdrifts
                float rimY1 = Mathf.Sin(i * 2.8f) * 0.08f * height;
                float rimY2 = Mathf.Sin((i + 1) * 2.8f) * 0.08f * height;

                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, rimY1, Mathf.Sin(a1) * radius);
                Vector3 p2 = new Vector3(Mathf.Cos(a2) * radius, rimY2, Mathf.Sin(a2) * radius);

                int start = verts.Count;
                verts.Add(tip); verts.Add(p2); verts.Add(p1);
                Vector3 n = Vector3.Cross(p2 - tip, p1 - tip).normalized;
                norms.Add(n); norms.Add(n); norms.Add(n);
                uvs.Add(new Vector2(0.5f, 1f));
                uvs.Add(new Vector2((float)(i + 1) / segments, 0f));
                uvs.Add(new Vector2((float)i / segments, 0f));
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Generates an A-frame triangular prism mesh (used for cottage pitched roofs).
        /// </summary>
        public static Mesh CreatePrismMesh(float width, float height, float length, string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            float hW = width * 0.5f;
            float hL = length * 0.5f;

            // Vertices:
            // 0: Apex Front (0, height, -hL)
            // 1: Apex Back  (0, height,  hL)
            // 2: Bottom-Left Front  (-hW, 0, -hL)
            // 3: Bottom-Left Back   (-hW, 0,  hL)
            // 4: Bottom-Right Front ( hW, 0, -hL)
            // 5: Bottom-Right Back  ( hW, 0,  hL)

            Vector3 v0 = new Vector3(0f, height, -hL);
            Vector3 v1 = new Vector3(0f, height, hL);
            Vector3 v2 = new Vector3(-hW, 0f, -hL);
            Vector3 v3 = new Vector3(-hW, 0f, hL);
            Vector3 v4 = new Vector3(hW, 0f, -hL);
            Vector3 v5 = new Vector3(hW, 0f, hL);

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
            {
                int start = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                verts.Add(a); verts.Add(c); verts.Add(d);

                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                for (int i = 0; i < 6; i++) norms.Add(n);

                uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC);
                uvs.Add(uvA); uvs.Add(uvC); uvs.Add(uvD);

                for (int i = 0; i < 6; i++) tris.Add(start + i);
            }

            void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
            {
                int start = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);

                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                norms.Add(n); norms.Add(n); norms.Add(n);

                uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC);

                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            }

            // 1. Left slope outward (-X, +Y normal)
            AddQuad(v0, v2, v3, v1, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f));
            // Left slope inward (double-sided seal)
            AddQuad(v0, v1, v3, v2, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f));

            // 2. Right slope outward (+X, +Y normal)
            AddQuad(v0, v1, v5, v4, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            // Right slope inward (double-sided seal)
            AddQuad(v0, v4, v5, v1, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f));

            // 3. Front triangle outward (-Z normal)
            AddTri(v0, v4, v2, new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            // Front triangle inward (double-sided seal)
            AddTri(v0, v2, v4, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f));

            // 4. Back triangle outward (+Z normal)
            AddTri(v1, v3, v5, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            // Back triangle inward (double-sided seal)
            AddTri(v1, v5, v3, new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f));

            // 5. Bottom face outward (-Y normal)
            AddQuad(v2, v4, v5, v3, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            // Bottom face inward (+Y normal)
            AddQuad(v2, v3, v5, v4, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion
    }
}
