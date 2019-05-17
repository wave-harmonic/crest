// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// A potential optimisation in the future would be to allocate scratch space in the job. this isn't supported yet in burst but will be
// https://forum.unity.com/threads/burst-dont-allow-me-to-create-a-nativearray.556105/

namespace Crest
{
	public static class ShapeGerstnerJobs
	{
		// General variables
		public static bool s_initialised = false;
		public static bool s_firstFrame = true;
		public static bool s_jobsRunning = false;

		const int MAX_WAVE_COMPONENTS = 512;

		// Wave data
		static NativeArray<float4> s_waveNumbers;
		static NativeArray<float4> s_amps;
		static NativeArray<float4> s_windDirX;
		static NativeArray<float4> s_windDirZ;
		static NativeArray<float4> s_phases;
		static NativeArray<float4> s_chopAmps;

		static int _waveVecCount = 0;
		static int _waveVecElemIndex = 0;

		const int MAX_QUERIES = 4096;

		static readonly float s_twoPi = Mathf.PI * 2f;

		// Query data for height samples
		static int s_lastQueryIndexHeights = 0;
		static NativeArray<Matrix4x4> s_queryPositionsMatrixes; // a simple list of how to transform the matrixes
		static NativeArray<Vector3> s_localQueryPositions; // the list of local query positions for all the points
		static NativeArray<float2> s_worldQueryPositions; // the world query positions which have been transformed by a job
		static NativeArray<float> s_heightQueryPositions; // the world query positions which have been transformed by a job

		// temp allocations
		static NativeArray<int2> _segments;
		static NativeArray<Matrix4x4> _matrixes;

		// results
		static NativeArray<float> s_resultHeights;

		static JobHandle s_matrixes;
		static JobHandle s_handleHeights;

		// Registries for the sampler IDs
		static Dictionary<int, int2> s_segmentRegistry = new Dictionary<int, int2>();
		static Dictionary<int, Transform> s_transformsRegistry = new Dictionary<int, Transform>();

		// list of Ocean Depth Caches, the float array is a flat representation of the depths and is updated only with a function
		static Dictionary<OceanDepthCache, float[]> depthCachesRegistry = new Dictionary<OceanDepthCache, float[]>();
		
		static NativeArray<int2> c_segmentRegistry;
		static NativeArray<Matrix4x4> c_matrix;
		static NativeArray<int> c_resolution;
		static NativeArray<float> c_size;

		static NativeArray<float> c_depthCaches;


		#region Init

