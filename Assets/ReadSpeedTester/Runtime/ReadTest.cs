using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ReadTest : MonoBehaviour
{
    public UnityEngine.UI.Text txt;

    IEnumerator Start()
    {
        yield return null;

#if UNITY_ANDROID && !UNITY_EDITOR
        WWW w1 = new WWW(Path.Combine(Application.streamingAssetsPath, "float.dat"));
        WWW w2 = new WWW(Path.Combine(Application.streamingAssetsPath, "float_offset1.dat"));
        while(!w1.isDone || !w2.isDone)
        {
            yield return null;
        }
        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "float.dat"), w1.bytes);
        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "float_offset1.dat"), w2.bytes);
#endif
    }

    public void Test()
    {
        string basePath = Application.streamingAssetsPath;
#if UNITY_ANDROID && !UNITY_EDITOR
        basePath = Application.persistentDataPath;

#endif
        string floatFile = Path.Combine(basePath, "float.dat");
        string floatOffsetFile = Path.Combine(basePath, "float_offset1.dat");


        ExecPerfTest("\nNoBuffer", () => { ReadFloatFileWithPtr(floatFile); });
        ExecPerfTest("NoBufferWithBitConvert", () => { ReadFloatFileWithBitConverter(floatFile); });
        ExecPerfTest("BufferStream", () => { ReadFloatFileWithBufferPtr(floatFile,512); });

        ExecPerfTest("\nWithPtr&Buffer512", () => { ReadFloatFileWithPtrAndBufferCoppy(floatFile,512,0); });
        ExecPerfTest("PtrCopy512", () => { ReadFloatFileWithPtrAndBufferCopyPtr(floatFile, 512,0); });
        ExecPerfTest("NoCopy512", () => { ReadFloatFileWithPtrAndNoCopy(floatFile, 512,0); });
        ExecPerfTest("BitConverter512", () => { ReadFloatFileCS(floatFile, 512,0); });
        ExecPerfTest("NoCopy1024", () => { ReadFloatFileWithPtrAndNoCopy(floatFile, 1024,0); });
        //
        ExecPerfTest("\nOffset1_PtrCopy512", () => { ReadFloatFileWithPtrAndBufferCopyPtr(floatOffsetFile, 512, 1); });
        ExecPerfTest("Offset1_NoCopy512", () => { ReadFloatFileWithPtrAndNoCopy(floatOffsetFile, 512, 1); });
        ExecPerfTest("Offset1_BitConverter512", () => { ReadFloatFileCS(floatOffsetFile, 512, 1); });
    }

    public void ExecPerfTest(string name,System.Action exec)
    {
#if ENABLE_IL2CPP
#endif
        float start = Time.realtimeSinceStartup;
        exec();
        float execTime = Time.realtimeSinceStartup - start;
        this.txt.text += name + "::" + execTime + "\n";
    }
    private unsafe void ReadFloatFileWithBufferPtr(string path,int bufsize)
    {
        var buffer = new byte[4];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            int loopNum = (int)(fs.Length / 4);
            BufferedStream bufferedStream = new BufferedStream(fs,bufsize);
            for (int i = 0; i < loopNum; ++i)
            {
                bufferedStream.Read(buffer, 0, 4);
                fixed (byte* ptr = &buffer[0])
                {
                    float* fp = (float*)ptr;
                    tmp = *fp;
                }
            }
            bufferedStream.Close();
        }
    }


    private unsafe  void ReadFloatFileWithPtr(string path)
    {
        var buffer = new byte[4];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            int loopNum = (int)(fs.Length / 4);
            for (int i = 0; i < loopNum; ++i)
            {
                fs.Read(buffer, 0, 4);
                fixed (byte* ptr = &buffer[0])
                {
                    float* fp = (float*)ptr;
                    tmp = *fp;
                }
            }
        }
    }

    private unsafe void ReadFloatFileWithBitConverter(string path)
    {
        var buffer = new byte[4];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            int loopNum = (int)(fs.Length / 4);
            for (int i = 0; i < loopNum; ++i)
            {
                fs.Read(buffer, 0, 4);
                tmp = System.BitConverter.ToSingle(buffer, 0);
            }
        }
    }

    // bufferにコピーするのはByteアライン対応をしたいという意図からです
    private unsafe void ReadFloatFileWithPtrAndBufferCoppy(string path,int bufferSize,int offset)
    {
        var buffer = new byte[bufferSize];
        var copyBuffer = new byte[4];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            for (int i = 0; i < offset; ++i) { fs.ReadByte(); }
            fixed (byte* bufPtr = &copyBuffer[0])
            {

                int loopNum = (int)((fs.Length + bufferSize - 1) / bufferSize);
                for (int i = 0; i < loopNum; ++i)
                {
                    int readSize = fs.Read(buffer, 0, bufferSize);
                    int bufLoop = readSize / 4;

                    for (int j = 0; j < bufLoop; ++j)
                    {
                        {
                            for (int k = 0; k < 4; ++k)
                            {
                                copyBuffer[k] = buffer[j + k];
                            }
                            float* fp = (float*)bufPtr;
                            tmp = *fp;
                        }
                    }
                }
            }
        }
    }

    private unsafe void ReadFloatFileWithPtrAndBufferCopyPtr(string path, int bufferSize,int offset)
    {
        var buffer = new byte[bufferSize];
        var copyBuffer = new byte[4];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            for (int i = 0; i < offset; ++i) { fs.ReadByte(); }
            fixed (byte* bufPtr = &copyBuffer[0])
            {

                int loopNum = (int)((fs.Length + bufferSize - 1) / bufferSize);
                for (int i = 0; i < loopNum; ++i)
                {
                    int readSize = fs.Read(buffer, 0, bufferSize);
                    int bufLoop = readSize / 4;

                    for (int j = 0; j < bufLoop; ++j)
                    {
                        {
                            byte* wPtr = bufPtr;
                            fixed (byte* bufferPtr = &buffer[j])
                            {
                                byte* rPtr = bufferPtr;

                                for (int k = 0; k < 4; ++k)
                                {
                                    *wPtr = *rPtr;
                                    ++rPtr;++wPtr;
                                }
                            }
                            float* fp = (float*)bufPtr;
                            tmp = *fp;
                        }
                    }
                }
            }
        }
    }





    private unsafe void ReadFloatFileCS(string path, int bufferSize,int offset)
    {
        var buffer = new byte[bufferSize];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {

            for (int i = 0; i < offset; ++i) { fs.ReadByte(); }
            int loopNum = (int)((fs.Length + bufferSize - 1) / bufferSize);
            for (int i = 0; i < loopNum; ++i)
            {
                int readSize = fs.Read(buffer, 0, bufferSize);
                int bufLoop = readSize / 4;

                for (int j = 0; j < bufLoop; ++j)
                {
                    tmp = System.BitConverter.ToSingle(buffer, j * 4);
                }
            }
        
        }
    }


    private unsafe void ReadFloatFileWithPtrAndNoCopy(string path, int bufferSize,int offset)
    {
        var buffer = new byte[bufferSize];
        float tmp = 0.0f;
        using (Stream fs = File.OpenRead(path))
        {
            for (int i = 0; i < offset; ++i) { fs.ReadByte(); }
            int loopNum = (int)((fs.Length + bufferSize - 1) / bufferSize);
            for (int i = 0; i < loopNum; ++i)
            {
                int readSize = fs.Read(buffer, 0, bufferSize);
                int bufLoop = readSize / 4;

                for (int j = 0; j < bufLoop; ++j)
                {
                    fixed (byte* ptr = &buffer[j * 4])
                    {
                        float* fp = (float*)ptr;
                        tmp = *fp;
                    }
                }
            }

        }
    }
}
