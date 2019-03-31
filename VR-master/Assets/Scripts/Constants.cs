public class Constants
{
    // 1) For No Feedback Sessions 
	public static bool IS_MI = true;
    public static int NUMBER_OF_CLASSES = 3;
	// 20 for MI and 15 for ME
    public static int TRIALS_PER_CLASS_PER_SESSION = IS_MI ? 15 : 10;
    public static float IMAGERY_SECONDS = 4f;
    public static float BLANK_SCREEN_SECONDS = 1f;
    public static float PREP_CROSS_SECONDS = 1f;
    public static float INITIAL_DELAY_SECONDS = 2f;
}