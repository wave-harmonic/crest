using UnityEngine;

public class OceanFX : MonoBehaviour
{
    public Transform[] _markers;
    Crest.ShapeGerstnerBatched _gerstner;
    public ParticleSystem _particles;

    Vector3[] basePositions;
    float[] moveTimes;

	void Start()
    {
        _gerstner = FindObjectOfType<Crest.ShapeGerstnerBatched>();

        basePositions = new Vector3[_markers.Length];
        moveTimes = new float[_markers.Length];

        for (int i = 0; i < _markers.Length; i++)
        {
            basePositions[i] = _markers[i].position;
        }
    }

    public float _emitThresh = 0.7f;
    public float _maxTimeStationary = 0.25f;
    public float _minYVel = 1f;
    public bool _showPoints = false;

    void Update()
    {
        for (int i = 0; i < _markers.Length; i++)
        {
            Transform marker = _markers[i];

            float ss = 0.5f;

            Vector3 pt = basePositions[i];

            Vector3 disp = _gerstner.GetDisplacement(pt, 0f);
            marker.position = pt + disp;

            Vector3 disp_x = Vector3.right * ss;
            Vector3 disp_z = Vector3.forward * ss;
            disp_x += _gerstner.GetDisplacement(disp_x + pt, 0f);
            disp_z += _gerstner.GetDisplacement(disp_z + pt, 0f);

            float dux = disp_x.x - disp.x;
            float duy = disp_x.z - disp.z;
            float duz = disp_z.x - disp.x;
            float duw = disp_z.z - disp.z;
            // The determinant of the displacement Jacobian is a good measure for turbulence:
            // > 1: Stretch
            // < 1: Squash
            // < 0: Overlap
            //float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
            //Vector4 du = new Vector4()
            float det = (dux * duw - duy * duz) / (ss * ss);
            //_marker.GetComponent<Renderer>().material.color = Color.white * (1f - det);
            det = Mathf.InverseLerp(1.6f, 0f, det);

            float dt = 1f / 60f;
            //Vector3 disp_tm = _gerstner.GetDisplacement(pt, -dt);
            Vector3 disp_tp = _gerstner.GetDisplacement(pt, dt);
            Vector3 vel = (disp_tp - disp) / dt;
            //float yacc = (disp_tm.y + disp_tp.y - 2f * disp.y) / (dt * dt);
            //float accScale = 0.2f;
            //yacc *= accScale;

            float col = det; // * -yacc;

            if (col < _emitThresh || vel.y < _minYVel)
            {
                col = 0f;
            }
            else
            {
                col = 1f;

                //ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

                //emitParams.position = marker.transform.position + Random.onUnitSphere * 0.5f;

                //Vector3 v = _yVelMul * vel;
                //v += addVel;
                //v.y = Mathf.Abs(v.y);
                //v += 0.4f * Random.onUnitSphere;
                //emitParams.velocity = _yVelMul * vel;
                //_particles.Emit(emitParams, 1);

                //new Vector3(rad * 2f * (Random.value - 0.5f), 0f, rad * 2f * (Random.value - 0.5f));
                //_particles.Emit(marker.transform.position, Vector3.up * 10f, 1f, 1f, Color.white);
                MoveRand(i);

                var ps = Instantiate(_particles.transform);
                ps.position = marker.transform.position;
                ps.LookAt(ps.position + Vector3.Lerp(Vector3.up, vel, Random.value));
            }

            Renderer rend = marker.GetComponent<Renderer>();
            rend.enabled = _showPoints;
            rend.material.color = Color.white * col;

            if (TimeSinceMove(i) > _maxTimeStationary)
                MoveRand(i);
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
