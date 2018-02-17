using UnityEngine;

namespace Crest
{
    public class OceanFX : MonoBehaviour
    {
        [Range(0, 63)]
        public int _samplesPerFrame = 32;

        ShapeGerstnerBatched _gerstner;
        public ParticleSystem _particlePrefab;

        void Start()
        {
            _gerstner = GetComponent<ShapeGerstnerBatched>();
        }

        public float _emitThresh = 0.7f;
        public float _minYVel = 1f;
        public bool _showPoints = false;

        void Update()
        {
            for (int i = 0; i < _samplesPerFrame; i++)
            {
                // this tests on screen positions undisplaced positions. it might be interesting to look a bit outside frustum.
                var randomPos = new Vector3(Random.value * Screen.width, Random.value * Screen.height, 0f);
                var ray = Camera.main.ScreenPointToRay(randomPos);
                // if close to or above horizon, just skip this sample. another option would be to negate the y component of the direction
                if (ray.direction.y > -0.01f)
                    continue;

                var queryPos = ray.origin + ray.direction * -ray.origin.y / ray.direction.y;
                var disp = _gerstner.GetDisplacement(queryPos, 0f);
                if (_showPoints)
                {
                    Debug.DrawLine(queryPos, queryPos + disp);
                }

                float ss = 0.5f;

                Vector3 disp_x = Vector3.right * ss;
                Vector3 disp_z = Vector3.forward * ss;
                disp_x += _gerstner.GetDisplacement(disp_x + queryPos, 0f);
                disp_z += _gerstner.GetDisplacement(disp_z + queryPos, 0f);

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

                // compute velocity
                float dt = 1f / 60f;
                //Vector3 disp_tm = _gerstner.GetDisplacement(pt, -dt);
                Vector3 disp_tp = _gerstner.GetDisplacement(queryPos, dt);
                Vector3 vel = (disp_tp - disp) / dt;
                //float yacc = (disp_tm.y + disp_tp.y - 2f * disp.y) / (dt * dt);

                if (det >= _emitThresh && vel.y >= _minYVel)
                {
                    var ps = Instantiate(_particlePrefab.transform);
                    ps.position = queryPos + disp;
                    ps.LookAt(ps.position + Vector3.Lerp(Vector3.up, vel, Random.value));
                    ps.GetComponent<TrackOceanSurface>()._basePosition = queryPos;
                }
            }
        }
    }
}
