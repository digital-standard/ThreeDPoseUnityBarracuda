using UnityEngine;
using UnityEngine.UI;
using Barracuda;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Define Joint points
/// </summary>
public class VNectBarracudaRunner : MonoBehaviour
{
    /// <summary>
    /// Neural network model
    /// </summary>
    public NNModel NNModel;

    public BarracudaWorkerFactory.Type WorkerType = BarracudaWorkerFactory.Type.ComputePrecompiled;
    public bool Verbose = true;

    public VNectModel VNectModel;

    public VideoCapture videoCapture;

    private Model _model;
    private IWorker _worker;

    /// <summary>
    /// Coordinates of joint points
    /// </summary>
    private VNectModel.JointPoint[] jointPoints;
    
    /// <summary>
    /// Number of joint points
    /// </summary>
    private const int JointNum = 24;

    /// <summary>
    /// input image size
    /// </summary>
    public int InputImageSize;

    /// <summary>
    /// column number of heatmap
    /// </summary>
    public int HeatMapCol;

    /// <summary>
    /// Column number of heatmap in 2D image
    /// </summary>
    private int HeatMapCol_Squared;
    
    /// <summary>
    /// Column nuber of heatmap in 3D model
    /// </summary>
    private int HeatMapCol_Cube;
    private float ImageScale;

    /// <summary>
    /// Buffer memory has 2D heat map
    /// </summary>
    private float[] heatMap2D;

    /// <summary>
    /// Buffer memory has offset 2D
    /// </summary>
    private float[] offset2D;
    
    /// <summary>
    /// Buffer memory has 3D heat map
    /// </summary>
    private float[] heatMap3D;
    
    /// <summary>
    /// Buffer memory hash 3D offset
    /// </summary>
    private float[] offset3D;
    private float unit;
    
    /// <summary>
    /// Number of joints in 2D image
    /// </summary>
    private int JointNum_Squared = JointNum * 2;
    
    /// <summary>
    /// Number of joints in 3D model
    /// </summary>
    private int JointNum_Cube = JointNum * 3;

    /// <summary>
    /// Lock to update VNectModel
    /// </summary>
    private bool Lock = true;

    /// <summary>
    /// For low pass filter
    /// </summary>
    public float Smooth;

    private void Start()
    {
        // Initialize 
        HeatMapCol_Squared = HeatMapCol * HeatMapCol;
        HeatMapCol_Cube = HeatMapCol * HeatMapCol * HeatMapCol;
        heatMap2D = new float[JointNum * HeatMapCol_Squared];
        offset2D = new float[JointNum * HeatMapCol_Squared * 2];
        heatMap3D = new float[JointNum * HeatMapCol_Cube];
        offset3D = new float[JointNum * HeatMapCol_Cube * 3];
        ImageScale = 224f / (float)InputImageSize;
        unit = 1f / (float)HeatMapCol;

        // Disabel sleep
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Init model
        _model = ModelLoader.Load(NNModel, Verbose);
        _worker = BarracudaWorkerFactory.CreateWorker(WorkerType, _model, Verbose);
        StartCoroutine("WaitLoad");

        // Init VNect model
        jointPoints = VNectModel.Init();
        // Init VideoCapture
        videoCapture.Init(InputImageSize, InputImageSize);

    }

    private void Update()
    {
        if (!Lock) UpdateVNectModel();
    }

    private IEnumerator WaitLoad()
    {
        yield return new WaitForSeconds(0.5f);
        Lock = false;
    }

    private void UpdateVNectModel()
    {
        input = new Tensor(videoCapture.MainTexture);

        if (inputs["1"] == null)
        {
            inputs["0"] = input;
            inputs["1"] = input;
            inputs["2"] = input;
        }
        else
        {
            inputs["2"].Dispose();
            inputs["2"] = inputs["1"];
            inputs["1"] = inputs["0"];
            inputs["0"] = input;
        }

        StartCoroutine(ExecuteModel());
    }

    /// <summary>
    /// Tensor has input image
    /// </summary>
    /// <returns></returns>
    Tensor input = new Tensor();
    Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>() { { "0", null }, { "1", null }, { "2", null }, };
    Tensor[] b_outputs = new Tensor[4];

    private IEnumerator ExecuteModel()
    {
        // Create input and Execute model
        yield return _worker.ExecuteAsync(inputs);

        // Get outputs
        for (var i = 2; i < _model.outputs.Count; i++)
        {
            b_outputs[i] = _worker.Peek(_model.outputs[i]);
        }

        // Get data from outputs
        offset3D = b_outputs[2].data.Download(b_outputs[2].data.GetMaxCount());
        heatMap3D = b_outputs[3].data.Download(b_outputs[3].data.GetMaxCount());
        /**/
        // Release outputs
        for (var i = 2; i < b_outputs.Length; i++)
        {
            b_outputs[i].Dispose();
        }

        PredictPose();
    }

