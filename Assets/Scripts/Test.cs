using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Options {
    public bool collisions;
    public bool scripts;
    public bool lighting;
    public string shape;

    private static readonly Dictionary<string, (int verts, int tris)> shapeVerts = new() {
        ["cube"] = (24, 12),
        ["capsule"] = (550, 832),
        ["cylinder"] = (88, 80),
        ["sphere"] = (515, 768),
    };

    public Options() {
        collisions = false;
        scripts = false;
        lighting = false;
        shape = "cube";
    }

    public string GetShapeName() {
        return (shape?[0].ToString().ToUpper() + shape?[1..].ToLower()) ?? "";
    }
    public string GetShapeSize() {
        var size = shapeVerts[shape.ToLower()];
        return $"{size.verts} verts, {size.tris} tris";
    }
    public override string ToString() {
        return $"  Collisions: {collisions}\n  Scripts: {scripts}\n  Lighting: {lighting}\n  Shape: {GetShapeName()} ({GetShapeSize()})";
    }
    public string ToFileName() {
        return $"Collisions_{(collisions ? "On" : "Off")} Scripts_{(scripts ? "On" : "Off")} Lighting_{(lighting ? "On" : "Off")} Shapes_{GetShapeName()}";
    }
}

public class Stats {
    public float countFpsLess70;
    public float countFpsLess60;
    public float countFpsLess50;
    public float countFpsLess40;
    public float countFpsLess30;

    public bool IsDone { get => countFpsLess30 > 0; }

    public Stats() {
        countFpsLess30 = countFpsLess40 = countFpsLess50 = countFpsLess60 = countFpsLess70 = -1;
    }

    public void Update(float fps, int count) {
        if (count < 1000) {
            return;
        }
        if (fps < 70 && countFpsLess70 == -1) {
            countFpsLess70 = count;
        }
        else if (fps < 60 && countFpsLess60 == -1) {
            countFpsLess60 = count;
        }
        else if (fps < 50 && countFpsLess50 == -1) {
            countFpsLess50 = count;
        }
        else if (fps < 40 && countFpsLess40 == -1) {
            countFpsLess40 = count;
        }
        else if (fps < 30 && countFpsLess30 == -1) {
            countFpsLess30 = count;
        }
    }

    public override string ToString() {
        return $"  fps < 70: {countFpsLess70:#,###}\n  fps < 60: {countFpsLess60:#,###}\n  fps < 50: {countFpsLess50:#,###}\n  fps < 40: {countFpsLess40:#,###}\n  fps < 30: {countFpsLess30:#,###}";
    }
}

public class Test : MonoBehaviour {
    public TextMeshProUGUI txtStatus;
    public TextMeshProUGUI txtOptions;
    public Transform spawnParent;
    public BoxCollider spawnRange;
    public GameObject spawnPrefab;

    private float spawnDelay;
    private float spawnAmount;
    private float spawnTimer;
    private int spawnCount;
    private Material matLit;
    private Material matUnlit;
    private Stats stats;
    private List<float> fpsList;
    private bool killSwitch;
    private Options options;
    private Dictionary<string, Mesh> meshes;

    private void Start() {
        spawnCount = 0;
        stats = new();
        fpsList = new();
        killSwitch = false;
        spawnAmount = 1;
        spawnDelay = 0.15f;
        spawnTimer = spawnDelay;
        XmlSerializer xs = new(typeof(Options));
        try {
            using (FileStream fs = new("options.xml", FileMode.Open)) {
                options = xs.Deserialize(fs) as Options;
            }
        }
        catch {
            options = new();
            using (FileStream fs = new("options.xml", FileMode.CreateNew)) {
                xs.Serialize(fs, options);
            }
        }
        if (!options.collisions) {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("SpawnObject"), LayerMask.NameToLayer("SpawnObject"));
        }
        txtOptions.text = options.ToString();

        meshes = new() {
            ["capsule"] = GetMesh(PrimitiveType.Capsule),
            ["cube"] = GetMesh(PrimitiveType.Cube),
            ["cylinder"] = GetMesh(PrimitiveType.Cylinder),
            ["sphere"] = GetMesh(PrimitiveType.Sphere),
        };
        matLit = Resources.Load<Material>("Mat Lit");
        matUnlit = Resources.Load<Material>("Mat Unlit");
    }
    private Mesh GetMesh(PrimitiveType type) {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.SetActive(false);
        return obj.GetComponent<MeshFilter>().mesh;
    }

    private void Update() {
        if (killSwitch) {
            return;
        }
        spawnTimer -= Time.deltaTime;
        if (spawnTimer < 0) {
            for (int i = 0; i < spawnAmount; i++) {
                var location = new Vector3(
                    Random.Range(spawnRange.bounds.min.x, spawnRange.bounds.max.x),
                    Random.Range(spawnRange.bounds.min.y, spawnRange.bounds.max.y),
                    Random.Range(spawnRange.bounds.min.z, spawnRange.bounds.max.z)
                );
                var spawnObj = Instantiate(spawnPrefab, spawnParent);
                spawnObj.transform.position = location;
                if (options.scripts) {
                    spawnObj.GetComponent<SpawnedObject>().enabled = true;
                }
                spawnObj.GetComponent<MeshFilter>().mesh = meshes[options.shape];
                spawnObj.GetComponent<Renderer>().material = (options.lighting ? matLit : matUnlit);
                spawnCount++;
            }
            spawnTimer = spawnDelay;
        }
        spawnAmount += Time.deltaTime * 0.6f;
        spawnDelay -= spawnDelay * spawnDelay;
        if (spawnDelay < 0.001f) {
            spawnDelay = 0.001f;
        }
        float fps = 1.0f / Time.deltaTime;
        fpsList.Add(fps);
        if (fpsList.Count > 40) {
            fpsList.RemoveAt(0);
        }
        fps = fpsList.Average();
        stats.Update(fps, spawnCount);
        killSwitch = stats.IsDone;
        txtStatus.text = $"Count: {spawnCount:#,###}\nFrame Time: {Time.deltaTime:0.0000}\nFPS: {fps:#,#00.0}\n\n{stats}";

        if (killSwitch) {
            string content = $"Options:\n{options}\n\nStats:\n{stats}";
            File.WriteAllText($"Results {options.ToFileName()}.txt", content);
        }
    }

    public void DeleteCube() {
        spawnCount--;
    }
}
