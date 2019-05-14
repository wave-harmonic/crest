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
	int guid; // local guid to track this info
	[SerializeField]
	Vector3[] localQueryPositions = new Vector3[0]; // the points in local space
	float[] resultsHeight = new float[0]; // the results

	private void Awake()
	{
		// Find a guid for this sampler
		guid = GetInstanceID();

		//int xLength = 10;
		//int yLength = 10;

		int xLength = 50;
		int yLength = 50;

		this.localQueryPositions = new Vector3[xLength * yLength];
		int index = 0;
		float spacing = 2;

		for(int x = 0; x < xLength; x++)
			for(int y = 0; y < yLength; y++)
				this.localQueryPositions[index++] = new Vector3((float)x / spacing, 0, (float)y / spacing);
	}

	private void OnDisable()
	{
		ShapeGerstnerJobs.RemoveQueryPoints(guid);
	}

	private void UpdatePoints(Vector3[] points)
	{
		if(localQueryPositions.Length != points.Length)
		{
			this.localQueryPositions = new Vector3[points.Length];
			this.resultsHeight = new float[points.Length];
		}	

		System.Array.Copy(points, this.localQueryPositions, points.Length);
	}

	private void Update()
	{
		if(localQueryPositions.Length != this.resultsHeight.Length)
		{
			this.resultsHeight = new float[localQueryPositions.Length];
		}

		Matrix4x4 boatMatrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);

		ShapeGerstnerJobs.CompleteJobs();
		ShapeGerstnerJobs.RetrieveResultHeights(guid, ref resultsHeight);

		for(int i = 0; i < resultsHeight.Length; i++)
		{
			Vector3 point = boatMatrix.MultiplyPoint3x4(localQueryPositions[i]);
			point.y = resultsHeight[i];

			Utility.Draw3DCross(point, Color.magenta, 0.25f);
		}

		// Schedule the next rounds jobs since the data is a frame old
		ShapeGerstnerJobs.UpdateQueryPoints(guid, boatMatrix, localQueryPositions);
	}

}
#endif