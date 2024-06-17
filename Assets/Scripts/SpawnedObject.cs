using UnityEngine;

public class SpawnedObject : MonoBehaviour {
    private Material mat;
    public float coolDown = 10.0f;

    private void Start() {
        mat = GetComponent<Renderer>().material;
        mat.color = Color.white;
    }

    private void Update() {
        if (coolDown > 0) {
            coolDown -= Time.deltaTime;
            mat.color = Color.Lerp(Color.black, Color.white, coolDown / 2.0f);
        }
    }
}