		/// <summary>
		/// Allocate storage. Should be called once - will assert if called while already initialised.
		/// </summary>
		public static void Init()
		{
			Debug.Assert(s_initialised == false);
			if(s_initialised)
				return;

			s_localQueryPositions = new NativeArray<Vector3>(MAX_QUERIES, Allocator.Persistent);
			s_worldQueryPositions = new NativeArray<float2>(MAX_QUERIES, Allocator.Persistent);
			s_heightQueryPositions = new NativeArray<float>(MAX_QUERIES, Allocator.Persistent);
			s_queryPositionsMatrixes = new NativeArray<Matrix4x4>(MAX_QUERIES, Allocator.Persistent);
			s_resultHeights = new NativeArray<float>(MAX_QUERIES, Allocator.Persistent);

			s_waveNumbers = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);
			s_amps = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);
			s_windDirX = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);
			s_windDirZ = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);
			s_phases = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);
			s_chopAmps = new NativeArray<float4>(MAX_WAVE_COMPONENTS / 4, Allocator.Persistent);

			c_segmentRegistry = new NativeArray<int2>(0, Allocator.Persistent);
			c_matrix = new NativeArray<Matrix4x4>(0, Allocator.Persistent);
			c_resolution = new NativeArray<int>(0, Allocator.Persistent);
			c_size = new NativeArray<float>(0, Allocator.Persistent);

			c_depthCaches = new NativeArray<float>(0, Allocator.Persistent);

			s_segmentRegistry.Clear();
			s_lastQueryIndexHeights = 0;

			s_initialised = true;
		}

		static void NextElement()
		{
			_waveVecElemIndex = (_waveVecElemIndex + 1) % 4;
			if(_waveVecElemIndex == 0) _waveVecCount++;
		}

		static void SetArrayFloat4(NativeArray<float4> array, float value)
		{
			// Do the read/write dance as poking in the data directly is not possible
			float4 vec = array[_waveVecCount];
			vec[_waveVecElemIndex] = value;
			array[_waveVecCount] = vec;
		}

		/// <summary>
		/// Call this once before calling AddWaveData().
		/// </summary>
		public static void StartSettingWaveData()
		{
			_waveVecCount = 0;
			_waveVecElemIndex = 0;
		}

		/// <summary>
		/// Set the Gerstner wave data. Will reallocate data if the number of waves changes.
		/// </summary>
		/// <returns>True if all added, false if ran out of space in wave data buffer.</returns>
		public static bool AddWaveData(float[] wavelengths, float[] amps, float[] angles, float[] phases, float[] chopScales, float[] gravityScales, int compPerOctave)
		{
			// How many vectors required to accommodate length numFloats

			// TODO - should add an option to not add all the waves in case we want smoother reading of the wave info

			var numFloats = wavelengths.Length;
			var numVecs = numFloats / 4;
			if(numVecs * 4 < numFloats)
				numVecs++;

			for(var inputi = 0; inputi < numFloats; inputi++)
			{
				if(_waveVecCount >= MAX_WAVE_COMPONENTS / 4) return false;
				if(amps[inputi] <= 0.001f) continue;

				var k = s_twoPi / wavelengths[inputi];
				SetArrayFloat4(s_waveNumbers, k);
				SetArrayFloat4(s_amps, amps[inputi]);

				var octavei = inputi / compPerOctave;
				SetArrayFloat4(s_chopAmps, chopScales[octavei] * amps[inputi]);

				float angle = (OceanRenderer.Instance._windDirectionAngle + angles[inputi]) * Mathf.Deg2Rad;
				SetArrayFloat4(s_windDirX, Mathf.Cos(angle));
				SetArrayFloat4(s_windDirZ, Mathf.Sin(angle));

				var C = Mathf.Sqrt(9.81f * wavelengths[inputi] * gravityScales[octavei] / s_twoPi);
				SetArrayFloat4(s_phases, phases[inputi] + k * C * OceanRenderer.Instance.CurrentTime);

				NextElement();
			}

			return true;
		}

		/// <summary>
		/// Call this after calling AddWaveData().
		/// </summary>
		public static void FinishAddingWaveData()
		{
			// Zero out trailing entries in the last vec
			while(_waveVecElemIndex != 0)
			{
				SetArrayFloat4(s_waveNumbers, 0f);
				SetArrayFloat4(s_amps, 0f);
				SetArrayFloat4(s_windDirX, 0f);
				SetArrayFloat4(s_windDirZ, 0f);
				SetArrayFloat4(s_phases, 0f);
				SetArrayFloat4(s_chopAmps, 1f);

				NextElement();
			}
		}

		#endregion

        /// <summary>
        /// DOES NOT WORK YET, need to find depth from the ocean cache texture
        /// </summary>
		public static void AddNewOceanDepthCache(OceanDepthCache newCache)
		{
            return;

			if(newCache.CacheTexture == null)
				return;

			// Call this so that nothing has issues
			CompleteJobs();

            // Code to read off the texture caches
            // TODO - figure out how to read the depths from the ocean cache
            RenderTexture renderTexture = newCache.CacheTexture;
            RenderTexture activeTexture = RenderTexture.active;

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.R16, false, true);
            Rect rectReadPicture = new Rect(0, 0, renderTexture.width, renderTexture.height);
            RenderTexture.active = renderTexture;

            // Read pixels
            texture.ReadPixels(rectReadPicture, 0, 0);
            texture.Apply();

            RenderTexture.active = activeTexture; // added to avoid errors 

            //// TODO - Need a faster way to do this
            Color[] readMap = texture.GetPixels(0, 0, texture.width, texture.height);
            float[] simpleCache = new float[readMap.Length];

            byte[] bytes;
            bytes = texture.EncodeToPNG();
            //System.IO.File.WriteAllBytes(Application.dataPath + "/texture.png", bytes);

            for(int i = 0; i < readMap.Length; i++)
            {
                float redChannel = readMap[i].r;

                // ????
                // here we need to convert the r channel to the depth in meters. Positive for deeper too.

                simpleCache[i] = 1000;
            }

            if(depthCachesRegistry.ContainsKey(newCache) == false)
                depthCachesRegistry.Add(newCache, simpleCache);
            else
                depthCachesRegistry[newCache] = simpleCache;

            UpdateJobOceanDepthCaches();
		}

		// Regenerates all the caches when a new one is added
		public static void UpdateJobOceanDepthCaches()
		{
			if(c_segmentRegistry.IsCreated) c_segmentRegistry.Dispose();
			if(c_matrix.IsCreated) c_matrix.Dispose();
			if(c_resolution.IsCreated) c_resolution.Dispose();
			if(c_size.IsCreated) c_size.Dispose();
            if(c_depthCaches.IsCreated) c_depthCaches.Dispose();

            // Find the total length of the cache
            int totalLength = 0;
			foreach(var floats in depthCachesRegistry.Values)
				totalLength += floats.Length;

			int totalRegistries = depthCachesRegistry.Count;
			
			c_segmentRegistry = new NativeArray<int2>(totalRegistries, Allocator.Persistent);
			c_matrix = new NativeArray<Matrix4x4>(totalRegistries, Allocator.Persistent);
			c_resolution = new NativeArray<int>(totalRegistries, Allocator.Persistent);
			c_size = new NativeArray<float>(totalRegistries, Allocator.Persistent);

			c_depthCaches = new NativeArray<float>(totalLength, Allocator.Persistent);
						
			int registryIndex = 0;
			int cacheStart = 0;
			foreach(var depthC in depthCachesRegistry.Keys)
			{
				int endOfCache = cacheStart + depthCachesRegistry[depthC].Length;

				c_segmentRegistry[registryIndex] = new int2(cacheStart, endOfCache);
				c_matrix[registryIndex] = depthC.transform.worldToLocalMatrix;
				c_resolution[registryIndex] = depthC.Resolution;
				c_size[registryIndex] = depthC.transform.lossyScale.x;

				NativeArray<float>.Copy(depthCachesRegistry[depthC], 0, c_depthCaches, cacheStart, depthCachesRegistry[depthC].Length);

				cacheStart = endOfCache;
				registryIndex++;
			}
		}

		/// <summary>
		/// Dispose storage
		/// </summary>
		public static void Cleanup()
		{
			s_initialised = false;

			s_handleHeights.Complete();

			s_waveNumbers.Dispose();
			s_amps.Dispose();
			s_windDirX.Dispose();
			s_windDirZ.Dispose();
			s_phases.Dispose();
			s_chopAmps.Dispose();

			s_localQueryPositions.Dispose();
			s_queryPositionsMatrixes.Dispose();
			s_heightQueryPositions.Dispose();
			s_worldQueryPositions.Dispose();
			s_resultHeights.Dispose();

			c_segmentRegistry.Dispose();
			c_matrix.Dispose();
			c_resolution.Dispose();
			c_size.Dispose();
			c_depthCaches.Dispose();

			// Dispose the temp jobs
			if(_segments.IsCreated) _segments.Dispose();
			if(_matrixes.IsCreated) _matrixes.Dispose();
		}

		/// <summary>
		/// Updates the local query positions and the transform that is being used (creates space for them the first time). If the query count doesn't match a new set of query
		/// position data will be created. This will force any running jobs to complete. The jobs will be kicked off in LateUpdate,
		/// so this should be called before the kick-off, such as from Update.
		/// </summary>
		/// <returns>True if successful.</returns>
		public static bool UpdateQueryPoints(int guid, Transform samplerTransform, Vector3[] localQueryPoints)
		{
			// Call this in case the user has not called it.
			CompleteJobs();

			// Get segment
			var segmentRetrieved = false;
			int2 querySegment;
			if(s_segmentRegistry.TryGetValue(guid, out querySegment))
			{
				// make sure segment size matches our query count
				var segmentSize = querySegment[1] - querySegment[0];
				if(segmentSize == localQueryPoints.Length)
				{
					// All good
					segmentRetrieved = true;
				}
				else
				{
					// Query count does not match segment - remove it. The segment will be recreated below.
					s_segmentRegistry.Remove(guid);
				}
			}

			// If no segment was retrieved, add one if there is space
			if(!segmentRetrieved)
			{
				if(s_lastQueryIndexHeights + localQueryPoints.Length > MAX_QUERIES)
				{
					Debug.LogError("Out of query data space. Try calling Compact() to reorganise query segments.");
					return false;
				}

				querySegment = new int2(s_lastQueryIndexHeights, s_lastQueryIndexHeights + localQueryPoints.Length);
				s_segmentRegistry.Add(guid, querySegment);
				s_transformsRegistry.Add(guid, samplerTransform);
				s_lastQueryIndexHeights += localQueryPoints.Length;
			}

			// Always updates which transform is being used
			s_transformsRegistry[guid] = samplerTransform;

			// Save the local the query data to the query segment location		
			NativeArray<Vector3>.Copy(localQueryPoints, 0, s_localQueryPositions, querySegment.x, querySegment.y - querySegment.x);

			return true;
		}

		/// <summary>
		/// Signal that the query storage for a particular guid is no longer required. This will leave air bubbles in the buffer -
		/// call CompactQueryStorage() to reorganise.
		/// </summary>
		public static void RemoveQueryPoints(int guid)
		{
			if(s_segmentRegistry.ContainsKey(guid))
			{
				s_segmentRegistry.Remove(guid);
			}
		}

		/// <summary>
		/// Change segment IDs to make them contiguous. This will invalidate any jobs and job results!
		/// </summary>
		public static void CompactQueryStorage()
		{
			// Make sure jobs are not running
			CompleteJobs();

			// A bit sneaky but just clear the registry. Will force segments to recreate which achieves the desired effect.
			s_segmentRegistry.Clear();
			s_lastQueryIndexHeights = 0;
		}

		/// <summary>
		/// Retrieve result data from jobs.
		/// </summary>
		/// <returns>True if data returned, false if failed</returns>
		public static bool RetrieveResultHeights(int guid, ref float[] outHeights)
		{
			var segment = new int2(0, 0);
			if(!s_segmentRegistry.TryGetValue(guid, out segment))
			{
				return false;
			}

			if(outHeights.Length != segment.y - segment.x)
				outHeights = new float[segment.y - segment.x];

			s_resultHeights.Slice(segment.x, segment.y - segment.x).CopyTo(outHeights);
			return true;
		}

		/// <summary>
		/// Run the jobs
		/// </summary>
		/// <returns>True if jobs kicked off, false if jobs already running.</returns>
		public static bool ScheduleJobs()
		{
			if(s_jobsRunning)
			{
				return false;
			}

			if(s_lastQueryIndexHeights == 0)
			{
				// Nothing to do
				return true;
			}

			s_jobsRunning = true;

			if(_segments.IsCreated) _segments.Dispose();
			if(_matrixes.IsCreated) _matrixes.Dispose();

			// Create a list of guid matrixes (do this every time a schedule happens since the matrixes are always updating)
			// Does it this way to not generate managed garbage
			NativeArray<int> guids = new NativeArray<int>(s_segmentRegistry.Count, Allocator.Temp);
			int index = 0;
			foreach(var guid in s_segmentRegistry.Keys)
				guids[index++] = guid;

			_segments = new NativeArray<int2>(guids.Length, Allocator.TempJob);
			_matrixes = new NativeArray<Matrix4x4>(guids.Length, Allocator.TempJob);

			for(int i = 0, l = guids.Length; i < l; i++)
			{
				int2 segment;
				s_segmentRegistry.TryGetValue(guids[i], out segment); // this should NEVER be false

				_segments[i] = segment;

				Transform trans;
				if(s_transformsRegistry.TryGetValue(guids[i], out trans))
				{
					_matrixes[i] = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);
					//matrixes[i] = trans.localToWorldMatrix;
				}
				// else a new matrix is empty which SHOULD transform that just based on world
			}

			guids.Dispose();

			var matrixJob = new MatrixTransformJob()
			{
				_querySegments = _segments,
				_guidMatrixes = _matrixes,

				_segmentRegistry = c_segmentRegistry,
				_matrix = c_matrix,
				_resolution = c_resolution,
				_size = c_size,

				_depthCaches = c_depthCaches,

				_localPositions = s_localQueryPositions,
				_worldQueryPositions = s_worldQueryPositions,
				_depthAtHeight = s_heightQueryPositions,
			};


			var heightJob = new HeightJob()
			{
				_attenuationInShallows = 0.95f, // TODO - hook this up with the info from the simulation settings
				_waveNumbers = s_waveNumbers,
				_amps = s_amps,
				_windDirX = s_windDirX,
				_windDirZ = s_windDirZ,
				_phases = s_phases,
				_chopAmps = s_chopAmps,
				_numWaveVecs = _waveVecCount,
				_queryPositions = s_worldQueryPositions,
				_depths = s_heightQueryPositions,
				_computeSegment = new int2(0, s_localQueryPositions.Length),
				_time = OceanRenderer.Instance.CurrentTime,
				_outHeights = s_resultHeights,
				_seaLevel = OceanRenderer.Instance.SeaLevel,
				_dontUseDepth = false,
			};

			JobHandle handler = matrixJob.Schedule();
			s_handleHeights = heightJob.Schedule(s_lastQueryIndexHeights, 32, handler);

			JobHandle.ScheduleBatchedJobs();

			s_firstFrame = false;

			return true;
		}

		/// <summary>
		/// Ensure that jobs are completed. Blocks until complete.
		/// </summary>
		public static void CompleteJobs()
		{
			if(!s_firstFrame && s_jobsRunning)
			{
				s_handleHeights.Complete();
				s_jobsRunning = false;
			}

			if(_segments.IsCreated) _segments.Dispose();
			if(_matrixes.IsCreated) _matrixes.Dispose();
		}
        		
		/// <summary>
		/// This sets up the proper matrixes so that the local points can be transformed to world points
		/// </summary>
		[BurstCompile]
		public struct MatrixTransformJob : IJob
		{
			[ReadOnly] public NativeArray<int2> _querySegments; // same length as the guid matrixes
			[ReadOnly] public NativeArray<Matrix4x4> _guidMatrixes; // same length as the query segements
			[ReadOnly] public NativeArray<Vector3> _localPositions;

			[ReadOnly] public NativeArray<int2> _segmentRegistry;
			[ReadOnly] public NativeArray<Matrix4x4> _matrix;
			[ReadOnly] public NativeArray<int> _resolution;
			[ReadOnly] public NativeArray<float> _size;

			[ReadOnly] public NativeArray<float> _depthCaches;

			[WriteOnly] public NativeArray<float2> _worldQueryPositions;
			[WriteOnly] public NativeArray<float> _depthAtHeight;

			public void Execute()
			{
				for(int segmentIndex = 0, segmentsLength = _querySegments.Length; segmentIndex < segmentsLength; segmentIndex++)
				{
					int2 segment = _querySegments[segmentIndex];

					for(int i = segment.x, l = segment.y - segment.x; i < l; i++)
					{
						float3 worldPos = _guidMatrixes[segmentIndex].MultiplyPoint3x4(_localPositions[i]);

						// Find the height at this point
						_depthAtHeight[i] = FindHeightAtWorldPosition(worldPos, _segmentRegistry, _matrix, _resolution, _size, _depthCaches);

						_worldQueryPositions[i] = new float2(worldPos.x, worldPos.z);
					}
				}
			}

			//This function is messy and not done well.Feel free to clean up!
			//public float FindHeightAtWorldPosition(float3 worldTestPos,
			//	NativeArray<int2> segmentRegistry, NativeArray<Matrix4x4> matrix, NativeArray<int> resolution, NativeArray<float> size,
			//	NativeArray<float> depthCaches)
			//{
			//	float returnHeight = 10000;

			//	for(int i = 0, l = _segmentRegistry.Length; i < l; i++)
			//	{
			//		// Brings the local test point to local (should be aligned properly in the grid)
			//		float3 point = matrix[i].MultiplyPoint3x4(worldTestPos);
			//		float2 localTestPoint = new float2(point.x, point.z);

			//		float halfSize = size[i] / 2;
			//		float radisuSqr = halfSize * halfSize;
			//		radisuSqr += radisuSqr;

			//		// Ignore this height query since it is not within the radius of this at all
			//		if(math.length(localTestPoint) > radisuSqr)
			//			continue;

			//		// Moves the test point into a positive spot so we can figure out where in the grid it is
			//		localTestPoint += new float2(halfSize, halfSize);

			//		float2 testIntPoint = new float2(localTestPoint.x / size[i], localTestPoint.y / size[i]);
			//		testIntPoint = math.round(testIntPoint * resolution[i]);
			//		int flatPoint = (int)testIntPoint.y * resolution[i] + (int)testIntPoint.x + segmentRegistry[i].x;

			//		if(flatPoint >= segmentRegistry[i].x && flatPoint < segmentRegistry[i].y)
			//		{
			//			float possibleHeight = depthCaches[flatPoint];

			//			if(possibleHeight < returnHeight)
			//				returnHeight = possibleHeight;
			//		}
			//	}

			//	return returnHeight;
			//}

			public float FindHeightAtWorldPosition(float3 worldTestPos,
				NativeArray<int2> segmentRegistry, NativeArray<Matrix4x4> matrix, NativeArray<int> resolution, NativeArray<float> size,
				NativeArray<float> depthCaches)
			{
				float returnHeight = 10000;

				for(int i = 0, l = _segmentRegistry.Length; i < l; i++)
				{
					// Brings the local test point to local (should be aligned properly with the texture the grid)
					float3 point = matrix[i].MultiplyPoint3x4(worldTestPos);

					float xOffset = point.x;
					float zOffset = point.z;
					float r = size[i] * resolution[i] / 2f;
					if(math.abs(xOffset) >= r || math.abs(zOffset) >= r)
					{
						// outside of this collision data
						continue;
					}

					float u = 0.5f + 0.5f * xOffset / r;
					float v = 0.5f + 0.5f * zOffset / r;
					int x = (int)math.floor(u * resolution[i]);
					int y = (int)math.floor(v * resolution[i]);
					int id = segmentRegistry[i].x + y * resolution[i] + x;

					float possibleHeight = depthCaches[id];

					if(possibleHeight < returnHeight)
						returnHeight = possibleHeight;
				}

				return returnHeight;
			}
		}

	

	

		/// <summary>
		/// This inverts the displacement to get the true water height at a position.
		/// </summary>
		[BurstCompile]
		public struct HeightJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<float4> _waveNumbers;
			[ReadOnly]
			public NativeArray<float4> _amps;
			[ReadOnly]
			public NativeArray<float4> _windDirX;
			[ReadOnly]
			public NativeArray<float4> _windDirZ;
			[ReadOnly]
			public NativeArray<float4> _phases;
			[ReadOnly]
			public NativeArray<float4> _chopAmps;
			[ReadOnly]
			public int _numWaveVecs;
			
			[ReadOnly]
			public NativeArray<float2> _queryPositions;
			[ReadOnly]
			public NativeArray<float> _depths;

			[WriteOnly]
			public NativeArray<float> _outHeights;

			[ReadOnly]
			public float _time;
			[ReadOnly]
			public int2 _computeSegment;
			[ReadOnly]
			public float _seaLevel;
			[ReadOnly]
			public float _attenuationInShallows;
			[ReadOnly]
			public bool _dontUseDepth;

			// runtime
			const float PI = 3.141593f;
			float4 oneMinusAttenuation;

			public void Execute(int index)
			{
				oneMinusAttenuation = FindOneMinusAttenuation(_attenuationInShallows);

				if(index >= _computeSegment.x && index < _computeSegment.y - _computeSegment.x)
				{
					// This could be even faster if i could allocate scratch space to store intermediate calculation results (not supported by burst yet)

					float2 undisplacedPos = _queryPositions[index];
					float seaFloorHeight = _depths[index];

					for(int iter = 0; iter < 4; iter++)
					{
						float2 displacement = new float2();
						if(_dontUseDepth)
							displacement = ComputeDisplacementHoriz(undisplacedPos);
						else
							displacement = ComputeDisplacementHorizWithOceanFloorData(undisplacedPos, seaFloorHeight);

						// Correct the undisplaced position - goal is to find the position that displaces to the query position
						float2 error = undisplacedPos + displacement - _queryPositions[index];
						undisplacedPos -= error;
					}

					// Our height is now the vertical component of the displacement from the undisp pos
					if(_dontUseDepth)
						_outHeights[index] = ComputeDisplacementVert(undisplacedPos) + _seaLevel;
					else
						_outHeights[index] = ComputeDisplacementVertWithOceanFloorData(undisplacedPos, seaFloorHeight) + _seaLevel;
				}
			}

			#region Depthless Calculations (slightly faster)

			float2 ComputeDisplacementHoriz(float2 queryPos)
			{
				float2 displacement = 0f;

				for(var iwavevec = 0; iwavevec < _numWaveVecs; iwavevec++)
				{
					// Wave direction
					float4 Dx = _windDirX[iwavevec], Dz = _windDirZ[iwavevec];

					// Wave number
					float4 k = _waveNumbers[iwavevec];

					// SIMD Dot product of wave direction with query pos
					float4 x = Dx * queryPos.x + Dz * queryPos.y;

					// Angle
					float4 t = k * x + _phases[iwavevec];

					// Add the four SIMD results
					float4 disp = -_chopAmps[iwavevec] * math.sin(t);
					displacement.x += math.csum(Dx * disp);
					displacement.y += math.csum(Dz * disp);
				}

				return displacement;
			}

			float ComputeDisplacementVert(float2 queryPos)
			{
				float height = 0f;

				for(var iwavevec = 0; iwavevec < _numWaveVecs; iwavevec++)
				{
					// Wave direction
					float4 Dx = _windDirX[iwavevec], Dz = _windDirZ[iwavevec];

					// Wave number
					float4 k = _waveNumbers[iwavevec];

					// SIMD Dot product of wave direction with query pos
					float4 x = Dx * queryPos.x + Dz * queryPos.y;

					// Angle
					float4 t = k * x + _phases[iwavevec];

					// Add the four SIMD results
					height += math.csum(_amps[iwavevec] * math.cos(t));
				}

				return height;
			}

			#endregion

			float2 ComputeDisplacementHorizWithOceanFloorData(float2 queryPos, float depth)
			{
				float2 displacement = 0f;

				for(var iWaveVec = 0; iWaveVec < _numWaveVecs; iWaveVec++)
				{
					//float4 depth_wt = saturate(Depth / (0.5f * this.MinWaveLength)); // slightly different result - do per wavelength for now
					float4 depth_wt = saturate(depth * _waveNumbers[iWaveVec] / PI);
					//// keep some proportion of amplitude so that there is some waves remaining					
					float4 wt = _attenuationInShallows * depth_wt + oneMinusAttenuation;

					// Wave direction
					float4 Dx = _windDirX[iWaveVec];
					float4 Dz = _windDirZ[iWaveVec];

					// Wave number
					float4 k = _waveNumbers[iWaveVec];

					// SIMD Dot product of wave direction with query pos
					float4 x = Dx * queryPos.x + Dz * queryPos.y;

					// Angle
					float4 t = k * x + _phases[iWaveVec];

					// Add the four SIMD results
					float4 disp = -_chopAmps[iWaveVec] * math.sin(t);

					displacement.x += math.dot(disp.x * Dx.x, wt.x);
					displacement.x += math.dot(disp.y * Dx.y, wt.y);
					displacement.x += math.dot(disp.z * Dx.z, wt.z);
					displacement.x += math.dot(disp.w * Dx.w, wt.w);

					displacement.y += math.dot(disp.x * Dz.x, wt.x);
					displacement.y += math.dot(disp.y * Dz.y, wt.y);
					displacement.y += math.dot(disp.z * Dz.z, wt.z);
					displacement.y += math.dot(disp.w * Dz.w, wt.w);
				}

				return displacement;
			}

			float ComputeDisplacementVertWithOceanFloorData(float2 queryPos, float depth)
			{
				float height = 0f;
				
				float PI = 3.141593f;
				float4 oneMinusAttenuation = FindOneMinusAttenuation(_attenuationInShallows);

				for(var iWaveVec = 0; iWaveVec < _numWaveVecs; iWaveVec++)
				{
					//float4 depth_wt = saturate(Depth / (0.5f * this.MinWaveLength)); // slightly different result - do per wavelength for now
					float4 depth_wt = saturate(depth * _waveNumbers[iWaveVec] / PI);
					//// keep some proportion of amplitude so that there is some waves remaining					
					float4 wt = _attenuationInShallows * depth_wt + oneMinusAttenuation;

					// Wave direction
					float4 Dx = _windDirX[iWaveVec];
					float4 Dz = _windDirZ[iWaveVec];

					// Wave number
					float4 k = _waveNumbers[iWaveVec];

					// SIMD Dot product of wave direction with query pos
					float4 x = Dx * queryPos.x + Dz * queryPos.y;

					// Angle
					float4 t = k * x + _phases[iWaveVec];

					// Add the four SIMD results
					height += math.dot((_amps[iWaveVec] * math.cos(t)).x, wt.x);
					height += math.dot((_amps[iWaveVec] * math.cos(t)).y, wt.y);
					height += math.dot((_amps[iWaveVec] * math.cos(t)).z, wt.z);
					height += math.dot((_amps[iWaveVec] * math.cos(t)).w, wt.w);
				}

				return height;
			}

			float4 FindOneMinusAttenuation(float attenuationInShallows)
			{
				return new float4(1 - attenuationInShallows, 1 - attenuationInShallows, 1 - attenuationInShallows, 1 - attenuationInShallows);
			}

			float4 saturate(float4 floatToSaturate)
			{
				floatToSaturate.x = math.max(0, math.min(1, floatToSaturate.x));
				floatToSaturate.y = math.max(0, math.min(1, floatToSaturate.y));
				floatToSaturate.z = math.max(0, math.min(1, floatToSaturate.z));
				floatToSaturate.w = math.max(0, math.min(1, floatToSaturate.w));

				return floatToSaturate;
			}			
		}

		/// <summary>
		/// This returns the vertical component of the wave displacement at a position.
		/// </summary>
		[BurstCompile]
		public struct VerticalDisplacementJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<float4> _waveNumbers;
			[ReadOnly]
			public NativeArray<float4> _amps;
			[ReadOnly]
			public NativeArray<float4> _windDirX;
			[ReadOnly]
			public NativeArray<float4> _windDirZ;
			[ReadOnly]
			public NativeArray<float4> _phases;
			[ReadOnly]
			public NativeArray<float4> _chopAmps;
			[ReadOnly]
			public int _numWaveVecs;

			[ReadOnly]
			public NativeArray<float2> _queryPositions;

			[WriteOnly]
			public NativeArray<float> _outHeights;

			[ReadOnly]
			public float _time;
			[ReadOnly]
			public float _globalWindAngle;
			[ReadOnly]
			public int2 _computeSegment;
			[ReadOnly]
			public float _seaLevel;

			public void Execute(int index)
			{
				if(index >= _computeSegment.x && index < _computeSegment.y - _computeSegment.x)
				{
					float resultHeight = 0f;

					for(var iwavevec = 0; iwavevec < _numWaveVecs; iwavevec++)
					{
						// Wave direction
						float4 Dx = _windDirX[iwavevec], Dz = _windDirZ[iwavevec];

						// Wave number
						float4 k = _waveNumbers[iwavevec];

						// SIMD Dot product of wave direction with query pos
						float4 x = Dx * _queryPositions[index].x + Dz * _queryPositions[index].y;

						// Angle
						float4 t = k * x + _phases[iwavevec];

						resultHeight += math.csum(_amps[iwavevec] * math.cos(t));
					}

					_outHeights[index] = resultHeight + _seaLevel;
				}
			}
		}

		///// <summary>
		///// This inverts the displacement to get the true water height at a position.
		///// </summary>
		//[BurstCompile]
		//public struct HeightJob : IJobParallelFor
		//      {
		//	// TODO - Allow use to not use some waves in case we want less accuracy on the boat

		//          [ReadOnly]
		//          public NativeArray<float4> _waveNumbers;
		//          [ReadOnly]
		//          public NativeArray<float4> _amps;
		//          [ReadOnly]
		//          public NativeArray<float4> _windDirX;
		//          [ReadOnly]
		//          public NativeArray<float4> _windDirZ;
		//          [ReadOnly]
		//          public NativeArray<float4> _phases;
		//          [ReadOnly]
		//          public NativeArray<float4> _chopAmps;
		//          [ReadOnly]
		//          public int _numWaveVecs;

		//	[ReadOnly]
		//	public NativeArray<Matrix4x4> _boatMatrixes;
		//	[ReadOnly]
		//          public NativeArray<Vector3> _localQueryPositions;

		//          [WriteOnly]
		//          public NativeArray<float> _outHeights;

		//          [ReadOnly]
		//          public float _time;
		//          [ReadOnly]
		//          public int2 _computeSegment;
		//          [ReadOnly]
		//          public float _seaLevel;

		//          public void Execute(int index)
		//          {
		//		if (index >= _computeSegment.x && index < _computeSegment.y - _computeSegment.x)
		//              {
		//			float3 worldPos = _boatMatrixes[index].MultiplyPoint3x4(_localQueryPositions[index]);
		//			float2 queryPosition = new float2(worldPos.x, worldPos.z);

		//			// This could be even faster if i could allocate scratch space to store intermediate calculation results (not supported by burst yet)

		//			float2 undisplacedPos = queryPosition;

		//                  for (int iter = 0; iter < 4; iter++)
		//                  {
		//                      float2 displacement = ComputeDisplacementHoriz(undisplacedPos);

		//                      // Correct the undisplaced position - goal is to find the position that displaces to the query position
		//                      float2 error = undisplacedPos + displacement - queryPosition;
		//                      undisplacedPos -= error;
		//                  }

		//                  // Our height is now the vertical component of the displacement from the undisp pos
		//                  _outHeights[index] = ComputeDisplacementVert(undisplacedPos) + _seaLevel;
		//              }
		//          }

		//	float2 ComputeDisplacementHoriz(float2 queryPos)
		//	{
		//		float2 displacement = 0f;

		//		for(var iwavevec = 0; iwavevec < _numWaveVecs; iwavevec++)
		//		{
		//			// Wave direction
		//			float4 Dx = _windDirX[iwavevec], Dz = _windDirZ[iwavevec];

		//			// Wave number
		//			float4 k = _waveNumbers[iwavevec];

		//			// SIMD Dot product of wave direction with query pos
		//			float4 x = Dx * queryPos.x + Dz * queryPos.y;

		//			// Angle
		//			float4 t = k * x + _phases[iwavevec];

		//			// Add the four SIMD results
		//			float4 disp = -_chopAmps[iwavevec] * math.sin(t);
		//			displacement.x += math.csum(Dx * disp);
		//			displacement.y += math.csum(Dz * disp);
		//		}

		//		return displacement;
		//	}

		//	float ComputeDisplacementVert(float2 queryPos)
		//	{
		//		float height = 0f;

		//		for(var iwavevec = 0; iwavevec < _numWaveVecs; iwavevec++)
		//		{
		//			// Wave direction
		//			float4 Dx = _windDirX[iwavevec], Dz = _windDirZ[iwavevec];

		//			// Wave number
		//			float4 k = _waveNumbers[iwavevec];

		//			// SIMD Dot product of wave direction with query pos
		//			float4 x = Dx * queryPos.x + Dz * queryPos.y;

		//			// Angle
		//			float4 t = k * x + _phases[iwavevec];

		//			// Add the four SIMD results
		//			height += math.csum(_amps[iwavevec] * math.cos(t));
		//		}

		//		return height;
		//	}
		//}
	}
}
