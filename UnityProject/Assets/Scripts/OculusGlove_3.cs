using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.Threading;
using System.Text;

public class OculusGlove_3 : MonoBehaviour
{

    #region OculusVariable
    public GameObject item;
    public OVRSkeleton leftHand;
    public OVRSkeleton rightHand;
    private List<OVRBone> leftBones;
    private List<OVRBone> rightBones;

    public GameObject sensorPrefab;
    public GameObject sensorPrefab1;  //This one is for testing only
    private int numSensors = 48;
    private GameObject[] leftSensors;
    private GameObject[] rightSensors;
    public float[] leftForces;
    public float[] rightForces;
    #endregion

    #region  BLE variable
    public KeyboardMonitoring other;
    #endregion

    public int MessageCounting; 
    public byte[] WriteByteArray = new byte[49]; 

    // Start is called before the first frame update
    private void Start()
    {


        #region oculus start
        leftSensors = new GameObject[numSensors];
        rightSensors = new GameObject[numSensors];
        leftForces = new float[numSensors];
        rightForces = new float[numSensors];

        StartCoroutine(InitializeSkeleton());
        #endregion
    }

    IEnumerator InitializeSkeleton()
    {
        while (leftHand.Bones.Count == 0 || rightHand.Bones.Count == 0)
            yield return null;

        leftBones = new List<OVRBone>(leftHand.Bones);
        rightBones = new List<OVRBone>(rightHand.Bones);
        print($"Left bone count: {leftBones.Count}");
        print($"Right bone count: {rightBones.Count}");

        foreach (var bone in leftBones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb2)
            {
                leftSensors[6] = Instantiate(sensorPrefab, new Vector3(-0.02f, 0.01f, 0f), Quaternion.identity);
                leftSensors[6].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb3)
            {
                leftSensors[5] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[4] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[3] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[2] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[1] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[0] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[5].transform.SetParent(bone.Transform, false);
                leftSensors[4].transform.SetParent(bone.Transform, false);
                leftSensors[3].transform.SetParent(bone.Transform, false);
                leftSensors[2].transform.SetParent(bone.Transform, false);
                leftSensors[1].transform.SetParent(bone.Transform, false);
                leftSensors[0].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index1)
            {
                leftSensors[14] = Instantiate(sensorPrefab, new Vector3(-0.03f, 0.01f, 0f), Quaternion.identity);
                leftSensors[14].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index2)
            {
                leftSensors[13] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.01f, 0f), Quaternion.identity);
                leftSensors[13].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index3)
            {
                leftSensors[12] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[11] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[10] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[9] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[8] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[7] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[12].transform.SetParent(bone.Transform, false);
                leftSensors[11].transform.SetParent(bone.Transform, false);
                leftSensors[10].transform.SetParent(bone.Transform, false);
                leftSensors[9].transform.SetParent(bone.Transform, false);
                leftSensors[8].transform.SetParent(bone.Transform, false);
                leftSensors[7].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1)
            {
                leftSensors[22] = Instantiate(sensorPrefab, new Vector3(-0.03f, 0.01f, 0f), Quaternion.identity);
                leftSensors[22].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle2)
            {
                leftSensors[21] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.01f, 0f), Quaternion.identity);
                leftSensors[21].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle3)
            {
                leftSensors[20] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[19] = Instantiate(sensorPrefab, new Vector3(-0.005f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[18] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[17] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[16] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, -0.002f), Quaternion.identity);
                leftSensors[15] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.008f, 0.002f), Quaternion.identity);
                leftSensors[20].transform.SetParent(bone.Transform, false);
                leftSensors[19].transform.SetParent(bone.Transform, false);
                leftSensors[18].transform.SetParent(bone.Transform, false);
                leftSensors[17].transform.SetParent(bone.Transform, false);
                leftSensors[16].transform.SetParent(bone.Transform, false);
                leftSensors[15].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring1)
            {
                leftSensors[26] = Instantiate(sensorPrefab, new Vector3(-0.03f, 0.01f, 0f), Quaternion.identity);
                leftSensors[26].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring2)
            {
                leftSensors[25] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.01f, 0f), Quaternion.identity);
                leftSensors[25].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring3)
            {
                leftSensors[24] = Instantiate(sensorPrefab, new Vector3(-0.007f, 0.008f, 0f), Quaternion.identity);
                leftSensors[23] = Instantiate(sensorPrefab, new Vector3(-0.013f, 0.008f, 0f), Quaternion.identity);
                leftSensors[24].transform.SetParent(bone.Transform, false);
                leftSensors[23].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky1)
            {
                leftSensors[30] = Instantiate(sensorPrefab, new Vector3(-0.03f, 0.01f, 0f), Quaternion.identity);
                leftSensors[30].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky2)
            {
                leftSensors[29] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.01f, 0f), Quaternion.identity);
                leftSensors[29].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky3)
            {
                leftSensors[28] = Instantiate(sensorPrefab, new Vector3(-0.007f, 0.008f, 0f), Quaternion.identity);
                leftSensors[27] = Instantiate(sensorPrefab, new Vector3(-0.013f, 0.008f, 0f), Quaternion.identity);
                leftSensors[28].transform.SetParent(bone.Transform, false);
                leftSensors[27].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
            {
                leftSensors[31] = Instantiate(sensorPrefab, new Vector3(-0.06f, 0.015f, 0f), Quaternion.identity);
                leftSensors[32] = Instantiate(sensorPrefab, new Vector3(-0.05f, 0.015f, 0.015f), Quaternion.identity);
                leftSensors[33] = Instantiate(sensorPrefab, new Vector3(-0.035f, 0.015f, 0.025f), Quaternion.identity);
                leftSensors[34] = Instantiate(sensorPrefab, new Vector3(-0.02f, 0.015f, 0.015f), Quaternion.identity);
                leftSensors[35] = Instantiate(sensorPrefab, new Vector3(-0.01f, 0.015f, 0f), Quaternion.identity);
                leftSensors[36] = Instantiate(sensorPrefab, new Vector3(-0.02f, 0.015f, -0.015f), Quaternion.identity);
                leftSensors[37] = Instantiate(sensorPrefab, new Vector3(-0.035f, 0.015f, -0.025f), Quaternion.identity);
                leftSensors[38] = Instantiate(sensorPrefab, new Vector3(-0.05f, 0.015f, -0.015f), Quaternion.identity);
                leftSensors[39] = Instantiate(sensorPrefab, new Vector3(-0.045f, 0.015f, 0.005f), Quaternion.identity);
                leftSensors[40] = Instantiate(sensorPrefab, new Vector3(-0.035f, 0.015f, 0.01f), Quaternion.identity);
                leftSensors[41] = Instantiate(sensorPrefab, new Vector3(-0.025f, 0.015f, 0.01f), Quaternion.identity);
                leftSensors[42] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.015f, 0.005f), Quaternion.identity);
                leftSensors[43] = Instantiate(sensorPrefab, new Vector3(-0.015f, 0.015f, -0.005f), Quaternion.identity);
                leftSensors[44] = Instantiate(sensorPrefab, new Vector3(-0.025f, 0.015f, -0.01f), Quaternion.identity);
                leftSensors[45] = Instantiate(sensorPrefab, new Vector3(-0.035f, 0.015f, -0.01f), Quaternion.identity);
                leftSensors[46] = Instantiate(sensorPrefab, new Vector3(-0.045f, 0.015f, -0.005f), Quaternion.identity);
                leftSensors[47] = Instantiate(sensorPrefab, new Vector3(-0.03f, 0.015f, 0f), Quaternion.identity);
                for (int i = 31; i < numSensors; i++)
                    leftSensors[i].transform.SetParent(bone.Transform, false);
            }
        }

        foreach (var bone in rightBones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb2)
            {
                rightSensors[6] = Instantiate(sensorPrefab, new Vector3(0.02f, -0.01f, 0f), Quaternion.identity);
                rightSensors[6].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Thumb3)
            {
                rightSensors[4] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[5] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[2] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[3] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[0] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[1] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[5].transform.SetParent(bone.Transform, false);
                rightSensors[4].transform.SetParent(bone.Transform, false);
                rightSensors[3].transform.SetParent(bone.Transform, false);
                rightSensors[2].transform.SetParent(bone.Transform, false);
                rightSensors[1].transform.SetParent(bone.Transform, false);
                rightSensors[0].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index1)
            {
                rightSensors[14] = Instantiate(sensorPrefab, new Vector3(0.03f, -0.01f, 0f), Quaternion.identity);
                rightSensors[14].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index2)
            {
                rightSensors[13] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.01f, 0f), Quaternion.identity);
                rightSensors[13].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Index3)
            {
                rightSensors[11] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[12] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[9] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[10] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[7] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[8] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[12].transform.SetParent(bone.Transform, false);
                rightSensors[11].transform.SetParent(bone.Transform, false);
                rightSensors[10].transform.SetParent(bone.Transform, false);
                rightSensors[9].transform.SetParent(bone.Transform, false);
                rightSensors[8].transform.SetParent(bone.Transform, false);
                rightSensors[7].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1)
            {
                rightSensors[22] = Instantiate(sensorPrefab, new Vector3(0.03f, -0.01f, 0f), Quaternion.identity);
                rightSensors[22].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle2)
            {
                rightSensors[21] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.01f, 0f), Quaternion.identity);
                rightSensors[21].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Middle3)
            {
                rightSensors[19] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[20] = Instantiate(sensorPrefab, new Vector3(0.005f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[17] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[18] = Instantiate(sensorPrefab, new Vector3(0.01f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[15] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, 0.002f), Quaternion.identity);
                rightSensors[16] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.008f, -0.002f), Quaternion.identity);
                rightSensors[20].transform.SetParent(bone.Transform, false);
                rightSensors[19].transform.SetParent(bone.Transform, false);
                rightSensors[18].transform.SetParent(bone.Transform, false);
                rightSensors[17].transform.SetParent(bone.Transform, false);
                rightSensors[16].transform.SetParent(bone.Transform, false);
                rightSensors[15].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring1)
            {
                rightSensors[26] = Instantiate(sensorPrefab, new Vector3(0.03f, -0.01f, 0f), Quaternion.identity);
                rightSensors[26].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring2)
            {
                rightSensors[25] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.01f, 0f), Quaternion.identity);
                rightSensors[25].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Ring3)
            {
                rightSensors[24] = Instantiate(sensorPrefab, new Vector3(0.007f, -0.008f, 0f), Quaternion.identity);
                rightSensors[23] = Instantiate(sensorPrefab, new Vector3(0.013f, -0.008f, 0f), Quaternion.identity);
                rightSensors[24].transform.SetParent(bone.Transform, false);
                rightSensors[23].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky1)
            {
                rightSensors[30] = Instantiate(sensorPrefab, new Vector3(0.03f, -0.01f, 0f), Quaternion.identity);
                rightSensors[30].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky2)
            {
                rightSensors[29] = Instantiate(sensorPrefab, new Vector3(0.015f, -0.01f, 0f), Quaternion.identity);
                rightSensors[29].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_Pinky3)
            {
                rightSensors[28] = Instantiate(sensorPrefab, new Vector3(0.007f, -0.008f, 0f), Quaternion.identity);
                rightSensors[27] = Instantiate(sensorPrefab, new Vector3(0.013f, -0.008f, 0f), Quaternion.identity);
                rightSensors[28].transform.SetParent(bone.Transform, false);
                rightSensors[27].transform.SetParent(bone.Transform, false);
            }
            else if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
            {
                rightSensors[31] = Instantiate(sensorPrefab, new Vector3(0.06f+0.03f, -0.015f - 0.009f, 0f), Quaternion.identity);
                rightSensors[32] = Instantiate(sensorPrefab, new Vector3(0.05f + 0.03f, -0.015f - 0.009f, -0.015f), Quaternion.identity);
                rightSensors[33] = Instantiate(sensorPrefab, new Vector3(0.035f + 0.03f, -0.015f - 0.009f, -0.025f), Quaternion.identity);
                rightSensors[34] = Instantiate(sensorPrefab, new Vector3(0.02f + 0.03f, -0.015f - 0.009f, -0.015f), Quaternion.identity);
                rightSensors[35] = Instantiate(sensorPrefab, new Vector3(0.01f + 0.03f, -0.015f - 0.009f, 0f), Quaternion.identity);
                rightSensors[36] = Instantiate(sensorPrefab, new Vector3(0.02f + 0.03f, -0.015f - 0.009f, 0.015f), Quaternion.identity);
                rightSensors[37] = Instantiate(sensorPrefab, new Vector3(0.035f + 0.03f, -0.015f - 0.009f, 0.025f), Quaternion.identity);
                rightSensors[38] = Instantiate(sensorPrefab, new Vector3(0.05f + 0.03f, -0.015f - 0.009f, 0.015f), Quaternion.identity);
                rightSensors[39] = Instantiate(sensorPrefab, new Vector3(0.045f + 0.03f, -0.015f - 0.009f, -0.005f), Quaternion.identity);
                rightSensors[40] = Instantiate(sensorPrefab, new Vector3(0.035f + 0.0325f, -0.015f - 0.009f, -0.01f), Quaternion.identity);
                rightSensors[41] = Instantiate(sensorPrefab, new Vector3(0.025f + 0.034f, -0.015f - 0.009f, -0.01f), Quaternion.identity);
                rightSensors[42] = Instantiate(sensorPrefab, new Vector3(0.015f + 0.038f, -0.015f - 0.009f, -0.005f), Quaternion.identity);
                rightSensors[43] = Instantiate(sensorPrefab, new Vector3(0.015f + 0.038f, -0.015f - 0.009f, 0.005f), Quaternion.identity);
                rightSensors[44] = Instantiate(sensorPrefab, new Vector3(0.025f + 0.034f, -0.015f - 0.009f, 0.01f), Quaternion.identity);
                rightSensors[45] = Instantiate(sensorPrefab, new Vector3(0.035f + 0.0325f, -0.015f - 0.009f, 0.01f), Quaternion.identity);
                rightSensors[46] = Instantiate(sensorPrefab, new Vector3(0.045f + 0.03f, -0.015f - 0.009f, 0.005f), Quaternion.identity);
                rightSensors[47] = Instantiate(sensorPrefab, new Vector3(0.03f + 0.034f, -0.015f - 0.009f, 0f), Quaternion.identity);
                for (int i = 31; i < numSensors; i++)
                    rightSensors[i].transform.SetParent(bone.Transform, false);
            }
        }
    }



    // Update is called once per frame
    private void Update()
    {
        ComputeForces();
        MessageCounting = MessageCounting + 1;
    }

    #region oculus compute force
    private void ComputeForces()
    {
        bool overlapped;
        var itemCollider = item.GetComponent<Collider>();
        Vector3 direction;
        float distance;

        for (int i = 0; i < numSensors; i++)
        {
            overlapped = Physics.ComputePenetration(leftSensors[i].GetComponent<Collider>(), leftSensors[i].transform.position, leftSensors[i].transform.rotation,
                itemCollider, item.transform.position, item.transform.rotation, out direction, out distance);
            if (overlapped)
            {
                leftForces[i] = distance;

                if (leftForces[27] > 0)         //Contact == Stick
                {
                    item.transform.parent = leftSensors[15].transform;
                    item.GetComponent<Rigidbody>().useGravity = false;
                    item.GetComponent<Rigidbody>().isKinematic = false;
                    print("Stick!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1");
                    other.StartThreading();
                }

            }
            else
                leftForces[i] = 0f;
            overlapped = Physics.ComputePenetration(rightSensors[i].GetComponent<Collider>(), rightSensors[i].transform.position, rightSensors[i].transform.rotation,
                itemCollider, item.transform.position, item.transform.rotation, out direction, out distance);
            if (overlapped)
            {
                rightForces[i] = distance;
                if (rightForces[10] > 0)         //Contact == OUT
                {
                    item.transform.parent = null;
                    item.GetComponent<Rigidbody>().useGravity = true;
                    item.GetComponent<Rigidbody>().isKinematic = false;
                    print("OUT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1");
                    other.StopThreading();

                }
            }
            else
                rightForces[i] = 0f;
        }
    }
    #endregion
   
}
