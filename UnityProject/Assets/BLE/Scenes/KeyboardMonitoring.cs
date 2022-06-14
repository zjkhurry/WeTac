using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class KeyboardMonitoring : MonoBehaviour
{
    public Thread receiveThread;
    public Thread ContactThread;
    public Demo other;
    public int threadStart;
    // Start is called before the first frame update
    void Start()
    {

        //receiveThread = new Thread(DelayFunction);
        //receiveThread.IsBackground = true;
        //receiveThread.Start();

        ContactThread = new Thread(ContactWrite);
        ContactThread.IsBackground = true;
        ContactThread.Start();

        threadStart = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("f"))
        {
            print("Keyboard input got");
        }


        if (Input.GetKey("r"))
        {
            other.PrintSthn();
        }

        if (Input.GetKey("w"))
        {
            byte lol222 = 2;
            byte[] arratf= {0};
            arratf[0] = lol222;
            other.WriteCallFunction(arratf);
        }

        if (Input.GetKeyDown("i"))
        {
            other.SelectCharacteristic_Initialization();
        }

        if (Input.GetKeyDown("n"))
        {
            threadStart = 1;
            print("Thread on");
        }

        if (Input.GetKeyDown("m"))
        {
            threadStart = 0;
            print("Thread off");
        }


    }

    public void StartThreading()
    {
        threadStart = 1;
        print("Thread on");
    }

    public void StopThreading()
    {
        threadStart = 0;
        print("Thread off");
    }

    private void ContactWrite()
    {
        while (true)//??
        {
            if (threadStart == 1)
            {
                byte[] arratf = { 0 };
                arratf[0] = 31;
                other.WriteCallFunction2(arratf);
                print("31 ON");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code
                arratf[0] = 31+32;
                other.WriteCallFunction2(arratf);
                print("31+32 Off");
                threadStart = 0;
            }
        }
    }

    private void DelayFunction()
    {
        while (true)
        {
            if (threadStart == 1)
            {
                //Open channel
                byte lol222 = 19;
                byte[] arratf = { 0 };
                arratf[0] = lol222;
                other.WriteCallFunction2(arratf);
                //print("19 On");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 23;
                other.WriteCallFunction2(arratf);
                //print("23 On");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 0;
                other.WriteCallFunction2(arratf);
                //print("0 On");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 27;
                other.WriteCallFunction2(arratf);
                //print("27 On");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 31;
                other.WriteCallFunction2(arratf);
                //print("31 On");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code
            }
            if(threadStart == 0) {
                //////////////////////////////////////////////////////////Close
                byte[] arratf = { 0 };
                arratf[0] = 19+32;
                other.WriteCallFunction2(arratf);
                //print("19+32 Off");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 23 + 32;
                other.WriteCallFunction2(arratf);
                //print("23+32 Off");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 0 + 32;
                other.WriteCallFunction2(arratf);
                //print("0+32 Off");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 27 + 32;
                other.WriteCallFunction2(arratf);
                //print("27+32 Off");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

                arratf[0] = 31 + 32;
                other.WriteCallFunction2(arratf);
                //print("31+32 Off");
                Thread.Sleep(100); //delay ms, but this is a C# code not unity code

            }
        }     
    }
}
