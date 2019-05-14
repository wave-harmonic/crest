#define USE_JOBS

#if USE_JOBS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;
using Unity.Jobs;
using Unity.Mathematics;

public class CrestWaterSampler : MonoBehaviour
{
	// Public fields
	[SerializeField]
	float _minSpatialLength = 12f;
	
	/// <summary> Locally offset points from this GameObject </summary>
	public Vector3[] _forcePoints = new Vector3[0];

	Rect _localSamplingAABB; // The sampling area
	
	SamplingData _samplingData = new SamplingData();
	SamplingData _samplingDataFlow = new SamplingData();

	public Vector3 DisplacementToBoat { get; set; }

	int _guid; // local guid to track this info
	float3[] _queryPositions; // all the query positions in local space (I think)
	float[] _resultHeights; // the results

	#region Unity Functions

	private void Awake()
	{
		// Find a guid for this sampler
		_guid = GetInstanceID();

		int xLength = 50;
		int yLength = 50;

		this._forcePoints = new Vector3[xLength * yLength];
		int index = 0;
		float spacing = 2;

		for(int x = 0; x < xLength; x++)
		{
			for(int y = 0; y < yLength; y++)
			{
				this._forcePoints[index++] = new Vector3((float)x / spacing, 0, (float)y / spacing);
			}
		}

	}
	
	private void OnDisable()
	{
		ShapeGerstnerJobs.RemoveQueryPoints(_guid);
	}

	#endregion

	Rect GetWorldAABB()
	{
		Bounds b = new Bounds(transform.position, Vector3.one);
		b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMin, 0f, _localSamplingAABB.yMin)));
		b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMin, 0f, _localSamplingAABB.yMax)));
		b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMax, 0f, _localSamplingAABB.yMin)));
		b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMax, 0f, _localSamplingAABB.yMax)));
		return Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
	}

	Rect ComputeLocalSamplingAABB()
	{
		if(_forcePoints.Length == 0)
			return new Rect();

		float xmin = _forcePoints[0].x;
		float zmin = _forcePoints[0].z;
		float xmax = xmin, zmax = zmin;
		for(int i = 1; i < _forcePoints.Length; i++)
		{
			float x = _forcePoints[i].x, z = _forcePoints[i].z;
			xmin = Mathf.Min(xmin, x); xmax = Mathf.Max(xmax, x);
			zmin = Mathf.Min(zmin, z); zmax = Mathf.Max(zmax, z);
		}

		return Rect.MinMaxRect(xmin, zmin, xmax, zmax);
	}

	#region Job Queries
	
	private void Update()
	{
		EnsureJobDataAllocated();
		ShapeGerstnerJobs.CompleteJobs();
		ShapeGerstnerJobs.RetrieveResultHeights(_guid, ref _resultHeights);

		for(int i = 0; i < _resultHeights.Length; i++)
		{
			Vector3 point = new Vector3 (_queryPositions[i].x, _resultHeights[i], _queryPositions[i].z);

			Utility.Draw3DCross(point, Color.magenta, 0.25f);
		}

		// Schedule the next rounds jobs since the data is a frame old
		this.ScheduleHeightReadback();

		//// Trigger processing of displacement textures that have come back this frame. This will be processed
		//// anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
		//if(OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.OceanDisplacementTexturesGPU && GPUReadbackDisps.Instance)
		//{
		//	GPUReadbackDisps.Instance.ProcessRequests();
		//}

		//var collProvider = OceanRenderer.Instance.CollisionProvider;
		//var thisRect = GetWorldAABB();
		//if(!collProvider.GetSamplingData(ref thisRect, _minSpatialLength, _samplingData))
		//{
		//	// No collision coverage for the sample area, in this case use the null provider.
		//	collProvider = CollProviderNull.Instance;
		//}

		//var position = transform.position;

		//Vector3 undispPos;
		//if(!collProvider.ComputeUndisplacedPosition(ref position, _samplingData, out undispPos))
		//{
		//	// If we couldn't get wave shape, assume flat water at sea level
		//	undispPos = position;
		//	undispPos.y = OceanRenderer.Instance.SeaLevel;
		//}

		//Vector3 displacement, waterSurfaceVel;
		//bool dispValid, velValid;
		//collProvider.SampleDisplacementVel(ref undispPos, _samplingData, out displacement, out dispValid, out waterSurfaceVel, out velValid);
		//if(dispValid)
		//{
		//	DisplacementToBoat = displacement;
		//}

		//if(GPUReadbackFlow.Instance)
		//{
		//	GPUReadbackFlow.Instance.ProcessRequests();

		//	var flowRect = new Rect(position.x, position.z, 0f, 0f);
		//	if(GPUReadbackFlow.Instance.GetSamplingData(ref flowRect, _minSpatialLength, _samplingDataFlow))
		//	{
		//		Vector2 surfaceFlow;
		//		GPUReadbackFlow.Instance.SampleFlow(ref position, _samplingDataFlow, out surfaceFlow);
		//		waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

		//		GPUReadbackFlow.Instance.ReturnSamplingData(_samplingDataFlow);
		//	}
		//}

		//collProvider.ReturnSamplingData(_samplingData);
	}

	void EnsureJobDataAllocated()
	{
		if(_resultHeights == null || _resultHeights.Length != _forcePoints.Length)
		{
			_resultHeights = new float[_forcePoints.Length];

			// Initialise heights to sea level, so it doesnt matter too much if results retrieval below fails
			for(int i = 0; i < _resultHeights.Length; i++) _resultHeights[i] = OceanRenderer.Instance.SeaLevel;
		}

		if(_queryPositions == null || _queryPositions.Length != _forcePoints.Length)
		{
			_queryPositions = new float3[_forcePoints.Length];

			// Give them defaults
			UpdateJobQueryPositions();
		}
	}


	void ScheduleHeightReadback()
	{
		EnsureJobDataAllocated();

		UpdateJobQueryPositions();

		ShapeGerstnerJobs.UpdateQueryPoints(_guid, _queryPositions);
	}

	void UpdateJobQueryPositions()
	{
		for(var i = 0; i < _forcePoints.Length; i++)
		{
			_queryPositions[i] = transform.TransformPoint(_forcePoints[i]);
		}
	}

	#endregion
}
#endif