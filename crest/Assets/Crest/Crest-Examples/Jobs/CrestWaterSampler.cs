#define USE_JOBS

#if USE_JOBS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public class CrestWaterSampler : MonoBehaviour
{
	public bool ShowDebug = false;

	public Vector3[] LocalQueryPositions = new Vector3[0]; // the points in local space
	public float[] ResultsHeight = new float[0]; // the results

	int guid; // local guid to track this info

	private void Awake()
	{
		// Find a guid for this sampler
		guid = GetInstanceID();

		FillQueriesWithTestData();
	}

	private void FillQueriesWithTestData(float spacing = 0.2f)
	{
		int xLength = 32;
		int zLength = 32;

		LocalQueryPositions = new Vector3[xLength * zLength];
		int index = 0;
		for(int x = 0; x < xLength; x++)
			for(int z = 0; z < zLength; z++)
				LocalQueryPositions[index++] = new Vector3((float)x * spacing, 0, (float)z * spacing);
	}

	private void OnDisable()
	{
		ShapeGerstnerJobs.RemoveQueryPoints(guid);
	}

	public void UpdatePoints(Vector3[] points)
	{
		if(LocalQueryPositions.Length != points.Length)
		{
			this.LocalQueryPositions = new Vector3[points.Length];
			this.ResultsHeight = new float[points.Length];

			// Fill the results height with the sea level as a base level
			for(int i = 0; i < this.ResultsHeight.Length; i++)
				this.ResultsHeight[i] = OceanRenderer.Instance.SeaLevel;
		}

		System.Array.Copy(points, this.LocalQueryPositions, points.Length);
	}

	private void Update()
	{
		if(LocalQueryPositions.Length != this.ResultsHeight.Length)
			this.ResultsHeight = new float[LocalQueryPositions.Length];

		//Matrix4x4 boatMatrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);

		ShapeGerstnerJobs.CompleteJobs();
		ShapeGerstnerJobs.RetrieveResultHeights(guid, ref ResultsHeight);

		if(ShowDebug)
		{
			for(int i = 0; i < ResultsHeight.Length; i++)
			{
				Vector3 point = this.transform.TransformPoint(LocalQueryPositions[i]);
				point.y = ResultsHeight[i];

				Utility.Draw3DCross(point, Color.magenta, 0.25f);
			}
		}

		// Schedule the next rounds jobs since the data is a frame old
		ShapeGerstnerJobs.UpdateQueryPoints(guid, this.transform, LocalQueryPositions);
	}
}
#endif