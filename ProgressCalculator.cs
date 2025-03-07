public class ProgressCalculator
{
    /// <summary>
    /// 計算一對一的進度（每章節分數均等）
    /// </summary>
    public static float CalculateOneToOneProgress(int completedChapters, int totalChapters)
    {
        if (totalChapters <= 0) return 0f;
        return (completedChapters / (float)totalChapters) * 100f;
    }

    /// <summary>
    /// 計算教室進度（支援每章節不同權重）
    /// </summary>
    public static float CalculateClassroomProgress(int completedChapters, int totalChapters, float[] chapterWeights = null)
    {
        if (totalChapters <= 0) return 0f;

        if (chapterWeights == null || chapterWeights.Length != totalChapters)
        {
            // 沒傳weights，預設平均
            return (completedChapters / (float)totalChapters) * 100f;
        }
        else
        {
            // 如果每章節分數不一樣，根據weights來算
            float earned = 0f;
            float total = 0f;
            for (int i = 0; i < totalChapters; i++)
            {
                total += chapterWeights[i];
                if (i < completedChapters)
                {
                    earned += chapterWeights[i];
                }
            }
            return (earned / total) * 100f;
        }
    }

    /// <summary>
    /// 整體進度 = 一對一+教室各50%
    /// </summary>
    public static float CalculateOverallProgress(float progressOneToOne, float progressClassroom)
    {
        return (progressOneToOne * 0.5f) + (progressClassroom * 0.5f);
    }
}
