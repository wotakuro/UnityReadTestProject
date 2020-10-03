using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GenerateDummyFile
{
    [MenuItem("Tools/CreateDummyFile")]
    public static void CreateDummyFile()
    {
        var streamingPath = Application.streamingAssetsPath;
        if(!Directory.Exists(streamingPath))
        {
            Directory.CreateDirectory(streamingPath);
        }
        CreateFloatDummyFile(Path.Combine(streamingPath, "float.dat"), 0);
        CreateFloatDummyFile(Path.Combine(streamingPath, "float_offset1.dat"), 1);

    }
    private  unsafe static void CreateFloatDummyFile(string path,int offset)
    {
        using (Stream fs = File.OpenWrite(path))
        {
            for(int i = 0; i < offset; ++i)
            {
                fs.WriteByte((byte)i);
            }
            var buffer = new byte[1024];

            for (int i = 0; i < 1024 * 16; ++i)
            {
                for (int j = 0; j < 1024 / 4; ++j)
                {
                    float random = UnityEngine.Random.Range(-200, 200);
                    fixed (byte* ptr = &buffer[j * 4])
                    {
                        float* pFloat = (float*)ptr;
                        *pFloat = random;
                    }
                }
                fs.Write(buffer, 0, 1024);
            }
            fs.Close();
        }
    }
}
