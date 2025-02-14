# MatrixRain

![Console View](MatrixRain.gif)

# Matrix Rain Effect

This repository demonstrates how to integrate the Matrix Rain effect into your application.

## Usage Instructions

To integrate the Matrix Rain effect, follow these steps:

1. **Capture the Original Console Output:**  
   Save the original `Console.Out` so you can reference it later if needed.

2. **Initialize and Start the Matrix Rain Effect:**  
   Create a new instance of `MatrixRainEffect` with your desired window dimensions, and then start the effect.

3. **Redirect Console Output:**  
   Replace the default `Console.Out` with a custom writer (`MatrixRainWriter`) that directs all console output to the Matrix Rain window.

4. **Write to the Console:**  
   After setup, any output from `Console.WriteLine` will appear within the matrix effect. The console window is also resizable without disrupting the effect.

### Example Code

```csharp
// Capture the original Console.Out before redirection.
TextWriter originalOut = Console.Out;

// Create and start the Matrix Rain effect with an initial window size.
_effect = new MatrixRainEffect(originalOut, maxContentWidth: 51, maxContentHeight: 5);
_effect.Start();

// Redirect Console.WriteLine so that its output goes to the text window.
Console.SetOut(new MatrixRainWriter(_effect));

// Everything else after this setup is pretty straight forward.
Console.WriteLine("Hello World");

// The console window is resizable without messing up the Rain Effect.
