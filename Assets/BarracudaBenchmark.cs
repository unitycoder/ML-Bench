using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Unity.Barracuda;

public class BarracudaBenchmark : MonoBehaviour {

    [Header("Preview")]
    public RawImage rawImage;
    public AspectRatioFitter aspectFitter;
    public Text predictionText;

    [Header("Prediction")]
    public ComputeShader preprocessor;
    public TextAsset labelFile;

    Model model;
    IWorker worker;
    string[] labels;
    WebCamTexture webCamTexture;

    ComputeBuffer inputBuffer;

    async void Start () {
        // Get labels
        labels = labelFile.text.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        // NatML and Barracuda both define importers for `.onnx` files, and Unity rejects both.
        // So instead we load the models from file.
        model = await LoadModelFromStreamingAssets("mobilenetv2-7.bc");
        worker = WorkerFactory.CreateWorker(model, WorkerFactory.Device.Compute);
        // Create input buffer
        inputBuffer = new ComputeBuffer(224 * 224 * 3, sizeof(float));
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
        // Preprocess image
        preprocessor.SetTexture(0, "_Texture", webCamTexture);
        preprocessor.SetBuffer(0, "_Tensor", inputBuffer);
        preprocessor.Dispatch(0, 224 / 8, 224 / 8, 1);
        // Run model
        using (var tensor = new Tensor(1, 224, 224, 3, inputBuffer))
            worker.Execute(tensor);
        var output = worker.CopyOutput();
        var labelIdx = Enumerable.Range(0, output.length).Aggregate((i, j) => output[i] > output[j] ? i : j);
        var label = labels[labelIdx];
        predictionText.text = label;
    }

    void OnDisable () {
        inputBuffer.Dispose();
        worker.Dispose();
    }

    private static async Task<Model> LoadModelFromStreamingAssets (string relativePath) {
        /**
         *  It seems that the folks at Unity forgot about `StreamingAssets` restrictions on Android.
         *  So we have to manually extract the file from the app archive.
         */
        if (Application.platform != RuntimePlatform.Android)
            return ModelLoader.LoadFromStreamingAssets(relativePath);
        var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
        using (var request = UnityWebRequest.Get(fullPath)) {
            // Download from APK/AAB
            request.SendWebRequest();
            while (!request.isDone)
                await Task.Yield();
            if (request.isNetworkError || request.isHttpError)
                throw new ArgumentException($"Failed to load model from path: {fullPath}", nameof(relativePath));
            // Create
            return ModelLoader.Load(request.downloadHandler.data);
        }
    }
}
