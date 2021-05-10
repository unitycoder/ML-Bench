using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using NatSuite.ML;
using NatSuite.ML.Vision;

public class NatMLBenchmark : MonoBehaviour {

    [Header("Preview")]
    public RawImage rawImage;
    public AspectRatioFitter aspectFitter;
    
    [Header("Prediction")]
    public TextAsset labelFile;

    MLModel model;
    MLClassificationPredictor predictor;

    async void Start () {
        // Get deps
        var modelPath = await StreamingAssetsToAbsolute("mobilenet_v2.onnx");
        var labels = labelFile.text.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        // Create model and predictor
        model = new MLModel(modelPath);
        predictor = new MLClassificationPredictor(model, labels);
    }

    void Update () {
        
    }

    /**
     *  NatML and Barracuda both define importers for `.onnx` files, and Unity rejects both.
     *  So instead we load the models from file.
     */
    private static async Task<string> StreamingAssetsToAbsolute (string relativePath) {
        // Get absolute path
        var absolutePath = Path.Combine(Application.streamingAssetsPath, relativePath);
        var persistentPath = Path.Combine(Application.persistentDataPath, relativePath);
        if (Application.platform != RuntimePlatform.Android)
            return absolutePath;
        // Check persistent storage
        if (File.Exists(persistentPath))
            return persistentPath;
        // Download from APK/AAB
        var request = UnityWebRequest.Get(absolutePath);
        request.SendWebRequest();
        while (!request.isDone)
            await Task.Yield();
        // Copy to persistent storage
        new FileInfo(persistentPath).Directory.Create();
        File.WriteAllBytes(persistentPath, request.downloadHandler.data);
        return persistentPath;
    }
}
