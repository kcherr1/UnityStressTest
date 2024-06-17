using UnityEngine;

public class ObjectDeleter : MonoBehaviour {
    public Test test;

    private void OnTriggerEnter(Collider other) {
        Destroy(other.gameObject);
        test.DeleteCube();
    }
}
