using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Svg;
using Svg.Transforms;

namespace Bitwarden.AutoType.Desktop.Helpers;
public static class SvgExtensions
{
    public static SvgDocument CreateSvgDocumentFromPathData(string pathData, Color color)
    {
        var svgDocument = new SvgDocument();
        var pathElement = new SvgPath { PathData = SvgPathBuilder.Parse(pathData) };
        svgDocument.Children.Add(pathElement);

        // Apply color to the path element
        pathElement.Fill = new SvgColourServer(color);

        return svgDocument;
    }

    public static Icon GetIconFromSvgDocument(SvgDocument svgDocument, int width, int height)
    {
        float scaleX, scaleY;
        if (svgDocument.ViewBox.Width > 0 && svgDocument.ViewBox.Height > 0)
        {
            scaleX = width / svgDocument.ViewBox.Width;
            scaleY = height / svgDocument.ViewBox.Height;
        }
        else
        {
            var dimensions = svgDocument.GetDimensions();
            scaleX = width / dimensions.Width;
            scaleY = height / dimensions.Height;
        }

        svgDocument.Width = new SvgUnit(width);
        svgDocument.Height = new SvgUnit(height);
        if (svgDocument.Transforms == null)
        {
            svgDocument.Transforms = new SvgTransformCollection();
        }
        svgDocument.Transforms.Add(new SvgScale(scaleX, scaleY));

        using var bitmap = svgDocument.Draw();
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        memoryStream.Position = 0;
        using var iconBitmap = new Bitmap(memoryStream);
        return Icon.FromHandle(iconBitmap.GetHicon());
    }

    public static Icon GetIconFromEmbeddedSvg(string resourceName, int width, int height, Color color)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using Stream? svgStream = assembly.GetManifestResourceStream(resourceName);

        if (svgStream == null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found.");
        }

        var svgDocument = SvgDocument.Open<SvgDocument>(svgStream);

        // Apply color to all elements recursively
        ApplyColorToSvgElements(svgDocument.Children, color);

        float scaleX, scaleY;
        if (svgDocument.ViewBox.Width > 0 && svgDocument.ViewBox.Height > 0)
        {
            scaleX = width / svgDocument.ViewBox.Width;
            scaleY = height / svgDocument.ViewBox.Height;
        }
        else
        {
            var dimensions = svgDocument.GetDimensions();
            scaleX = width / dimensions.Width;
            scaleY = height / dimensions.Height;
        }

        svgDocument.Width = new SvgUnit(width);
        svgDocument.Height = new SvgUnit(height);
        if (svgDocument.Transforms == null)
        {
            svgDocument.Transforms = new SvgTransformCollection();
        }
        svgDocument.Transforms.Add(new SvgScale(scaleX, scaleY));

        using var bitmap = svgDocument.Draw();
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        memoryStream.Position = 0;
        using var iconBitmap = new Bitmap(memoryStream);
        return Icon.FromHandle(iconBitmap.GetHicon());
    }

    private static void ApplyColorToSvgElements(SvgElementCollection elements, Color color)
    {
        foreach (var element in elements)
        {
            if (element is SvgVisualElement visualElement)
            {
                if (visualElement.Fill != SvgPaintServer.None)
                {
                    visualElement.Fill = new SvgColourServer(color);
                }
                if (visualElement.Stroke != SvgPaintServer.None)
                {
                    visualElement.Stroke = new SvgColourServer(color);
                }
            }

            ApplyColorToSvgElements(element.Children, color);
        }
    }
}