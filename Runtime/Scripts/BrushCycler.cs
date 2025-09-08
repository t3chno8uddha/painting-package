using UnityEngine;

public class BrushCycler : MonoBehaviour
{
    public Texture2D[] brushArray;
    public ParticleSystem pSys;

    private void Start()
    {
        Renderer particleRenderer = pSys.GetComponent<Renderer>();
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        
        particleRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetTexture("_MainTex", brushArray[Random.Range(0, brushArray.Length)]);
        
        particleRenderer.SetPropertyBlock(propertyBlock);
    }
}