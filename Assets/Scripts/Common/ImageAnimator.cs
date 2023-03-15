using UnityEngine;
using UnityEngine.UI;

public class ImageAnimator : MonoBehaviour
{
	[SerializeField] private Image Target;
	[SerializeField] private Sprite[] AnimationFrames;
	[SerializeField] private float FrameTime = 0.1f;

	private int currentFrame = 0;
	private float timeSinceLastFrame = 0;
	
	public void SetAnimation(Sprite[] gemsCollectionGem)
	{
		AnimationFrames = gemsCollectionGem;
		currentFrame = 0;
		timeSinceLastFrame = 0;
	}

	private void Update()
	{
		if (AnimationFrames == null || AnimationFrames.Length == 0) return;
		
		timeSinceLastFrame += Time.deltaTime;
		if (timeSinceLastFrame >= FrameTime)
		{
			timeSinceLastFrame = 0;
			currentFrame++;
			if (currentFrame >= AnimationFrames.Length)
			{
				currentFrame = 0;
			}
			Target.sprite = AnimationFrames[currentFrame];
		}
	}
}