    /// <summary>
    /// Predict positions of each of joints based on network
    /// </summary>
    private void PredictPose()
    {
        var cubeOffsetLinear = HeatMapCol * JointNum_Cube;
        var cubeOffsetSquared = HeatMapCol_Squared * JointNum_Cube;
        for (var j = 0; j < JointNum; j++)
        {
            var maxXIndex = 0;
            var maxYIndex = 0;
            var maxZIndex = 0;
            jointPoints[j].score3D = 0.0f;
            for (var z = 0; z < HeatMapCol; z++)
            {
                for (var y = 0; y < HeatMapCol; y++)
                {
                    for (var x = 0; x < HeatMapCol; x++)
                    {
                        float v = heatMap3D[y * HeatMapCol_Squared * JointNum + x * HeatMapCol * JointNum + j * HeatMapCol + z];
                        if (v > jointPoints[j].score3D)
                        {
                            jointPoints[j].score3D = v;
                            maxXIndex = x;
                            maxYIndex = y;
                            maxZIndex = z;
                        }
                    }
                }
            }
           
            jointPoints[j].Now3D.x = ((offset3D[maxYIndex * cubeOffsetSquared + maxXIndex * cubeOffsetLinear + j * HeatMapCol + maxZIndex] * 0.5f + 0.5f + (float)maxXIndex) / (float)HeatMapCol) * (float)InputImageSize;
            jointPoints[j].Now3D.y = (float)InputImageSize - ((offset3D[maxYIndex * cubeOffsetSquared + maxXIndex * cubeOffsetLinear + (j + JointNum) * HeatMapCol + maxZIndex] * 0.5f + 0.5f + (float)maxYIndex) / (float)HeatMapCol) * (float)InputImageSize;
            jointPoints[j].Now3D.z = ((offset3D[maxYIndex * cubeOffsetSquared + maxXIndex * cubeOffsetLinear + (j + JointNum_Squared) * HeatMapCol + maxZIndex] * 0.5f + 0.5f + (float)(maxZIndex - 14)) / (float)HeatMapCol) * (float)InputImageSize;
        }

        // Calculate hip location
        var lc = (jointPoints[PositionIndex.rThighBend.Int()].Now3D + jointPoints[PositionIndex.lThighBend.Int()].Now3D) / 2f;
        jointPoints[PositionIndex.hip.Int()].Now3D = (jointPoints[PositionIndex.abdomenUpper.Int()].Now3D + lc) / 2f;

        // Calculate neck location
        jointPoints[PositionIndex.neck.Int()].Now3D = (jointPoints[PositionIndex.rShldrBend.Int()].Now3D + jointPoints[PositionIndex.lShldrBend.Int()].Now3D) / 2f;

        // Calculate head location
        var cEar = (jointPoints[PositionIndex.rEar.Int()].Now3D + jointPoints[PositionIndex.lEar.Int()].Now3D) / 2f;
        var hv = cEar - jointPoints[PositionIndex.neck.Int()].Now3D;
        var nhv = Vector3.Normalize(hv);
        var nv = jointPoints[PositionIndex.Nose.Int()].Now3D - jointPoints[PositionIndex.neck.Int()].Now3D;
        jointPoints[PositionIndex.head.Int()].Now3D = jointPoints[PositionIndex.neck.Int()].Now3D + nhv * Vector3.Dot(nhv, nv);

        // Calculate spine location
        jointPoints[PositionIndex.spine.Int()].Now3D = jointPoints[PositionIndex.abdomenUpper.Int()].Now3D;

        // Low pass filter
        foreach (var jp in jointPoints)
        {
            jp.Now3D *= ImageScale;

            jp.Pos3D = jp.PrevPos3D * Smooth + jp.Now3D * (1f - Smooth);
            jp.PrevPos3D = jp.Pos3D;
            KalmanUpdate(jp);
        }
        VNectModel.IsPoseUpdate = true;
    }

    /// <summary>
    /// Kalman filter
    /// </summary>
    /// <param name="measurement">joint points</param>
    void KalmanUpdate(VNectModel.JointPoint measurement)
    {
        measurementUpdate(measurement);
        measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
        measurement.X = measurement.Pos3D;
    }

	void measurementUpdate(VNectModel.JointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + VNectModel.JointPoint.Q) / (measurement.P.x + VNectModel.JointPoint.Q + VNectModel.JointPoint.R);
        measurement.K.y = (measurement.P.y + VNectModel.JointPoint.Q) / (measurement.P.y + VNectModel.JointPoint.Q + VNectModel.JointPoint.R);
        measurement.K.z = (measurement.P.z + VNectModel.JointPoint.Q) / (measurement.P.z + VNectModel.JointPoint.Q + VNectModel.JointPoint.R);
        measurement.P.x = VNectModel.JointPoint.R * (measurement.P.x + VNectModel.JointPoint.Q) / (VNectModel.JointPoint.R + measurement.P.x + VNectModel.JointPoint.Q);
        measurement.P.y = VNectModel.JointPoint.R * (measurement.P.y + VNectModel.JointPoint.Q) / (VNectModel.JointPoint.R + measurement.P.y + VNectModel.JointPoint.Q);
        measurement.P.z = VNectModel.JointPoint.R * (measurement.P.z + VNectModel.JointPoint.Q) / (VNectModel.JointPoint.R + measurement.P.z + VNectModel.JointPoint.Q);
    }
}
