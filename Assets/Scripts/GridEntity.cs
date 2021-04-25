using System.Collections.Generic;
using UnityEngine;

public class GridEntity : MonoBehaviour
{
    public Vector2Int position;

    public GameObject[] views;

    public Coroutine moveTween;

    private void OnValidate()
    {
        position = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        transform.position = (Vector2)position;
    }

    private void Start()
    {
        foreach(var view in views)
        {
            Instantiate(view, view.transform.position + new Vector3(-WorldController.MAP_WIDTH, 0.0f), view.transform.rotation, view.transform.parent);
            Instantiate(view, view.transform.position + new Vector3(WorldController.MAP_WIDTH, 0.0f), view.transform.rotation, view.transform.parent);
        }
    }
}