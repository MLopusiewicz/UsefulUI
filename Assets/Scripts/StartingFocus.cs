using UnityEngine;
using UnityEngine.UIElements;

public class StartingFocus : MonoBehaviour {

    public string entryPoint;
    void Start() {
        GetComponent<UIDocument>().rootVisualElement.Q(name: entryPoint).Focus();
        GetComponent<UIDocument>().rootVisualElement.Query<Button>().ForEach(x => x.clicked += () => Debug.Log($"c: {x.name}"));
    }

    void Update() {

    }
}
