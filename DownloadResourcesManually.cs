using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Globalization;
using System.Security.Cryptography;

public class DownloadResourcesManually: EditorWindow
{
    string packagePath;

    Rect rect;  

    [MenuItem("Tools/DownloadResourcesManually")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DownloadResourcesManually));
    }

    private void Awake()
    {
        downloads= RefreshList();
    }




    List<Download> downloads;
    Download nowUse = null;
    void OnGUI()
    {
        if (GUI.Button(new Rect(30, 30, 100, 50), "Refresh"))
        {
            downloads = RefreshList();
        }

        int y = 80, padding = 10,ySize=30;

        foreach (var item in downloads)
        {
            var i = item;
            y += ySize + padding;
            if (GUI.Button(new Rect(30, y, 300, ySize), "Choose:"+item.name))
            {
                nowUse = i;
            }
        }

        if(nowUse!=null)
        {
            y += ySize + padding;
            EditorGUI.LabelField(new Rect(30, y, 100, 20), "name:");
            EditorGUI.SelectableLabel(new Rect(140, y, 500, 30), nowUse.name);

            y += 20 + padding;


            EditorGUI.LabelField(new Rect(30, y, 100, 20), "url:");
            EditorGUI.SelectableLabel(new Rect(140, y, 500, 30), nowUse.download.url);

            y += 20+padding;

            EditorGUI.LabelField(new Rect(30, y, 100, 20), "package path:");
            packagePath = filePathReceiver(new Rect(140, y, 500, 30), packagePath);

            y+= 20 + padding;

            if (GUI.Button(new Rect(30, y, 100, 50), "Decrypt"))
            {
                DecryptFile(packagePath, packagePath + ".unitypackage", nowUse.download.key);
            }

        }

    }

    string filePathReceiver(Rect rect, string content)
    {
        // //将上面的框作为文本输入框  
        content = EditorGUI.TextField(rect, content);  

        //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
        if ((Event.current.type == EventType.DragUpdated  
            || Event.current.type == EventType.DragExited)  
            && rect.Contains(Event.current.mousePosition))  
        {  
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;  
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)  
            {  
                content = DragAndDrop.paths[0];  
            }  
        }

        return content;
    }



    //methods：



    List<Download> RefreshList()
    {
        string path;
        path = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x";
        Debug.Log(path);

        List<DirectoryInfo> floders = new List<DirectoryInfo>();
        List<FileInfo> files = new List<FileInfo>();
        floders.Add(new DirectoryInfo(path));
        while(floders.Count>0)
        {
            floders.AddRange(floders[0].GetDirectories());
            files.AddRange(floders[0].GetFiles());
            floders.RemoveAt(0);
        }
        
        List<Download> list = new List<Download>();
        foreach (var file in files)
        {
            if (file.Extension==".json")
            {
                string json =File.ReadAllText(file.FullName);
                Download downloadInfo = JsonUtility.FromJson<Download>(json);
                downloadInfo.name = file.Name.Substring(1).Split(new string[] { "-content" }, StringSplitOptions.None)[0];
                list.Add(downloadInfo);
            }
        }

        return list;
    }



    [System.Serializable]
    class Download
    {
        [System.Serializable]
        public class Detail
        {
            public string url;
            public string key;
        }

        public string name = "";
        public Detail download;
    }


     void HexStringToByteArray(string hex, byte[] array, int offset)
    {
        if (offset + array.Length * 2 > hex.Length)
            throw new ArgumentException("Hex string too short");
        for (int index = 0; index < array.Length; ++index)
        {
            string s = hex.Substring(index * 2 + offset, 2);
            array[index] = byte.Parse(s, NumberStyles.HexNumber);
        }
    }

    void DecryptFile(string inputFile, string outputFile, string keyIV)
    {
        byte[] array1 = new byte[32];
        byte[] array2 = new byte[16];
        HexStringToByteArray(keyIV, array1, 0);
        HexStringToByteArray(keyIV, array2, 64);
        EditorUtility.DisplayProgressBar("Decrypting", "Decrypting package", 0.0f);
        FileStream fileStream1 = File.Open(inputFile, System.IO.FileMode.Open);
        FileStream fileStream2 = File.Open(outputFile, System.IO.FileMode.CreateNew);
        long length = fileStream1.Length;
        long num = 0;
        AesManaged aesManaged = new AesManaged();
        aesManaged.Key = array1;
        aesManaged.IV = array2;
        CryptoStream cryptoStream = new CryptoStream((Stream)fileStream1, aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV), CryptoStreamMode.Read);
        try
        {
            byte[] numArray = new byte[40960];
            int count;
            while ((count = cryptoStream.Read(numArray, 0, numArray.Length)) > 0)
            {
                fileStream2.Write(numArray, 0, count);
                num += (long)count;
                if (EditorUtility.DisplayCancelableProgressBar("Decrypting", "Decrypting package", (float)num / (float)length))
                    throw new Exception("User cancelled decryption");
            }
        }
        finally
        {
            cryptoStream.Close();
            fileStream1.Close();
            fileStream2.Close();
            EditorUtility.ClearProgressBar();
        }
    }



















}
