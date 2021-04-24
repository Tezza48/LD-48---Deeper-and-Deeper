using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : GridEntity
{
    public UnityEvent onPlayerMoved;

    public int xp;
    public int level;

    private void Awake()
    {
        onPlayerMoved = new UnityEvent();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Up"))
        {
            move(Vector2Int.up);
        }

        if (Input.GetButtonDown("Down"))
        {
            move(Vector2Int.down);
        }

        if (Input.GetButtonDown("Left"))
        {
            move(Vector2Int.left);
        }

        if (Input.GetButtonDown("Right"))
        {
            move(Vector2Int.right);
        }
    }

    private void move(Vector2Int delta)
    {
        position += delta;

        onPlayerMoved.Invoke();
    }
}