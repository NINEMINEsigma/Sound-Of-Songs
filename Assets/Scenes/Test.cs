using AD.BASE;
using System;
using System.IO;
using UnityEngine;

public class Test : MonoBehaviour
{
    public TestCycle data;
    public TestCycle data2;
    public bool isRead = false;

    // Start is called before the first frame update
    void Start()
    {
        ADSettings settings = new(Path.Combine(Application.streamingAssetsPath, "in.txt"), ADStreamEnum.Location.File, ADStreamEnum.Format.LINE) ;
        using ADFile file = new(settings);
        if (isRead)
        {
            file.Deserialize(out data, "test");
            data2 = data.next;
        }
        else
        {
            data = new()
            {
                id = "1"
            };
            data2 = new()
            {
                id = "test"
            };
            data.next = data2;
            //data2.next = data;

            file.Serialize(data2, "test");
        }
    }
}

[Serializable]
public class TestCycle
{
    public string id;
    public TestCycle next;
}

[Serializable]
public class TestClass
{
    public TestData a;
    public TestData b;
    public string c;
    public float d;
}

[Serializable]
public class TestData
{
    public int id;
    public TestData2 d1;
    public TestData2 d2;
}

[Serializable]
public class TestData2
{
    public string k;
    public string v;
    public Result result;
}

[Serializable]
public class Result
{
    public float ilong;
    public string id;
}