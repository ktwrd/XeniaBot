namespace XeniaBot.Core.Helpers;
using Image = NetVips.Image;
using NetVips;

public static class NetVipsHelper
{
    /// <summary>
    /// vips_image_get_n_pages implemented in C#
    /// </summary>
    /// <param name="image">Image to fetch page count</param>
    /// <returns>Page count for image. Will be 1 to 1000</returns>
    public static int GetNPages(Image image)
    {
        var a = image.GetTypeOf("n-pages");
        var b = image.Get("n-pages");
        if (b != null)
        {
            var c = (int)b;
            if (c is > 1 and < 10000)
                return c;
        }
        return 1;
    }
    public static Image Normalize(Image img)
    {
        if (!img.HasAlpha())
            return img.Bandjoin(255);
        else
            return img;
        if (img.Bands < 3)
            img = img.Colourspace(Enums.Interpretation.Srgb);

        if (img.Bands == 3)
            img = img.Bandjoin(255);

        return img;
    }
}