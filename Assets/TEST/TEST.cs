using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AD.BASE;
using Unity.VisualScripting;
using UnityEngine;

public class TEST : MonoBehaviour
{
    public ADFile m_File;
    public Testing testingData;

    public void Start()
    {
        m_File = new ADFile(new ADSettings( Application.streamingAssetsPath + "/Test.txt"));
        testingData = new()
        {
            //next = new()
            //{
            //    id = "123"
            //},
            x = 5,
            y = 5
        };
    }

    public void TestStream()
    {
        //using var s = ADFile.CreateStream(new ADSettings(m_File.FilePath), ADStreamEnum.FileMode.Write);
        //using var sw =new StreamWriter(s);
        //sw.Write("xxxxx");
        //sw.Dispose();
        ES3.Save("Test", testingData, m_File.FilePath);
    }

    public void Save()
    {
        m_File.Serialize<Testing>(testingData, "Test");
    }

    public void Load()
    {
        m_File.Deserialize<Testing>(out testingData, "Test");
    }
}

[Serializable]
public class Testing
{
    public int x;
    public int y;
    //public Testing2 next;
}

[Serializable]
public class Testing2
{
    public string id;
}
