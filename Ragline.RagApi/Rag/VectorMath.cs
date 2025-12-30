namespace Ragline.RagApi.Rag;

public static class VectorMath
{
    public static double Cosine(float[] a, float[] b)
    {
        // embeddings are normalized in Python -> cosine = dot product
        double sum = 0;
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) sum += a[i] * b[i];
        return sum;
    }
}
