using System;
using System.IO;
using System.Threading;
using MatrixRainEffectDemo;

class Program
{
    private static MatrixRainEffect _effect;
    static void Main(string[] args)
    {
        // Capture the original Console.Out before redirection.
        TextWriter originalOut = Console.Out;

        // Create and start the Matrix Rain effect with an initial window size.
        _effect = new MatrixRainEffect(originalOut, maxContentWidth: 51, maxContentHeight: 5);
        _effect.Start();

        // Redirect Console.WriteLine so that its output goes to the text window.
        Console.SetOut(new MatrixRainWriter(_effect));

        WriteFileContents("BannerText.txt", 51, 5,0);
        Thread.Sleep(3000);
        WriteFileContents("Pitch.txt", 65, 12,250);
        Thread.Sleep(3000);
        WriteFileContents("SciFi.txt",65, 5,500);
        Thread.Sleep(3000);

        _effect.ClearText();
        Console.ReadKey();

        Console.Clear();
    }

    private static void WriteFileContents(string filePath, int width, int height,int lineDelay)
    {
        _effect.ClearText();
        _effect.SetWindowSize(width, height);

        foreach (var line in File.ReadAllLines(@"DemoContent\"+filePath))
        {
            Console.WriteLine(line);
            Thread.Sleep(lineDelay);
        }
    }
}
