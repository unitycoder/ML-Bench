using System;
using UnityEngine;
using UnityEngine.UI;
using NatSuite.ML;
using NatSuite.ML.Features;
using NatSuite.ML.Vision;

public class NatMLBenchmark : MonoBehaviour {

    [Header("Preview")]
    public RawImage rawImage;
    public AspectRatioFitter aspectFitter;
    public Text predictionText;
    
    [Header("Prediction")]
    public TextAsset labelFile;

    MLModel model;
    MLClassificationPredictor predictor;
    WebCamTexture webCamTexture;
    Color32[] pixelBuffer;

    async void Start () {
        // NatML and Barracuda both define importers for `.onnx` files, and Unity rejects both.
        // So instead we load the models from file.
        var modelData = await MLModelData.FromStreamingAssets("mobilenetv2-7.onnx");
        var labels = labelFile.text.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        // Create model and predictor
        model = modelData.Deserialize();
        predictor = new MLClassificationPredictor(model, labels);
        // Start the camera preview
        webCamTexture = new WebCamTexture(1280, 720, 30);
        webCamTexture.Play();
        // Display the camera preview
        rawImage.texture = webCamTexture;
    }

    void Update () {
        // Check that webcam is up
        if (!webCamTexture || !webCamTexture.isPlaying || webCamTexture.width == 16 || webCamTexture.height == 16)
            return;
        aspectFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
        // Get pixels
        pixelBuffer = pixelBuffer ?? webCamTexture.GetPixels32();
        webCamTexture.GetPixels32(pixelBuffer);
        // Create feature
        var input = new MLImageFeature(pixelBuffer, webCamTexture.width, webCamTexture.height);
        input.mean = new Vector3(0.485f, 0.456f, 0.406f);
        input.std = new Vector3(0.229f, 0.224f, 0.225f);
        input.aspectMode = MLImageFeature.AspectMode.AspectFill;
        // Predict
        var (label, confidence) = predictor.Predict(input);
        predictionText.text = label;
    }

    void OnDisable () {
        // Dispose the model
        model.Dispose();
    }
}
