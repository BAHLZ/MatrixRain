# Matrix Rain Effect

![Console View](MatrixRain.gif)

This repository demonstrates how to integrate the Matrix Rain effect into your console application.

## Usage Instructions

1. **Capture the Original Console Output:**  
   Save the original `Console.Out` so you can reference it later if needed.

2. **Initialize and Start the Matrix Rain Effect:**  
   Create a new instance of `MatrixRainEffect` with your desired window dimensions, and then start the effect.

3. **Redirect Console Output:**  
   Replace the default `Console.Out` with a custom writer (`MatrixRainWriter`) that directs all console output to the Matrix Rain window.

4. **Display Content:**  
   Use `Console.WriteLine` to send text to the matrix effect window. You can also use `_effect.ClearText();` to clear the inside window and remove it from the matrix rain effect.

## Full Example

Below is a complete example of how to use the Matrix Rain effect in your console application:

```csharp
using MatrixRainEffectDemo;

class Program
{
    private static MatrixRainEffect? _effect;
    static void Main(string[] args)
    {
        // Capture the original Console.Out before redirection.
        TextWriter originalOut = Console.Out;

        // Create and start the Matrix Rain effect with an initial window size.
        _effect = new MatrixRainEffect(originalOut, maxContentWidth: 51, maxContentHeight: 5);
        _effect.Start();

        // Redirect Console.WriteLine so that its output goes to the text window.
        Console.SetOut(new MatrixRainWriter(_effect));

        WriteFileContents("BannerText.txt", 51, 5, 0);
        Thread.Sleep(3000);
        WriteFileContents("Pitch.txt", 65, 12, 250);
        Thread.Sleep(3000);
        WriteFileContents("SciFi.txt", 65, 5, 500);
        Thread.Sleep(3000);

        // Clear the inside window and remove it from the matrix rain effect.
        _effect.ClearText();
        Console.ReadKey();

        Console.Clear();
    }

    private static void WriteFileContents(string filePath, int width, int height, int lineDelay)
    {
        _effect.ClearText();
        _effect.SetWindowSize(width, height);

        foreach (var line in File.ReadAllLines(@"DemoContent\" + filePath))
        {
            Console.WriteLine(line);
            Thread.Sleep(lineDelay);
        }
    }
}
```

Here is a much more simplified version of the implementation

```csharp
using MatrixRainEffectDemo;

class Program
{
    static void Main(string[] args)
    {
        // Capture the original Console.Out.
        TextWriter originalOut = Console.Out;

        // Initialize and start the Matrix Rain effect with a simple window size.
        var effect = new MatrixRainEffect(originalOut, maxContentWidth: 51, maxContentHeight: 5);
        effect.Start();

        // Redirect Console output to the Matrix Rain effect.
        Console.SetOut(new MatrixRainWriter(effect));

        // Write "Hello World" to the matrix window.
        Console.WriteLine("Hello World");

        // Optional: Wait and then clear the matrix window.
        Thread.Sleep(3000);
        effect.ClearText();
    }
}
