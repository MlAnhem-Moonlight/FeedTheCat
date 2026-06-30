using UnityEngine;

public class ChangeImage : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image image;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeSprite(bool isOn)
    {
        image.sprite = isOn ? onSprite : offSprite;
    }
}
