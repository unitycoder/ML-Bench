using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

public class BarracudaBenchmark : MonoBehaviour {

    [Header("Preview")]
    public RawImage rawImage;
    public AspectRatioFitter aspectFitter;

    [Header("Prediction")]
    public TextAsset labelFile;

    Model model;
    IWorker worker;
    string[] labels;

    void Start () {
        // Get labels
        labels = labelFile.text.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        // Create model
        model = ModelLoader.LoadFromStreamingAssets("mobilenet_v2.onnx");
        worker = WorkerFactory.CreateWorker(model, WorkerFactory.Device.Compute);
    }

    void Update () {
        
    }
}
