//#define USE_JOBS

//#if USE_JOBS
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Crest;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Collections;

//public class OldCrestWaterSampler : MonoBehaviour
//{
//	// Public fields
//	[SerializeField]
//	float _minSpatialLength = 12f;
	
//	/// <summary> Locally offset points from this GameObject </summary>
//	public Vector3[] _forcePoints = new Vector3[0];

//	Rect _localSamplingAABB; // The sampling area
	
//	SamplingData _samplingData = new SamplingData();
//	SamplingData _samplingDataFlow = new SamplingData();

//	public Vector3 DisplacementToBoat { get; set; }

//	int _guid; // local guid to track this info
//	float3[] _queryPositions; // all the query positions in local space (I think)
//	float[] _resultHeights; // the results

//	#region Unity Functions

//	private void Awake()
//	{
//		// Find a guid for this sampler
//		_guid = GetInstanceID();

//		int xLength = 50;
//		int yLength = 50;

//		this._forcePoints = new Vector3[xLength * yLength];
//		int index = 0;
//		spacing = 2;

//		for(int x = 0; x < xLength; x++)
//		{
//			for(int y = 0; y < yLength; y++)
//			{
//				this._forcePoints[index++] = new Vector3((float)x / spacing, 0, (float)y / spacing);
//			}
//		}
//	}

//	float spacing = 2 - 0.1f;

//	//private void OverLoadSystem()
//	//{
//	//	int xLength = 50;
//	//	int yLength = 50;

//	//	spacing -= 0.001f;

//	//	int index = 0;

//	//	for(int x = 0; x < xLength; x++)
//	//	{
//	//		for(int y = 0; y < yLength; y++)
//	//		{
//	//			this._forcePoints[index++] = new Vector3((float)x / spacing, 0, (float)y / spacing);
//	//		}
//	//	}

//	//	UpdateJobQueryPositions();
//	//}
	
//	private void OnDisable()
//	{
//		ShapeGerstnerJobs.RemoveQueryPoints(_guid);

//		if(this.allSamplePoints.IsCreated) this.allSamplePoints.Dispose();
//	}

//	#endregion



//	#region Job Queries

//	int frames = 0;

//	public NativeArray<Vector3> allSamplePoints;
//	bool _updatePoints;

//	public void UpdateSamplePoints(NativeArray<Vector3> newPoints)
//	{
//		//if(this.allSamplePoints.IsCreated) this.allSamplePoints.Dispose();
//		//allSamplePoints = new NativeArray<Vector3>(newPoints.le)
//	}

//	private void Update()
//	{
//		if(_updatePoints)
//		{

//		}


//		EnsureJobDataAllocated();
//		ShapeGerstnerJobs.CompleteJobs();
//		ShapeGerstnerJobs.RetrieveResultHeights(_guid, ref _resultHeights);

//		for(int i = 0; i < _resultHeights.Length; i++)
//		{
//			Vector3 point = new Vector3 (_queryPositions[i].x, _resultHeights[i], _queryPositions[i].z);

//			Utility.Draw3DCross(point, Color.magenta, 0.25f);
//		}

//		// Schedule the next rounds jobs since the data is a frame old
//		this.ScheduleHeightReadback();

//	}

//	void EnsureJobDataAllocated()
//	{
//		if(_resultHeights == null || _resultHeights.Length != _forcePoints.Length)
//		{
//			_resultHeights = new float[_forcePoints.Length];

//			// Initialise heights to sea level, so it doesnt matter too much if results retrieval below fails
//			for(int i = 0; i < _resultHeights.Length; i++) _resultHeights[i] = OceanRenderer.Instance.SeaLevel;
//		}

//		if(_queryPositions == null || _queryPositions.Length != _forcePoints.Length)
//		{
//			_queryPositions = new float3[_forcePoints.Length];

//			// Give them defaults
//			UpdateJobQueryPositions();
//		}
//	}


//	void ScheduleHeightReadback()
//	{
//		EnsureJobDataAllocated();

//		UpdateJobQueryPositions();

//		ShapeGerstnerJobs.UpdateQueryPoints(_guid, _queryPositions);
//	}

//	void UpdateJobQueryPositions()
//	{
//		for(var i = 0; i < _forcePoints.Length; i++)
//		{
//			_queryPositions[i] = transform.TransformPoint(_forcePoints[i]);
//		}
//	}

//	#endregion
//}
//#endif