using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelegateTest : MonoBehaviour
{
    public delegate void MyDelegate();
    public List<MyDelegate> myDelegates = new List<MyDelegate>();
    public void MyDelegateFunction(int x) {
        Debug.Log("MyDelegateFunction called with x = " + x);
    }
    public List<Action<int>> myActions = new List<Action<int>>();
    public List<Action> myVoidActions = new List<Action>();
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            int n = i;
            //myDelegates.Add(() => { MyDelegateFunction(n); });
            //myActions.Add((x) => { MyDelegateFunction(x); });
            myVoidActions.Add(() => { MyDelegateFunction(i); });
        }
        for (int i = 0; i < 5; i++)
            //myDelegates[i].Invoke();
            // myActions[i].Invoke(i);
            myVoidActions[i].Invoke();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
