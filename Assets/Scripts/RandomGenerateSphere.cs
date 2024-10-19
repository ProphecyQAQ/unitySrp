using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomGenerateSphere : MonoBehaviour
{
    // Start is called before the first frame update
    public Material[] material;
    static MaterialPropertyBlock block;
    void Start()
    {
        for (int i = -2; i < 2; i ++)
        {
            for (int j = -2; j < 2; j ++)
            {

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = new Vector3(i*2, 0, j*2);

                Renderer renderer = sphere.GetComponent<Renderer>();
                renderer.material = material[Random.Range(0, 3)];
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
