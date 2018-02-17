using UnityEngine;

namespace Crest
{
    public class OceanFX : MonoBehaviour
    {
        [Range(0, 63)]
        public int _samplesPerFrame = 32;

        ShapeGerstnerBatched _gerstner;
        public ParticleSystem _particles;

        Vector3[] basePositions;
        float[] moveTimes;

        void Start()
        {
            _gerstner = GetComponent<ShapeGerstnerBatched>();

            basePositions = new Vector3[_samplesPerFrame];
            moveTimes = new float[_samplesPerFrame];

            for (int i = 0; i < _samplesPerFrame; i++)
            {
                basePositions[i] = Camera.main.transform.position + 50f * Random.insideUnitSphere;
                basePositions[i].y = 0f;
                moveTimes[i] = 0f;
            }
        }

        public float _emitThresh = 0.7f;
        public float _maxTimeStationary = 0.25f;
        public float _minYVel = 1f;
        public bool _showPoints = false;

        void Update()
        {
            for (int i = 0; i < _samplesPerFrame; i++)
            {
                float ss = 0.5f;

                Vector3 disp = _gerstner.GetDisplacement(basePositions[i], 0f);

                if (_showPoints)
                {
                    Debug.DrawLine(basePositions[i], basePositions[i] + disp);
                }

                Vector3 disp_x = Vector3.right * ss;
                Vector3 disp_z = Vector3.forward * ss;
                disp_x += _gerstner.GetDisplacement(disp_x + basePositions[i], 0f);
                disp_z += _gerstner.GetDisplacement(disp_z + basePositions[i], 0f);

                float dux = disp_x.x - disp.x;
                float duy = disp_x.z - disp.z;
                float duz = disp_z.x - disp.x;
                float duw = disp_z.z - disp.z;
                // The determinant of the displacement Jacobian is a good measure for turbulence:
                // > 1: Stretch
                // < 1: Squash
                // < 0: Overlap
                float det = (dux * duw - duy * duz) / (ss * ss);
                det = Mathf.InverseLerp(1.6f, 0f, det);

                float dt = 1f / 60f;
                //Vector3 disp_tm = _gerstner.GetDisplacement(pt, -dt);
                Vector3 disp_tp = _gerstner.GetDisplacement(basePositions[i], dt);
                Vector3 vel = (disp_tp - disp) / dt;
                //float yacc = (disp_tm.y + disp_tp.y - 2f * disp.y) / (dt * dt);

                if (det >= _emitThresh && vel.y >= _minYVel)
                {
                    var ps = Instantiate(_particles.transform);
                    ps.position = basePositions[i] + disp;
                    ps.LookAt(ps.position + Vector3.Lerp(Vector3.up, vel, Random.value));
                    ps.GetComponent<TrackOceanSurface>()._basePosition = basePositions[i];

                    MoveRand(i);
                }
                else if (TimeSinceMove(i) > _maxTimeStationary)
                {
                    MoveRand(i);
                }
            }
        }

        float TimeSinceMove(int i)
        {
            return Time.time - moveTimes[i];
        }


        void MoveRand(int i)
        {
            float radForwards = 80f;
            float rad = 40f;
            basePositions[i] = Camera.main.transform.position + Random.value * radForwards * Camera.main.transform.forward + 2f * (Random.value - 0.5f) * rad * Camera.main.transform.right;
            basePositions[i].y = 0f;

            moveTimes[i] = Time.time;
        }
    }
}
