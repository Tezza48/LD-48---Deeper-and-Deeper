using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : GridEntity
{
    public bool mature = false;

    public GameObject immatureView;
    public GameObject matureView;

    public void Eat()
    {
        mature = true;
        immatureView.SetActive(false);
        matureView.SetActive(true);
    }
}
